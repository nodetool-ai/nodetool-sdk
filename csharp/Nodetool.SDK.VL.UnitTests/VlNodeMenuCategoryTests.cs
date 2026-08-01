using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Utilities;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class VlNodeMenuCategoryTests
{
    [Theory]
    [InlineData("apify.scraping", "apify.scraping.ApifyWebScraper", "Nodetool Nodes.apify.scraping")]
    [InlineData("fal.image_to_image", "fal.image_to_image.Flux", "Nodetool Nodes.fal.image_to_image")]
    [InlineData("lib.image.effects", "lib.image.effects.Blur", "Nodetool Nodes.lib.image.effects")]
    [InlineData("huggingface", "huggingface.Whisper", "Nodetool Nodes.huggingface")]
    public void For_PreservesCompleteNodeToolNamespaceIdentity(
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
            "Nodetool Nodes.nodetool.constant",
            VlNodeMenuCategory.For(metadata));
    }

    [Fact]
    public void For_PreservesCapitalizeTextPatchIdentity()
    {
        var metadata = new NodeMetadataResponse
        {
            Namespace = "nodetool.text",
            NodeType = "nodetool.text.CapitalizeText"
        };

        Assert.Equal(
            "Nodetool Nodes.nodetool.text",
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
