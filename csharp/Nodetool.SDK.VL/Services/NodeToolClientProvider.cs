using Nodetool.SDK.Api;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Connection;
using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;
using Nodetool.SDK.VL.Factories;
using Nodetool.SDK.VL.Utilities;
using VL.Core;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Provides a shared NodeTool execution client for VL nodes.
/// This ensures all nodes use the same connection.
/// </summary>
public static class NodeToolClientProvider
{
    public const int DefaultExecutionTimeoutSeconds = 300;
    public const int MaximumExecutionTimeoutSeconds = 86400;
    public const int DefaultInlineMediaLimitBytes =
        MediaInputPreparer.DefaultInlineLimitBytes;
    public const int MaximumInlineMediaLimitBytes = 64 * 1024 * 1024;

    private static readonly object _lock = new();
    private static readonly VlNodeToolHostSettings _fallbackSettings = new();
    private static readonly NodeToolConnectionSession _fallbackConnectionSession =
        new(_fallbackSettings.CreateProfile());
    private static readonly INodeToolExecutionClient _nullClient = new NullNodeToolExecutionClient();

    static NodeToolClientProvider()
    {
        _fallbackConnectionSession.StatusChanged +=
            status => OnClientStatusChanged(
                status,
                _fallbackSettings);
    }

    private static NodeToolConnectionSession Session
    {
        get
        {
            try
            {
                var hostSession =
                    AppHost.CurrentOrGlobal.Services.GetService(
                        typeof(NodeToolConnectionSession))
                    as NodeToolConnectionSession;
                if (hostSession is { IsDisposed: false })
                    return hostSession;
            }
            catch
            {
                // Unit tests and design-time factory calls may not have a
                // current AppHost. They use the non-host-owned fallback.
            }

            return _fallbackConnectionSession;
        }
    }

    private static VlNodeToolHostSettings Settings
    {
        get
        {
            try
            {
                var hostSettings =
                    AppHost.CurrentOrGlobal.Services.GetService(
                        typeof(VlNodeToolHostSettings))
                    as VlNodeToolHostSettings;
                if (hostSettings != null)
                    return hostSettings;
            }
            catch
            {
                // Unit tests and design-time factory calls use the fallback.
            }

            return _fallbackSettings;
        }
    }

    /// <summary>
    /// Creates a connection session suitable for AppHost-managed ownership.
    /// </summary>
    internal static NodeToolConnectionSession CreateHostSession(
        VlNodeToolHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var session = new NodeToolConnectionSession(
            settings.CreateProfile());
        session.StatusChanged +=
            status => OnClientStatusChanged(status, settings);
        return session;
    }

    /// <summary>
    /// Applies current Connect-node settings to an AppHost-owned session. The
    /// facade resolves the service from the current AppHost and never owns it.
    /// </summary>
    internal static void UseHostSession(
        NodeToolConnectionSession connectionSession,
        VlNodeToolHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(connectionSession);
        ArgumentNullException.ThrowIfNull(settings);
        lock (_lock)
        {
            connectionSession.Configure(settings.CreateProfile());
        }
        NodesFactory.SetEnabled(settings.LoadNodes);
        NodesFactory.SetShowAllNodes(settings.ShowAllNodes);
        WorkflowNodeFactory.SetEnabled(settings.LoadWorkflows);
    }

    /// <summary>
    /// Current connection status.
    /// </summary>
    public static string Status =>
        Settings.ConfigurationError is null
            ? Session.Status
            : "error";

    /// <summary>
    /// Last error message if connection failed.
    /// </summary>
    public static string? LastError =>
        Settings.ConfigurationError ??
        Session.LastError ??
        Session.CurrentClient?.LastError;

    /// <summary>
    /// Whether the client is currently connected.
    /// </summary>
    public static bool IsConnected => Session.IsConnected;

    /// <summary>
    /// Current worker URL as configured by the Connect node.
    /// </summary>
    public static string CurrentWorkerUrl => Settings.WorkerUrl;

    /// <summary>
    /// Current API base URL derived from the worker URL (ws/wss → http/https).
    /// Used for workflow/node metadata discovery.
    /// </summary>
    public static Uri? CurrentApiBaseUrl => Settings.ApiBaseUrl;

    /// <summary>
    /// Current auth token / API key configured via the Connect node (if any).
    /// Used for HTTP requests (assets/workflow discovery) and WS payload auth token.
    /// </summary>
    public static string? CurrentAuthToken => Settings.ApiKey;

