using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.VL.Services;

namespace Nodetool.SDK.Tests.VL;

public class AssetFileMaterializerTests
{
    [Fact]
    public async Task InlineAudioBytes_AreWrittenAndReusedFromCache()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var asset = new AudioRef
            {
                Data = new byte[] { 82, 73, 70, 70 },
                Metadata = new Dictionary<string, object?>
                {
                    ["content_type"] = "audio/wav"
                }
            };

            var first = await AssetFileMaterializer.MaterializeAsync(
                asset,
                forceRefresh: false,
                CancellationToken.None,
                cacheDirectory);
            var second = await AssetFileMaterializer.MaterializeAsync(
                asset,
                forceRefresh: false,
                CancellationToken.None,
                cacheDirectory);

            Assert.EndsWith(".wav", first.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new byte[] { 82, 73, 70, 70 }, await File.ReadAllBytesAsync(first.Path));
            Assert.False(first.FromCache);
            Assert.True(second.FromCache);
            Assert.Equal(first.Path, second.Path);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExistingLocalDocument_IsPassedThroughWithoutCopy()
    {
        var directory = CreateTemporaryDirectory();
        var sourcePath = Path.Combine(directory, "fixture.pdf");
        await File.WriteAllBytesAsync(sourcePath, new byte[] { 37, 80, 68, 70 });
        try
        {
            var result = await AssetFileMaterializer.MaterializeAsync(
                new DocumentRef { Uri = sourcePath },
                forceRefresh: false,
                CancellationToken.None,
                Path.Combine(directory, "cache"));

            Assert.Equal(Path.GetFullPath(sourcePath), result.Path);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.False(result.FromCache);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TextData_IsWrittenAsUtf8InsteadOfTreatedAsBase64()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var result = await AssetFileMaterializer.MaterializeAsync(
                new TextRef { Data = "test" },
                forceRefresh: false,
                CancellationToken.None,
                cacheDirectory);

            Assert.EndsWith(".txt", result.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("test", await File.ReadAllTextAsync(result.Path));
            Assert.Equal("text/plain", result.ContentType);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DataUriInUri_IsMaterializedWithoutNetworkAccess()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var result = await AssetFileMaterializer.MaterializeAsync(
                new DocumentRef
                {
                    Uri = "data:application/pdf;base64,JVBERg=="
                },
                forceRefresh: false,
                CancellationToken.None,
                cacheDirectory);

            Assert.EndsWith(".pdf", result.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("%PDF", await File.ReadAllTextAsync(result.Path));
            Assert.Equal("application/pdf", result.ContentType);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public void StoredAssetUri_MapsToCurrentStorageEndpoint()
    {
        var uri = AssetFileMaterializer.ResolveStoredAssetUri(
            "asset://clip.webm");

        Assert.NotNull(uri);
        Assert.Equal("/api/storage/clip.webm", uri.AbsolutePath);
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nodetool-sdk-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
