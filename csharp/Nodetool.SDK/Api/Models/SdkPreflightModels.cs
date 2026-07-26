using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nodetool.SDK.Api.Models;

public static class SdkPreflightLevels
{
    public const string Static = "static";
    public const string Availability = "availability";
    public const string Execution = "execution";

    public static bool IsValid(string level) =>
        level is Static or Availability or Execution;
}

public static class SdkExecutionTargetKinds
{
    public const string Local = "local";
    public const string Worker = "worker";
    public const string Runner = "runner";
}

public sealed class SdkExecutionTarget
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = SdkExecutionTargetKinds.Local;

    [JsonPropertyName("worker_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WorkerId { get; set; }

    [JsonPropertyName("runner_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RunnerId { get; set; }

    [JsonPropertyName("concurrent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Concurrent { get; set; }
}

public sealed class SdkPreflightRequest
{
    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    [JsonPropertyName("workspace_id")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("workflow_etag")]
    public string? WorkflowEtag { get; set; }

    [JsonPropertyName("interface_version")]
    public int InterfaceVersion { get; set; } = 1;

    [JsonPropertyName("level")]
    public string Level { get; set; } = SdkPreflightLevels.Static;

    [JsonPropertyName("inputs")]
    public Dictionary<string, object?> Inputs { get; set; } =
        new(StringComparer.Ordinal);

    [JsonPropertyName("execution_target")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SdkExecutionTarget? ExecutionTarget { get; set; }
}

public sealed class SdkPreflightResponse
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("workflow_id")]
    public string WorkflowId { get; set; } = string.Empty;

    [JsonPropertyName("workflow_etag")]
    public string? WorkflowEtag { get; set; }

    [JsonPropertyName("runnable")]
    public bool Runnable { get; set; }

    [JsonPropertyName("issues")]
    public List<SdkPreflightIssue> Issues { get; set; } = new();

    [JsonPropertyName("requirements")]
    public List<SdkPreflightRequirement> Requirements { get; set; } = new();

    [JsonPropertyName("cost")]
    public SdkPreflightCost? Cost { get; set; }
}

public sealed class SdkPreflightIssue
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

public sealed class SdkPreflightRequirement
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("blocking")]
    public bool Blocking { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public Dictionary<string, JsonElement>? Details { get; set; }
}

public sealed class SdkPreflightCost
{
    [JsonPropertyName("amount")]
    public double? Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;

    [JsonPropertyName("unknown_cost_nodes")]
    public List<string> UnknownCostNodes { get; set; } = new();

    [JsonPropertyName("approval_required")]
    public bool ApprovalRequired { get; set; }
}
