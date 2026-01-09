namespace Nodetool.SDK.Assets;

/// <summary>
/// High-level helpers for consuming assets without manually handling URI schemes.
/// </summary>
public static class AssetResolver
{
    /// <summary>
    /// Resolve an asset reference to a local file path (uses cache; downloads if needed).
    /// </summary>
    public static Task<string> ResolveLocalPathAsync(
        IAssetManager assetManager,
        AssetRef asset,
        Uri? apiBaseUrl = null,
        string? localPath = null,
        CancellationToken cancellationToken = default)
    {
        if (assetManager == null) throw new ArgumentNullException(nameof(assetManager));
        if (asset == null) throw new ArgumentNullException(nameof(asset));

        var uri = AssetUri.ToAbsolute(asset.Uri, apiBaseUrl);
        return assetManager.DownloadAssetAsync(uri, localPath, cancellationToken);
    }

    /// <summary>
    /// Resolve a URI/path into a local file path (uses cache; downloads if needed).
    /// </summary>
    public static Task<string> ResolveLocalPathAsync(
        IAssetManager assetManager,
        string uriOrPath,
        Uri? apiBaseUrl = null,
        string? localPath = null,
        CancellationToken cancellationToken = default)
    {
        if (assetManager == null) throw new ArgumentNullException(nameof(assetManager));
        if (string.IsNullOrWhiteSpace(uriOrPath)) throw new ArgumentException("Value is required", nameof(uriOrPath));

        var normalized = AssetUri.ToAbsolute(uriOrPath, apiBaseUrl);
        // If it's a direct local path, shortcut (keeps original file extension and avoids copying).
        try
        {
            var full = Path.GetFullPath(normalized);
            if (File.Exists(full))
                return Task.FromResult(full);
        }
        catch
        {
            // ignore
        }

        return assetManager.DownloadAssetAsync(normalized, localPath, cancellationToken);
    }

    /// <summary>
    /// Best-effort cache check only (no network).
    /// Returns null if not cached.
    /// </summary>
    public static string? TryGetCachedPath(IAssetManager assetManager, string uriOrPath, Uri? apiBaseUrl = null)
    {
        if (assetManager == null) throw new ArgumentNullException(nameof(assetManager));
        if (string.IsNullOrWhiteSpace(uriOrPath)) return null;
        var normalized = AssetUri.ToAbsolute(uriOrPath, apiBaseUrl);
        return assetManager.GetCachedPath(normalized);
    }
}


