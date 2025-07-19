# NodeTool WebSocket Streaming Execution Plan

## 🎯 **Current Understanding**

✅ **Universal Execution Model**: All NodeTool execution uses WebSocket streaming  
✅ **Streaming vs Non-streaming**: Nodes marked as "streaming" just push more frequent updates  
✅ **Real-time Data Flow**: `output_update` messages carry actual data during execution  
✅ **Final Consolidation**: `job_update` completion message also contains final data

## 🔍 **WebSocket Protocol Analysis**

### **Request Format**

```json
{
  "type": "run_job",
  "command": "run_job",
  "data": {
    "type": "run_job_request",
    "api_url": "http://localhost:8000",
    "user_id": "1",
    "workflow_id": "e6d15280642f11f0b6430000751c9fe9",
    "auth_token": "local_token",
    "job_type": "workflow",
    "params": {
      // Workflow input parameters
    }
  }
}
```

### **Response Message Types**

#### **job_update** - Overall execution status

```json
{
  "type": "job_update",
  "status": "running|completed|error",
  "job_id": "962ea7e201624552b282477ffa4ce92b",
  "message": "Workflow completed in 0.49 seconds",
  "result": {
    "image_output": {
      "type": "image",
      "uri": "http://...",
      "asset_id": "...",
      "data": {
        /* actual image data object */
      }
    }
  },
  "error": null
}
```

#### **output_update** - Real-time output data

```json
{
  "type": "output_update",
  "node_id": "2da292f0-530e-44ba-98ed-f5b66c971480",
  "node_name": "Image Output",
  "output_name": "image_output",
  "value": {
    "type": "image",
    "uri": "http://...",
    "asset_id": "...",
    "data": {
      /* actual image data object */
    }
  }
}
```

#### **node_update** - Individual node progress

```json
{
  "type": "node_update",
  "node_id": "cd3eb796-f91a-4b5b-9366-6542b0b8ba0a",
  "node_name": "Crop",
  "status": "running|completed|error",
  "error": null,
  "logs": [],
  "result": {
    /* intermediate results */
  },
  "properties": {
    /* node state */
  }
}
```

## 📋 **Implementation Plan: WebSocket-Only Execution**

### **Phase 1: Core WebSocket Client** _(2-3 days)_

**🎯 Goal**: Build robust WebSocket client that handles NodeTool's exact protocol

#### **1.0 Dependencies**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Nodetool.SDK.csproj`

```xml
<PackageReference Include="MessagePack" Version="2.5.124" />
<PackageReference Include="MessagePackAnalyzer" Version="2.5.124" />
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

**Why MessagePack?**

- ✅ **Binary format**: More efficient than JSON for real-time data
- ✅ **VL Compatible**: VL has MessagePack support built-in
- ✅ **Unity Support**: Works with Unity and other C# platforms
- ✅ **Fallback**: Can still handle JSON for compatibility

#### **1.1 NodeTool WebSocket Client**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/NodeToolWebSocketClient.cs`

