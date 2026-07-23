using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Configuration;
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
    private readonly MessagePackWebSocketClient _webSocketClient;
    private readonly ILogger<NodeToolExecutionClient> _logger;
    private readonly NodeToolClientOptions _options;
    private readonly ConcurrentDictionary<string, ExecutionSession> _sessions;
    private readonly PendingExecutionSessions _pendingSessions;
    private readonly ConcurrentDictionary<string, string> _workflowIdsByName;
    private readonly Uri _serverUri;
    private readonly string? _apiKey;
    private bool _disposed;
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
        ILogger<NodeToolExecutionClient>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

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
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NodeToolExecutionClient>.Instance;
        _sessions = new ConcurrentDictionary<string, ExecutionSession>();
        _pendingSessions = new PendingExecutionSessions();
        _workflowIdsByName = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Create WebSocket client
        _webSocketClient = new MessagePackWebSocketClient(_logger);

        // Subscribe to WebSocket events
        _webSocketClient.MessageReceived += OnMessageReceived;
        _webSocketClient.ConnectionStatusChanged += OnConnectionStatusChanged;
    }

    /// <summary>
    /// Backwards-compat convenience constructor. Prefer <see cref="NodeToolExecutionClient(NodeToolClientOptions,string?,ILogger{NodeToolExecutionClient}?)"/>.
    /// </summary>
    [Obsolete("Pass explicit NodeToolClientOptions (no hardcoded localhost defaults).")]
    public NodeToolExecutionClient(
        string serverUrl,
        string? apiKey = null,
        ILogger<NodeToolExecutionClient>? logger = null)
        : this(
            new NodeToolClientOptions { WorkerWebSocketUrl = new Uri(serverUrl) },
            apiKey,
            logger)
    {
    }

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ConnectionStatus = "connecting";
            ConnectionStatusChanged?.Invoke(ConnectionStatus);

            var result = await _webSocketClient.ConnectAsync(_serverUri, cancellationToken);

            if (result)
            {
                ConnectionStatus = "connected";
                LastError = null;
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
            _logger.LogError(ex, "Failed to connect to NodeTool server at {Uri}", _serverUri);
            ConnectionStatus = "error";
            LastError = ex.Message;
            ConnectionStatusChanged?.Invoke(ConnectionStatus);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        await _webSocketClient.DisconnectAsync();
        ConnectionStatus = "disconnected";
        ConnectionStatusChanged?.Invoke(ConnectionStatus);
    }

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteWorkflowAsync(
        string workflowId,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
    {
        // Server assigns job_id; we start a pending session keyed by workflow_id.
        var session = CreatePendingSession(workflowId);

        var command = new WebSocketCommand
        {
            command = "run_job",
            type = "run_job",
            data = new RunJobRequest
            {
                WorkflowId = workflowId,
                Params = inputs,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _options.AuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
            }
        };

        var success = await _webSocketClient.SendMessageAsync(command, cancellationToken);
        if (!success)
        {
            _pendingSessions.Remove(workflowId, session);
            session.ProcessJobUpdate(new JobUpdate
            {
                status = "failed",
                error = "Failed to send execution request"
            });
        }

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
        // Use a non-empty pending key so we can bind the first job_update even if the server doesn't echo workflow_id.
        var pendingKey = Guid.NewGuid().ToString();
        var session = CreatePendingSession(workflowId: pendingKey);

        var command = new WebSocketCommand
        {
            command = "run_job",
            type = "run_job",
            data = new RunJobRequest
            {
                WorkflowId = pendingKey,
                Graph = graph,
                Params = inputs,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _options.AuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
            }
        };

        var success = await _webSocketClient.SendMessageAsync(command, cancellationToken);
        if (!success)
        {
            _pendingSessions.Remove(pendingKey, session);
            session.ProcessJobUpdate(new JobUpdate
            {
                status = "failed",
                error = "Failed to send execution request"
            });
        }

        return session;
    }

    /// <inheritdoc/>
    public async Task<IExecutionSession> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default)
    {
        // Create a simple graph with just this node
        var nodeId = Guid.NewGuid().ToString();
        // Use nodeId as the pending key for binding job updates.
        var session = CreatePendingSession(workflowId: nodeId);
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
                WorkflowId = nodeId,
                Graph = graph,
                JobType = "workflow",
                ExecutionStrategy = _options.ExecutionStrategy,
                ApiUrl = _options.ApiUrl,
                UserId = _options.UserId ?? "",
                AuthToken = _options.AuthToken ?? "",
                ExplicitTypes = _options.ExplicitTypes,
            }
        };

        var success = await _webSocketClient.SendMessageAsync(command, cancellationToken);
        if (!success)
        {
            _pendingSessions.Remove(nodeId, session);
            session.ProcessJobUpdate(new JobUpdate
            {
                status = "failed",
                error = "Failed to send execution request"
            });
        }

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

    private ExecutionSession CreateSession(string jobId)
    {
        var session = new ExecutionSession(jobId)
        {
            CancelAction = CancelJobAsync
        };
        _sessions[jobId] = session;
        return session;
    }

    private ExecutionSession CreatePendingSession(string workflowId)
    {
        var session = new ExecutionSession(jobId: "", workflowId: workflowId)
        {
            CancelAction = CancelJobAsync
        };
        _pendingSessions.Add(workflowId, session);
        return session;
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
        var raw = await _webSocketClient.SendRequestAsync(
            "list_nodes",
            new Dictionary<string, object?> { ["fields"] = "full" },
            cancellationToken);
        return DeserializeListResult<NodeMetadataResponse>(raw, "list_nodes", "nodes");
    }

    /// <inheritdoc/>
    public async Task<NodeMetadataResponse?> GetNodeAsync(string nodeType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching node {NodeType} via WebSocket", nodeType);
        var raw = await _webSocketClient.SendRequestAsync(
            "get_node",
            new Dictionary<string, object?> { ["node_type"] = nodeType },
            cancellationToken);
        return DeserializeSingleResult<NodeMetadataResponse>(raw, "get_node");
    }

    /// <inheritdoc/>
    public async Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching workflows via WebSocket");
        var raw = await _webSocketClient.SendRequestAsync(
            "list_workflows",
            new Dictionary<string, object?>(),
            cancellationToken);
        return DeserializeListResult<WorkflowResponse>(raw, "list_workflows", "workflows");
    }

    /// <inheritdoc/>
    public async Task<WorkflowResponse?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching workflow {WorkflowId} via WebSocket", workflowId);
        var raw = await _webSocketClient.SendRequestAsync(
            "get_workflow",
            new Dictionary<string, object?> { ["id"] = workflowId },
            cancellationToken);
        return DeserializeSingleResult<WorkflowResponse>(raw, "get_workflow");
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
        var raw = await _webSocketClient.SendRequestAsync("list_assets", data, cancellationToken);
        return DeserializeListResult<AssetResponse>(raw, "list_assets", "assets");
    }

    /// <inheritdoc/>
    public async Task<AssetResponse?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching asset {AssetId} via WebSocket", assetId);
        var raw = await _webSocketClient.SendRequestAsync(
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
            throw new InvalidOperationException($"[{code}] {msg}");
        }
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
            switch (args.Message)
            {
                case JobUpdate jobUpdate:
                    // Bind pending session (workflow_id -> job_id) on first update
                    if (jobUpdate.job_id != null)
                    {
                        if (_pendingSessions.TryTake(
                            jobUpdate.workflow_id,
                            out var pending,
                            out var matchedWorkflowId) &&
                            pending != null)
                        {
                            if (jobUpdate.workflow_id == null)
                            {
                                _logger.LogDebug(
                                    "Binding job_id {JobId} to the only pending session (workflow_id={WorkflowId})",
                                    jobUpdate.job_id,
                                    matchedWorkflowId);
                            }
                            pending.SetJobId(jobUpdate.job_id);
                            _sessions[jobUpdate.job_id] = pending;
                        }
                    }

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
    }

    private ExecutionSession? GetOnlyBoundSession()
    {
        var sessions = _sessions.Values.Take(2).ToArray();
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
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _webSocketClient.MessageReceived -= OnMessageReceived;
            _webSocketClient.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _webSocketClient.Dispose();

            foreach (var session in _sessions.Values)
            {
                session.Dispose();
            }
            _sessions.Clear();
            foreach (var pending in _pendingSessions.Drain())
                pending.Dispose();
            _workflowIdsByName.Clear();
        }
    }
}
