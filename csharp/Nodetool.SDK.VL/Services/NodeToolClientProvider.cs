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

            // If client doesn't exist or connection settings changed, create new client
            if (_client == null || url != _currentUrl || key != _currentApiKey)
            {
                _client?.Dispose();
                
                _currentUrl = url;
                _currentApiKey = key;

                var workerUri = new Uri(url);
                var apiBaseUrl = TryDeriveApiBaseUrl(workerUri);
                _currentApiBaseUrl = apiBaseUrl;

                var options = new NodeToolClientOptions
                {
                    WorkerWebSocketUrl = workerUri,
                    ApiBaseUrl = apiBaseUrl,
                    AuthToken = key,
                };

                // Pass apiKey separately too (some deployments expect Bearer on HTTP; WS payload uses AuthToken).
                _client = new NodeToolExecutionClient(options, apiKey: key);
                _client.ConnectionStatusChanged += OnClientStatusChanged;
                
                Status = "disconnected";
                StatusChanged?.Invoke(Status);
            }

            return _client;
        }
    }

    /// <summary>
    /// Connect the shared client to the server.
    /// </summary>
    /// <returns>True if connected successfully.</returns>
    public static async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = GetClient();
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
        if (_client != null)
        {
            await _client.DisconnectAsync();
            Status = "disconnected";
            StatusChanged?.Invoke(Status);
        }
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
}
