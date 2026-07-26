using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Tests.Assets;

public sealed class NodeToolAssetValueParserTests
{
    [Fact]
    public void ImagePayload_AcceptsBase64Data()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "ImageRef",
            ["uri"] = "",
            ["data"] = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })
        });

        Assert.True(NodeToolAssetValueParser.TryParse(
            value,
            "image",
            out var asset));
        var image = Assert.IsType<ImageRef>(asset);
        Assert.True(NodeToolAssetValueParser.TryGetBytes(image, out var bytes));
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

        Assert.True(NodeToolAssetValueParser.TryParse(
            value,
            "image",
            out var asset));
        var image = Assert.IsType<ImageRef>(asset);
        Assert.Equal("image/png", image.MimeType);
        Assert.True(NodeToolAssetValueParser.TryGetBytes(image, out var bytes));
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes);
    }

    [Fact]
    public void ImagePayload_PreservesStorageUri()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "image_ref",
            ["uri"] = "/api/storage/roundtrip.png",
            ["data"] = null
        });

        Assert.True(NodeToolAssetValueParser.TryParse(
            value,
            "image",
            out var asset));
        Assert.Equal("/api/storage/roundtrip.png", asset.Uri);
    }

    [Fact]
    public void ImagePayload_PreservesIdOnlyAsset()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "image",
            ["uri"] = "",
            ["asset_id"] = "asset-123"
        });

        Assert.True(NodeToolAssetValueParser.TryParse(
            value,
            "image",
            out var asset));
        Assert.Equal("asset-123", asset.AssetId);
        Assert.True(asset.IsSet());
    }

    [Theory]
    [InlineData("asset:asset-123", "asset-123")]
    [InlineData("asset://asset-123", "asset-123")]
    [InlineData("asset:///asset-123", "asset-123")]
    [InlineData("asset://asset-123.png", "asset-123.png")]
    public void AssetReferenceUri_ExtractsCanonicalKey(
        string uri,
        string expected)
    {
        Assert.True(AssetReferenceUri.TryGetAssetKey(uri, out var key));
        Assert.Equal(expected, key);
    }

    [Fact]
    public void MismatchedMediaType_IsRejected()
    {
        var value = NodeToolValue.From(new Dictionary<string, object?>
        {
            ["type"] = "audio",
            ["uri"] = "https://example.test/audio.wav"
        });

        Assert.False(NodeToolAssetValueParser.TryParse(
            value,
            "image",
            out _));
    }
}
