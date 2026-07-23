using Nodetool.SDK.Types;
using Nodetool.SDK.VL.Utilities;
using Nodetool.Types.Core;

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
}
