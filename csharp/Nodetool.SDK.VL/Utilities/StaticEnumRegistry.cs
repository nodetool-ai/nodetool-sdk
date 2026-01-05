using System;
using System.Collections.Generic;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.VL.Utilities;

internal static partial class StaticEnumRegistry
{
    public static bool TryGetEnumType(string? typeName, out Type enumType)
    {
        enumType = null!;
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        return TryGetEnumTypeByName(typeName.Trim(), out enumType);
    }

    public static bool TryGetEnumType(string? typeName, IReadOnlyList<object>? values, out Type enumType)
    {
        // We intentionally do not validate values here: static enums are generated from package metadata,
        // and value lists should match. If they don't, we still prefer a working dropdown over fallback.
        return TryGetEnumType(typeName, out enumType);
    }

    public static bool TryGetEnumType(TypeMetadata tm, out Type enumType)
        => TryGetEnumType(tm.TypeName, tm.Values, out enumType);

    public static object GetDefaultValue(Type enumType) => Enum.ToObject(enumType, 0);

    public static bool TryToNodeToolLiteral(Enum value, out object? literal)
    {
        literal = null;
        if (!TryGetEnumInfo(value.GetType(), out var info))
            return false;

        var idx = Convert.ToInt32(value);
        if (info.ValueByIndex.TryGetValue(idx, out var lit))
        {
            literal = lit;
            return true;
        }

        return false;
    }

    public static object ToNodeToolLiteral(Enum value)
        => TryToNodeToolLiteral(value, out var lit) ? (lit ?? "") : value.ToString();

    public static bool TryFromNodeToolLiteral(Type enumType, object? literal, out object? enumValue)
    {
        enumValue = null;
        if (!TryGetEnumInfo(enumType, out var info))
            return false;

        if (literal is null)
        {
            enumValue = Enum.ToObject(enumType, 0);
            return true;
        }

        var key = literal.ToString() ?? "";
        if (info.IndexByValue.TryGetValue(key, out var idx))
        {
            enumValue = Enum.ToObject(enumType, idx);
            return true;
        }

        // Fallback: case-insensitive match (only on miss; keeps IndexByValue fast & stable).
        foreach (var kvp in info.IndexByValue)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                enumValue = Enum.ToObject(enumType, kvp.Value);
                return true;
            }
        }

        // Numeric fallback: allow passing raw indices.
        if (int.TryParse(key, out var parsedIdx) && info.ValueByIndex.ContainsKey(parsedIdx))
        {
            enumValue = Enum.ToObject(enumType, parsedIdx);
            return true;
        }

        enumValue = Enum.ToObject(enumType, 0);
        return true;
    }
}


