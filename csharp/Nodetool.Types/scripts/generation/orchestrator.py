"""
Orchestration of the C# type and node generation process.
"""
import os
import shutil
from typing import List, Dict

try:
    from nodetool.metadata.types import BaseType
    from nodetool.workflows.base_node import BaseNode
    from nodetool.packages.registry import discover_node_packages
except ImportError:
    class BaseType: pass
    class BaseNode: pass
    def discover_node_packages(): return []

from .utils import get_package_name
from .codegen import generate_class_source, generate_node_class_source
from .discovery import discover_all_base_types, discover_all_base_nodes

def generate_types_for_source(source_name: str, classes: List[type[BaseType]], output_dir: str) -> tuple[int, int]:
    """Generate C# classes for a specific source (core or package)."""
    generated = 0
    errors = 0
    
    # Get clean names
    pkg_name = get_package_name(source_name)  # e.g., 'nodetool-huggingface' -> 'Huggingface'
    
    # Create source-specific namespace and directory using pretty name
    namespace = f"Nodetool.Types.{pkg_name}"  # e.g., Nodetool.Types.Huggingface

    # Support nested namespaces like "Lib.Audio" ➜ folder "Lib/Audio"
    dir_parts = pkg_name.split('.')  # ["Lib", "Audio"] or ["Huggingface"]
    source_dir = os.path.join(output_dir, "Types", *dir_parts)  # e.g., Types/Lib/Audio or Types/Huggingface
    
    os.makedirs(source_dir, exist_ok=True)
    
    # Generate individual type files
    for cls in classes:
        try:
            src = generate_class_source(cls, namespace)
            output_file = os.path.join(source_dir, f"{cls.__name__}.cs")
            
            with open(output_file, "w", encoding="utf-8") as f:
                f.write(src)
            
            generated += 1
            
        except Exception as e:
            print(f"[ERROR] Error generating {cls.__name__} from {pkg_name}: {e}")
            errors += 1
    
    # Generate summary file at package level
    if generated > 0:
        # Place the summary next to generated files, preserving nested structure
        summary_path = os.path.join(output_dir, "Types", *dir_parts) + ".cs"
        os.makedirs(os.path.dirname(summary_path), exist_ok=True)
        with open(summary_path, "w", encoding="utf-8") as f:
            f.write(f"""//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NodeTool SDK Type Generator.
// </auto-generated>
//------------------------------------------------------------------------------

using MessagePack;
using System;
using System.Collections.Generic;

namespace Nodetool.Types;

/// <summary>
/// Types from {pkg_name} package.
/// </summary>
public static class {pkg_name}
{{
    // Type definitions
{chr(10).join(f'    [MessagePackObject] public class {cls.__name__} {{ ... }}' for cls in classes)}

    // Register types with MessagePack
    internal static void RegisterTypes()
    {{
{chr(10).join(f'        NodeToolTypes.KnownTypes.Add(typeof({cls.__name__}));' for cls in classes)}
    }}
}}
""")
    
    return generated, errors

def generate_nodes_for_source(source_name: str, nodes: List[type[BaseNode]], output_dir: str) -> tuple[int, int]:
    """Generate C# classes for nodes from a specific source."""
    generated = 0
    errors = 0
    
    # Get clean names
    pkg_name = get_package_name(source_name)  # e.g., 'nodetool-huggingface' -> 'Huggingface'
    
    # Create namespace-specific directory using pretty name
    dir_parts = pkg_name.split('.')  # Support nested namespaces
    source_dir = os.path.join(output_dir, "Nodes", *dir_parts)  # e.g., Nodes/Lib/Audio
    
    os.makedirs(source_dir, exist_ok=True)
    
    # Generate individual node files
    for node_cls in nodes:
        try:
            src = generate_node_class_source(node_cls, source_name)
            filename = f"{node_cls.__name__}.cs"
            filepath = os.path.join(source_dir, filename)
            
            with open(filepath, "w", encoding="utf-8") as f:
                f.write(src)
            
            generated += 1
            
        except Exception as e:
            print(f"[ERROR] Error generating {node_cls.__name__}: {e}")
            errors += 1
    
    # Generate summary file at package level
    if generated > 0:
        summary_path = os.path.join(output_dir, "Nodes", *dir_parts) + ".cs"
        os.makedirs(os.path.dirname(summary_path), exist_ok=True)
        with open(summary_path, "w", encoding="utf-8") as f:
            f.write(f"""//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NodeTool SDK Type Generator.
// </auto-generated>
//------------------------------------------------------------------------------

using MessagePack;
using System;
using System.Collections.Generic;

namespace Nodetool.Nodes;

/// <summary>
/// Nodes from {pkg_name} package.
/// </summary>
public static class {pkg_name}
{{
    // Node definitions
{chr(10).join(f'    [MessagePackObject] public class {node_cls.__name__} {{ ... }}' for node_cls in nodes)}

    // Register types with MessagePack
    internal static void RegisterTypes()
    {{
{chr(10).join(f'        NodeToolTypes.KnownTypes.Add(typeof({node_cls.__name__}));' for node_cls in nodes)}
    }}
}}
""")
    
    return generated, errors

