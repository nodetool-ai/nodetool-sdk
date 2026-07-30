using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Connection;
using Nodetool.SDK.Diagnostics;
using Nodetool.SDK.Types;
using Nodetool.SDK.Values;
using Nodetool.SDK.WebSocket;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Implementation of the NodeTool execution client using WebSocket communication.
/// Manages connections, session tracking, and message routing.
/// </summary>
public class NodeToolExecutionClient : INodeToolExecutionClient
{
    private readonly INodeToolWebSocketTransport _webSocketClient;
    private readonly ILogger<NodeToolExecutionClient> _logger;
    private readonly NodeToolClientOptions _options;
    private readonly ConcurrentDictionary<string, ExecutionSession> _sessions;
    private readonly ConcurrentDictionary<string, string> _workflowIdsByName;
    private readonly ConcurrentDictionary<string, byte> _recoveryMonitors = new();
    private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
    private readonly Uri _serverUri;
    private readonly string? _apiKey;
    private string? _resolvedAuthToken;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private int _reconnectLoopActive;
    private volatile bool _disconnectRequested;
    private volatile bool _hasConnectedBefore;
    private volatile bool _autoReconnectEnabled;
    private volatile bool _disposed;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc/>
    public bool IsConnected => _webSocketClient.IsConnected;

    /// <inheritdoc/>
    public string ConnectionStatus { get; private set; } = "disconnected";

    /// <inheritdoc/>
    public string? LastError { get; private set; }

    /// <inheritdoc/>
    public string? ExecutionTargetId { get; private set; }

    internal string? ResolvedAuthToken => _resolvedAuthToken;

    public bool AutoReconnectEnabled
    {
        get => _autoReconnectEnabled;
        set => _autoReconnectEnabled = value;
    }

    /// <inheritdoc/>
    public event Action<string>? ConnectionStatusChanged;