```csharp
public class NodeToolWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _webSocket;
    private readonly string _wsUrl;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger _logger;

    // Event handlers for different message types
    public event EventHandler<JobUpdateMessage>? JobUpdated;
    public event EventHandler<NodeUpdateMessage>? NodeUpdated;
    public event EventHandler<OutputUpdateMessage>? OutputUpdated;
    public event EventHandler<string>? RawMessageReceived; // For debugging

    public NodeToolWebSocketClient(string baseUrl, ILogger? logger = null)
    {
        _webSocket = new ClientWebSocket();
        _wsUrl = baseUrl.Replace("http://", "ws://").Replace("https://", "wss://") + "/predict";
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger ?? new NullLogger();
    }

    public async Task ConnectAsync()
    {
        _logger.LogInformation($"Connecting to NodeTool WebSocket: {_wsUrl}");

        await _webSocket.ConnectAsync(new Uri(_wsUrl), _cancellationTokenSource.Token);

        // Start message listening loop
        _ = Task.Run(MessageListenerLoop, _cancellationTokenSource.Token);

        _logger.LogInformation("Connected to NodeTool WebSocket");
    }

    public async Task<string> StartWorkflowAsync(
        string workflowId,
        Dictionary<string, object> parameters,
        string userId = "1",
        string authToken = "local_token")
    {
        var jobId = Guid.NewGuid().ToString("N")[..24]; // Match NodeTool job ID format

        var message = new
        {
            type = "run_job",
            command = "run_job",
            data = new
            {
                type = "run_job_request",
                api_url = _wsUrl.Replace("/predict", "").Replace("ws://", "http://"),
                user_id = userId,
                workflow_id = workflowId,
                auth_token = authToken,
                job_type = "workflow",
                @params = parameters,
                job_id = jobId
            }
        };

        await SendMessageAsync(message);

        _logger.LogInformation($"Started workflow: {workflowId} → Job: {jobId}");
        return jobId;
    }

    private async Task SendMessageAsync(object message)
    {
        // Try MessagePack first (binary), fallback to JSON (text)
        byte[] messageBytes;
        WebSocketMessageType messageType;

        try
        {
            // Serialize with MessagePack (binary format)
            messageBytes = MessagePackSerializer.Serialize(message);
            messageType = WebSocketMessageType.Binary;
            _logger.LogDebug($"Sending MessagePack message: {message.GetType().Name}");
        }
        catch (Exception ex)
        {
            // Fallback to JSON if MessagePack fails
            _logger.LogDebug($"MessagePack failed, falling back to JSON: {ex.Message}");
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            messageBytes = Encoding.UTF8.GetBytes(json);
            messageType = WebSocketMessageType.Text;
        }

        await _webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes),
            messageType,
            true,
            _cancellationTokenSource.Token);

        _logger.LogDebug($"Sent {messageType} message ({messageBytes.Length} bytes)");
    }

    private async Task MessageListenerLoop()
    {
        var buffer = new byte[16384]; // Larger buffer for binary data
        var messageBuffer = new List<byte>();

        while (_webSocket.State == WebSocketState.Open &&
               !_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                WebSocketReceiveResult result;
                messageBuffer.Clear();

                // Handle potentially large messages (both text and binary)
                do
                {
                    result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text ||
                        result.MessageType == WebSocketMessageType.Binary)
                    {
                        messageBuffer.AddRange(buffer.Take(result.Count));
                    }
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await ProcessBinaryMessage(messageBuffer.ToArray());
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var textMessage = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    await ProcessTextMessage(textMessage);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket message loop");
                await Task.Delay(1000); // Brief pause before retry
            }
        }
    }

    private async Task ProcessBinaryMessage(byte[] messageBytes)
    {
        try
        {
            _logger.LogDebug($"Received binary message ({messageBytes.Length} bytes)");

            // Try MessagePack deserialization first
            var messageObj = MessagePackSerializer.Deserialize<Dictionary<string, object>>(messageBytes);

            if (messageObj.TryGetValue("type", out var typeObj) && typeObj is string messageType)
            {
                await ProcessTypedMessage(messageType, messageBytes);
            }
            else
            {
                _logger.LogWarning("Binary message missing 'type' property");
            }
        }
        catch (MessagePackSerializationException ex)
        {
            _logger.LogError(ex, "Failed to deserialize MessagePack data");

            // Try interpreting as UTF-8 text as fallback
            try
            {
                var textMessage = Encoding.UTF8.GetString(messageBytes);
                await ProcessTextMessage(textMessage);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to process binary message as text fallback");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing binary message");
        }
    }

    private async Task ProcessTextMessage(string message)
    {
        try
        {
            _logger.LogDebug($"Received text message: {message}");
            RawMessageReceived?.Invoke(this, message);

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProperty))
            {
                _logger.LogWarning($"Text message missing 'type' property: {message}");
                return;
            }

            var messageType = typeProperty.GetString();
            await ProcessTypedMessage(messageType, Encoding.UTF8.GetBytes(message));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Failed to parse JSON message: {message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing text message: {message}");
        }
    }

    private async Task ProcessTypedMessage(string? messageType, byte[] messageBytes)
    {
        if (string.IsNullOrEmpty(messageType)) return;

        try
        {
            switch (messageType)
            {
                case "job_update":
                    var jobUpdate = DeserializeMessage<JobUpdateMessage>(messageBytes);
                    if (jobUpdate != null)
                        JobUpdated?.Invoke(this, jobUpdate);
                    break;

                case "node_update":
                    var nodeUpdate = DeserializeMessage<NodeUpdateMessage>(messageBytes);
                    if (nodeUpdate != null)
                        NodeUpdated?.Invoke(this, nodeUpdate);
                    break;

                case "output_update":
                    var outputUpdate = DeserializeMessage<OutputUpdateMessage>(messageBytes);
                    if (outputUpdate != null)
                        OutputUpdated?.Invoke(this, outputUpdate);
                    break;

                default:
                    _logger.LogDebug($"Unhandled message type: {messageType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process {messageType} message");
        }
    }

    private T? DeserializeMessage<T>(byte[] messageBytes) where T : class
    {
        // Try MessagePack first (more efficient)
        try
        {
            return MessagePackSerializer.Deserialize<T>(messageBytes);
        }
        catch (MessagePackSerializationException)
        {
            _logger.LogDebug($"MessagePack failed for {typeof(T).Name}, trying JSON");
        }

        // Fallback to JSON
        try
        {
            var json = Encoding.UTF8.GetString(messageBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Failed to deserialize {typeof(T).Name} from JSON");
        }

        return null;
    }

    public bool IsConnected => _webSocket.State == WebSocketState.Open;

    public async Task DisconnectAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnect", CancellationToken.None);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _webSocket?.Dispose();
        _cancellationTokenSource?.Dispose();
    }
}
```

