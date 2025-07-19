using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Configuration;

namespace Nodetool.SDK.Api;

/// <summary>
/// HTTP client implementation for the Nodetool API
/// </summary>
public class NodetoolClient : INodetoolClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NodetoolClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public NodetoolClient(HttpClient? httpClient = null, ILogger<NodetoolClient>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        // Set default configuration
        Configure(NodetoolConstants.Defaults.BaseUrl);
    }

    public void Configure(string baseUrl, string? apiKey = null)
    {
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", NodetoolConstants.Defaults.UserAgent);
        _httpClient.Timeout = TimeSpan.FromSeconds(NodetoolConstants.Defaults.TimeoutSeconds);
        
        _logger?.LogDebug("Configured Nodetool client: {BaseUrl}", baseUrl);
    }

    #region Node Operations

    public async Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching node types");
        
        var response = await _httpClient.GetAsync(NodetoolConstants.Endpoints.NodesMetadata, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var nodeTypes = JsonSerializer.Deserialize<List<NodeMetadataResponse>>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved {Count} node types", nodeTypes?.Count ?? 0);
        return nodeTypes ?? new List<NodeMetadataResponse>();
    }

    public async Task<Dictionary<string, object>> ExecuteNodeAsync(
        string nodeType, 
        Dictionary<string, object> inputs, 
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Executing node: {NodeType}", nodeType);
        
        var request = new NodeExecutionRequest
        {
            NodeType = nodeType,
            Inputs = inputs
        };
        
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, NodetoolConstants.ContentTypes.Json);
        
        var response = await _httpClient.PostAsync(NodetoolConstants.Endpoints.NodeExecute, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson, _jsonOptions);
        
        _logger?.LogDebug("Node execution completed with {OutputCount} outputs", result?.Count ?? 0);
        return result ?? new Dictionary<string, object>();
    }

    #endregion

    #region Workflow Operations

    public async Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching workflows");
        
        var response = await _httpClient.GetAsync(NodetoolConstants.Endpoints.Workflows, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var workflowListResponse = JsonSerializer.Deserialize<WorkflowListResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved {Count} workflows", workflowListResponse?.Workflows?.Count ?? 0);
        return workflowListResponse?.Workflows ?? new List<WorkflowResponse>();
    }

    public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching workflow: {WorkflowId}", workflowId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.WorkflowById, workflowId);
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var workflow = JsonSerializer.Deserialize<WorkflowResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved workflow: {Name}", workflow?.Name ?? "Unknown");
        return workflow ?? throw new InvalidOperationException($"Failed to deserialize workflow {workflowId}");
    }

    public async Task<Dictionary<string, object>> ExecuteWorkflowAsync(
        string workflowId, 
        Dictionary<string, object> parameters, 
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Executing workflow: {WorkflowId}", workflowId);
        
        var request = new WorkflowExecutionRequest
        {
            Params = parameters
        };
        
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, NodetoolConstants.ContentTypes.Json);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.WorkflowRun, workflowId);
        var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var resultJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson, _jsonOptions);
        
        _logger?.LogDebug("Workflow execution completed");
        return result ?? new Dictionary<string, object>();
    }

    #endregion

    #region Asset Operations

    public async Task<AssetResponse> UploadAssetAsync(
        string fileName, 
        Stream content, 
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Uploading asset: {FileName}", fileName);
        
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");
        form.Add(streamContent, "file", fileName);
        
        var response = await _httpClient.PostAsync(NodetoolConstants.Endpoints.Assets, form, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var asset = JsonSerializer.Deserialize<AssetResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Asset uploaded: {AssetId}", asset?.Id);
        return asset ?? throw new InvalidOperationException("Failed to upload asset");
    }

    public async Task<AssetResponse> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching asset: {AssetId}", assetId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.AssetById, assetId);
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var asset = JsonSerializer.Deserialize<AssetResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved asset: {Name}", asset?.Name);
        return asset ?? throw new InvalidOperationException($"Failed to get asset {assetId}");
    }

    public async Task<Stream> DownloadAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Downloading asset: {AssetId}", assetId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.AssetDownload, assetId);
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    #endregion

    #region Job Operations

    public async Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching job: {JobId}", jobId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.JobById, jobId);
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var job = JsonSerializer.Deserialize<JobResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved job: {Status}", job?.Status);
        return job ?? throw new InvalidOperationException($"Failed to get job {jobId}");
    }

    public async Task CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Cancelling job: {JobId}", jobId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.JobCancel, jobId);
        var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        _logger?.LogDebug("Job cancelled: {JobId}", jobId);
    }

    #endregion

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
} 