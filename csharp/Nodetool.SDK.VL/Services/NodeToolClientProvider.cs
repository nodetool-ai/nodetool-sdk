using Nodetool.SDK.Configuration;
using Nodetool.SDK.Execution;

namespace Nodetool.SDK.VL.Services;

/// <summary>
/// Provides a shared NodeTool execution client for VL nodes.
/// This ensures all nodes use the same connection.
/// </summary>
public static class NodeToolClientProvider
{
    private static NodeToolExecutionClient? _client;
    private static readonly object _lock = new();
    private static string _currentUrl = "ws://localhost:7777";
    private static string? _currentApiKey;
    private static Uri? _currentApiBaseUrl;
    private static readonly INodeToolExecutionClient _nullClient = new NullNodeToolExecutionClient();

    /// <summary>
    /// Current connection status.
    /// </summary>
    public static string Status { get; private set; } = "disconnected";

    /// <summary>
    /// Last error message if connection failed.
    /// </summary>
    public static string? LastError { get; private set; }

    /// <summary>
    /// Whether the client is currently connected.
    /// </summary>
    public static bool IsConnected => _client?.IsConnected ?? false;

    /// <summary>
    /// Current worker URL as configured by the Connect node.
    /// </summary>
    public static string CurrentWorkerUrl => _currentUrl;

    /// <summary>
    /// Current API base URL derived from the worker URL (ws/wss → http/https).
    /// Used for workflow/node metadata discovery.
    /// </summary>
    public static Uri? CurrentApiBaseUrl => _currentApiBaseUrl;

    /// <summary>
    /// Current auth token / API key configured via the Connect node (if any).
    /// Used for HTTP requests (assets/workflow discovery) and WS payload auth token.
    /// </summary>
    public static string? CurrentAuthToken => _currentApiKey;

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
            var url = serverUrl ?? _currentUrl;
            var key = apiKey ?? _currentApiKey;

            // If settings changed, dispose current client but DO NOT eagerly create a new one here.
            // This is important for VL: default value injection should never fail node instantiation.
            if (url != _currentUrl || key != _currentApiKey)
            {
                Configure(url, key, disposeExistingClient: true);
            }

            return _client ?? _nullClient;
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
            if (disposeExistingClient && _client != null)
            {
                try
                {
                    _client.ConnectionStatusChanged -= OnClientStatusChanged;
                    _client.Dispose();
                }
                catch
                {
                    // ignore dispose errors
                }
                _client = null;
            }

            _currentUrl = serverUrl;
            _currentApiKey = apiKey;

            try
            {
                var workerUri = new Uri(serverUrl);
                _currentApiBaseUrl = TryDeriveApiBaseUrl(workerUri);
                Status = "disconnected";
                LastError = null;
            }
            catch (Exception ex)
            {
                Status = "error";
                LastError = $"Invalid URL: {ex.Message}";
            }

            StatusChanged?.Invoke(Status);
        }
    }

    private static INodeToolExecutionClient EnsureClientCreated()
    {
        lock (_lock)
        {
            if (_client != null)
                return _client;

            try
            {
                var workerUri = new Uri(_currentUrl);
                var apiBaseUrl = TryDeriveApiBaseUrl(workerUri);
                _currentApiBaseUrl = apiBaseUrl;

                var options = new NodeToolClientOptions
                {
                    WorkerWebSocketUrl = workerUri,
                    ApiBaseUrl = apiBaseUrl,
                    AuthToken = _currentApiKey,
                };

                // Pass apiKey separately too (some deployments expect Bearer on HTTP; WS payload uses AuthToken).
                _client = new NodeToolExecutionClient(options, apiKey: _currentApiKey);
                _client.ConnectionStatusChanged += OnClientStatusChanged;

                Status = "disconnected";
                LastError = null;
                StatusChanged?.Invoke(Status);
                return _client;
            }
            catch (Exception ex)
            {
                Status = "error";
                LastError = $"Failed to create client: {ex.Message}";
                StatusChanged?.Invoke(Status);
                return _nullClient;
            }
        }
    }

    /// <summary>
    /// Connect the shared client to the server.
    /// </summary>
    /// <returns>True if connected successfully.</returns>
    public static async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = EnsureClientCreated();
        var result = await client.ConnectAsync(cancellationToken);
        
        if (result)
        {
            Status = "connected";
            LastError = null;
        }
        else
        {
            Status = "error";
            LastError = "Connection failed";
        }
        
        StatusChanged?.Invoke(Status);
        return result;
    }

    /// <summary>
    /// Disconnect the shared client.
    /// </summary>
    public static async Task DisconnectAsync()
    {
        var client = _client;
        if (client == null)
            return;

        await client.DisconnectAsync();
        Status = "disconnected";
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
            if (_client != null)
            {
                _client.ConnectionStatusChanged -= OnClientStatusChanged;
                _client.Dispose();
                _client = null;
            }
            Status = "disconnected";
            LastError = null;
            StatusChanged?.Invoke(Status);
        }
    }

    private static void OnClientStatusChanged(string status)
    {
        Status = status;
        if (status == "error" && _client != null)
        {
            LastError = _client.LastError;
        }
        StatusChanged?.Invoke(status);
    }

    private static Uri? TryDeriveApiBaseUrl(Uri workerUrl)
    {
        // Convert ws/wss to http/https and strip path/query/fragment.
        var scheme = workerUrl.Scheme switch
        {
            "ws" => "http",
            "wss" => "https",
            "http" => "http",
            "https" => "https",
            _ => "http"
        };

        var builder = new UriBuilder(workerUrl)
        {
            Scheme = scheme,
            Path = "",
            Query = "",
            Fragment = ""
        };

        return builder.Uri;
    }

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

        public void Dispose() { }
    }
}