    /// <summary>
    /// Default timeout used by VL execution nodes. Individual nodes can override it.
    /// </summary>
    public static int ExecutionTimeoutSeconds =>
        Settings.ExecutionTimeoutSeconds;

    /// <summary>
    /// Maximum media payload embedded in a run_job frame. Larger values are uploaded first.
    /// </summary>
    public static int InlineMediaLimitBytes =>
        Settings.InlineMediaLimitBytes;

    /// <summary>
    /// Whether workflow discovery should use the shared execution WebSocket when connected.
    /// HTTP remains the bootstrap transport while the socket is unavailable.
    /// </summary>
    public static bool UseWebSocketDiscovery =>
        Settings.UseWebSocketDiscovery;

    public static bool LoadNodes => Settings.LoadNodes;

    public static bool ShowAllNodes => Settings.ShowAllNodes;

    public static bool LoadWorkflows => Settings.LoadWorkflows;

    public static WorkflowExecutionOptions ExecutionOptions =>
        Settings.ExecutionOptions;

    public static void SetAutoReconnect(bool enabled)
    {
        Settings.SetAutoReconnect(enabled);
        Session.SetAutoReconnect(enabled);
    }

    public static void SetUseWebSocketDiscovery(bool enabled)
    {
        if (!Settings.SetUseWebSocketDiscovery(enabled))
            return;
        WorkflowNodeFactory.RequestRefresh();
    }

    public static void SetLoadNodes(bool enabled)
    {
        Settings.SetLoadNodes(enabled);
        NodesFactory.SetEnabled(enabled);
    }

    public static void SetLoadWorkflows(bool enabled)
    {
        Settings.SetLoadWorkflows(enabled);
        WorkflowNodeFactory.SetEnabled(enabled);
    }

    public static void SetShowAllNodes(bool enabled)
    {
        if (!Settings.SetShowAllNodes(enabled))
            return;
        NodesFactory.SetShowAllNodes(enabled);
    }

    public static void RefreshDiscovery()
    {
        if (Settings.LoadNodes)
            NodesFactory.RequestRefresh();
        if (Settings.LoadWorkflows)
            WorkflowNodeFactory.RequestRefresh();
    }

    public static void SetWorkflowPersistence(WorkflowPersistence value)
        => Settings.SetWorkflowPersistence(value);

    public static void SetWorkflowEventDetail(WorkflowEventDetail value)
        => Settings.SetWorkflowEventDetail(value);

    public static void SetWorkflowAssetPersistence(
        WorkflowAssetPersistence value)
        => Settings.SetWorkflowAssetPersistence(value);

    internal static async Task<WorkflowExecutionOptions?>
        ResolveExecutionOptionsAsync(
            CancellationToken cancellationToken = default)
    {
        var options = Settings.ExecutionOptions;
        if (WorkflowExecutionOptionNegotiator.IsDefault(options))
            return null;

        var capabilities = await Session
            .GetSdkCapabilitiesAsync(cancellationToken)
            .ConfigureAwait(false);
        WorkflowExecutionOptionNegotiator.EnsureSupported(
            capabilities,
            options);
        return options;
    }

    internal static async Task<bool> SupportsTemporaryAssetUploadAsync(
        CancellationToken cancellationToken = default)
    {
        if (Settings.ExecutionOptions.AssetPersistence !=
            WorkflowAssetPersistence.Temporary)
        {
            return false;
        }

        var capabilities = await Session
            .GetSdkCapabilitiesAsync(cancellationToken)
            .ConfigureAwait(false);
        return capabilities.Profiles.TryGetValue(
                "temporary_asset_upload",
                out var status) &&
            string.Equals(status, "available", StringComparison.Ordinal);
    }

    /// <summary>
    /// Updates the shared execution timeout without resetting discovery or the connection.
    /// </summary>
    public static void SetExecutionTimeoutSeconds(int timeoutSeconds)
    {
        Settings.SetExecutionTimeoutSeconds(timeoutSeconds);
    }

    /// <summary>
    /// Resolves a per-node timeout. Zero or a negative value inherits the shared default.
    /// </summary>
    public static int ResolveExecutionTimeoutSeconds(int perNodeTimeoutSeconds)
        => perNodeTimeoutSeconds > 0
            ? Math.Clamp(perNodeTimeoutSeconds, 1, MaximumExecutionTimeoutSeconds)
            : Settings.ExecutionTimeoutSeconds;

