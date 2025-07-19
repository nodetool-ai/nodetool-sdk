# NodeTool Single Node Execution Plan

## 🎯 **Current Status**

✅ **C# SDK Infrastructure** - Complete  
✅ **VL Node Generation** - Working  
✅ **Node Discovery & Categories** - Working  
✅ **Node Documentation in VL** - Working  
❌ **Single Node WebSocket Backend** - To be implemented in nodetool-core
❌ **Single Node WebSocket SDK** - Needs implementation
❌ **Real Node Execution in VL** - Currently mocked

## 🔍 **Execution Approach: WebSocket-Only**

**Key Insight**: Single node execution will **also use WebSocket streaming** just like workflows. Each node execution will:

- Send WebSocket messages with node type and inputs
- Receive real-time `node_update` messages during processing
- Receive `output_update` messages with intermediate/final results
- Get final completion message when done

**Backend**: To be implemented in **nodetool-core** later using WebSocket protocol

## 📋 **Implementation Plan: WebSocket Single Node Execution**

### **Phase 1: WebSocket Message Format** _(Planning)_

**🎯 Goal**: Define WebSocket protocol for single node execution

#### **1.1 Single Node Execution Message**

```json
{
  "type": "run_job",
  "command": "run_node",
  "data": {
    "type": "run_node_request",
    "api_url": "http://localhost:8000",
    "user_id": "1",
    "node_type": "nodetool.text.Concatenate",
    "auth_token": "local_token",
    "inputs": {
      "a": "Hello",
      "b": "World"
    },
    "job_id": "single_node_abc123"
  }
}
```

#### **1.2 Response Messages**

**Same format as workflows**:

- `job_update`: Overall execution status (running → completed)
- `node_update`: Real-time node processing updates
- `output_update`: Final outputs with data objects

### **Phase 2: C# SDK WebSocket Implementation** _(2-3 days)_

**🎯 Goal**: Implement single node execution using existing WebSocket client

#### **2.1 Single Node WebSocket Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/SingleNodeExecutionService.cs`

```csharp
public class SingleNodeExecutionService
{
    private readonly NodeToolWebSocketClient _client;
    private readonly ILogger _logger;

    public SingleNodeExecutionService(NodeToolWebSocketClient client, ILogger? logger = null)
    {
        _client = client;
        _logger = logger ?? new NullLogger();
    }

    public async Task<SingleNodeExecutionResult> ExecuteNodeAsync(
        string nodeType,
        Dictionary<string, object> inputs,
        IProgress<NodeExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid().ToString("N")[..24];

        // Send single node execution message
        await _client.StartSingleNodeAsync(nodeType, inputs, jobId);

        // Wait for completion using same session logic as workflows
        var session = new SingleNodeExecutionSession(nodeType, progress, _logger);
        return await session.WaitForCompletionAsync(cancellationToken);
    }
}

public class SingleNodeExecutionResult
{
    public bool Success { get; set; }
    public string NodeType { get; set; } = "";
    public Dictionary<string, NodeToolDataObject> Outputs { get; set; } = new();
    public List<string> ExecutionLogs { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
```

#### **2.2 Update WebSocket Client for Single Nodes**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/NodeToolWebSocketClient.cs`

```csharp
public async Task<string> StartSingleNodeAsync(
    string nodeType,
    Dictionary<string, object> inputs,
    string? jobId = null,
    string userId = "1",
    string authToken = "local_token")
{
    jobId ??= Guid.NewGuid().ToString("N")[..24];

    var message = new
    {
        type = "run_job",
        command = "run_node",
        data = new
        {
            type = "run_node_request",
            api_url = _wsUrl.Replace("/predict", "").Replace("ws://", "http://"),
            user_id = userId,
            node_type = nodeType,
            auth_token = authToken,
            inputs = inputs,
            job_id = jobId
        }
    };

    await SendMessageAsync(message);

    _logger.LogInformation($"Started single node execution: {nodeType} → Job: {jobId}");
    return jobId;
}
```

