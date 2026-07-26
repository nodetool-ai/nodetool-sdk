using System.Reflection;
using Nodetool.SDK.Types;
using Nodetool.SDK.Types.Assets;
using SkiaSharp;
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
            "file" or "file_path" => (typeof(VlPath), new VlPath("")),
            "list" or "array" or "tuple" =>
                GetSpreadTypeAndDefault(metadata, forInput: true),
            _ => GetTypeAndDefault(metadata)
        };
    }

    public static (Type Type, object? DefaultValue) GetTypeAndDefault(TypeMetadata metadata)
    {
        var type = metadata.Type?.Trim().ToLowerInvariant();
        return type switch
        {
            "string" or "str" or "text" or "chunk" => (typeof(string), ""),
            "enum" => GetEnumTypeAndDefault(metadata),
            "int" or "integer" => (typeof(int), 0),
            "float" or "number" => (typeof(float), 0.0f),
            "bool" or "boolean" => (typeof(bool), false),
            "bytes" => (typeof(byte[]), Array.Empty<byte>()),
            "list" or "array" or "tuple" =>
                GetSpreadTypeAndDefault(metadata, forInput: false),
            "file" or "file_path" => (typeof(string), ""),
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

    private static (Type Type, object? DefaultValue) GetEnumTypeAndDefault(
        TypeMetadata metadata)
    {
        var enumType = DynamicWorkflowEnumFactory.GetOrCreate(
            metadata.TypeName,
            metadata.Values);
        return enumType is null
            ? (typeof(string), "")
            : (enumType, DynamicWorkflowEnumFactory.GetDefaultValue(enumType));
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
        var registry = new NodeToolTypeRegistry();
        // The generated catalog belongs to this adapter rather than the portable SDK.
        // Loading it explicitly also makes discovery deterministic before a generated
        // DTO has otherwise been touched by the CLR.
        registry.RegisterAllTypes(Assembly.Load("Nodetool.Types"));
        return registry;
    }
}
