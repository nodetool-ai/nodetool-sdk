using Nodetool.SDK.Execution;
using Nodetool.SDK.Types;

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
                
                // Create type services
                var typeRegistry = new NodeToolTypeRegistry(
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NodeToolTypeRegistry>.Instance);
                var enumRegistry = new EnumRegistry(
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<EnumRegistry>.Instance);
                var typeLookup = new TypeLookupService(typeRegistry, enumRegistry,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<TypeLookupService>.Instance);
                typeLookup.Initialize();

                _client = new NodeToolExecutionClient(url, key, typeLookup);
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
}
