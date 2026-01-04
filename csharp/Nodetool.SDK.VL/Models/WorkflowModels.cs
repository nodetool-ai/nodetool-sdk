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

    // JSON Schema definitions (used by $ref)
    [JsonPropertyName("definitions")]
    public Dictionary<string, WorkflowPropertyDefinition>? Definitions { get; set; }

    [JsonPropertyName("$defs")]
    public Dictionary<string, WorkflowPropertyDefinition>? Defs { get; set; }

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

    /// <summary>
    /// Get input properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description, object? DefaultValue)> GetInputProperties()
    {
        if (InputSchema?.Properties == null)
            yield break;

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
    }

    /// <summary>
    /// Get output properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description)> GetOutputProperties()
    {
        if (OutputSchema?.Properties == null)
            yield break;

        foreach (var prop in OutputSchema.Properties)
        {
            var metadata = prop.Value.ToTypeMetadata();

            // If output schema is too generic ("any"), try to infer type from workflow graph edges.
            if (string.Equals(metadata.Type, "any", StringComparison.OrdinalIgnoreCase) &&
                TryInferOutputTypeFromGraph(prop.Key, out var inferred))
            {
                metadata.Type = inferred;
            }

            yield return (
                Name: prop.Key,
                Type: metadata,
                Description: prop.Value.Description ?? prop.Value.Title ?? ""
            );
        }
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