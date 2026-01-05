using System;
using System.Globalization;
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

            if (targetType == typeof(string[]))
            {
                if (value is Array array)
                {
                    var stringArray = new string[array.Length];
                    for (int i = 0; i < array.Length; i++)
                        stringArray[i] = array.GetValue(i)?.ToString() ?? "";
                    return stringArray;
                }
                return new[] { value.ToString() ?? "" };
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

        if (targetType == typeof(string[]))
        {
            if (je.ValueKind == JsonValueKind.Array)
            {
                var list = new string[je.GetArrayLength()];
                var idx = 0;
                foreach (var item in je.EnumerateArray())
                    list[idx++] = item.ValueKind == JsonValueKind.String ? (item.GetString() ?? "") : (item.ToString() ?? "");
                converted = list;
                return true;
            }
            return false;
        }

        // Fallback: allow assigning a string representation into object pins, etc.
        if (targetType == typeof(object))
        {
            converted = je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Number when je.TryGetDouble(out var d) => d,
                JsonValueKind.True or JsonValueKind.False => je.GetBoolean(),
                _ => je.ToString()
            };
            return true;
        }

        return false;
    }
}


