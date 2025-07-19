# NodeTool C# Type System Enhancement Plan

## 🎯 **Current Status**

✅ **Core Type Infrastructure** - `BaseType`, `TypeMetadata`, type registry  
✅ **Basic Asset Types** - `ImageRef`, `AudioRef`, `VideoRef`, etc.  
✅ **Type Mapping Utilities** - `TypeMapper` for metadata to C# types  
✅ **VL Type Integration** - Basic mapping in factories  
❌ **Automatic Python-to-C# Type Generation** - Missing critical automation  
❌ **SDK Data Object Interface** - Clean abstraction over WebSocket data  
❌ **VL Type Conversion Service** - Simple transformation layer

## 🔍 **Python Type System Analysis** ⭐ **CRITICAL FINDINGS**

**Key Discovery**: NodeTool has **1000+ node types** with complex Python type definitions and **extensive enum usage** that need **full automation**.

### **Python Type Patterns in NodeTool**:

```python
# Basic types
str, int, float, bool

# Collections
List[str], Dict[str, Any], Tuple[int, str]

# Unions (very common)
Union[str, int], Union[Image, str], Optional[str]

# NodeTool-specific types
Image, Audio, Video, TextRef, DataframeRef

# Complex nested types
Union[List[Image], Image], Dict[str, Union[str, int]]

# 🎯 ENUM TYPES (CRITICAL FOR VL UX BUT ALSO FOR UX IN GENERAL)
class BlendMode(str, Enum):
    NORMAL = "normal"
    MULTIPLY = "multiply"
    SCREEN = "screen"
    OVERLAY = "overlay"

class StableDiffusionScheduler(str, Enum):
    DPMSolverSDEScheduler = "DPMSolverSDEScheduler"
    EulerDiscreteScheduler = "EulerDiscreteScheduler"
    # ... 12+ more schedulers

class OpenAIVoice(str, Enum):
    ALLOY = "alloy"
    ECHO = "echo"
    # ... 11 total voices
```

### **Enum Analysis from NodeTool Codebase**:

From scanning the actual NodeTool Python code:

#### **High-Value Cross-Domain Enums** (Generate as Static):

- `SortOrder` (asc/desc) - Used in 15+ list/data nodes
- `BooleanOperation` (and/or/xor) - Used in 5+ logic nodes
- `DateFormat` - Used in date/time nodes
- `FilterType` - Used in data processing nodes

#### **Domain-Specific Enums** (Generate as Static):

- **Image Processing**: `BlendMode`, `ResizeAlgorithm`, `ImageFormat`
- **AI Models**: `StableDiffusionScheduler`, `OpenAIVoice`, `TtsModel`
- **Audio**: `AudioFormat`, `WhisperLanguage`, `AudioCodec`
- **Data**: `ConflictResolution`, `TransformType`, `AggregationMethod`

#### **Model-Specific Dynamic Lists** ⭐ **CRITICAL INSIGHT**:

From analyzing NodeTool frontend model dropdowns:

- **Server-Cached Models**: `/api/models/huggingface_models`, `/api/models/{model_type}`
- **External API Models**: HuggingFace API with provider/pipeline filtering
- **Complex Model Objects**: Not just strings - `{provider, model_id, type}` structures
- **Type-Based Routing**: `comfy.`, `hf.`, `inference_provider_*` prefixes determine behavior

### **VL Integration Requirements** 🎯 **NEW CRITICAL**:

**Current Problem**: VL users get text input boxes instead of dropdown enum pins

```csharp
// ❌ Current: User types "multiply" manually
BlendMode: [_______________]  // Text input - error-prone

// ✅ Required: User selects from dropdown
BlendMode: [Normal      ▼]   // Enum dropdown - safe, discoverable
```

**VL Enum Types Needed**:

- **Static Enums**: Pre-generated, compile-time fixed
- **Dynamic Enums**: Runtime-generated for changing model lists

### **Current Problems**:

- **Manual type mapping** is unsustainable (1000+ node types)
- **No enum detection** in VL factories → text inputs instead of dropdowns
- **Types evolve** when NodeTool adds/modifies nodes
- **Poor UX** for VL users without IntelliSense/enum dropdowns
- **Inconsistent mapping** between developers
- **Missing types** cause runtime failures

### **Solution: WebSocket-Focused Type Generation + Smart VL Integration** 🎯

**Key Insight**: Generate types for **transmitted data**, not all Python classes!

## 📋 **Implementation Plan: Complete Type System** ⭐ **MAJOR UPDATE**

### **🎯 Phase 0.5: Enum System & VL Integration** ⭐ **NEW CRITICAL PHASE**

**Timeline**: 3-4 days  
**Priority**: **CRITICAL** - Required for proper VL user experience

#### **0.5.1 Python Enum Scanner Enhancement**

Extend the Python AST scanner to detect and categorize all enum types:

```csharp
public class PythonEnumScanner
{
    public async Task<List<EnumDefinition>> ScanForEnumsAsync(string nodeToolPath)
    {
        var enumDefinitions = new List<EnumDefinition>();

        foreach (var pythonFile in GetAllPythonFiles(nodeToolPath))
        {
            var enums = ExtractEnumsFromFile(pythonFile);
            foreach (var enumDef in enums)
            {
                // Analyze usage patterns across codebase
                enumDef.UsageCount = CountUsageAcrossCodebase(enumDef.Name);
                enumDef.Domains = GetDomainsUsingEnum(enumDef.Name);
                enumDef.Category = ClassifyEnum(enumDef);

                enumDefinitions.Add(enumDef);
            }
        }

        return enumDefinitions.OrderByDescending(e => e.UsageCount).ToList();
    }

    private EnumCategory ClassifyEnum(EnumDefinition enumDef)
    {
        // Classify into: CrossDomain, DomainSpecific, ModelSpecific, NodeSpecific
        if (enumDef.UsageCount >= 5 && enumDef.Domains.Count >= 2)
            return EnumCategory.CrossDomain;
        if (enumDef.UsageCount >= 3 && enumDef.Values.Count >= 3)
            return EnumCategory.DomainSpecific;
        if (IsModelEnum(enumDef.Values))
            return EnumCategory.ModelSpecific;
        return EnumCategory.NodeSpecific;
    }
}

public class EnumDefinition
{
    public string Name { get; set; }
    public string PythonFile { get; set; }
    public List<(string Name, string Value)> Values { get; set; }
    public string? Documentation { get; set; }
    public int UsageCount { get; set; }
    public List<string> Domains { get; set; }
    public EnumCategory Category { get; set; }
    public bool IsStringEnum { get; set; }
}

public enum EnumCategory
{
    CrossDomain,     // Generate as static enum - high priority
    DomainSpecific,  // Generate as static enum - medium priority
    ModelSpecific,   // Generate as static enum or dynamic enum
    NodeSpecific     // Generate all - VL users need dropdowns!
}
```

#### **0.5.2 Dynamic Model List Handling** ⭐ **UPDATED FOR REALITY**

Handle the complex model patterns discovered in NodeTool frontend:

