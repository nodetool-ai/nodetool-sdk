using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Values;

namespace Nodetool.SDK.Assets;

/// <summary>
/// Converts transport-level NodeTool media values into canonical typed asset
/// references. It performs no I/O and is reusable by VL, Unity, and console
/// hosts.
/// </summary>
public static class NodeToolAssetValueParser
{
    public static bool TryParse(
        NodeToolValue value,
        string expectedMediaType,
        out AssetRef asset)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedMediaType);

        expectedMediaType = NormalizeMediaType(expectedMediaType);
        asset = CreateAsset(expectedMediaType);

        if (value.TryGetBytes(out var directBytes) && directBytes.Length > 0)
        {
            asset.Data = directBytes;
            return true;
        }

        var map = ExtractFirstMap(value);
        if (map == null)
            return false;

        if (map.TryGetValue("type", out var typeValue) &&
            typeValue.AsString() is { Length: > 0 } type &&
            !IsCompatibleMediaType(type, expectedMediaType))
        {
            return false;
        }

        asset.Uri = ReadString(map, "uri") ?? "";
        asset.AssetId = ReadString(map, "asset_id");
        asset.TempId = ReadString(map, "temp_id");

        if (map.TryGetValue("data", out var data) &&
            TryGetBytes(data, out var bytes))
        {
            asset.Data = bytes;
        }
        else if (TryDecodeBase64OrDataUri(asset.Uri, out bytes))
        {
            asset.Data = bytes;
        }

        if (asset is ImageRef image)
        {
            image.MimeType =
                ReadString(map, "mime_type") ??
                ReadString(map, "mimeType") ??
                ReadDataUriContentType(asset.Uri);
        }

        return !asset.IsEmpty();
    }

    public static bool TryGetBytes(AssetRef asset, out byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(asset);
        bytes = asset.Data switch
        {
            byte[] array => array,
            ReadOnlyMemory<byte> memory => memory.ToArray(),
            Memory<byte> memory => memory.ToArray(),
            string encoded when TryDecodeBase64OrDataUri(encoded, out var decoded)
                => decoded,
            _ => Array.Empty<byte>()
        };
        return bytes.Length > 0;
    }

    private static IReadOnlyDictionary<string, NodeToolValue>? ExtractFirstMap(
        NodeToolValue value)
    {
        if (value.Kind == NodeToolValueKind.Map)
            return value.AsMapOrEmpty();
        if (value.Kind != NodeToolValueKind.List)
            return null;

        foreach (var item in value.AsListOrEmpty())
        {
            if (item.Kind == NodeToolValueKind.Map)
                return item.AsMapOrEmpty();
        }
        return null;
    }

    private static bool TryGetBytes(NodeToolValue value, out byte[] bytes)
    {
        if (value.TryGetBytes(out bytes) && bytes.Length > 0)
            return true;

        if (value.Kind == NodeToolValueKind.List)
        {
            var values = value.AsListOrEmpty();
            var buffer = new byte[values.Count];
            for (var index = 0; index < values.Count; index++)
            {
                if (!values[index].TryGetLong(out var item) ||
                    item is < byte.MinValue or > byte.MaxValue)
                {
                    bytes = Array.Empty<byte>();
                    return false;
                }
                buffer[index] = (byte)item;
            }
            bytes = buffer;
            return bytes.Length > 0;
        }

        return value.AsString() is { } encoded &&
               TryDecodeBase64OrDataUri(encoded, out bytes);
    }

    private static bool TryDecodeBase64OrDataUri(
        string? value,
        out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        var encoded = value?.Trim() ?? "";
        if (encoded.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = encoded.IndexOf(',');
            if (comma < 0 ||
                encoded.AsSpan(0, comma).IndexOf(
                    ";base64",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }
            encoded = encoded[(comma + 1)..];
        }

        try
        {
            bytes = Convert.FromBase64String(encoded);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, NodeToolValue> map,
        string key)
        => map.TryGetValue(key, out var value) &&
           value.AsString() is { Length: > 0 } text
            ? text
            : null;

    private static string? ReadDataUriContentType(string uri)
    {
        if (!uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;
        var separator = uri.IndexOfAny([';', ',']);
        return separator > 5 ? uri[5..separator] : null;
    }

    private static bool IsCompatibleMediaType(
        string actual,
        string expected)
        => NormalizeMediaType(actual) == expected;

    private static string NormalizeMediaType(string value)
        => value.Trim().ToLowerInvariant() switch
        {
            "imageref" or "image_ref" => "image",
            "audioref" or "audio_ref" => "audio",
            "videoref" or "video_ref" => "video",
            "assetref" or "asset_ref" => "asset",
            var normalized => normalized
        };

    private static AssetRef CreateAsset(string mediaType)
        => mediaType switch
        {
            "image" => new ImageRef(),
            "audio" => new AudioRef(),
            "video" => new VideoRef(),
            "text" => new TextRef(),
            "document" => new DocumentRef(),
            "model_3d" => new Model3DRef(),
            "font" => new FontRef(),
            _ => new GenericAssetRef()
        };
}
