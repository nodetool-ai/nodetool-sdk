using System;
using System.Collections;
using System.Reflection;
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
        if (DynamicWorkflowEnumFactory.TryToWireValue(value, out var wireValue))
            return wireValue ?? "";

        return NodeToolValueConverter.NormalizeForTransport(value) ?? "";
    }

    /// <summary>
    /// Replaces VL dynamic-enum values with their NodeTool wire values while
    /// preserving host-native media objects for the media input adapter.
    /// </summary>
    public static object? NormalizeDynamicEnumsForTransport(object? value)
    {
        if (DynamicWorkflowEnumFactory.TryToWireValue(value, out var wireValue))
            return wireValue;

        if (value is IDictionary dictionary)
        {
            var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = Convert.ToString(
                    entry.Key,
                    System.Globalization.CultureInfo.InvariantCulture) ?? "";
                normalized[key] = NormalizeDynamicEnumsForTransport(entry.Value);
            }
            return normalized;
        }

        if (value is IEnumerable enumerable and not string and not byte[])
        {
            return enumerable
                .Cast<object?>()
                .Select(NormalizeDynamicEnumsForTransport)
                .ToArray();
        }

        return value;
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

        // VL dynamic enums are reference types implementing IDynamicEnum, not
        // CLR enum types, so Type.IsEnum must not gate this conversion.
        if (DynamicWorkflowEnumFactory.TryFromWireValue(
                targetType,
                value,
                out var enumValue))
        {
            return enumValue;
        }

        if (targetType == typeof(VlPath))
        {
            return NodeToolValueConverter.TryConvert(
                value,
                typeof(string),
                out var path,
                out _)
                ? new VlPath(path as string ?? "")
                : fallback;
        }

        if (IsSpreadType(targetType) &&
            TryGetCollectionElementType(targetType, out var elementType))
        {
            var normalized = NodeToolValueConverter.Convert(value, typeof(object));
            var items = normalized is IEnumerable enumerable and not string
                ? enumerable.Cast<object?>().ToArray()
                : new[] { normalized };
            var elementFallback = GetDefaultValue(elementType);
            var convertedItems = items
                .Select(item => ConvertOrFallback(item, elementType, elementFallback))
                .ToArray();
            return CreateCollection(targetType, elementType, convertedItems);
        }

        return NodeToolValueConverter.TryConvert(
            value,
            targetType,
            out var converted,
            out _)
            ? converted
            : fallback;
    }

    private static object? GetDefaultValue(Type type)
        => type.IsValueType ? Activator.CreateInstance(type) : null;
}


