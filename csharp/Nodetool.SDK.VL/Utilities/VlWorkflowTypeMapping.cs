using System;
using System.Globalization;
using System.Text.Json;
using Nodetool.SDK.Types;
using SkiaSharp;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlWorkflowTypeMapping
{
    public static (Type Type, object? DefaultValue) MapWorkflowType(
        string pinName,
        TypeMetadata typeMetadata,
        object? schemaDefaultValue)
    {
        var t = (typeMetadata.Type ?? "any").Trim().ToLowerInvariant();

        // Phase 2 rules: unknown/any should be ergonomic in VL, so default to string.
        if (t is "any" or "")
            return (typeof(string), "");

        if (t is "str" or "string" or "text")
            return (typeof(string), schemaDefaultValue as string ?? "");

        if (t is "int" or "integer")
            return (typeof(int), CoerceDefault<int>(schemaDefaultValue, 0));

        if (t is "float" or "number")
            return (typeof(float), CoerceDefault<float>(schemaDefaultValue, 0.0f));

        if (t is "bool" or "boolean")
            return (typeof(bool), CoerceDefault<bool>(schemaDefaultValue, false));

        if (t is "list" or "array")
            return (typeof(string[]), Array.Empty<string>());

        if (t == "image")
            return (typeof(SKImage), null!);

        if (t == "datetime")
            return (typeof(DateTime), default(DateTime));

        if (t == "enum" && typeMetadata.Values != null && typeMetadata.Values.Count > 0)
        {
            // Prefer stable identity from backend (type_name); fallback to pinName.
            var enumType = DynamicEnumFactory.GetOrCreateEnumType(
                typeMetadata.TypeName,
                typeMetadata.Values,
                fallbackName: $"Enum_{pinName}");

            if (enumType == null)
                return (typeof(string), "");

            // Default: schema default if provided; else first enum entry.
            if (TryParseEnumDefault(enumType, schemaDefaultValue, out var enumValue))
                return (enumType, enumValue);

            return (enumType, DynamicEnumFactory.GetDefaultValue(enumType));
        }

        // Fallback: keep it readable.
        return (typeof(string), schemaDefaultValue?.ToString() ?? "");
    }

    private static bool TryParseEnumDefault(Type enumType, object? schemaDefaultValue, out object? enumValue)
    {
        enumValue = null;
        if (schemaDefaultValue == null)
            return false;

        // Schema defaults may arrive as JsonElement.
        var literal = schemaDefaultValue is JsonElement je
            ? je.ValueKind == JsonValueKind.String ? (object?)(je.GetString() ?? "") : (object?)je.ToString()
            : schemaDefaultValue;

        return DynamicEnumFactory.TryFromNodeToolLiteral(enumType, literal, out enumValue);
    }

    private static T CoerceDefault<T>(object? value, T fallback) where T : struct
    {
        if (value == null)
            return fallback;

        try
        {
            if (value is T direct)
                return direct;

            if (value is JsonElement je)
            {
                if (typeof(T) == typeof(int) && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var i))
                    return (T)(object)i;
                if (typeof(T) == typeof(float) && je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var d))
                    return (T)(object)(float)d;
                if (typeof(T) == typeof(bool) && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False))
                    return (T)(object)je.GetBoolean();
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
        catch
        {
            return fallback;
        }
    }
}


