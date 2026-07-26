using Nodetool.SDK.Values;

namespace Nodetool.SDK.Execution;

public enum WorkflowExecutionState
{
    Idle,
    Starting,
    Running,
    Cancelling,
    Completed,
    Failed,
    Cancelled,
    TimedOut,
    Disposed
}

public sealed record WorkflowOutputState(
    string PublicName,
    NodeToolValue Value,
    bool IsStreaming,
    bool Done,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Immutable controller state. Hosts may marshal snapshots to their own
/// frame/main thread without sharing mutable controller collections.
/// </summary>
public sealed record WorkflowExecutionSnapshot(
    WorkflowExecutionState State,
    string WorkflowId,
    string? JobId,
    float Progress,
    IReadOnlyDictionary<string, WorkflowOutputState> Outputs,
    string? Error,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt)
{
    public bool IsTerminal => State is
        WorkflowExecutionState.Completed or
        WorkflowExecutionState.Failed or
        WorkflowExecutionState.Cancelled or
        WorkflowExecutionState.TimedOut or
        WorkflowExecutionState.Disposed;

    public static WorkflowExecutionSnapshot Idle { get; } = new(
        WorkflowExecutionState.Idle,
        "",
        null,
        0,
        new Dictionary<string, WorkflowOutputState>(StringComparer.Ordinal),
        null,
        null,
        null);
}
