using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Thin VL connection projection over portable asset upload and save services.
/// </summary>
internal static class AssetTransferService
{
    internal static async Task<AssetRef> UploadAsync(
        string localPath,
        string? contentType,
        bool temporary,
        CancellationToken cancellationToken)
    {
        if (NodeToolClientProvider.CurrentApiBaseUrl is not { } apiBaseUrl)
        {
            throw new InvalidOperationException(
                "Connect to NodeTool before uploading an asset.");
        }

        var fullPath = Path.GetFullPath(localPath);
        var resolvedContentType = string.IsNullOrWhiteSpace(contentType)
            ? AssetContentType.FromPath(fullPath)
            : contentType.Trim();
        using var client = new NodetoolClient(
            apiBaseUrl,
            NodeToolClientProvider.CurrentAuthToken);
        var uploader = new AssetUploader(
            client,
            useTemporaryUploads: temporary);
        return await uploader.UploadAssetAsync(
            fullPath,
            resolvedContentType,
            cancellationToken);
    }

    internal static Task<AssetSaveResult> SaveAsync(
        AssetRef asset,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
        => new AssetSaver(AssetFileMaterializer.CreateMaterializer())
            .SaveAsync(
                asset,
                destinationPath,
                overwrite,
                cancellationToken);
}
