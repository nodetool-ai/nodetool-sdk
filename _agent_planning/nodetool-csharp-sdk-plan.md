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

## **🤖 Automated Type Generation Pipeline**

### **🔄 Python -> C# Type Generation**:

```python
# tools/generate_csharp_types.py
def generate_csharp_sdk():
    """Generate C# types from Python Nodetool types"""

    # 1. Scan all Python BaseType subclasses
    all_types = get_all_nodetool_types()

    # 2. Extract TypeMetadata for each
    type_metadata = [get_type_metadata(t) for t in all_types]

    # 3. Generate C# classes
    for metadata in type_metadata:
        generate_csharp_class(metadata)

    # 4. Generate type registry
    generate_type_registry(all_types)

    # 5. Generate JSON converters
    generate_json_converters(all_types)
```

### **🎯 Generated C# Example**:

```csharp
// Generated from Python ImageRef
[JsonConverter(typeof(NodetoolTypeConverter))]
public class ImageRef : BaseType
{
    public override string Type => "image";
    public string Uri { get; set; } = "";
    public string? AssetId { get; set; }

    // Auto-generated from Python type annotations
    public static TypeMetadata GetTypeMetadata() => new()
    {
        Type = "image",
        Optional = false,
        TypeArgs = new List<TypeMetadata>()
    };
}

// Generated type registry
public static class NodetoolTypes
{
    private static readonly Dictionary<string, Type> NameToType = new()
    {
        ["image"] = typeof(ImageRef),
        ["audio"] = typeof(AudioRef),
        ["str"] = typeof(string),
        // ... auto-generated from Python NameToType
    };

    public static Type? GetType(string typeName) =>
        NameToType.TryGetValue(typeName, out var type) ? type : null;
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

### **Phase 1: Core SDK Foundation** _(~3-5 days)_

1. **🏗️ Create Nodetool.SDK project**
2. **🤖 Build Python->C# type generator**
3. **📡 Implement NodetoolClient with full API**
4. **🧪 Add comprehensive tests**

### **Phase 2: VL Integration** _(~2-3 days)_

1. **🔄 Refactor existing VL code to use SDK**
2. **✅ Keep all current functionality working**
3. **📚 Ensure documentation tooltips still work**
4. **🧪 Test node and workflow execution**

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

**Timeline: 1-2 weeks** for a production-ready SDK that transforms VL integration and unlocks Unity/enterprise potential.
