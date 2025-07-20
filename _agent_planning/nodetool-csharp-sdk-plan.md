# 🎯 **Nodetool C# SDK - Universal .NET Integration**

> **⚠️ UPDATE**: This plan remains valid, but execution will use **WebSocket streaming** instead of HTTP as originally assumed. See `workflow-execution-plan.md` for WebSocket implementation details.

## **🌟 Vision: From VL-Specific to Universal C# SDK**

Instead of building VL-specific type mappings, create a **comprehensive C# SDK** that can be used by:

- 🔧 **VL/vvvv** (our current focus)
- 🎮 **Unity** (game dev + AI workflows)
- 🏢 **WPF/WinUI** (desktop apps)
- 🌐 **Blazor** (web apps)
- 📱 **MAUI** (mobile apps)
- ⚡ **Console Apps** (automation)

---

## **📋 Current Nodetool Type System Analysis**

### **🧬 Core Type Architecture** _(from source analysis)_:

```python
# 1. TypeMetadata - Universal type representation
class TypeMetadata(BaseModel):
    type: str                     # "str", "int", "list", "union", etc.
    optional: bool = False        # Optional[T]
    values: list[str|int] = None  # For enums
    type_args: list[TypeMetadata] # For generics: List[T], Dict[K,V]
    type_name: str = None         # For custom types

# 2. BaseType - All Nodetool types inherit from this
class BaseType(BaseModel):
    type: str  # Automatic registration in NameToType dict

# 3. Type Categories
- Primitives: str, int, float, bool, bytes
- Collections: list, dict, tuple, union
- Assets: ImageRef, AudioRef, VideoRef, DocumentRef, etc.
- Comfy: ComfyModel, ComfyData (ComfyUI integration)
- AI Models: HuggingFaceModel, LoraWeight
- Special: Date, Datetime, Event, NPArray
```

### **🎯 Type Generation Strategy**:

```csharp
// Generate from TypeMetadata -> C# types
TypeMetadata { type: "list", type_args: [{ type: "str" }] }
=> List<string>

TypeMetadata { type: "union", type_args: [{ type: "str" }, { type: "int" }] }
=> object // or Union<string, int> with custom union type

TypeMetadata { type: "image", optional: true }
=> ImageRef?
```

---

## **🏗️ Proposed C# SDK Architecture**

### **📦 Package Structure**:

```
Nodetool.SDK/                    # 🎯 Core SDK (universal)
├── Types/
│   ├── TypeMetadata.cs         # C# version of Python TypeMetadata
│   ├── BaseType.cs             # C# base class with type registration
│   ├── Assets/                 # ImageRef, AudioRef, VideoRef, etc.
│   ├── Collections/            # List, Dict, Union handling
│   ├── AI/                     # HuggingFaceModel, ComfyModel, etc.
│   └── Generated/              # 🤖 Auto-generated from Python
├── Api/
│   ├── INodetoolClient.cs      # HTTP client interface
│   ├── NodetoolClient.cs       # Main API client
│   ├── Endpoints/              # Typed endpoint wrappers
│   └── Models/                 # Request/Response DTOs
├── Serialization/
│   ├── NodetoolJsonConverter.cs # Custom JSON handling
│   └── TypeSerializer.cs       # Type<->JSON conversion
└── Utilities/
    ├── TypeMapper.cs           # TypeMetadata -> C# Type
    └── SchemaValidator.cs      # Runtime type validation

Nodetool.SDK.VL/                # 🔧 VL-specific extensions
├── Factories/
│   ├── NodeFactory.cs          # IVLNodeDescriptionFactory impl
│   ├── WorkflowFactory.cs      # Workflow -> VL nodes
├── Nodes/
│   ├── NodetoolNodeBase.cs     # VL node base class
│   ├── WorkflowNodeBase.cs     # VL workflow executor
└── TypeSystem/
    └── VLTypeMapper.cs         # SDK types -> VL pins

Nodetool.SDK.Unity/             # 🎮 Unity-specific extensions
├── Components/
│   ├── NodetoolComponent.cs    # MonoBehaviour wrapper
│   ├── WorkflowRunner.cs       # Unity coroutine execution
└── Editor/
    └── NodetoolWindow.cs       # Custom Unity window

Nodetool.SDK.Console/           # ⚡ Console/automation tools
└── Program.cs                  # CLI for testing/automation
```

---

## **🤖 MessagePack Type Generation in nodetool-core** ⭐ **UPDATED APPROACH**

### **🔄 nodetool-core Type Generation**:

```python
# In nodetool-core repository
# tools/generate_csharp_messagepack_types.py
def generate_csharp_types():
    """Generate ALL C# types using MessagePack serialization"""

    # 1. Scan ALL Python node classes (brute force approach)
    all_node_types = get_all_node_classes()

    # 2. Generate C# classes with MessagePack attributes
    for node_type in all_node_types:
        generate_messagepack_class(node_type)

    # 3. Generate ALL Python enums as C# enums
    all_enums = get_all_python_enums()
    for enum_type in all_enums:
        generate_csharp_enum(enum_type)

    # 4. Generate type registry for runtime lookup
    generate_complete_type_registry(all_node_types, all_enums)

    # 5. Package as distributable library
    package_for_distribution()
```

### **🎯 Generated C# Example**:

