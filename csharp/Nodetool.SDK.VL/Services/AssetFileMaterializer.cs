using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.VL.Services;

internal sealed record AssetFileResult(
    string Path,
    string ContentType,
    string SourceUri,
    bool FromCache);

/// <summary>
/// Thin VL connection projection over the host-neutral asset materializer.
/// </summary>
internal static class AssetFileMaterializer
{
    public static async Task<AssetFileResult> MaterializeAsync(
        AssetRef asset,
        bool forceRefresh,
        CancellationToken cancellationToken,
        string? cacheDirectory = null)
    {
        var materializer = CreateMaterializer(cacheDirectory);
        var result = await materializer.MaterializeAsync(
            asset,
            forceRefresh,
            cancellationToken);
        return new AssetFileResult(
            result.Path,
            result.ContentType,
            result.SourceUri,
            result.FromCache);
    }

    internal static Uri? ResolveStoredAssetUri(string value)
        => CreateMaterializer().ResolveStoredAssetUri(value);

    private static AssetMaterializer CreateMaterializer(
        string? cacheDirectory = null)
        => new(
            resolveAsset: NodeToolClientProvider.IsConnected
                ? NodeToolClientProvider.GetClient().GetAssetAsync
                : null,
            apiBaseUrl: NodeToolClientProvider.CurrentApiBaseUrl,
            authToken: NodeToolClientProvider.CurrentAuthToken,
            cacheDirectory: cacheDirectory);
}
