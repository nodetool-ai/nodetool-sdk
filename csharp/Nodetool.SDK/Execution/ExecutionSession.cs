using Nodetool.SDK.Types;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Implementation of an execution session that tracks job progress and results.
/// </summary>
public class ExecutionSession : IExecutionSession
{
    private readonly string _jobId;
    private readonly string? _workflowId;
    private readonly TaskCompletionSource<bool> _completionSource;
    private readonly Dictionary<string, NodeToolValue> _latestOutputs;
    private readonly object _lock = new();
    private bool _cancellationSent;
    private bool _disposed;

    /// <summary>
    /// Creates a new execution session.
    /// </summary>
    /// <param name="jobId">The job identifier for this session.</param>
    public ExecutionSession(string jobId, string? workflowId = null)
    {
        _jobId = !string.IsNullOrWhiteSpace(jobId)
            ? jobId
            : throw new ArgumentException("Job ID must not be empty.", nameof(jobId));
        _workflowId = workflowId;
        _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _latestOutputs = new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
        CurrentStatus = "pending";
    }

    /// <inheritdoc/>
    public string JobId => _jobId;

    /// <summary>
    /// Workflow id this session was started for (used before server assigns job_id).
    /// </summary>
    public string? WorkflowId => _workflowId;

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

    /// <inheritdoc/>
    public event Action<ExecutionStreamUpdate>? StreamReceived;

    public event Action<ExecutionPreviewUpdate>? PreviewReceived;

    /// <inheritdoc/>
    public event Action<NodeUpdate>? NodeUpdated;

    /// <inheritdoc/>
    public event Action<bool, string?>? Completed;

    /// <summary>
    /// Cancel action delegate - set by the execution client.
    /// </summary>
    internal Func<string, string?, CancellationToken, Task>? CancelAction { get; set; }

    internal Func<StreamInputData, CancellationToken, Task>?
        StreamInputAction { get; set; }

    internal Func<EndInputStreamData, CancellationToken, Task>?
        EndInputStreamAction { get; set; }

    internal Func<UpdateNodePropertiesData, CancellationToken, Task>?
        UpdateNodePropertiesAction { get; set; }

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
    public async Task CancelAsync(CancellationToken cancellationToken = default)
    {
        Func<string, string?, CancellationToken, Task>? cancelAction = null;
        string? jobId = null;
        lock (_lock)
        {
            if (!_cancellationSent &&
                CancelAction != null)
            {
                _cancellationSent = true;
                cancelAction = CancelAction;
                jobId = _jobId;
            }
        }
        if (cancelAction != null && jobId != null)
            await cancelAction(jobId, _workflowId, cancellationToken);
    }

