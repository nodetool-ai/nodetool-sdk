# NodeTool Single Node Execution Plan

## 🎯 **Current Status**

✅ **C# SDK Infrastructure** - Complete  
✅ **VL Node Generation** - Working  
✅ **Node Discovery & Categories** - Working  
✅ **Node Documentation in VL** - Working  
❌ **Single Node Execution Backend** - Missing API endpoint  
❌ **Real Node Execution in VL** - Currently mocked

## 📋 **Next Steps: Real Single Node Execution**

### **Phase 1: Backend API Implementation** _(3-5 days)_

**🎯 Goal**: Add single node execution endpoint to nodetool-core

#### **1.1 Create Single Node Execution Endpoint**

**File**: `nodetool-core/src/nodetool/api/node.py`

```python
@router.post("/api/nodes/{node_type}/execute")
async def execute_single_node(
    node_type: str,
    request: SingleNodeExecutionRequest,
    current_user = Depends(get_current_user_optional)
) -> SingleNodeExecutionResponse:
    """Execute a single node with provided inputs"""

    try:
        runner = SingleNodeRunner(
            workspace_dir=get_workspace_dir(current_user),
            device=request.device or "cpu"
        )

        result = await runner.execute_node(
            node_type=node_type,
            inputs=request.inputs
        )

        return SingleNodeExecutionResponse(
            success=True,
            outputs=result,
            execution_time=time.time() - start_time
        )

    except Exception as e:
        return SingleNodeExecutionResponse(
            success=False,
            error=str(e),
            error_type=type(e).__name__
        )
```

#### **1.2 Implement SingleNodeRunner**

**File**: `nodetool-core/src/nodetool/workflows/single_node_runner.py`

```python
class SingleNodeRunner:
    """Executes individual nodes without full workflow context"""

    def __init__(self, workspace_dir: str, device: str = "cpu"):
        self.workspace_dir = workspace_dir
        self.device = device

    async def execute_node(
        self,
        node_type: str,
        inputs: Dict[str, Any]
    ) -> Dict[str, Any]:
        """Execute a single node with given inputs"""

        # 1. Import and instantiate node class
        node_class = self._get_node_class(node_type)
        context = self._create_minimal_context()
        node = node_class(id="single_node", context=context)

        # 2. Set input values
        for key, value in inputs.items():
            setattr(node, key, value)

        # 3. Execute node
        await node.process(context)

        # 4. Extract outputs
        return self._extract_outputs(node)

    def _create_minimal_context(self) -> ProcessingContext:
        """Create lightweight context for single node execution"""
        return MinimalProcessingContext(
            workspace_dir=self.workspace_dir,
            device=self.device,
            temp_dir=tempfile.mkdtemp(),
            job_id=f"single_node_{uuid.uuid4()}"
        )
```

#### **1.3 Add Request/Response Models**

```python
class SingleNodeExecutionRequest(BaseModel):
    inputs: Dict[str, Any]
    device: Optional[str] = "cpu"
    timeout: Optional[int] = 30

class SingleNodeExecutionResponse(BaseModel):
    success: bool
    outputs: Optional[Dict[str, Any]] = None
    error: Optional[str] = None
    error_type: Optional[str] = None
    execution_time: Optional[float] = None
```

#### **1.4 Testing Strategy**

```python
# Test single node execution
async def test_execute_text_concatenate():
    runner = SingleNodeRunner("/tmp/test", "cpu")
    result = await runner.execute_node(
        "nodetool.text.Concatenate",
        {"a": "Hello", "b": "World"}
    )
    assert result["output"] == "HelloWorld"

# Test different node categories
test_categories = {
    "Text": ["nodetool.text.Concatenate", "nodetool.text.Split"],
    "Math": ["nodetool.math.Add", "nodetool.math.Multiply"],
    "Image": ["nodetool.image.Resize", "nodetool.image.Blur"],
}
```

### **Phase 2: VL Integration** _(2-3 days)_

**🎯 Goal**: Replace mock execution with real HTTP calls in VL nodes

#### **2.1 Update NodeBase Execution**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Nodes/NodeBase.cs`

```csharp
private async Task ExecuteNodeAsync()
{
    _isRunning = true;
    _lastError = "";

    try
    {
        // Collect input values (existing code)
        var inputData = GetInputValues();

        // ✅ REPLACE MOCK: Execute real node
        var client = new NodetoolClient();
        var outputs = await client.ExecuteNodeAsync(
            _nodeMetadata.NodeType,
            inputData
        );

        // ✅ REPLACE MOCK: Use real outputs
        _lastOutputs = outputs;

        _lastError = "";
    }
    catch (Exception ex)
    {
        _lastError = $"Execution error: {ex.Message}";
        _lastOutputs.Clear();
    }
    finally
    {
        _isRunning = false;
    }
}
```

