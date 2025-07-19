using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Nodetool.SDK.Types;

/// <summary>
/// Registry for discovering and managing all NodeTool enums.
/// Provides enum values for dynamic dropdown population in VL and other UI scenarios.
/// </summary>
public class EnumRegistry
{
    private readonly ILogger<EnumRegistry> _logger;
    private readonly Dictionary<string, Type> _enumTypes = new();
    private readonly Dictionary<Type, EnumInfo> _enumInfoCache = new();
    private bool _isInitialized = false;

    public EnumRegistry(ILogger<EnumRegistry>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<EnumRegistry>.Instance;
    }

    /// <summary>
    /// Information about an enum type including its values and display names.
    /// </summary>
    public class EnumInfo
    {
        public Type EnumType { get; init; } = null!;
        public string Name { get; init; } = string.Empty;
        public List<EnumValue> Values { get; init; } = new();
        public string Namespace { get; init; } = string.Empty;
    }

    /// <summary>
    /// Information about a specific enum value.
    /// </summary>
    public class EnumValue
    {
        public string Name { get; init; } = string.Empty;
        public object Value { get; init; } = null!;
        public string DisplayName { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    /// <summary>
    /// Discovers and registers all enum types from loaded assemblies.
    /// Call this once during application startup.
    /// </summary>
    public void RegisterAllEnums()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("Enum registry already initialized");
            return;
        }

        _logger.LogInformation("Discovering and registering NodeTool enums...");

        var discoveredEnums = 0;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var enumTypes = assembly.GetTypes()
                    .Where(IsNodeToolEnum)
                    .ToList();

                foreach (var enumType in enumTypes)
                {
                    RegisterEnum(enumType);
                    discoveredEnums++;
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning("Failed to load enum types from assembly {Assembly}: {Error}", 
                    assembly.FullName, ex.Message);
            }
        }

