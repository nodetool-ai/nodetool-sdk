using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.VL.Services;

internal sealed record AssetFileResult(
    string Path,
    string ContentType,
    string SourceUri,
    bool FromCache);

internal static class AssetFileMaterializer
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks =
        new(StringComparer.Ordinal);

    public static async Task<AssetFileResult> MaterializeAsync(
        AssetRef asset,
        bool forceRefresh,
        CancellationToken cancellationToken,
        string? cacheDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.IsEmpty())
            throw new InvalidOperationException("The asset reference is empty.");

        if (TryGetExistingLocalPath(asset.Uri, out var localPath))
        {
            return new AssetFileResult(
                localPath,
                InferContentType(localPath, asset),
                new Uri(localPath).AbsoluteUri,
                FromCache: false);
        }

        cacheDirectory ??= GetDefaultCacheDirectory();
        Directory.CreateDirectory(cacheDirectory);

        if (TryGetInlineBytes(asset, out var inlineBytes, out var inlineContentType))
        {
            var identity = CreateIdentity(asset, inlineBytes);
            var extension = GetExtension(asset.Uri, inlineContentType, asset.Type);
            return await WriteBytesAsync(
                cacheDirectory,
                identity,
                extension,
                inlineBytes,
                inlineContentType,
                asset.Uri,
                forceRefresh,
                cancellationToken);
        }

        var source = await ResolveSourceAsync(asset, cancellationToken);
        if (source.Uri.IsFile)
        {
            if (!File.Exists(source.Uri.LocalPath))
                throw new FileNotFoundException("Asset file was not found.", source.Uri.LocalPath);
            return new AssetFileResult(
                source.Uri.LocalPath,
                source.ContentType,
                source.Uri.ToString(),
                FromCache: false);
        }

        if (source.Uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException(
                $"Unsupported asset URI scheme '{source.Uri.Scheme}'.");

        var sourceIdentity = CreateIdentity(asset, Encoding.UTF8.GetBytes(source.Uri.ToString()));
        var sourceExtension = GetExtension(
            source.Name ?? source.Uri.AbsolutePath,
            source.ContentType,
            asset.Type);
        var targetPath = Path.Combine(cacheDirectory, $"{sourceIdentity}{sourceExtension}");
        var cacheLock = CacheLocks.GetOrAdd(sourceIdentity, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            var cachedPath = File.Exists(targetPath)
                ? targetPath
                : FindCachedFile(cacheDirectory, sourceIdentity);
            if (!forceRefresh && cachedPath != null)
            {
                return new AssetFileResult(
                    cachedPath,
                    string.IsNullOrWhiteSpace(source.ContentType)
                        ? Nodetool.SDK.Utilities.DataUri.GetMimeTypeFromExtension(
                            Path.GetExtension(cachedPath))
                        : source.ContentType,
                    source.Uri.ToString(),
                    FromCache: true);
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, source.Uri);
            var apiBase = NodeToolClientProvider.CurrentApiBaseUrl;
            var authToken = NodeToolClientProvider.CurrentAuthToken;
            if (!string.IsNullOrWhiteSpace(authToken) && IsSameOrigin(source.Uri, apiBase))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    authToken);
            }

            using var response = await HttpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContentType =
                response.Content.Headers.ContentType?.MediaType ??
                source.ContentType;
            var responseExtension = GetExtension(
                source.Name ?? source.Uri.AbsolutePath,
                responseContentType,
                asset.Type);
            var responseTargetPath = Path.Combine(
                cacheDirectory,
                $"{sourceIdentity}{responseExtension}");
            var temporaryPath = responseTargetPath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var sourceStream =
                    await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var targetStream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true))
                {
                    await sourceStream.CopyToAsync(targetStream, cancellationToken);
                }

                File.Move(temporaryPath, responseTargetPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            return new AssetFileResult(
                responseTargetPath,
                responseContentType,
                source.Uri.ToString(),
                FromCache: false);
        }
        finally
        {
            cacheLock.Release();
        }
    }

    internal static Uri? ResolveStoredAssetUri(string value)
    {
        if (!TryExtractAssetKey(value, out var key))
            return null;

        var apiBase = NodeToolClientProvider.CurrentApiBaseUrl;
        return apiBase == null || string.IsNullOrEmpty(Path.GetExtension(key))
            ? null
            : new Uri(apiBase, $"/api/storage/{Uri.EscapeDataString(key)}");
    }

    internal static string CreateIdentity(AssetRef asset, byte[] discriminator)
    {
        using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hasher.AppendData(Encoding.UTF8.GetBytes(
            $"{asset.Type}|{asset.AssetId}|{asset.TempId}|{asset.Uri}|"));
        hasher.AppendData(discriminator);
        return Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant()[..24];
    }

    private static async Task<ResolvedAssetSource> ResolveSourceAsync(
        AssetRef asset,
        CancellationToken cancellationToken)
    {
        var uriText = asset.Uri?.Trim() ?? "";
        if (TryExtractAssetKey(uriText, out var assetKey))
        {
            if (!string.IsNullOrEmpty(Path.GetExtension(assetKey)))
            {
                var storedUri = ResolveStoredAssetUri(uriText)
                    ?? throw new InvalidOperationException(
                        "Cannot resolve asset storage URI without an HTTP API URL.");
                return new ResolvedAssetSource(
                    storedUri,
                    Path.GetFileName(assetKey),
                    InferContentType(assetKey, asset));
            }

            return await ResolveAssetIdAsync(assetKey, asset, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(uriText))
        {
            if (Uri.TryCreate(uriText, UriKind.Absolute, out var absolute))
            {
                return new ResolvedAssetSource(
                    absolute,
                    Path.GetFileName(absolute.AbsolutePath),
                    InferContentType(absolute.AbsolutePath, asset));
            }

            if (uriText.StartsWith("/", StringComparison.Ordinal) &&
                NodeToolClientProvider.CurrentApiBaseUrl is { } apiBase)
            {
                var relative = new Uri(apiBase, uriText);
                return new ResolvedAssetSource(
                    relative,
                    Path.GetFileName(relative.AbsolutePath),
                    InferContentType(relative.AbsolutePath, asset));
            }
        }

        if (!string.IsNullOrWhiteSpace(asset.AssetId))
            return await ResolveAssetIdAsync(asset.AssetId, asset, cancellationToken);

        throw new InvalidOperationException(
            $"Asset URI '{uriText}' is not a local file, HTTP URL, storage URI, or resolvable asset ID.");
    }

    private static async Task<ResolvedAssetSource> ResolveAssetIdAsync(
        string assetId,
        AssetRef asset,
        CancellationToken cancellationToken)
    {
        AssetResponse? response = null;
        if (NodeToolClientProvider.IsConnected)
        {
            response = await NodeToolClientProvider
                .GetClient()
                .GetAssetAsync(assetId, cancellationToken);
        }

        if (response == null && NodeToolClientProvider.CurrentApiBaseUrl is { } apiBase)
        {
            using var client = new HttpClient();
            var api = new NodetoolClient(client);
            api.Configure(apiBase.ToString(), NodeToolClientProvider.CurrentAuthToken);
            response = await api.GetAssetAsync(assetId, cancellationToken);
        }

        if (response == null)
            throw new InvalidOperationException($"Asset '{assetId}' could not be resolved.");

        var uriText = response.GetUrl ?? response.Uri;
        if (string.IsNullOrWhiteSpace(uriText))
            throw new InvalidOperationException($"Asset '{assetId}' has no downloadable URI.");

        Uri uri;
        if (TryExtractAssetKey(uriText, out var storedKey) &&
            !string.IsNullOrEmpty(Path.GetExtension(storedKey)))
        {
            uri = ResolveStoredAssetUri(uriText)
                ?? throw new InvalidOperationException(
                    "Cannot resolve asset storage URI without an HTTP API URL.");
        }
        else if (Uri.TryCreate(uriText, UriKind.Absolute, out var absolute))
        {
            uri = absolute;
        }
        else if (NodeToolClientProvider.CurrentApiBaseUrl is { } currentApiBase)
        {
            uri = new Uri(currentApiBase, uriText);
        }
        else
        {
            throw new InvalidOperationException(
                $"Asset '{assetId}' returned a relative URI without an HTTP API URL.");
        }

        return new ResolvedAssetSource(
            uri,
            response.Name,
            string.IsNullOrWhiteSpace(response.ContentType)
                ? InferContentType(response.Name, asset)
                : response.ContentType);
    }

    private static async Task<AssetFileResult> WriteBytesAsync(
        string cacheDirectory,
        string identity,
        string extension,
        byte[] bytes,
        string contentType,
        string sourceUri,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var targetPath = Path.Combine(cacheDirectory, $"{identity}{extension}");
        var cacheLock = CacheLocks.GetOrAdd(targetPath, _ => new SemaphoreSlim(1, 1));
        await cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && File.Exists(targetPath))
            {
                return new AssetFileResult(
                    targetPath,
                    contentType,
                    sourceUri,
                    FromCache: true);
            }

            var temporaryPath = targetPath + $".{Guid.NewGuid():N}.tmp";
            try
            {
                await File.WriteAllBytesAsync(temporaryPath, bytes, cancellationToken);
                File.Move(temporaryPath, targetPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            return new AssetFileResult(
                targetPath,
                contentType,
                sourceUri,
                FromCache: false);
        }
        finally
        {
            cacheLock.Release();
        }
    }

    private static bool TryGetExistingLocalPath(string? value, out string path)
    {
        path = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            path = uri.LocalPath;
            return File.Exists(path);
        }

        try
        {
            var fullPath = Path.GetFullPath(trimmed);
            if (File.Exists(fullPath))
            {
                path = fullPath;
                return true;
            }
        }
        catch
        {
            // The value is not a valid local path.
        }

        return false;
    }

    private static string? FindCachedFile(string cacheDirectory, string identity)
        => Directory
            .EnumerateFiles(cacheDirectory, $"{identity}.*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path =>
                !path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));

    private static bool TryGetInlineBytes(
        AssetRef asset,
        out byte[] bytes,
        out string contentType)
    {
        contentType = InferContentType(asset.Uri, asset);
        if (asset.Data == null &&
            asset.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) &&
            TryDecodeDataString(asset.Uri, out bytes, out var uriContentType))
        {
            if (!string.IsNullOrWhiteSpace(uriContentType))
                contentType = uriContentType;
            return bytes.Length > 0;
        }

        if (asset is TextRef && asset.Data is string textData)
        {
            if (textData.StartsWith("data:", StringComparison.OrdinalIgnoreCase) &&
                TryDecodeDataString(textData, out bytes, out var textContentType))
            {
                if (!string.IsNullOrWhiteSpace(textContentType))
                    contentType = textContentType;
                return true;
            }

            bytes = Encoding.UTF8.GetBytes(textData);
            contentType = "text/plain";
            return true;
        }

        switch (asset.Data)
        {
            case byte[] direct:
                bytes = direct;
                return direct.Length > 0;
            case ReadOnlyMemory<byte> readOnlyMemory:
                bytes = readOnlyMemory.ToArray();
                return bytes.Length > 0;
            case Memory<byte> memory:
                bytes = memory.ToArray();
                return bytes.Length > 0;
            case string text when TryDecodeDataString(text, out bytes, out var parsedContentType):
                if (!string.IsNullOrWhiteSpace(parsedContentType))
                    contentType = parsedContentType;
                return bytes.Length > 0;
            default:
                bytes = Array.Empty<byte>();
                return false;
        }
    }

    private static bool TryDecodeDataString(
        string value,
        out byte[] bytes,
        out string contentType)
    {
        bytes = Array.Empty<byte>();
        contentType = "";
        try
        {
            if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var comma = value.IndexOf(',');
                if (comma <= 5)
                    return false;
                var header = value[5..comma];
                contentType = header.Split(';', 2)[0];
                bytes = header.Contains(";base64", StringComparison.OrdinalIgnoreCase)
                    ? Convert.FromBase64String(value[(comma + 1)..])
                    : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(value[(comma + 1)..]));
                return true;
            }

            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryExtractAssetKey(string value, out string key)
    {
        key = "";
        if (!value.StartsWith("asset:", StringComparison.OrdinalIgnoreCase))
            return false;
        var encoded = value["asset:".Length..].TrimStart('/');
        if (string.IsNullOrWhiteSpace(encoded))
            return false;
        key = Uri.UnescapeDataString(encoded);
        return true;
    }

    private static string InferContentType(string? source, AssetRef asset)
    {
        if (asset is ImageRef { MimeType: { Length: > 0 } mimeType })
            return mimeType;
        if (asset.Metadata != null)
        {
            foreach (var key in new[] { "content_type", "mime_type", "mimeType" })
            {
                if (asset.Metadata.TryGetValue(key, out var value) &&
                    value is string metadataContentType &&
                    !string.IsNullOrWhiteSpace(metadataContentType))
                {
                    return metadataContentType;
                }
            }
        }

        return Nodetool.SDK.Utilities.DataUri.GetMimeTypeFromExtension(
            Path.GetExtension(source ?? ""));
    }

    private static string GetExtension(
        string? source,
        string? contentType,
        string assetType)
    {
        var extension = Path.GetExtension(source ?? "");
        if (!string.IsNullOrWhiteSpace(extension) &&
            extension.Length <= 10 &&
            extension.All(character => char.IsLetterOrDigit(character) || character == '.'))
        {
            return extension.ToLowerInvariant();
        }

        return contentType?.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            "image/svg+xml" => ".svg",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/mpeg" or "audio/mp3" => ".mp3",
            "audio/mp4" or "audio/x-m4a" => ".m4a",
            "audio/aac" => ".aac",
            "audio/ogg" => ".ogg",
            "audio/flac" => ".flac",
            "video/mp4" => ".mp4",
            "video/quicktime" => ".mov",
            "video/webm" => ".webm",
            "video/x-msvideo" => ".avi",
            "video/x-matroska" => ".mkv",
            "application/pdf" => ".pdf",
            "application/json" => ".json",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "text/csv" => ".csv",
            "text/markdown" => ".md",
            "text/plain" => ".txt",
            _ => assetType switch
            {
                "image" => ".png",
                "document" => ".bin",
                _ => ".bin"
            }
        };
    }

    private static string GetDefaultCacheDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Nodetool",
            "SdkCache",
            "assets");

    private static bool IsSameOrigin(Uri target, Uri? origin)
        => origin != null &&
           string.Equals(target.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase) &&
           string.Equals(target.Host, origin.Host, StringComparison.OrdinalIgnoreCase) &&
           target.Port == origin.Port;

    private sealed record ResolvedAssetSource(
        Uri Uri,
        string? Name,
        string ContentType);
}
