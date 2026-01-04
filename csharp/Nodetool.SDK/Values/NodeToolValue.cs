using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Nodetool.SDK.Values;

public enum NodeToolValueKind
{
    Null,
    Boolean,
    Integer,
    Float,
    String,
    Bytes,
    List,
    Map,
    Other
}

/// <summary>
/// Safe, universal value tree for NodeTool payloads carried over MessagePack.
///
/// This exists because key WS payload fields are open-ended (unknown/any):
/// - output_update.value
/// - preview_update.value
/// - node_update.result (free-form object)
///
/// The goal is: never throw on inspection, provide best-effort conversions,
/// and keep access to the raw underlying value.
/// </summary>
public sealed class NodeToolValue
{
    private NodeToolValue(NodeToolValueKind kind, object? raw, IReadOnlyList<NodeToolValue>? list, IReadOnlyDictionary<string, NodeToolValue>? map)
    {
        Kind = kind;
        Raw = raw;
        _list = list;
        _map = map;
    }

    private readonly IReadOnlyList<NodeToolValue>? _list;
    private readonly IReadOnlyDictionary<string, NodeToolValue>? _map;

    public NodeToolValueKind Kind { get; }
    public object? Raw { get; }

    /// <summary>
    /// If this is a map and contains a "type" discriminator, returns it.
    /// </summary>
    public string? TypeDiscriminator
    {
        get
        {
            if (_map != null && _map.TryGetValue("type", out var t))
            {
                return t.AsString();
            }
            return null;
        }
    }

    public static NodeToolValue From(object? value)
    {
        if (value == null) return new NodeToolValue(NodeToolValueKind.Null, null, null, null);

        switch (value)
        {
            case NodeToolValue v:
                return v;
            case bool b:
                return new NodeToolValue(NodeToolValueKind.Boolean, b, null, null);
            case string s:
                return new NodeToolValue(NodeToolValueKind.String, s, null, null);
            case byte[] bytes:
                return new NodeToolValue(NodeToolValueKind.Bytes, bytes, null, null);
            case ReadOnlyMemory<byte> rom:
                return new NodeToolValue(NodeToolValueKind.Bytes, rom.ToArray(), null, null);
            case Memory<byte> mem:
                return new NodeToolValue(NodeToolValueKind.Bytes, mem.ToArray(), null, null);
        }

        if (IsInteger(value))
        {
            return new NodeToolValue(NodeToolValueKind.Integer, value, null, null);
        }
        if (IsFloat(value))
        {
            return new NodeToolValue(NodeToolValueKind.Float, value, null, null);
        }

        // Map/dict normalization (MessagePack often yields IDictionary with object keys)
        if (value is IDictionary dict)
        {
            var map = new Dictionary<string, NodeToolValue>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key switch
                {
                    null => "null",
                    string ks => ks,
                    _ => Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? entry.Key.ToString() ?? "key"
                };
                map[key] = From(entry.Value);
            }
            return new NodeToolValue(NodeToolValueKind.Map, value, null, map);
        }

        // List/array normalization
        if (value is IEnumerable enumerable && value is not string)
        {
            var list = new List<NodeToolValue>();
            foreach (var item in enumerable)
            {
                list.Add(From(item));
            }
            return new NodeToolValue(NodeToolValueKind.List, value, list, null);
        }

        return new NodeToolValue(NodeToolValueKind.Other, value, null, null);
    }

    public IReadOnlyList<NodeToolValue> AsListOrEmpty() => _list ?? Array.Empty<NodeToolValue>();
    public IReadOnlyDictionary<string, NodeToolValue> AsMapOrEmpty() => _map ?? new Dictionary<string, NodeToolValue>();

    public string? AsString()
    {
        return Raw switch
        {
            null => null,
            string s => s,
            bool b => b ? "true" : "false",
            _ => Convert.ToString(Raw, CultureInfo.InvariantCulture)
        };
    }

    public bool TryGetBool(out bool value)
    {
        if (Raw is bool b)
        {
            value = b;
            return true;
        }
        if (Raw is string s && bool.TryParse(s, out var parsed))
        {
            value = parsed;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetLong(out long value)
    {
        try
        {
            if (Raw == null)
            {
                value = default;
                return false;
            }
            if (Raw is long l)
            {
                value = l;
                return true;
            }
            if (Raw is int i)
            {
                value = i;
                return true;
            }
            if (Raw is string s && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = Convert.ToInt64(Raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public bool TryGetDouble(out double value)
    {
        try
        {
            if (Raw == null)
            {
                value = default;
                return false;
            }
            if (Raw is double d)
            {
                value = d;
                return true;
            }
            if (Raw is float f)
            {
                value = f;
                return true;
            }
            if (Raw is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
            value = Convert.ToDouble(Raw, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public bool TryGetBytes(out byte[] bytes)
    {
        if (Raw is byte[] b)
        {
            bytes = b;
            return true;
        }
        bytes = Array.Empty<byte>();
        return false;
    }

    public string ToJsonString()
    {
        try
        {
            var normalized = ToPlainObject();
            return JsonSerializer.Serialize(normalized, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return AsString() ?? "";
        }
    }

    private object? ToPlainObject()
    {
        return Kind switch
        {
            NodeToolValueKind.Null => null,
            NodeToolValueKind.Boolean or NodeToolValueKind.Integer or NodeToolValueKind.Float or NodeToolValueKind.String => Raw,
            NodeToolValueKind.Bytes => Raw is byte[] b ? Convert.ToBase64String(b) : Raw,
            NodeToolValueKind.List => AsListOrEmpty().Select(v => v.ToPlainObject()).ToList(),
            NodeToolValueKind.Map => AsMapOrEmpty().ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToPlainObject()),
            _ => Raw?.ToString()
        };
    }

    private static bool IsInteger(object v) =>
        v is sbyte or byte or short or ushort or int or uint or long or ulong;

    private static bool IsFloat(object v) =>
        v is float or double or decimal;
}


