using Nodetool.SDK.Types;
using Nodetool.SDK.Types.Assets;
using SkiaSharp;
using System.Reflection;
using VL.Lib.Collections;
using VlPath = VL.Lib.IO.Path;

namespace Nodetool.SDK.VL.Utilities;

internal static class WorkflowVlTypeMapping
{
    private static readonly Lazy<NodeToolTypeRegistry> TypeRegistry = new(CreateTypeRegistry);

    public static (Type Type, object? DefaultValue) GetInputTypeAndDefault(
        TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type switch
        {
            "audio" or "video" or "document" or "asset" or "asset_ref" or
            "folder" or "model_ref" or "model_3d" or "font"
                => (typeof(VlPath), new VlPath("")),
            "list" or "array" => GetSpreadTypeAndDefault(metadata, forInput: true),
            _ => GetTypeAndDefault(metadata)
        };
    }

    public static (Type Type, object? DefaultValue) GetTypeAndDefault(TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type switch
        {
            "string" or "str" or "text" or "chunk" or "enum" => (typeof(string), ""),
            "int" or "integer" => (typeof(int), 0),
            "float" or "number" => (typeof(float), 0.0f),
            "bool" or "boolean" => (typeof(bool), false),
            "list" or "array" => GetSpreadTypeAndDefault(metadata, forInput: false),
            "image" => (typeof(SKImage), null),
            "audio" => (typeof(AudioRef), new AudioRef()),
            "video" => (typeof(VideoRef), new VideoRef()),
            "document" => (typeof(DocumentRef), new DocumentRef()),
            "asset" or "asset_ref" => (typeof(GenericAssetRef), new GenericAssetRef()),
            "folder" => (typeof(FolderRef), new FolderRef()),
            "model_ref" => (typeof(ModelRef), new ModelRef()),
            "model_3d" => (typeof(Model3DRef), new Model3DRef()),
            "font" => (typeof(FontRef), new FontRef()),
            _ => GetStructuredTypeAndDefault(metadata)
        };
    }

    public static bool UsesObjectFallback(TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type is not ("any" or "object" or "dict" or "map") &&
               GetTypeAndDefault(metadata).Type == typeof(object);
    }

    private static (Type Type, object DefaultValue) GetSpreadTypeAndDefault(
        TypeMetadata metadata,
        bool forInput)
    {
        var elementMetadata = metadata.TypeArgs.FirstOrDefault() ?? new TypeMetadata { Type = "any" };
        var elementType = (forInput
            ? GetInputTypeAndDefault(elementMetadata)
            : GetTypeAndDefault(elementMetadata)).Type;
        var spreadType = typeof(Spread<>).MakeGenericType(elementType);
        return (spreadType, VlValueConversion.CreateEmptySpread(elementType));
    }

    private static (Type Type, object? DefaultValue) GetStructuredTypeAndDefault(TypeMetadata metadata)
    {
        var candidates = new[] { metadata.TypeName, metadata.Type }
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Select(candidate => candidate!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            if (TypeRegistry.Value.GetType(candidate) is { } resolvedType)
                return (resolvedType, null);
        }

        return (typeof(object), null);
    }

    private static NodeToolTypeRegistry CreateTypeRegistry()
    {
        // The project reference can remain unloaded until a CLR type is used. Load it
        // explicitly before registry discovery so workflow pins see generated DTOs.
        try
        {
            Assembly.Load("Nodetool.Types");
        }
        catch
        {
            // The object fallback remains available when generated types are not packaged.
        }

        var registry = new NodeToolTypeRegistry();
        registry.RegisterAllTypes();
        return registry;
    }
}
