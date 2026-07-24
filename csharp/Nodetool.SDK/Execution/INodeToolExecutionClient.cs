using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Execution;

/// <summary>
/// Client interface for executing NodeTool workflows and nodes.
/// This is the main entry point for SDK users.
/// </summary>
public interface INodeToolExecutionClient : IDisposable, IAsyncDisposable
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
    /// Execute a workflow by name (WebSocket discovery + execution).
    /// </summary>
    /// <param name="workflowName">Workflow name (case-insensitive).</param>
    /// <param name="inputs">Input parameters keyed by input name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Execution session for tracking progress and results.</returns>
    Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        Dictionary<string, object>? inputs = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a workflow by name with a single input value (common case).
    /// </summary>
    Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        string inputName,
        object? inputValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a workflow by name with tuple inputs (convenient for small input sets).
    /// </summary>
    Task<IExecutionSession> ExecuteWorkflowByNameAsync(
        string workflowName,
        CancellationToken cancellationToken = default,
        params (string Name, object? Value)[] inputs);

    /// <summary>
    /// Fetch all available node types from the server via WebSocket (<c>list_nodes</c>).
    /// </summary>
    Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch one bounded recursive node type inventory page.
    /// </summary>
    Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
        int cursor = 0,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single node type by its type identifier (<c>get_node</c>).
    /// </summary>
    Task<NodeMetadataResponse?> GetNodeAsync(string nodeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch all workflows from the server via WebSocket (<c>list_workflows</c>).
    /// </summary>
    Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single workflow by ID from the server via WebSocket (<c>get_workflow</c>).
    /// </summary>
    Task<WorkflowResponse?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch compact workflow summaries without graph payloads.
    /// </summary>
    Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch the authoritative graph-derived interface for one workflow.
    /// </summary>
    Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
        string workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch authoritative graph-derived interfaces for up to 100 workflows.
    /// </summary>
    Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
        IReadOnlyCollection<string> workflowIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch assets from the server via WebSocket (<c>list_assets</c>).
    /// </summary>
    Task<List<AssetResponse>> GetAssetsAsync(
        string? contentType = null,
        string? parentId = null,
        int pageSize = 10000,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch a single asset by ID from the server via WebSocket (<c>get_asset</c>).
    /// </summary>
    Task<AssetResponse?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default);

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