    /// <summary>
    /// Creates a new NodeTool execution client.
    /// </summary>
    /// <param name="options">Client options (endpoints + auth).</param>
    /// <param name="apiKey">Optional API key for authentication.</param>
    /// <param name="logger">Logger instance.</param>
    public NodeToolExecutionClient(
        NodeToolClientOptions options,
        string? apiKey = null,
        ILogger<NodeToolExecutionClient>? logger = null,
        INodeToolWebSocketTransport? webSocketTransport = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.ReadRetryPolicy.Validate();

        // We accept http/https schemes as a convenience (convert to ws/wss),
        // but callers must provide explicit host/port/path in options.
        var wsUri = _options.GetNormalizedWorkerWebSocketUrl();

        // If caller provided a host+port root, we still try the conventional /ws path.
        // (This mirrors current server defaults and keeps samples ergonomic.)
        if (!wsUri.AbsolutePath.EndsWith("/ws", StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(wsUri);
            builder.Path = builder.Path.TrimEnd('/') + "/ws";
            wsUri = builder.Uri;
        }

        _serverUri = wsUri;
        _apiKey = apiKey;
        AutoReconnectEnabled = _options.AutoReconnect;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NodeToolExecutionClient>.Instance;
        _sessions = new ConcurrentDictionary<string, ExecutionSession>();
        _workflowIdsByName = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Create WebSocket client
        _webSocketClient =
            webSocketTransport ??
            new MessagePackWebSocketClient(_logger);

        // Subscribe to WebSocket events
        _webSocketClient.MessageReceived += OnMessageReceived;
        _webSocketClient.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ExecutionTargetId = null;
        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            _disconnectRequested = false;
            ConnectionStatus = "connecting";
            ConnectionStatusChanged?.Invoke(ConnectionStatus);

            _resolvedAuthToken = NormalizeToken(
                _options.TokenProvider == null
                    ? _options.AuthToken ?? _apiKey
                    : await _options.TokenProvider
                        .GetTokenAsync(cancellationToken)
                        .ConfigureAwait(false));
            var result = await _webSocketClient.ConnectAsync(
                _serverUri,
                _resolvedAuthToken,
                cancellationToken);

            if (result)
            {
                ConnectionStatus = "connected";
                LastError = null;
                var shouldReconnectJobs = _hasConnectedBefore;
                _hasConnectedBefore = true;
                if (shouldReconnectJobs)
                    await ReconnectActiveJobsAsync(cancellationToken);
            }
            else
            {
                ConnectionStatus = "error";
                LastError = "Connection failed";
            }

            ConnectionStatusChanged?.Invoke(ConnectionStatus);
            return result;
        }
        catch (Exception ex)
        {
            var safeError = NodeToolDiagnosticRedactor.RedactText(
                ex.Message,
                _resolvedAuthToken);
            _logger.LogError(
                "Failed to connect to NodeTool server at {Uri}: {Error}",
                NodeToolDiagnosticRedactor.RedactUri(_serverUri),
                safeError);
            ConnectionStatus = "error";
            LastError = safeError;
            ConnectionStatusChanged?.Invoke(ConnectionStatus);
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        _disconnectRequested = true;
        await _connectionSemaphore.WaitAsync();
        try
        {
            await _webSocketClient.DisconnectAsync();
            ConnectionStatus = "disconnected";
            ConnectionStatusChanged?.Invoke(ConnectionStatus);
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <inheritdoc/>
    public Task<IExecutionSession> ExecuteWorkflowAsync(
        string workflowId,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
        => ExecuteWorkflowAsync(
            workflowId,
            inputs,
            executionOptions: null,
            cancellationToken);

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteWorkflowAsync(
        string workflowId,
        Dictionary<string, object>? inputs,
        WorkflowExecutionOptions? executionOptions,
        CancellationToken cancellationToken = default)
    {
        // Pre-bind the client-generated job ID before sending. The current worker protocol
        // preserves this ID, so even very fast updates can be routed without a pending-session race.
        var jobId = Guid.NewGuid().ToString();
        var session = CreateSession(jobId, workflowId);

        var command = new WebSocketCommand
        {
            command = "run_job",
            type = "run_job",
            data = new RunJobRequest
            {
                JobId = jobId,
                WorkflowId = workflowId,
                Params = inputs,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _resolvedAuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
                ExecutionOptions = CreateRunJobExecutionOptions(executionOptions),
            }
        };

        await SendExecutionRequestAsync(command, jobId, session, cancellationToken);

        return session;
    }

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflowName))
        {
            throw new ArgumentException("Workflow name must not be empty.", nameof(workflowName));
        }

        var workflowId = await ResolveWorkflowIdByNameAsync(workflowName, cancellationToken);
        return await ExecuteWorkflowAsync(workflowId, inputs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        string inputName,
        object? inputValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputName))
        {
            throw new ArgumentException("Input name must not be empty.", nameof(inputName));
        }

        var inputs = new Dictionary<string, object> { [inputName] = inputValue! };
        return ExecuteWorkflowByNameAsync(workflowName, inputs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        CancellationToken cancellationToken = default,
        params (string Name, object? Value)[] inputs)
    {
        var dict = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (name, value) in inputs)
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            dict[name] = value!;
        }
        return ExecuteWorkflowByNameAsync(workflowName, dict, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteGraphAsync(
        Graph graph,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString();
        var session = CreateSession(jobId, jobId);

        var command = new WebSocketCommand
        {
            command = "run_job",
            type = "run_job",
            data = new RunJobRequest
            {
                JobId = jobId,
                WorkflowId = jobId,
                Graph = graph,
                Params = inputs,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _resolvedAuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
            }
        };

        await SendExecutionRequestAsync(command, jobId, session, cancellationToken);

        return session;
    }

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
        => await ExecuteNodeAsync(
            nodeType,
            inputs,
            executionOptions: null,
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object>? inputs,
        WorkflowExecutionOptions? executionOptions,
        CancellationToken cancellationToken = default)
    {
        // Create a simple graph with just this node
        var nodeId = Guid.NewGuid().ToString();
        var jobId = Guid.NewGuid().ToString();
        var session = CreateSession(jobId, nodeId);
        var graph = new Graph
        {
            nodes = new List<GraphNode>
            {
                new GraphNode
                {
                    id = nodeId,
                    type = nodeType,
                    data = inputs ?? new Dictionary<string, object>()
                }
            },
            edges = new List<GraphEdge>()
        };

        var command = new WebSocketCommand
        {
            command = "run_job",
            type = "run_job",
            data = new RunJobRequest
            {
                JobId = jobId,
                WorkflowId = nodeId,
                Graph = graph,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _resolvedAuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
                ExecutionOptions =
                    CreateRunJobExecutionOptions(executionOptions),
            }
        };

        await SendExecutionRequestAsync(command, jobId, session, cancellationToken);

        return session;
    }

    /// <summary>
    /// Cancel a running job.
    /// </summary>
    public async Task CancelJobAsync(string jobId, string? workflowId = null, CancellationToken cancellationToken = default)
    {
        var command = new WebSocketCommand
        {
            command = "cancel_job",
            type = "cancel_job",
            data = new CancelJobData { job_id = jobId, workflow_id = workflowId }
        };

        await _webSocketClient.SendMessageAsync(command, cancellationToken);
    }

    private Task StreamInputAsync(
        StreamInputData data,
        CancellationToken cancellationToken)
        => SendLiveExecutionCommandAsync(
            "stream_input",
            data,
            cancellationToken);

    private Task EndInputStreamAsync(
        EndInputStreamData data,
        CancellationToken cancellationToken)
        => SendLiveExecutionCommandAsync(
            "end_input_stream",
            data,
            cancellationToken);

    private Task UpdateNodePropertiesAsync(
        UpdateNodePropertiesData data,
        CancellationToken cancellationToken)
        => SendLiveExecutionCommandAsync(
            "update_node_properties",
            data,
            cancellationToken);

    private async Task SendLiveExecutionCommandAsync(
        string commandName,
        object data,
        CancellationToken cancellationToken)
    {
        var command = new WebSocketCommand
        {
            command = commandName,
            type = commandName,
            data = data
        };
        if (!await _webSocketClient
                .SendMessageAsync(command, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Failed to send '{commandName}' for the active execution.");
        }
    }

    internal static WebSocketCommand CreateReconnectCommand(string jobId, string? workflowId)
        => new()
        {
            command = "reconnect_job",
            type = "reconnect_job",
            data = new ReconnectJobData { job_id = jobId, workflow_id = workflowId }
        };

    private async Task ReconnectActiveJobsAsync(CancellationToken cancellationToken)
    {
        foreach (var session in _sessions.Values.Where(session => !session.IsCompleted))
        {
            if (!await _webSocketClient.SendMessageAsync(
                    CreateReconnectCommand(session.JobId, session.WorkflowId),
                    cancellationToken))
            {
                _logger.LogWarning("Failed to reconnect active job {JobId}", session.JobId);
                continue;
            }
            if (_recoveryMonitors.TryAdd(session.JobId, 0))
                _ = MonitorReconnectedJobAsync(session, _lifetimeCts.Token);
        }
    }

    private async Task MonitorReconnectedJobAsync(
        ExecutionSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!session.IsCompleted && IsConnected && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                if (session.IsCompleted || !IsConnected)
                    return;
                await _webSocketClient.SendMessageAsync(
                    CreateReconnectCommand(session.JobId, session.WorkflowId),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disposal or another disconnect ends recovery polling.
        }
        finally
        {
            _recoveryMonitors.TryRemove(session.JobId, out _);
        }
    }

    internal ExecutionSession CreateSession(string jobId, string? workflowId = null)
    {
        var session = new ExecutionSession(jobId, workflowId)
        {
            CancelAction = CancelJobAsync,
            StreamInputAction = StreamInputAsync,
            EndInputStreamAction = EndInputStreamAsync,
            UpdateNodePropertiesAction = UpdateNodePropertiesAsync
        };
        _sessions[jobId] = session;
        return session;
    }

    internal static RunJobExecutionOptions? CreateRunJobExecutionOptions(
        WorkflowExecutionOptions? options)
    {
        if (options is null)
            return null;

        return new RunJobExecutionOptions
        {
            Persistence = options.Persistence switch
            {
                WorkflowPersistence.Session => "session",
                _ => "job"
            },
            EventDetail = options.EventDetail switch
            {
                WorkflowEventDetail.Outputs => "outputs",
                WorkflowEventDetail.Terminal => "terminal",
                _ => "full"
            },
            AssetPersistence = options.AssetPersistence switch
            {
                WorkflowAssetPersistence.Temporary => "temporary",
                _ => "auto"
            }
        };
    }

    private async Task SendExecutionRequestAsync(
        WebSocketCommand command,
        string jobId,
        ExecutionSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await _webSocketClient.SendMessageAsync(command, cancellationToken))
                return;

            _sessions.TryRemove(jobId, out _);
            session.ProcessJobUpdate(new JobUpdate
            {
                job_id = jobId,
                status = "failed",
                error = "Failed to send execution request"
            });
        }
        catch
        {
            _sessions.TryRemove(jobId, out _);
            session.Dispose();
            throw;
        }
    }

