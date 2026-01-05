using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlTypeMapping
{
    public static (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
    {
        if (nodeType == null || string.IsNullOrWhiteSpace(nodeType.Type))
            return (typeof(string), "");

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
                _ => (typeof(string), "")
            };
        }

        return t switch
        {
            "str" or "string" => (typeof(string), ""),
            "int" or "integer" => (typeof(int), 0),
            "float" or "number" => (typeof(float), 0.0f),
            "bool" or "boolean" => (typeof(bool), false),
            "list" or "array" => (typeof(string[]), Array.Empty<string>()),
            "dict" or "object" => (typeof(object), null),
            // Node-level image/audio/video nodes currently use path-or-ref strings in VL.
            "image" => (typeof(string), ""),
            "audio" => (typeof(string), ""),
            "video" => (typeof(string), ""),
            _ => (typeof(string), "")
        };
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


