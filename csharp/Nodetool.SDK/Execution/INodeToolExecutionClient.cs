namespace Nodetool.SDK.Execution;

/// <summary>
/// Client interface for executing NodeTool workflows and nodes.
/// This is the main entry point for SDK users.
/// </summary>
public interface INodeToolExecutionClient : IDisposable
{
    /// <summary>
    /// Whether the client is connected to the NodeTool server.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Current connection status: "disconnected", "connecting", "connected", "error".
    /// </summary>
    string ConnectionStatus { get; }

    /// <summary>
    /// Last error message from connection attempt.
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Connect to the NodeTool server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection succeeded.</returns>
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnect from the NodeTool server.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Execute a workflow by ID.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="inputs">Input parameters keyed by input name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution session for tracking progress and results.</returns>
    Task<IExecutionSession> ExecuteWorkflowAsync(
        string workflowId,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a workflow with a graph definition.
    /// </summary>
    /// <param name="graph">Graph definition containing nodes and edges.</param>
    /// <param name="inputs">Input parameters keyed by input name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution session for tracking progress and results.</returns>
    Task<IExecutionSession> ExecuteGraphAsync(
        Types.Graph graph,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a single node by type.
    /// </summary>
    /// <param name="nodeType">Node type identifier (e.g., "nodetool.image.transform.Resize").</param>
    /// <param name="inputs">Input parameters keyed by input name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution session for tracking progress and results.</returns>
    Task<IExecutionSession> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when connection status changes.
    /// </summary>
    event Action<string>? ConnectionStatusChanged;
}
