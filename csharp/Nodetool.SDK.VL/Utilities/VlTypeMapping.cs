using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types.Assets;
using VL.Lib.Collections;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlTypeMapping
{
    public static (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
    {
        if (nodeType == null || string.IsNullOrWhiteSpace(nodeType.Type))
            return (typeof(string), "");

        if (TryMapAssetReference(nodeType) is { } assetReference)
            return assetReference;

        var t = nodeType.Type.Trim().ToLowerInvariant();

        // Handle "any"/union-ish types by looking at type_args/type_name.
        if (t is "any" or "union" or "oneof" or "either")
        {
            var kind = Classify(nodeType);
            return kind switch
            {
                Kind.Numeric => (typeof(float), 0.0f),
                Kind.Boolean => (typeof(bool), false),
                Kind.String => (typeof(string), ""),
                Kind.Object => (typeof(object), null),
                _ => (typeof(object), null)
            };
        }

        return t switch
        {
            "str" or "string" or "text" or "chunk" or "enum" => (typeof(string), ""),
            "int" or "integer" => (typeof(int), 0),
            "float" or "number" => (typeof(float), 0.0f),
            "bool" or "boolean" => (typeof(bool), false),
            "bytes" => (typeof(byte[]), Array.Empty<byte>()),
            "list" or "array" or "tuple" => MapCollection(nodeType),
            "dict" or "object" or "json" or "record_type" => (typeof(object), null),
            "file" or "file_path" => (typeof(string), ""),
            // Preserve an unsupported structured value as an object/JSON value.
            // Treating it as text makes the pin look more specific than the
            // server metadata warrants and loses list/map structure.
            _ => (typeof(object), null)
        };
    }

    private static (Type, object)? TryMapAssetReference(NodeTypeDefinition nodeType)
    {
        var type = nodeType.Type?.Trim().ToLowerInvariant();
        if (type is "list" or "array" or "tuple")
            return null;

        if (MapAssetToken(nodeType.TypeName, includeTextReference: true) is { } named)
            return named;

        return MapAssetToken(nodeType.Type, includeTextReference: false);
    }

    private static (Type, object)? MapAssetToken(
        string? value,
        bool includeTextReference)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var qualifiedToken = value
            .Trim()
            .Split(new[] { '.', '+', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? value;
        var token = new string(qualifiedToken
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
        return token switch
        {
            "image" or "imageref" => (typeof(ImageRef), new ImageRef()),
            "audio" or "audioref" => (typeof(AudioRef), new AudioRef()),
            "video" or "videoref" => (typeof(VideoRef), new VideoRef()),
            "document" or "documentref" => (typeof(DocumentRef), new DocumentRef()),
            "asset" or "assetref" => (typeof(GenericAssetRef), new GenericAssetRef()),
            "folder" or "folderref" => (typeof(FolderRef), new FolderRef()),
            "modelref" => (typeof(ModelRef), new ModelRef()),
            "model3d" or "model3dref" => (typeof(Model3DRef), new Model3DRef()),
            "font" or "fontref" => (typeof(FontRef), new FontRef()),
            "textref" when includeTextReference => (typeof(TextRef), new TextRef()),
            _ => null
        };
    }

    private static (Type, object) MapCollection(NodeTypeDefinition nodeType)
    {
        var elementDefinition = nodeType.TypeArgs?.FirstOrDefault();
        var elementType = elementDefinition == null
            ? typeof(object)
            : MapNodeType(elementDefinition).Item1 ?? typeof(object);
        var spreadType = typeof(Spread<>).MakeGenericType(elementType);
        return (spreadType, VlValueConversion.CreateEmptySpread(elementType));
    }

    private enum Kind
    {
        Unknown,
        Numeric,
        Boolean,
        String,
        Object,
    }

    private static Kind Classify(NodeTypeDefinition nodeType)
    {
        // If server provides a helpful "type_name", use it.
        if (!string.IsNullOrWhiteSpace(nodeType.TypeName))
        {
            var n = nodeType.TypeName.Trim().ToLowerInvariant();
            if (n is "number" or "float" or "int" or "integer")
                return Kind.Numeric;
            if (n is "bool" or "boolean")
                return Kind.Boolean;
            if (n is "str" or "string" or "text")
                return Kind.String;
        }

        // Inspect type args (common for "any"/union: e.g., [int, float]).
        if (nodeType.TypeArgs != null && nodeType.TypeArgs.Count > 0)
        {
            var hasNumeric = nodeType.TypeArgs.Any(IsNumericish);
            var hasBool = nodeType.TypeArgs.Any(IsBoolish);
            var hasString = nodeType.TypeArgs.Any(IsStringish);

            // If it can be numeric, prefer float for VL (covers both int + float well).
            if (hasNumeric && !hasString && !hasBool)
                return Kind.Numeric;

            if (hasBool && !hasNumeric && !hasString)
                return Kind.Boolean;

            if (hasString && !hasNumeric && !hasBool)
                return Kind.String;

            if (hasNumeric && !hasString) // numeric + maybe other non-string → still prefer numeric (math ops)
                return Kind.Numeric;

            // Mixed unions: fall back to object for safety (caller can stringify if needed).
            return Kind.Object;
        }

        return Kind.Unknown;
    }

    private static bool IsNumericish(NodeTypeDefinition t)
    {
        var type = t.Type?.Trim().ToLowerInvariant() ?? "";
        if (type is "float" or "number" or "int" or "integer")
            return true;
        return t.TypeArgs != null && t.TypeArgs.Any(IsNumericish);
    }

    private static bool IsBoolish(NodeTypeDefinition t)
    {
        var type = t.Type?.Trim().ToLowerInvariant() ?? "";
        if (type is "bool" or "boolean")
            return true;
        return t.TypeArgs != null && t.TypeArgs.Any(IsBoolish);
    }

    private static bool IsStringish(NodeTypeDefinition t)
    {
        var type = t.Type?.Trim().ToLowerInvariant() ?? "";
        if (type is "str" or "string" or "text")
            return true;
        return t.TypeArgs != null && t.TypeArgs.Any(IsStringish);
    }
}


