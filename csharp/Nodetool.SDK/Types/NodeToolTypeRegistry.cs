using System.Reflection;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace Nodetool.SDK.Types;

/// <summary>
/// Registry for discovering and managing all NodeTool MessagePack types.
/// Automatically discovers types from nodetool-core/csharp_types/ and provides runtime lookup.
/// </summary>
public class NodeToolTypeRegistry
{
    private readonly ILogger<NodeToolTypeRegistry> _logger;
    private readonly Dictionary<string, Type> _typesByName = new();
    private readonly Dictionary<Type, string> _namesByType = new();
    private readonly Dictionary<Type, PropertyInfo?> _typePropertyCache = new();
    private bool _isInitialized = false;

    public NodeToolTypeRegistry(ILogger<NodeToolTypeRegistry>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<NodeToolTypeRegistry>.Instance;
    }

    /// <summary>
    /// Discovers and registers all MessagePack types from loaded assemblies.
    /// Call this once during application startup.
    /// </summary>
    public void RegisterAllTypes()
    {
        if (_isInitialized)
        {
            _logger.LogDebug("Type registry already initialized");
            return;
        }

        _logger.LogInformation("Discovering and registering NodeTool types...");

        var discoveredTypes = 0;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var messagePackTypes = assembly.GetTypes()
                    .Where(IsMessagePackType)
                    .ToList();

                foreach (var type in messagePackTypes)
                {
                    var typeName = ExtractTypeName(type);
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        RegisterType(type, typeName);
                        discoveredTypes++;
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                _logger.LogWarning("Failed to load types from assembly {Assembly}: {Error}", 
                    assembly.FullName, ex.Message);
            }
        }

        _isInitialized = true;
        _logger.LogInformation("Registered {Count} NodeTool types", discoveredTypes);
    }

    /// <summary>
    /// Register a specific type with its type name.
    /// </summary>
    /// <param name="type">The .NET type</param>
    /// <param name="typeName">The NodeTool type identifier</param>
    public void RegisterType(Type type, string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            _logger.LogWarning("Skipping type {Type} - no type name", type.Name);
            return;
        }

        if (_typesByName.ContainsKey(typeName))
        {
            _logger.LogWarning("Type name collision: {TypeName} already registered. Skipping {Type}", 
                typeName, type.Name);
            return;
        }

        _typesByName[typeName] = type;
        _namesByType[type] = typeName;

        _logger.LogDebug("Registered type: {TypeName} -> {Type}", typeName, type.Name);
    }

    /// <summary>
    /// Get a .NET type by its NodeTool type name.
    /// </summary>
    /// <param name="typeName">The NodeTool type identifier (e.g., "image", "hf.stable_diffusion")</param>
    /// <returns>The corresponding .NET type, or null if not found</returns>
    public Type? GetType(string typeName)
    {
        EnsureInitialized();
        return _typesByName.TryGetValue(typeName, out var type) ? type : null;
    }

    /// <summary>
    /// Get the NodeTool type name for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type</param>
    /// <returns>The NodeTool type identifier, or null if not found</returns>
    public string? GetTypeName(Type type)
    {
        EnsureInitialized();
        return _namesByType.TryGetValue(type, out var name) ? name : null;
    }

    /// <summary>
    /// Get all registered type names.
    /// </summary>
    /// <returns>Collection of all registered type identifiers</returns>
    public IReadOnlyCollection<string> GetAllTypeNames()
    {
        EnsureInitialized();
        return _typesByName.Keys;
    }

    /// <summary>
    /// Get all registered types grouped by namespace/category.
    /// </summary>
    /// <returns>Dictionary mapping namespace to types (e.g., "hf" -> ["hf.stable_diffusion", ...])</returns>
    public IReadOnlyDictionary<string, List<string>> GetTypesByNamespace()
    {
        EnsureInitialized();
        
        var grouped = new Dictionary<string, List<string>>();
        
        foreach (var typeName in _typesByName.Keys)
        {
            var namespaceName = ExtractNamespace(typeName);
            if (!grouped.ContainsKey(namespaceName))
                grouped[namespaceName] = new List<string>();
            
            grouped[namespaceName].Add(typeName);
        }

        return grouped;
    }

    /// <summary>
    /// Check if a type is a MessagePack type suitable for NodeTool.
    /// </summary>
    private static bool IsMessagePackType(Type type)
    {
        return type.IsClass && 
               !type.IsAbstract && 
               type.GetCustomAttribute<MessagePackObjectAttribute>() != null &&
               type.Namespace == "Nodetool.Types";
    }

    /// <summary>
    /// Extract the NodeTool type name from a .NET type.
    /// Looks for a 'type' property with a default value.
    /// </summary>
    private string? ExtractTypeName(Type type)
    {
        // Cache property lookup for performance
        if (!_typePropertyCache.TryGetValue(type, out var typeProperty))
        {
            typeProperty = type.GetProperty("type", BindingFlags.Public | BindingFlags.Instance);
            _typePropertyCache[type] = typeProperty;
        }

        if (typeProperty == null)
            return null;

        try
        {
            // Create an instance to get the default type value
            var instance = Activator.CreateInstance(type);
            if (instance == null)
                return null;

            var typeValue = typeProperty.GetValue(instance);
            return typeValue?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to extract type name from {Type}: {Error}", type.Name, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Extract namespace from type name (e.g., "hf.stable_diffusion" -> "hf").
    /// </summary>
    private static string ExtractNamespace(string typeName)
    {
        var dotIndex = typeName.IndexOf('.');
        return dotIndex > 0 ? typeName.Substring(0, dotIndex) : "core";
    }

    /// <summary>
    /// Ensure the registry is initialized before use.
    /// </summary>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            RegisterAllTypes();
        }
    }
} 