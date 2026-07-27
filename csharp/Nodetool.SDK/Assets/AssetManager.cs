using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Diagnostics;
using Nodetool.SDK.Types.Assets;

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
    private readonly bool _ownsHttpClient;
    private readonly bool _useTemporaryUploads;
    private bool _disposed;

    /// <inheritdoc/>
    public string CacheDirectory => _cacheDirectory;

    /// <summary>
    /// Creates a new asset manager.
    /// </summary>
    /// <param name="cacheDirectory">Cache directory path. Defaults to ~/.nodetool/cache/assets/</param>
    /// <param name="nodetoolClient">Optional NodeTool API client for uploads.</param>
    /// <param name="httpClient">Optional HTTP client for downloads.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="useTemporaryUploads">
    /// Use the SDK temporary execution-input route instead of creating
    /// persistent assets.
    /// </param>
    public AssetManager(
        string? cacheDirectory = null,
        INodetoolClient? nodetoolClient = null,
        HttpClient? httpClient = null,
        ILogger<AssetManager>? logger = null,
        bool useTemporaryUploads = false)
    {
        _cacheDirectory = cacheDirectory ?? GetDefaultCacheDirectory();
        _nodetoolClient = nodetoolClient;
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        _useTemporaryUploads = useTemporaryUploads;
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
        ArgumentNullException.ThrowIfNull(asset);
        return await DownloadAssetAsync(asset.Uri, localPath, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAssetAsync(string uri, string? localPath = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        // Check if already cached
        var cachedPath = GetCachedPath(uri);
        if (cachedPath != null && File.Exists(cachedPath))
        {
            _logger.LogDebug(
                "Asset cache hit: {Uri} -> {Path}",
                SafeUri(uri),
                cachedPath);
            return cachedPath;
        }

        // Handle data URIs (base64)
        if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return await SaveDataUri(uri, localPath, cancellationToken);
        }

        // Handle file URIs
        if (Uri.TryCreate(uri, UriKind.Absolute, out var fileUri) &&
            fileUri.IsFile)
        {
            var filePath = fileUri.LocalPath;
            if (File.Exists(filePath))
                return filePath;
            throw new FileNotFoundException($"Local file not found: {filePath}");
        }

        // Handle HTTP/HTTPS URIs
        if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
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
        ArgumentNullException.ThrowIfNull(asset);
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
        return await UploadAssetAsync(
            fileName,
            stream,
            contentType,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AssetRef> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (_nodetoolClient == null)
        {
            throw new InvalidOperationException("NodeTool client not configured for asset uploads");
        }

        using var nonDisposingContent = new NonDisposingStream(content);
        if (_useTemporaryUploads)
        {
            var temporary = await _nodetoolClient
                .UploadTemporaryAssetAsync(
                    fileName,
                    nonDisposingContent,
                    contentType,
                    cancellationToken);
            return CreateTemporaryAssetReference(
                temporary,
                contentType);
        }

        var persistent = await _nodetoolClient.UploadAssetAsync(
            fileName,
            nonDisposingContent,
            contentType,
            cancellationToken);
        return CreateAssetReference(persistent, contentType);
    }

    /// <inheritdoc/>
    public async Task<AssetRef> UploadAssetAsync(
        string fileName,
        ReadOnlyMemory<byte> content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(content.ToArray(), writable: false);
        return await UploadAssetAsync(
            fileName,
            stream,
            contentType,
            cancellationToken);
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
                    _logger.LogWarning(
                        "Failed to delete cache file {File}: {Error}",
                        file,
                        NodeToolDiagnosticRedactor.RedactText(ex.Message));
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_ownsHttpClient)
            _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<string> DownloadHttpAsset(string uri, string? localPath, CancellationToken cancellationToken)
    {
        var cacheKey = GetCacheKey(uri);
        var extension = GetExtensionFromUri(uri);
        var targetPath = localPath ?? Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");

        _logger.LogDebug(
            "Downloading asset: {Uri} -> {Path}",
            SafeUri(uri),
            targetPath);

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
        await response.Content.CopyToAsync(fileStream, cancellationToken);

        _logger.LogDebug(
            "Asset downloaded: {Uri} -> {Path} ({Size} bytes)",
            SafeUri(uri),
            targetPath,
            new FileInfo(targetPath).Length);
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
        await File.WriteAllBytesAsync(targetPath, data, cancellationToken);

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
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
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

    private static AssetRef CreateAssetReference(
        AssetResponse response,
        string requestedContentType)
    {
        var contentType = string.IsNullOrWhiteSpace(response.ContentType)
            ? requestedContentType
            : response.ContentType;
        var result = CreateTypedAssetReference(contentType);

        result.AssetId = response.Id;
        result.Uri = response.GetUrl
            ?? (!string.IsNullOrWhiteSpace(response.Uri)
                ? response.Uri
                : $"/api/assets/{response.Id}/download");
        result.Metadata = new Dictionary<string, object?>
        {
            ["content_type"] = contentType,
            ["name"] = response.Name,
            ["size"] = response.Size
        };
        return result;
    }

    private static AssetRef CreateTemporaryAssetReference(
        TemporaryAssetUploadResponse response,
        string requestedContentType)
    {
        if (string.IsNullOrWhiteSpace(response.Uri))
        {
            throw new InvalidOperationException(
                "Temporary asset upload returned no URI.");
        }

        var contentType = string.IsNullOrWhiteSpace(response.ContentType)
            ? requestedContentType
            : response.ContentType;
        var result = CreateTypedAssetReference(contentType);
        result.Uri = response.Uri;
        result.Metadata = new Dictionary<string, object?>
        {
            ["content_type"] = contentType,
            ["name"] = response.Name,
            ["size"] = response.Size,
            ["temporary"] = true,
            ["expires_at"] = response.ExpiresAt
        };
        return result;
    }

    private static AssetRef CreateTypedAssetReference(string contentType)
        => contentType.ToLowerInvariant() switch
        {
            var value when value.StartsWith("image/") => new ImageRef
            {
                MimeType = contentType
            },
            var value when value.StartsWith("audio/") => new AudioRef(),
            var value when value.StartsWith("video/") => new VideoRef(),
            "application/pdf" => new DocumentRef(),
            _ => new GenericAssetRef()
        };

    private static string SafeUri(string value)
    {
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return NodeToolDiagnosticRedactor.RedactText(value);
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? NodeToolDiagnosticRedactor.RedactUri(uri).AbsoluteUri
            : NodeToolDiagnosticRedactor.RedactText(value);
    }

    private static string GetDefaultCacheDirectory()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userHome, ".nodetool", "cache", "assets");
    }

    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count)
            => inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin)
            => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count)
            => inner.Write(buffer, offset, count);
        public override Task FlushAsync(CancellationToken cancellationToken)
            => inner.FlushAsync(cancellationToken);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            // The caller owns the wrapped stream.
            base.Dispose(disposing);
        }
    }
}