    /// <inheritdoc/>
    public Task StreamInputAsync(
        string inputName,
        object? value,
        string? sourceHandle = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputName);
        var action = GetActiveAction(
            StreamInputAction,
            "Streaming input is unavailable for this execution session.");
        return action(
            new StreamInputData
            {
                job_id = _jobId,
                workflow_id = _workflowId,
                input = inputName,
                handle = NormalizeOptional(sourceHandle),
                value = value
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task EndInputStreamAsync(
        string inputName,
        string? sourceHandle = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputName);
        var action = GetActiveAction(
            EndInputStreamAction,
            "Ending an input stream is unavailable for this execution session.");
        return action(
            new EndInputStreamData
            {
                job_id = _jobId,
                workflow_id = _workflowId,
                input = inputName,
                handle = NormalizeOptional(sourceHandle)
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateNodePropertiesAsync(
        string nodeId,
        IReadOnlyDictionary<string, object?> properties,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentNullException.ThrowIfNull(properties);
        var action = GetActiveAction(
            UpdateNodePropertiesAction,
            "Live property updates are unavailable for this execution session.");
        return action(
            new UpdateNodePropertiesData
            {
                job_id = _jobId,
                workflow_id = _workflowId,
                node_id = nodeId,
                properties = new Dictionary<string, object?>(
                    properties,
                    StringComparer.Ordinal)
            },
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (IsCompleted)
            return ErrorMessage == null;

        return await _completionSource.Task.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Process a job update message from the server.
    /// </summary>
    internal void ProcessJobUpdate(JobUpdate update)
    {
        if (update.job_id != null && update.job_id != _jobId)
            return;

        bool? completionSucceeded = null;
        string? completionError = null;

        lock (_lock)
        {
            if (IsCompleted)
                return;

            CurrentStatus = update.status;

            switch (update.status)
            {
                case "running":
                    IsRunning = true;
                    break;

                case "completed":
                    // The SDK opts into an authoritative terminal snapshot. The
                    // runtime may internally produce an earlier completed event;
                    // do not finish the public session until result.outputs arrives.
                    if (update.result?.ContainsKey("outputs") != true)
                    {
                        IsRunning = true;
                        CurrentStatus = "finalizing";
                        break;
                    }
                    IsRunning = false;
                    IsCompleted = true;
                    if (update.result.TryGetValue("outputs", out var outputs))
                    {
                        foreach (var kvp in NodeToolValue.From(outputs).AsMapOrEmpty())
                        {
                            _latestOutputs[$"job_result:{kvp.Key}"] = kvp.Value;
                        }
                    }
                    _completionSource.TrySetResult(true);
                    completionSucceeded = true;
                    break;

                case "failed":
                    IsRunning = false;
                    IsCompleted = true;
                    ErrorMessage = update.error ?? update.message ?? "Unknown error";
                    _completionSource.TrySetResult(false);
                    completionSucceeded = false;
                    completionError = ErrorMessage;
                    break;

                case "cancelled":
                    IsRunning = false;
                    IsCompleted = true;
                    ErrorMessage = "Job cancelled";
                    _completionSource.TrySetResult(false);
                    completionSucceeded = false;
                    completionError = ErrorMessage;
                    break;

                case "suspended":
                    IsRunning = false;
                    // Not completed yet - can be resumed
                    break;
            }
        }

        if (completionSucceeded.HasValue)
            Completed?.Invoke(completionSucceeded.Value, completionError);
    }

    /// <summary>
    /// Process a node update message from the server.
    /// </summary>
    internal void ProcessNodeUpdate(NodeUpdate update)
    {
        if (!MatchesJob(update.job_id))
            return;
        NodeUpdated?.Invoke(update);
    }

    /// <summary>
    /// Process a node progress message from the server.
    /// </summary>
    internal void ProcessNodeProgress(NodeProgress progress)
    {
        if (!MatchesJob(progress.job_id))
            return;
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
        if (!MatchesJob(update.job_id))
            return;
        var receivedAt = DateTimeOffset.UtcNow;
        var value = NodeToolValue.From(update.value);
        var stream = ExecutionStreamUpdate.FromOutputUpdate(
            _jobId,
            update,
            receivedAt);
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
            ReceivedAt: receivedAt,
            Disposition: stream?.Disposition ??
                ExecutionStreamUpdate.NormalizeDisposition(update.disposition),
            Done: stream?.Done ?? update.done ?? false
        ));
        if (stream != null)
            StreamReceived?.Invoke(stream);
    }

    internal void ProcessStreamChunk(ChunkMessage message)
    {
        if (!MatchesJob(message.job_id) ||
            string.IsNullOrWhiteSpace(message.job_id))
        {
            return;
        }
        StreamReceived?.Invoke(
            ExecutionStreamUpdate.FromChunkMessage(
                message,
                DateTimeOffset.UtcNow));
    }

    internal void ProcessPreviewUpdate(PreviewUpdate update)
    {
        if (!MatchesJob(update.job_id))
            return;
        var receivedAt = DateTimeOffset.UtcNow;
        var value = NodeToolValue.From(update.value);
        PreviewReceived?.Invoke(new ExecutionPreviewUpdate(update.node_id, value, receivedAt));
    }

    private static string OutputKey(string nodeId, string outputName) => $"{nodeId}:{outputName}";

    private bool MatchesJob(string? jobId)
    {
        return string.IsNullOrWhiteSpace(jobId)
            || string.Equals(jobId, _jobId, StringComparison.Ordinal);
    }

    private TAction GetActiveAction<TAction>(
        TAction? action,
        string unavailableMessage)
        where TAction : Delegate
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsCompleted)
            {
                throw new InvalidOperationException(
                    "The execution session has already completed.");
            }
            return action ?? throw new NotSupportedException(unavailableMessage);
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value;

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
