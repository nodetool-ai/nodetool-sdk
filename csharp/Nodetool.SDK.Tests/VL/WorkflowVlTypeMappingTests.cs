using Nodetool.SDK.Types;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Values;
using Nodetool.SDK.VL.Nodes;
using Nodetool.SDK.VL.Utilities;
using Nodetool.Types.Core;
using System.Text;

namespace Nodetool.SDK.Tests.VL;

public class WorkflowVlTypeMappingTests
{
    [Fact]
    public void StructuredTypeName_ResolvesGeneratedClrType()
    {
        var metadata = new TypeMetadata
        {
            Type = "object",
            TypeName = "CalendarEvent"
        };

        var (type, defaultValue) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(CalendarEvent), type);
        Assert.Null(defaultValue);
        Assert.False(WorkflowVlTypeMapping.UsesObjectFallback(metadata));
    }

    [Fact]
    public void StructuredDiscriminator_ResolvesGeneratedClrType()
    {
        var metadata = new TypeMetadata { Type = "calendar_event" };

        var (type, _) = WorkflowVlTypeMapping.GetTypeAndDefault(metadata);

        Assert.Equal(typeof(CalendarEvent), type);
    }

    [Fact]
    public void StructuredOutputMap_ConvertsToGeneratedClrType()
    {
        var raw = new Dictionary<string, object?>
        {
            ["id"] = "call-1",
            ["error"] = null,
            ["result"] = "ok",
            ["type"] = "tool_result"
        };

        var converted = VlValueConversion.ConvertOrFallback(raw, typeof(ToolResultEvent), null);
        var result = Assert.IsType<ToolResultEvent>(converted);

        Assert.Equal("call-1", result.id);
        Assert.Equal("tool_result", result.type?.ToString());
    }

    [Fact]
    public void ChunkStream_AppliesAppendReplaceAndEmptyDoneSemantics()
    {
        var buffers = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);

        Assert.True(WorkflowNodeBase.TryAccumulateChunk(
            buffers, "text", Chunk("hel", "append"), out var first));
        Assert.Equal("hel", first);

        Assert.True(WorkflowNodeBase.TryAccumulateChunk(
            buffers, "text", Chunk("lo", "append"), out var appended));
        Assert.Equal("hello", appended);

        Assert.True(WorkflowNodeBase.TryAccumulateChunk(
            buffers, "text", Chunk("world", "replace"), out var replaced));
        Assert.Equal("world", replaced);

        Assert.True(WorkflowNodeBase.TryAccumulateChunk(
            buffers, "text", Chunk("", "append", done: true), out var completed));
        Assert.Equal("world", completed);
    }

    private static ExecutionOutputUpdate Chunk(string content, string disposition, bool done = false)
        => new(
            NodeId: "output-node",
            NodeName: "text",
            OutputName: "text",
            OutputType: "chunk",
            Value: NodeToolValue.From(new Dictionary<string, object>
            {
                ["type"] = "chunk",
                ["content"] = content
            }),
            Metadata: new Dictionary<string, NodeToolValue>(),
            ReceivedAt: DateTimeOffset.UtcNow,
            Disposition: disposition,
            Done: done);
}
