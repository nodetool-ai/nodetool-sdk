using System.Text.Json.Serialization;
using System.Text.Json;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.VL.Models;

/// <summary>
/// Represents a workflow's input or output schema definition
/// </summary>
public class WorkflowSchemaDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, WorkflowPropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();

    // Root schema refs/wrappers (some workflows return schemas as anyOf/oneOf/allOf at the root)
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }

    // JSON Schema definitions (used by $ref)
    [JsonPropertyName("definitions")]
    public Dictionary<string, WorkflowPropertyDefinition>? Definitions { get; set; }

    [JsonPropertyName("$defs")]
    public Dictionary<string, WorkflowPropertyDefinition>? Defs { get; set; }

    [JsonPropertyName("anyOf")]
    public List<WorkflowPropertyDefinition>? AnyOf { get; set; }

    [JsonPropertyName("oneOf")]
    public List<WorkflowPropertyDefinition>? OneOf { get; set; }

    [JsonPropertyName("allOf")]
    public List<WorkflowPropertyDefinition>? AllOf { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Represents a property definition within a workflow schema
/// </summary>
public class WorkflowPropertyDefinition
{
    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("enum")]
    public List<object>? Enum { get; set; }

    [JsonPropertyName("const")]
    public object? Const { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, WorkflowPropertyDefinition>? Properties { get; set; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    [JsonPropertyName("items")]
    public WorkflowPropertyDefinition? Items { get; set; }

    // NodeTool schemas may wrap refs in anyOf/oneOf/allOf (e.g., nullable refs).
    // If we don't model these, System.Text.Json will drop the info and adapters (image/audio) won't trigger.
    [JsonPropertyName("anyOf")]
    public List<WorkflowPropertyDefinition>? AnyOf { get; set; }

    [JsonPropertyName("oneOf")]
    public List<WorkflowPropertyDefinition>? OneOf { get; set; }

    [JsonPropertyName("allOf")]
    public List<WorkflowPropertyDefinition>? AllOf { get; set; }

    /// <summary>
    /// Convert this property definition to SDK TypeMetadata
    /// </summary>
    public TypeMetadata ToTypeMetadata()
    {
        var metadata = new TypeMetadata
        {
            Type = Type ?? "any",
            Optional = !IsRequired(),
            TypeName = Title
        };

        // Handle enum types
        if (Enum != null && Enum.Count > 0)
        {
            metadata.Type = "enum";
            metadata.Values = Enum;
        }

        // Handle array types
        if (Type == "array" && Items != null)
        {
            metadata.Type = "list";
            metadata.TypeArgs.Add(Items.ToTypeMetadata());
        }

        // Handle object types with properties
        if (Type == "object" && Properties != null && Properties.Count > 0)
        {
            metadata.Type = "dict";
            // For complex objects, we could add more sophisticated type args
            metadata.TypeArgs.Add(new TypeMetadata { Type = "str" });
            metadata.TypeArgs.Add(new TypeMetadata { Type = "any" });
        }

        return metadata;
    }

    private bool IsRequired()
    {
        // This will be set by the parent schema's required array
        // For now, assume non-required unless specified
        return false;
    }
}

/// <summary>
/// Detailed workflow information including schemas
/// </summary>
public class WorkflowDetail
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("input_schema")]
    public WorkflowSchemaDefinition? InputSchema { get; set; }

    [JsonPropertyName("output_schema")]
    public WorkflowSchemaDefinition? OutputSchema { get; set; }

    // Optional full graph (used for output type inference when output_schema is "any")
    [JsonPropertyName("graph")]
    public WorkflowGraph? Graph { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    // Typed workflow I/O metadata (present when fetched with include_type_metadata=1)
    public Dictionary<string, TypeMetadata>? InputTypeMetadata { get; set; }
    public Dictionary<string, TypeMetadata>? OutputTypeMetadata { get; set; }

    public bool TryGetInputType(string name, out TypeMetadata typeMetadata)
    {
        typeMetadata = new TypeMetadata { Type = "any" };
        return InputTypeMetadata != null && InputTypeMetadata.TryGetValue(name, out typeMetadata);
    }

    public bool TryGetOutputType(string name, out TypeMetadata typeMetadata)
    {
        typeMetadata = new TypeMetadata { Type = "any" };
        return OutputTypeMetadata != null && OutputTypeMetadata.TryGetValue(name, out typeMetadata);
    }

    /// <summary>
    /// Get input properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description, object? DefaultValue)> GetInputProperties()
    {
        // Preferred (Phase 2): backend-supplied typed metadata, merged with schema defaults/required/description.
        if (InputTypeMetadata != null && InputTypeMetadata.Count > 0)
        {
            var required = InputSchema?.Required ?? new List<string>();
            var schemaProps = InputSchema?.Properties ?? new Dictionary<string, WorkflowPropertyDefinition>(StringComparer.Ordinal);

            // Prefer schema ordering when available (stable UX).
            var names = schemaProps.Count > 0
                ? schemaProps.Keys
                : InputTypeMetadata.Keys;

            foreach (var name in names)
            {
                if (!InputTypeMetadata.TryGetValue(name, out var tm))
                    continue;

                var meta = tm;
                meta.Optional = !required.Contains(name);

                if (schemaProps.TryGetValue(name, out var prop))
                {
                    yield return (
                        Name: name,
                        Type: meta,
                        Description: prop.Description ?? prop.Title ?? "",
                        DefaultValue: prop.Default
                    );
                }
                else
                {
                    yield return (Name: name, Type: meta, Description: "", DefaultValue: null);
                }
            }
            yield break;
        }

        // Preferred: schema-driven inputs
        if (InputSchema?.Properties != null && InputSchema.Properties.Count > 0)
        {
            var required = InputSchema.Required ?? new List<string>();
            foreach (var prop in InputSchema.Properties)
            {
                var metadata = prop.Value.ToTypeMetadata();
                metadata.Optional = !required.Contains(prop.Key);

                yield return (
                    Name: prop.Key,
                    Type: metadata,
                    Description: prop.Value.Description ?? prop.Value.Title ?? "",
                    DefaultValue: prop.Value.Default
                );
            }
            yield break;
        }

        // Fallback: infer inputs from graph when schema is missing/empty (some workflows return root anyOf/$ref schemas)
        if (Graph?.Nodes == null)
            yield break;

        foreach (var node in Graph.Nodes)
        {
            if (node == null)
                continue;

            if (!TryInferInputTypeFromGraphNode(node.Type, out var inferredType))
                continue;

            if (!TryGetNodeName(node.Data, out var name) || string.IsNullOrWhiteSpace(name))
                continue;

            var meta = new TypeMetadata { Type = inferredType, Optional = false };
            string desc = "";
            if (TryGetNodeDescription(node.Data, out var d) && !string.IsNullOrWhiteSpace(d))
                desc = d!;

            yield return (Name: name!, Type: meta, Description: desc, DefaultValue: null);
        }
    }

    /// <summary>
    /// Get output properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description)> GetOutputProperties()
    {
        // Preferred (Phase 2): backend-supplied typed metadata, merged with schema descriptions when available.
        if (OutputTypeMetadata != null && OutputTypeMetadata.Count > 0)
        {
            var schemaProps = OutputSchema?.Properties ?? new Dictionary<string, WorkflowPropertyDefinition>(StringComparer.Ordinal);

            // Prefer schema ordering when available.
            var names = schemaProps.Count > 0
                ? schemaProps.Keys
                : OutputTypeMetadata.Keys;

            foreach (var name in names)
            {
                if (!OutputTypeMetadata.TryGetValue(name, out var tm))
                    continue;

                if (schemaProps.TryGetValue(name, out var prop))
                {
                    yield return (
                        Name: name,
                        Type: tm,
                        Description: prop.Description ?? prop.Title ?? prop.Label ?? ""
                    );
                }
                else
                {
                    yield return (Name: name, Type: tm, Description: "");
                }
            }
            yield break;
        }

        // Preferred: schema-driven outputs
        if (OutputSchema?.Properties != null && OutputSchema.Properties.Count > 0)
        {
            foreach (var prop in OutputSchema.Properties)
            {
                var metadata = prop.Value.ToTypeMetadata();

                // Prefer output_schema for inference when possible (better than graph heuristics).
                if (TryInferOutputTypeFromSchema(prop.Value, OutputSchema, out var schemaInferred))
                {
                    metadata.Type = schemaInferred;
                }

                // If output schema is too generic ("any"), try to infer type from workflow graph edges.
                if (string.Equals(metadata.Type, "any", StringComparison.OrdinalIgnoreCase) &&
                    TryInferOutputTypeFromGraph(prop.Key, out var inferred))
                {
                    metadata.Type = inferred;
                }

                // If it's still "any", default to string for VL ergonomics.
                if (string.Equals(metadata.Type, "any", StringComparison.OrdinalIgnoreCase))
                {
                    metadata.Type = "string";
                }

                yield return (
                    Name: prop.Key,
                    Type: metadata,
                    Description: prop.Value.Description ?? prop.Value.Title ?? ""
                );
            }
            yield break;
        }

        // Fallback: infer outputs from graph when schema is missing/empty.
        if (Graph?.Nodes == null)
            yield break;

        var outputNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Graph.Nodes)
        {
            if (!string.Equals(node.Type, "nodetool.output.Output", StringComparison.Ordinal))
                continue;
            if (TryGetNodeName(node.Data, out var name) && !string.IsNullOrWhiteSpace(name))
                outputNames.Add(name!);
        }

        foreach (var name in outputNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            var meta = new TypeMetadata { Type = "string", Optional = false };
            if (TryInferOutputTypeFromGraph(name, out var inferred))
                meta.Type = inferred;
            yield return (Name: name, Type: meta, Description: "");
        }
    }

    private static bool TryInferInputTypeFromGraphNode(string nodeType, out string inferredType)
    {
        inferredType = "";
        if (string.IsNullOrWhiteSpace(nodeType))
            return false;

        // Only infer for known input nodes
        return nodeType switch
        {
            "nodetool.input.StringInput" => Set("string", out inferredType),
            "nodetool.input.BooleanInput" => Set("bool", out inferredType),
            "nodetool.input.IntegerInput" => Set("int", out inferredType),
            "nodetool.input.FloatInput" => Set("float", out inferredType),
            "nodetool.input.ImageInput" => Set("image", out inferredType),
            "nodetool.input.AudioInput" => Set("audio", out inferredType),
            "nodetool.input.FilePathInput" => Set("string", out inferredType),
            _ => false
        };

        static bool Set(string s, out string inferred)
        {
            inferred = s;
            return true;
        }
    }

    private static bool TryGetNodeDescription(JsonElement data, out string? description)
    {
        description = null;
        try
        {
            if (data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("description", out var d) &&
                d.ValueKind == JsonValueKind.String)
            {
                description = d.GetString();
                return !string.IsNullOrWhiteSpace(description);
            }
        }
        catch
        {
            // ignore
        }
        return false;
    }

    private static bool TryInferOutputTypeFromSchema(
        WorkflowPropertyDefinition prop,
        WorkflowSchemaDefinition rootSchema,
        out string inferredType)
    {
        inferredType = "";
        return TryInferOutputTypeFromSchemaRecursive(prop, rootSchema, depth: 0, out inferredType);
    }

    private static bool TryInferOutputTypeFromSchemaRecursive(
        WorkflowPropertyDefinition? prop,
        WorkflowSchemaDefinition rootSchema,
        int depth,
        out string inferredType)
    {
        inferredType = "";
        if (prop == null || depth > 12)
            return false;

        // $ref resolution (best-effort; only works if schema includes definitions/$defs)
        if (!string.IsNullOrWhiteSpace(prop.Ref))
        {
            var resolved = ResolveRef(rootSchema, prop.Ref!);
            if (resolved != null)
                return TryInferOutputTypeFromSchemaRecursive(resolved, rootSchema, depth + 1, out inferredType);
        }

        // anyOf/oneOf/allOf wrappers
        if (prop.AnyOf != null)
        {
            foreach (var p in prop.AnyOf)
                if (TryInferOutputTypeFromSchemaRecursive(p, rootSchema, depth + 1, out inferredType))
                    return true;
        }
        if (prop.OneOf != null)
        {
            foreach (var p in prop.OneOf)
                if (TryInferOutputTypeFromSchemaRecursive(p, rootSchema, depth + 1, out inferredType))
                    return true;
        }
        if (prop.AllOf != null)
        {
            foreach (var p in prop.AllOf)
                if (TryInferOutputTypeFromSchemaRecursive(p, rootSchema, depth + 1, out inferredType))
                    return true;
        }

        // Strong hint: format
        if (!string.IsNullOrWhiteSpace(prop.Format))
        {
            var fmt = prop.Format.Trim().ToLowerInvariant();
            if (fmt is "image" or "audio" or "video")
            {
                inferredType = fmt;
                return true;
            }
        }

        // Primitive types
        if (!string.IsNullOrWhiteSpace(prop.Type))
        {
            var t = prop.Type.Trim().ToLowerInvariant();
            if (t is "string" or "str")
            {
                inferredType = "string";
                return true;
            }
        }

        // Common NodeTool ref schema: { type:"object", properties:{ type:{enum:["image"]}, uri:{format:"uri"} } }
        if (string.Equals(prop.Type, "object", StringComparison.OrdinalIgnoreCase) &&
            prop.Properties != null &&
            prop.Properties.TryGetValue("type", out var typeProp))
        {
            // const "image"
            if (typeProp.Const is string cs && !string.IsNullOrWhiteSpace(cs))
            {
                var s = cs.Trim().ToLowerInvariant();
                if (s is "image" or "audio" or "video")
                {
                    inferredType = s;
                    return true;
                }
            }

            // enum ["image"]
            if (typeProp.Enum != null)
            {
                foreach (var v in typeProp.Enum)
                {
                    var s = v?.ToString()?.Trim().ToLowerInvariant();
                    if (s is "image" or "audio" or "video")
                    {
                        inferredType = s;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static WorkflowPropertyDefinition? ResolveRef(WorkflowSchemaDefinition root, string refStr)
    {
        if (!refStr.StartsWith("#/", StringComparison.Ordinal))
            return null;

        // "#/definitions/X" or "#/$defs/X"
        var path = refStr.Substring(2);
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && string.Equals(parts[0], "definitions", StringComparison.Ordinal))
        {
            return root.Definitions != null && root.Definitions.TryGetValue(parts[1], out var def) ? def : null;
        }
        if (parts.Length == 2 && string.Equals(parts[0], "$defs", StringComparison.Ordinal))
        {
            return root.Defs != null && root.Defs.TryGetValue(parts[1], out var def) ? def : null;
        }
        return null;
    }

    private bool TryInferOutputTypeFromGraph(string outputName, out string inferredType)
    {
        inferredType = "";
        if (Graph?.Nodes == null || Graph.Edges == null)
            return false;

        // Find nodetool.output.Output nodes with data.name == outputName
        var outputNodeIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in Graph.Nodes)
        {
            if (!string.Equals(node.Type, "nodetool.output.Output", StringComparison.Ordinal))
                continue;

            if (TryGetNodeName(node.Data, out var name) && string.Equals(name, outputName, StringComparison.Ordinal))
                outputNodeIds.Add(node.Id);
        }

        if (outputNodeIds.Count == 0)
            return false;

        // Find an incoming edge and use its ui_properties.className as a type hint (e.g. "image").
        foreach (var edge in Graph.Edges)
        {
            if (!outputNodeIds.Contains(edge.Target))
                continue;

            var cls = edge.UiProperties?.ClassName;
            if (string.IsNullOrWhiteSpace(cls))
                continue;

            // Normalize a few common classNames to our type strings.
            var c = cls.Trim().ToLowerInvariant();
            if (c == "image")
            {
                inferredType = "image";
                return true;
            }
            if (c == "audio")
            {
                inferredType = "audio";
                return true;
            }
            if (c == "video")
            {
                inferredType = "video";
                return true;
            }
            if (c == "text")
            {
                inferredType = "string";
                return true;
            }
            if (c == "chunk")
            {
                inferredType = "string";
                return true;
            }
        }

        return false;
    }

    private static bool TryGetNodeName(JsonElement data, out string? name)
    {
        name = null;
        try
        {
            if (data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("name", out var n) &&
                n.ValueKind == JsonValueKind.String)
            {
                name = n.GetString();
                return !string.IsNullOrWhiteSpace(name);
            }
        }
        catch
        {
            // ignore
        }
        return false;
    }

    /// <summary>
    /// Generate a node name suitable for VL
    /// </summary>
    public string GetVLNodeName()
    {
        return $"Execute{SanitizeName(Name)}";
    }

    /// <summary>
    /// Generate a category for VL node organization
    /// </summary>
    public string GetVLCategory()
    {
        var category = "Nodetool.Workflows";
        
        if (Tags != null && Tags.Count > 0)
        {
            var primaryTag = Tags.FirstOrDefault(t => !string.IsNullOrEmpty(t));
            if (primaryTag != null)
            {
                category += $".{SanitizeName(primaryTag)}";
            }
        }

        return category;
    }

    private static string SanitizeName(string name)
    {
        // Remove or replace characters that are not suitable for VL node names
        return System.Text.RegularExpressions.Regex.Replace(name, @"[^\w\s]", "")
            .Replace(" ", "")
            .Trim();
    }
} 