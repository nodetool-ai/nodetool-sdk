using System.Text.Json.Serialization;
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

            yield return (
                Name: prop.Key,
                Type: metadata,
                Description: prop.Value.Description ?? prop.Value.Title ?? ""
            );
        }
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