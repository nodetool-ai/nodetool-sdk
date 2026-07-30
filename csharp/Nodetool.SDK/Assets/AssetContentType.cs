namespace Nodetool.SDK.Assets;

/// <summary>
/// Small host-neutral MIME type helper used by asset upload and save flows.
/// </summary>
public static class AssetContentType
{
    public static string FromPath(
        string path,
        string fallback = "application/octet-stream")
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
            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".glb" => "model/gltf-binary",
            ".gltf" => "model/gltf+json",
            ".obj" => "text/plain",
            ".ttf" => "font/ttf",
            ".otf" => "font/otf",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => fallback
        };
}
