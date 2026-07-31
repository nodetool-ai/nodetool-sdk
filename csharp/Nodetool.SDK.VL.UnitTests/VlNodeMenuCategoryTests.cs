using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Utilities;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class VlNodeMenuCategoryTests
{
    [Theory]
    [InlineData("apify.scraping", "apify.scraping.ApifyWebScraper", "Nodetool Nodes.Apify.Scraping")]
    [InlineData("fal.image_to_image", "fal.image_to_image.Flux", "Nodetool Nodes.Fal.Image_to_image")]
    [InlineData("lib.image.effects", "lib.image.effects.Blur", "Nodetool Nodes.Lib.Image.Effects")]
    [InlineData("huggingface", "huggingface.Whisper", "Nodetool Nodes.Huggingface")]
    public void For_CapitalizesCompleteNodeToolNamespaceHierarchy(
        string declaredNamespace,
        string nodeType,
        string expected)
    {
        var metadata = new NodeMetadataResponse
        {
            Namespace = declaredNamespace,
            NodeType = nodeType
        };

        Assert.Equal(expected, VlNodeMenuCategory.For(metadata));
    }

    [Fact]
    public void For_DerivesNamespaceFromNodeTypeForOlderMetadata()
    {
        var metadata = new NodeMetadataResponse
        {
            NodeType = "nodetool.constant.Float"
        };

        Assert.Equal(
            "Nodetool Nodes.Nodetool.Constant",
            VlNodeMenuCategory.For(metadata));
    }

    [Fact]
    public void For_UsesGeneralWhenNoNamespaceCanBeResolved()
    {
        Assert.Equal(
            "Nodetool Nodes.General",
            VlNodeMenuCategory.For(new NodeMetadataResponse()));
    }
}