        _isInitialized = true;
        _logger.LogInformation("Registered {Count} NodeTool enums", discoveredEnums);
    }

    /// <summary>
    /// Register a specific enum type.
    /// </summary>
    /// <param name="enumType">The enum type to register</param>
    public void RegisterEnum(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            _logger.LogWarning("Type {Type} is not an enum, skipping", enumType.Name);
            return;
        }

        var enumName = enumType.Name;
        
        if (_enumTypes.ContainsKey(enumName))
        {
            _logger.LogWarning("Enum name collision: {EnumName} already registered. Skipping {Type}", 
                enumName, enumType.FullName);
            return;
        }

        _enumTypes[enumName] = enumType;
        
        // Build enum info
        var enumInfo = BuildEnumInfo(enumType);
        _enumInfoCache[enumType] = enumInfo;

        _logger.LogDebug("Registered enum: {EnumName} with {ValueCount} values", 
            enumName, enumInfo.Values.Count);
    }

    /// <summary>
    /// Get an enum type by name.
    /// </summary>
    /// <param name="enumName">The enum type name</param>
    /// <returns>The enum type, or null if not found</returns>
    public Type? GetEnumType(string enumName)
    {
        EnsureInitialized();
        return _enumTypes.TryGetValue(enumName, out var type) ? type : null;
    }

    /// <summary>
    /// Get enum information by enum name.
    /// </summary>
    /// <param name="enumName">The enum type name</param>
    /// <returns>EnumInfo containing values and metadata, or null if not found</returns>
    public EnumInfo? GetEnumInfo(string enumName)
    {
        var enumType = GetEnumType(enumName);
        return enumType != null ? GetEnumInfo(enumType) : null;
    }

    /// <summary>
    /// Get enum information by enum type.
    /// </summary>
    /// <param name="enumType">The enum type</param>
    /// <returns>EnumInfo containing values and metadata</returns>
    public EnumInfo? GetEnumInfo(Type enumType)
    {
        EnsureInitialized();
        return _enumInfoCache.TryGetValue(enumType, out var info) ? info : null;
    }

    /// <summary>
    /// Get all registered enum names.
    /// </summary>
    /// <returns>Collection of all registered enum names</returns>
    public IReadOnlyCollection<string> GetAllEnumNames()
    {
        EnsureInitialized();
        return _enumTypes.Keys;
    }

    /// <summary>
    /// Get all enum information grouped by namespace.
    /// </summary>
    /// <returns>Dictionary mapping namespace to enum infos</returns>
    public IReadOnlyDictionary<string, List<EnumInfo>> GetEnumsByNamespace()
    {
        EnsureInitialized();
        
        var grouped = new Dictionary<string, List<EnumInfo>>();
        
        foreach (var enumInfo in _enumInfoCache.Values)
        {
            var ns = enumInfo.Namespace;
            if (!grouped.ContainsKey(ns))
                grouped[ns] = new List<EnumInfo>();
            
            grouped[ns].Add(enumInfo);
        }

        return grouped;
    }

    /// <summary>
    /// Get enum values as string array (useful for VL dynamic enums).
    /// </summary>
    /// <param name="enumName">The enum type name</param>
    /// <returns>Array of enum value names, or empty array if not found</returns>
    public string[] GetEnumValueNames(string enumName)
    {
        var enumInfo = GetEnumInfo(enumName);
        return enumInfo?.Values.Select(v => v.Name).ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Get enum values with their underlying values (useful for serialization).
    /// </summary>
    /// <param name="enumName">The enum type name</param>
    /// <returns>Dictionary mapping display names to underlying values</returns>
    public Dictionary<string, object> GetEnumValueMap(string enumName)
    {
        var enumInfo = GetEnumInfo(enumName);
        if (enumInfo == null)
            return new Dictionary<string, object>();

        return enumInfo.Values.ToDictionary(v => v.DisplayName, v => v.Value);
    }

    /// <summary>
    /// Check if a type is a NodeTool-related enum.
    /// </summary>
    private static bool IsNodeToolEnum(Type type)
    {
        return type.IsEnum && 
               (type.Namespace?.StartsWith("Nodetool") == true ||
                type.Namespace?.StartsWith("Comfy") == true ||
                type.Namespace?.StartsWith("HuggingFace") == true ||
                type.DeclaringType?.Namespace?.StartsWith("Nodetool") == true);
    }

    /// <summary>
    /// Build detailed information about an enum type.
    /// </summary>
    private EnumInfo BuildEnumInfo(Type enumType)
    {
        var values = new List<EnumValue>();
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValues(enumType);

        for (int i = 0; i < enumNames.Length; i++)
        {
            var name = enumNames[i];
            var value = enumValues.GetValue(i)!;
            var displayName = FormatDisplayName(name);
            
            // Try to get description from attributes if available
            var field = enumType.GetField(name);
            var description = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

            values.Add(new EnumValue
            {
                Name = name,
                Value = value,
                DisplayName = displayName,
                Description = description
            });
        }

        return new EnumInfo
        {
            EnumType = enumType,
            Name = enumType.Name,
            Values = values,
            Namespace = ExtractNamespace(enumType)
        };
    }

    /// <summary>
    /// Format enum name for display (e.g., "SomeEnumValue" -> "Some Enum Value").
    /// </summary>
    private static string FormatDisplayName(string enumName)
    {
        if (string.IsNullOrEmpty(enumName))
            return enumName;

        // Insert spaces before capital letters (except the first)
        var result = string.Empty;
        for (int i = 0; i < enumName.Length; i++)
        {
            if (i > 0 && char.IsUpper(enumName[i]) && !char.IsUpper(enumName[i - 1]))
            {
                result += " ";
            }
            result += enumName[i];
        }

        return result;
    }

    /// <summary>
    /// Extract namespace category from enum type.
    /// </summary>
    private static string ExtractNamespace(Type enumType)
    {
        var ns = enumType.Namespace ?? "Unknown";
        
        if (ns.StartsWith("Nodetool"))
            return "Nodetool";
        if (ns.StartsWith("Comfy"))
            return "ComfyUI";
        if (ns.StartsWith("HuggingFace"))
            return "HuggingFace";
        
        return "Other";
    }

    /// <summary>
    /// Ensure the registry is initialized before use.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            RegisterAllEnums();
        }
    }
} 