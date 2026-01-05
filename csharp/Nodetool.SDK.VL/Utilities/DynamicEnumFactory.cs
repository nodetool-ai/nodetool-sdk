using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;

namespace Nodetool.SDK.VL.Utilities;

/// <summary>
/// Creates CLR enum types at runtime (for VL dropdown pins) and provides conversion helpers
/// between NodeTool enum payloads (typically string/int values) and CLR enum values.
/// </summary>
internal static class DynamicEnumFactory
{
    private sealed record EnumMapping(
        Type EnumType,
        IReadOnlyList<object> OriginalValues,
        IReadOnlyDictionary<string, int> IndexByNormalizedValue
    );

    private static readonly ConcurrentDictionary<string, EnumMapping> Cache = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, EnumMapping> ByType = new();

    private static readonly object EmitLock = new();
    private static AssemblyBuilder? _assemblyBuilder;
    private static ModuleBuilder? _moduleBuilder;

    public static bool IsDynamicEnum(Type t) => ByType.ContainsKey(t);

    public static Type? GetOrCreateEnumType(string? typeName, IReadOnlyList<object>? values, string fallbackName)
    {
        if (values == null || values.Count == 0)
            return null;

        var safeTypeName = SanitizeIdentifier(!string.IsNullOrWhiteSpace(typeName) ? typeName! : fallbackName);
        if (string.IsNullOrWhiteSpace(safeTypeName))
            safeTypeName = "NodeToolEnum";

        var normalizedValues = NormalizeValues(values);
        var key = $"{safeTypeName}:{string.Join("|", normalizedValues.Select(NormalizeKeyFragment))}";

        if (Cache.TryGetValue(key, out var cached))
            return cached.EnumType;

        lock (EmitLock)
        {
            if (Cache.TryGetValue(key, out cached))
                return cached.EnumType;

            EnsureModule();

            var enumFullName = $"Nodetool.SDK.VL.DynamicEnums.{safeTypeName}_{StableHash(normalizedValues)}";

            // Define an int-backed enum (VL uses CLR enum type for dropdown rendering).
            var eb = _moduleBuilder!.DefineEnum(enumFullName, TypeAttributes.Public, typeof(int));

            var indexByNorm = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < normalizedValues.Count; i++)
            {
                var original = normalizedValues[i];
                var memberName = MakeEnumMemberName(original, i);
                eb.DefineLiteral(memberName, i);

                var norm = NormalizeKeyFragment(original);
                if (!indexByNorm.ContainsKey(norm))
                    indexByNorm[norm] = i;
            }

            var enumType = eb.CreateTypeInfo()!.AsType();
            var mapping = new EnumMapping(enumType, normalizedValues, indexByNorm);

            Cache[key] = mapping;
            ByType[enumType] = mapping;
            return enumType;
        }
    }

    public static object GetDefaultValue(Type enumType)
    {
        // Default to the first entry.
        return Enum.ToObject(enumType, 0);
    }

    /// <summary>
    /// Convert a CLR enum value into the original NodeTool enum literal (string/int) for transport.
    /// </summary>
    public static bool TryToNodeToolLiteral(object enumValue, out object? nodeToolLiteral)
    {
        nodeToolLiteral = null;
        if (enumValue == null)
            return false;

        var t = enumValue.GetType();
        if (!ByType.TryGetValue(t, out var mapping))
            return false;

        try
        {
            var idx = Convert.ToInt32(enumValue, CultureInfo.InvariantCulture);
            if (idx < 0 || idx >= mapping.OriginalValues.Count)
                return false;
            nodeToolLiteral = mapping.OriginalValues[idx];
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Convert a NodeTool enum literal (typically string/int) into a CLR enum value of the specified enum type.
    /// </summary>
    public static bool TryFromNodeToolLiteral(Type enumType, object? nodeToolLiteral, out object? enumValue)
    {
        enumValue = null;

        if (!ByType.TryGetValue(enumType, out var mapping))
            return false;

        var normalized = NormalizeKeyFragment(NormalizeValue(nodeToolLiteral));
        if (mapping.IndexByNormalizedValue.TryGetValue(normalized, out var idx))
        {
            enumValue = Enum.ToObject(enumType, idx);
            return true;
        }

        // Best-effort: accept numeric index
        if (TryConvertToInt(nodeToolLiteral, out var intIdx) &&
            intIdx >= 0 &&
            intIdx < mapping.OriginalValues.Count)
        {
            enumValue = Enum.ToObject(enumType, intIdx);
            return true;
        }

        // Default to first.
        enumValue = Enum.ToObject(enumType, 0);
        return true;
    }

    private static void EnsureModule()
    {
        if (_moduleBuilder != null)
            return;

        var name = new AssemblyName("Nodetool.SDK.VL.DynamicEnums");
        _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        _moduleBuilder = _assemblyBuilder.DefineDynamicModule(name.Name!);
    }

    private static List<object> NormalizeValues(IReadOnlyList<object> values)
    {
        var list = new List<object>(values.Count);
        foreach (var v in values)
            list.Add(NormalizeValue(v) ?? "");
        return list;
    }

    private static object? NormalizeValue(object? v)
    {
        if (v == null)
            return null;

        if (v is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.String => je.GetString() ?? "",
                JsonValueKind.Number when je.TryGetInt32(out var i) => i,
                JsonValueKind.Number when je.TryGetInt64(out var l) => l,
                JsonValueKind.Number when je.TryGetDouble(out var d) => d,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => je.ToString()
            };
        }

        return v;
    }

    private static bool TryConvertToInt(object? v, out int i)
    {
        i = 0;
        if (v == null)
            return false;

        try
        {
            if (v is int ii)
            {
                i = ii;
                return true;
            }
            if (v is long l)
            {
                i = (int)l;
                return true;
            }
            if (v is string s && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                i = parsed;
                return true;
            }
            if (v is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var jeInt))
            {
                i = jeInt;
                return true;
            }
        }
        catch
        {
            // ignore
        }
        return false;
    }

    private static string MakeEnumMemberName(object original, int index)
    {
        var s = original?.ToString() ?? "";
        s = s.Trim();
        if (string.IsNullOrWhiteSpace(s))
            return $"Value_{index:D2}";

        // Favor readable member names, but keep deterministic.
        var candidate = SanitizeIdentifier(s);
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = $"Value_{index:D2}";

        // Enums cannot start with a digit.
        if (char.IsDigit(candidate[0]))
            candidate = "V_" + candidate;

        return candidate;
    }

    private static string SanitizeIdentifier(string s)
    {
        var sb = new StringBuilder(s.Length);
        var prevUnderscore = false;
        foreach (var ch in s)
        {
            var ok = char.IsLetterOrDigit(ch) || ch == '_';
            var c = ok ? ch : '_';
            if (c == '_')
            {
                if (prevUnderscore)
                    continue;
                prevUnderscore = true;
            }
            else
            {
                prevUnderscore = false;
            }
            sb.Append(c);
        }
        return sb.ToString().Trim('_');
    }

    private static string NormalizeKeyFragment(object? v)
    {
        if (v == null)
            return "null";
        return Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim() ?? "null";
    }

    private static string StableHash(IReadOnlyList<object> values)
    {
        unchecked
        {
            var hash = 17;
            foreach (var v in values)
                hash = (hash * 31) + NormalizeKeyFragment(v).GetHashCode(StringComparison.Ordinal);
            return Math.Abs(hash).ToString(CultureInfo.InvariantCulture);
        }
    }
}


