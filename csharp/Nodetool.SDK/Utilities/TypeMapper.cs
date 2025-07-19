using Nodetool.SDK.Types;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Utilities;

/// <summary>
/// Maps TypeMetadata to C# types for the SDK
/// </summary>
public static class TypeMapper
{
    private static readonly Dictionary<string, Type> PrimitiveTypeMap = new()
    {
        ["str"] = typeof(string),
        ["int"] = typeof(int),
        ["float"] = typeof(double),
        ["bool"] = typeof(bool),
        ["bytes"] = typeof(byte[]),
        ["any"] = typeof(object),
        ["none"] = typeof(void)
    };

    private static readonly Dictionary<string, Type> AssetTypeMap = new()
    {
        ["image"] = typeof(ImageRef),
        ["audio"] = typeof(AudioRef),
        ["video"] = typeof(VideoRef),
        ["text"] = typeof(TextRef),
        ["document"] = typeof(DocumentRef),
        ["folder"] = typeof(FolderRef),
        ["model_ref"] = typeof(ModelRef)
    };

    /// <summary>
    /// Map TypeMetadata to a C# Type
    /// </summary>
    /// <param name="typeMetadata">The type metadata to map</param>
    /// <returns>The corresponding C# type</returns>
    public static Type MapToType(TypeMetadata typeMetadata)
    {
        var baseType = MapToBaseType(typeMetadata);
        
        // Handle optional types (nullable)
        if (typeMetadata.Optional && baseType.IsValueType)
        {
            return typeof(Nullable<>).MakeGenericType(baseType);
        }
        
        return baseType;
    }

    private static Type MapToBaseType(TypeMetadata typeMetadata)
    {
        return typeMetadata.Type switch
        {
            // Primitive types
            var primitiveType when PrimitiveTypeMap.ContainsKey(primitiveType) 
                => PrimitiveTypeMap[primitiveType],

            // Asset types
            var assetType when AssetTypeMap.ContainsKey(assetType) 
                => AssetTypeMap[assetType],

            // Collection types
            "list" => MapListType(typeMetadata),
            "dict" => MapDictType(typeMetadata),
            "tuple" => MapTupleType(typeMetadata),
            "union" => MapUnionType(typeMetadata),

            // Enum types
            "enum" => MapEnumType(typeMetadata),

            // ComfyUI types - map to object for now
            var comfyType when comfyType.StartsWith("comfy.") => typeof(object),

            // Custom types - try to resolve from registry
            _ => ResolveCustomType(typeMetadata)
        };
    }

    private static Type MapListType(TypeMetadata typeMetadata)
    {
        if (typeMetadata.TypeArgs.Count == 0)
        {
            return typeof(List<object>);
        }

        var elementType = MapToType(typeMetadata.TypeArgs[0]);
        return typeof(List<>).MakeGenericType(elementType);
    }

    private static Type MapDictType(TypeMetadata typeMetadata)
    {
        if (typeMetadata.TypeArgs.Count < 2)
        {
            return typeof(Dictionary<string, object>);
        }

        var keyType = MapToType(typeMetadata.TypeArgs[0]);
        var valueType = MapToType(typeMetadata.TypeArgs[1]);
        return typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
    }

    private static Type MapTupleType(TypeMetadata typeMetadata)
    {
        if (typeMetadata.TypeArgs.Count == 0)
        {
            return typeof(object);
        }

        var argTypes = typeMetadata.TypeArgs.Select(MapToType).ToArray();
        
        // Handle common tuple arities
        return argTypes.Length switch
        {
            1 => typeof(Tuple<>).MakeGenericType(argTypes),
            2 => typeof(Tuple<,>).MakeGenericType(argTypes),
            3 => typeof(Tuple<,,>).MakeGenericType(argTypes),
            4 => typeof(Tuple<,,,>).MakeGenericType(argTypes),
            5 => typeof(Tuple<,,,,>).MakeGenericType(argTypes),
            6 => typeof(Tuple<,,,,,>).MakeGenericType(argTypes),
            7 => typeof(Tuple<,,,,,,>).MakeGenericType(argTypes),
            _ => typeof(object[]) // Fallback for large tuples
        };
    }

    private static Type MapUnionType(TypeMetadata typeMetadata)
    {
        // For unions, we need to find a common base type or use object
        if (typeMetadata.TypeArgs.Count == 0)
        {
            return typeof(object);
        }

        var argTypes = typeMetadata.TypeArgs.Select(MapToType).ToArray();

        // Check for nullable pattern (T | None)
        if (argTypes.Length == 2)
        {
            var nonNullType = argTypes.FirstOrDefault(t => t != typeof(void));
            if (nonNullType != null && argTypes.Any(t => t == typeof(void)))
            {
                return nonNullType.IsValueType 
                    ? typeof(Nullable<>).MakeGenericType(nonNullType)
                    : nonNullType;
            }
        }

        // Find common base type
        var commonBase = FindCommonBaseType(argTypes);
        return commonBase ?? typeof(object);
    }

    private static Type MapEnumType(TypeMetadata typeMetadata)
    {
        // For now, return object for enums
        // TODO: Generate actual enum types based on values
        return typeof(object);
    }

    private static Type ResolveCustomType(TypeMetadata typeMetadata)
    {
        // Try to resolve from BaseType registry
        if (typeMetadata.TypeName != null)
        {
            var registeredType = BaseType.GetType(typeMetadata.TypeName);
            if (registeredType != null)
            {
                return registeredType;
            }
        }

        // Try to resolve by type name
        var typeByName = BaseType.GetType(typeMetadata.Type);
        if (typeByName != null)
        {
            return typeByName;
        }

        // Fallback to object
        return typeof(object);
    }

    private static Type? FindCommonBaseType(Type[] types)
    {
        if (types.Length == 0)
            return null;

        var commonType = types[0];
        
        for (int i = 1; i < types.Length; i++)
        {
            commonType = FindCommonBaseType(commonType, types[i]);
            if (commonType == typeof(object))
                break; // No point checking further
        }

        return commonType;
    }

    private static Type FindCommonBaseType(Type type1, Type type2)
    {
        if (type1 == type2)
            return type1;

        if (type1.IsAssignableFrom(type2))
            return type1;

        if (type2.IsAssignableFrom(type1))
            return type2;

        // Find common base in inheritance hierarchy
        var current = type1;
        while (current != null && current != typeof(object))
        {
            if (current.IsAssignableFrom(type2))
                return current;
            current = current.BaseType;
        }

        return typeof(object);
    }

    /// <summary>
    /// Get default value for a type
    /// </summary>
    /// <param name="type">The type to get default value for</param>
    /// <returns>Default value for the type</returns>
    public static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type == typeof(string))
        {
            return string.Empty;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return Activator.CreateInstance(type);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
} 