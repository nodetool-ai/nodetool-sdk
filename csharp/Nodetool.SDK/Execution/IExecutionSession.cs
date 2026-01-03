namespace Nodetool.SDK.Execution;

/// <summary>
/// Represents an active execution session for a workflow or node.
/// Provides real-time status updates and output retrieval.
/// </summary>
public interface IExecutionSession : IDisposable
{
    /// <summary>
    /// Unique identifier for this job/session.
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Whether the execution is currently running.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Whether the execution has completed (successfully or with error).
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Error message if execution failed, null otherwise.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Current progress as a percentage (0.0 to 1.0).
    /// </summary>
    float ProgressPercent { get; }

    /// <summary>
    /// Current status string: "pending", "running", "completed", "failed", "cancelled", "suspended".
    /// </summary>
    string CurrentStatus { get; }

    /// <summary>
    /// Get a typed output value by name.
    /// </summary>
    /// <typeparam name="T">Expected output type.</typeparam>
    /// <param name="name">Output name.</param>
    /// <returns>Output value or default if not found.</returns>
    T? GetOutput<T>(string name);

    /// <summary>
    /// Get all outputs as a dictionary.
    /// </summary>
    /// <returns>Dictionary of output name to value.</returns>
    Dictionary<string, object?> GetAllOutputs();

    /// <summary>
    /// Event fired when progress changes.
    /// </summary>
    event Action<float>? ProgressChanged;

    /// <summary>
    /// Event fired when an output value is received.
    /// </summary>
    event Action<string, object?>? OutputReceived;

    /// <summary>
    /// Event fired when a node update is received.
    /// </summary>
    event Action<Types.NodeUpdate>? NodeUpdated;

    /// <summary>
    /// Event fired when execution completes. Parameters: success, errorMessage.
    /// </summary>
    event Action<bool, string?>? Completed;

    /// <summary>
    /// Cancel the running execution.
    /// </summary>
    Task CancelAsync();

    /// <summary>
    /// Wait for the execution to complete.
    /// </summary>
    /// <returns>True if completed successfully, false if failed or cancelled.</returns>
    Task<bool> WaitForCompletionAsync(CancellationToken cancellationToken = default);
}