#### **1.2 Enhanced Message Models with Data Handling**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Models/WebSocketMessages.cs`

```csharp
[MessagePackObject]
public class JobUpdateMessage
{
    [Key("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [Key("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [Key("job_id")]
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = "";

    [Key("message")]
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [Key("result")]
    [JsonPropertyName("result")]
    public Dictionary<string, object>? Result { get; set; }

    [Key("error")]
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [IgnoreMember]
    [JsonIgnore]
    public bool IsCompleted => Status == "completed";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsFailed => Status == "error";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsRunning => Status == "running";

    // Extract typed output data
    [IgnoreMember]
    [JsonIgnore]
    public T? GetOutputData<T>(string outputName)
    {
        if (Result?.TryGetValue(outputName, out var element) == true)
        {
            try
            {
                // Handle both MessagePack and JSON scenarios
                if (element is T directValue)
                    return directValue;

                // Try deserializing if it's a complex object
                var json = JsonSerializer.Serialize(element);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize output {outputName}: {ex.Message}");
            }
        }
        return default;
    }

    // Get all outputs as NodeTool data objects
    [IgnoreMember]
    [JsonIgnore]
    public Dictionary<string, NodeToolDataObject> GetAllOutputs()
    {
        var outputs = new Dictionary<string, NodeToolDataObject>();

        if (Result != null)
        {
            foreach (var kvp in Result)
            {
                try
                {
                    NodeToolDataObject? dataObj = null;

                    // Try direct cast first (MessagePack scenario)
                    if (kvp.Value is NodeToolDataObject direct)
                    {
                        dataObj = direct;
                    }
                    else
                    {
                        // Try JSON conversion (fallback scenario)
                        var json = JsonSerializer.Serialize(kvp.Value);
                        dataObj = JsonSerializer.Deserialize<NodeToolDataObject>(json);
                    }

                    if (dataObj != null)
                        outputs[kvp.Key] = dataObj;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse output {kvp.Key}: {ex.Message}");
                }
            }
        }

        return outputs;
    }
}

[MessagePackObject]
public class OutputUpdateMessage
{
    [Key("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [Key("node_id")]
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = "";

    [Key("node_name")]
    [JsonPropertyName("node_name")]
    public string NodeName { get; set; } = "";

    [Key("output_name")]
    [JsonPropertyName("output_name")]
    public string OutputName { get; set; } = "";

    [Key("value")]
    [JsonPropertyName("value")]
    public object? Value { get; set; }

    // Extract the data object from value
    [IgnoreMember]
    [JsonIgnore]
    public NodeToolDataObject? GetDataObject()
    {
        if (Value == null) return null;

        try
        {
            // Try direct cast first (MessagePack scenario)
            if (Value is NodeToolDataObject direct)
                return direct;

            // Try JSON conversion (fallback scenario)
            var json = JsonSerializer.Serialize(Value);
            return JsonSerializer.Deserialize<NodeToolDataObject>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse output data: {ex.Message}");
        }

        return null;
    }

    // Extract specific data type
    [IgnoreMember]
    [JsonIgnore]
    public T? GetTypedData<T>()
    {
        if (Value == null) return default;

        try
        {
            // Try direct cast first
            if (Value is T direct)
                return direct;

            // Try JSON conversion
            var json = JsonSerializer.Serialize(Value);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse typed data: {ex.Message}");
        }

        return default;
    }
}

[MessagePackObject]
public class NodeUpdateMessage
{
    [Key("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [Key("node_id")]
    [JsonPropertyName("node_id")]
    public string NodeId { get; set; } = "";

    [Key("node_name")]
    [JsonPropertyName("node_name")]
    public string NodeName { get; set; } = "";

    [Key("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [Key("error")]
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [Key("logs")]
    [JsonPropertyName("logs")]
    public List<string>? Logs { get; set; }

    [Key("result")]
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [Key("properties")]
    [JsonPropertyName("properties")]
    public object? Properties { get; set; }

    [IgnoreMember]
    [JsonIgnore]
    public bool IsCompleted => Status == "completed";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsFailed => Status == "error";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsRunning => Status == "running";
}

// Universal NodeTool data object format
[MessagePackObject]
public class NodeToolDataObject
{
    [Key("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [Key("uri")]
    [JsonPropertyName("uri")]
    public string? Uri { get; set; }

    [Key("asset_id")]
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    [Key("data")]
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    // Additional common properties
    [Key("width")]
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [Key("height")]
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [Key("duration")]
    [JsonPropertyName("duration")]
    public double? Duration { get; set; }

    [Key("format")]
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [Key("size")]
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [IgnoreMember]
    [JsonIgnore]
    public bool IsImage => Type == "image";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsAudio => Type == "audio";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsVideo => Type == "video";

    [IgnoreMember]
    [JsonIgnore]
    public bool IsText => Type == "text";

    // Check if actual data is embedded vs referenced
    [IgnoreMember]
    [JsonIgnore]
    public bool HasEmbeddedData => Data != null;

    [IgnoreMember]
    [JsonIgnore]
    public bool HasAssetReference => !string.IsNullOrEmpty(Uri) || !string.IsNullOrEmpty(AssetId);

    // Extract embedded data as specific type
    [IgnoreMember]
    [JsonIgnore]
    public T? GetEmbeddedData<T>()
    {
        if (!HasEmbeddedData) return default;

        try
        {
            // Try direct cast first (MessagePack scenario)
            if (Data is T direct)
                return direct;

            // Try JSON conversion (fallback scenario)
            var json = JsonSerializer.Serialize(Data);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to extract embedded data: {ex.Message}");
        }

        return default;
    }
}
```

