using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.WebSocket;

/// <summary>
/// WebSocket client with MessagePack support and JSON fallback for NodeTool communication.
/// Integrates with the type registry system for automatic message deserialization.
/// </summary>
public class MessagePackWebSocketClient : IDisposable
{
    private readonly ILogger _logger;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private bool _disposed = false;
    private readonly MessagePackSerializerOptions _options;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _pendingRequests = new();

    /// <summary>
    /// Event fired when a message is received and deserialized.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Event fired when connection status changes.
    /// </summary>
    public event EventHandler<ConnectionStatusEventArgs>? ConnectionStatusChanged;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public WebSocketState State => _webSocket?.State ?? WebSocketState.None;

    /// <summary>
    /// Whether the client is currently connected.
    /// </summary>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public MessagePackWebSocketClient(ILogger? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

        // We need to interop with Python msgpack which uses map/dict structures.
        // Contractless resolver handles Dictionary<string, object> value trees well.
        _options = MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(
                ContractlessStandardResolver.Instance,
                StandardResolver.Instance
            )
        );
    }

    /// <summary>
    /// Connect to a NodeTool WebSocket endpoint.
    /// </summary>
    /// <param name="uri">WebSocket URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection succeeded</returns>
    public async Task<bool> ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_webSocket != null)
            {
                await DisconnectAsync();
            }

            _webSocket = new ClientWebSocket();
            // WebSocket-level keepalive frames (independent of NodeTool's application-level ping/pong).
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            _cancellationTokenSource = new CancellationTokenSource();

            _logger.LogInformation("Connecting to NodeTool WebSocket: {Uri}", uri);

            await _webSocket.ConnectAsync(uri, cancellationToken);

            _logger.LogInformation("Successfully connected to NodeTool WebSocket");

            // Start receiving messages
            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            OnConnectionStatusChanged(new ConnectionStatusEventArgs 
            { 
                Status = "connected", 
                Message = "Successfully connected to NodeTool" 
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to NodeTool WebSocket: {Uri}", uri);
            
            OnConnectionStatusChanged(new ConnectionStatusEventArgs 
            { 
                Status = "error", 
                Message = $"Connection failed: {ex.Message}" 
            });

            return false;
        }
    }

    /// <summary>
    /// Disconnect from the WebSocket.
    /// </summary>
    public async Task DisconnectAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();

            if (_webSocket?.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
            }

            OnConnectionStatusChanged(new ConnectionStatusEventArgs 
            { 
                Status = "disconnected", 
                Message = "Disconnected from NodeTool" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during WebSocket disconnect");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// Send a message using MessagePack serialization.
    /// </summary>
    /// <param name="message">Message object to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message was sent successfully</returns>
    public async Task<bool> SendMessageAsync(object message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send message - WebSocket not connected");
            return false;
        }

        await _sendSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Serialize using MessagePack (map-based)
            var data = MessagePackSerializer.Serialize(message, _options);
            
            if (data.Length == 0)
            {
                _logger.LogWarning("Failed to serialize message of type {Type}", message.GetType().Name);
                return false;
            }

            var buffer = new ArraySegment<byte>(data);
            await _webSocket!.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);

            _logger.LogDebug("Sent MessagePack message: {Type}, {Size} bytes", 
                message.GetType().Name, data.Length);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WebSocket message");
            return false;
        }
        finally
        {
            _sendSemaphore.Release();
        }
    }

    /// <summary>
    /// Send a request command and await the correlated response.
    /// Adds a top-level <c>request_id</c> to the command envelope and waits for
    /// a server message with the same <c>request_id</c> echoed back.
    /// </summary>
    /// <param name="command">Command name (e.g. "get_node_metadata").</param>
    /// <param name="data">Command data payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="timeout">Response timeout (default 30 s).</param>
    /// <returns>The raw response message as a string-keyed dictionary, or null on failure.</returns>
    public async Task<Dictionary<string, object?>?> SendRequestAsync(
        string command,
        Dictionary<string, object?> data,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var requestId = Guid.NewGuid().ToString("N");

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[requestId] = tcs;

        var envelope = CreateRequestEnvelope(command, data, requestId);
        if (!await SendMessageAsync(envelope, cancellationToken))
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new InvalidOperationException($"Failed to send '{command}' request over WebSocket");
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
        linkedCts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            var rawBytes = await tcs.Task;
            return MessagePackSerializer.Deserialize<Dictionary<string, object?>>(rawBytes, _options);
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    internal static Dictionary<string, object?> CreateRequestEnvelope(
        string command,
        Dictionary<string, object?> data,
        string requestId) => new()
        {
            ["command"] = command,
            ["request_id"] = requestId,
            ["data"] = data
        };

    /// <summary>
    /// Send a workflow execution request.
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="inputs">Input parameters</param>
    /// <param name="jobId">Optional job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if request was sent successfully</returns>
    [Obsolete("Legacy message shape. Prefer NodeToolExecutionClient which uses run_job over the worker protocol.")]
    public async Task<bool> ExecuteWorkflowAsync(
        string workflowId, 
        Dictionary<string, object> inputs,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
        // Keep for backwards compatibility, but the canonical protocol is run_job.
        var request = new WorkflowExecuteRequest
        {
            workflow_id = workflowId,
            inputs = inputs,
            job_id = jobId ?? Guid.NewGuid().ToString()
        };

        return await SendMessageAsync(request, cancellationToken);
    }

    /// <summary>
    /// Send a single node execution request.
    /// </summary>
    /// <param name="nodeType">Node type identifier</param>
    /// <param name="inputs">Input parameters</param>
    /// <param name="nodeId">Optional node identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if request was sent successfully</returns>
    [Obsolete("Legacy message shape. Prefer NodeToolExecutionClient which uses run_job over the worker protocol.")]
    public async Task<bool> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object> inputs,
        string? nodeId = null,
        CancellationToken cancellationToken = default)
    {
        // Keep for backwards compatibility, but the canonical protocol is run_job with a minimal graph.
        var request = new NodeExecuteRequest
        {
            node_type = nodeType,
            inputs = inputs,
            node_id = nodeId ?? Guid.NewGuid().ToString()
        };

        return await SendMessageAsync(request, cancellationToken);
    }

    /// <summary>
    /// Cancel a running job.
    /// </summary>
    /// <param name="jobId">Job identifier to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation request was sent successfully</returns>
    [Obsolete("Legacy message shape. Prefer NodeToolExecutionClient which uses cancel_job over the worker protocol.")]
    public async Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        // Keep for backwards compatibility, but the canonical protocol is cancel_job.
        var request = new JobCancelRequest { job_id = jobId };
        return await SendMessageAsync(request, cancellationToken);
    }

    /// <summary>
    /// Main receive loop for processing incoming WebSocket messages.
    /// </summary>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 16]; // 16KB buffer
        var messageBuffer = new List<byte>();

        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            try
            {
                var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation("WebSocket closed by server");
                    break;
                }

                // Accumulate message data
                messageBuffer.AddRange(buffer.Take(result.Count));

                if (result.EndOfMessage)
                {
                    // Process complete message
                    await ProcessMessage(messageBuffer.ToArray(), result.MessageType);
                    messageBuffer.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("WebSocket receive loop cancelled");
                break;
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error in receive loop");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in WebSocket receive loop");
                break;
            }
        }

        OnConnectionStatusChanged(new ConnectionStatusEventArgs 
        { 
            Status = "disconnected", 
            Message = "Receive loop ended" 
        });
    }

    /// <summary>
    /// Process a complete WebSocket message.
    /// </summary>
    private async Task ProcessMessage(byte[] data, WebSocketMessageType messageType)
    {
        try
        {
            object? message = null;
            string? typeName = null;

            if (messageType == WebSocketMessageType.Binary)
            {
                // Try MessagePack first
                message = await TryDeserializeMessagePack(data);
            }
            else if (messageType == WebSocketMessageType.Text)
            {
                // We run MessagePack-only for workflow execution; text frames are unexpected.
                var jsonText = Encoding.UTF8.GetString(data);
                _logger.LogWarning("Received unexpected text WebSocket message: {Text}", jsonText);
                message = new Dictionary<string, object?> { ["type"] = "text", ["text"] = jsonText };
            }

            if (message != null)
            {
                // Server sends { type: "ping", ts } every ~25s; reply so middleboxes and future server logic stay happy.
                if (message is Dictionary<string, object?> appDict &&
                    appDict.TryGetValue("type", out var appType) &&
                    appType is string appTypeStr &&
                    appTypeStr == "ping")
                {
                    var pong = new Dictionary<string, object?>
                    {
                        ["type"] = "pong",
                        ["ts"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    if (!await SendMessageAsync(pong, CancellationToken.None))
                    {
                        _logger.LogDebug("Failed to send pong reply to server ping");
                    }
                    return;
                }

                typeName = ExtractTypeName(message);
                
                OnMessageReceived(new MessageReceivedEventArgs
                {
                    Message = message,
                    TypeName = typeName,
                    RawData = data,
                    MessageType = messageType
                });
            }
            else
            {
                _logger.LogWarning("Failed to deserialize message of type {MessageType}, {Size} bytes", 
                    messageType, data.Length);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WebSocket message");
        }
    }

    /// <summary>
    /// Try to deserialize MessagePack data by detecting message type.
    /// </summary>
    private Task<object?> TryDeserializeMessagePack(byte[] data)
    {
        try
        {
            // Peek at "type" and "request_id" first
            var tempDict = MessagePackSerializer.Deserialize<Dictionary<string, object?>>(data, _options);

            // Complete a pending request-reply correlation before continuing.
            if (tempDict.TryGetValue("request_id", out var reqIdObj) && reqIdObj is string reqId &&
                _pendingRequests.TryGetValue(reqId, out var pendingTcs))
            {
                pendingTcs.TrySetResult(data);
                // Fall through — still fire MessageReceived so callers can observe the response.
            }

            if (tempDict.TryGetValue("type", out var typeObj) && typeObj is string typeStr)
            {
                object? message;
                switch (typeStr)
                {
                    case "job_update":
                        message = MessagePackSerializer.Deserialize<JobUpdate>(data, _options);
                        break;
                    case "node_update":
                        message = MessagePackSerializer.Deserialize<NodeUpdate>(data, _options);
                        break;
                    case "output_update":
                        message = MessagePackSerializer.Deserialize<OutputUpdate>(data, _options);
                        break;
                    case "preview_update":
                        message = MessagePackSerializer.Deserialize<PreviewUpdate>(data, _options);
                        break;
                    case "progress_update":
                        message = MessagePackSerializer.Deserialize<ProgressUpdate>(data, _options);
                        break;
                    case "node_progress":
                        message = MessagePackSerializer.Deserialize<NodeProgress>(data, _options);
                        break;
                    case "connection_status":
                        message = MessagePackSerializer.Deserialize<ConnectionStatus>(data, _options);
                        break;
                    case "error":
                        message = MessagePackSerializer.Deserialize<ErrorMessage>(data, _options);
                        break;
                    default:
                        message = tempDict;
                        break;
                }
                return Task.FromResult<object?>(message);
            }

            return Task.FromResult<object?>(tempDict);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize MessagePack data ({Size} bytes)", data.Length);
            return Task.FromResult<object?>(null);
        }
    }

    private static string? ExtractTypeName(object message)
    {
        if (message is JobUpdate ju) return ju.type;
        if (message is NodeUpdate nu) return nu.type;
        if (message is OutputUpdate ou) return ou.type;
        if (message is PreviewUpdate pru) return pru.type;
        if (message is ProgressUpdate pu) return pu.type;
        if (message is NodeProgress np) return np.type;
        if (message is ConnectionStatus cs) return cs.type;
        if (message is ErrorMessage em) return em.type;
        if (message is Dictionary<string, object?> dict && dict.TryGetValue("type", out var t) && t is string ts) return ts;
        return null;
    }

    /// <summary>
    /// Fire the MessageReceived event.
    /// </summary>
    protected virtual void OnMessageReceived(MessageReceivedEventArgs args)
    {
        MessageReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Fire the ConnectionStatusChanged event.
    /// </summary>
    protected virtual void OnConnectionStatusChanged(ConnectionStatusEventArgs args)
    {
        ConnectionStatusChanged?.Invoke(this, args);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _ = DisconnectAsync();
            _sendSemaphore.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Event args for received WebSocket messages.
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public object Message { get; init; } = null!;
    public string? TypeName { get; init; }
    public byte[] RawData { get; init; } = Array.Empty<byte>();
    public WebSocketMessageType MessageType { get; init; }
}

/// <summary>
/// Event args for connection status changes.
/// </summary>
public class ConnectionStatusEventArgs : EventArgs
{
    public string Status { get; init; } = "";
    public string? Message { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