    private async Task<string> ResolveWorkflowIdByNameAsync(string workflowName, CancellationToken cancellationToken)
    {
        if (_workflowIdsByName.TryGetValue(workflowName, out var cachedWorkflowId))
        {
            _logger.LogDebug("Using cached workflow ID for '{WorkflowName}': {WorkflowId}", workflowName, cachedWorkflowId);
            return cachedWorkflowId;
        }

        var workflows = await GetWorkflowsAsync(cancellationToken);
        var matches = workflows
            .Where(w => string.Equals(w.Name, workflowName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            var available = string.Join(", ", workflows.Select(w => w.Name));
            throw new InvalidOperationException($"Workflow not found: '{workflowName}'. Available: {available}");
        }

        if (matches.Count > 1)
        {
            var ids = string.Join(", ", matches.Select(w => $"{w.Id} ({w.Name})"));
            throw new InvalidOperationException(
                $"Multiple workflows named '{workflowName}' found: {ids}. Use ExecuteWorkflowAsync(workflowId, ...) to disambiguate.");
        }

        var resolvedWorkflowId = matches[0].Id;
        _workflowIdsByName[workflowName] = resolvedWorkflowId;
        return resolvedWorkflowId;
    }

    /// <inheritdoc/>
    public async Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching node types via WebSocket");
        var raw = await SendReadRequestAsync(
            "list_nodes",
            new Dictionary<string, object?> { ["fields"] = "full" },
            cancellationToken);
        return DeserializeListResult<NodeMetadataResponse>(raw, "list_nodes", "nodes");
    }