### **Phase 2: Streaming Workflow Execution Service** _(2-3 days)_

**🎯 Goal**: High-level service that orchestrates WebSocket execution with real-time data collection

#### **2.1 WebSocket Workflow Execution Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/WebSocketWorkflowExecutionService.cs`

```csharp
public class WebSocketWorkflowExecutionService : IDisposable
{
    private readonly NodeToolWebSocketClient _client;
    private readonly ConcurrentDictionary<string, WorkflowExecutionSession> _activeSessions;
    private readonly ILogger _logger;

    public WebSocketWorkflowExecutionService(string baseUrl, ILogger? logger = null)
    {
        _client = new NodeToolWebSocketClient(baseUrl, logger);
        _activeSessions = new ConcurrentDictionary<string, WorkflowExecutionSession>();
        _logger = logger ?? new NullLogger();

        // Subscribe to all WebSocket events
        _client.JobUpdated += OnJobUpdated;
        _client.NodeUpdated += OnNodeUpdated;
        _client.OutputUpdated += OnOutputUpdated;
    }

    public async Task InitializeAsync()
    {
        await _client.ConnectAsync();
    }

    public async Task<WebSocketWorkflowResult> ExecuteAsync(
        string workflowId,
        Dictionary<string, object> parameters,
        IProgress<WorkflowExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var session = new WorkflowExecutionSession(workflowId, progress, _logger);

        var jobId = await _client.StartWorkflowAsync(workflowId, parameters);
        _activeSessions[jobId] = session;

        try
        {
            // Wait for completion with cancellation support
            return await session.WaitForCompletionAsync(cancellationToken);
        }
        finally
        {
            _activeSessions.TryRemove(jobId, out _);
        }
    }

    private void OnJobUpdated(object? sender, JobUpdateMessage message)
    {
        if (_activeSessions.TryGetValue(message.JobId, out var session))
        {
            session.HandleJobUpdate(message);
        }
    }

    private void OnNodeUpdated(object? sender, NodeUpdateMessage message)
    {
        // Forward to all active sessions (they'll filter by relevance)
        foreach (var session in _activeSessions.Values)
        {
            session.HandleNodeUpdate(message);
        }
    }

    private void OnOutputUpdated(object? sender, OutputUpdateMessage message)
    {
        // Forward to all active sessions
        foreach (var session in _activeSessions.Values)
        {
            session.HandleOutputUpdate(message);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

public class WorkflowExecutionSession
{
    private readonly TaskCompletionSource<WebSocketWorkflowResult> _completionSource;
    private readonly IProgress<WorkflowExecutionProgress>? _progress;
    private readonly ILogger _logger;

    private readonly Dictionary<string, NodeToolDataObject> _realtimeOutputs;
    private readonly Dictionary<string, NodeProgress> _nodeProgress;
    private readonly List<string> _executionLogs;

    public string WorkflowId { get; }

    public WorkflowExecutionSession(
        string workflowId,
        IProgress<WorkflowExecutionProgress>? progress,
        ILogger logger)
    {
        WorkflowId = workflowId;
        _completionSource = new TaskCompletionSource<WebSocketWorkflowResult>();
        _progress = progress;
        _logger = logger;

        _realtimeOutputs = new Dictionary<string, NodeToolDataObject>();
        _nodeProgress = new Dictionary<string, NodeProgress>();
        _executionLogs = new List<string>();
    }

    public Task<WebSocketWorkflowResult> WaitForCompletionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.Register(() => _completionSource.TrySetCanceled());
        return _completionSource.Task;
    }

    public void HandleJobUpdate(JobUpdateMessage message)
    {
        _logger.LogInformation($"Job {message.JobId}: {message.Status} - {message.Message}");

        if (message.IsCompleted)
        {
            // Merge final results with real-time outputs
            var finalOutputs = new Dictionary<string, NodeToolDataObject>(_realtimeOutputs);

            if (message.Result != null)
            {
                foreach (var kvp in message.GetAllOutputs())
                {
                    finalOutputs[kvp.Key] = kvp.Value; // Final results override real-time
                }
            }

            var result = new WebSocketWorkflowResult
            {
                Success = true,
                WorkflowId = WorkflowId,
                Outputs = finalOutputs,
                NodeProgress = _nodeProgress.Values.ToList(),
                ExecutionLogs = _executionLogs.ToList(),
                ExecutionMessage = message.Message
            };

            _completionSource.TrySetResult(result);
        }
        else if (message.IsFailed)
        {
            var result = new WebSocketWorkflowResult
            {
                Success = false,
                WorkflowId = WorkflowId,
                ErrorMessage = message.Error ?? "Workflow execution failed",
                NodeProgress = _nodeProgress.Values.ToList(),
                ExecutionLogs = _executionLogs.ToList()
            };

            _completionSource.TrySetResult(result);
        }

        // Report progress
        ReportProgress(message.Status, message.Message);
    }

    public void HandleNodeUpdate(NodeUpdateMessage message)
    {
        var nodeProgress = _nodeProgress.GetValueOrDefault(message.NodeId);
        if (nodeProgress == null)
        {
            nodeProgress = new NodeProgress
            {
                NodeId = message.NodeId,
                NodeName = message.NodeName
            };
            _nodeProgress[message.NodeId] = nodeProgress;
        }

        nodeProgress.Status = message.Status;
        nodeProgress.Error = message.Error;

        // Add new logs
        if (message.Logs?.Any() == true)
        {
            foreach (var log in message.Logs)
            {
                var logEntry = $"[{message.NodeName}] {log}";
                if (!_executionLogs.Contains(logEntry))
                {
                    _executionLogs.Add(logEntry);
                    nodeProgress.Logs.Add(log);
                }
            }
        }

        ReportProgress("running", $"Processing {message.NodeName}");
    }

    public void HandleOutputUpdate(OutputUpdateMessage message)
    {
        _logger.LogDebug($"Output update: {message.OutputName} from {message.NodeName}");

        var dataObject = message.GetDataObject();
        if (dataObject != null)
        {
            _realtimeOutputs[message.OutputName] = dataObject;

            var logEntry = $"Output '{message.OutputName}' updated: {dataObject.Type}";
            if (dataObject.HasAssetReference)
                logEntry += $" (Asset: {dataObject.AssetId})";
            if (dataObject.HasEmbeddedData)
                logEntry += " (With embedded data)";

            _executionLogs.Add(logEntry);
        }

        ReportProgress("running", $"Generated output: {message.OutputName}");
    }

    private void ReportProgress(string status, string? message)
    {
        var progress = new WorkflowExecutionProgress
        {
            Status = status,
            Message = message,
            CompletedNodes = _nodeProgress.Values.Count(n => n.Status == "completed"),
            TotalNodes = _nodeProgress.Values.Count,
            RealtimeOutputs = new Dictionary<string, NodeToolDataObject>(_realtimeOutputs),
            NodeProgress = _nodeProgress.Values.ToList(),
            ExecutionLogs = _executionLogs.ToList()
        };

        _progress?.Report(progress);
    }
}
```

