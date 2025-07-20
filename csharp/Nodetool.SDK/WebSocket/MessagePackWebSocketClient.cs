using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MessagePack;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.WebSocket;

/// <summary>
/// WebSocket client with MessagePack support and JSON fallback for NodeTool communication.
/// Integrates with the type registry system for automatic message deserialization.
/// </summary>
public class MessagePackWebSocketClient : IDisposable
{
    private readonly TypeLookupService _typeLookup;
    private readonly ILogger<MessagePackWebSocketClient> _logger;
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
    private bool _disposed = false;

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

    public MessagePackWebSocketClient(
        TypeLookupService typeLookup,
        ILogger<MessagePackWebSocketClient>? logger = null)
    {
        _typeLookup = typeLookup;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MessagePackWebSocketClient>.Instance;
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
            // Serialize using MessagePack
            var data = _typeLookup.Serialize(message);
            
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
    /// Send a workflow execution request.
    /// </summary>
    /// <param name="workflowId">Workflow identifier</param>
    /// <param name="inputs">Input parameters</param>
    /// <param name="jobId">Optional job identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if request was sent successfully</returns>
    public async Task<bool> ExecuteWorkflowAsync(
        string workflowId, 
        Dictionary<string, object> inputs,
        string? jobId = null,
        CancellationToken cancellationToken = default)
    {
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
    public async Task<bool> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object> inputs,
        string? nodeId = null,
        CancellationToken cancellationToken = default)
    {
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
    public async Task<bool> CancelJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
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
                // Fallback to JSON
                var jsonText = Encoding.UTF8.GetString(data);
                message = await TryDeserializeJson(jsonText);
            }

            if (message != null)
            {
                typeName = _typeLookup.GetTypeName(message);
                
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
    private async Task<object?> TryDeserializeMessagePack(byte[] data)
    {
        try
        {
            // First, try to deserialize as a generic object to peek at the type
            var tempDict = MessagePackSerializer.Deserialize<Dictionary<string, object>>(data);
            
            if (tempDict.TryGetValue("type", out var typeObj) && typeObj is string typeName)
            {
                // Try known message types first
                return typeName switch
                {
                    "job_update" => _typeLookup.Deserialize<JobUpdate>(data),
                    "node_update" => _typeLookup.Deserialize<NodeUpdate>(data),
                    "output_update" => _typeLookup.Deserialize<OutputUpdate>(data),
                    "progress_update" => _typeLookup.Deserialize<ProgressUpdate>(data),
                    "connection_status" => _typeLookup.Deserialize<ConnectionStatus>(data),
                    "error" => _typeLookup.Deserialize<ErrorMessage>(data),
                    _ => _typeLookup.DeserializeByTypeName(data, typeName)
                };
            }

            return tempDict; // Fallback to dictionary if no type detected
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to deserialize MessagePack data");
            return null;
        }
    }

    /// <summary>
    /// Try to deserialize JSON data as fallback.
    /// </summary>
    private async Task<object?> TryDeserializeJson(string jsonText)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonText);
            if (jsonDoc.RootElement.TryGetProperty("type", out var typeElement))
            {
                var typeName = typeElement.GetString();
                if (!string.IsNullOrEmpty(typeName))
                {
                    // Convert JSON to MessagePack for consistent handling
                    var jsonBytes = Encoding.UTF8.GetBytes(jsonText);
                    return _typeLookup.DeserializeByTypeName(jsonBytes, typeName);
                }
            }

            // Fallback to generic dictionary
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to deserialize JSON data");
            return null;
        }
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