#### **2.2 Add Error Handling**

```csharp
private async Task ExecuteNodeAsync()
{
    try
    {
        // ... execution code ...
    }
    catch (HttpRequestException ex)
    {
        _lastError = "Network error: Cannot connect to NodeTool server";
    }
    catch (TaskCanceledException ex)
    {
        _lastError = "Request timed out";
    }
    catch (JsonException ex)
    {
        _lastError = "Invalid response from server";
    }
    catch (Exception ex)
    {
        _lastError = $"Unexpected error: {ex.Message}";
    }
}
```

#### **2.3 Configuration Management**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Configuration/VLConfiguration.cs`

```csharp
public static class VLConfiguration
{
    public static NodetoolOptions GetDefaultOptions()
    {
        return new NodetoolOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("NODETOOL_API_URL")
                     ?? "http://localhost:8000",
            TimeoutSeconds = 30,
            EnableDetailedLogging = true
        };
    }
}
```

### **Phase 3: Testing & Validation** _(1-2 days)_

#### **3.1 Integration Testing**

```csharp
[Test]
public async Task TestRealNodeExecution()
{
    // Test with NodeTool server running
    var nodeBase = CreateTestNode("nodetool.text.Concatenate");
    SetInputValue(nodeBase, "a", "Hello");
    SetInputValue(nodeBase, "b", "World");

    await ExecuteNode(nodeBase);

    var output = GetOutputValue(nodeBase, "output");
    Assert.AreEqual("HelloWorld", output);
}

[Test]
public async Task TestErrorHandling()
{
    // Test with server offline
    var nodeBase = CreateTestNode("nodetool.text.Concatenate");
    await ExecuteNode(nodeBase);

    Assert.IsTrue(nodeBase.HasError);
    Assert.Contains("Network error", nodeBase.LastError);
}
```

#### **3.2 Performance Testing**

```csharp
[Test]
public async Task TestExecutionPerformance()
{
    var stopwatch = Stopwatch.StartNew();

    // Execute 10 nodes concurrently
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => ExecuteTestNode("nodetool.math.Add"))
        .ToArray();

    await Task.WhenAll(tasks);
    stopwatch.Stop();

    Assert.Less(stopwatch.ElapsedMilliseconds, 5000,
        "10 concurrent executions should complete in < 5 seconds");
}
```

## 🔍 **Technical Challenges**

NOTE: all listed challenges are guesses and should be evaluated.
Executing a single node is probably not as complicated as pictured here.
During workflow execution all nodes are already processed with messaging so that
nodes kind of act independently. Meaning all necessary inputs for a node will just be
provided by the host (in our first case vvvv).

### **Challenge 1: Node Context Dependencies**

- **Issue**: Some nodes require workflow context (workspace, previous nodes)
- **Solution**: Create minimal execution context with temp workspace
- **Complexity**: Medium - may need NodeTool core modifications

### **Challenge 2: Asset Handling**

- **Issue**: Nodes that expect asset inputs from previous nodes
- **Solution**: Support direct asset upload/conversion in single node execution
- **Complexity**: Medium - coordinate with type system improvements

### **Challenge 3: GPU Resource Management**

- **Issue**: GPU nodes need proper resource allocation
- **Solution**: Implement device selection and queuing in SingleNodeRunner
- **Complexity**: High - may need NodeTool infrastructure changes

## 📊 **Success Metrics**

### **Backend Implementation**:

- ✅ API endpoint `/api/nodes/{type}/execute` responds correctly
- ✅ 80%+ of NodeTool nodes execute successfully standalone
- ✅ Proper error handling for unsupported nodes
- ✅ Execution time < 10 seconds for simple nodes

### **VL Integration**:

- ✅ Replace all mock execution with real API calls
- ✅ Clear error messages displayed in VL nodes
- ✅ Network failures don't crash VL
- ✅ Concurrent node execution works correctly

### **Quality Assurance**:

- ✅ Comprehensive test coverage for both backend and VL
- ✅ Performance benchmarks meet targets
- ✅ Memory usage remains stable
- ✅ Production-ready error handling and logging

## ⏱️ **Timeline**

- **Week 1**: Backend API implementation and testing
- **Week 2**: VL integration and error handling
- **Week 3**: Testing, performance optimization, documentation

**Total Duration**: ~3 weeks for production-ready single node execution

## 🚀 **Implementation Priority**

1. **High Priority**: Basic text/math nodes execution
2. **Medium Priority**: Image/audio processing nodes
3. **Low Priority**: Complex workflow-dependent nodes
4. **Future**: GPU-intensive AI model nodes

This plan focuses specifically on single node execution and can proceed in parallel with workflow execution and type system improvements.