### **Phase 3: Data Object Processing** _(1-2 days)_

**🎯 Goal**: Handle embedded data and asset references properly

#### **3.1 Data Object Processor**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/DataObjectProcessor.cs`

```csharp
public class DataObjectProcessor
{
    private readonly INodetoolClient _httpClient; // For asset downloads
    private readonly ILogger _logger;

    public DataObjectProcessor(INodetoolClient httpClient, ILogger? logger = null)
    {
        _httpClient = httpClient;
        _logger = logger ?? new NullLogger();
    }

    // Process NodeTool data object into platform-specific format
    public async Task<ProcessedDataObject> ProcessAsync(
        NodeToolDataObject dataObject,
        CancellationToken cancellationToken = default)
    {
        var processed = new ProcessedDataObject
        {
            Type = dataObject.Type,
            OriginalObject = dataObject
        };

        // Handle embedded data first (highest priority)
        if (dataObject.HasEmbeddedData)
        {
            processed.EmbeddedData = await ProcessEmbeddedData(dataObject);
            processed.IsEmbedded = true;
        }

        // Handle asset references
        if (dataObject.HasAssetReference)
        {
            processed.AssetReference = new AssetReference
            {
                Uri = dataObject.Uri ?? "",
                AssetId = dataObject.AssetId ?? "",
                Type = dataObject.Type,
                Size = dataObject.Size ?? 0,
                Metadata = ExtractMetadata(dataObject)
            };
        }

        return processed;
    }

    private async Task<object?> ProcessEmbeddedData(NodeToolDataObject dataObject)
    {
        if (!dataObject.HasEmbeddedData) return null;

        try
        {
            return dataObject.Type switch
            {
                "image" => await ProcessEmbeddedImage(dataObject),
                "audio" => ProcessEmbeddedAudio(dataObject),
                "video" => ProcessEmbeddedVideo(dataObject),
                "text" => ProcessEmbeddedText(dataObject),
                "json" => ProcessEmbeddedJson(dataObject),
                "tensor" => ProcessEmbeddedTensor(dataObject),
                _ => dataObject.GetEmbeddedData<object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process embedded {dataObject.Type} data");
            return null;
        }
    }

    private async Task<object?> ProcessEmbeddedImage(NodeToolDataObject dataObject)
    {
        // Handle different embedded image formats
        var imageData = dataObject.GetEmbeddedData<object>();

        if (imageData is string base64)
        {
            // Base64 encoded image
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return new ImageData { Bytes = bytes, Format = dataObject.Format ?? "png" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode base64 image");
                return null;
            }
        }

        if (imageData is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            // Structured image data
            return new ImageData
            {
                Width = dataObject.Width ?? 0,
                Height = dataObject.Height ?? 0,
                Format = dataObject.Format ?? "unknown",
                RawData = element
            };
        }

        return imageData;
    }

    private object? ProcessEmbeddedAudio(NodeToolDataObject dataObject)
    {
        var audioData = dataObject.GetEmbeddedData<object>();

        if (audioData is string base64)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return new AudioData
                {
                    Bytes = bytes,
                    Duration = dataObject.Duration ?? 0,
                    Format = dataObject.Format ?? "wav"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode base64 audio");
                return null;
            }
        }

        return audioData;
    }

    private object? ProcessEmbeddedText(NodeToolDataObject dataObject)
    {
        return dataObject.GetEmbeddedData<string>();
    }

    private object? ProcessEmbeddedJson(NodeToolDataObject dataObject)
    {
        return dataObject.GetEmbeddedData<Dictionary<string, object>>();
    }

    private object? ProcessEmbeddedTensor(NodeToolDataObject dataObject)
    {
        // Handle tensor data (arrays, matrices, etc.)
        return dataObject.GetEmbeddedData<float[]>();
    }

    private Dictionary<string, object> ExtractMetadata(NodeToolDataObject dataObject)
    {
        var metadata = new Dictionary<string, object>();

        if (dataObject.Width.HasValue) metadata["width"] = dataObject.Width.Value;
        if (dataObject.Height.HasValue) metadata["height"] = dataObject.Height.Value;
        if (dataObject.Duration.HasValue) metadata["duration"] = dataObject.Duration.Value;
        if (!string.IsNullOrEmpty(dataObject.Format)) metadata["format"] = dataObject.Format;
        if (dataObject.Size.HasValue) metadata["size"] = dataObject.Size.Value;

        return metadata;
    }
}

// Processed data object with both embedded and reference data
public class ProcessedDataObject
{
    public string Type { get; set; } = "";
    public NodeToolDataObject OriginalObject { get; set; } = null!;

    public bool IsEmbedded { get; set; }
    public object? EmbeddedData { get; set; }

    public AssetReference? AssetReference { get; set; }

    // Convenience accessors
    public T? GetEmbedded<T>() => EmbeddedData is T t ? t : default;
    public bool HasAsset => AssetReference != null;
}

// Specific data types
public class ImageData
{
    public byte[]? Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Format { get; set; } = "";
    public JsonElement? RawData { get; set; }
}

public class AudioData
{
    public byte[]? Bytes { get; set; }
    public double Duration { get; set; }
    public string Format { get; set; } = "";
}
```

