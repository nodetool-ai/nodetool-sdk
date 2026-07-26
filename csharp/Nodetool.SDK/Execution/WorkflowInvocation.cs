using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Host-neutral request to execute one workflow.
/// </summary>
public sealed record WorkflowInvocation(
    string WorkflowId,
    IReadOnlyDictionary<string, object?> Inputs,
    TimeSpan? Timeout = null,
    bool RetainOutputs = true,
    WorkflowExecutionOptions? ExecutionOptions = null)
{
    public WorkflowInvocation(string workflowId)
        : this(
            workflowId,
            new Dictionary<string, object?>(StringComparer.Ordinal),
            null,
            true,
            null)
    {
    }
}

/// <summary>
/// Additive server execution preferences. Omitting this object preserves the
/// normal persisted job, full event stream, and automatic asset persistence.
/// </summary>
public sealed record WorkflowExecutionOptions(
    WorkflowPersistence Persistence = WorkflowPersistence.Job,
    WorkflowEventDetail EventDetail = WorkflowEventDetail.Full,
    WorkflowAssetPersistence AssetPersistence = WorkflowAssetPersistence.Auto);

public enum WorkflowPersistence
{
    Job,
    Session
}

public enum WorkflowEventDetail
{
    Full,
    Outputs,
    Terminal
}

public enum WorkflowAssetPersistence
{
    Auto,
    Temporary
}

public static class WorkflowExecutionOptionNegotiator
{
    public static bool IsDefault(WorkflowExecutionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options == new WorkflowExecutionOptions();
    }

    public static void EnsureSupported(
        SdkCapabilitiesResponse capabilities,
        WorkflowExecutionOptions options)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(options);
        if (IsDefault(options))
            return;

        var support = capabilities.ExecutionOptions ??
            throw new NotSupportedException(
                "The server does not advertise SDK execution options.");
        EnsureValue(
            support.Persistence,
            options.Persistence == WorkflowPersistence.Session
                ? "session"
                : "job",
            "persistence");
        EnsureValue(
            support.EventDetail,
            options.EventDetail switch
            {
                WorkflowEventDetail.Outputs => "outputs",
                WorkflowEventDetail.Terminal => "terminal",
                _ => "full"
            },
            "event_detail");
        EnsureValue(
            support.AssetPersistence,
            options.AssetPersistence == WorkflowAssetPersistence.Temporary
                ? "temporary"
                : "auto",
            "asset_persistence");
    }

    private static void EnsureValue(
        IReadOnlyCollection<string> supported,
        string requested,
        string option)
    {
        if (!supported.Contains(requested, StringComparer.Ordinal))
        {
            throw new NotSupportedException(
                $"The server does not support execution option " +
                $"'{option}={requested}'.");
        }
    }
}

/// <summary>
/// Determines how a controller handles a request made while another invocation
/// is active. Queued policies coalesce requests and retain only the latest one.
/// </summary>
public enum WorkflowStartPolicy
{
    Reject,
    QueueLatest,
    CancelAndRestart
}
