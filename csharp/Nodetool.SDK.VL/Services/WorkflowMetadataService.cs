using Microsoft.Extensions.Logging;
using System.Text.Json;
using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.VL.Models;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;
using Nodetool.SDK.Workflows;
using System.Security.Cryptography;
using System.Text;
using Nodetool.SDK.VL.Utilities;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Service for fetching and caching workflow metadata from Nodetool API
/// </summary>
public class WorkflowMetadataService : IDisposable
{
    private INodetoolClient _client;
    private INodeToolExecutionClient? _webSocketClient;
    private readonly ILogger<WorkflowMetadataService>? _logger;
    private bool _ownsClient;
    private WorkflowCatalog _catalog;
    private string _cacheScope = NodetoolConstants.Defaults.BaseUrl;
    private bool _disposed;
    
    public string StatusMessage { get; private set; } = "Not initialized";
    public DateTimeOffset? LastSuccessfulRefreshUtc { get; private set; }
    public string ServerVersion { get; private set; } = "unknown";
    public string InterfaceSource { get; private set; } = "unknown";
    public string? LastError { get; private set; }
    public int CacheHitCount { get; private set; }
    public string DiscoveryTransport => _webSocketClient is null ? "HTTP" : "WebSocket";
    internal bool IsDisposed => _disposed;

    public WorkflowMetadataService(
        ILogger<WorkflowMetadataService>? logger = null,
        INodeToolExecutionClient? webSocketClient = null)
        : this(new NodetoolClient(), logger, webSocketClient, ownsClient: true)
    {
    }

    internal WorkflowMetadataService(
        INodetoolClient client,
        ILogger<WorkflowMetadataService>? logger = null,
        INodeToolExecutionClient? webSocketClient = null,
        bool ownsClient = false)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
        _webSocketClient = webSocketClient?.IsConnected == true ? webSocketClient : null;
        _ownsClient = ownsClient;
        
        // Configure with default base URL - can be overridden by calling Configure
        _client.Configure(NodetoolConstants.Defaults.BaseUrl);
        _catalog = CreateCatalog(_cacheScope);
        
