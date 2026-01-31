using System.Text;

namespace Nodetool.SDK.Utilities;

/// <summary>
/// Helper for encoding local files as data URIs (Option B UX: "I have a file path, send it").
/// </summary>
public static class DataUri
{
    public static async Task<string> FromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required", nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        var bytes = await File.ReadAllBytesAsync(filePath);
        var mime = GetMimeTypeFromExtension(Path.GetExtension(filePath));
        var b64 = Convert.ToBase64String(bytes);
        return $"data:{mime};base64,{b64}";
    }

    public static string GetMimeTypeFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return "application/octet-stream";

        var ext = extension.Trim().TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            // Images
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "webp" => "image/webp",
            "svg" => "image/svg+xml",

            // Audio
            "wav" => "audio/wav",
            "mp3" => "audio/mpeg",
            "ogg" => "audio/ogg",
            "flac" => "audio/flac",

            // Video (if we later support)
            "mp4" => "video/mp4",
            "webm" => "video/webm",

            _ => "application/octet-stream"
        };
    }
}

