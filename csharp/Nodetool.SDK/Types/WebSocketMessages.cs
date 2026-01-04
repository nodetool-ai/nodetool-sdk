using MessagePack;

namespace Nodetool.SDK.Types;

/// <summary>
/// WebSocket message indicating job status changes.
/// </summary>
[MessagePackObject(true)]
public class JobUpdate
{
    public string type { get; set; } = "job_update";

    public string status { get; set; } = "";

    public string? job_id { get; set; } = null;

    public string? workflow_id { get; set; } = null;

    public string? message { get; set; } = null;

    public Dictionary<string, object>? result { get; set; } = null;

    public string? error { get; set; } = null;

    public string? traceback { get; set; } = null;

    public Dictionary<string, object>? run_state { get; set; } = null;
}

/// <summary>
/// WebSocket message indicating node execution status and results.
/// </summary>
[MessagePackObject(true)]
public class NodeUpdate
{
    public string type { get; set; } = "node_update";

    public string? job_id { get; set; } = null;

    public string? workflow_id { get; set; } = null;

    public string node_id { get; set; } = "";

    public string node_name { get; set; } = "";

    public string? node_type { get; set; } = null;

    public string status { get; set; } = "";

    public string? error { get; set; } = null;

    public Dictionary<string, object>? result { get; set; } = null;

    public Dictionary<string, object>? properties { get; set; } = null;
}

/// <summary>
/// WebSocket message for streaming node output values during execution.
/// </summary>
[MessagePackObject(true)]
public class OutputUpdate
{
    public string type { get; set; } = "output_update";

    public string? job_id { get; set; } = null;

    public string? workflow_id { get; set; } = null;

    public string node_id { get; set; } = "";

    public string node_name { get; set; } = "";

    public string output_name { get; set; } = "";

    public object? value { get; set; } = null;

    public string output_type { get; set; } = "";

    public Dictionary<string, object>? metadata { get; set; } = null;
}

/// <summary>
/// WebSocket message for streaming preview values during execution.
///
/// Note: value is intentionally untyped (OpenAPI uses unknown).
/// </summary>
[MessagePackObject(true)]
public class PreviewUpdate
{
    public string type { get; set; } = "preview_update";

    public string? job_id { get; set; } = null;

    public string? workflow_id { get; set; } = null;

    public string node_id { get; set; } = "";

    public object? value { get; set; } = null;
}

/// <summary>
/// Base interface for all WebSocket messages from NodeTool.
/// </summary>
public interface IWebSocketMessage
{
    string type { get; }
}

/// <summary>
/// WebSocket message for workflow execution requests.
/// </summary>
[MessagePackObject(true)]
[Obsolete("Legacy message shape. Use the worker protocol: WebSocketCommand {command/type: run_job, data: RunJobRequest}.")]
public class WorkflowExecuteRequest
{
    public string type { get; set; } = "workflow_execute";

    public string workflow_id { get; set; } = "";

    public Dictionary<string, object> inputs { get; set; } = new();

    public string? job_id { get; set; } = null;

    public Dictionary<string, object>? options { get; set; } = null;
}

/// <summary>
/// WebSocket message for single node execution requests.
/// </summary>
[MessagePackObject(true)]
[Obsolete("Legacy message shape. Use the worker protocol: WebSocketCommand {command/type: run_job, data: RunJobRequest} with a minimal graph.")]
public class NodeExecuteRequest
{
    public string type { get; set; } = "node_execute";

    public string node_type { get; set; } = "";

    public Dictionary<string, object> inputs { get; set; } = new();

    public string? node_id { get; set; } = null;

    public Dictionary<string, object>? properties { get; set; } = null;
}

/// <summary>
/// WebSocket message for job cancellation requests.
/// </summary>
[MessagePackObject(true)]
[Obsolete("Legacy message shape. Use the worker protocol: WebSocketCommand {command/type: cancel_job, data: CancelJobData}.")]
public class JobCancelRequest
{
    public string type { get; set; } = "job_cancel";

    public string job_id { get; set; } = "";
}

/// <summary>
/// Canonical cancel_job payload for the worker protocol.
/// </summary>
[MessagePackObject(true)]
public class CancelJobData
{
    public string job_id { get; set; } = "";

    public string? workflow_id { get; set; } = null;
}

