using MessagePack;

namespace Nodetool.SDK.Types;

/// <summary>
/// WebSocket message indicating job status changes.
/// </summary>
[MessagePackObject]
public class JobUpdate
{
    [Key(0)]
    public string type { get; set; } = "job_update";

    [Key(1)]
    public string status { get; set; } = "";

    [Key(2)]
    public string? job_id { get; set; } = null;

    [Key(3)]
    public string? message { get; set; } = null;

    [Key(4)]
    public Dictionary<string, object>? result { get; set; } = null;

    [Key(5)]
    public string? error { get; set; } = null;
}

/// <summary>
/// WebSocket message indicating node execution status and results.
/// </summary>
[MessagePackObject]
public class NodeUpdate
{
    [Key(0)]
    public string type { get; set; } = "node_update";

    [Key(1)]
    public string node_id { get; set; } = "";

    [Key(2)]
    public string node_name { get; set; } = "";

    [Key(3)]
    public string status { get; set; } = "";

    [Key(4)]
    public string? error { get; set; } = null;

    [Key(5)]
    public string? logs { get; set; } = null;

    [Key(6)]
    public Dictionary<string, object>? result { get; set; } = null;

    [Key(7)]
    public Dictionary<string, object>? properties { get; set; } = null;
}

/// <summary>
/// WebSocket message for streaming node output values during execution.
/// </summary>
[MessagePackObject]
public class OutputUpdate
{
    [Key(0)]
    public string type { get; set; } = "output_update";

    [Key(1)]
    public string node_id { get; set; } = "";

    [Key(2)]
    public string node_name { get; set; } = "";

    [Key(3)]
    public string output_name { get; set; } = "";

    [Key(4)]
    public object? value { get; set; } = null;

    [Key(5)]
    public string output_type { get; set; } = "";
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
[MessagePackObject]
public class WorkflowExecuteRequest
{
    [Key(0)]
    public string type { get; set; } = "workflow_execute";

    [Key(1)]
    public string workflow_id { get; set; } = "";

    [Key(2)]
    public Dictionary<string, object> inputs { get; set; } = new();

    [Key(3)]
    public string? job_id { get; set; } = null;

    [Key(4)]
    public Dictionary<string, object>? options { get; set; } = null;
}

/// <summary>
/// WebSocket message for single node execution requests.
/// </summary>
[MessagePackObject]
public class NodeExecuteRequest
{
    [Key(0)]
    public string type { get; set; } = "node_execute";

    [Key(1)]
    public string node_type { get; set; } = "";

    [Key(2)]
    public Dictionary<string, object> inputs { get; set; } = new();

    [Key(3)]
    public string? node_id { get; set; } = null;

    [Key(4)]
    public Dictionary<string, object>? properties { get; set; } = null;
}

/// <summary>
/// WebSocket message for job cancellation requests.
/// </summary>
[MessagePackObject]
public class JobCancelRequest
{
    [Key(0)]
    public string type { get; set; } = "job_cancel";

    [Key(1)]
    public string job_id { get; set; } = "";
}

/// <summary>
/// WebSocket message for connection status and authentication.
/// </summary>
[MessagePackObject]
public class ConnectionStatus
{
    [Key(0)]
    public string type { get; set; } = "connection_status";

    [Key(1)]
    public string status { get; set; } = "";

    [Key(2)]
    public string? message { get; set; } = null;

    [Key(3)]
    public Dictionary<string, object>? metadata { get; set; } = null;
}

/// <summary>
/// WebSocket message for error notifications.
/// </summary>
[MessagePackObject]
public class ErrorMessage
{
    [Key(0)]
    public string type { get; set; } = "error";

    [Key(1)]
    public string error_code { get; set; } = "";

    [Key(2)]
    public string message { get; set; } = "";

    [Key(3)]
    public Dictionary<string, object>? details { get; set; } = null;
}

/// <summary>
/// WebSocket message for progress updates with percentage and status.
/// </summary>
[MessagePackObject]
public class ProgressUpdate
{
    [Key(0)]
    public string type { get; set; } = "progress_update";

    [Key(1)]
    public string? job_id { get; set; } = null;

    [Key(2)]
    public string? node_id { get; set; } = null;

    [Key(3)]
    public double progress { get; set; } = 0.0;

    [Key(4)]
    public string? status_message { get; set; } = null;

    [Key(5)]
    public int? total_steps { get; set; } = null;

    [Key(6)]
    public int? current_step { get; set; } = null;
} 