using Nodetool.SDK.Types;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Implementation of an execution session that tracks job progress and results.
/// </summary>
public class ExecutionSession : IExecutionSession
{
    private readonly string _jobId;
    private readonly TaskCompletionSource<bool> _completionSource;
    private readonly Dictionary<string, object?> _outputs;
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Creates a new execution session.
    /// </summary>
    /// <param name="jobId">The job identifier for this session.</param>
    public ExecutionSession(string jobId)
    {
        _jobId = jobId;
        _completionSource = new TaskCompletionSource<bool>();
        _outputs = new Dictionary<string, object?>();
        CurrentStatus = "pending";
    }

    /// <inheritdoc/>
    public string JobId => _jobId;

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
    public event Action<string, object?>? OutputReceived;

    /// <inheritdoc/>
    public event Action<NodeUpdate>? NodeUpdated;

    /// <inheritdoc/>
    public event Action<bool, string?>? Completed;

    /// <summary>
    /// Cancel action delegate - set by the execution client.
    /// </summary>
    internal Func<string, CancellationToken, Task>? CancelAction { get; set; }

    /// <inheritdoc/>
    public T? GetOutput<T>(string name)
    {
        lock (_lock)
        {
            if (_outputs.TryGetValue(name, out var value))
            {
                if (value is T typedValue)
                    return typedValue;

                try
                {
                    return (T?)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return default;
                }
            }
            return default;
        }
    }

    /// <inheritdoc/>
    public Dictionary<string, object?> GetAllOutputs()
    {
        lock (_lock)
        {
            return new Dictionary<string, object?>(_outputs);
        }
    }

    /// <inheritdoc/>
    public async Task CancelAsync()
    {
        if (CancelAction != null)
        {
            await CancelAction(_jobId, CancellationToken.None);
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
        if (update.job_id != _jobId)
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
                            _outputs[kvp.Key] = kvp.Value;
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
        lock (_lock)
        {
            _outputs[update.output_name] = update.value;
        }
        OutputReceived?.Invoke(update.output_name, update.value);
    }

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
