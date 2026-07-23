using System;
using System.Globalization;
using System.Collections;
using System.Text.Json;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlValueConversion
{
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

            if (targetType == typeof(int))
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return Convert.ToSingle(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture);

            if (targetType.IsArray && targetType.GetElementType() is Type elementType)
            {
                if (value is IEnumerable enumerable and not string)
                {
                    var items = enumerable.Cast<object?>().ToArray();
                    var convertedArray = Array.CreateInstance(elementType, items.Length);
                    var elementFallback = GetDefaultValue(elementType);
                    for (var i = 0; i < items.Length; i++)
                        convertedArray.SetValue(ConvertOrFallback(items[i], elementType, elementFallback), i);
                    return convertedArray;
                }

                var single = Array.CreateInstance(elementType, 1);
                single.SetValue(ConvertOrFallback(value, elementType, GetDefaultValue(elementType)), 0);
                return single;
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

        if (targetType.IsArray && targetType.GetElementType() is Type elementType)
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                var list = Array.CreateInstance(elementType, je.GetArrayLength());
                var idx = 0;
                foreach (var item in je.EnumerateArray())
                {
                    if (!TryConvertJsonElement(item, elementType, out var itemValue))
                        itemValue = GetDefaultValue(elementType);
                    list.SetValue(itemValue, idx++);
                }
                converted = list;
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


