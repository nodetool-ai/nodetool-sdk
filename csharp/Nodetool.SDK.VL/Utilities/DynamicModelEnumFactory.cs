using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using VL.Core;
using VL.Lib.Collections;
using Nodetool.SDK.Models;

namespace Nodetool.SDK.VL.Utilities;

/// <summary>
/// Projects the current server model catalog onto stable compiler-visible
/// dynamic-enum types. Type identity depends only on compatibility, never on
/// the changing model list.
/// </summary>
internal static class DynamicModelEnumFactory
{
    private sealed record EnumMapping(
        string Compatibility,
        IReadOnlyDictionary<string, object> VisibleWireValues,
        IReadOnlyDictionary<string, object> HistoricalWireValues);

    private static readonly ConcurrentDictionary<Type, EnumMapping> ByType = new();
    private static readonly object DefinitionUpdateLock = new();
    private static readonly MethodInfo ConfigureSlotMethod = typeof(
            DynamicModelEnumFactory)
        .GetMethod(
            nameof(ConfigureSlot),
            BindingFlags.NonPublic | BindingFlags.Static)!;
    private static SynchronizationContext? _synchronizationContext;
    private static volatile bool _hasCatalog;

    public static void Configure(SynchronizationContext? synchronizationContext)
        => _synchronizationContext = synchronizationContext;

    public static bool IsModelType(string? value)
    {
        var type = value?.Trim().ToLowerInvariant();
        return type is
                "language_model" or "image_model" or "video_model" or
                "tts_model" or "asr_model" or "music_model" or
                "embedding_model" or "llama_model" or "llama_cpp_model" ||
            type?.StartsWith("hf.", StringComparison.Ordinal) == true ||
            type?.StartsWith("tjs.", StringComparison.Ordinal) == true;
    }

    public static Type? GetOrCreate(string? compatibility)
    {
        if (!_hasCatalog || !IsModelType(compatibility))
            return null;

        var normalized = compatibility!.Trim().ToLowerInvariant();
        var markerType = CreateMarkerType(SHA256.HashData(
            Encoding.UTF8.GetBytes(normalized)));
        var enumType = typeof(ModelDynamicEnum<>).MakeGenericType(markerType);
        var mapping = ByType.GetOrAdd(
            enumType,
            _ => new EnumMapping(
                normalized,
                new Dictionary<string, object>(StringComparer.Ordinal),
                new Dictionary<string, object>(StringComparer.Ordinal)));
        ConfigureDefinition(markerType, mapping.VisibleWireValues);
        return enumType;
    }

