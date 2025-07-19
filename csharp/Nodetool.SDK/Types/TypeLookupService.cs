using MessagePack;
using Microsoft.Extensions.Logging;

namespace Nodetool.SDK.Types;

/// <summary>
/// Service for runtime type resolution and conversion.
/// Integrates NodeToolTypeRegistry and EnumRegistry for complete type management.
/// </summary>
public class TypeLookupService
{
    private readonly NodeToolTypeRegistry _typeRegistry;
    private readonly EnumRegistry _enumRegistry;
    private readonly ILogger<TypeLookupService> _logger;

    public TypeLookupService(
        NodeToolTypeRegistry typeRegistry,
        EnumRegistry enumRegistry,
        ILogger<TypeLookupService>? logger = null)
    {
        _typeRegistry = typeRegistry;
        _enumRegistry = enumRegistry;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TypeLookupService>.Instance;
    }

    /// <summary>
    /// Initialize all registries. Call this once during application startup.
    /// </summary>
    public void Initialize()
    {
        _logger.LogInformation("Initializing NodeTool type system...");
        
        _typeRegistry.RegisterAllTypes();
        _enumRegistry.RegisterAllEnums();
        
        _logger.LogInformation("NodeTool type system initialized successfully");
    }

    /// <summary>
    /// Deserialize MessagePack data to a specific type based on type name.
    /// </summary>
    /// <param name="data">MessagePack serialized data</param>
    /// <param name="typeName">NodeTool type identifier (e.g., "image", "hf.stable_diffusion")</param>
    /// <returns>Deserialized object, or null if type not found</returns>
    public object? DeserializeByTypeName(byte[] data, string typeName)
    {
        var targetType = _typeRegistry.GetType(typeName);
        if (targetType == null)
        {
            _logger.LogWarning("Unknown type name for deserialization: {TypeName}", typeName);
            return null;
        }

        try
        {
            return MessagePackSerializer.Deserialize(targetType, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize data for type {TypeName}", typeName);
            return null;
        }
    }

    /// <summary>
    /// Deserialize MessagePack data to a specific type.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="data">MessagePack serialized data</param>
    /// <returns>Deserialized object</returns>
    public T? Deserialize<T>(byte[] data)
    {
        try
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize data for type {Type}", typeof(T).Name);
            return default;
        }
    }

