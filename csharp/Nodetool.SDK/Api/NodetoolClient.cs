using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Connection;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Diagnostics;

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
    private readonly NodeToolReadRetryPolicy _readRetryPolicy;
    private Uri _baseAddress = new(NodetoolConstants.Defaults.BaseUrl);
    private string? _apiKey;
    
    public NodetoolClient(
        HttpClient? httpClient = null,
        ILogger<NodetoolClient>? logger = null,
        NodeToolReadRetryPolicy? readRetryPolicy = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient == null;
        if (_ownsHttpClient)
        {
            _httpClient.Timeout =
                TimeSpan.FromSeconds(
                    NodetoolConstants.Defaults.TimeoutSeconds);
        }
        _logger = logger;
        _readRetryPolicy =
            readRetryPolicy ?? NodeToolReadRetryPolicy.None;
        _readRetryPolicy.Validate();
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
        ILogger<NodetoolClient>? logger = null,
        NodeToolReadRetryPolicy? readRetryPolicy = null)
        : this(httpClient, logger, readRetryPolicy)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);
        Configure(baseAddress.AbsoluteUri, apiKey);
    }

    public void Configure(string baseUrl, string? apiKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        var parsedBaseAddress = new Uri(baseUrl, UriKind.Absolute);
        _baseAddress = parsedBaseAddress.AbsoluteUri.EndsWith(
            "/",
            StringComparison.Ordinal)
            ? parsedBaseAddress
            : new Uri($"{parsedBaseAddress.AbsoluteUri}/");
        _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? null
            : apiKey.Trim();

        _logger?.LogDebug(
            "Configured Nodetool client: {BaseUrl}",
            NodeToolDiagnosticRedactor.RedactUri(
                _baseAddress));
    }

    internal void SetAuthToken(string? token)
    {
        _apiKey = string.IsNullOrWhiteSpace(token)
            ? null
            : token.Trim();
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            NodetoolConstants.Endpoints.Health);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<HealthResponse>(json, _jsonOptions)
            ?? throw new InvalidDataException("The NodeTool health response was empty or malformed.");
    }

    public async Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await GetSdkResponseAsync<SdkCapabilitiesResponse>(
            NodetoolConstants.Endpoints.SdkCapabilitiesV1,
            cancellationToken);
        if (!string.Equals(result.ProtocolVersion, "1", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unsupported SDK protocol version '{result.ProtocolVersion}'.");
        }
        if (string.IsNullOrWhiteSpace(result.NodetoolVersion) ||
            result.SupportedEncodings.Count == 0)
        {
            throw new InvalidDataException(
                "The SDK capability response is incomplete.");
        }
        return result;
    }

    public async Task<SdkPreflightResponse> PreflightWorkflowAsync(
        SdkPreflightRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkflowId);
        ArgumentNullException.ThrowIfNull(request.Inputs);
        if (request.InterfaceVersion != 1)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "Only workflow interface version 1 is supported.");
        if (!SdkPreflightLevels.IsValid(request.Level))
            throw new ArgumentException(
                $"Unsupported preflight level '{request.Level}'.",
                nameof(request));
        if (request.ExecutionTarget is { } target)
        {
            if (target.Kind is not (
                    SdkExecutionTargetKinds.Local or
                    SdkExecutionTargetKinds.Worker or
                    SdkExecutionTargetKinds.Runner))
            {
                throw new ArgumentException(
                    $"Unsupported execution target kind '{target.Kind}'.",
                    nameof(request));
            }
            if (target.Kind == SdkExecutionTargetKinds.Worker &&
                string.IsNullOrWhiteSpace(target.WorkerId))
            {
                throw new ArgumentException(
                    "A worker execution target requires a worker ID.",
                    nameof(request));
            }
            if (target.Kind == SdkExecutionTargetKinds.Runner &&
                string.IsNullOrWhiteSpace(target.RunnerId))
            {
                throw new ArgumentException(
                    "A runner execution target requires a runner ID.",
                    nameof(request));
            }
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            NodetoolConstants.Endpoints.SdkPreflightV1)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, _jsonOptions),
                Encoding.UTF8,
                NodetoolConstants.ContentTypes.Json)
        };
        var result = await SendSdkResponseAsync<SdkPreflightResponse>(
            message,
            cancellationToken);
        if (result.Version != 1 ||
            !SdkPreflightLevels.IsValid(result.Level) ||
            !string.Equals(result.WorkflowId, request.WorkflowId, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The NodeTool preflight response does not match the requested v1 workflow.");
        }
        return result;
    }

    #region Node Operations

    public async Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Fetching node types");
        
        // Server defaults to slim summaries without ?fields=full — VL and SDK clients need properties/outputs.
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            $"{NodetoolConstants.Endpoints.NodesMetadata}?fields=full");
        using var response = await _httpClient.SendAsync(
            request,
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

            using var request = CreateConfiguredRequest(
                HttpMethod.Get,
                endpoint);
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);
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
        var requestId = Guid.NewGuid().ToString("N");
        var template = await ReadRequestTemplateAsync(
            request,
            cancellationToken).ConfigureAwait(false);
        HttpResponseMessage? response = null;
        Exception? lastTransportError = null;

        for (var attempt = 1;
             attempt <= _readRetryPolicy.MaximumAttempts;
             attempt++)
        {
            using var attemptRequest = CreateRequest(
                template,
                requestId);
            try
            {
                response = await _httpClient.SendAsync(
                    attemptRequest,
                    cancellationToken).ConfigureAwait(false);
                if (!_readRetryPolicy.ShouldRetry(
                        response.StatusCode,
                        attempt))
                {
                    break;
                }

                var delay = _readRetryPolicy.GetDelay(
                    attempt,
                    GetRetryAfter(response));
                response.Dispose();
                response = null;
                await Task.Delay(delay, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or IOException &&
                attempt < _readRetryPolicy.MaximumAttempts)
            {
                lastTransportError = exception;
                await Task.Delay(
                        _readRetryPolicy.GetDelay(attempt),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (response == null)
            throw lastTransportError ??
                  new HttpRequestException(
                      "NodeTool SDK request failed before receiving a response.");
        using (response)
        {
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

            throw new SdkApiException(
                response.StatusCode,
                error?.Code,
                error?.Retryable ?? false,
                error?.Detail ??
                $"NodeTool SDK request failed ({response.StatusCode}).");
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions)
            ?? throw new InvalidDataException(
                $"The SDK response from '{request.RequestUri}' was empty or malformed.");
        }
    }

    private sealed record RequestTemplate(
        HttpMethod Method,
        Uri? RequestUri,
        IReadOnlyList<KeyValuePair<string, IEnumerable<string>>> Headers,
        byte[]? Content,
        IReadOnlyList<KeyValuePair<string, IEnumerable<string>>> ContentHeaders);

    private static async Task<RequestTemplate> ReadRequestTemplateAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = request.Content == null
            ? null
            : await request.Content.ReadAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);
        return new RequestTemplate(
            request.Method,
            request.RequestUri,
            request.Headers
                .Select(pair =>
                    new KeyValuePair<string, IEnumerable<string>>(
                        pair.Key,
                        pair.Value.ToArray()))
                .ToArray(),
            content,
            request.Content?.Headers
                .Select(pair =>
                    new KeyValuePair<string, IEnumerable<string>>(
                        pair.Key,
                        pair.Value.ToArray()))
                .ToArray() ?? []);
    }

    private HttpRequestMessage CreateRequest(
        RequestTemplate template,
        string requestId)
    {
        var clone = new HttpRequestMessage(
            template.Method,
            ResolveEndpoint(template.RequestUri));
        ApplyConfiguredHeaders(clone);
        foreach (var header in template.Headers)
            clone.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        clone.Headers.TryAddWithoutValidation(
            "X-NodeTool-Request-Id",
            requestId);
        if (template.Content != null)
        {
            clone.Content = new ByteArrayContent(template.Content);
            foreach (var header in template.ContentHeaders)
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
        }
        return clone;
    }

    private HttpRequestMessage CreateConfiguredRequest(
        HttpMethod method,
        string endpoint,
        HttpContent? content = null)
    {
        var request = new HttpRequestMessage(
            method,
            ResolveEndpoint(endpoint))
        {
            Content = content
        };
        ApplyConfiguredHeaders(request);
        return request;
    }

    private Uri ResolveEndpoint(string endpoint)
        => ResolveEndpoint(new Uri(endpoint, UriKind.RelativeOrAbsolute));

    private Uri ResolveEndpoint(Uri? endpoint)
    {
        if (endpoint is null)
            throw new InvalidOperationException(
                "The NodeTool request has no endpoint.");
        if (endpoint.IsAbsoluteUri)
            return endpoint;

        // SDK endpoint constants begin with '/'. Treat them as relative to the
        // configured NodeTool deployment root so reverse-proxy subpaths work.
        return new Uri(
            _baseAddress,
            endpoint.OriginalString.TrimStart('/'));
    }

    private void ApplyConfiguredHeaders(HttpRequestMessage request)
    {
        request.Headers.UserAgent.TryParseAdd(
            NodetoolConstants.Defaults.UserAgent);
        if (_apiKey != null)
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }
    }

    private static TimeSpan? GetRetryAfter(
        HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
            return delta;
        if (retryAfter?.Date is { } date)
        {
            var duration = date - DateTimeOffset.UtcNow;
            return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        }
        return null;
    }

    public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        _logger?.LogDebug("Fetching workflow: {WorkflowId}", workflowId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.WorkflowById, workflowId);
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            endpoint);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var workflow = JsonSerializer.Deserialize<WorkflowResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Retrieved workflow: {Name}", workflow?.Name ?? "Unknown");
        return workflow ?? throw new InvalidOperationException($"Failed to deserialize workflow {workflowId}");
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
        // .NET emits simple Content-Disposition parameters without quotes.
        // Node's Web API multipart parser requires the RFC form used by
        // browsers/curl (`name="file"; filename="..."`).
        var disposition = streamContent.Headers.ContentDisposition!;
        disposition.Name = QuoteMultipartParameter("file");
        disposition.FileName = QuoteMultipartParameter(fileName);
        disposition.FileNameStar = null;
        
        using var request = CreateConfiguredRequest(
            HttpMethod.Post,
            NodetoolConstants.Endpoints.Assets,
            form);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var asset = JsonSerializer.Deserialize<AssetResponse>(json, _jsonOptions);
        
        _logger?.LogDebug("Asset uploaded: {AssetId}", asset?.Id);
        return asset ?? throw new InvalidOperationException("Failed to upload asset");
    }

    private static string QuoteMultipartParameter(string value)
        => $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    public async Task<AssetResponse> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetId);
        _logger?.LogDebug("Fetching asset: {AssetId}", assetId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.AssetById, assetId);
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            endpoint);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
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
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            endpoint);
        var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        try
        {
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            return new ResponseOwnedStream(stream, response);
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    #endregion

    #region Job Operations

    public async Task<JobResponse> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        _logger?.LogDebug("Fetching job: {JobId}", jobId);
        
        var endpoint = string.Format(NodetoolConstants.Endpoints.JobById, jobId);
        using var request = CreateConfiguredRequest(
            HttpMethod.Get,
            endpoint);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
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
        using var request = CreateConfiguredRequest(
            HttpMethod.Post,
            endpoint);
        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        
        _logger?.LogDebug("Job cancelled: {JobId}", jobId);
    }

    #endregion

    private sealed class ResponseOwnedStream(
        Stream inner,
        HttpResponseMessage response) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken)
            => inner.FlushAsync(cancellationToken);
        public override int Read(
            byte[] buffer,
            int offset,
            int count)
            => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer)
            => inner.Read(buffer);
        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin)
            => inner.Seek(offset, origin);
        public override void SetLength(long value)
            => inner.SetLength(value);
        public override void Write(
            byte[] buffer,
            int offset,
            int count)
            => inner.Write(buffer, offset, count);
        public override void Write(ReadOnlySpan<byte> buffer)
            => inner.Write(buffer);
        public override ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
            => inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
                response.Dispose();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync().ConfigureAwait(false);
            response.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