        _logger?.LogDebug("WorkflowMetadataService initialized with base URL: {BaseUrl}", NodetoolConstants.Defaults.BaseUrl);
    }

    /// <summary>
    /// Configure the service with custom options
    /// </summary>
    public void Configure(NodetoolOptions options)
        => Configure(options, _webSocketClient);

    internal void Configure(
        NodetoolOptions options,
        INodeToolExecutionClient? webSocketClient)
        => Configure(
            _client,
            options,
            webSocketClient,
            configureClient: true);

    internal void Configure(
        INodetoolClient client,
        NodetoolOptions options,
        INodeToolExecutionClient? webSocketClient)
        => Configure(
            client,
            options,
            webSocketClient,
            configureClient: false);

    private void Configure(
        INodetoolClient client,
        NodetoolOptions options,
        INodeToolExecutionClient? webSocketClient,
        bool configureClient)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        var selectedWebSocketClient =
            webSocketClient?.IsConnected == true
                ? webSocketClient
                : null;
        var nextCacheScope = CreateCacheScope(
            options.BaseUrl,
            options.ApiKey);
        var configurationChanged =
            !string.Equals(
                _cacheScope,
                nextCacheScope,
                StringComparison.Ordinal) ||
            !ReferenceEquals(
                _client,
                client) ||
            !ReferenceEquals(
                _webSocketClient,
                selectedWebSocketClient);

        if (configureClient)
            client.Configure(options.BaseUrl, options.ApiKey);
        if (!configurationChanged)
            return;

        var previousClient = _client;
        var disposePreviousClient = _ownsClient &&
                                    !ReferenceEquals(previousClient, client);
        _client = client;
        _ownsClient = configureClient && _ownsClient;
        _cacheScope = nextCacheScope;
        _webSocketClient = selectedWebSocketClient;
        _catalog.Dispose();
        _catalog = CreateCatalog(_cacheScope);
        if (disposePreviousClient)
            previousClient.Dispose();
        _logger?.LogDebug(
            "WorkflowMetadataService configured for {Transport}",
            DiscoveryTransport);
    }

    /// <summary>
    /// Fetch workflow metadata from the API with caching
    /// </summary>
    public async Task<List<WorkflowDetail>> FetchWorkflowMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        StatusMessage = "Fetching workflow metadata...";
        _logger?.LogInformation("Fetching workflow metadata from API");

        var healthTask = FetchHealthSafelyAsync(cancellationToken);
        var snapshot = await _catalog.RefreshAsync(
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (snapshot.LastSuccessfulRefreshUtc is null &&
            !string.IsNullOrWhiteSpace(snapshot.LastError))
        {
            LastError = snapshot.LastError;
            StatusMessage = $"Failed to fetch workflow metadata: {snapshot.LastError}";
            throw new InvalidOperationException(StatusMessage);
        }

        var workflowDetails = snapshot.Workflows
            .Select(CreateWorkflowDetail)
            .ToList();
        CacheHitCount = snapshot.CacheHitCount;
        InterfaceSource = string.Join(", ", snapshot.Workflows
            .Select(workflow => workflow.InterfaceSource)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal));
        if (string.IsNullOrWhiteSpace(InterfaceSource))
            InterfaceSource = "unknown";

        var health = await healthTask.ConfigureAwait(false);
        ServerVersion = string.IsNullOrWhiteSpace(health?.Version)
            ? "unknown"
            : health.Version;
        LastSuccessfulRefreshUtc = snapshot.LastSuccessfulRefreshUtc;
        LastError = snapshot.LastError;
        var cacheSummary = snapshot.CacheHitCount > 0
            ? $"; reused {snapshot.CacheHitCount} cached"
            : "";
        var stalePrefix = snapshot.IsStale ? "Stale snapshot retained; " : "";
        StatusMessage = snapshot.SkippedCount == 0
            ? $"{stalePrefix}fetched {workflowDetails.Count} workflow definitions via {DiscoveryTransport}{cacheSummary}"
            : $"{stalePrefix}fetched {workflowDetails.Count} workflow definitions via {DiscoveryTransport}{cacheSummary}; skipped {snapshot.SkippedCount} invalid interfaces";
        return workflowDetails;
    }

    /// <summary>
    /// Get a specific workflow by ID
    /// </summary>
    public async Task<WorkflowDetail?> GetWorkflowByIdAsync(string workflowId)
    {
        try
        {
            var cached = _catalog.GetById(workflowId);
            if (cached is not null)
                return CreateWorkflowDetail(cached);

            await _catalog.RefreshAsync().ConfigureAwait(false);
            return _catalog.GetById(workflowId) is { } descriptor
                ? CreateWorkflowDetail(descriptor)
                : null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(
                "Error fetching workflow {Id}: {Error}",
                workflowId,
                VlLog.SafeError(ex));
            return null;
        }
    }

    /// <summary>
    /// Clear the workflow cache
    /// </summary>
    public void ClearCache() => _catalog.Clear();

    private WorkflowCatalog CreateCatalog(string scope)
        => new(
            (IWorkflowDiscoveryClient?)_webSocketClient ?? _client,
            scope,
            TimeSpan.FromMinutes(NodetoolConstants.Defaults.CacheValidTimeMinutes));

    private async Task<HealthResponse?> FetchHealthSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _client.GetHealthAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(
                "Server health/version request failed during workflow discovery: {Error}",
                VlLog.SafeError(ex));
            return null;
        }
    }

    private static string CreateCacheScope(string baseUrl, string? apiKey)
    {
        var tokenHash = string.IsNullOrEmpty(apiKey)
            ? "anonymous"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
        return $"{baseUrl.TrimEnd('/')}|{tokenHash}";
    }

    private static WorkflowDetail CreateWorkflowDetail(
        WorkflowDescriptor descriptor)
    {
        var workflowInterface = new WorkflowInterfaceResponse
        {
            Version = descriptor.InterfaceVersion,
            WorkflowId = descriptor.Id,
            Etag = descriptor.InterfaceEtag,
            Source = descriptor.InterfaceSource,
            Inputs = descriptor.Inputs.Select(input => new WorkflowInterfaceInput
            {
                NodeId = input.NodeId,
                Name = input.Name,
                Description = input.Description,
                Type = ConvertType(input.Type),
                Required = input.Required,
                Default = input.DefaultValue ?? default,
                Min = input.Minimum,
                Max = input.Maximum
            }).ToList(),
            Outputs = descriptor.Outputs.Select(output => new WorkflowInterfaceOutput
            {
                NodeId = output.NodeId,
                Name = output.Name,
                Description = output.Description,
                Type = ConvertType(output.Type),
                Stream = output.Stream
            }).ToList(),
            Diagnostics = descriptor.Diagnostics.Select(diagnostic =>
                new WorkflowInterfaceDiagnostic
                {
                    Severity = diagnostic.Severity,
                    Code = diagnostic.Code,
                    Message = diagnostic.Message,
                    NodeId = diagnostic.NodeId,
                    PinName = diagnostic.PinName
                }).ToList()
        };
        var updatedAt = DateTime.TryParse(descriptor.Revision, out var parsedRevision)
            ? parsedRevision
            : DateTime.MinValue;
        return new WorkflowDetail
        {
            Id = descriptor.Id,
            Name = descriptor.Name,
            Description = descriptor.Description,
            UpdatedAt = updatedAt,
            Interface = workflowInterface,
            WorkflowRevision = descriptor.Revision,
            RegistryRevision = descriptor.RegistryRevision,
            Descriptor = descriptor,
            InputSchema = CreateInterfaceSchema(workflowInterface.Inputs),
            OutputSchema = CreateInterfaceSchema(workflowInterface.Outputs)
        };
    }

    private static NodeTypeDefinition ConvertType(WorkflowTypeDescriptor type)
        => new()
        {
            Type = type.Type,
            Optional = type.Optional,
            TypeName = type.TypeName,
            Values = type.Values.ToList(),
            TypeArgs = type.TypeArguments.Select(ConvertType).ToList()
        };

    private static WorkflowSchemaDefinition CreateInterfaceSchema(
        IEnumerable<WorkflowInterfacePin> pins)
    {
        var schema = new WorkflowSchemaDefinition();
        foreach (var pin in pins)
        {
            var property = ConvertInterfaceType(
                pin.Type,
                pin.Description,
                pin is WorkflowInterfaceInput input &&
                input.Default.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
                    ? input.Default
                    : null);
            if (pin is WorkflowInterfaceInput inputPin)
            {
                property.Minimum = inputPin.Min;
                property.Maximum = inputPin.Max;
                if (inputPin.Required)
                    schema.Required.Add(pin.Name);
            }
            schema.Properties[pin.Name] = property;
        }
        return schema;
    }

    private static WorkflowPropertyDefinition ConvertInterfaceType(
        NodeTypeDefinition type,
        string description,
        object? defaultValue)
    {
        var property = new WorkflowPropertyDefinition
        {
            Type = type.Type,
            Title = type.TypeName,
            Description = description,
            Default = defaultValue,
            Enum = type.Values
        };
        if (string.Equals(type.Type, "list", StringComparison.OrdinalIgnoreCase))
        {
            property.Type = "array";
            property.Items = type.TypeArgs?.Count > 0
                ? ConvertInterfaceType(type.TypeArgs[0], "", null)
                : new WorkflowPropertyDefinition { Type = "any" };
        }
        return property;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _catalog.Dispose();
        if (_ownsClient)
            _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