    public static void UpdateCatalog(ModelCatalogSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _hasCatalog = snapshot.LastSuccessfulRefreshUtc.HasValue;
        if (!_hasCatalog)
            return;

        foreach (var group in snapshot.Models
                     .Where(model => model.IsReady && IsModelType(model.Compatibility))
                     .GroupBy(model => model.Compatibility, StringComparer.OrdinalIgnoreCase))
        {
            UpdateCompatibility(group.Key, group.ToArray());
        }

        var active = snapshot.Models
            .Where(model => model.IsReady)
            .Select(model => model.Compatibility)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in ByType)
        {
            if (!active.Contains(pair.Value.Compatibility))
                UpdateCompatibility(pair.Value.Compatibility, []);
        }
    }

    public static object? GetDefaultValue(Type enumType)
    {
        if (!IsAppHostAvailable())
            return null;
        return Activator.CreateInstance(enumType, "");
    }

    public static bool TryToWireValue(object? value, out object? wireValue)
    {
        wireValue = null;
        if (value is not IDynamicEnum dynamicEnum ||
            !ByType.TryGetValue(value.GetType(), out var mapping))
        {
            return false;
        }

        if (mapping.HistoricalWireValues.TryGetValue(
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
        var identity = WireIdentity(wireValue);
        var entry = mapping.HistoricalWireValues.FirstOrDefault(pair =>
            string.Equals(
                WireIdentity(pair.Value),
                identity,
                StringComparison.Ordinal));
        if (string.IsNullOrEmpty(entry.Key))
            return false;
        enumValue = Activator.CreateInstance(enumType, entry.Key);
        return enumValue != null;
    }

    private static string WireIdentity(object? value)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToElement(value);
        if (json.ValueKind != System.Text.Json.JsonValueKind.Object)
            return json.ToString();
        return string.Join(
            "\u001f",
            new[] { "type", "provider", "id", "repo_id", "path" }
                .Select(name => json.TryGetProperty(name, out var property)
                    ? property.ToString()
                    : ""));
    }

    internal static void ResetCatalog()
    {
        _hasCatalog = false;
        foreach (var pair in ByType)
            UpdateCompatibility(pair.Value.Compatibility, []);
    }

    private static void UpdateCompatibility(
        string compatibility,
        IReadOnlyList<ModelDescriptor> models)
    {
        var normalized = compatibility.Trim().ToLowerInvariant();
        var markerType = CreateMarkerType(SHA256.HashData(
            Encoding.UTF8.GetBytes(normalized)));
        var enumType = typeof(ModelDynamicEnum<>).MakeGenericType(markerType);
        ByType.TryGetValue(enumType, out var previous);
        var visible = CreateEntries(models);
        var historical = previous?.HistoricalWireValues.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal) ??
            new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var pair in visible)
            historical[pair.Key] = pair.Value;
        var mapping = new EnumMapping(normalized, visible, historical);
        ByType[enumType] = mapping;
        ConfigureDefinition(markerType, visible);
    }

    private static IReadOnlyDictionary<string, object> CreateEntries(
        IReadOnlyList<ModelDescriptor> models)
    {
        var entries = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var model in models.OrderBy(
                     model => model.DisplayName,
                     StringComparer.OrdinalIgnoreCase))
        {
            var source = model.Provider ?? model.RepositoryId ?? model.Id;
            var label = model.DisplayName;
            if (entries.ContainsKey(label))
                label = $"{model.DisplayName} ({source})";
            var suffix = 2;
            var unique = label;
            while (entries.ContainsKey(unique))
                unique = $"{label} ({suffix++})";
            entries[unique] = model.Select().ToInputValue();
        }
        return entries;
    }

    private static void ConfigureDefinition(
        Type markerType,
        IReadOnlyDictionary<string, object> entries)
    {
        if (!IsAppHostAvailable())
            return;

        void Apply()
        {
            try
            {
                ConfigureSlotMethod
                    .MakeGenericMethod(markerType)
                    .Invoke(null, [entries]);
            }
            catch (Exception exception)
            {
                VlLog.Error(
                    $"model enum update failed: " +
                    VlLog.SafeError(exception.GetBaseException()));
            }
        }
        var context = _synchronizationContext;
        if (context != null && SynchronizationContext.Current != context)
            context.Post(_ => Apply(), null);
        else
            Apply();
    }

    private static void ConfigureSlot<TMarker>(
        IReadOnlyDictionary<string, object> entries)
    {
        // Catalog refreshes from overlapping AppHosts can be posted onto
        // different synchronization contexts. The underlying vvvv enum
        // definition is process-global and its Clear/Add update is not atomic.
        lock (DefinitionUpdateLock)
        {
            var definition = ModelDynamicEnumDefinition<TMarker>.Instance;
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
}

public sealed class ModelDynamicEnumDefinition<TMarker>
    : ManualDynamicEnumDefinitionBase<ModelDynamicEnumDefinition<TMarker>>
{
}

public sealed class ModelDynamicEnum<TMarker>
    : DynamicEnumBase<
        ModelDynamicEnum<TMarker>,
        ModelDynamicEnumDefinition<TMarker>>
{
    public ModelDynamicEnum() : base("") { }
    public ModelDynamicEnum(string value) : base(value) { }
}
