using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Types;
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
    private readonly ConcurrentDictionary<string, ExecutionSession> _pendingByWorkflowId;
    private readonly Uri _serverUri;
    private readonly string? _apiKey;
    private bool _disposed;

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
        _pendingByWorkflowId = new ConcurrentDictionary<string, ExecutionSession>();

        // Create WebSocket client
        _webSocketClient = new MessagePackWebSocketClient(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MessagePackWebSocketClient>.Instance);

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

        if (_options.ApiBaseUrl == null)
        {
            throw new InvalidOperationException(
                "ExecuteWorkflowByNameAsync requires NodeToolClientOptions.ApiBaseUrl to be set (HTTP discovery).");
        }

        using var api = new NodetoolClient();
        var apiKey = _apiKey ?? _options.AuthToken;
        api.Configure(_options.ApiBaseUrl.ToString().TrimEnd('/'), apiKey: apiKey);

        var workflows = await api.GetWorkflowsAsync(cancellationToken);
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
            // Avoid silently picking a random one when names collide.
            var ids = string.Join(", ", matches.Select(w => $"{w.Id} ({w.Name})"));
            throw new InvalidOperationException(
                $"Multiple workflows named '{workflowName}' found: {ids}. Use ExecuteWorkflowAsync(workflowId, ...) to disambiguate.");
        }

        return await ExecuteWorkflowAsync(matches[0].Id, inputs, cancellationToken);
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
        _pendingByWorkflowId[workflowId] = session;
        return session;
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
                        ExecutionSession? pending = null;

                        if (jobUpdate.workflow_id != null &&
                            _pendingByWorkflowId.TryRemove(jobUpdate.workflow_id, out pending))
                        {
                            // bound by workflow id
                        }
                        else if (_pendingByWorkflowId.Count == 1)
                        {
                            // best-effort bind when the server doesn't echo workflow_id but we only have one pending
                            var only = _pendingByWorkflowId.FirstOrDefault();
                            if (!string.IsNullOrEmpty(only.Key) && _pendingByWorkflowId.TryRemove(only.Key, out pending))
                            {
                                _logger.LogDebug("Binding job_id {JobId} to the only pending session (workflow_id={WorkflowId})", jobUpdate.job_id, only.Key);
                            }
                        }

                        if (pending != null)
                        {
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
                        // Fallback: broadcast when the server doesn't include job_id.
                        foreach (var session in _sessions.Values)
                        {
                            session.ProcessNodeUpdate(nodeUpdate);
                        }
                    }
                    break;

                case NodeProgress nodeProgress:
                    foreach (var session in _sessions.Values)
                    {
                        session.ProcessNodeProgress(nodeProgress);
                    }
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
                        // Fallback: route to all sessions (v0.1 safety)
                        foreach (var session in _sessions.Values)
                        {
                            session.ProcessOutputUpdate(outputUpdate);
                        }
                    }
                    break;

                case PreviewUpdate previewUpdate:
                    if (previewUpdate.job_id != null && _sessions.TryGetValue(previewUpdate.job_id, out var sessionPreview))
                    {
                        sessionPreview.ProcessPreviewUpdate(previewUpdate);
                    }
                    else
                    {
                        foreach (var session in _sessions.Values)
                        {
                            session.ProcessPreviewUpdate(previewUpdate);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
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
            _pendingByWorkflowId.Clear();
        }
    }
}