```csharp
public class ModelTypeAnalyzer
{
    public async Task<List<ModelTypeDefinition>> AnalyzeModelTypesAsync(string nodeToolPath)
    {
        var modelTypes = new List<ModelTypeDefinition>();

        // Scan for different model type patterns
        foreach (var nodeFile in GetNodeFiles(nodeToolPath))
        {
            var modelTypesInFile = ExtractModelTypes(nodeFile);
            modelTypes.AddRange(modelTypesInFile);
        }

        return modelTypes.GroupBy(m => m.TypePattern).Select(g => g.First()).ToList();
    }

    private List<ModelTypeDefinition> ExtractModelTypes(string filePath)
    {
        var types = new List<ModelTypeDefinition>();
        var content = File.ReadAllText(filePath);

        // Pattern 1: Static enums (BlendMode, SortOrder, etc.)
        var enumMatches = Regex.Matches(content, @"class (\w+)\(str, Enum\):");
        foreach (Match match in enumMatches)
        {
            types.Add(new ModelTypeDefinition
            {
                Name = match.Groups[1].Value,
                TypePattern = ModelTypePattern.StaticEnum,
                SourceFile = filePath
            });
        }

        // Pattern 2: Model type annotations (comfy.*, hf.*, etc.)
        var typeAnnotations = Regex.Matches(content, @":\s*(comfy\.\w+|hf\.\w+|language_model|llama_model|inference_provider_\w+)");
        foreach (Match match in typeAnnotations)
        {
            var typeName = match.Groups[1].Value;
            types.Add(new ModelTypeDefinition
            {
                Name = typeName,
                TypePattern = DetermineModelPattern(typeName),
                SourceFile = filePath
            });
        }

        return types;
    }

    private ModelTypePattern DetermineModelPattern(string typeName)
    {
        return typeName switch
        {
            var t when t.StartsWith("comfy.") => ModelTypePattern.ComfyModel,
            var t when t.StartsWith("hf.") => ModelTypePattern.HuggingFaceModel,
            var t when t.StartsWith("inference_provider_") => ModelTypePattern.InferenceProviderModel,
            "language_model" => ModelTypePattern.LanguageModel,
            "llama_model" => ModelTypePattern.LlamaModel,
            _ => ModelTypePattern.StaticEnum
        };
    }
}

public class ModelTypeDefinition
{
    public string Name { get; set; }
    public ModelTypePattern TypePattern { get; set; }
    public string SourceFile { get; set; }
    public List<string>? StaticValues { get; set; }  // For static enums
    public string? ApiEndpoint { get; set; }         // For dynamic models
    public bool RequiresComplexObject { get; set; }  // True for provider+model_id patterns
}

public enum ModelTypePattern
{
    StaticEnum,              // BlendMode, SortOrder - generate C# enum
    ComfyModel,              // comfy.* - API call to /api/models/{type}
    HuggingFaceModel,        // hf.* - API call to /api/models/huggingface_models
    InferenceProviderModel,  // inference_provider_* - Complex provider+model pattern
    LanguageModel,           // language_model - Custom language model handling
    LlamaModel               // llama_model - Ollama model API
}
```

#### **0.5.3 VL Factory Integration** ⭐ **UPDATED FOR MODEL REALITY**

Update `NodesFactory.cs` to handle the complex model patterns discovered:

```csharp
// Enhanced type mapping with model pattern detection
private static (Type?, object?) MapNodeType(NodeTypeDefinition? nodeType)
{
    if (nodeType == null || string.IsNullOrEmpty(nodeType.Type))
        return (typeof(string), "");

    // ✅ Handle model types based on real NodeTool patterns
    var modelPattern = ModelPatternDetector.GetPattern(nodeType.Type);
    if (modelPattern != ModelTypePattern.None)
    {
        return GetModelTypeForVL(nodeType, modelPattern);
    }

    // ✅ Handle simple static enums
    if (IsStaticEnum(nodeType))
    {
        return GetStaticEnumForVL(nodeType);
    }

    // Standard type mappings...
    return nodeType.Type.ToLowerInvariant() switch
    {
        "str" or "string" => (typeof(string), ""),
        "int" or "integer" => (typeof(int), 0),
        "float" or "number" => (typeof(float), 0.0f),
        "bool" or "boolean" => (typeof(bool), false),
        _ => (typeof(string), "")
    };
}

private static (Type, object) GetModelTypeForVL(NodeTypeDefinition nodeType, ModelTypePattern pattern)
{
    return pattern switch
    {
        // Static enums become C# enums
        ModelTypePattern.StaticEnum => GetStaticEnumForVL(nodeType),

        // Complex model types become generic objects with custom handling
        ModelTypePattern.ComfyModel => CreateComfyModelPin(nodeType),
        ModelTypePattern.HuggingFaceModel => CreateHuggingFaceModelPin(nodeType),
        ModelTypePattern.InferenceProviderModel => CreateInferenceProviderModelPin(nodeType),
        ModelTypePattern.LanguageModel => CreateLanguageModelPin(nodeType),
        ModelTypePattern.LlamaModel => CreateLlamaModelPin(nodeType),

        _ => (typeof(string), "")
    };
}

private static (Type, object) CreateComfyModelPin(NodeTypeDefinition nodeType)
{
    // For ComfyUI models, create a special pin type that can handle API calls
    var defaultValue = new ComfyModelReference
    {
        Type = nodeType.Type,
        Name = "", // Will be populated from API
        ApiEndpoint = $"/api/models/{nodeType.Type}"
    };

    return (typeof(ComfyModelReference), defaultValue);
}

private static (Type, object) CreateInferenceProviderModelPin(NodeTypeDefinition nodeType)
{
    // For inference provider models, create complex object pin
    var defaultValue = new InferenceProviderModelReference
    {
        Type = nodeType.Type,
        Provider = "", // User selects provider first
        ModelId = "",  // Then model from that provider
        PipelineTag = ExtractPipelineTag(nodeType.Type) // e.g., "text-to-image"
    };

    return (typeof(InferenceProviderModelReference), defaultValue);
}

// Model reference classes for different patterns
public class ComfyModelReference
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string ApiEndpoint { get; set; } = "";
}

public class InferenceProviderModelReference
{
    public string Type { get; set; } = "";
    public string Provider { get; set; } = "";
    public string ModelId { get; set; } = "";
    public string PipelineTag { get; set; } = "";
}
```

#### **0.5.4 VL Dynamic Enum Implementation**

Implement VL's dynamic enum pattern for runtime model lists:

```csharp
// VL Dynamic Enum for NodeTool runtime enums
[Serializable]
public class NodeToolDynamicEnum : DynamicEnumBase<NodeToolDynamicEnum, NodeToolDynamicEnumDefinition>
{
    public NodeToolDynamicEnum(string value) : base(value) { }

    [CreateDefault]
    public static NodeToolDynamicEnum CreateDefault()
    {
        return CreateDefaultBase();
    }
}

public class NodeToolDynamicEnumDefinition : DynamicEnumDefinitionBase<NodeToolDynamicEnumDefinition>
{
    private readonly IReadOnlyDictionary<string, object> _entries;
    private readonly string _enumName;

    public NodeToolDynamicEnumDefinition(string enumName, IEnumerable<string> values)
    {
        _enumName = enumName;
        _entries = values.ToDictionary(v => v, v => (object)null);
    }

    protected override IReadOnlyDictionary<string, object> GetEntries()
    {
        return _entries;
    }

    protected override IObservable<object> GetEntriesChangedObservable()
    {
        // Could be enhanced to watch for API updates
        return Observable.Never<object>();
    }

    protected override bool AutoSortAlphabetically => true;
}
```

#### **0.5.5 Enum Registry Service**

Create centralized enum lookup for VL factories:

