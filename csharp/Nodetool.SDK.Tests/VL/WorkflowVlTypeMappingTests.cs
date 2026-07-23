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

    [Theory]
    [InlineData(3, typeof(int), 3)]
    [InlineData(0.5, typeof(float), 0.5f)]
    [InlineData(true, typeof(bool), true)]
    public void TerminalSingletonList_UnwrapsForScalarPins(
        object terminalValue,
        Type expectedType,
        object expected)
    {
        var value = NodeToolValue.From(new[] { terminalValue });

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(value, expectedType);

        Assert.Equal(expected, converted);
    }

    [Fact]
    public void TerminalList_RemainsAnArrayForSpreadPins()
    {
        var value = NodeToolValue.From(new object[] { "alpha", "beta" });

        var converted = WorkflowNodeBase.ConvertNodeToolValueToExpectedType(value, typeof(string[]));

        Assert.Equal(new[] { "alpha", "beta" }, Assert.IsType<string[]>(converted));
    }

    [Fact]
    public void ImagePayload_AcceptsImageRefBase64Data()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "ImageRef",
            ["uri"] = "",
            ["data"] = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        });

        Assert.True(WorkflowNodeBase.TryExtractImageBytes(value, out var bytes));
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, bytes);
    }

    [Fact]
    public void ImagePayload_AcceptsDataUriInSingletonTerminalList()
    {
        var value = NodeToolValue.From(new object[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "image",
                ["uri"] = "data:image/png;base64,iVBORw0KGgo=",
                ["data"] = null
            }
        });

        Assert.True(WorkflowNodeBase.TryExtractImageBytes(value, out var bytes));
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes);
    }

    [Fact]
    public void ImagePayload_ExposesRelativeStorageUri()
    {
        var value = NodeToolValue.From(new object[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "ImageRef",
                ["uri"] = "/api/storage/roundtrip.png",
                ["data"] = null
            }
        });

        Assert.True(WorkflowNodeBase.TryExtractImageUri(value, out var uri));
        Assert.Equal("/api/storage/roundtrip.png", uri);
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
