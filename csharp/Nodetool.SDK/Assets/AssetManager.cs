using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Implementation of asset management with local caching.
/// </summary>
public class AssetManager : IAssetManager
{
    private readonly HttpClient _httpClient;
    private readonly INodetoolClient? _nodetoolClient;
    private readonly ILogger<AssetManager> _logger;
    private readonly string _cacheDirectory;

    /// <inheritdoc/>
    public string CacheDirectory => _cacheDirectory;

    /// <summary>
    /// Creates a new asset manager.
    /// </summary>
    /// <param name="cacheDirectory">Cache directory path. Defaults to ~/.nodetool/cache/assets/</param>
    /// <param name="nodetoolClient">Optional NodeTool API client for uploads.</param>
    /// <param name="httpClient">Optional HTTP client for downloads.</param>
    /// <param name="logger">Logger instance.</param>
    public AssetManager(
        string? cacheDirectory = null,
        INodetoolClient? nodetoolClient = null,
        HttpClient? httpClient = null,
        ILogger<AssetManager>? logger = null)
    {
        _cacheDirectory = cacheDirectory ?? GetDefaultCacheDirectory();
        _nodetoolClient = nodetoolClient;
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AssetManager>.Instance;

        // Ensure cache directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAssetAsync(AssetRef asset, string? localPath = null, CancellationToken cancellationToken = default)
    {
        return await DownloadAssetAsync(asset.Uri, localPath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAssetAsync(string uri, string? localPath = null, CancellationToken cancellationToken = default)
    {
        // Check if already cached
        var cachedPath = GetCachedPath(uri);
        if (cachedPath != null && File.Exists(cachedPath))
        {
            _logger.LogDebug("Asset cache hit: {Uri} -> {Path}", uri, cachedPath);
            return cachedPath;
        }

        // Handle data URIs (base64)
        if (uri.StartsWith("data:"))
        {
            return await SaveDataUri(uri, localPath, cancellationToken);
        }

        // Handle file URIs
        if (uri.StartsWith("file://"))
        {
            var filePath = uri.Substring(7);
            if (File.Exists(filePath))
                return filePath;
            throw new FileNotFoundException($"Local file not found: {filePath}");
        }

        // Handle HTTP/HTTPS URIs
        if (uri.StartsWith("http://") || uri.StartsWith("https://"))
        {
            return await DownloadHttpAsset(uri, localPath, cancellationToken);
        }

        // If it looks like a local path already, just return it
        if (File.Exists(uri))
            return uri;

        throw new ArgumentException($"Unsupported URI scheme: {uri}");
    }

    /// <inheritdoc/>
    public string? GetCachedPath(AssetRef asset)
    {
        return GetCachedPath(asset.Uri);
    }

    /// <inheritdoc/>
    public string? GetCachedPath(string uri)
    {
        if (string.IsNullOrEmpty(uri))
            return null;

        var cacheKey = GetCacheKey(uri);
        var extension = GetExtensionFromUri(uri);
        var cachedPath = Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");

        return File.Exists(cachedPath) ? cachedPath : null;
    }

    /// <inheritdoc/>
    public async Task<AssetRef> UploadAssetAsync(string localPath, string contentType, CancellationToken cancellationToken = default)
    {
        if (_nodetoolClient == null)
        {
            throw new InvalidOperationException("NodeTool client not configured for asset uploads");
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Local file not found: {localPath}");
        }

        var fileName = Path.GetFileName(localPath);
        await using var stream = File.OpenRead(localPath);

        var response = await _nodetoolClient.UploadAssetAsync(fileName, stream, cancellationToken);

        return new AssetRef
        {
            Type = InferAssetType(contentType),
            Uri = $"/api/assets/{response.Id}/download",
            AssetId = response.Id
        };
    }

    /// <inheritdoc/>
    public void ClearCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            foreach (var file in Directory.GetFiles(_cacheDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cache file: {File}", file);
                }
            }
        }
        _logger.LogInformation("Cache cleared: {Directory}", _cacheDirectory);
    }

    /// <inheritdoc/>
    public long GetCacheSize()
    {
        if (!Directory.Exists(_cacheDirectory))
            return 0;

        return Directory.GetFiles(_cacheDirectory)
            .Sum(f => new FileInfo(f).Length);
    }

    private async Task<string> DownloadHttpAsset(string uri, string? localPath, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(uri);
        var extension = GetExtensionFromUri(uri);
        var targetPath = localPath ?? Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");

        _logger.LogDebug("Downloading asset: {Uri} -> {Path}", uri, targetPath);

        var response = await _httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Try to get extension from content type if not already set
        if (string.IsNullOrEmpty(extension) && response.Content.Headers.ContentType?.MediaType != null)
        {
            extension = GetExtensionFromContentType(response.Content.Headers.ContentType.MediaType);
            targetPath = localPath ?? Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = File.Create(targetPath);
#if NET8_0_OR_GREATER
        await response.Content.CopyToAsync(fileStream, cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        await response.Content.CopyToAsync(fileStream);
#endif

        _logger.LogDebug("Asset downloaded: {Uri} -> {Path} ({Size} bytes)", uri, targetPath, new FileInfo(targetPath).Length);
        return targetPath;
    }

    private async Task<string> SaveDataUri(string dataUri, string? localPath, CancellationToken cancellationToken)
    {
        // Parse data URI: data:image/png;base64,XXXXX
        var commaIndex = dataUri.IndexOf(',');
        if (commaIndex < 0)
            throw new ArgumentException("Invalid data URI");

        var header = dataUri.Substring(5, commaIndex - 5); // Skip "data:"
        var base64Data = dataUri.Substring(commaIndex + 1);

        // Parse content type and encoding
        var parts = header.Split(';');
        var contentType = parts[0];
        var extension = GetExtensionFromContentType(contentType);

        var cacheKey = GetCacheKey(dataUri);
        var targetPath = localPath ?? Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = Convert.FromBase64String(base64Data);
#if NET8_0_OR_GREATER
        await File.WriteAllBytesAsync(targetPath, data, cancellationToken);
#else
        cancellationToken.ThrowIfCancellationRequested();
        await File.WriteAllBytesAsync(targetPath, data);
#endif

        _logger.LogDebug("Data URI saved: {Path} ({Size} bytes)", targetPath, data.Length);
        return targetPath;
    }

    // Placeholder URI for data URI parsing
    private const string DataUriPlaceholder = "http://data.local/placeholder";

    private static string GetCacheKey(string uri)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(uri));
        // Take first 8 bytes for a shorter cache key
        return ToHexString(hash, 8);
    }

    private static string GetExtensionFromUri(string uri)
    {
        try
        {
            // Data URIs don't have a path, use placeholder for parsing
            var uriObj = new Uri(uri.StartsWith("data:") ? DataUriPlaceholder : uri);
            var path = uriObj.AbsolutePath;
            var ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
                return ext;
        }
        catch
        {
            // Ignore URI parsing errors
        }

        return "";
    }

    private static string GetExtensionFromContentType(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/wav" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            "video/avi" => ".avi",
            "application/json" => ".json",
            "text/plain" => ".txt",
            "text/csv" => ".csv",
            "application/pdf" => ".pdf",
            _ => ""
        };
    }

    private static string InferAssetType(string contentType)
    {
        if (contentType.StartsWith("image/"))
            return "image";
        if (contentType.StartsWith("audio/"))
            return "audio";
        if (contentType.StartsWith("video/"))
            return "video";
        if (contentType == "text/csv" || contentType == "application/json")
            return "dataframe";
        return "file";
    }

    private static string GetDefaultCacheDirectory()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userHome, ".nodetool", "cache", "assets");
    }

    private static string ToHexString(byte[] bytes, int length)
    {
        var count = Math.Min(length, bytes.Length);
        var sb = new StringBuilder(count * 2);
        for (var i = 0; i < count; i++)
        {
            sb.Append(bytes[i].ToString("x2"));
        }
        return sb.ToString();
    }
}
