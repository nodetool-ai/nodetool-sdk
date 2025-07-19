using System.Text.Json.Serialization;

namespace Nodetool.SDK.Types;

/// <summary>
/// Base class for all Nodetool types - C# equivalent of Python's BaseType.
/// Provides automatic type registration and JSON serialization.
/// </summary>
public abstract class BaseType
{
    /// <summary>
    /// The type identifier for this type
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// Static type registry mapping type names to .NET types
    /// </summary>
    private static readonly Dictionary<string, Type> NameToType = new();
    
    /// <summary>
    /// Reverse mapping from .NET types to type names
    /// </summary>
    private static readonly Dictionary<Type, string> TypeToName = new();

    /// <summary>
    /// Register a type in the type registry
    /// </summary>
    /// <param name="dotnetType">The .NET type</param>
    /// <param name="typeName">The Nodetool type name</param>
    public static void RegisterType(Type dotnetType, string typeName)
    {
        NameToType[typeName] = dotnetType;
        TypeToName[dotnetType] = typeName;
    }

    /// <summary>
    /// Get a .NET type by its Nodetool type name
    /// </summary>
    /// <param name="typeName">The Nodetool type name</param>
    /// <returns>The .NET type, or null if not found</returns>
    public static Type? GetType(string typeName)
    {
        return NameToType.TryGetValue(typeName, out var type) ? type : null;
    }

    /// <summary>
    /// Get the Nodetool type name for a .NET type
    /// </summary>
    /// <param name="dotnetType">The .NET type</param>
    /// <returns>The Nodetool type name, or null if not found</returns>
    public static string? GetTypeName(Type dotnetType)
    {
        return TypeToName.TryGetValue(dotnetType, out var name) ? name : null;
    }

    /// <summary>
    /// Create an instance from a dictionary (JSON deserialization)
    /// </summary>
    /// <param name="data">The data dictionary</param>
    /// <returns>An instance of the appropriate type</returns>
    /// <exception cref="ArgumentException">If type name is missing or unknown</exception>
    public static BaseType FromDict(Dictionary<string, object> data)
    {
        if (!data.TryGetValue("type", out var typeNameObj) || typeNameObj is not string typeName)
        {
            throw new ArgumentException("Type name is missing. Types must derive from BaseType");
        }

        if (!NameToType.TryGetValue(typeName, out var type))
        {
            throw new ArgumentException($"Unknown type name: {typeName}. Types must derive from BaseType");
        }

        // Create instance using JSON deserialization
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var instance = System.Text.Json.JsonSerializer.Deserialize(json, type);
        
        if (instance is not BaseType baseTypeInstance)
        {
            throw new ArgumentException($"Type {typeName} does not derive from BaseType");
        }

        return baseTypeInstance;
    }

    /// <summary>
    /// Convert this instance to a dictionary (JSON serialization)
    /// </summary>
    /// <returns>A dictionary representation</returns>
    public virtual Dictionary<string, object> ToDict()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, GetType());
        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        return dict ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Get type metadata for this type
    /// </summary>
    /// <returns>TypeMetadata describing this type</returns>
    public virtual TypeMetadata GetTypeMetadata()
    {
        return new TypeMetadata { Type = Type };
    }

    static BaseType()
    {
        // Register primitive types
        RegisterType(typeof(string), "str");
        RegisterType(typeof(int), "int");
        RegisterType(typeof(float), "float");
        RegisterType(typeof(double), "float");
        RegisterType(typeof(bool), "bool");
        RegisterType(typeof(object), "any");
        RegisterType(typeof(byte[]), "bytes");
    }
} 