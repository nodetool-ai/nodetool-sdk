# 🎯 NodeTool C# SDK - Final Planning Summary

## 📋 **Current Planning Status** ✅

All planning files have been reviewed, updated, and aligned around the **WebSocket streaming execution model** discovered from real NodeTool execution examples.

## 🔑 **Key Architectural Insights**

### **1. Universal WebSocket Execution**

- **All execution** (workflows AND single nodes) uses WebSocket streaming
- **Real-time progress** with `job_update`, `node_update`, `output_update` messages
- **Binary format**: MessagePack primary, JSON fallback for compatibility

### **2. Real Data Object Format**

```json
{
  "type": "image",
  "uri": "http://...",
  "asset_id": "...",
  "data": {
    /* embedded content */
  },
  "width": 1024,
  "height": 768
}
```

- **Dual nature**: Embedded data + asset references
- **Priority**: Embedded data takes precedence over asset downloads
- **Rich metadata**: Type-specific properties (width, height, duration, etc.)

### **3. SDK-First Architecture**

- **Universal C# SDK**: Works with VL, Unity, .NET applications
- **VL as consumer**: Thin wrapper around robust SDK core
- **Platform agnostic**: MessagePack works across all C# platforms

### **4. Clean SDK-First Architecture** ⭐ **NEW**

