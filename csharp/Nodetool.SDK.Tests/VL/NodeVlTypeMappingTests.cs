using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.Tests.VL;

public class NodeVlTypeMappingTests
{
    [Fact]
    public void ListElementType_IsMappedRecursively()
    {
        var metadata = new NodeTypeDefinition
        {
            Type = "list",
            TypeArgs = [new NodeTypeDefinition { Type = "int" }]
        };

        var (type, defaultValue) = VlTypeMapping.MapNodeType(metadata);

        Assert.Equal(typeof(int[]), type);
        Assert.Empty(Assert.IsType<int[]>(defaultValue));
    }

    [Fact]
    public void BinaryType_RemainsBinary()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition { Type = "bytes" });

        Assert.Equal(typeof(byte[]), type);
        Assert.Empty(Assert.IsType<byte[]>(defaultValue));
    }

    [Fact]
    public void UnknownStructuredType_UsesObjectFallback()
    {
        var (type, defaultValue) = VlTypeMapping.MapNodeType(
            new NodeTypeDefinition { Type = "dataframe" });

        Assert.Equal(typeof(object), type);
        Assert.Null(defaultValue);
    }
}
