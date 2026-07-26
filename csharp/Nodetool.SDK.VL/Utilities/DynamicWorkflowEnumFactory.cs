using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VL.Core;
using VL.Lib.Collections;

namespace Nodetool.SDK.VL.Utilities;

/// <summary>
/// Assigns authoritative workflow option lists to compiler-visible vvvv
/// dynamic-enum types. Closed generic types are used instead of emitted CLR
/// enums, because VL's Roslyn platform cannot resolve Reflection.Emit modules.
/// </summary>
internal static class DynamicWorkflowEnumFactory
{
    private sealed record EnumMapping(
        string Identity,
        IReadOnlyDictionary<string, object> WireValueByEntry);

    private static readonly ConcurrentDictionary<Type, EnumMapping> ByType = new();
    private static readonly ConcurrentDictionary<Type, byte> ConfiguredTypes = new();
    private static readonly MethodInfo ConfigureSlotMethod = typeof(
            DynamicWorkflowEnumFactory)
        .GetMethod(
            nameof(ConfigureSlot),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    public static Type? GetOrCreate(
        string? typeName,
        IReadOnlyList<object>? values)
    {
        if (values is not { Count: > 0 })
            return null;

        var normalized = values.Select(NormalizeValue).ToArray();
        var identity = $"{typeName}\u001e{string.Join(
            "\u001f",
            normalized.Select(NormalizeKey))}";
        var markerType = CreateMarkerType(SHA256.HashData(
            Encoding.UTF8.GetBytes(identity)));
        var enumType = typeof(WorkflowDynamicEnum<>).MakeGenericType(markerType);
        var mapping = CreateMapping(identity, normalized);

        if (ByType.TryGetValue(enumType, out var existing))
        {
            if (!string.Equals(
                existing.Identity,
                identity,
                StringComparison.Ordinal))
            {
                return null;
            }
            EnsureConfigured(markerType, enumType, existing);
            return enumType;
        }

        if (!ByType.TryAdd(enumType, mapping))
        {
            if (!ByType.TryGetValue(enumType, out existing) ||
                !string.Equals(
                    existing.Identity,
                    identity,
                    StringComparison.Ordinal))
            {
                return null;
            }
            EnsureConfigured(markerType, enumType, existing);
            return enumType;
        }

        EnsureConfigured(markerType, enumType, mapping);
        return enumType;
    }

    public static object? GetDefaultValue(Type enumType)
    {
        if (!IsAppHostAvailable())
            return null;

        var firstEntry = ByType.TryGetValue(enumType, out var mapping)
            ? mapping.WireValueByEntry.Keys.FirstOrDefault() ?? ""
            : "";
        return Activator.CreateInstance(enumType, firstEntry)
            ?? throw new InvalidOperationException(
                $"Could not create dynamic enum {enumType}.");
    }

    private static bool IsAppHostAvailable()
    {
        try
        {
            _ = AppHost.Global;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static void EnsureConfigured(
        Type markerType,
        Type enumType,
        EnumMapping mapping)
    {
        if (!IsAppHostAvailable() ||
            !ConfiguredTypes.TryAdd(enumType, 0))
        {
            return;
        }

        try
        {
            ConfigureSlotMethod
                .MakeGenericMethod(markerType)
                .Invoke(null, [mapping.WireValueByEntry]);
        }
        catch
        {
            ConfiguredTypes.TryRemove(enumType, out _);
            throw;
        }
    }

    public static bool TryToWireValue(
        object? enumValue,
        out object? wireValue)
    {
        wireValue = null;
        if (enumValue is not IDynamicEnum dynamicEnum ||
            !ByType.TryGetValue(enumValue.GetType(), out var mapping))
        {
            return false;
        }

        if (mapping.WireValueByEntry.TryGetValue(
            dynamicEnum.Value,
            out var mapped))
        {
            wireValue = mapped;
            return true;
        }

        wireValue = dynamicEnum.Tag ?? dynamicEnum.Value;
        return true;
    }

    public static bool TryFromWireValue(
        Type enumType,
        object? wireValue,
        out object? enumValue)
    {
        enumValue = null;
        if (!ByType.TryGetValue(enumType, out var mapping))
            return false;

        var normalized = NormalizeKey(NormalizeValue(wireValue));
        var entry = mapping.WireValueByEntry.FirstOrDefault(pair =>
            string.Equals(
                NormalizeKey(pair.Value),
                normalized,
                StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(entry.Key))
            return false;

        enumValue = Activator.CreateInstance(enumType, entry.Key);
        return enumValue is not null;
    }

    private static EnumMapping CreateMapping(
        string identity,
        IReadOnlyList<object> values)
    {
        var entries = new Dictionary<string, object>(StringComparer.Ordinal);
        for (var index = 0; index < values.Count; index++)
        {
            var baseName = NormalizeKey(values[index]);
            if (string.IsNullOrWhiteSpace(baseName))
                baseName = $"Value {index + 1}";
            var name = baseName;
            var suffix = 2;
            while (entries.ContainsKey(name))
                name = $"{baseName} ({suffix++})";
            entries[name] = values[index];
        }
        return new EnumMapping(identity, entries);
    }

    private static void ConfigureSlot<TMarker>(
        IReadOnlyDictionary<string, object> entries)
    {
        var definition = WorkflowDynamicEnumDefinition<TMarker>.Instance;
        definition.BeginUpdate();
        try
        {
            definition.Clear();
            foreach (var entry in entries)
                definition.AddEntry(entry.Key, entry.Value);
        }
        finally
        {
            definition.EndUpdate();
        }
    }

    private static Type CreateMarkerType(byte[] hash)
    {
        var markerAssembly = typeof(E00).Assembly;
        var markerNamespace = typeof(E00).Namespace;
        var bytes = new Type[4];
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = markerAssembly.GetType(
                $"{markerNamespace}.E{hash[index]:X2}",
                throwOnError: true)!;
        }
        return typeof(EnumId<,,,>).MakeGenericType(bytes);
    }

    private static object NormalizeValue(object? value)
        => value switch
        {
            null => "",
            JsonElement { ValueKind: JsonValueKind.String } json =>
                json.GetString() ?? "",
            JsonElement { ValueKind: JsonValueKind.Number } json
                when json.TryGetInt64(out var integer) => integer,
            JsonElement { ValueKind: JsonValueKind.Number } json
                when json.TryGetDouble(out var number) => number,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            JsonElement json => json.ToString(),
            _ => value
        };

    private static string NormalizeKey(object? value)
        => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
}

// Public generic definitions make every closed pin type resolvable by VL's
// Roslyn platform while avoiding one source-generated class per workflow.
public sealed class WorkflowDynamicEnumDefinition<TMarker>
    : ManualDynamicEnumDefinitionBase<
        WorkflowDynamicEnumDefinition<TMarker>>
{
}

public sealed class WorkflowDynamicEnum<TMarker>
    : DynamicEnumBase<
        WorkflowDynamicEnum<TMarker>,
        WorkflowDynamicEnumDefinition<TMarker>>
{
    public WorkflowDynamicEnum()
        : base("")
    {
    }

    public WorkflowDynamicEnum(string value)
        : base(value)
    {
    }
}

public sealed class WorkflowEnumBit0 { }
public sealed class WorkflowEnumBit1 { }
public sealed class WorkflowEnumByte<T0, T1, T2, T3, T4, T5, T6, T7> { }
public sealed class WorkflowEnumSlot<T0, T1, T2, T3> { }
public sealed class EnumId<T0, T1, T2, T3> { }
