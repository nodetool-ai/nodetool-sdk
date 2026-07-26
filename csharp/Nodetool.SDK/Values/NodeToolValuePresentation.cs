using System.Text;

namespace Nodetool.SDK.Values;

/// <summary>
/// Presents transport values in a stable host-neutral form. UI adapters can
/// apply their own native pin and collection types after this policy has
/// handled NodeTool's typed text and chunk payload shapes.
/// </summary>
public static class NodeToolValuePresentation
{
    private static readonly string[] PreferredTextFields =
    [
        "uri",
        "asset_id",
        "text",
        "content",
        "delta",
        "chunk",
        "value",
        "result"
    ];

    public static string ToDisplayString(NodeToolValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (TryRenderTypedText(value, out var rendered))
            return rendered;

        if (value.Kind == NodeToolValueKind.Map)
        {
            var map = value.AsMapOrEmpty();
            foreach (var field in PreferredTextFields)
            {
                if (map.TryGetValue(field, out var fieldValue))
                {
                    if (fieldValue.AsString() is { } fieldText)
                    {
                        if (!string.IsNullOrWhiteSpace(fieldText))
                            return fieldText;
                        continue;
                    }
                    return fieldValue.ToJsonString();
                }
            }
            return value.ToJsonString();
        }

        if (value.Kind == NodeToolValueKind.List)
        {
            if (TryConcatChunkList(value, out var text))
                return text;

            var list = value.AsListOrEmpty();
            if (list.Count == 1 &&
                TryGetStringValue(list[0], out var singleton))
            {
                return singleton;
            }
            return value.ToJsonString();
        }

        return value.AsString() ?? value.ToJsonString();
    }

    public static object? ToDisplayObject(NodeToolValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Kind == NodeToolValueKind.Map)
        {
            var map = value.AsMapOrEmpty();
            foreach (var field in new[] { "text", "delta", "content", "chunk" })
            {
                if (map.TryGetValue(field, out var fieldValue))
                    return fieldValue.AsString() ?? fieldValue.ToJsonString();
            }
            return value.ToJsonString();
        }
        return value.Kind == NodeToolValueKind.List
            ? value.ToJsonString()
            : value.Raw;
    }

    private static bool TryRenderTypedText(
        NodeToolValue value,
        out string text)
    {
        ArgumentNullException.ThrowIfNull(value);
        text = "";
        if (value.Kind != NodeToolValueKind.Map)
            return false;

        var map = value.AsMapOrEmpty();
        if (!map.TryGetValue("type", out var typeValue))
            return false;

        var type = typeValue.AsString();
        if (string.Equals(type, "string", StringComparison.OrdinalIgnoreCase) &&
            map.TryGetValue("value", out var stringValue))
        {
            text = stringValue.AsString() ?? stringValue.ToJsonString();
            return true;
        }

        if (string.Equals(type, "list", StringComparison.OrdinalIgnoreCase) &&
            map.TryGetValue("value", out var listValue))
        {
            if (TryConcatChunkList(listValue, out text))
                return true;

            if (listValue.Kind == NodeToolValueKind.List)
            {
                var items = listValue.AsListOrEmpty();
                var strings = new List<string>(items.Count);
                foreach (var item in items)
                {
                    if (!TryGetStringValue(item, out var itemText))
                        return false;
                    strings.Add(itemText);
                }
                if (strings.Count > 0)
                {
                    text = strings.Count == 1
                        ? strings[0]
                        : string.Join("\n", strings);
                    return true;
                }
            }
        }

        if (string.Equals(type, "chunk", StringComparison.OrdinalIgnoreCase) &&
            map.TryGetValue("content", out var content))
        {
            text = content.AsString() ?? "";
            return true;
        }

        return false;
    }

    private static bool TryConcatChunkList(
        NodeToolValue value,
        out string text)
    {
        ArgumentNullException.ThrowIfNull(value);
        text = "";

        IReadOnlyList<NodeToolValue> list;
        if (value.Kind == NodeToolValueKind.List)
        {
            list = value.AsListOrEmpty();
        }
        else if (value.Kind == NodeToolValueKind.Map)
        {
            var map = value.AsMapOrEmpty();
            if (!map.TryGetValue("type", out var type) ||
                !string.Equals(
                    type.AsString(),
                    "list",
                    StringComparison.OrdinalIgnoreCase) ||
                !map.TryGetValue("value", out var listValue) ||
                listValue.Kind != NodeToolValueKind.List)
            {
                return false;
            }
            list = listValue.AsListOrEmpty();
        }
        else
        {
            return false;
        }

        var builder = new StringBuilder();
        var sawChunk = false;
        foreach (var item in list)
        {
            if (item.Kind != NodeToolValueKind.Map ||
                !string.Equals(
                    item.TypeDiscriminator,
                    "chunk",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            sawChunk = true;
            var map = item.AsMapOrEmpty();
            if (map.TryGetValue("content", out var content) &&
                content.AsString() is { } chunk)
            {
                builder.Append(chunk);
            }
        }

        if (!sawChunk)
            return false;
        text = builder.ToString();
        return true;
    }

    private static bool TryGetStringValue(
        NodeToolValue value,
        out string text)
    {
        if (value.Kind == NodeToolValueKind.String &&
            value.AsString() is { } direct)
        {
            text = direct;
            return true;
        }

        if (value.Kind == NodeToolValueKind.Map)
        {
            var map = value.AsMapOrEmpty();
            if (map.TryGetValue("type", out var type) &&
                string.Equals(
                    type.AsString(),
                    "string",
                    StringComparison.OrdinalIgnoreCase) &&
                map.TryGetValue("value", out var inner))
            {
                text = inner.AsString() ?? inner.ToJsonString();
                return true;
            }
        }

        text = "";
        return false;
    }
}