    private static string? NormalizeToken(string? token)
        => string.IsNullOrWhiteSpace(token) ? null : token.Trim();

    private async Task<Dictionary<string, object?>?> SendReadRequestAsync(
        string command,
        Dictionary<string, object?>? data = null,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var policy = _options.ReadRetryPolicy;
        for (var attempt = 1;
             attempt <= policy.MaximumAttempts;
             attempt++)
        {
            try
            {
                if (!_webSocketClient.IsConnected &&
                    !await ConnectAsync(cancellationToken)
                        .ConfigureAwait(false))
                {
                    throw new InvalidOperationException(
                        LastError ??
                        "Failed to connect before WebSocket read RPC.");
                }

                return await _webSocketClient.SendRequestAsync(
                    command,
                    data,
                    cancellationToken,
                    timeout,
                    requestId).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (
                cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                IsTransientReadFailure(ex) &&
                attempt < policy.MaximumAttempts)
            {
                var delay = policy.GetDelay(attempt);
                _logger.LogWarning(
                    "WebSocket read RPC {Command} attempt {Attempt} failed; retrying after {DelayMs} ms: {Error}",
                    command,
                    attempt,
                    delay.TotalMilliseconds,
                    NodeToolDiagnosticRedactor.RedactText(
                        ex.Message,
                        _resolvedAuthToken,
                        _apiKey));
                await Task.Delay(delay, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException(
            $"WebSocket read RPC '{command}' exhausted its retry policy.");
    }

    private static bool IsTransientReadFailure(Exception exception)
        => exception is
            InvalidOperationException or
            OperationCanceledException or
            IOException or
            System.Net.WebSockets.WebSocketException;

    /// <inheritdoc/>
    public async Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
        int cursor = 0,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (cursor < 0)
            throw new ArgumentOutOfRangeException(nameof(cursor));
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit));

        var raw = await SendReadRequestAsync(
            "get_node_type_inventory",
            new Dictionary<string, object?>
            {
                ["cursor"] = cursor,
                ["limit"] = limit
            },
            cancellationToken);
        var result = DeserializeRequiredResult<NodeTypeInventoryResponse>(
            raw,
            "get_node_type_inventory");
        if (result.Version != 1 || !result.RegistryReady)
            throw new InvalidDataException(
                "The server returned an unsupported or unready node type inventory.");
        return result;
    }

    /// <inheritdoc/>
    public async Task<NodeMetadataResponse?> GetNodeAsync(string nodeType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching node {NodeType} via WebSocket", nodeType);
        var raw = await SendReadRequestAsync(
            "get_node",
            new Dictionary<string, object?> { ["node_type"] = nodeType },
            cancellationToken);
        return DeserializeSingleResult<NodeMetadataResponse>(raw, "get_node");
    }

    /// <inheritdoc/>
    public async Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching workflows via WebSocket");
        var raw = await SendReadRequestAsync(
            "list_workflows",
            new Dictionary<string, object?>(),
            cancellationToken);
        return DeserializeListResult<WorkflowResponse>(raw, "list_workflows", "workflows");
    }

