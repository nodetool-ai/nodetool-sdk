using System.Text.Json;

namespace Nodetool.SDK.Workflows;

public sealed record WorkflowTypeDescriptor(
    string Type,
    bool Optional,
    string? TypeName,
    IReadOnlyList<object> Values,
    IReadOnlyList<WorkflowTypeDescriptor> TypeArguments);

public abstract record WorkflowPinDescriptor(
    string NodeId,
    string Name,
    string Description,
    WorkflowTypeDescriptor Type);

public sealed record WorkflowInputDescriptor(
    string NodeId,
    string Name,
    string Description,
    WorkflowTypeDescriptor Type,
    bool Required,
    JsonElement? DefaultValue,
    double? Minimum,
    double? Maximum)
    : WorkflowPinDescriptor(NodeId, Name, Description, Type);

public sealed record WorkflowOutputDescriptor(
    string NodeId,
    string Name,
    string Description,
    WorkflowTypeDescriptor Type,
    bool Stream)
    : WorkflowPinDescriptor(NodeId, Name, Description, Type);

public sealed record WorkflowDiagnosticDescriptor(
    string Severity,
    string Code,
    string Message,
    string? NodeId,
    string? PinName);

public sealed record WorkflowDescriptor(
    string Id,
    string Name,
    string Description,
    string Revision,
    long? RegistryRevision,
    string? RunMode,
    int InterfaceVersion,
    string? InterfaceEtag,
    string InterfaceSource,
    IReadOnlyList<WorkflowInputDescriptor> Inputs,
    IReadOnlyList<WorkflowOutputDescriptor> Outputs,
    IReadOnlyList<WorkflowDiagnosticDescriptor> Diagnostics);

public sealed record WorkflowCatalogSnapshot(
    IReadOnlyList<WorkflowDescriptor> Workflows,
    DateTimeOffset? LastSuccessfulRefreshUtc,
    bool IsStale,
    string? LastError,
    int CacheHitCount,
    int SkippedCount)
{
    public static WorkflowCatalogSnapshot Empty { get; } = new(
        Array.Empty<WorkflowDescriptor>(),
        null,
        false,
        null,
        0,
        0);
}
