using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Api;

/// <summary>
/// Interface for the Nodetool API client
/// </summary>
public interface INodetoolClient : IDisposable
{
    #region Node Operations
    
    /// <summary>
    /// Get all available node types with their metadata
    /// </summary>
    Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a single node
    /// </summary>
    /// <param name="nodeType">The node type to execute</param>
    /// <param name="inputs">Input parameters for the node</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution result</returns>
    Task<Dictionary<string, object>> ExecuteNodeAsync(
        string nodeType, 
        Dictionary<string, object> inputs, 
        CancellationToken cancellationToken = default);
    
    #endregion

    #region Workflow Operations
    
    /// <summary>
    /// Get all workflows
    /// </summary>
    Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific workflow by ID
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow details</returns>
    Task<WorkflowResponse> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute a workflow
    /// </summary>
    /// <param name="workflowId">The workflow ID</param>
    /// <param name="parameters">Input parameters for the workflow</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution result</returns>
    Task<Dictionary<string, object>> ExecuteWorkflowAsync(
        string workflowId, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default);
    
    #endregion

    #region Asset Operations
    
    /// <summary>
    /// Upload an asset
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <param name="content">The file content</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The uploaded asset information</returns>
    Task<AssetResponse> UploadAssetAsync(
        string fileName, 
        Stream content, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get asset information
    /// </summary>
    /// <param name="assetId">The asset ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The asset information</returns>
    Task<AssetResponse> GetAssetAsync(string assetId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download an asset
    /// </summary>
    /// <param name="assetId">The asset ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The asset content stream</returns>
    Task<Stream> DownloadAssetAsync(string assetId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Job Operations
    
    /// <summary>
    /// Get job status
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The job status</returns>
    Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a job
    /// </summary>
    /// <param name="jobId">The job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Configuration
    
    /// <summary>
    /// Configure the client with base URL and authentication
    /// </summary>
    /// <param name="baseUrl">The Nodetool API base URL</param>
    /// <param name="apiKey">Optional API key for authentication</param>
    void Configure(string baseUrl, string? apiKey = null);
    
    #endregion
} 