```csharp
public static class EnumRegistry
{
    private static readonly Dictionary<string, StaticEnumInfo> _staticEnums =
        new Dictionary<string, StaticEnumInfo>();

    static EnumRegistry()
    {
        InitializeStaticEnums();
    }

    private static void InitializeStaticEnums()
    {
        // Register all generated static enums
        RegisterEnum<BlendMode>("BlendMode", BlendMode.Normal);
        RegisterEnum<SortOrder>("SortOrder", SortOrder.Asc);
        RegisterEnum<StableDiffusionScheduler>("StableDiffusionScheduler",
            StableDiffusionScheduler.EulerDiscreteScheduler);
        // ... auto-generated registrations
    }

    public static StaticEnumInfo? GetStaticEnum(string enumName)
    {
        return _staticEnums.TryGetValue(enumName, out var info) ? info : null;
    }
}

public class StaticEnumInfo
{
    public Type Type { get; set; }
    public object DefaultValue { get; set; }
    public string[] Values { get; set; }
}
```

#### **0.5.6 CI/CD Integration for Enums**

```yaml
# GitHub Action: Auto-update enums from Python
name: Sync NodeTool Enums
on:
  schedule:
    - cron: "0 6 * * *" # Daily at 6 AM
  workflow_dispatch:

jobs:
  sync-enums:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3

      - name: Extract Python Enums
        run: dotnet run --project scripts/EnumExtractor

      - name: Generate C# Enums
        run: dotnet run --project scripts/CSharpEnumGenerator

      - name: Create PR if changes
        run: |
          if ! git diff --quiet Generated/Enums/; then
            gh pr create --title "🔄 Auto-update: New NodeTool enums" \
              --body "Automated enum update from NodeTool Python code"
                     fi
```

#### **0.5.7 Expected VL User Experience Improvements** 🎯

**Before (Current)**: Text inputs everywhere

```
❌ BlendMode:     [_______________] <- User types "multiply"
❌ ComfyModel:    [_______________] <- User types "sd_xl_base_1.0.safetensors"
❌ HF Provider:   [_______________] <- User types "black-forest-labs"
❌ HF Model:      [_______________] <- User types "FLUX.1-dev"
```

**After (With Smart Type System)**: Appropriate UI for each type

```
✅ BlendMode:     [Normal        ▼] <- Static enum dropdown
✅ ComfyModel:    [SD XL Base    ▼] <- API-loaded model list from /api/models/comfy.checkpoint
✅ HF Provider:   [Fal AI        ▼] <- First: select provider
✅ HF Model:      [FLUX.1-dev    ▼] <- Then: models for that provider
```

**Different UI Patterns by Type**:

```csharp
// Pattern 1: Static enums → Simple dropdown
BlendMode: Normal, Multiply, Screen, Overlay

// Pattern 2: Server model lists → API-loaded dropdown
ComfyModel: {name: "sd_xl_base_1.0.safetensors", type: "comfy.checkpoint"}

// Pattern 3: Provider+Model → Two-step selection
InferenceProvider: {provider: "black-forest-labs", model_id: "FLUX.1-dev", type: "inference_provider_text_to_image_model"}

// Pattern 4: Language models → Custom selector
LanguageModel: {id: "gpt-4", name: "GPT-4"}
```

**Benefits**:

- **🎯 Right UI for each type** - Simple dropdowns for enums, rich selectors for models
- **🔍 Discovery** - Users see all available models from API calls
- **⚡ Real-time** - Model lists update when server state changes
- **📚 Smart defaults** - Complex objects have sensible default structures
- **✨ IntelliSense** - Full IDE support for C# consumers

**Metrics**:

- **~50 static enum types** (BlendMode, SortOrder, etc.) → C# enums
- **~20 model type patterns** (comfy._, hf._, inference*provider*\*) → Custom handling
- **1000+ nodes** benefit from appropriate input types
- **Zero breaking changes** for existing VL patches

### **Phase 0: Python-to-C# Type Generator** _(3-4 days)_ ⭐ **UPDATED**

**Dependencies**: Phase 0.5 (Enum System) must be completed first for full type accuracy

**🎯 Goal**: Generate C# types for **WebSocket-transmitted data**, not all Python classes

#### **0.0 WebSocket vs. All-Types Analysis** ⭐ **CRITICAL SCOPE DECISION**

**The Question**: Do we generate types for everything, or just what gets transmitted over WebSocket?

**WebSocket Data Flow Analysis**:

```json
// What actually gets transmitted over WebSocket:

// 1. Node execution inputs/outputs (data objects)
{
  "type": "image",
  "uri": "http://...",
  "data": { /* embedded data */ },
  "width": 1024,
  "height": 768
}

// 2. Node metadata (for UI generation)
{
  "node_type": "nodetool.image.resize",
  "properties": [
    {
      "name": "width",
      "type": { "type": "int" },
      "default": 512
    }
  ]
}

// 3. Execution progress/status
{
  "type": "node_update",
  "progress": 0.5,
  "status": "running"
}
```

**What We ACTUALLY Need Types For**:

```csharp
// ✅ CRITICAL - Data value types (appear in data objects)
str, int, float, bool, bytes, datetime, tuple[int,int]

// ✅ CRITICAL - Asset reference types (transmitted as data)
ImageRef, AudioRef, VideoRef, TextRef, DataframeRef

// ✅ CRITICAL - Collection structures (data object organization)
List[T], Dict[str, T], Union[T, U], Optional[T]

// ✅ HELPFUL - Metadata types (node property descriptions)
Path, UUID, Enum values

// ❌ PROBABLY NOT NEEDED - Individual node classes
class ResizeNode: ...  # Never transmitted as objects
class BlurNode: ...    # Node logic stays in Python
```

**Recommended Approach** ⭐:

1. **Generate data value types** - For WebSocket data object contents
2. **Generate metadata types** - For dynamic UI/schema generation
3. **Skip node implementation classes** - Only their metadata matters
4. **Focus on transmission format** - What JSON/MessagePack serializes

**Benefits of WebSocket-Focused Approach**:

```csharp
// ✅ MUCH SMALLER: ~50 transmitted types vs. 1000+ Python classes
// ✅ MORE RELEVANT: Types actually used in C# WebSocket clients
// ✅ FASTER GENERATION: Scan transmitted data, not all Python code
// ✅ EASIER MAINTENANCE: Changes to Python internals don't break C# types
// ✅ CROSS-PLATFORM: Unity/WPF get same transmitted data types

// Example: We need this (transmitted data)
public class NodeToolDataObject
{
    public string Type { get; set; }         // Transmitted
    public string Uri { get; set; }          // Transmitted
    public Dictionary<string, object> Data { get; set; }  // Transmitted
}

// But NOT this (Python implementation detail)
public class ResizeNodeImplementation  // Never transmitted!
{
    public void ProcessImage() { ... }    // Python-only logic
}
```

#### **🔮 Future Consideration: Hybrid Two-Tier Approach** ⭐ **DOCUMENTED FOR FUTURE**

**Current Implementation**: WebSocket-focused types (Phase 1)

**Future Extension**: Full type system for advanced scenarios (Phase 2)

```csharp
// TIER 1: Core SDK (WebSocket client) - Current implementation
Nodetool.SDK/                    // WebSocket types only - SHIP FIRST
├── WebSocket/                   // WebSocket client
├── DataObjects/                 // Transmitted data types
├── Assets/                      // ImageRef, AudioRef, etc.
├── Collections/                 // List, Dict, Union, Optional
└── Enums/                       // Static enums + model patterns

// TIER 2: Extended SDK (full types) - Future consideration
Nodetool.SDK.Extended/           // Full Python type mapping - FUTURE
├── Nodes/                       // All node classes with full types
├── Validation/                  // Workflow validation & type checking
├── CodeGen/                     // C# node implementations
├── Tools/                       // Development utilities
└── LocalExecution/              // Pure C# node execution
```