    /// <inheritdoc/>
    public async Task<WorkflowResponse?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching workflow {WorkflowId} via WebSocket", workflowId);
        var raw = await SendReadRequestAsync(
            "get_workflow",
            new Dictionary<string, object?> { ["id"] = workflowId },
            cancellationToken);
        return DeserializeSingleResult<WorkflowResponse>(raw, "get_workflow");
    }

    /// <inheritdoc/>
    public async Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        const int pageSize = 50;
        var workflows = new List<WorkflowSummaryResponse>();
        var visitedCursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;

        do
        {
            var data = new Dictionary<string, object?> { ["limit"] = pageSize };
            if (cursor != null)
                data["cursor"] = cursor;
            var raw = await SendReadRequestAsync(
                "list_workflow_summaries",
                data,
                cancellationToken);
            var page = DeserializeRequiredResult<WorkflowSummaryListResponse>(
                raw,
                "list_workflow_summaries");
            workflows.AddRange(page.Workflows);
            cursor = string.IsNullOrWhiteSpace(page.Next) ? null : page.Next;
            if (cursor != null && !visitedCursors.Add(cursor))
                throw new InvalidDataException(
                    $"The workflow summary cursor repeated ({cursor}); pagination cannot advance.");
        }
        while (cursor != null);

        return workflows;
    }

    /// <inheritdoc/>
    public async Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        var raw = await SendReadRequestAsync(
            "get_workflow_interface",
            new Dictionary<string, object?>
            {
                ["id"] = workflowId,
                ["version"] = 1
            },
            cancellationToken);
        var result = DeserializeRequiredResult<WorkflowInterfaceResponse>(
            raw,
            "get_workflow_interface");
        ValidateWorkflowInterface(result, workflowId);
        return result;
    }

    /// <inheritdoc/>
    public async Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
        IReadOnlyCollection<string> workflowIds,
        CancellationToken cancellationToken = default)
    {
        ValidateWorkflowIds(workflowIds);
        var ids = workflowIds.ToArray();
        var raw = await SendReadRequestAsync(
            "get_workflow_interfaces",
            new Dictionary<string, object?>
            {
                ["ids"] = ids,
                ["version"] = 1
            },
            cancellationToken);
        var result = DeserializeRequiredResult<WorkflowInterfacesResponse>(
            raw,
            "get_workflow_interfaces");
        var requestedIds = ids.ToHashSet(StringComparer.Ordinal);
        foreach (var workflowInterface in result.Interfaces)
        {
            ValidateWorkflowInterface(workflowInterface, workflowInterface.WorkflowId);
            if (!requestedIds.Contains(workflowInterface.WorkflowId))
                throw new InvalidDataException(
                    "The workflow-interface batch contained an unrequested contract.");
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<List<AssetResponse>> GetAssetsAsync(
        string? contentType = null,
        string? parentId = null,
        int pageSize = 10000,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching assets via WebSocket");
        var data = new Dictionary<string, object?> { ["page_size"] = pageSize };
        if (contentType != null) data["content_type"] = contentType;
        if (parentId != null) data["parent_id"] = parentId;
        var raw = await SendReadRequestAsync(
            "list_assets",
            data,
            cancellationToken);
        return DeserializeListResult<AssetResponse>(raw, "list_assets", "assets");
    }

    /// <inheritdoc/>
    public async Task<AssetResponse?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching asset {AssetId} via WebSocket", assetId);
        var raw = await SendReadRequestAsync(
            "get_asset",
            new Dictionary<string, object?> { ["id"] = assetId },
            cancellationToken);
        return DeserializeSingleResult<AssetResponse>(raw, "get_asset");
    }

    private static void ThrowIfRpcError(Dictionary<string, object?>? raw, string command)
    {
        if (raw is null)
            throw new InvalidOperationException($"No response received for '{command}'");
        if (raw.TryGetValue("error", out var err) && err is not null)
        {
            var errorMap = NodeToolValue.From(err).AsMapOrEmpty();
            var code = errorMap.GetValueOrDefault("code")?.AsString() ?? "UNKNOWN";
            var msg = errorMap.GetValueOrDefault("message")?.AsString() ?? err.ToString()!;
            var apiCode = errorMap.GetValueOrDefault("apiCode")?.AsString();
            throw new SdkApiException(
                SdkApiTransport.WebSocket,
                apiCode ?? code,
                string.Equals(
                    apiCode,
                    "SERVICE_UNAVAILABLE",
                    StringComparison.Ordinal),
                $"[{code}] {msg}");
        }
    }

    internal T DeserializeRequiredResult<T>(
        Dictionary<string, object?>? raw,
        string command)
    {
        ThrowIfRpcError(raw, command);
        if (raw is null || !raw.TryGetValue("result", out var resultObj) || resultObj is null)
            throw new InvalidDataException($"The '{command}' response did not contain a result.");
        return JsonSerializer.Deserialize<T>(
            NodeToolValue.From(resultObj).ToJsonString(),
            _jsonOptions) ?? throw new InvalidDataException(
                $"The '{command}' response result could not be deserialized.");
    }

    private static void ValidateWorkflowIds(IReadOnlyCollection<string> workflowIds)
    {
        ArgumentNullException.ThrowIfNull(workflowIds);
        if (workflowIds.Count is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(workflowIds),
                "Expected between 1 and 100 workflow IDs.");
        if (workflowIds.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Workflow IDs cannot be empty.", nameof(workflowIds));
        if (workflowIds.Distinct(StringComparer.Ordinal).Count() != workflowIds.Count)
            throw new ArgumentException("Workflow IDs must be unique.", nameof(workflowIds));
    }

    private static void ValidateWorkflowInterface(
        WorkflowInterfaceResponse workflowInterface,
        string expectedWorkflowId)
    {
        if (workflowInterface.Version != 1 ||
            !string.Equals(workflowInterface.Source, "server", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Workflow {expectedWorkflowId} returned an unsupported workflow-interface contract.");
        }
        if (!string.Equals(workflowInterface.WorkflowId, expectedWorkflowId, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"Workflow-interface response ID '{workflowInterface.WorkflowId}' does not match '{expectedWorkflowId}'.");
    }

    internal List<T> DeserializeListResult<T>(Dictionary<string, object?>? raw, string command, string key)
    {
        ThrowIfRpcError(raw, command);
        if (raw is null || !raw.TryGetValue("result", out var resultObj)) return new List<T>();
        var resultMap = NodeToolValue.From(resultObj).AsMapOrEmpty();
        if (!resultMap.TryGetValue(key, out var listValue))
            return new List<T>();
        return JsonSerializer.Deserialize<List<T>>(
            listValue.ToJsonString(),
            _jsonOptions) ?? new List<T>();
    }

    private T? DeserializeSingleResult<T>(Dictionary<string, object?>? raw, string command) where T : class
    {
        ThrowIfRpcError(raw, command);
        if (raw is null || !raw.TryGetValue("result", out var resultObj) || resultObj is null)
            return null;
        return JsonSerializer.Deserialize<T>(
            NodeToolValue.From(resultObj).ToJsonString(),
            _jsonOptions);
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs args)
    {
        try
        {
            RouteExecutionMessage(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Error processing WebSocket message: {Error}",
                NodeToolDiagnosticRedactor.RedactText(
                    ex.Message,
                    _resolvedAuthToken));
        }
    }

    internal void RouteExecutionMessage(object message)
    {
        if (message is Dictionary<string, object?> envelope &&
            envelope.TryGetValue("type", out var type) &&
            string.Equals(
                type as string,
                "sdk_execution_target",
                StringComparison.Ordinal) &&
            envelope.TryGetValue("runner_id", out var runnerId) &&
            runnerId is string id &&
            !string.IsNullOrWhiteSpace(id))
        {
            ExecutionTargetId = id;
            return;
        }

        switch (message)
        {
            case JobUpdate jobUpdate:
                if (jobUpdate.job_id != null && _sessions.TryGetValue(jobUpdate.job_id, out var session1))
                {
                    session1.ProcessJobUpdate(jobUpdate);

                    // Clean up completed sessions after a delay
                    if (session1.IsCompleted)
                    {
                        var jobIdToRemove = jobUpdate.job_id;
                        _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(t =>
                        {
                            _sessions.TryRemove(jobIdToRemove, out var removed);
                        });
                    }
                }
                break;

            case NodeUpdate nodeUpdate:
                // Prefer routing by job_id when available to avoid cross-talk between sessions.
                if (nodeUpdate.job_id != null && _sessions.TryGetValue(nodeUpdate.job_id, out var sessionNode))
                {
                    sessionNode.ProcessNodeUpdate(nodeUpdate);
                }
                else
                {
                    GetOnlyBoundSession()?.ProcessNodeUpdate(nodeUpdate);
                }
                break;

            case NodeProgress nodeProgress:
                if (nodeProgress.job_id != null &&
                    _sessions.TryGetValue(nodeProgress.job_id, out var progressSession))
                {
                    progressSession.ProcessNodeProgress(nodeProgress);
                }
                else
                    GetOnlyBoundSession()?.ProcessNodeProgress(nodeProgress);
                break;

            case ProgressUpdate progressUpdate:
                if (progressUpdate.job_id != null && _sessions.TryGetValue(progressUpdate.job_id, out var session2))
                {
                    session2.ProcessProgressUpdate(progressUpdate);
                }
                break;

            case OutputUpdate outputUpdate:
                if (outputUpdate.job_id != null && _sessions.TryGetValue(outputUpdate.job_id, out var sessionOut))
                {
                    sessionOut.ProcessOutputUpdate(outputUpdate);
                }
                else
                {
                    GetOnlyBoundSession()?.ProcessOutputUpdate(outputUpdate);
                }
                break;

            case ChunkMessage chunk:
                if (chunk.job_id != null &&
                    _sessions.TryGetValue(chunk.job_id, out var streamSession))
                {
                    streamSession.ProcessStreamChunk(chunk);
                }
                break;

            case PreviewUpdate previewUpdate:
                if (previewUpdate.job_id != null && _sessions.TryGetValue(previewUpdate.job_id, out var sessionPreview))
                {
                    sessionPreview.ProcessPreviewUpdate(previewUpdate);
                }
                else
                {
                    GetOnlyBoundSession()?.ProcessPreviewUpdate(previewUpdate);
                }
                break;
        }
    }

    private ExecutionSession? GetOnlyBoundSession()
    {
        var sessions = _sessions.Values
            .Where(session => !session.IsCompleted)
            .Take(2)
            .ToArray();
        if (sessions.Length == 1)
            return sessions[0];
        if (sessions.Length > 1)
            _logger.LogWarning("Dropped an unscoped execution update while multiple jobs were active");
        return null;
    }

    private void OnConnectionStatusChanged(object? sender, ConnectionStatusEventArgs args)
    {
        ConnectionStatus = args.Status;
        if (args.Status == "error")
        {
            LastError = args.Message;
        }
        ConnectionStatusChanged?.Invoke(args.Status);
        if (args.Status == "disconnected" &&
            AutoReconnectEnabled &&
            !_disconnectRequested &&
            !_disposed &&
            Interlocked.CompareExchange(ref _reconnectLoopActive, 1, 0) == 0)
        {
            _ = RunReconnectLoopAsync();
        }
    }

    private async Task RunReconnectLoopAsync()
    {
        try
        {
            var delay = TimeSpan.FromMilliseconds(500);
            for (var attempt = 1; attempt <= 5 && !_disposed && !_disconnectRequested; attempt++)
            {
                try
                {
                    await Task.Delay(delay, _lifetimeCts.Token);
                    if (_disposed || _disconnectRequested || !AutoReconnectEnabled)
                        return;
                    if (await ConnectAsync(_lifetimeCts.Token))
                        return;
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Reconnect attempt {Attempt} failed: {Error}",
                        attempt,
                        NodeToolDiagnosticRedactor.RedactText(
                            ex.Message,
                            _resolvedAuthToken));
                }
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 8000));
            }
        }
        finally
        {
            Interlocked.Exchange(ref _reconnectLoopActive, 0);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _disconnectRequested = true;
        _lifetimeCts.Cancel();
        _webSocketClient.MessageReceived -= OnMessageReceived;
        _webSocketClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
        await _webSocketClient.DisposeAsync().ConfigureAwait(false);

        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
        _workflowIdsByName.Clear();
        _recoveryMonitors.Clear();
        _lifetimeCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