def generate_summary_file(output_dir: str, discovered_packages: List[str]) -> None:
    """Generate the main NodeToolTypes.cs file."""
    # Package names are already in pretty format (e.g., 'Huggingface')
    package_names = sorted(discovered_packages)
    print("\n>>> Generating NodeToolTypes.cs")
    print(f"  Discovered packages: {', '.join(package_names)}")
    
    # Generate registration lines for each package
    registration_lines = []
    for pkg in package_names:
        # Check if Types and/or Nodes directories exist for this package
        has_types = False
        has_nodes = False
        
        if '.' in pkg:
            # For nested namespaces (e.g., Lib.Audio) create nested folders Lib/Audio
            parts = pkg.split('.')
            namespace = pkg  # Keep full namespace for registration

            type_dir = os.path.join(output_dir, "Types", *parts)
            node_dir = os.path.join(output_dir, "Nodes", *parts)
            
            # Ensure directories exist (they may not if only other kind generated)
            os.makedirs(type_dir, exist_ok=True)
            os.makedirs(node_dir, exist_ok=True)
        else:
            # Regular packages
            folder_path = pkg
            namespace = pkg
            type_dir = os.path.join(output_dir, "Types", pkg)
            node_dir = os.path.join(output_dir, "Nodes", pkg)
        
        # Check if directories exist and have files
        has_types = os.path.exists(type_dir) and any(f.endswith('.cs') for f in os.listdir(type_dir)) if os.path.exists(type_dir) else False
        has_nodes = os.path.exists(node_dir) and any(f.endswith('.cs') for f in os.listdir(node_dir)) if os.path.exists(node_dir) else False
        
        print(f"  Package {namespace}:")
        print(f"    Types: {'[x]' if has_types else '[ ]'} ({type_dir})")
        print(f"    Nodes: {'[x]' if has_nodes else '[ ]'} ({node_dir})")
        
        # Only register what exists
        if has_types:
            registration_lines.append(f"            Types.{namespace}.RegisterTypes();")
        if has_nodes:
            registration_lines.append(f"            Nodes.{namespace}.RegisterTypes();")
    
    print(f"  Total packages registered: {len(package_names)}")
    
    # Generate example lines
    example_lines = []
    if "Core" in package_names:
        example_lines.append("    var audio = new Core.AudioRef();")
    if "Huggingface" in package_names:
        example_lines.append("    var classifier = new Huggingface.AudioClassifier();")
    if "Lib.Audio" in package_names:
        example_lines.append("    var noise = new Lib.Audio.WhiteNoise();")
    if not example_lines:
        example_lines.append("    // var myType = new PackageName.TypeName();")
    
    summary_path = os.path.join(output_dir, "NodeToolTypes.cs")
    with open(summary_path, "w", encoding="utf-8") as f:
        f.write(f"""//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NodeTool SDK Type Generator.
// </auto-generated>
//------------------------------------------------------------------------------

using MessagePack;
using System;
using System.Collections.Generic;

namespace Nodetool;

/// <summary>
/// Configures MessagePack serialization for all NodeTool types.
/// 
/// Usage:
/// 1. Initialize MessagePack:
///    NodeToolTypes.Initialize();
///    
/// 2. Use types directly:
///    using Nodetool.Types;
///    using Nodetool.Nodes;
///    
{chr(10).join(example_lines)}
///    
/// 3. MessagePack Serialization:
///    var data = MessagePackSerializer.Serialize(obj);
///    var obj = MessagePackSerializer.Deserialize<T>(data);
/// </summary>
public static class NodeToolTypes
{{
    private static bool isInitialized = false;
    private static readonly object initLock = new object();
    internal static readonly List<Type> KnownTypes = new();

    public static void Initialize()
    {{
        if (isInitialized) return;

        lock (initLock)
        {{
            if (isInitialized) return;

            // Register types from all packages
{chr(10).join(registration_lines)}

            // Configure MessagePack
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.StandardResolver.Instance,
                MessagePack.Resolvers.DynamicObjectResolver.Instance
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;

            // Register all types with MessagePack
            foreach (var type in KnownTypes)
            {{
                MessagePackSerializer.SerializerCache.Get(type, options);
            }}

            isInitialized = true;
        }}
    }}
}}
""")

def cleanup_directory(output_dir: str) -> None:
    """Clean up the output directory before generating new files."""
    print("\n>>> Cleaning up output directory...")
    
    if os.path.exists(output_dir):
        # Remove main directories
        for dir_name in ["Types", "Nodes"]:
            dir_path = os.path.join(output_dir, dir_name)
            if os.path.exists(dir_path):
                try:
                    shutil.rmtree(dir_path)
                    print(f"  Removed directory: {dir_name}/")
                except Exception as e:
                    print(f"  [ERROR] Could not remove directory {dir_name}: {e}")
        
        # Remove main summary file
        summary_file = os.path.join(output_dir, "NodeToolTypes.cs")
        if os.path.exists(summary_file):
            try:
                os.remove(summary_file)
                print("  Removed: NodeToolTypes.cs")
            except Exception as e:
                print(f"  [ERROR] Could not remove NodeToolTypes.cs: {e}")
    
    # Create output directory if it doesn't exist
    os.makedirs(output_dir, exist_ok=True)
    print("Cleanup completed.")