### **Phase 3: VL Integration** _(2-3 days)_

**🎯 Goal**: Update VL nodes to use real WebSocket single node execution

#### **3.1 Update NodeBase Implementation**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Nodes/NodeBase.cs`

```csharp
private async Task ExecuteNodeAsync()
{
 _isRunning = true;
 _lastError = "";

 try
 {
     // Collect input values
     var inputData = GetInputValues();

     // Execute single node via WebSocket
     var executionService = VLServiceLocator.GetService<SingleNodeExecutionService>();

     var progress = new Progress<NodeExecutionProgress>(OnExecutionProgress);
     var result = await executionService.ExecuteNodeAsync(
         _nodeMetadata.NodeType,
         inputData,
         progress);

     // Process results
     if (result.Success)
     {
         await SetOutputsFromResult(result);
            }
            else
            {
         _lastError = result.ErrorMessage ?? "Execution failed";
     }
        }
        catch (Exception ex)
        {
     _lastError = FormatUserFriendlyError(ex);
 }
 finally
 {
     _isRunning = false;
 }
}

private void OnExecutionProgress(NodeExecutionProgress progress)
{
 // Update VL node progress indicators
 _executionStatus = progress.Status;
 _executionMessage = progress.Message ?? "";
}
```

#### **3.2 Service Registration**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Services/VLServiceLocator.cs`

```csharp
public static async Task InitializeAsync()
{
 // Initialize WebSocket client (shared with workflows)
 var wsClient = new NodeToolWebSocketClient("http://localhost:8000");
 await wsClient.ConnectAsync();

 // Register single node execution service
 var singleNodeService = new SingleNodeExecutionService(wsClient);
 RegisterService(singleNodeService);

 // Register other services...
 RegisterService(new WebSocketWorkflowExecutionService(wsClient));
}
```

## ⏱️ **Implementation Timeline**

- **Week 1**: C# SDK WebSocket implementation for single nodes
- **Week 2**: VL integration updates + testing
- **Week 3**: Testing + optimization
- **Later**: Backend implementation in nodetool-core (WebSocket protocol)

**Total Duration**: ~3 weeks for C# SDK, backend implementation TBD

## 🎯 **Implementation Strategy**

### **WebSocket-Only Approach** _(Consistent & Future-Proof)_

- **Pros**: Consistent with workflows, real-time progress, unified protocol
- **Cons**: Requires backend implementation in nodetool-core
- **Benefits**: Same data object handling, streaming updates, error handling

### **Message Protocol Reuse**

```csharp
// Same WebSocket client handles both workflows and single nodes
_client.JobUpdated += OnJobUpdated;        // Same handler
_client.NodeUpdated += OnNodeUpdated;      // Same handler
_client.OutputUpdated += OnOutputUpdated;  // Same handler

// Different message types trigger different execution modes
await _client.StartWorkflowAsync(workflowId, params);  // Workflow
await _client.StartSingleNodeAsync(nodeType, inputs); // Single node
```

## 📊 **Success Metrics**

### **C# SDK Implementation**:

- ✅ Single nodes execute via WebSocket (same as workflows)
- ✅ Real-time progress updates during node processing
- ✅ Proper data object handling (embedded data + assets)
- ✅ Consistent error handling with workflow execution
- ✅ Unified WebSocket client handles both workflows and nodes

### **VL Integration**:

- ✅ Replace all mock execution with real WebSocket calls
- ✅ Real-time progress indicators in VL nodes
- ✅ Network failures don't crash VL
- ✅ Same user experience as workflow nodes

## 🔄 **Integration with Other Plans**

- **Reuses**: Entire WebSocket infrastructure from `workflow-execution-plan.md`
- **Shares**: Data object processing from `csharp-types-plan.md`
- **Extends**: Same MessagePack/JSON protocol for single nodes
- **Backend**: Will be implemented in nodetool-core using WebSocket protocol

This approach provides **complete consistency** between workflow and single node execution while reusing all existing WebSocket infrastructure.
