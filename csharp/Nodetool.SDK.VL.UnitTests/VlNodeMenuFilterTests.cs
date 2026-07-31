using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Utilities;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public class VlNodeMenuFilterTests
{
    [Theory]
    [InlineData("nodetool.input")]
    [InlineData("nodetool.workflows.subgraph")]
    [InlineData("nodetool.constant")]
    [InlineData("nodetool.control")]
    [InlineData("nodetool.list")]
    public void CuratedCatalog_ExcludesInfrastructureAndNativeDuplicates(
        string nodeNamespace)
    {
        var metadata = Metadata(nodeNamespace);

        Assert.False(VlNodeMenuFilter.ShouldPublish(
            metadata,
            showAllNodes: false,
            out var exclusion));
        Assert.NotEqual(VlNodeMenuExclusion.None, exclusion);
    }

    [Fact]
    public void CuratedCatalog_UsesNamespaceSegmentBoundaries()
    {
        var metadata = Metadata("nodetool.input_tools");

        Assert.True(VlNodeMenuFilter.ShouldPublish(
            metadata,
            showAllNodes: false,
            out var exclusion));
        Assert.Equal(VlNodeMenuExclusion.None, exclusion);
    }

    [Fact]
    public void CuratedCatalog_ExcludesHiddenAndDeprecatedNodes()
    {
        var hidden = Metadata("nodetool.image");
        hidden.Hidden = true;
        var deprecated = Metadata("nodetool.image");
        deprecated.Deprecated = true;

        Assert.False(VlNodeMenuFilter.ShouldPublish(
            hidden,
            showAllNodes: false,
            out var hiddenReason));
        Assert.Equal(VlNodeMenuExclusion.Hidden, hiddenReason);
        Assert.False(VlNodeMenuFilter.ShouldPublish(
            deprecated,
            showAllNodes: false,
            out var deprecatedReason));
        Assert.Equal(VlNodeMenuExclusion.Deprecated, deprecatedReason);
    }

    [Fact]
    public void CuratedCatalog_KeepsUsefulExecutionNodes()
    {
        var metadata = Metadata("fal.image_to_image");

        Assert.True(VlNodeMenuFilter.ShouldPublish(
            metadata,
            showAllNodes: false,
            out var exclusion));
        Assert.Equal(VlNodeMenuExclusion.None, exclusion);
    }

    [Fact]
    public void ShowAllNodes_RestoresExcludedNodes()
    {
        var metadata = Metadata("nodetool.constant");
        metadata.Deprecated = true;

        Assert.True(VlNodeMenuFilter.ShouldPublish(
            metadata,
            showAllNodes: true,
            out var exclusion));
        Assert.Equal(VlNodeMenuExclusion.None, exclusion);
    }

    private static NodeMetadataResponse Metadata(string nodeNamespace)
        => new()
        {
            Namespace = nodeNamespace,
            NodeType = $"{nodeNamespace}.Example"
        };
}
