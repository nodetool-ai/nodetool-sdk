using Nodetool.SDK.Types;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Identifies which NodeTool wire shape produced a portable stream update.
/// </summary>
public enum ExecutionStreamSource
{
    OutputUpdate,
    StandaloneChunk
}

/// <summary>
/// Host-neutral streamed content from an active execution.
/// </summary>
/// <remarks>
/// NodeTool can carry a chunk as an <c>output_update.value</c> or as a
/// standalone <c>chunk</c> message. This record normalizes both without
/// requiring consumers to inspect protocol dictionaries.
/// </remarks>
public sealed record ExecutionStreamUpdate(
    string JobId,
    string? WorkflowId,
    string? ThreadId,
    string? NodeId,
    string? NodeName,
    string? OutputName,
    string? OutputType,
    string? ContentType,
    NodeToolValue Content,
    IReadOnlyDictionary<string, NodeToolValue> ContentMetadata,
    string Disposition,
    bool Done,
    bool Thinking,
    DateTimeOffset ReceivedAt,
    ExecutionStreamSource Source)
{
    internal static ExecutionStreamUpdate? FromOutputUpdate(
        string jobId,
        OutputUpdate update,
        DateTimeOffset receivedAt)
    {
        var value = NodeToolValue.From(update.value);
        if (value.Kind != NodeToolValueKind.Map ||
            !string.Equals(
                value.TypeDiscriminator,
                "chunk",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var chunk = value.AsMapOrEmpty();
        var metadata = chunk.TryGetValue("content_metadata", out var metadataValue)
            ? metadataValue.AsMapOrEmpty()
            : EmptyMetadata();
        var done = update.done ??
            ReadBoolean(chunk, "done") ??
            false;

        return new ExecutionStreamUpdate(
            JobId: jobId,
            WorkflowId: update.workflow_id ??
                ReadString(chunk, "workflow_id"),
            ThreadId: ReadString(chunk, "thread_id"),
            NodeId: update.node_id,
            NodeName: update.node_name,
            OutputName: update.output_name,
            OutputType: update.output_type,
            ContentType: ReadString(chunk, "content_type"),
            Content: chunk.TryGetValue("content", out var content)
                ? content
                : NodeToolValue.From(null),
            ContentMetadata: metadata,
            Disposition: NormalizeDisposition(update.disposition),
            Done: done,
            Thinking: ReadBoolean(chunk, "thinking") ?? false,
            ReceivedAt: receivedAt,
            Source: ExecutionStreamSource.OutputUpdate);
    }

    internal static ExecutionStreamUpdate FromChunkMessage(
        ChunkMessage message,
        DateTimeOffset receivedAt)
        => new(
            JobId: message.job_id!,
            WorkflowId: message.workflow_id,
            ThreadId: message.thread_id,
            NodeId: message.node_id,
            NodeName: null,
            OutputName: null,
            OutputType: null,
            ContentType: message.content_type,
            Content: NodeToolValue.From(message.content),
            ContentMetadata: (message.content_metadata ??
                new Dictionary<string, object>())
                .ToDictionary(
                    pair => pair.Key,
                    pair => NodeToolValue.From(pair.Value),
                    StringComparer.Ordinal),
            Disposition: "append",
            Done: message.done ?? false,
            Thinking: message.thinking ?? false,
            ReceivedAt: receivedAt,
            Source: ExecutionStreamSource.StandaloneChunk);

    internal static string NormalizeDisposition(string? disposition)
        => string.Equals(
            disposition,
            "replace",
            StringComparison.OrdinalIgnoreCase)
                ? "replace"
                : "append";

    private static string? ReadString(
        IReadOnlyDictionary<string, NodeToolValue> values,
        string key)
        => values.TryGetValue(key, out var value)
            ? value.AsString()
            : null;

    private static bool? ReadBoolean(
        IReadOnlyDictionary<string, NodeToolValue> values,
        string key)
        => values.TryGetValue(key, out var value) &&
           value.TryGetBool(out var result)
            ? result
            : null;

    private static IReadOnlyDictionary<string, NodeToolValue> EmptyMetadata()
        => new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
}
