using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Result of saving a NodeTool asset to a caller-selected local file.
/// </summary>
public sealed record AssetSaveResult(
    string Path,
    string ContentType,
    string SourceUri);

/// <summary>
/// Materializes and atomically copies NodeTool assets to user-selected files.
/// </summary>
public sealed class AssetSaver(IAssetMaterializer materializer) : IAssetSaver
{
    private readonly IAssetMaterializer _materializer =
        materializer ?? throw new ArgumentNullException(nameof(materializer));

    public async Task<AssetSaveResult> SaveAsync(
        AssetRef asset,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        var source = await _materializer.MaterializeAsync(
            asset,
            cancellationToken: cancellationToken);
        var targetPath = ResolveTargetPath(
            destinationPath,
            source.Path,
            asset);
        var sourcePath = Path.GetFullPath(source.Path);

        if (string.Equals(
            sourcePath,
            targetPath,
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal))
        {
            return new AssetSaveResult(
                targetPath,
                source.ContentType,
                source.SourceUri);
        }

        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        if (!overwrite && File.Exists(targetPath))
        {
            throw new IOException(
                $"Destination file already exists: {targetPath}");
        }

        var temporaryPath =
            $"{targetPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var sourceStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                useAsync: true))
            await using (var targetStream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await sourceStream.CopyToAsync(
                    targetStream,
                    cancellationToken);
            }

            File.Move(temporaryPath, targetPath, overwrite);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }

        return new AssetSaveResult(
            targetPath,
            source.ContentType,
            source.SourceUri);
    }

    private static string ResolveTargetPath(
        string destinationPath,
        string materializedPath,
        AssetRef asset)
    {
        var targetPath = Path.GetFullPath(destinationPath);
        var isDirectory =
            Directory.Exists(targetPath) ||
            Path.EndsInDirectorySeparator(destinationPath);
        if (isDirectory)
        {
            targetPath = Path.Combine(
                targetPath,
                GetSuggestedFileName(materializedPath, asset));
        }
        else if (string.IsNullOrEmpty(Path.GetExtension(targetPath)))
        {
            targetPath += Path.GetExtension(materializedPath);
        }
        return targetPath;
    }

    private static string GetSuggestedFileName(
        string materializedPath,
        AssetRef asset)
    {
        if (asset.Metadata != null &&
            asset.Metadata.TryGetValue("name", out var nameValue) &&
            nameValue is string name &&
            !string.IsNullOrWhiteSpace(name))
        {
            return Path.GetFileName(name);
        }

        if (Uri.TryCreate(asset.Uri, UriKind.Absolute, out var uri))
        {
            var uriName = Path.GetFileName(uri.IsFile
                ? uri.LocalPath
                : uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(uriName))
                return uriName;
        }

        var pathName = Path.GetFileName(asset.Uri);
        return !string.IsNullOrWhiteSpace(pathName)
            ? pathName
            : Path.GetFileName(materializedPath);
    }
}