    /// <summary>
    /// Serialize an object to MessagePack format.
    /// </summary>
    /// <param name="value">Object to serialize</param>
    /// <returns>MessagePack serialized data</returns>
    public byte[] Serialize(object value)
    {
        try
        {
            return MessagePackSerializer.Serialize(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize object of type {Type}", value.GetType().Name);
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Determine the NodeTool type name from an object instance.
    /// </summary>
    /// <param name="obj">Object instance</param>
    /// <returns>NodeTool type identifier, or null if not found</returns>
    public string? GetTypeName(object obj)
    {
        if (obj == null)
            return null;

        var typeName = _typeRegistry.GetTypeName(obj.GetType());
        if (typeName != null)
            return typeName;

        // Try to extract from type property if available
        var typeProperty = obj.GetType().GetProperty("type");
        if (typeProperty != null)
        {
            var value = typeProperty.GetValue(obj);
            return value?.ToString();
        }

        return null;
    }

    /// <summary>
    /// Check if a type name is a registered NodeTool type.
    /// </summary>
    /// <param name="typeName">Type name to check</param>
    /// <returns>True if the type is registered</returns>
    public bool IsKnownType(string typeName)
    {
        return _typeRegistry.GetType(typeName) != null;
    }

    /// <summary>
    /// Check if a type name represents an enum.
    /// </summary>
    /// <param name="typeName">Type name to check</param>
    /// <returns>True if the type is a registered enum</returns>
    public bool IsEnumType(string typeName)
    {
        var type = _typeRegistry.GetType(typeName);
        return type?.IsEnum == true;
    }

    /// <summary>
    /// Get enum information for a type name.
    /// </summary>
    /// <param name="typeName">Type name</param>
    /// <returns>EnumInfo if the type is an enum, null otherwise</returns>
    public EnumRegistry.EnumInfo? GetEnumInfo(string typeName)
    {
        var type = _typeRegistry.GetType(typeName);
        if (type?.IsEnum == true)
        {
            return _enumRegistry.GetEnumInfo(type);
        }
        return null;
    }

    /// <summary>
    /// Get all registered type names grouped by category.
    /// </summary>
    /// <returns>Dictionary mapping category to type names</returns>
    public IReadOnlyDictionary<string, List<string>> GetTypesByCategory()
    {
        return _typeRegistry.GetTypesByNamespace();
    }

    /// <summary>
    /// Get all registered enum names grouped by category.
    /// </summary>
    /// <returns>Dictionary mapping category to enum infos</returns>
    public IReadOnlyDictionary<string, List<EnumRegistry.EnumInfo>> GetEnumsByCategory()
    {
        return _enumRegistry.GetEnumsByNamespace();
    }

    /// <summary>
    /// Create a typed wrapper for WebSocket message deserialization.
    /// </summary>
    /// <typeparam name="T">Expected message type</typeparam>
    /// <param name="data">MessagePack data</param>
    /// <returns>Deserialized message or default value</returns>
    public T? DeserializeMessage<T>(byte[] data) where T : class
    {
        try
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize WebSocket message of type {Type}", typeof(T).Name);
            return default;
        }
    }

    /// <summary>
    /// Convert an object to a different type if possible.
    /// Useful for type coercion in VL integration.
    /// </summary>
    /// <typeparam name="T">Target type</typeparam>
    /// <param name="value">Source value</param>
    /// <returns>Converted value or default</returns>
    public T? ConvertTo<T>(object? value)
    {
        if (value == null)
            return default;

        if (value is T directCast)
            return directCast;

        try
        {
            // Try standard conversion
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to convert {SourceType} to {TargetType}", 
                value.GetType().Name, typeof(T).Name);
            return default;
        }
    }

    /// <summary>
    /// Validate that an object matches its declared type.
    /// </summary>
    /// <param name="obj">Object to validate</param>
    /// <param name="expectedTypeName">Expected NodeTool type name</param>
    /// <returns>True if object matches expected type</returns>
    public bool ValidateType(object obj, string expectedTypeName)
    {
        var actualTypeName = GetTypeName(obj);
        if (actualTypeName == null)
        {
            _logger.LogWarning("Object of type {Type} has no NodeTool type name", obj.GetType().Name);
            return false;
        }

        var isValid = actualTypeName == expectedTypeName;
        if (!isValid)
        {
            _logger.LogWarning("Type mismatch: expected {Expected}, got {Actual}", 
                expectedTypeName, actualTypeName);
        }

        return isValid;
    }

    /// <summary>
    /// Get type information for debugging and development.
    /// </summary>
    /// <returns>Summary of all registered types and enums</returns>
    public TypeSystemInfo GetTypeSystemInfo()
    {
        var types = _typeRegistry.GetAllTypeNames();
        var enums = _enumRegistry.GetAllEnumNames();
        var typesByCategory = _typeRegistry.GetTypesByNamespace();
        var enumsByCategory = _enumRegistry.GetEnumsByNamespace();

        return new TypeSystemInfo
        {
            TotalTypes = types.Count,
            TotalEnums = enums.Count,
            TypesByCategory = typesByCategory,
            EnumsByCategory = enumsByCategory
        };
    }

    /// <summary>
    /// Information about the type system for debugging and monitoring.
    /// </summary>
    public class TypeSystemInfo
    {
        public int TotalTypes { get; init; }
        public int TotalEnums { get; init; }
        public IReadOnlyDictionary<string, List<string>> TypesByCategory { get; init; } = new Dictionary<string, List<string>>();
        public IReadOnlyDictionary<string, List<EnumRegistry.EnumInfo>> EnumsByCategory { get; init; } = new Dictionary<string, List<EnumRegistry.EnumInfo>>();
    }
} 