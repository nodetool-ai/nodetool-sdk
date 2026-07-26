namespace Nodetool.SDK.WebSocket;

/// <summary>
/// Lowest-level injectable WebSocket transport used by the execution client.
/// Implementations own framing, correlation, and receive dispatch.
/// </summary>
public interface INodeToolWebSocketTransport :
    IDisposable,
    IAsyncDisposable
{
    bool IsConnected { get; }

    event EventHandler<MessageReceivedEventArgs>? MessageReceived;
    event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    Task<bool> ConnectAsync(
        Uri uri,
        string? bearerToken,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync();

    Task<bool> SendMessageAsync(
        object message,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, object?>?> SendRequestAsync(
        string command,
        Dictionary<string, object?>? data = null,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        string? requestId = null);
}