/// <summary>
/// WebSocket message for connection status and authentication.
/// </summary>
[MessagePackObject(true)]
public class ConnectionStatus
{
    public string type { get; set; } = "connection_status";

    public string status { get; set; } = "";

    public string? message { get; set; } = null;

    public Dictionary<string, object>? metadata { get; set; } = null;
}

/// <summary>
/// WebSocket message for error notifications.
/// </summary>
[MessagePackObject(true)]
public class ErrorMessage
{
    public string type { get; set; } = "error";

    public string error_code { get; set; } = "";

    public string message { get; set; } = "";

    public Dictionary<string, object>? details { get; set; } = null;
}

/// <summary>
/// WebSocket message for progress updates with percentage and status.
/// </summary>
[MessagePackObject(true)]
public class ProgressUpdate
{
    public string type { get; set; } = "progress_update";

    public string? job_id { get; set; } = null;

    public string? node_id { get; set; } = null;

    public double progress { get; set; } = 0.0;

    public string? status_message { get; set; } = null;

    public int? total_steps { get; set; } = null;

    public int? current_step { get; set; } = null;
}

/// <summary>
/// NodeProgress message from the server indicating node execution progress.
/// </summary>
[MessagePackObject(true)]
public class NodeProgress
{
    public string type { get; set; } = "node_progress";

    public string? job_id { get; set; } = null;

    public string? workflow_id { get; set; } = null;

    public string node_id { get; set; } = "";

    public int progress { get; set; } = 0;

    public int total { get; set; } = 100;

    public string chunk { get; set; } = "";
}

/// <summary>
/// Graph structure for workflow execution.
/// </summary>
[MessagePackObject(true)]
public class Graph
{
    public List<GraphNode> nodes { get; set; } = new();

    public List<GraphEdge> edges { get; set; } = new();
}

/// <summary>
/// Node in a workflow graph.
/// </summary>
[MessagePackObject(true)]
public class GraphNode
{
    public string id { get; set; } = "";

    public string type { get; set; } = "";

    public Dictionary<string, object> data { get; set; } = new();

    public string? parent_id { get; set; } = null;

    public Dictionary<string, object> ui_properties { get; set; } = new();

    public Dictionary<string, object> dynamic_properties { get; set; } = new();

    public string sync_mode { get; set; } = "on_any";
}

/// <summary>
/// Edge connecting two nodes in a workflow graph.
/// </summary>
[MessagePackObject(true)]
public class GraphEdge
{
    public string? id { get; set; } = null;

    public string source { get; set; } = "";

    public string sourceHandle { get; set; } = "";

    public string target { get; set; } = "";

    public string targetHandle { get; set; } = "";
}

/// <summary>
/// Run job request sent to the server.
///
/// Note: This is a map-based msgpack object matching nodetool-core's RunJobRequest model.
/// We use string keys to preserve Python field names (e.g. "params") while keeping C# identifiers clean.
/// </summary>
[MessagePackObject]
public class RunJobRequest
{
    [Key("type")]
    public string Type { get; set; } = "run_job_request";

    [Key("job_type")]
    public string JobType { get; set; } = "workflow";

    [Key("execution_strategy")]
    public string ExecutionStrategy { get; set; } = "threaded";

    [Key("workflow_id")]
    public string WorkflowId { get; set; } = "";

    [Key("user_id")]
    public string UserId { get; set; } = "";

    [Key("auth_token")]
    public string AuthToken { get; set; } = "";

    [Key("api_url")]
    public string? ApiUrl { get; set; } = null;

    [Key("env")]
    public Dictionary<string, object>? Env { get; set; } = null;

    [Key("graph")]
    public Graph? Graph { get; set; } = null;

    [Key("params")]
    public object? Params { get; set; } = null;

    [Key("explicit_types")]
    public bool? ExplicitTypes { get; set; } = false;

    [Key("resource_limits")]
    public Dictionary<string, object>? ResourceLimits { get; set; } = null;
}

/// <summary>
/// WebSocket command wrapper for sending commands to the server.
/// </summary>
[MessagePackObject(true)]
public class WebSocketCommand
{
    /// <summary>
    /// Command name (e.g. "run_job", "cancel_job").
    /// </summary>
    public string command { get; set; } = "";

    /// <summary>
    /// Message type. For the worker protocol this should match <see cref="command"/>.
    /// </summary>
    public string type { get; set; } = "";

    public object data { get; set; } = new Dictionary<string, object>();
}