## ⏱️ **Implementation Timeline**

- **Week 1**: WebSocket client + MessagePack/JSON message handling + data object processing
- **Week 2**: Streaming execution service + real-time progress tracking
- **Week 3**: VL integration + testing across different serialization formats

**Total Duration**: ~3 weeks for production-ready WebSocket execution with binary format support

## 🔧 **MessagePack Platform Support**

### **VL/vvvv** ✅

- **Native Support**: VL has MessagePack libraries available
- **Binary Performance**: Ideal for real-time visual programming
- **Memory Efficient**: Reduced GC pressure vs JSON

### **Unity** ✅

- **Package Available**: MessagePack-CSharp works with Unity 2020.3+
- **IL2CPP Compatible**: Works with AOT compilation
- **Mobile Ready**: Efficient for iOS/Android builds

### **General .NET** ✅

- **.NET Standard 2.0+**: Works with .NET Framework, .NET Core, .NET 5+
- **NuGet Package**: Easy installation via package manager
- **Performance**: 5-10x faster than JSON for binary data

### **Fallback Strategy** 🔄

```csharp
// Dual serialization support ensures compatibility
private T? DeserializeMessage<T>(byte[] messageBytes) where T : class
{
    // Try MessagePack first (binary, efficient)
    try { return MessagePackSerializer.Deserialize<T>(messageBytes); }
    catch (MessagePackSerializationException) { /* fallback to JSON */ }

    // JSON fallback (text, universal compatibility)
    try {
        var json = Encoding.UTF8.GetString(messageBytes);
        return JsonSerializer.Deserialize<T>(json);
    }
    catch (JsonException) { return null; }
}
```

This focused WebSocket approach handles the real NodeTool protocol with proper **binary MessagePack** support while maintaining **JSON fallback** compatibility!
