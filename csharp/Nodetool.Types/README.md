# NodeTool.Types

This project contains C# types generated from NodeTool's Python `BaseType` subclasses and `BaseNode` subclasses. The types are designed to work with MessagePack serialization for efficient WebSocket communication.

## Architecture

### **Centralized Type Generation (No Duplication)**

NodeTool uses a **centralized type generation approach** where the SDK generates all types from all sources in one place:

```
nodetool-core/                    # Core types and CLI
    └── src/nodetool/
        ├── metadata/types.py     # BaseType base class
        └── csharp_codegen.py     # Type generation logic

nodetool-huggingface/             # Example package
    ├── pyproject.toml            # Package metadata
    ├── src/nodetool/nodes/       # BaseNode subclasses
    └── src/nodetool/types/       # Package-specific BaseType subclasses

nodetool-sdk/csharp/              # C# SDK
    ├── Nodetool.Types/           # This project - ALL types in one place
    │   ├── generate-types.ps1    # 🆕 Main entry point
    │   ├── scripts/              # 🆕 Generation scripts
    │   │   ├── generate-all-types.py
    │   │   └── generate-all-types.ps1
    │   └── *.cs                  # All types from all sources
    └── Nodetool.SDK/             # Main SDK project
        └── Types/                # Type registry and lookup services
```

### **Type Generation Flow**

1. **Package Development**: Each package defines `BaseType` and `BaseNode` subclasses
2. **Centralized Generation**: SDK discovers and generates ALL types from ALL sources
3. **Namespace Organization**: Types are organized by source (core, huggingface, base, etc.)
4. **Runtime Discovery**: The SDK discovers and registers all types at runtime

## Type Generation

### **Complete Type Generation (Recommended)**

Generate ALL types from ALL sources in one command:

```powershell
# In nodetool-sdk/csharp/Nodetool.Types/
.\generate-types.ps1 -Clean
```

**What this does:**

1. **Discovers** all installed nodetool packages
2. **Generates** types from nodetool-core
3. **Generates** types from each package
4. **Organizes** by namespace (e.g., `Nodetool.Types.core`, `Nodetool.Types.huggingface`)
5. **Avoids duplication** - each type is generated once

### **Manual Generation (Legacy)**

You can still generate types directly from nodetool-core:

```powershell
# Generate types in current directory
.\generate-types.ps1

# Generate types with custom output directory
.\generate-types.ps1 -OutputDir "generated"

# Clean existing types and regenerate
.\generate-types.ps1 -Clean
```

### **Package-Level Generation (For Package Development)**

Individual packages can generate their own types for testing:

```bash
# In your package directory (e.g., nodetool-huggingface/)
nodetool package codegen-csharp --output-dir csharp_types
nodetool package codegen-csharp-nodes --output-dir csharp_nodes

# Or generate both at once
nodetool package codegen-all
```

**Note:** This is mainly for package development/testing. The SDK uses the centralized approach.

## Generated Types

The generator discovers all `BaseType` and `BaseNode` subclasses and generates corresponding C# classes with:

- **MessagePack attributes** for serialization
- **Proper type mapping** from Python to C#
- **Default values** where applicable
- **Source-specific namespaces** for organization

### **Example Generated Types**

```csharp
// Core types from nodetool-core
namespace Nodetool.Types.core;

[MessagePackObject]
public class ImageAsset
{
    [Key(0)]
    public string id { get; set; } = "";

    [Key(1)]
    public string filename { get; set; } = "";
}

// Package-specific types from nodetool-huggingface
namespace Nodetool.Types.huggingface;

[MessagePackObject]
public class HuggingFaceModel
{
    [Key(0)]
    public string repo_id { get; set; } = "";

    [Key(1)]
    public string type { get; set; } = "";
}
```

## Integration with SDK

The `Nodetool.SDK` project references this types project and uses the generated types for:

- **WebSocket message serialization/deserialization**
- **Type registry and lookup services**
- **Asset handling and model configuration**
- **Workflow execution data structures**

## Package Development Workflow

### **1. Package Setup**

```bash
# Initialize a new package
nodetool package init

# Define your BaseType and BaseNode subclasses in src/nodetool/
```

### **2. Package Testing (Optional)**

```bash
# Generate C# types for your package (for testing)
nodetool package codegen-all

# This creates:
# - csharp_types/ (BaseType subclasses)
# - csharp_nodes/ (BaseNode subclasses)
```

### **3. Package Installation**

```bash
# Install your package
pip install -e .

# Or publish to registry
nodetool package publish
```

### **4. SDK Integration**

```powershell
# In nodetool-sdk, generate ALL types from ALL sources
.\generate-types.ps1 -Clean

# Build the SDK
dotnet build
```

## Key Benefits

### **No Code Duplication**

- Each type is generated **once** in the SDK
- No duplicate `ImageAsset.cs` files across packages
- Clean, maintainable codebase

### **Automatic Discovery**

- SDK automatically discovers all installed packages
- New packages automatically contribute types
- No manual configuration needed

### **Namespace Organization**

- Core types: `Nodetool.Types.core.*`
- Package types: `Nodetool.Types.{package_name}.*`
- Clear separation and organization

### **Flexible Output**

- Can specify custom output directories
- Can generate types for specific sources only
- Supports both PowerShell and Python scripts

## Troubleshooting

### **Common Issues**

1. **Python not found**: Ensure Python is installed and in PATH
2. **Package not found**: Check that packages are properly installed
3. **Import errors**: Make sure nodetool-core is properly installed
4. **Compilation errors**: Some generated types may have issues - check the import script for fixes

### **Fixing Generated Types**

The `import-types.ps1` script handles common issues:

- **Reserved keyword conflicts** (e.g., `object` property names)
- **Missing type dependencies** (e.g., `ColumnDef` for `RecordType`)
- **Namespace and using statement fixes**

Run the import script after generation to apply fixes:

```powershell
.\import-types.ps1
```

## Type Registry Integration

The generated types are automatically discovered by the `NodeToolTypeRegistry` in the SDK, which provides:

- **Runtime type discovery** from loaded assemblies
- **MessagePack type registration** for serialization
- **Type lookup services** for the SDK
- **Enum discovery** for UI integration
- **Source-specific type organization**

## Future Enhancements

- **Incremental updates** for changed types only
- **Type validation** and schema checking
- **IDE integration** for type discovery and IntelliSense
- **Automatic generation** on package installation
- **Type versioning** and compatibility checking
