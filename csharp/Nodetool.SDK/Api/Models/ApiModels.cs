using System.Text.Json.Serialization;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.Api.Models;

/// <summary>
/// Type definition for node properties and outputs
/// </summary>
public class NodeTypeDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("optional")]
    public bool Optional { get; set; } = false;

    [JsonPropertyName("values")]
    public List<object>? Values { get; set; }

    [JsonPropertyName("type_args")]
    public List<NodeTypeDefinition>? TypeArgs { get; set; }

    [JsonPropertyName("type_name")]
    public string? TypeName { get; set; }
}

/// <summary>
/// Node property definition
/// </summary>
public class NodeProperty
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public NodeTypeDefinition Type { get; set; } = new();

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("min")]
    public object? Min { get; set; }

    [JsonPropertyName("max")]
    public object? Max { get; set; }
}

/// <summary>
/// Node output definition
/// </summary>
public class NodeOutput
{
    [JsonPropertyName("type")]
    public NodeTypeDefinition Type { get; set; } = new();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// Response model for node metadata - matches actual API structure
/// </summary>
public class NodeMetadataResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;

    [JsonPropertyName("layout")]
    public string Layout { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public List<NodeProperty> Properties { get; set; } = new();

    [JsonPropertyName("outputs")]
    public List<NodeOutput> Outputs { get; set; } = new();

    [JsonPropertyName("the_model_info")]
    public Dictionary<string, object> ModelInfo { get; set; } = new();

    [JsonPropertyName("recommended_models")]
    public List<object> RecommendedModels { get; set; } = new();

    [JsonPropertyName("basic_fields")]
    public List<string> BasicFields { get; set; } = new();

    [JsonPropertyName("is_dynamic")]
    public bool IsDynamic { get; set; } = false;

    [JsonPropertyName("is_streaming")]
    public bool IsStreaming { get; set; } = false;
}

/// <summary>
/// Property metadata for nodes (deprecated - use NodeProperty instead)
/// </summary>
[Obsolete("Use NodeProperty instead - this will be removed")]
public class PropertyMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("type")]
    public TypeMetadata Type { get; set; } = new();
    
    [JsonPropertyName("default")]
    public object? Default { get; set; }
    
    [JsonPropertyName("required")]
    public bool Required { get; set; } = false;
}

/// <summary>
/// Response model for workflows
/// </summary>
public class WorkflowResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("input_schema")]
    public SchemaDefinition? InputSchema { get; set; }
    
    [JsonPropertyName("output_schema")]
    public SchemaDefinition? OutputSchema { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Schema definition for workflow inputs/outputs
/// </summary>
public class SchemaDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, PropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Property definition within a schema
/// </summary>
public class PropertyDefinition
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
    public Dictionary<string, PropertyDefinition>? Properties { get; set; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    [JsonPropertyName("items")]
    public PropertyDefinition? Items { get; set; }
}

/// <summary>
/// Response model for assets
/// </summary>
public class AssetResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;
    
    [JsonPropertyName("size")]
    public long Size { get; set; }
    
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response model for jobs
/// </summary>
public class JobResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("progress")]
    public double Progress { get; set; }
    
    [JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("started_at")]
    public DateTime? StartedAt { get; set; }
    
    [JsonPropertyName("completed_at")]
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Response model for workflow execution
/// </summary>
public class WorkflowExecutionRequest
{
    [JsonPropertyName("params")]
    public Dictionary<string, object> Params { get; set; } = new();
}

/// <summary>
/// Response model for workflow list endpoint
/// </summary>
public class WorkflowListResponse
{
    [JsonPropertyName("workflows")]
    public List<WorkflowResponse> Workflows { get; set; } = new();

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

/// <summary>
/// Request model for node execution
/// </summary>
public class NodeExecutionRequest
{
    [JsonPropertyName("node_type")]
    public string NodeType { get; set; } = string.Empty;
    
    [JsonPropertyName("inputs")]
    public Dictionary<string, object> Inputs { get; set; } = new();
} 