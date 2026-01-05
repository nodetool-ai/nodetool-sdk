using Nodetool.SDK.Values;

namespace Nodetool.SDK.Execution;

public sealed record ExecutionOutputUpdate(
    string NodeId,
    string NodeName,
    string OutputName,
    string OutputType,
    NodeToolValue Value,
    IReadOnlyDictionary<string, NodeToolValue> Metadata,
    DateTimeOffset ReceivedAt
);

public sealed record ExecutionPreviewUpdate(
    string NodeId,
    NodeToolValue Value,
    DateTimeOffset ReceivedAt
);


