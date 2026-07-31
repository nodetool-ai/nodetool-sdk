using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Api;

/// <summary>
/// Interface for the Nodetool API client
/// </summary>
public interface INodetoolClient :
    IDisposable,
    Workflows.IWorkflowDiscoveryClient,
    global::Nodetool.SDK.Models.IModelCatalogClient,
    global::Nodetool.SDK.Models.IModelDownloadClient
{
    /// <summary>
    /// Get server version and uptime information.
    /// </summary>
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the feature-flagged SDK lifecycle capabilities and enforced limits.
    /// </summary>
    Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate workflow inputs and optionally check current requirement availability
    /// without starting an execution.
    /// </summary>
    Task<SdkPreflightResponse> PreflightWorkflowAsync(
        SdkPreflightRequest request,
        CancellationToken cancellationToken = default);

    #region Node Operations
    
    /// <summary>
    /// Get all available node types with their metadata
    /// </summary>
    Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get one bounded page of recursive node type usage and registry readiness.
    /// </summary>
    Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
        int cursor = 0,
        int limit = 100,
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
    /// Upload an asset with an explicit MIME content type.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">The MIME content type sent with the upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The uploaded asset information.</returns>
    Task<AssetResponse> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload an execution input directly to temporary storage without
    /// creating persistent asset metadata or a thumbnail.
    /// </summary>
    Task<TemporaryAssetUploadResponse> UploadTemporaryAssetAsync(
        string fileName,
        Stream content,
        string contentType,
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
