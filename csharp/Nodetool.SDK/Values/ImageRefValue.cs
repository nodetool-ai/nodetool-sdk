using System.Globalization;

namespace Nodetool.SDK.Values;

/// <summary>
/// Helper for working with NodeTool ImageRef-shaped values.
///
/// Expected shapes:
/// - { type:"image", uri?:string|null, asset_id?:string|null, data?:byte[]|number[], metadata?:{...} }
/// - [ { ...ImageRef... } ] (common for workflow outputs)
/// </summary>
public sealed record ImageRefValue(
    byte[]? Data,
    string? Uri,
    string? AssetId,
    IReadOnlyDictionary<string, NodeToolValue> Metadata
)
{
    public bool HasData => Data != null && Data.Length > 0;

    public static bool TryParse(NodeToolValue value, out ImageRefValue? image)
    {
        image = null;

        // Common: workflow outputs may wrap a single image in a list.
        if (value.Kind == NodeToolValueKind.List)
        {
            var firstMap = value.AsListOrEmpty().FirstOrDefault(v => v.Kind == NodeToolValueKind.Map);
            if (firstMap != null)
                value = firstMap;
        }

        if (value.Kind != NodeToolValueKind.Map)
            return false;

        var map = value.AsMapOrEmpty();

        if (!map.TryGetValue("type", out var typeVal))
            return false;

        var typeStr = typeVal.AsString();
        if (!string.Equals(typeStr, "image", StringComparison.OrdinalIgnoreCase))
            return false;

        string? uri = map.TryGetValue("uri", out var uriVal) ? uriVal.AsString() : null;
        string? assetId = map.TryGetValue("asset_id", out var assetIdVal) ? assetIdVal.AsString() : null;

        byte[]? data = null;
        if (map.TryGetValue("data", out var dataVal))
        {
            // Fast path: MessagePack often deserializes binary as byte[].
            if (dataVal.TryGetBytes(out var bytes))
            {
                data = bytes;
            }
            else if (dataVal.Kind == NodeToolValueKind.List)
            {
                // JSON-ish fallback: a list of numbers.
                var list = dataVal.AsListOrEmpty();
                var tmp = new byte[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].TryGetLong(out var l))
                        tmp[i] = unchecked((byte)l);
                    else
                    {
                        // last resort: try convert
                        tmp[i] = Convert.ToByte(list[i].Raw, CultureInfo.InvariantCulture);
                    }
                }
                data = tmp;
            }
        }

        var metadata = new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
        if (map.TryGetValue("metadata", out var metaVal) && metaVal.Kind == NodeToolValueKind.Map)
        {
            foreach (var kvp in metaVal.AsMapOrEmpty())
                metadata[kvp.Key] = kvp.Value;
        }

        image = new ImageRefValue(data, uri, assetId, metadata);
        return true;
    }
}


