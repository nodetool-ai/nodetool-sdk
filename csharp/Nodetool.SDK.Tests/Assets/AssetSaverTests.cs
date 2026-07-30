using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;
using Xunit;

namespace Nodetool.SDK.Tests.Assets;

public sealed class AssetSaverTests
{
    [Fact]
    public async Task SaveAsync_CopiesToRequestedFileAtomically()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "source.png");
            var destinationPath = Path.Combine(
                directory,
                "export",
                "result.png");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);
            var saver = new AssetSaver(new StubMaterializer(sourcePath));

            var result = await saver.SaveAsync(
                new ImageRef { Uri = "asset://source.png" },
                destinationPath);

            Assert.Equal(
                Path.GetFullPath(destinationPath),
                result.Path);
            Assert.Equal("image/png", result.ContentType);
            Assert.Equal(
                new byte[] { 1, 2, 3, 4 },
                await File.ReadAllBytesAsync(destinationPath));
            Assert.Empty(
                Directory.EnumerateFiles(
                    Path.GetDirectoryName(destinationPath)!,
                    "*.tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_AppendsMaterializedExtensionWhenMissing()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "cached.wav");
            await File.WriteAllBytesAsync(sourcePath, [82, 73, 70, 70]);
            var saver = new AssetSaver(new StubMaterializer(
                sourcePath,
                "audio/wav"));

            var result = await saver.SaveAsync(
                new AudioRef(),
                Path.Combine(directory, "recording"));

            Assert.EndsWith(
                "recording.wav",
                result.Path,
                StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(result.Path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_UsesAssetNameForDirectoryDestination()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "cached.bin");
            var destinationDirectory = Path.Combine(directory, "export");
            Directory.CreateDirectory(destinationDirectory);
            await File.WriteAllBytesAsync(sourcePath, [9, 8, 7]);
            var saver = new AssetSaver(new StubMaterializer(sourcePath));
            var asset = new GenericAssetRef
            {
                Metadata = new Dictionary<string, object?>
                {
                    ["name"] = "chosen.glb"
                }
            };

            var result = await saver.SaveAsync(
                asset,
                destinationDirectory);

            Assert.Equal(
                Path.Combine(destinationDirectory, "chosen.glb"),
                result.Path);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_RequiresExplicitOverwrite()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var sourcePath = Path.Combine(directory, "source.txt");
            var destinationPath = Path.Combine(directory, "destination.txt");
            await File.WriteAllTextAsync(sourcePath, "new");
            await File.WriteAllTextAsync(destinationPath, "old");
            var saver = new AssetSaver(new StubMaterializer(
                sourcePath,
                "text/plain"));

            await Assert.ThrowsAsync<IOException>(() =>
                saver.SaveAsync(new TextRef(), destinationPath));
            Assert.Equal(
                "old",
                await File.ReadAllTextAsync(destinationPath));

            await saver.SaveAsync(
                new TextRef(),
                destinationPath,
                overwrite: true);
            Assert.Equal(
                "new",
                await File.ReadAllTextAsync(destinationPath));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("image.webp", "image/webp")]
    [InlineData("sound.flac", "audio/flac")]
    [InlineData("mesh.glb", "model/gltf-binary")]
    [InlineData("unknown.custom", "application/octet-stream")]
    public void AssetContentType_InfersCommonAssetTypes(
        string path,
        string expected)
        => Assert.Equal(expected, AssetContentType.FromPath(path));

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nodetool-sdk-saver-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class StubMaterializer(
        string path,
        string contentType = "image/png")
        : IAssetMaterializer
    {
        public Task<AssetMaterializationResult> MaterializeAsync(
            AssetRef asset,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new AssetMaterializationResult(
                path,
                contentType,
                "asset://source",
                FromCache: false));
        }
    }
}