**Nodetool.SDK Responsibility** _(Universal C# Foundation)_:

- ✅ **Generate ALL C# types** from Python (static enums + model patterns)
- ✅ **Handle ALL WebSocket** connections and message parsing
- ✅ **Manage ALL model API calls** (ComfyUI, HuggingFace, Ollama, etc.)
- ✅ **Cache model lists** and handle rate limiting/errors
- ✅ **Provide clean interfaces**: `IExecutionSession.GetOutput<T>()`
- ✅ **Handle complex objects** (InferenceProviderModel, ComfyModel, etc.)
- ✅ **Work across ALL .NET platforms** (VL, Unity, WPF, Console, etc.)

**Nodetool.SDK.VL Responsibility** _(Thin VL-Specific Layer)_:

- ✅ **Transform SDK data → VL types ONLY**: `NodeToolDataObject` → `SKImage`
- ✅ **Create VL pins** with appropriate types from SDK metadata
- ✅ **Handle VL-specific UI** (dynamic dropdowns, custom selectors)
- ❌ **NO WebSocket handling** - SDK does this
- ❌ **NO API calls** - SDK does this
- ❌ **NO type generation** - SDK does this
- ❌ **NO business logic** - SDK does this

```csharp
// SDK provides clean execution interface
public interface IExecutionSession
{
    bool IsRunning { get; }
    string? ErrorMessage { get; }
    T? GetOutput<T>(string outputName);  // SDK handles all complexity
    double ProgressPercent { get; }
}

// VL just consumes session state
public void Update()
{
    _isRunningPin.Value = _session.IsRunning;
    _imagePin.Value = _converter.ToSKImage(_session.GetOutput<NodeToolDataObject>("image"));
}
```

## 📚 **Planning File Overview**

| **File**                      | **Status**       | **Purpose**                               |
| ----------------------------- | ---------------- | ----------------------------------------- |
| `workflow-execution-plan.md`  | ✅ **READY**     | WebSocket streaming workflow execution    |
| `node-execution-plan.md`      | ✅ **READY**     | WebSocket single node execution           |
| `csharp-types-plan.md`        | ✅ **READY**     | Real data object handling & VL conversion |
| `nodetool-csharp-sdk-plan.md` | ✅ **CURRENT**   | Universal SDK architecture vision         |
| `plan-nodes.md`               | ✅ **COMPLETED** | Node discovery & VL integration           |
| `vl-writing-nodes.md`         | ✅ **REFERENCE** | VL integration patterns & constraints     |

## 🚀 **Implementation Roadmap**

### **Phase 0: OpenAPI + MessagePack Setup in nodetool-core** _(External Dependency)_ ⭐ **INDUSTRY STANDARD**

**Key Insight**: **nodetool-core** exposes MessagePack-enabled OpenAPI, **NSwag auto-generates** complete client + types!

1. **MessagePack.WebApi.Client Integration** - Add MessagePack formatters and schema processors
2. **OpenAPI Schema Enhancement** - Inject MessagePack attributes via schema processor
3. **NSwag Configuration** - Configure MessagePack client generation templates
4. **CI/CD Integration** - Auto-regenerate client on API changes

### **Phase 1: NSwag Client Integration** _(1 week)_ ⭐ **AUTOMATED**

1. **NSwag.MSBuild Setup** - Auto-generate MessagePack client on build
2. **SDK Service Wrapper** - Clean abstraction over generated client
3. **WebSocket Integration** - MessagePack-compatible WebSocket client
4. **Dynamic Model Enums** - Runtime model selection for VL

### **Phase 2: Ultra-Simple VL Integration** _(1 week)_ ⭐ **MINIMAL COMPLEXITY**

1. **VL Type Converter** - Simple transformation from generated types to VL types
2. **Direct Type Usage** - VL nodes use pre-generated types directly
3. **Asset Download Service** - Handle asset references when needed

### **Phase 3: Backend Implementation** _(TBD)_

- **Single Node Execution** - WebSocket protocol in nodetool-core
- **Message Protocol** - `run_node` command alongside `run_job`

### **Phase 4: Platform Expansion** _(Future)_

- **Unity Integration** - Game development + AI workflows
- **Enterprise Applications** - .NET desktop/web applications
- **Mobile Platforms** - Xamarin/MAUI applications

## ⚡ **Technical Highlights**

### **MessagePack Type Generation** ⭐ **COMPLETE COVERAGE**

```csharp
// nodetool-core generates ALL types using MessagePack
// Python: class ResizeNode(Node):
//   image: Image = InputProperty()
//   width: int = InputProperty(default=512)

// Generated C# (by nodetool-core):
[MessagePackObject]
public class ResizeNode
{
    [Key(0)]
    public ImageRef Image { get; set; } = new();
    [Key(1)]
    public int Width { get; set; } = 512;
    [Key(2)]
    public ImageRef Output { get; set; } = new();
}

// SDK simply consumes these pre-generated types
var nodeType = NodeTypeRegistry.GetType<ResizeNode>();
```

### **Clean SDK Interface**

```csharp
// SDK provides ultra-simple execution interface WITH full type handling
var session = await client.ExecuteWorkflowAsync(workflowId, inputs);

// Consumers get strongly-typed data (all complexity hidden)
var isRunning = session.IsRunning;                                    // SDK handles WebSocket
var image = session.GetOutput<NodeToolDataObject>("image_output");    // SDK handles parsing
var comfyModel = session.GetOutput<ComfyModelReference>("model");     // SDK handles model APIs
var error = session.ErrorMessage;                                     // SDK handles errors
```

### **Simplified VL Integration**

```csharp
// VL nodes become ULTRA-simple with SDK-first approach
public void Update()
{
    // Status pins - direct from SDK (no complexity)
    _isRunningPin.Value = _session.IsRunning;                    // SDK handles WebSocket events
    _progressPin.Value = _session.ProgressPercent;               // SDK handles progress parsing
    _errorPin.Value = _session.ErrorMessage ?? "";              // SDK handles error management

    // Data pins - VL's ONLY job: SDK data → VL types
    var imageData = _session.GetOutput<NodeToolDataObject>("image");     // SDK gives typed data
    var modelData = _session.GetOutput<ComfyModelReference>("model");    // SDK handles model APIs

    _imagePin.Value = _vlConverter.ToSKImage(imageData);         // VL: just type conversion
    _modelPin.Value = modelData?.Name ?? "";                     // VL: just UI display

    // No WebSocket, no API calls, no parsing, no state management!
}
```

### **WebSocket Client** _(Internal SDK Complexity)_

```csharp
// Dual serialization support (hidden from consumers)
private T? DeserializeMessage<T>(byte[] messageBytes)
{
    try { return MessagePackSerializer.Deserialize<T>(messageBytes); }
    catch { return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(messageBytes)); }
}
```

## 🎯 **Success Criteria**

### **Immediate Goals**

- ✅ Nodetool workflows execute in vvvv via WebSocket (not mocked)
- ✅ Nodetool nodes execute in vvvv via WebSocket (not mocked)
- ✅ Real-time progress indicators in VL
- ✅ Output pins update their output whenever new data arrives for that item
- ✅ Proper data object handling (embedded + assets)
- ✅ MessagePack binary format support
- ✅ Consistent error handling and user experience

### **Future Goals**

- ✅ Universal C# SDK for all .NET platforms
- ✅ Unity integration with NodeTool workflows
- ✅ Enterprise-ready SDK with comprehensive documentation
- ✅ Community adoption across .NET ecosystem

### **Versioning & Build Safety** ⭐ **NEW REQUIREMENTS**

- **SemVer alignment**: All auto-generated C# types/enums must match the **major.minor** version of the NodeTool core API. The Nodetool.SDK NuGet package therefore shares the same `MAJOR.MINOR` to signal compatibility.
- **CI drift gate**: The daily enum/type sync workflow now contains a drift-check step that **fails the build** whenever generated code is out-of-date. Contributors must regenerate (or accept the bot PR) before the pipeline turns green.

## 📊 **Platform Compatibility**

| **Platform**     | **MessagePack**   | **WebSocket** | **Status** |
| ---------------- | ----------------- | ------------- | ---------- |
| **VL/vvvv**      | ✅ Native support | ✅ Built-in   | Ready      |
| **Unity**        | ✅ Unity 2020.3+  | ✅ Native     | Ready      |
| **.NET Core/5+** | ✅ NuGet package  | ✅ Native     | Ready      |
| **Xamarin/MAUI** | ✅ Compatible     | ✅ Native     | Future     |

## 🔄 **Next Steps**

1. **Start Implementation**: Begin with WebSocket client and MessagePack handling - add logging early
2. **Add a convenience class for status updates in the SDK. Use this in vvvv as a node, currently called WorkflowAPIStatus.**
3. **VL Integration**: Update existing nodes to use real WebSocket execution
4. **Testing Strategy**: Validate against real NodeTool server
5. **Documentation**: Create comprehensive SDK documentation

---

## 💡 **Key Takeaways**

- **WebSocket-only execution** provides consistency and real-time experience
- **MessagePack binary format** enables efficient streaming for VL and Unity
- **Real data object handling** supports both embedded content and asset references
- **SDK-first approach** enables universal .NET platform support
- **VL as thin wrapper** around robust SDK core maximizes reusability
- **Smart type system** handles both static enums AND complex model patterns ⭐ **NEW**
- **WebSocket-focused generation** - types for transmitted data, not all Python classes ⭐ **KEY INSIGHT**
- **~50 transmitted types** vs. 1000+ Python classes - much more focused approach
- **Model pattern analysis** reveals ~50 static enums + ~20 model type patterns requiring API integration
- **Appropriate UI per type** instead of one-size-fits-all enum approach

This comprehensive plan provides a **production-ready NodeTool C# SDK** that works across the entire .NET ecosystem while maintaining excellent integration with VL/vvvv! 🎉
