using System.Security.Cryptography;

namespace Nodetool.SDK.VL.Utilities;

internal static class AssetCacheWriter
{
    public static string GetOrWriteFile(string cacheDir, string prefix, byte[] bytes, string extensionWithDot)
    {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        if (string.IsNullOrWhiteSpace(cacheDir)) throw new ArgumentException("Cache directory is required", nameof(cacheDir));

        Directory.CreateDirectory(cacheDir);

        var ext = string.IsNullOrWhiteSpace(extensionWithDot) ? ".bin" : extensionWithDot.Trim();
        if (!ext.StartsWith(".", StringComparison.Ordinal))
            ext = "." + ext;

        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var fileName = $"{prefix}-{hash}{ext}";
        var path = Path.Combine(cacheDir, fileName);

        if (File.Exists(path))
            return path;

        // Atomic-ish write: write temp then move into place.
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, bytes);
        try
        {
            File.Move(tmp, path, overwrite: false);
        }
        catch
        {
            // If someone else won the race, keep the existing file.
            if (!File.Exists(path))
                throw;
            try { File.Delete(tmp); } catch { /* ignore */ }
        }

        return path;
    }

    public static string GetAudioExtension(byte[] bytes, string? declaredFormat)
    {
        // Sniff common headers first (more reliable than metadata).
        if (bytes.Length >= 12)
        {
            // WAV: "RIFF" .... "WAVE"
            if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'A' && bytes[10] == (byte)'V' && bytes[11] == (byte)'E')
                return ".wav";

            // MP3 (ID3 tag)
            if (bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3')
                return ".mp3";

            // OGG
            if (bytes[0] == (byte)'O' && bytes[1] == (byte)'g' && bytes[2] == (byte)'g' && bytes[3] == (byte)'S')
                return ".ogg";

            // FLAC
            if (bytes[0] == (byte)'f' && bytes[1] == (byte)'L' && bytes[2] == (byte)'a' && bytes[3] == (byte)'C')
                return ".flac";
        }

        var fmt = (declaredFormat ?? "").Trim().TrimStart('.').ToLowerInvariant();
        return fmt switch
        {
            "wav" => ".wav",
            "mp3" or "mpeg" => ".mp3",
            "ogg" => ".ogg",
            "flac" => ".flac",
            _ => ".bin"
        };
    }
}