    public static void SetInlineMediaLimitBytes(int limitBytes)
    {
        Settings.SetInlineMediaLimitBytes(limitBytes);
    }

    /// <summary>
    /// Event raised when connection status changes.
    /// </summary>
    public static event Action<string>? StatusChanged;

    /// <summary>
    /// Get or create the shared execution client.
    /// </summary>
    /// <param name="serverUrl">Server URL (default: ws://localhost:7777)</param>
    /// <param name="apiKey">Optional API key</param>
    /// <returns>The shared execution client.</returns>
    public static INodeToolExecutionClient GetClient(string? serverUrl = null, string? apiKey = null)
    {
        lock (_lock)
        {
            var settings = Settings;
            var url = NormalizeServerUrl(
                serverUrl ?? settings.WorkerUrl);
            var key = NormalizeApiKey(
                apiKey ?? settings.ApiKey);

            // If settings changed, dispose current client but DO NOT eagerly create a new one here.
            // This is important for VL: default value injection should never fail node instantiation.
            if (url != settings.WorkerUrl ||
                key != settings.ApiKey)
            {
                Configure(url, key, disposeExistingClient: true);
            }

            return Session.CurrentClient ?? _nullClient;
        }
    }

    /// <summary>
    /// Updates the connection configuration without forcing client creation.
    /// Safe to call during VL default value injection.
    /// </summary>
    public static void Configure(string serverUrl, string? apiKey, bool disposeExistingClient = true)
    {
        lock (_lock)
        {
            var settings = Settings;
            var normalizedUrl = NormalizeServerUrl(serverUrl);
            var normalizedApiKey = NormalizeApiKey(apiKey);
            var configurationChanged =
                !string.Equals(
                    normalizedUrl,
                    settings.WorkerUrl,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    normalizedApiKey,
                    settings.ApiKey,
                    StringComparison.Ordinal);

            if (!configurationChanged)
            {
                settings.Configure(
                    normalizedUrl,
                    normalizedApiKey);
                return;
            }

            try
            {
                settings.Configure(
                    normalizedUrl,
                    normalizedApiKey);
                Session.Configure(settings.CreateProfile());
            }
            catch (Exception ex)
            {
                settings.SetConfigurationError(
                    $"Invalid URL: {VlLog.SafeError(ex, normalizedApiKey)}");
            }

            VlReadinessLog.Reset();
            VlReadinessLog.MarkRegistered();
            WorkflowNodeFactory.Reset();
            NodesFactory.Reset();

            StatusChanged?.Invoke(Status);
        }
    }

