using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Interface for managing asset downloads and caching.
/// </summary>
public interface IAssetManager : IDisposable
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
    /// Upload a stream as an asset. The caller retains ownership of the stream.
    /// </summary>
    Task<AssetRef> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload in-memory bytes as an asset.
    /// </summary>
    Task<AssetRef> UploadAssetAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default);

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
