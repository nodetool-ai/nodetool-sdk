using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Prepares host-neutral media values for workflow execution.
/// Small local values are inlined; values above the configured limit are
/// uploaded through an injected asset manager.
/// </summary>
public sealed class MediaInputPreparer
{
    public const int DefaultInlineLimitBytes = 4 * 1024 * 1024;

    private readonly IAssetManager? _assetManager;
    private readonly long _inlineLimitBytes;

    public MediaInputPreparer(
        IAssetManager? assetManager = null,
        long inlineLimitBytes = DefaultInlineLimitBytes)
    {
        if (inlineLimitBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(inlineLimitBytes));
        _assetManager = assetManager;
        _inlineLimitBytes = inlineLimitBytes;
    }

    public async Task<object> PrepareAsync(
        string inputName,
        string mediaType,
        object? value,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputName);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        mediaType = NormalizeMediaType(mediaType);

        byte[]? bytes = value switch
        {
            byte[] array => array,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            Memory<byte> memory => memory.ToArray(),
            _ => null
        };
        string? uriText = null;

        if (value is AssetRef asset)
        {
            bytes = GetAssetBytes(asset.Data);
            uriText = asset.Uri;
            if (bytes is { Length: > 0 } &&
                ShouldUpload(bytes.LongLength))
            {
                var format = DetectContent(bytes, mediaType);
                return await UploadBytesAsync(
                    inputName,
                    mediaType,
                    bytes,
                    format.Extension,
                    format.ContentType,
                    cancellationToken);
            }

            if (!asset.IsEmpty())
                return ToTransport(asset, mediaType, preserveData: true);
        }
        else if (bytes == null)
        {
            uriText = value switch
            {
                string text => text.Trim().Trim('"'),
                Uri uri => uri.ToString(),
                null => null,
                _ => value.ToString()?.Trim()
            };

            if (!string.IsNullOrWhiteSpace(uriText))
            {
                var fullPath = TryGetFullPath(uriText);
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    if (ShouldUpload(fileInfo.Length))
                    {
                        return await UploadFileAsync(
                            inputName,
                            mediaType,
                            fullPath,
                            cancellationToken);
                    }

                    bytes = await File.ReadAllBytesAsync(
                        fullPath,
                        cancellationToken);
                    uriText = new Uri(fullPath).AbsoluteUri;
                }
            }
        }

        if ((bytes == null || bytes.Length == 0) &&
            string.IsNullOrWhiteSpace(uriText))
        {
            throw new InvalidOperationException(
                $"{mediaType} input '{inputName}' is empty. " +
                "Provide an asset reference, file path, URL, or bytes.");
        }

        if (bytes is { Length: > 0 } && ShouldUpload(bytes.LongLength))
        {
            var format = DetectContent(bytes, mediaType);
            return await UploadBytesAsync(
                inputName,
                mediaType,
                bytes,
                format.Extension,
                format.ContentType,
                cancellationToken);
        }

        return new Dictionary<string, object?>
        {
            ["type"] = mediaType,
            ["asset_id"] = null,
            ["uri"] = uriText ?? "",
            ["data"] = bytes
        };
    }

    private bool ShouldUpload(long byteCount)
        => byteCount > _inlineLimitBytes;

    private async Task<object> UploadFileAsync(
        string inputName,
        string mediaType,
        string path,
        CancellationToken cancellationToken)
    {
        var manager = RequireAssetManager(inputName, mediaType);
        var asset = await manager.UploadAssetAsync(
            path,
            GetContentType(path, mediaType),
            cancellationToken);
        return ToTransport(asset, mediaType, preserveData: false);
    }

    private async Task<object> UploadBytesAsync(
        string inputName,
        string mediaType,
        byte[] bytes,
        string extension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var manager = RequireAssetManager(inputName, mediaType);
        var asset = await manager.UploadAssetAsync(
            $"nodetool-{mediaType}-{Guid.NewGuid():N}{extension}",
            bytes,
            contentType,
            cancellationToken);
        return ToTransport(asset, mediaType, preserveData: false);
    }

    private IAssetManager RequireAssetManager(
        string inputName,
        string mediaType)
        => _assetManager ?? throw new InvalidOperationException(
            $"Cannot upload large {mediaType} input '{inputName}': " +
            "no asset manager is configured.");

    private static Dictionary<string, object> ToTransport(
        AssetRef asset,
        string mediaType,
        bool preserveData)
    {
        var transport = asset.ToDict();
        transport["type"] = mediaType;
        if (!preserveData)
            transport["data"] = null!;
        return transport;
    }

    private static byte[]? GetAssetBytes(object? data)
        => data switch
        {
            byte[] bytes => bytes,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            Memory<byte> memory => memory.ToArray(),
            _ => null
        };

    private static string TryGetFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    private static string NormalizeMediaType(string mediaType)
        => mediaType.Trim().ToLowerInvariant() switch
        {
            "asset_ref" => "asset",
            var normalized => normalized
        };

    private static string GetContentType(string path, string mediaType)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".wav" => "audio/wav",
            ".mp3" => "audio/mpeg",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".mkv" => "video/x-matroska",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".json" => "application/json",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".obj" => "text/plain",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => GetDefaultContentType(mediaType)
        };

    private static string GetDefaultContentType(string mediaType)
        => mediaType == "image"
            ? "image/png"
            : "application/octet-stream";

    private static string GetDefaultExtension(string mediaType)
        => mediaType == "image" ? ".png" : ".bin";

    private static (string Extension, string ContentType) DetectContent(
        ReadOnlySpan<byte> bytes,
        string mediaType)
    {
        if (StartsWith(bytes, [0x89, 0x50, 0x4E, 0x47]))
            return (".png", "image/png");
        if (StartsWith(bytes, [0xFF, 0xD8, 0xFF]))
            return (".jpg", "image/jpeg");
        if (StartsWithAscii(bytes, "GIF87a") ||
            StartsWithAscii(bytes, "GIF89a"))
        {
            return (".gif", "image/gif");
        }
        if (StartsWithAscii(bytes, "RIFF") &&
            HasAsciiAt(bytes, 8, "WEBP"))
        {
            return (".webp", "image/webp");
        }
        if (StartsWithAscii(bytes, "RIFF") &&
            HasAsciiAt(bytes, 8, "WAVE"))
        {
            return (".wav", "audio/wav");
        }
        if (StartsWithAscii(bytes, "OggS"))
            return (".ogg", "audio/ogg");
        if (StartsWithAscii(bytes, "fLaC"))
            return (".flac", "audio/flac");
        if (StartsWithAscii(bytes, "%PDF"))
            return (".pdf", "application/pdf");
        if (HasAsciiAt(bytes, 4, "ftyp"))
            return (".mp4", mediaType == "audio" ? "audio/mp4" : "video/mp4");
        if (StartsWithAscii(bytes, "glTF"))
            return (".glb", "model/gltf-binary");

        return (
            GetDefaultExtension(mediaType),
            GetDefaultContentType(mediaType));
    }

    private static bool StartsWith(
        ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> prefix)
        => value.StartsWith(prefix);

    private static bool StartsWithAscii(
        ReadOnlySpan<byte> value,
        string prefix)
        => HasAsciiAt(value, 0, prefix);

    private static bool HasAsciiAt(
        ReadOnlySpan<byte> value,
        int offset,
        string expected)
    {
        if (offset < 0 || value.Length - offset < expected.Length)
            return false;
        for (var index = 0; index < expected.Length; index++)
        {
            if (value[offset + index] != (byte)expected[index])
                return false;
        }
        return true;
    }
}
