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

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonIgnore]
    public WorkflowInterfaceResponse? Interface { get; set; }

    [JsonIgnore]
    public string WorkflowRevision { get; set; } = string.Empty;

    [JsonIgnore]
    public long? RegistryRevision { get; set; }

    /// <summary>
    /// Get input properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description, object? DefaultValue)> GetInputProperties()
    {
        if (Interface == null)
            yield break;

        foreach (var input in Interface.Inputs)
        {
            yield return (
                Name: input.Name,
                Type: ConvertType(input.Type),
                Description: input.Description,
                DefaultValue: input.Default.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                    ? null
                    : input.Default
            );
        }
    }

    /// <summary>
    /// Get output properties as TypeMetadata for VL pin creation
    /// </summary>
    public IEnumerable<(string Name, TypeMetadata Type, string Description)> GetOutputProperties()
    {
        if (Interface == null)
            yield break;

        foreach (var output in Interface.Outputs)
        {
            yield return (
                Name: output.Name,
                Type: ConvertType(output.Type),
                Description: output.Description
            );
        }
    }

    private static TypeMetadata ConvertType(NodeTypeDefinition type)
    {
        return new TypeMetadata
        {
            Type = type.Type,
            Optional = type.Optional,
            Values = type.Values,
            TypeName = type.TypeName,
            TypeArgs = type.TypeArgs?.Select(ConvertType).ToList() ?? new List<TypeMetadata>()
        };
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