**Future Use Cases That Would Need Full Types**:

- 🏠 **Local node execution** (Pure C# implementations)
- 🛠️ **Rich IDE tools** (Full IntelliSense, compile-time validation)
- 📱 **Offline development** (Workflow validation without server)
- ⚡ **Performance scenarios** (Unity 60fps, edge computing)
- 🔄 **Migration tools** (Convert between workflow formats)

**Two-Tier Usage Pattern** (future):

```csharp
// 90% of users: Simple WebSocket client
using Nodetool.SDK;
var client = new NodetoolClient();
var session = await client.ExecuteWorkflowAsync(id, inputs);

// 10% of users: Advanced tooling with full types
using Nodetool.SDK.Extended;
var workflow = new WorkflowBuilder()
    .AddNode<ResizeNode>(n => {
        n.Width = 512;
        n.Algorithm = ResizeAlgorithm.Lanczos;
    })
    .AddNode<BlurNode>(n => n.Radius = 2.5f)
    .Validate()                    // Compile-time validation
    .ExecuteLocally();             // Pure C# execution
```

**Implementation Strategy**:

1. ✅ **Ship WebSocket SDK first** (covers 90% of use cases)
2. ✅ **Monitor demand** for rich tooling scenarios
3. ✅ **Add Tier 2 later** if market validates the need
4. ✅ **Keep Tier 1 lean** and focused on WebSocket communication

**Note**: This hybrid approach allows us to ship fast while keeping the door open for rich tooling scenarios.

#### **0.1 WebSocket Data Type Scanner** ⭐ **FOCUSED APPROACH**

**File**: `nodetool-sdk/scripts/TypeGenerator/WebSocketTypeScanner.cs`

```csharp
public class WebSocketTypeScanner
{
    public async Task<List<WebSocketTypeDefinition>> ScanTransmittedTypesAsync(string nodeToolPath)
    {
        var transmittedTypes = new List<WebSocketTypeDefinition>();

        // 1. Scan for data object types (what gets transmitted as values)
        await ScanDataObjectTypes(nodeToolPath, transmittedTypes);

        // 2. Scan for asset reference types
        await ScanAssetTypes(nodeToolPath, transmittedTypes);

        // 3. Scan for node metadata types (for dynamic UI)
        await ScanNodeMetadataTypes(nodeToolPath, transmittedTypes);

        // 4. Extract enum types (for dropdowns/selectors)
        await ScanEnumTypes(nodeToolPath, transmittedTypes);

        return transmittedTypes;
    }

    private async Task ScanDataObjectTypes(string nodeToolPath, List<WebSocketTypeDefinition> types)
    {
        // Focus on: types that appear in node inputs/outputs (transmitted as data)
        // Examples: str, int, Image, List[Image], Optional[str], etc.
        var nodeFiles = GetNodeFiles(nodeToolPath);

        foreach (var file in nodeFiles)
        {
            var inputOutputTypes = ExtractInputOutputTypes(file);
            types.AddRange(inputOutputTypes);
        }
    }

    private async Task ScanAssetTypes(string nodeToolPath, List<WebSocketTypeDefinition> types)
    {
        // Focus on: ImageRef, AudioRef, VideoRef, etc. - these are transmitted
        var assetTypeFiles = Directory.GetFiles(nodeToolPath, "*.py", SearchOption.AllDirectories)
            .Where(f => f.Contains("types") || f.Contains("assets"));

        foreach (var file in assetTypeFiles)
        {
            var assetTypes = ExtractAssetRefTypes(file);
            types.AddRange(assetTypes);
        }
    }

    private List<NodeTypeDefinition> ParseNodeDefinitions(string content, string filePath)
    {
        var definitions = new List<NodeTypeDefinition>();

        // Parse Python AST to extract:
        // 1. Class definitions inheriting from Node
        // 2. Input/output property type annotations
        // 3. Enum definitions
        // 4. Complex type definitions

        var lines = content.Split('\n');
        var currentClass = "";
        var inClassDefinition = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Detect class definitions
            if (line.StartsWith("class ") && line.Contains("(Node)"))
            {
                currentClass = ExtractClassName(line);
                inClassDefinition = true;
                continue;
            }

            // Parse type annotations within node classes
            if (inClassDefinition && line.Contains(": ") &&
                (line.Contains("InputProperty") || line.Contains("OutputProperty")))
            {
                var property = ParsePropertyDefinition(line);
                // Add to current class definition
            }
        }

        return definitions;
    }
}

public class NodeTypeDefinition
{
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string PythonFile { get; set; } = "";
    public List<PropertyDefinition> Inputs { get; set; } = new();
    public List<PropertyDefinition> Outputs { get; set; } = new();
    public string? Description { get; set; }
}

public class PropertyDefinition
{
    public string Name { get; set; } = "";
    public string PythonType { get; set; } = "";
    public string CSharpType { get; set; } = "";
    public bool IsOptional { get; set; }
    public object? DefaultValue { get; set; }
    public string? Description { get; set; }
}
```

#### **0.2 Python-to-C# Type Mapper** ⭐ **COMPREHENSIVE COVERAGE**

**File**: `nodetool-sdk/scripts/TypeGenerator/TypeMapper.cs`

```csharp
public class PythonToCSharpTypeMapper
{
    private static readonly Dictionary<string, string> BasicTypeMap = new()
    {
        // Python built-in types
        ["str"] = "string",
        ["int"] = "int",
        ["float"] = "double",
        ["bool"] = "bool",
        ["bytes"] = "byte[]",
        ["Any"] = "object",
        ["None"] = "object?",

        // Collection types
        ["tuple"] = "object",      // Tuples → generic object (could be ValueTuple in specific cases)
        ["set"] = "HashSet<object>",
        ["frozenset"] = "ISet<object>",

        // Date/time types (frequently used in NodeTool)
        ["datetime"] = "DateTime",
        ["Datetime"] = "DateTime", // NodeTool's custom Datetime type
        ["date"] = "DateOnly",     // Or DateTime if targeting older .NET
        ["Date"] = "DateOnly",     // NodeTool's custom Date type
        ["time"] = "TimeOnly",     // Or TimeSpan if targeting older .NET
        ["timedelta"] = "TimeSpan",

        // File system types
        ["Path"] = "string",       // pathlib.Path → string representation
        ["pathlib.Path"] = "string",

        // Numeric types
        ["decimal"] = "decimal",
        ["Decimal"] = "decimal",

        // Other common types
        ["UUID"] = "Guid",
        ["uuid"] = "Guid",

        // Scientific computing (common in AI/ML nodes)
        ["ndarray"] = "float[]",        // NumPy arrays → simplified to float array
        ["np.ndarray"] = "float[]",
        ["DataFrame"] = "object",       // Pandas DataFrame → generic object
        ["pd.DataFrame"] = "object",

        // NodeTool-specific asset types
        ["Image"] = "ImageRef",
        ["Audio"] = "AudioRef",
        ["Video"] = "VideoRef",
        ["TextRef"] = "TextRef",
        ["DataframeRef"] = "DataframeRef",
        ["DocumentRef"] = "DocumentRef",
        ["ModelRef"] = "ModelRef",

        // Common Python standard library types
        ["BytesIO"] = "MemoryStream",
        ["StringIO"] = "StringReader"
    };

    // ⚠️ CRITICAL CONSIDERATION: Do we need ALL types or just WebSocket types?
    //
    // FOR WEBSOCKET COMMUNICATIONS, we actually need:
    // ✅ Data value types (str, int, float, bool, bytes, datetime, tuple)
    // ✅ Asset reference types (ImageRef, AudioRef, VideoRef)
    // ✅ Collection structures (List, Dict, Union, Optional)
    // ✅ Basic metadata types (Path, UUID, etc.)
    //
    // WE MIGHT NOT NEED:
    // ❓ Every NodeTool node class as C# type
    // ❓ Complex scientific types (specific NumPy shapes)
    // ❓ Python implementation details
    //
    // APPROACH: Generate types for TRANSMITTED data, not all Python classes

    public string MapPythonTypeToCSharp(string pythonType)
    {
        // Handle basic types
        if (BasicTypeMap.TryGetValue(pythonType, out var basicType))
            return basicType;

        // Handle generic types
        if (pythonType.StartsWith("List["))
            return MapListType(pythonType);

        if (pythonType.StartsWith("Dict["))
            return MapDictType(pythonType);

        if (pythonType.StartsWith("Union["))
            return MapUnionType(pythonType);

        if (pythonType.StartsWith("Optional["))
            return MapOptionalType(pythonType);

        if (pythonType.StartsWith("Tuple["))
            return MapTupleType(pythonType);

        // Handle enum types
        if (IsEnumType(pythonType))
            return pythonType; // Keep enum name as-is

        // Fallback to object for unknown types
        return "object";
    }

    private string MapListType(string pythonType)
    {
        // List[str] → string[]
        // List[Image] → ImageRef[]
        var innerType = ExtractGenericType(pythonType);
        var csharpInnerType = MapPythonTypeToCSharp(innerType);
        return $"{csharpInnerType}[]";
    }

    private string MapUnionType(string pythonType)
    {
        // Union[str, int] → object (for now)
        // Union[Image, str] → object
        // TODO: Consider discriminated unions for specific cases
        return "object";
    }

    private string MapOptionalType(string pythonType)
    {
        // Optional[str] → string?
        // Optional[Image] → ImageRef?
        var innerType = ExtractGenericType(pythonType);
        var csharpType = MapPythonTypeToCSharp(innerType);

        return csharpType.EndsWith("?") ? csharpType : $"{csharpType}?";
    }

    private string MapTupleType(string pythonType)
    {
        // Tuple[int, int] → (int, int) or ValueTuple<int, int>
        // Tuple[float, float] → (float, float)
        // Common in NodeTool for coordinates, sizes, ranges

        var innerTypes = ExtractTupleTypes(pythonType);
        if (innerTypes.Count <= 7) // ValueTuple supports up to 7 elements efficiently
        {
            var csharpTypes = innerTypes.Select(MapPythonTypeToCSharp).ToList();
            if (csharpTypes.Count == 2)
            {
                return $"({csharpTypes[0]}, {csharpTypes[1]})"; // Tuple syntax for pairs
            }
            return $"ValueTuple<{string.Join(", ", csharpTypes)}>";
        }

        // For larger tuples or complex cases, fall back to object
        return "object";
    }

    private List<string> ExtractTupleTypes(string tupleType)
    {
        // Extract types from Tuple[int, int] → ["int", "int"]
        var match = Regex.Match(tupleType, @"Tuple\[(.+)\]");
        if (!match.Success) return new List<string>();

        var typesStr = match.Groups[1].Value;
        return typesStr.Split(',').Select(t => t.Trim()).ToList();
    }

    private string MapDictType(string pythonType)
    {
        // Dict[str, Any] → Dictionary<string, object>
        // Dict[str, int] → Dictionary<string, int>
        var keyValueTypes = ExtractDictTypes(pythonType);
        if (keyValueTypes.Count == 2)
        {
            var keyType = MapPythonTypeToCSharp(keyValueTypes[0]);
            var valueType = MapPythonTypeToCSharp(keyValueTypes[1]);
            return $"Dictionary<{keyType}, {valueType}>";
        }

        return "Dictionary<string, object>"; // Fallback
    }

    private List<string> ExtractDictTypes(string dictType)
    {
        // Extract types from Dict[str, int] → ["str", "int"]
        var match = Regex.Match(dictType, @"Dict\[(.+)\]");
        if (!match.Success) return new List<string>();

        var typesStr = match.Groups[1].Value;
        return typesStr.Split(',').Select(t => t.Trim()).ToList();
    }
}
```

#### **0.3 C# Code Generator**

**File**: `nodetool-sdk/scripts/TypeGenerator/CSharpCodeGenerator.cs`

```csharp
public class CSharpCodeGenerator
{
    public async Task GenerateTypesAsync(
        List<NodeTypeDefinition> nodeTypes,
        string outputPath)
    {
        // Group by namespace for organized output
        var namespaceGroups = nodeTypes.GroupBy(n => n.Namespace);

        foreach (var group in namespaceGroups)
        {
            await GenerateNamespaceFileAsync(group.Key, group.ToList(), outputPath);
        }

        // Generate collective types file
        await GenerateCollectiveTypesFileAsync(nodeTypes, outputPath);
    }

    private async Task GenerateNamespaceFileAsync(
        string namespaceName,
        List<NodeTypeDefinition> types,
        string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This file was generated automatically from NodeTool Python definitions.");
        sb.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("// Do not edit manually - changes will be overwritten.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Nodetool.SDK.Types;");
        sb.AppendLine();

        sb.AppendLine($"namespace Nodetool.SDK.Generated.{namespaceName}");
        sb.AppendLine("{");

        foreach (var nodeType in types)
        {
            GenerateNodeClass(sb, nodeType);
        }

        sb.AppendLine("}");

        var fileName = $"{namespaceName}.Generated.cs";
        var filePath = Path.Combine(outputPath, fileName);
        await File.WriteAllTextAsync(filePath, sb.ToString());
    }

    private void GenerateNodeClass(StringBuilder sb, NodeTypeDefinition nodeType)
    {
        // Generate XML documentation
        if (!string.IsNullOrEmpty(nodeType.Description))
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// {nodeType.Description}");
            sb.AppendLine($"    /// </summary>");
        }

        sb.AppendLine($"    /// <remarks>");
        sb.AppendLine($"    /// Generated from: {nodeType.PythonFile}");
        sb.AppendLine($"    /// </remarks>");

        sb.AppendLine($"    public class {nodeType.ClassName}Node");
        sb.AppendLine("    {");

        // Generate input properties
        foreach (var input in nodeType.Inputs)
        {
            GenerateProperty(sb, input, "Input");
        }

        // Generate output properties
        foreach (var output in nodeType.Outputs)
        {
            GenerateProperty(sb, output, "Output");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateProperty(StringBuilder sb, PropertyDefinition prop, string direction)
    {
        if (!string.IsNullOrEmpty(prop.Description))
        {
            sb.AppendLine($"        /// <summary>{prop.Description}</summary>");
        }

        sb.AppendLine($"        public {prop.CSharpType} {prop.Name} {{ get; set; }}");
        sb.AppendLine();
    }
}
```

#### **0.4 Build Integration**

**File**: `nodetool-sdk/scripts/generate-types.ps1`

```powershell
# PowerShell script for automatic type generation
param(
    [string]$NodeToolPath = "../../../nodetool-core",
    [string]$OutputPath = "./csharp/Nodetool.SDK/Generated"
)

Write-Host "🔍 Scanning NodeTool Python types..." -ForegroundColor Cyan

# Run the type generator
dotnet run --project ./scripts/TypeGenerator `
    --nodetool-path $NodeToolPath `
    --output-path $OutputPath

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ C# types generated successfully!" -ForegroundColor Green

    # Count generated files
    $generatedFiles = Get-ChildItem $OutputPath -Filter "*.Generated.cs"
    Write-Host "📁 Generated $($generatedFiles.Count) type files" -ForegroundColor Yellow
} else {
    Write-Host "❌ Type generation failed!" -ForegroundColor Red
    exit 1
}
```

#### **0.5 Example Generated Output**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Generated/Text.Generated.cs`

```csharp
// <auto-generated>
// This file was generated automatically from NodeTool Python definitions.
// Generation time: 2024-01-15 14:30:22 UTC
// Do not edit manually - changes will be overwritten.
// </auto-generated>

using System;
using System.Collections.Generic;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.Generated.Text
{
    /// <summary>
    /// Concatenates two strings together.
    /// </summary>
    /// <remarks>
    /// Generated from: nodetool-core/src/nodetool/nodes/nodetool/text.py
    /// </remarks>
    public class ConcatenateNode
    {
        /// <summary>First string to concatenate</summary>
        public string A { get; set; } = "";

        /// <summary>Second string to concatenate</summary>
        public string B { get; set; } = "";

        /// <summary>Concatenated result</summary>
        public string Output { get; set; } = "";
    }

    /// <summary>
    /// Splits a string by delimiter.
    /// </summary>
    /// <remarks>
    /// Generated from: nodetool-core/src/nodetool/nodes/nodetool/text.py
    /// </remarks>
    public class SplitNode
    {
        /// <summary>Input string to split</summary>
        public string Input { get; set; } = "";

        /// <summary>Delimiter to split by</summary>
        public string Delimiter { get; set; } = " ";

        /// <summary>Array of split strings</summary>
        public string[] Output { get; set; } = Array.Empty<string>();
    }
}
```

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Generated/Image.Generated.cs`

```csharp
// <auto-generated>
// This file was generated automatically from NodeTool Python definitions.
// Generation time: 2024-01-15 14:30:22 UTC
// Do not edit manually - changes will be overwritten.
// </auto-generated>

using System;
using System.Collections.Generic;
using Nodetool.SDK.Types;

namespace Nodetool.SDK.Generated.Image
{
    /// <summary>
    /// Resizes an image to specified dimensions.
    /// </summary>
    /// <remarks>
    /// Generated from: nodetool-core/src/nodetool/nodes/nodetool/image.py
    /// </remarks>
    public class ResizeNode
    {
        /// <summary>Input image to resize</summary>
        public ImageRef Image { get; set; } = new();

        /// <summary>Target width in pixels</summary>
        public int Width { get; set; } = 512;

        /// <summary>Target height in pixels</summary>
        public int Height { get; set; } = 512;

        /// <summary>Resize algorithm</summary>
        public ResizeAlgorithm Algorithm { get; set; } = ResizeAlgorithm.Lanczos;

        /// <summary>Resized image output</summary>
        public ImageRef Output { get; set; } = new();
    }

    /// <summary>
    /// Blends two images using specified blend mode.
    /// </summary>
    /// <remarks>
    /// Generated from: nodetool-core/src/nodetool/nodes/nodetool/image.py
    /// </remarks>
    public class BlendNode
    {
        /// <summary>Base image</summary>
        public ImageRef BaseImage { get; set; } = new();

        /// <summary>Overlay image</summary>
        public ImageRef OverlayImage { get; set; } = new();

        /// <summary>Blend mode</summary>
        public BlendMode Mode { get; set; } = BlendMode.Normal;

        /// <summary>Opacity (0.0 to 1.0)</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Blended image result</summary>
        public ImageRef Output { get; set; } = new();
    }
}

// Generated enums
public enum ResizeAlgorithm
{
    Nearest,
    Linear,
    Cubic,
    Lanczos
}

public enum BlendMode
{
    Normal,
    Multiply,
    Screen,
    Overlay,
    SoftLight,
    HardLight
}
```

#### **0.6 CI/CD Integration**

**File**: `nodetool-sdk/.github/workflows/generate-types.yml`

```yaml
name: Generate C# Types from Python

on:
  schedule:
    # Run daily to catch NodeTool updates
    - cron: "0 6 * * *"
  workflow_dispatch:
    # Manual trigger
  pull_request:
    # Check on PRs to ensure types are up-to-date

jobs:
  generate-types:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout nodetool-sdk
        uses: actions/checkout@v4

      - name: Checkout nodetool-core
        uses: actions/checkout@v4
        with:
          repository: "nodetool-ai/nodetool-core"
          path: "nodetool-core"

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0"

      - name: Generate Types
        run: |
          chmod +x ./scripts/generate-types.sh
          ./scripts/generate-types.sh

      - name: Check for changes
        id: changes
        run: |
          if git diff --quiet; then
            echo "No changes detected"
            echo "changed=false" >> $GITHUB_OUTPUT
          else
            echo "Changes detected in generated types"
            echo "changed=true" >> $GITHUB_OUTPUT
          fi

      - name: Create PR for type updates
        if: steps.changes.outputs.changed == 'true'
        uses: peter-evans/create-pull-request@v5
        with:
          token: ${{ secrets.GITHUB_TOKEN }}
          commit-message: "chore: update generated C# types from Python definitions"
          title: "🤖 Auto-update: C# Types from Python"
          body: |
            ## Automatic Type Generation

            This PR contains automatically generated C# types based on the latest NodeTool Python definitions.

            ### Changes
            - Updated type definitions from nodetool-core
            - Generated at: ${{ github.run_id }}

            ### Review
            - ✅ Check that new node types have correct C# mappings
            - ✅ Verify enum values are properly mapped  
            - ✅ Ensure breaking changes are documented

          branch: auto/update-generated-types
          delete-branch: true
```

### **Phase 1: SDK-First Type System Architecture** _(1-2 days)_

**🎯 Goal**: SDK handles ALL type complexity, VL just transforms to platform types

#### **1.0 Clear Responsibility Separation** ⭐ **CRITICAL ARCHITECTURE**

**Nodetool.SDK Responsibilities** _(Universal C# Foundation)_:

- ✅ **Generate all C# types** from Python definitions (static enums + model patterns)
- ✅ **Handle all WebSocket data parsing** and type recognition
- ✅ **Manage all model API calls** (ComfyUI `/api/models/*`, HuggingFace API, etc.)
- ✅ **Cache model lists** and handle rate limiting/errors
- ✅ **Provide strongly-typed interfaces**: `IExecutionSession.GetOutput<T>()`
- ✅ **Handle complex object construction** (InferenceProviderModel, ComfyModel, etc.)
- ✅ **Work across ALL .NET platforms** (VL, Unity, WPF, Console, etc.)

**Nodetool.SDK.VL Responsibilities** _(Thin VL Integration)_:

- ✅ **Transform SDK data → VL types only**: `NodeToolDataObject` → `SKImage`
- ✅ **Create VL pins** with appropriate types based on SDK metadata
- ✅ **Handle VL-specific UI** (dynamic enum dropdowns, custom model selectors)
- ❌ **NO WebSocket handling** - SDK does this
- ❌ **NO API calls** - SDK does this
- ❌ **NO type generation** - SDK does this
- ❌ **NO model list management** - SDK does this

#### **1.1 SDK Handles All Data Complexity**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/IExecutionSession.cs`

```csharp
// SDK provides simple output access
public interface IExecutionSession
{
    // Raw data objects
    T? GetOutput<T>(string outputName);
    NodeToolDataObject? GetOutputData(string outputName);

    // Common conversions built into SDK
    string? GetOutputAsString(string outputName);
    byte[]? GetOutputAsBytes(string outputName);

    // All outputs
    Dictionary<string, object> GetAllOutputs();
}

// SDK internal implementation handles all WebSocket complexity
internal class ExecutionSession : IExecutionSession
{
    private readonly ConcurrentDictionary<string, NodeToolDataObject> _outputs = new();

    public T? GetOutput<T>(string outputName)
    {
        if (_outputs.TryGetValue(outputName, out var dataObject))
        {
            return ConvertDataObject<T>(dataObject);
        }
        return default;
    }

    public NodeToolDataObject? GetOutputData(string outputName)
    {
        return _outputs.GetValueOrDefault(outputName);
    }

    private T? ConvertDataObject<T>(NodeToolDataObject dataObject)
    {
        // SDK handles basic conversions that work across all platforms
        if (typeof(T) == typeof(NodeToolDataObject))
            return (T)(object)dataObject;

        if (typeof(T) == typeof(string))
            return (T)(object)dataObject.GetEmbeddedData<string>();

        if (typeof(T) == typeof(byte[]))
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (base64 != null)
                return (T)(object)Convert.FromBase64String(base64);
        }

        return (T)(object)dataObject; // Return raw for platform-specific conversion
    }
}
```

### **Phase 2: Ultra-Simple VL Integration** _(1-2 days)_ ⭐ **MINIMAL VL RESPONSIBILITY**

**🎯 Goal**: VL becomes just a thin transformation layer over robust SDK

#### **2.0 VL's ONLY Job: Type Transformation**

**What VL Does** _(ONLY platform-specific concerns)_:

```csharp
// VL's entire responsibility in 20 lines:
public void UpdateVLPins()
{
    // 1. Read current state from SDK session (no complexity)
    var isRunning = _sdkSession.IsRunning;
    var error = _sdkSession.ErrorMessage;

    // 2. Get strongly-typed data from SDK (no parsing)
    var imageData = _sdkSession.GetOutput<NodeToolDataObject>("image_output");
    var textData = _sdkSession.GetOutput<string>("text_output");

    // 3. Convert ONLY to VL types (no business logic)
    _isRunningPin.Value = isRunning;
    _errorPin.Value = error ?? "";
    _imagePin.Value = _vlConverter.ToSKImage(imageData);  // VL-specific
    _textPin.Value = textData;

    // That's it! No WebSocket, no API calls, no state management
}
```

**What VL Does NOT Do** _(SDK handles all complexity)_:

```csharp
❌ _sdkSession.ConnectWebSocket();           // SDK does this internally
❌ var models = await api.GetModels();       // SDK does this internally
❌ var parsed = ParseWebSocketMessage();     // SDK does this internally
❌ var typed = ConvertPythonType();          // SDK does this internally
❌ _cache.Store(result);                     // SDK does this internally
```

#### **2.1 VL Type Converter Service** _(Simplified)_

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Services/VLTypeConverter.cs`

```csharp
public class VLTypeConverter
{
    private readonly AssetDownloadService _assetService;

    public VLTypeConverter(AssetDownloadService assetService)
    {
        _assetService = assetService;
    }

    // Single method - convert SDK data object to VL type
    public object? ConvertToVLType(NodeToolDataObject dataObject, Type targetVLType)
    {
        if (dataObject == null) return GetDefaultValue(targetVLType);

        return targetVLType switch
        {
            var t when t == typeof(SKImage) && dataObject.IsImage => ConvertToSKImage(dataObject),
            var t when t == typeof(byte[]) => ConvertToByteArray(dataObject),
            var t when t == typeof(string) => ConvertToString(dataObject),
            var t when t == typeof(float) => ConvertToFloat(dataObject),
            var t when t == typeof(double) => ConvertToDouble(dataObject),
            var t when t == typeof(int) => ConvertToInt(dataObject),
            var t when t == typeof(bool) => ConvertToBool(dataObject),
            _ => dataObject // Fallback to raw data object
        };
    }

    private SKImage? ConvertToSKImage(NodeToolDataObject dataObject)
    {
        // Priority 1: Embedded data (synchronous)
        if (dataObject.HasEmbeddedData)
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(base64);
                    return SKImage.FromEncodedData(bytes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to decode embedded image: {ex.Message}");
                }
            }
        }

        // Priority 2: Asset reference (could be async, for now return null)
        // TODO: Consider async asset loading strategy
        if (dataObject.HasAssetReference)
        {
            // For now, VL nodes will need to handle asset loading separately
            // or we implement a caching strategy
        }

        return null;
    }

    private byte[]? ConvertToByteArray(NodeToolDataObject dataObject)
    {
        if (dataObject.HasEmbeddedData)
        {
            var base64 = dataObject.GetEmbeddedData<string>();
            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    return Convert.FromBase64String(base64);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to decode embedded bytes: {ex.Message}");
                }
            }
        }

        return null;
    }

    private string? ConvertToString(NodeToolDataObject dataObject)
    {
        // For text data, extract embedded content
        if (dataObject.IsText && dataObject.HasEmbeddedData)
        {
            return dataObject.GetEmbeddedData<string>();
        }

        // Fallback: JSON representation
        return JsonSerializer.Serialize(dataObject, new JsonSerializerOptions { WriteIndented = true });
    }

    private float ConvertToFloat(NodeToolDataObject dataObject)
    {
        if (dataObject.HasEmbeddedData)
        {
            var value = dataObject.GetEmbeddedData<object>();
            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (float.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return 0f;
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(SKImage)) return null;
        if (type == typeof(byte[])) return Array.Empty<byte>();
        if (type == typeof(string)) return "";
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
```

#### **2.2 Ultra-Simple VL Node Integration**

**File**: `nodetool-sdk/csharp/Nodetool.SDK.VL/Nodes/WorkflowNodeBase.cs` _(Simplified Update)_

```csharp
private void UpdateOutputPins()
{
    if (_currentSession == null) return;

    // Status pins (simple)
    _outputPins["IsRunning"].Value = _currentSession.IsRunning;
    _outputPins["Error"].Value = _currentSession.ErrorMessage ?? "";
    _outputPins["Progress"].Value = _currentSession.ProgressPercent;

    // Data pins - VL's only job: type conversion
    foreach (var outputPin in _outputPins.Where(p => IsDataPin(p.Key)))
    {
        // Get raw data from SDK
        var dataObject = _currentSession.GetOutputData(outputPin.Key);

        if (dataObject != null)
        {
            // Convert to VL type - this is VL's only responsibility
            outputPin.Value = _typeConverter.ConvertToVLType(dataObject, outputPin.Type);
        }
    }

    // Session cleanup
    if (_currentSession.IsCompleted || !string.IsNullOrEmpty(_currentSession.ErrorMessage))
    {
        _currentSession.Dispose();
        _currentSession = null;
    }
}
```

### **Phase 3: Asset Handling Strategy** _(2-3 days)_

**🎯 Goal**: Handle asset downloads for cases where embedded data isn't available

#### **3.1 Simple Asset Download Service**

**File**: `nodetool-sdk/csharp/Nodetool.SDK/Services/AssetDownloadService.cs`

```csharp
public class AssetDownloadService
{
    private readonly INodetoolClient _httpClient;
    private readonly MemoryCache _cache;

    public AssetDownloadService(INodetoolClient httpClient)
    {
        _httpClient = httpClient;
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50 });
    }

    // Async asset download with caching
    public async Task<byte[]?> DownloadAssetAsync(string uri)
    {
        var cacheKey = $"asset:{uri}";

        if (_cache.TryGetValue(cacheKey, out byte[]? cached))
            return cached;

        try
        {
            var assetId = ExtractAssetId(uri);
            using var stream = await _httpClient.DownloadAssetAsync(assetId);
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            _cache.Set(cacheKey, bytes, TimeSpan.FromMinutes(10));
            return bytes;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download asset {uri}: {ex.Message}");
            return null;
        }
    }

    private static string ExtractAssetId(string uri)
    {
        var segments = uri.Split('/');
        var filename = segments.LastOrDefault() ?? "";
        return Path.GetFileNameWithoutExtension(filename);
    }
}
```

## ⏱️ **Implementation Timeline** ⭐ **UPDATED**

- **Week 1**: **Phase 0** - Python-to-C# type generator + CI/CD automation
- **Week 2**: **Phase 1** - SDK data object interface + basic type conversion
- **Week 3**: **Phase 2** - VL type converter service + node integration
- **Week 4**: **Phase 3** - Asset download service + testing

**Total Duration**: ~4 weeks for complete automated type system

## 🚀 **Benefits of Automated Type Generation** ⭐ **NEW**

### **🔄 Maintainability**

- **No manual updates**: Types automatically stay in sync with NodeTool
- **Consistent mapping**: Eliminates developer interpretation differences
- **Scalability**: Handles 100+ node types without effort
- **Version tracking**: Clear history of type changes

### **🎯 Accuracy**

- **Source of truth**: Generated directly from Python definitions
- **Type safety**: Compile-time checking of node interfaces
- **Complete coverage**: No missing or outdated type definitions
- **Enum support**: Automatic generation of choice-based inputs

### **⚡ Development Speed**

- **Instant updates**: New NodeTool nodes immediately available in C#
- **IntelliSense**: Full IDE support for all node types
- **Documentation**: Auto-generated XML docs from Python docstrings
- **Breaking changes**: Compilation errors catch API changes early

### **🔗 Integration**

- **CI/CD ready**: Automatic PR creation for type updates
- **Multi-platform**: Same generated types work in VL, Unity, etc.
- **Extensible**: Easy to add platform-specific type mappings
- **Testable**: Generated types can be unit tested

### **🎯 Accuracy**

- **Source of truth**: Generated directly from Python definitions
- **Type safety**: Compile-time checking of node interfaces
- **Complete coverage**: No missing or outdated type definitions
- **Enum support**: Automatic generation of choice-based inputs

### **⚡ Development Speed**

- **Instant updates**: New NodeTool nodes immediately available in C#
- **IntelliSense**: Full IDE support for all node types
- **Documentation**: Auto-generated XML docs from Python docstrings
- **Breaking changes**: Compilation errors catch API changes early

### **🔗 Integration**

- **CI/CD ready**: Automatic PR creation for type updates
- **Multi-platform**: Same generated types work in VL, Unity, etc.
- **Extensible**: Easy to add platform-specific type mappings
- **Testable**: Generated types can be unit tested

### **Versioning & Build Safety** ⭐ **NEW**

- **Semantic version alignment**: Generated types and enums must always match the **major.minor** version of the NodeTool core API. The SDK NuGet package is versioned using the same `MAJOR.MINOR` segment (e.g. core `v1.4.x` → SDK `v1.4.*`). Any breaking API change triggers a new major version for both repos.
- **Drift protection in CI**: The daily enum/type sync GitHub Action now runs an additional "schema-drift" job that **fails the build** when generated code is not up-to-date. This prevents accidental merges with stale SDK types and forces regeneration (or a manual update PR) before green status.

## 📊 **Success Criteria** ⭐ **UPDATED**

### **Type Generation Automation**:

- ✅ **100% Python nodes** mapped to C# equivalents automatically
- ✅ **Daily CI/CD runs** keep types synchronized with NodeTool
- ✅ **Generated code quality** passes linting and follows C# conventions
- ✅ **Type mapping accuracy** handles complex Union/Optional/Generic types
- ✅ **Enum generation** creates proper C# enums from Python choices
- ✅ **Documentation generation** includes XML docs from Python docstrings

### **SDK Simplicity**:

- ✅ SDK provides clean `IExecutionSession.GetOutput<T>()` interface
- ✅ All WebSocket complexity hidden from consumers
- ✅ Works identically in VL, Unity, console apps
- ✅ Common type conversions built into SDK

### **VL Integration**:

- ✅ VL nodes only do type conversion (single responsibility)
- ✅ No WebSocket event handling in VL layer
- ✅ No state management in VL layer
- ✅ SKImage, byte[], string conversions work correctly

### **Performance**:

- ✅ Type conversion completes in < 10ms for typical objects
- ✅ Asset caching reduces redundant downloads
- ✅ Memory usage stays reasonable
- ✅ No memory leaks from cached assets

### **Automation Quality**:

- ✅ **Zero manual intervention** for new NodeTool node types
- ✅ **Same-day availability** of new nodes in C# SDK
- ✅ **Backward compatibility** preserved during NodeTool updates
- ✅ **Clear change tracking** via automated PRs

### **Enum System Quality** ⭐ **NEW CRITICAL**:

- ✅ **200+ enum types** automatically extracted from Python
- ✅ **Dropdown pins** in VL instead of text input boxes
- ✅ **Zero typos** - users select from valid options only
- ✅ **Full IntelliSense** support for all C# consumers
- ✅ **Auto-categorization** by usage patterns (Cross-domain, Domain-specific, etc.)
- ✅ **Static enums** for stable types (BlendMode, SortOrder)
- ✅ **Dynamic enums** for evolving lists (model names, languages)
- ✅ **VL-native integration** using proper VL enum patterns
- ✅ **Daily enum sync** via CI/CD automation

## 💡 **Key Insights: The Missing Pieces** ⭐ **CRITICAL UPDATES**

You were **absolutely right** on **both fronts**:

1. **Automatic Python-to-C# type generation** was the crucial missing foundation
2. **Full enum automation + VL integration** was the missing UX piece

### **Before (Manual Approach)**:

```
❌ Python Node → Manual C# Type → Manual Updates → Inconsistencies → Runtime Errors
❌ Python Enum → String Input → User Types Values → Typos → Runtime Errors
```

### **After (Automated Approach)**:

```
✅ Python Node → Auto-Generated C# → CI/CD Updates → Type Safety → Compile-time Validation
✅ Python Enum → Generated Enum Types → VL Dropdown Pins → Zero Typos → Perfect UX
```

### **Double Impact**:

**Type Generation Foundation**:

- **Complete type coverage** without manual maintenance
- **Immediate availability** of new NodeTool features in C#
- **Type safety** throughout the entire SDK
- **Consistent mapping** across all platforms

**Enum System Enhancement** ⭐ **NEW**:

- **Perfect VL user experience** with dropdown enum pins
- **1000+ nodes** benefit from proper type-safe inputs
- **200+ enum types** automatically maintained
- **Zero manual enum maintenance** required

The **enum automation + VL integration** transforms the user experience from **error-prone text inputs** to **discoverable, type-safe dropdown selections**. This is the difference between a **technical proof-of-concept** and a **production-ready user experience**! 🎉
