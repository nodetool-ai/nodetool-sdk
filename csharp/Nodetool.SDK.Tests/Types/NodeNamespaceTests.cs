using Nodetool.SDK.Types;
using Xunit;

namespace Nodetool.SDK.Tests.Types;

public class NodeNamespaceTests
{
    [Theory]
    [InlineData("fal.image_to_image", "fal.image_to_image.Flux", "fal.image_to_image")]
    [InlineData("lib.image.effects", "lib.image.effects.Blur", "lib.image.effects")]
    [InlineData("huggingface", "huggingface.Whisper", "huggingface")]
    public void Resolve_PreservesCompleteDeclaredNamespace(
        string declaredNamespace,
        string nodeType,
        string expected)
    {
        Assert.Equal(expected, NodeNamespace.Resolve(declaredNamespace, nodeType));
    }

    [Fact]
    public void Resolve_FallsBackToCompleteNodeTypeNamespace()
    {
        Assert.Equal(
            "lib.image.effects",
            NodeNamespace.Resolve(null, "lib.image.effects.Blur"));
    }

    [Fact]
    public void GetSegments_IncludesHighestAndNestedNamespaces()
    {
        Assert.Equal(
            ["lib", "image", "effects"],
            NodeNamespace.GetSegments("lib.image.effects"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "Blur")]
    [InlineData("  ", "  ")]
    public void Resolve_WithoutNamespace_ReturnsEmpty(
        string? declaredNamespace,
        string? nodeType)
    {
        Assert.Equal(string.Empty, NodeNamespace.Resolve(declaredNamespace, nodeType));
    }
}
