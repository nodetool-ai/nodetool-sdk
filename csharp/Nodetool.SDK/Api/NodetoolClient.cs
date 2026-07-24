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
    private readonly bool _ownsHttpClient;
    private readonly ILogger<NodetoolClient>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public NodetoolClient(HttpClient? httpClient = null, ILogger<NodetoolClient>? logger = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
        
        // Set default configuration
        Configure(NodetoolConstants.Defaults.BaseUrl);
    }

    /// <summary>
    /// Creates an HTTP client configured for an explicit NodeTool API endpoint.
    /// </summary>
    public NodetoolClient(
        Uri baseAddress,
        string? apiKey = null,
        HttpClient? httpClient = null,
        ILogger<NodetoolClient>? logger = null)
        : this(httpClient, logger)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        Configure(baseAddress.AbsoluteUri, apiKey);
    }

    public void Configure(string baseUrl, string? apiKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
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

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            NodetoolConstants.Endpoints.Health,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<HealthResponse>(json, _jsonOptions)
            ?? throw new InvalidDataException("The NodeTool health response was empty or malformed.");
    }

    #region Node Operations

    public async Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching node types");
        
        // Server defaults to slim summaries without ?fields=full — VL and SDK clients need properties/outputs.
        var response = await _httpClient.GetAsync(
            $"{NodetoolConstants.Endpoints.NodesMetadata}?fields=full",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var nodeTypes = JsonSerializer.Deserialize<List<NodeMetadataResponse>>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved {Count} node types", nodeTypes?.Count ?? 0);
        return nodeTypes ?? new List<NodeMetadataResponse>();
    }

    public async Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
        int cursor = 0,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (cursor < 0)
            throw new ArgumentOutOfRangeException(nameof(cursor));
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit));

        var endpoint =
            $"{NodetoolConstants.Endpoints.NodeTypeInventoryV1}?cursor={cursor}&limit={limit}";
        var result = await GetSdkResponseAsync<NodeTypeInventoryResponse>(
            endpoint,
            cancellationToken);
        if (result.Version != 1 || !result.RegistryReady)
            throw new InvalidDataException(
                "The server returned an unsupported or unready node type inventory.");
        return result;
    }

    public async Task<Dictionary<string, object>> ExecuteNodeAsync(
        string nodeType, 
        Dictionary<string, object> inputs, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeType);
        ArgumentNullException.ThrowIfNull(inputs);
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

        const int pageSize = 25;
        var workflows = new List<WorkflowResponse>();
        var visitedCursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;

        do
        {
            var endpoint = $"{NodetoolConstants.Endpoints.Workflows}?limit={pageSize}";
            if (cursor != null)
                endpoint += $"&cursor={Uri.EscapeDataString(cursor)}";

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var page = JsonSerializer.Deserialize<WorkflowListResponse>(json, _jsonOptions)
                ?? throw new InvalidDataException("The workflow list response was empty or malformed.");
            workflows.AddRange(page.Workflows);

            cursor = string.IsNullOrWhiteSpace(page.Next) ? null : page.Next;
            if (cursor != null && !visitedCursors.Add(cursor))
                throw new InvalidDataException($"The workflow list cursor repeated ({cursor}); pagination cannot advance.");
        }
        while (cursor != null);

        _logger?.LogDebug("Retrieved {Count} workflows", workflows.Count);
        return workflows;
    }

    public async Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(
        CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching compact workflow summaries");

        const int pageSize = 50;
        var workflows = new List<WorkflowSummaryResponse>();
        var visitedCursors = new HashSet<string>(StringComparer.Ordinal);
        string? cursor = null;

        do
        {
            var endpoint = $"{NodetoolConstants.Endpoints.WorkflowSummariesV1}?limit={pageSize}";
            if (cursor != null)
                endpoint += $"&cursor={Uri.EscapeDataString(cursor)}";

            var page = await GetSdkResponseAsync<WorkflowSummaryListResponse>(endpoint, cancellationToken);
            workflows.AddRange(page.Workflows);

            cursor = string.IsNullOrWhiteSpace(page.Next) ? null : page.Next;
            if (cursor != null && !visitedCursors.Add(cursor))
                throw new InvalidDataException(
                    $"The workflow summary cursor repeated ({cursor}); pagination cannot advance.");
        }
        while (cursor != null);

        _logger?.LogDebug("Retrieved {Count} compact workflow summaries", workflows.Count);
        return workflows;
    }

    public async Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        var endpoint = string.Format(
            NodetoolConstants.Endpoints.WorkflowInterfaceV1,
            Uri.EscapeDataString(workflowId));
        var result = await GetSdkResponseAsync<WorkflowInterfaceResponse>(endpoint, cancellationToken);
        if (result.Version != 1 || !string.Equals(result.Source, "server", StringComparison.Ordinal))
            throw new InvalidDataException(
                $"Workflow {workflowId} returned an unsupported workflow-interface contract.");
        if (!string.Equals(result.WorkflowId, workflowId, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"Workflow-interface response ID '{result.WorkflowId}' does not match '{workflowId}'.");
        return result;
    }

    public async Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(
        IReadOnlyCollection<string> workflowIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflowIds);
        if (workflowIds.Count is < 1 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(workflowIds),
                "Expected between 1 and 100 workflow IDs.");
        if (workflowIds.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Workflow IDs cannot be empty.", nameof(workflowIds));
        if (workflowIds.Distinct(StringComparer.Ordinal).Count() != workflowIds.Count)
            throw new ArgumentException("Workflow IDs must be unique.", nameof(workflowIds));

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            NodetoolConstants.Endpoints.WorkflowInterfacesV1)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(
                    new WorkflowInterfacesRequest { Ids = workflowIds },
                    _jsonOptions),
                Encoding.UTF8,
                NodetoolConstants.ContentTypes.Json)
        };
        var result = await SendSdkResponseAsync<WorkflowInterfacesResponse>(
            request,
            cancellationToken);
        var requestedIds = workflowIds.ToHashSet(StringComparer.Ordinal);
        foreach (var workflowInterface in result.Interfaces)
        {
            if (workflowInterface.Version != 1
                || !string.Equals(workflowInterface.Source, "server", StringComparison.Ordinal)
                || !requestedIds.Contains(workflowInterface.WorkflowId))
            {
                throw new InvalidDataException(
                    "The workflow-interface batch contained an unsupported or unrequested contract.");
            }
        }
        return result;
    }

    private async Task<T> GetSdkResponseAsync<T>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        return await SendSdkResponseAsync<T>(request, cancellationToken);
    }

    private async Task<T> SendSdkResponseAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            ApiErrorResponse? error = null;
            try
            {
                error = JsonSerializer.Deserialize<ApiErrorResponse>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // Preserve the HTTP status when an older server returns a non-JSON error page.
            }

            if (response.StatusCode is System.Net.HttpStatusCode.BadRequest
                or System.Net.HttpStatusCode.NotFound
                or System.Net.HttpStatusCode.ServiceUnavailable)
            {
                throw new WorkflowInterfaceUnavailableException(
                    response.StatusCode,
                    error?.Code,
                    error?.Detail ?? $"NodeTool does not provide the required workflow-interface v1 API ({response.StatusCode}).");
            }
            response.EnsureSuccessStatusCode();
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidDataException(
                $"The SDK response from '{request.RequestUri}' was empty or malformed.");
    }

    public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        ArgumentNullException.ThrowIfNull(parameters);
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
        => await UploadAssetAsync(fileName, content, "application/octet-stream", cancellationToken);

    public async Task<AssetResponse> UploadAssetAsync(
        string fileName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        _logger?.LogDebug("Uploading asset: {FileName}", fileName);
        
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);
        _logger?.LogDebug("Downloading asset: {AssetId}", assetId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.AssetDownload, assetId);
        return await _httpClient.GetStreamAsync(endpoint, cancellationToken);
    }

    #endregion

    #region Job Operations

    public async Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        _logger?.LogDebug("Cancelling job: {JobId}", jobId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.JobCancel, jobId);
        var response = await _httpClient.PostAsync(endpoint, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        _logger?.LogDebug("Job cancelled: {JobId}", jobId);
    }

    #endregion

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
