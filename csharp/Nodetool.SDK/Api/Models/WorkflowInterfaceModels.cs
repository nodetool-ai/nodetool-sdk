using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public sealed class WorkflowSummaryResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;

    [JsonPropertyName("registry_revision")]
    public long? RegistryRevision { get; set; }

    [JsonPropertyName("run_mode")]
    public string? RunMode { get; set; }
}

public sealed class WorkflowSummaryListResponse
{
    [JsonPropertyName("workflows")]
    public List<WorkflowSummaryResponse> Workflows { get; set; } = new();

    [JsonPropertyName("next")]
    public string? Next { get; set; }
}

public sealed class WorkflowInterfaceDiagnostic
{
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    [JsonPropertyName("pin_name")]
    public string? PinName { get; set; }
}

public abstract class WorkflowInterfacePin
{
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public NodeTypeDefinition Type { get; set; } = new();
}

public sealed class WorkflowInterfaceInput : WorkflowInterfacePin
{
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("default")]
    public JsonElement Default { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }
}

public sealed class WorkflowInterfaceOutput : WorkflowInterfacePin
{
    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

public sealed class WorkflowInterfaceResponse
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    [JsonPropertyName("etag")]
    public string? Etag { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("inputs")]
    public List<WorkflowInterfaceInput> Inputs { get; set; } = new();

    [JsonPropertyName("outputs")]
    public List<WorkflowInterfaceOutput> Outputs { get; set; } = new();

    [JsonPropertyName("diagnostics")]
    public List<WorkflowInterfaceDiagnostic> Diagnostics { get; set; } = new();
}

public sealed class WorkflowInterfacesRequest
{
    [JsonPropertyName("ids")]
    public IReadOnlyCollection<string> Ids { get; set; } = Array.Empty<string>();

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;
}

public sealed class WorkflowInterfaceItemError
{
    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public sealed class WorkflowInterfacesResponse
{
    [JsonPropertyName("interfaces")]
    public List<WorkflowInterfaceResponse> Interfaces { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<WorkflowInterfaceItemError> Errors { get; set; } = new();
}

internal sealed class ApiErrorResponse
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