    /// <summary>
    /// Connect the shared client to the server.
    /// </summary>
    /// <returns>True if connected successfully.</returns>
    public static async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Session.GetConnectedClientAsync(
                cancellationToken);
            StatusChanged?.Invoke(Status);
            return true;
        }
        catch (Exception ex)
        {
            var safeError =
                LastError ??
                VlLog.SafeError(ex, CurrentAuthToken);
            VlLog.Error(
                $"connect failed to '{CurrentWorkerUrl}': {safeError}");
            StatusChanged?.Invoke(Status);
            return false;
        }
    }

    /// <summary>
    /// Borrows the HTTP API client owned by the current AppHost connection
    /// session. Callers must not dispose the returned client.
    /// </summary>
    internal static Task<INodetoolClient> GetApiClientAsync(
        CancellationToken cancellationToken = default)
        => Session.GetApiClientAsync(cancellationToken);

    internal static Task<SdkCapabilitiesResponse> GetSdkCapabilitiesAsync(
        CancellationToken cancellationToken = default)
        => Session.GetSdkCapabilitiesAsync(cancellationToken);

    /// <summary>
    /// Disconnect the shared client.
    /// </summary>
    public static async Task DisconnectAsync()
    {
        await Session.DisconnectAsync();
        StatusChanged?.Invoke(Status);
    }

    /// <summary>
    /// Reconnect the client (disconnect then connect).
    /// </summary>
    public static async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        await DisconnectAsync();
        return await ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// Reset the client (dispose and recreate on next GetClient call).
    /// </summary>
    public static void Reset()
    {
        lock (_lock)
        {
            Session.Reset();
            VlReadinessLog.Reset();
            VlReadinessLog.MarkRegistered();
            WorkflowNodeFactory.Reset();
            NodesFactory.Reset();
            StatusChanged?.Invoke(Status);
        }
    }

    private static void OnClientStatusChanged(
        string status,
        VlNodeToolHostSettings settings)
    {
        if (status == "connected" && settings.LoadNodes)
            NodesFactory.RequestRefresh();
        if (status == "connected" && settings.LoadWorkflows)
            WorkflowNodeFactory.RequestRefresh();
        StatusChanged?.Invoke(status);
    }

    private static string NormalizeServerUrl(string serverUrl)
    {
        var value = serverUrl.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return value;

        var builder = new UriBuilder(uri);
        if (builder.Path == "/")
            builder.Path = "";

        var normalized = builder.Uri.AbsoluteUri;
        return string.IsNullOrEmpty(builder.Query) && string.IsNullOrEmpty(builder.Fragment)
            ? normalized.TrimEnd('/')
            : normalized;
    }

    private static string? NormalizeApiKey(string? apiKey)
        => string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;

    private sealed class NullNodeToolExecutionClient : INodeToolExecutionClient
    {
        public bool IsConnected => false;
        public string ConnectionStatus => "disconnected";
        public string? LastError => NodeToolClientProvider.LastError;
        public event Action<string>? ConnectionStatusChanged;

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            ConnectionStatusChanged?.Invoke(ConnectionStatus);
            return Task.FromResult(false);
        }

        public Task DisconnectAsync() => Task.CompletedTask;

        public Task<IExecutionSession> ExecuteWorkflowAsync(string workflowId, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(string workflowName, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(string workflowName, string inputName, object? inputValue, CancellationToken cancellationToken = default)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<IExecutionSession> ExecuteWorkflowByNameAsync(string workflowName, CancellationToken cancellationToken = default, params (string Name, object? Value)[] inputs)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<IExecutionSession> ExecuteGraphAsync(Nodetool.SDK.Types.Graph graph, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<IExecutionSession> ExecuteNodeAsync(string nodeType, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default)
            => Task.FromException<IExecutionSession>(new InvalidOperationException("Not connected."));

        public Task<List<NodeMetadataResponse>> GetNodeTypesAsync(CancellationToken cancellationToken = default)
            => Task.FromException<List<NodeMetadataResponse>>(new InvalidOperationException("Not connected."));

        public Task<NodeTypeInventoryResponse> GetNodeTypeInventoryAsync(
            int cursor = 0,
            int limit = 100,
            CancellationToken cancellationToken = default)
            => Task.FromException<NodeTypeInventoryResponse>(new InvalidOperationException("Not connected."));

        public Task<NodeMetadataResponse?> GetNodeAsync(string nodeType, CancellationToken cancellationToken = default)
            => Task.FromException<NodeMetadataResponse?>(new InvalidOperationException("Not connected."));

        public Task<List<WorkflowResponse>> GetWorkflowsAsync(CancellationToken cancellationToken = default)
            => Task.FromException<List<WorkflowResponse>>(new InvalidOperationException("Not connected."));

        public Task<WorkflowResponse?> GetWorkflowAsync(string workflowId, CancellationToken cancellationToken = default)
            => Task.FromException<WorkflowResponse?>(new InvalidOperationException("Not connected."));

        public Task<List<WorkflowSummaryResponse>> GetWorkflowSummariesAsync(CancellationToken cancellationToken = default)
            => Task.FromException<List<WorkflowSummaryResponse>>(new InvalidOperationException("Not connected."));

        public Task<WorkflowInterfaceResponse> GetWorkflowInterfaceAsync(string workflowId, CancellationToken cancellationToken = default)
            => Task.FromException<WorkflowInterfaceResponse>(new InvalidOperationException("Not connected."));

        public Task<WorkflowInterfacesResponse> GetWorkflowInterfacesAsync(IReadOnlyCollection<string> workflowIds, CancellationToken cancellationToken = default)
            => Task.FromException<WorkflowInterfacesResponse>(new InvalidOperationException("Not connected."));

        public Task<List<AssetResponse>> GetAssetsAsync(
            string? contentType = null,
            string? parentId = null,
            int pageSize = 10000,
            CancellationToken cancellationToken = default)
            => Task.FromException<List<AssetResponse>>(new InvalidOperationException("Not connected."));

        public Task<AssetResponse?> GetAssetAsync(string assetId, CancellationToken cancellationToken = default)
            => Task.FromException<AssetResponse?>(new InvalidOperationException("Not connected."));

        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
