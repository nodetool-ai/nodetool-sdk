using Nodetool.SDK.Types;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Implementation of an execution session that tracks job progress and results.
/// </summary>
public class ExecutionSession : IExecutionSession
{
    private string _jobId;
    private readonly string? _workflowId;
    private readonly TaskCompletionSource<bool> _completionSource;
    private readonly Dictionary<string, NodeToolValue> _latestOutputs;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new execution session.
    /// </summary>
    /// <param name="jobId">The job identifier for this session.</param>
    public ExecutionSession(string jobId, string? workflowId = null)
    {
        _jobId = jobId;
        _workflowId = workflowId;
        _completionSource = new TaskCompletionSource<bool>();
        _latestOutputs = new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
        CurrentStatus = "pending";
    }

    /// <inheritdoc/>
    public string JobId => _jobId;

    /// <summary>
    /// Workflow id this session was started for (used before server assigns job_id).
    /// </summary>
    public string? WorkflowId => _workflowId;

    internal void SetJobId(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return;
        _jobId = jobId;
    }

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public bool IsCompleted { get; private set; }

    /// <inheritdoc/>
    public string? ErrorMessage { get; private set; }

    /// <inheritdoc/>
    public float ProgressPercent { get; private set; }

    /// <inheritdoc/>
    public string CurrentStatus { get; private set; }

    /// <inheritdoc/>
    public event Action<float>? ProgressChanged;

    /// <inheritdoc/>
    public event Action<ExecutionOutputUpdate>? OutputReceived;

    public event Action<ExecutionPreviewUpdate>? PreviewReceived;

    /// <inheritdoc/>
    public event Action<NodeUpdate>? NodeUpdated;

    /// <inheritdoc/>
    public event Action<bool, string?>? Completed;

    /// <summary>
    /// Cancel action delegate - set by the execution client.
    /// </summary>
    internal Func<string, string?, CancellationToken, Task>? CancelAction { get; set; }

    /// <inheritdoc/>
    public NodeToolValue? GetLatestOutput(string nodeId, string outputName)
    {
        lock (_lock)
        {
            var key = OutputKey(nodeId, outputName);
            return _latestOutputs.TryGetValue(key, out var value) ? value : null;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, NodeToolValue> GetLatestOutputs()
    {
        lock (_lock)
        {
            return new Dictionary<string, NodeToolValue>(_latestOutputs);
        }
    }

    /// <inheritdoc/>
    public async Task CancelAsync()
    {
        if (CancelAction != null)
        {
            if (string.IsNullOrWhiteSpace(_jobId))
                return;
            await CancelAction(_jobId, _workflowId, CancellationToken.None);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted)
            return ErrorMessage == null;

        try
        {
            using var registration = cancellationToken.Register(() =>
                _completionSource.TrySetCanceled());
            return await _completionSource.Task;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Process a job update message from the server.
    /// </summary>
    internal void ProcessJobUpdate(JobUpdate update)
    {
        // If server assigned job_id after we started, accept the first matching update and lock onto it.
        if (update.job_id != null && string.IsNullOrWhiteSpace(_jobId))
        {
            _jobId = update.job_id;
        }

        if (update.job_id != null && !string.IsNullOrWhiteSpace(_jobId) && update.job_id != _jobId)
            return;

        lock (_lock)
        {
            CurrentStatus = update.status;

            switch (update.status)
            {
                case "running":
                    IsRunning = true;
                    break;

                case "completed":
                    IsRunning = false;
                    IsCompleted = true;
                    if (update.result != null)
                    {
                        foreach (var kvp in update.result)
                        {
                            // job_update.result is free-form; store under a synthetic key to avoid collisions
                            _latestOutputs[$"job_result:{kvp.Key}"] = NodeToolValue.From(kvp.Value);
                        }
                    }
                    _completionSource.TrySetResult(true);
                    Completed?.Invoke(true, null);
                    break;

                case "failed":
                    IsRunning = false;
                    IsCompleted = true;
                    ErrorMessage = update.error ?? update.message ?? "Unknown error";
                    _completionSource.TrySetResult(false);
                    Completed?.Invoke(false, ErrorMessage);
                    break;

                case "cancelled":
                    IsRunning = false;
                    IsCompleted = true;
                    ErrorMessage = "Job cancelled";
                    _completionSource.TrySetResult(false);
                    Completed?.Invoke(false, ErrorMessage);
                    break;

                case "suspended":
                    IsRunning = false;
                    // Not completed yet - can be resumed
                    break;
            }
        }
    }

    /// <summary>
    /// Process a node update message from the server.
    /// </summary>
    internal void ProcessNodeUpdate(NodeUpdate update)
    {
        NodeUpdated?.Invoke(update);
    }

    /// <summary>
    /// Process a node progress message from the server.
    /// </summary>
    internal void ProcessNodeProgress(NodeProgress progress)
    {
        if (progress.total > 0)
        {
            ProgressPercent = (float)progress.progress / progress.total;
            ProgressChanged?.Invoke(ProgressPercent);
        }
    }

    /// <summary>
    /// Process a progress update message from the server.
    /// </summary>
    internal void ProcessProgressUpdate(ProgressUpdate update)
    {
        if (update.job_id != _jobId)
            return;

        ProgressPercent = (float)update.progress;
        ProgressChanged?.Invoke(ProgressPercent);
    }

    /// <summary>
    /// Process an output update message from the server.
    /// </summary>
    internal void ProcessOutputUpdate(OutputUpdate update)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var value = NodeToolValue.From(update.value);
        var metadata = (update.metadata ?? new Dictionary<string, object>())
            .ToDictionary(kvp => kvp.Key, kvp => NodeToolValue.From(kvp.Value), StringComparer.Ordinal);
        var key = OutputKey(update.node_id, update.output_name);

        lock (_lock)
        {
            _latestOutputs[key] = value;
        }

        OutputReceived?.Invoke(new ExecutionOutputUpdate(
            NodeId: update.node_id,
            NodeName: update.node_name,
            OutputName: update.output_name,
            OutputType: update.output_type,
            Value: value,
            Metadata: metadata,
            ReceivedAt: receivedAt
        ));
    }

    internal void ProcessPreviewUpdate(PreviewUpdate update)
    {
        var receivedAt = DateTimeOffset.UtcNow;
        var value = NodeToolValue.From(update.value);
        PreviewReceived?.Invoke(new ExecutionPreviewUpdate(update.node_id, value, receivedAt));
    }

    private static string OutputKey(string nodeId, string outputName) => $"{nodeId}:{outputName}";

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _completionSource.TrySetCanceled();
        }
    }
}
