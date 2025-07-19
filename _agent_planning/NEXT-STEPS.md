# 🎯 **Next Steps: C# SDK + VL Implementation Roadmap**

## **📋 Implementation Priority Order**

### **🥇 Phase 1: Foundation (Week 1)** ⭐ **SIMPLIFIED**

**Goal**: SDK type consumption system with WebSocket communication

#### **1.1 Type Registry System** ⭐ **START HERE**

- **Why First**: SDK needs to consume pre-generated types from nodetool-core
- **Scope**: Type loading and registration (not generation - that's in nodetool-core)
- **Output**:
  - `Nodetool.SDK/Types/` - Type registry and lookup system
  - `Nodetool.SDK/Generated/` - Reference to pre-generated types from nodetool-core
  - `Nodetool.SDK/WebSocket/` - MessagePack-compatible WebSocket client

**Implementation Steps**:

```
1. Create NodeTypeRegistry.cs (load all pre-generated types)
2. Build EnumRegistry.cs (catalog all generated enums)
3. Implement TypeLookupService.cs (runtime type resolution)
4. Integrate with WebSocket client for MessagePack serialization
5. Add basic validation and error handling
```

#### **1.2 WebSocket Client Foundation**

- **Why Second**: Need communication layer for everything else
- **Scope**: Basic WebSocket connection with MessagePack + JSON fallback
- **Output**: `Nodetool.SDK/WebSocket/NodetoolClient.cs`

**Implementation Steps**:

```
1. WebSocket connection management
2. MessagePack serialization/deserialization
3. JSON fallback handling
4. Basic message routing (job_update, node_update, output_update)
5. Connection error handling & reconnection
```

---

### **🥈 Phase 2: Core Execution (Week 2)**

**Goal**: Execute workflows and see real-time updates

#### **2.1 Workflow Execution** ⭐ **HIGHEST VALUE**

- **Why Before Single Nodes**: Workflows are the primary use case
- **Scope**: Full workflow execution with real-time progress
- **Output**: `IExecutionSession` interface with clean async API

**Implementation Steps**:

```
0. APIStatus class for convenient usage and debugging (will be a node in VL that outputs state)
1. ExecutionSession class (manages workflow state)
2. Real-time progress tracking (job_update messages)
3. Node-level progress updates (node_update messages)
4. Output handling (output_update messages)
5. Error handling and cancellation
```

**Target API**:

```csharp
var client = new NodetoolClient();
var session = await client.ExecuteWorkflowAsync(workflowId, inputs);

// Real-time updates
session.OnProgress += (progress) => Console.WriteLine($"Progress: {progress}%");
session.OnNodeComplete += (nodeId, outputs) => HandleNodeOutput(outputs);

var finalOutputs = await session.WaitForCompletionAsync();
```

#### **2.2 Single Node Execution**

- **Why After Workflows**: Simpler case, can reuse workflow infrastructure
- **Scope**: Execute individual nodes for testing/experimentation
- **Output**: `INodeExecutionSession` interface

---

### **🥉 Phase 3: VL Integration (Week 3)**

**Goal**: Working VL nodes that consume SDK

#### **3.1 VL SDK Wrapper** ⭐ **THIN LAYER ONLY**

- **Why Third**: Needs working SDK to wrap
- **Scope**: Convert SDK data objects to VL native types
- **Output**: Enhanced `Nodetool.SDK.VL` project

**VL Responsibilities** (ONLY):

```
✅ Type conversion (ImageRef → SKImage)
✅ VL pin creation (inputs/outputs)
✅ VL-specific UI (static / dynamic enums)
✅ Asset caching integration
❌ WebSocket handling (SDK does this)
❌ Workflow management (SDK does this)
❌ API calls (SDK does this)
```

#### **3.2 Dynamic Model Integration**

- **Why After Basic VL**: Needs VL wrapper foundation
- **Scope**: Dropdown population for HuggingFace, Comfy, etc.
- **Output**: VL dynamic enum system

**Implementation**:

```csharp
// VL calls SDK, SDK handles complexity
var models = await SDK.Models.GetHuggingFaceModelsAsync("text-generation");
var vlEnum = VLTypeConverter.CreateDynamicEnum(models);
```

---

### **🏅 Phase 4: Polish & Assets (Week 4)**

**Goal**: Production-ready experience

#### **4.1 Asset Download & Caching**

- **Scope**: Download images/audio/video from NodeTool server
- **Output**: `Nodetool.SDK/Assets/AssetManager.cs`
- **Integration**: VL nodes get local file paths for assets

#### **4.2 Error Handling & UX**

- **Scope**: Graceful error handling, connection recovery, user feedback
- **Output**: Robust production experience

---

## **🚀 Quick Win Strategy**

### **Week 1 Target**: Basic Demo

```csharp
// Generate basic types + WebSocket client
var client = new NodetoolClient();
await client.ConnectAsync();
Console.WriteLine("✅ Connected to NodeTool!");

// Send a simple workflow
var result = await client.ExecuteWorkflowAsync("simple-resize-workflow", inputs);
Console.WriteLine($"✅ Workflow completed: {result.OutputCount} outputs");
```

### **Week 2 Target**: Real-time Workflow

```csharp
// Real-time progress updates
var session = await client.ExecuteWorkflowAsync(workflowId, inputs);
session.OnProgress += progress => progressBar.Value = progress;
session.OnNodeComplete += (nodeId, outputs) => UpdateUI(nodeId, outputs);
```

### **Week 3 Target**: Working VL Patch

```
VL patch with NodeTool nodes:
[ImageInput] → [Resize(algorithm=Lanczos)] → [Blur(radius=2.5)] → [ImageOutput]
                    ↓ dropdown populated        ↓ dynamic slider
                from SDK enum              from SDK metadata
```

### **Week 4 Target**: Production Ready

```
✅ Error handling & recovery
✅ Asset download & caching
✅ Model dropdown population
✅ Documentation & examples
```

---

## **📊 Success Metrics**

| **Phase**   | **Success Criteria**                                      |
| ----------- | --------------------------------------------------------- |
| **Phase 1** | Generate C# types + connect to NodeTool WebSocket         |
| **Phase 2** | Execute workflows with real-time progress updates         |
| **Phase 3** | Working VL patch that calls NodeTool nodes                |
| **Phase 4** | VL users can build complex workflows with model selection |

---

## **🎯 Implementation Notes**

### **Start With**:

1. **Type generation** - Everything depends on this
2. **WebSocket client** - Core communication layer
3. **Simple workflow execution** - Highest value demo

### **Key Principles**:

- ✅ **SDK does the heavy lifting** (WebSocket, API calls, state management)
- ✅ **VL is a thin wrapper** (type conversion, UI concerns only)
- ✅ **Focus on transmitted data** (not all Python classes)
- ✅ **Real-time updates** (progress, node completion, outputs)
- ✅ **Clean async APIs** (no blocking VL UI thread)

### **Avoid Early**:

- ❌ Complex VL-specific features before basic execution works
- ❌ Full Python type generation (stick to WebSocket data only)
- ❌ Local node execution (focus on WebSocket communication first)
- ❌ Advanced tooling features (save for Phase 2 future consideration)

---

## **🔄 Iteration Strategy**

Each phase should produce a **working demo** that validates the approach:

- **Phase 1**: Console app that connects and sends/receives WebSocket messages
- **Phase 2**: Console app that executes workflows with real-time progress
- **Phase 3**: Simple VL patch with working NodeTool nodes
- **Phase 4**: Complex VL patch with model selection and asset handling

**Ship early, ship often!** 🚀
