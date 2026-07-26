using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Tests.Assets;

public class MediaInputPreparerTests
{
    [Fact]
    public async Task SmallLocalFileIsInlinedWithFileUri()
    {
        var path = CreateTemporaryFile(".wav", [1, 2, 3, 4]);
        try
        {
            var preparer = new MediaInputPreparer(inlineLimitBytes: 8);

            var result = Assert.IsType<Dictionary<string, object?>>(
                await preparer.PrepareAsync("audio", "audio", path));

            Assert.Equal("audio", result["type"]);
            Assert.Equal(new Uri(path).AbsoluteUri, result["uri"]);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, result["data"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LargeLocalFileUsesInjectedAssetManager()
    {
        var path = CreateTemporaryFile(".mp4", [1, 2, 3, 4]);
        try
        {
            using var assets = new RecordingAssetManager();
            var preparer = new MediaInputPreparer(
                assets,
                inlineLimitBytes: 2);

            var result = Assert.IsType<Dictionary<string, object>>(
                await preparer.PrepareAsync("clip", "video", path));

            Assert.Equal(path, assets.UploadedPath);
            Assert.Equal("video/mp4", assets.ContentType);
            Assert.Equal("video", result["type"]);
            Assert.Equal("asset-1", result["asset_id"]);
            Assert.Null(result["data"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LargeBytesUseMemoryUploadAndMediaDefaults()
    {
        using var assets = new RecordingAssetManager();
        var preparer = new MediaInputPreparer(
            assets,
            inlineLimitBytes: 2);

        var result = Assert.IsType<Dictionary<string, object>>(
            await preparer.PrepareAsync(
                "image",
                "image",
                new byte[] { 1, 2, 3 }));

        Assert.Equal(new byte[] { 1, 2, 3 }, assets.UploadedBytes);
        Assert.EndsWith(".png", assets.FileName);
        Assert.Equal("image/png", assets.ContentType);
        Assert.Equal("image", result["type"]);
        Assert.Null(result["data"]);
    }

    [Fact]
    public async Task ByteSignatureSelectsUploadExtensionAndContentType()
    {
        using var assets = new RecordingAssetManager();
        var preparer = new MediaInputPreparer(
            assets,
            inlineLimitBytes: 2);

        await preparer.PrepareAsync(
            "photo",
            "image",
            new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });

        Assert.EndsWith(".jpg", assets.FileName);
        Assert.Equal("image/jpeg", assets.ContentType);
    }

    [Theory]
    [InlineData(".glb", "model/gltf-binary")]
    [InlineData(".gltf", "model/gltf+json")]
    [InlineData(".ttf", "font/ttf")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task LocalFileExtensionSelectsSpecializedContentType(
        string extension,
        string expectedContentType)
    {
        var path = CreateTemporaryFile(extension, [1, 2, 3]);
        try
        {
            using var assets = new RecordingAssetManager();
            var preparer = new MediaInputPreparer(
                assets,
                inlineLimitBytes: 1);

            await preparer.PrepareAsync(
                "file",
                "asset",
                path);

            Assert.Equal(expectedContentType, assets.ContentType);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExistingAssetReferencePreservesMetadataAndTypeOverride()
    {
        var asset = new AudioRef
        {
            Uri = "asset://audio-1",
            AssetId = "audio-1",
            Duration = 1.25f,
            Metadata = new Dictionary<string, object?>
            {
                ["sample_rate"] = 48000
            }
        };
        var preparer = new MediaInputPreparer();

        var result = Assert.IsType<Dictionary<string, object>>(
            await preparer.PrepareAsync("asset", "asset_ref", asset));

        Assert.Equal("asset", result["type"]);
        Assert.Equal("audio-1", result["asset_id"]);
        Assert.Equal(1.25f, result["duration"]);
        Assert.Same(asset.Metadata, result["metadata"]);
    }

    [Fact]
    public async Task LargeValueWithoutAssetManagerHasStableError()
    {
        var preparer = new MediaInputPreparer(inlineLimitBytes: 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync(
                "document",
                "document",
                new byte[] { 1, 2 }));

        Assert.Contains("no asset manager", exception.Message);
        Assert.Contains("document", exception.Message);
    }

    [Fact]
    public async Task EmptyValueIsRejected()
    {
        var preparer = new MediaInputPreparer();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => preparer.PrepareAsync("audio", "audio", null));

        Assert.Contains("audio input 'audio' is empty", exception.Message);
    }

    private static string CreateTemporaryFile(
        string extension,
        byte[] bytes)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"nodetool-media-{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private sealed class RecordingAssetManager : IAssetManager
    {
        public string CacheDirectory => "";
        public string? UploadedPath { get; private set; }
        public string? FileName { get; private set; }
        public string? ContentType { get; private set; }
        public byte[]? UploadedBytes { get; private set; }

        public Task<AssetRef> UploadAssetAsync(
            string localPath,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            UploadedPath = localPath;
            ContentType = contentType;
            return Task.FromResult<AssetRef>(UploadedAsset());
        }

        public Task<AssetRef> UploadAssetAsync(
            string fileName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AssetRef> UploadAssetAsync(
            string fileName,
            ReadOnlyMemory<byte> content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            FileName = fileName;
            ContentType = contentType;
            UploadedBytes = content.ToArray();
            return Task.FromResult<AssetRef>(UploadedAsset());
        }

        public Task<string> DownloadAssetAsync(
            AssetRef asset,
            string? localPath = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> DownloadAssetAsync(
            string uri,
            string? localPath = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public string? GetCachedPath(AssetRef asset) => null;
        public string? GetCachedPath(string uri) => null;
        public void ClearCache() { }
        public long GetCacheSize() => 0;
        public void Dispose() { }

        private static GenericAssetRef UploadedAsset()
            => new()
            {
                AssetId = "asset-1",
                Uri = "/api/assets/asset-1/download"
            };
    }
}
