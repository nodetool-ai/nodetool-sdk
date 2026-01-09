using Nodetool.SDK.Api;
using Nodetool.SDK.Utilities;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Upload helpers that hide AssetRef construction; caller just provides a local file path.
/// </summary>
public static class AssetUploader
{
    /// <summary>
    /// Upload a local file to NodeTool and return an AssetRef with an ABSOLUTE download URL.
    /// </summary>
    public static async Task<AssetRef> UploadFromPathAsync(
        INodetoolClient client,
        Uri apiBaseUrl,
        string localPath,
        CancellationToken cancellationToken = default)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        if (apiBaseUrl == null) throw new ArgumentNullException(nameof(apiBaseUrl));
        if (string.IsNullOrWhiteSpace(localPath)) throw new ArgumentException("Local path is required", nameof(localPath));
        if (!File.Exists(localPath)) throw new FileNotFoundException("File not found", localPath);

        var fileName = Path.GetFileName(localPath);
        await using var stream = File.OpenRead(localPath);
        var response = await client.UploadAssetAsync(fileName, stream, cancellationToken);

        var contentType = DataUri.GetMimeTypeFromExtension(Path.GetExtension(localPath));
        var assetType = AssetTypeUtils.InferAssetTypeFromContentType(contentType);

        // Canonical download endpoint.
        var downloadRelative = $"/api/assets/{response.Id}/download";
        var absolute = AssetUri.ToAbsolute(downloadRelative, apiBaseUrl);

        return new AssetRef
        {
            Type = assetType,
            Uri = absolute,
            AssetId = response.Id,
        };
    }
}