```csharp
// Generated from Python ImageRef (by nodetool-core)
[MessagePackObject]
public class ImageRef : BaseDataObject
{
    [Key(0)]
    public string Uri { get; set; } = "";

    [Key(1)]
    public string? AssetId { get; set; }

    [Key(2)]
    public int Width { get; set; }

    [Key(3)]
    public int Height { get; set; }

    [Key(4)]
    public Dictionary<string, object>? Data { get; set; }
}

// Generated node class (by nodetool-core)
[MessagePackObject]
public class ResizeNode : BaseNode
{
    [Key(0)]
    public ImageRef Image { get; set; } = new();

    [Key(1)]
    public int Width { get; set; } = 512;

    [Key(2)]
    public int Height { get; set; } = 512;

    [Key(3)]
    public ResizeAlgorithm Algorithm { get; set; } = ResizeAlgorithm.Lanczos;

    [Key(4)]
    public ImageRef Output { get; set; } = new();
}

// Generated enum (by nodetool-core)
public enum ResizeAlgorithm
{
    Nearest,
    Linear,
    Cubic,
    Lanczos
}

// SDK type registry (consumes generated types)
public static class NodeTypeRegistry
{
    public static void LoadGeneratedTypes()
    {
        // All types pre-generated by nodetool-core - just register them
        RegisterAllGeneratedTypes();
    }
}
```

---

## **🔧 VL Integration Strategy**

### **✅ Keep Current VL Code, Add SDK Layer**:

```csharp
// Current: VL.NodetoolNodes/TypeSystem/NodetoolTypeMapper.cs
public class NodetoolTypeMapper
{
    // ✅ Keep existing logic, but delegate to SDK
    public static Type MapToVLType(TypeMetadata typeMetadata)
    {
        // Use Nodetool.SDK.TypeMapper for core mapping
        var sdkType = Nodetool.SDK.TypeMapper.MapToType(typeMetadata);

        // Apply VL-specific transformations
        return ApplyVLTransformations(sdkType, typeMetadata);
    }
}

// Current: VL.NodetoolNodes/Core/NodetoolNodeBase.cs
public class NodetoolNodeBase : IVLNode
{
    // ✅ Keep existing VL node logic
    // ✅ Use Nodetool.SDK.Api.NodetoolClient for HTTP calls
    private readonly INodetoolClient _client;

    protected override async Task ExecuteAsync()
    {
        // Use SDK client instead of custom HTTP logic
        var result = await _client.ExecuteNodeAsync(nodeType, inputs);
        UpdateOutputPins(result);
    }
}
```

---

## **⚡ Implementation Phases**

### **Phase 1: Simplified SDK Foundation** _(~1-2 days)_ ⭐ **MUCH FASTER**

1. **🏗️ Create Nodetool.SDK project**
2. **📦 Integrate pre-generated types from nodetool-core**
3. **📡 Implement NodetoolClient with MessagePack support**
4. **🧪 Add basic type registry and lookup system**

### **Phase 2: VL Integration** _(~1-2 days)_ ⭐ **SIMPLIFIED**

1. **🔄 Update VL code to use pre-generated types**
2. **✅ Keep all current functionality working**
3. **📚 Enum properties automatically become dropdowns**
4. **🧪 Test with real generated types**

### **Phase 3: Unity Integration** _(~2-3 days)_

1. **🎮 Create Nodetool.SDK.Unity package**
2. **🧩 Build MonoBehaviour components**
3. **🎨 Create Unity Editor integration**
4. **🎯 Test with Unity workflows**

### **Phase 4: Additional Platforms** _(ongoing)_

1. **📱 MAUI/mobile integration**
2. **🌐 Blazor web components**
3. **🖥️ WPF/WinUI desktop apps**
4. **⚡ Console automation tools**

---

## **🎯 Benefits of This Approach**

### **🔧 For VL Users**:

- ✅ **All current functionality preserved**
- ✅ **Better type safety and performance**
- ✅ **Consistent API patterns**
- ✅ **Automatic updates when Nodetool adds new types**

### **🎮 For Unity Developers**:

- ✅ **Native C# integration**
- ✅ **Drag-and-drop Nodetool components**
- ✅ **Editor integration for workflows**
- ✅ **Coroutine-based async execution**

### **🏢 For .NET Ecosystem**:

- ✅ **Reusable across all .NET platforms**
- ✅ **NuGet packages for easy distribution**
- ✅ **Strong typing throughout**
- ✅ **Comprehensive documentation**

### **🤝 For Nodetool Project**:

- ✅ **Broader .NET ecosystem adoption**
- ✅ **Professional enterprise integration**
- ✅ **Community contributions to SDK**
- ✅ **Reference implementation for other languages**

---

## **🚀 Next Steps**

**Ready to proceed with Phase 1?**

1. **🤖 Generate initial C# types from Python codebase**
2. **📡 Create unified NodetoolClient API**
3. **🔄 Refactor VL code to use SDK (maintaining compatibility)**
4. **🧪 Ensure all current functionality works**

This approach gives us a **solid foundation** for VL while opening up **massive opportunities** for Unity, enterprise apps, and the broader .NET ecosystem! 🎯

**Timeline: 3-5 days** for a production-ready SDK that transforms VL integration and unlocks Unity/enterprise potential.

## **Versioning & Build Safety** ⭐ **NEW REQUIREMENTS**

- **SDK ↔ Core SemVer lock**: The Nodetool.SDK NuGet package must share the same `MAJOR.MINOR` as the NodeTool core server API. A breaking API change in core increments the major version for **both** repositories.
- **CI drift gate**: The enum/type-sync workflow now includes a drift-check step that fails the pipeline when generated code is stale, preventing merges that would break runtime compatibility.

---
