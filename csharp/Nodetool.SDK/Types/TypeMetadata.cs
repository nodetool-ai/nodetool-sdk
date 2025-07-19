using System.Text.Json.Serialization;

namespace Nodetool.SDK.Types;

/// <summary>
/// Metadata for a type - C# equivalent of Python's TypeMetadata.
/// Provides universal type representation for all Nodetool types.
/// </summary>
public class TypeMetadata
{
    /// <summary>
    /// The type identifier (e.g., "str", "int", "list", "union", "image", etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this type is optional (nullable)
    /// </summary>
    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;

    /// <summary>
    /// Enum values for enum types
    /// </summary>
    [JsonPropertyName("values")]
    public List<object>? Values { get; set; }

    /// <summary>
    /// Type arguments for generic types (e.g., List[T], Dict[K,V])
    /// </summary>
    [JsonPropertyName("type_args")]
    public List<TypeMetadata> TypeArgs { get; set; } = new();

    /// <summary>
    /// Custom type name for complex types
    /// </summary>
    [JsonPropertyName("type_name")]
    public string? TypeName { get; set; }

    public override string ToString()
    {
        return Type switch
        {
            "list" when TypeArgs.Count > 0 => $"List[{TypeArgs[0]}]",
            "dict" when TypeArgs.Count >= 2 => $"Dict[{TypeArgs[0]}, {TypeArgs[1]}]",
            "dict" => "Dict[Any, Any]",
            "tuple" => $"Tuple[{string.Join(", ", TypeArgs)}]",
            "union" => $"({string.Join(" | ", TypeArgs)})",
            "enum" => $"Enum{Values?.ToString() ?? "[]"}",
            _ => TypeName ?? Type
        };
    }

    // Type checking methods (mirrors Python implementation)
    
    public bool IsAssetType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsAssetType(recursive: true));
            
        return AssetTypes.Contains(Type);
    }

    public bool IsCacheableType()
    {
        if (IsListType() || IsUnionType() || IsDictType())
            return TypeArgs.All(t => t.IsCacheableType());
            
        if (IsComfyDataType()) return true;
        if (IsComfyType()) return false;
        
        return true;
    }

    public bool IsSerializableType()
    {
        if (IsListType() || IsUnionType() || IsDictType())
            return TypeArgs.All(t => t.IsSerializableType());
            
        if (IsComfyDataType()) return true;
        if (IsComfyType()) return false;
        
        return true;
    }

    public bool IsComfyType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsComfyType(recursive: true));
            
        return Type.StartsWith("comfy.");
    }

    public bool IsComfyModel(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsComfyModel(recursive: true));
            
        return ComfyModelTypes.Contains(Type);
    }

    public bool IsModelFileType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsModelFileType(recursive: true));
            
        return ModelFileTypes.Contains(Type);
    }

    public bool IsComfyDataType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsComfyDataType(recursive: true));
            
        return ComfyDataTypes.Contains(Type);
    }

    public bool IsPrimitiveType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsPrimitiveType(recursive: true));
            
        return new[] { "int", "float", "bool", "str", "text" }.Contains(Type);
    }

    public bool IsEnumType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsEnumType(recursive: true));
            
        return Type == "enum";
    }

    public bool IsListType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsListType(recursive: true));
            
        return Type == "list";
    }

    public bool IsTupleType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsTupleType(recursive: true));
            
        return Type == "tuple";
    }

    public bool IsDictType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsDictType(recursive: true));
            
        return Type == "dict";
    }

    public bool IsImageType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsImageType(recursive: true));
            
        return Type == "image";
    }

    public bool IsAudioType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsAudioType(recursive: true));
            
        return Type == "audio";
    }

    public bool IsVideoType(bool recursive = false)
    {
        if (recursive && IsUnionType())
            return TypeArgs.Any(t => t.IsVideoType(recursive: true));
            
        return Type == "video";
    }

    public bool IsUnionType() => Type == "union";

    // Type collections (to be populated from Python analysis)
    private static readonly HashSet<string> AssetTypes = new()
    {
        "image", "audio", "video", "document", "text", "folder", "model_ref"
    };

    private static readonly HashSet<string> ComfyModelTypes = new()
    {
        "comfy.model", "comfy.clip", "comfy.vae", "comfy.controlnet"
    };

    private static readonly HashSet<string> ModelFileTypes = new()
    {
        "model_file", "checkpoint_file", "lora_file"
    };

    private static readonly HashSet<string> ComfyDataTypes = new()
    {
        "comfy.image", "comfy.latent", "comfy.conditioning"
    };
} 