namespace Nodetool.SDK.Assets;

/// <summary>
/// Represents a reference to an asset (image, audio, video, etc.).
/// </summary>
public class AssetRef
{
    /// <summary>
    /// Type of asset: "image", "audio", "video", "dataframe", etc.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// URI of the asset (could be http URL, data URI, or file path).
    /// </summary>
    public string Uri { get; set; } = "";

    /// <summary>
    /// Optional asset ID for server-stored assets.
    /// </summary>
    public string? AssetId { get; set; }
}

/// <summary>
/// Interface for managing asset downloads and caching.
/// </summary>
public interface IAssetManager
{
    /// <summary>
    /// Download an asset to the local cache.
    /// </summary>
    /// <param name="asset">Asset reference to download.</param>
    /// <param name="localPath">Optional specific local path. If null, uses cache directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Local file path where the asset was saved.</returns>
    Task<string> DownloadAssetAsync(AssetRef asset, string? localPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download an asset by URI to the local cache.
    /// </summary>
    /// <param name="uri">Asset URI to download.</param>
    /// <param name="localPath">Optional specific local path. If null, uses cache directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Local file path where the asset was saved.</returns>
    Task<string> DownloadAssetAsync(string uri, string? localPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the cached path for an asset if it exists in cache.
    /// </summary>
    /// <param name="asset">Asset reference.</param>
    /// <returns>Local path if cached, null otherwise.</returns>
    string? GetCachedPath(AssetRef asset);

    /// <summary>
    /// Get the cached path for a URI if it exists in cache.
    /// </summary>
    /// <param name="uri">Asset URI.</param>
    /// <returns>Local path if cached, null otherwise.</returns>
    string? GetCachedPath(string uri);

    /// <summary>
    /// Upload a local file as an asset.
    /// </summary>
    /// <param name="localPath">Path to the local file.</param>
    /// <param name="contentType">MIME content type of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asset reference for the uploaded file.</returns>
    Task<AssetRef> UploadAssetAsync(string localPath, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear the entire cache.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Get the current cache size in bytes.
    /// </summary>
    /// <returns>Cache size in bytes.</returns>
    long GetCacheSize();

    /// <summary>
    /// Cache directory path.
    /// </summary>
    string CacheDirectory { get; }
}