def generate_all_types(output_dir: str, namespace: str = "Nodetool.Types") -> None:
    """Generate C# classes for all BaseType subclasses from all sources."""
    print("=== NodeTool SDK Complete Type Generator ===")
    print(f"Output: {output_dir}")
    print(f"Namespace: {namespace}")
    print()
    
    # Discover all types
    all_types = discover_all_base_types()
    
    # Create output directory
    os.makedirs(output_dir, exist_ok=True)
    
    # Track statistics
    total_generated = 0
    total_errors = 0
    
    # Generate types for each source
    for source_name, classes in all_types.items():
        if classes:
            print(f"\n>>> Generating types from {source_name}...")
            generated, errors = generate_types_for_source(source_name, classes, output_dir)
            total_generated += generated
            total_errors += errors
    
    print(f"\n=== Type Generation Summary ===")
    print(f"  Generated: {total_generated}")
    print(f"  Errors: {total_errors}")
    print(f"  Output: {output_dir}")
    
    if total_errors == 0:
        print("\nType generation completed successfully!")
    else:
        print(f"\nType generation completed with {total_errors} errors.")

def generate_all_nodes(output_dir: str, namespace: str = "Nodetool.Types") -> None:
    """Generate C# classes for all BaseNode subclasses from all sources."""
    print("=== NodeTool SDK Complete Node Generator ===")
    print(f"Output: {output_dir}")
    print(f"Namespace: {namespace}")
    print()
    
    # Discover all nodes
    all_nodes = discover_all_base_nodes()
    
    # Create output directory
    os.makedirs(output_dir, exist_ok=True)
    
    # Track statistics
    total_generated = 0
    total_errors = 0
    
    # Generate nodes for each source
    for source_name, nodes in all_nodes.items():
        if nodes:
            print(f"\n>>> Generating nodes from {source_name}...")
            
            # Create source-specific directory (support nested namespace parts)
            dir_parts = [part.capitalize() for part in source_name.split('.')]
            source_dir = os.path.join(output_dir, "Nodes", *dir_parts)
            os.makedirs(source_dir, exist_ok=True)
            
            # Generate each node class
            for node_cls in nodes:
                try:
                    src = generate_node_class_source(node_cls, source_name)
                    filename = f"{node_cls.__name__}.cs"
                    filepath = os.path.join(source_dir, filename)
                    
                    # Create subdirectories if needed
                    os.makedirs(os.path.dirname(filepath), exist_ok=True)
                    
                    with open(filepath, "w", encoding="utf-8") as f:
                        f.write(src)
                    
                    total_generated += 1
                    
                except Exception as e:
                    print(f"[ERROR] Error generating {node_cls.__name__}: {e}")
                    total_errors += 1
    
    print(f"\nNode Generation Summary:")
    print(f"  Generated: {total_generated}")
    print(f"  Errors: {total_errors}")
    print(f"  Output: {output_dir}")
    
    if total_errors == 0:
        print("\nNode generation completed successfully!")
    else:
        print(f"\nNode generation completed with {total_errors} errors")

def generate_all_types_and_nodes(output_dir: str, namespace: str = "Nodetool.Types") -> None:
    """Generate C# classes for all discovered BaseType and BaseNode subclasses."""
    print("=== NodeTool SDK Type & Node Generator ===")
    print(f"Output directory: {output_dir}")
    
    # Clean up output directory first
    cleanup_directory(output_dir)
    
    # Discover all packages first
    print(">>> Discovering packages...")
    try:
        # First get packages from discover_node_packages
        packages = discover_node_packages()
        discovered_packages = set(["Core"])  # Start with Core
        
        # Add packages from discover_node_packages and Nodes directory
        for p in packages:
            discovered_packages.add(get_package_name(p.name))
            
        nodes_dir = os.path.join(output_dir, "Nodes")
        if os.path.exists(nodes_dir):
            for item in os.listdir(nodes_dir):
                if os.path.isdir(os.path.join(nodes_dir, item)):
                    discovered_packages.add(get_package_name(item))
        
        discovered_packages = sorted(list(discovered_packages))
        print(f"Found packages: {', '.join(discovered_packages)}")
    except Exception as e:
        print(f"[WARNING] Error discovering packages: {e}")
        discovered_packages = ["Core"]
    
    # Generate types and nodes
    generate_all_types(output_dir, namespace)
    generate_all_nodes(output_dir, namespace)
    
    # Generate summary file
    generate_summary_file(output_dir, discovered_packages) 