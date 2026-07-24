using System;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Text.Json;
using Nodetool.SDK.Types.Assets;
using Nodetool.SDK.Values;
using VL.Lib.Collections;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlValueConversion
{
    private static readonly MethodInfo CreateSpreadFromArrayMethod = typeof(Spread)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method =>
            method.Name == nameof(Spread.Create) &&
            method.IsGenericMethodDefinition &&
            method.GetParameters() is [{ ParameterType.IsArray: true }]);

    public static bool IsSpreadType(Type type)
        => type.IsGenericType &&
           type.GetGenericTypeDefinition() == typeof(Spread<>);

    public static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type.IsArray && type.GetElementType() is { } arrayElementType)
        {
            elementType = arrayElementType;
            return true;
        }

        if (IsSpreadType(type))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = typeof(object);
        return false;
    }

    public static object CreateEmptySpread(Type elementType)
        => CreateSpread(elementType, Array.Empty<object?>());

    public static object CreateCollection(
        Type collectionType,
        Type elementType,
        IReadOnlyList<object?> items)
    {
        var array = Array.CreateInstance(elementType, items.Count);
        for (var i = 0; i < items.Count; i++)
            array.SetValue(items[i], i);

        return collectionType.IsArray
            ? array
            : CreateSpreadFromArrayMethod.MakeGenericMethod(elementType)
                .Invoke(null, [array])
                ?? throw new InvalidOperationException($"Could not create {collectionType}.");
    }

    public static object NormalizeForTransport(object value)
    {
        if (value is VlPath path)
            return path.ToString();

        if (value is AssetRef assetReference)
            return assetReference.ToDict();

        if (!IsSpreadType(value.GetType()) || value is not IEnumerable enumerable)
            return value;

        var elementType = value.GetType().GetGenericArguments()[0];
        var items = enumerable
            .Cast<object?>()
            .Select(item => item == null ? null : NormalizeForTransport(item))
            .ToArray();
        return items.All(item => item == null || elementType.IsInstanceOfType(item))
            ? CreateCollection(elementType.MakeArrayType(), elementType, items)
            : items;
    }

    public static NodeToolValue UnwrapTerminalResultEnvelope(NodeToolValue value)
    {
        if (value.Kind != NodeToolValueKind.List)
            return value;

        var values = value.AsListOrEmpty();
        return values.Count == 1 ? values[0] : value;
    }

    public static AssetRef ConvertNodeToolValueToAssetRef(
        NodeToolValue value,
        Type expectedType)
    {
        if (!typeof(AssetRef).IsAssignableFrom(expectedType))
            throw new ArgumentException(
                $"{expectedType.Name} is not an asset-reference type.",
                nameof(expectedType));

        if (value.Kind == NodeToolValueKind.List &&
            value.AsListOrEmpty() is { Count: 1 } singleton)
        {
            value = singleton[0];
        }

        var result = (AssetRef)(Activator.CreateInstance(expectedType)
            ?? throw new InvalidOperationException(
                $"Cannot create asset reference type {expectedType.Name}."));

        if (value.Kind == NodeToolValueKind.String)
        {
            result.Uri = value.AsString() ?? "";
            return result;
        }

        if (value.Kind != NodeToolValueKind.Map)
            return result;

        var map = value.AsMapOrEmpty();
        if (map.TryGetValue("uri", out var uri))
            result.Uri = uri.AsString() ?? "";
        if (string.IsNullOrWhiteSpace(result.Uri) &&
            map.TryGetValue("get_url", out var getUrl))
        {
            result.Uri = getUrl.AsString() ?? "";
        }
        if (map.TryGetValue("asset_id", out var assetId))
            result.AssetId = assetId.AsString();
        if (map.TryGetValue("temp_id", out var tempId))
            result.TempId = tempId.AsString();
        if (map.TryGetValue("data", out var data))
            result.Data = ConvertAssetData(data);
        if (map.TryGetValue("metadata", out var metadata) &&
            metadata.Kind == NodeToolValueKind.Map)
        {
            result.Metadata = metadata
                .AsMapOrEmpty()
                .ToDictionary(
                    item => item.Key,
                    item => ConvertNodeToolValueToPlainObject(item.Value),
                    StringComparer.Ordinal);
        }

        if (result is ImageRef image)
        {
            if (map.TryGetValue("mimeType", out var mimeType) ||
                map.TryGetValue("mime_type", out mimeType))
            {
                image.MimeType = mimeType.AsString();
            }
            if (map.TryGetValue("width", out var width) && width.TryGetLong(out var widthValue))
                image.Width = checked((int)widthValue);
            if (map.TryGetValue("height", out var height) && height.TryGetLong(out var heightValue))
                image.Height = checked((int)heightValue);
        }

        if (result is AudioRef audio &&
            map.TryGetValue("duration", out var audioDuration) &&
            audioDuration.TryGetDouble(out var audioSeconds))
        {
            audio.Duration = (float)audioSeconds;
        }

        if (result is VideoRef video)
        {
            if (map.TryGetValue("duration", out var duration) &&
                duration.TryGetDouble(out var seconds))
            {
                video.Duration = (float)seconds;
            }
            if (map.TryGetValue("format", out var format))
                video.Format = format.AsString();
        }

        return result;
    }

    private static object? ConvertAssetData(NodeToolValue data)
    {
        if (data.TryGetBytes(out var bytes))
            return bytes;

        if (data.Kind == NodeToolValueKind.List)
        {
            var values = data.AsListOrEmpty();
            var byteValues = new byte[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                if (!values[i].TryGetLong(out var value) || value is < 0 or > 255)
                    return ConvertNodeToolValueToPlainObject(data);
                byteValues[i] = (byte)value;
            }
            return byteValues;
        }

        return ConvertNodeToolValueToPlainObject(data);
    }

    private static object? ConvertNodeToolValueToPlainObject(NodeToolValue value)
        => value.Kind switch
        {
            NodeToolValueKind.Map => value
                .AsMapOrEmpty()
                .ToDictionary(
                    item => item.Key,
                    item => ConvertNodeToolValueToPlainObject(item.Value),
                    StringComparer.Ordinal),
            NodeToolValueKind.List => value
                .AsListOrEmpty()
                .Select(ConvertNodeToolValueToPlainObject)
                .ToArray(),
            _ => value.Raw
        };

    private static object CreateSpread(Type elementType, IReadOnlyList<object?> items)
        => CreateCollection(typeof(Spread<>).MakeGenericType(elementType), elementType, items);

    public static object? ConvertOrFallback(object? value, Type targetType, object? fallback)
    {
        if (value == null)
            return fallback;

        if (targetType.IsAssignableFrom(value.GetType()))
            return value;

        // System.Text.Json often deserializes "object" as JsonElement
        if (value is JsonElement je)
        {
            if (TryConvertJsonElement(je, targetType, out var converted))
                return converted;

            return fallback;
        }

        try
        {
            if (targetType == typeof(string))
                return value.ToString() ?? "";

            if (targetType == typeof(VlPath))
                return new VlPath(value.ToString() ?? "");

            if (targetType == typeof(int))
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

            if (TryGetCollectionElementType(targetType, out var elementType))
            {
                if (value is IEnumerable enumerable and not string)
                {
                    var items = enumerable.Cast<object?>().ToArray();
                    var elementFallback = GetDefaultValue(elementType);
                    var convertedItems = new object?[items.Length];
                    for (var i = 0; i < items.Length; i++)
                        convertedItems[i] = ConvertOrFallback(items[i], elementType, elementFallback);
                    return CreateCollection(targetType, elementType, convertedItems);
                }

                return CreateCollection(
                    targetType,
                    elementType,
                    [ConvertOrFallback(value, elementType, GetDefaultValue(elementType))]);
            }

            if (targetType.IsClass && value is IDictionary or IEnumerable)
            {
                var json = JsonSerializer.Serialize(value);
                return JsonSerializer.Deserialize(json, targetType, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? fallback;
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }

    private static bool TryConvertJsonElement(JsonElement je, Type targetType, out object? converted)
    {
        converted = null;

        if (targetType == typeof(string))
        {
            converted = je.ValueKind == JsonValueKind.String ? (je.GetString() ?? "") : (je.ToString() ?? "");
            return true;
        }

        if (targetType == typeof(VlPath))
        {
            converted = new VlPath(
                je.ValueKind == JsonValueKind.String
                    ? je.GetString() ?? ""
                    : je.ToString());
            return true;
        }

        if (targetType == typeof(bool))
        {
            if (je.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                converted = je.GetBoolean();
                return true;
            }

            if (je.ValueKind == JsonValueKind.String && bool.TryParse(je.GetString(), out var b))
            {
                converted = b;
                return true;
            }

            return false;
        }

        if (targetType == typeof(int))
        {
            if (je.ValueKind == JsonValueKind.Number)
            {
                if (je.TryGetInt32(out var i))
                {
                    converted = i;
                    return true;
                }
                if (je.TryGetDouble(out var d))
                {
                    converted = (int)d;
                    return true;
                }
            }

            if (je.ValueKind == JsonValueKind.String && int.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var istring))
            {
                converted = istring;
                return true;
            }

            return false;
        }

        if (targetType == typeof(float))
        {
            if (je.ValueKind == JsonValueKind.Number)
            {
                if (je.TryGetDouble(out var d))
                {
                    converted = (float)d;
                    return true;
                }
            }

            if (je.ValueKind == JsonValueKind.String && float.TryParse(je.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fstring))
            {
                converted = fstring;
                return true;
            }

            return false;
        }

        if (TryGetCollectionElementType(targetType, out var elementType))
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                var items = new object?[je.GetArrayLength()];
                var idx = 0;
                foreach (var item in je.EnumerateArray())
                {
                    if (!TryConvertJsonElement(item, elementType, out var itemValue))
                        itemValue = GetDefaultValue(elementType);
                    items[idx++] = itemValue;
                }
                converted = CreateCollection(targetType, elementType, items);
                return true;
            }
            return false;
        }

        // Preserve structured JSON for object fallback pins and MessagePack execution inputs.
        if (targetType == typeof(object))
        {
            converted = ConvertJsonElementToClr(je);
            return true;
        }

        return false;
    }

    private static object? ConvertJsonElementToClr(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertJsonElementToClr(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToClr).ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };
    }

    private static object? GetDefaultValue(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;
}


