using Nodetool.SDK.Types;
using Nodetool.SDK.Types.Assets;
using SkiaSharp;

namespace Nodetool.SDK.VL.Utilities;

internal static class WorkflowVlTypeMapping
{
    public static (Type Type, object? DefaultValue) GetTypeAndDefault(TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type switch
        {
            "string" or "str" or "text" or "chunk" or "enum" => (typeof(string), ""),
            "int" or "integer" => (typeof(int), 0),
            "float" or "number" => (typeof(float), 0.0f),
            "bool" or "boolean" => (typeof(bool), false),
            "list" or "array" => GetArrayTypeAndDefault(metadata),
            "image" => (typeof(SKImage), null),
            "audio" => (typeof(AudioRef), new AudioRef()),
            "video" => (typeof(VideoRef), new VideoRef()),
            "document" => (typeof(DocumentRef), new DocumentRef()),
            "asset" or "asset_ref" => (typeof(GenericAssetRef), new GenericAssetRef()),
            _ => (typeof(object), null)
        };
    }

    public static bool UsesObjectFallback(TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type is not ("any" or "object" or "dict" or "map") &&
               GetTypeAndDefault(metadata).Type == typeof(object);
    }

    private static (Type Type, object DefaultValue) GetArrayTypeAndDefault(TypeMetadata metadata)
    {
        var elementMetadata = metadata.TypeArgs.FirstOrDefault() ?? new TypeMetadata { Type = "any" };
        var elementType = GetTypeAndDefault(elementMetadata).Type;
        var arrayType = elementType.MakeArrayType();
        return (arrayType, Array.CreateInstance(elementType, 0));
    }
}
