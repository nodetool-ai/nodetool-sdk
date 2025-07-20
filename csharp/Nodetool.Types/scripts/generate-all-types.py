#!/usr/bin/env python3
"""
NodeTool SDK Complete Type Generator
Generates C# types from nodetool-core AND all installed packages in one centralized location.
This avoids code duplication by generating everything in the SDK.
"""

import os
import sys
import shutil
import importlib
import inspect
import pkgutil
from pathlib import Path
from typing import Any, get_origin, get_args, List, Dict, Set
import argparse

# Import nodetool modules
try:
    from nodetool.metadata.types import BaseType
    from nodetool.workflows.base_node import BaseNode
    from nodetool.packages.discovery import walk_source_modules
    from nodetool.packages.registry import discover_node_packages
except ImportError as e:
    print(f"[ERROR] Error importing nodetool modules: {e}")
    print("   Make sure nodetool-core is installed: pip install -e ../../../nodetool-core")
    sys.exit(1)

# Type mapping from Python to C#
_PRIMITIVE_TYPE_MAP = {
    str: "string",
    int: "int", 
    float: "double",
    bool: "bool",
    bytes: "byte[]",
    type(None): "object",
    object: "object",
}

def python_type_to_csharp(tp: Any) -> str:
    """Convert a Python type annotation to a C# type string."""
    origin = get_origin(tp)
    if origin is None:
        if isinstance(tp, type):
            if tp in _PRIMITIVE_TYPE_MAP:
                return _PRIMITIVE_TYPE_MAP[tp]
            # Handle BaseType subclasses
            try:
                if issubclass(tp, BaseType):
                    return f"Nodetool.Types.{tp.__name__}"
            except TypeError:
                pass
        # typing.Any or unknown
        if tp is Any:
            return "object"
        # typing.Literal values
        if str(tp).startswith("typing.Literal"):
            args = get_args(tp)
            if args:
                return python_type_to_csharp(type(args[0]))
        return "object"

    if origin is list:
        args = get_args(tp)
        inner = python_type_to_csharp(args[0]) if args else "object"
        return f"List<{inner}>"
    if origin is dict:
        args = get_args(tp)
        key = python_type_to_csharp(args[0]) if args else "object"
        val = python_type_to_csharp(args[1]) if len(args) > 1 else "object"
        return f"Dictionary<{key}, {val}>"
    if origin is set:
        args = get_args(tp)
        inner = python_type_to_csharp(args[0]) if args else "object"
        return f"HashSet<{inner}>"
    if origin is tuple:
        args = get_args(tp)
        if len(args) == 2 and args[1] is ...:
            inner = python_type_to_csharp(args[0])
            return f"List<{inner}>"
        else:
            inner = python_type_to_csharp(args[0]) if args else "object"
            return f"List<{inner}>"
    if origin is type(None):
        return "object"
    if str(origin).startswith("typing.Union"):
        args = [a for a in get_args(tp) if a is not type(None)]
        if len(args) == 1:
            return python_type_to_csharp(args[0]) + "?"
        return "object"

    return "object"

def default_value_to_csharp(value: Any) -> str | None:
    """Convert a Python default value to C# default value string."""
    if value is None:
        return "null"
    if isinstance(value, str):
        return f'"{value}"'
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, list):
        return "new List<object>()"
    if isinstance(value, dict):
        return "new Dictionary<string, object>()"
    if isinstance(value, BaseType):
        return f"new Nodetool.Types.{type(value).__name__}()"
    return None

def get_package_name(raw_name: str) -> str:
    """Convert raw package name to proper C# namespace name.
    Examples:
        'nodetool-base' -> 'Base'
        'nodetool-fal' -> 'Fal'
        'nodetool_huggingface' -> 'Huggingface'
        'nodetool-huggingface' -> 'Huggingface'
        'core' -> 'Core'
    """
    # Remove nodetool prefix and any separators
    name = raw_name.lower()
    if name.startswith("nodetool-") or name.startswith("nodetool_"):
        name = name[9:]
    
    # Convert to PascalCase
    parts = name.replace("-", "_").split("_")
    return "".join(part.capitalize() for part in parts)

def get_folder_name(raw_name: str) -> str:
    """Get clean folder name without any prefixes.
    Examples:
        'nodetool-base' -> 'Base'
        'nodetool_huggingface' -> 'Huggingface'
        'core' -> 'Core'
    """
    return get_package_name(raw_name)

def generate_class_source(cls: type[BaseType], namespace: str) -> str:
    """Generate C# class source code for a BaseType subclass."""
    lines = [
        "using MessagePack;",
        "using System.Collections.Generic;", 
        "",
        f"namespace {namespace};",
        "",
        "[MessagePackObject]",
        f"public class {cls.__name__}",
        "{"
    ]
    
    index = 0
    for name, field in cls.model_fields.items():
        csharp_type = python_type_to_csharp(field.annotation)
        default = default_value_to_csharp(field.default)
        lines.append(f"    [Key({index})]")
        if default is not None:
            lines.append(f"    public {csharp_type} {name} {{ get; set; }} = {default};")
        else:
            lines.append(f"    public {csharp_type} {name} {{ get; set; }}")
        index += 1
    
    lines.append("}")
    return "\n".join(lines) + "\n"

def discover_all_base_types() -> Dict[str, List[type[BaseType]]]:
    """Discover all BaseType subclasses from nodetool-core and all packages."""
    all_types = {}
    
    # 1. Discover types from nodetool-core
    print(">>> Discovering types from nodetool-core...")
    core_types = []
    import nodetool
    
    print(f"  Path: {nodetool.__path__}")
    print(f"  Name: {nodetool.__name__}")
    
    # Use pkgutil to walk through nodetool modules
    module_count = 0
    for _, module_name, _ in pkgutil.walk_packages(nodetool.__path__, nodetool.__name__ + "."):
        module_count += 1
        try:
            module = importlib.import_module(module_name)
            class_count = 0
            for _, obj in inspect.getmembers(module, inspect.isclass):
                try:
                    if (inspect.isclass(obj) and
                        issubclass(obj, BaseType) and obj is not BaseType and
                        obj.__module__.startswith("nodetool")):
                        core_types.append(obj)
                        class_count += 1
                except Exception as e:
                    continue
            if class_count > 0:
                print(f"    [OK] {module_name}: {class_count} BaseType subclasses")
        except Exception as e:
            print(f"    [ERROR] Could not import {module_name}: {e}")
            continue
    
    print(f"  Scanned {module_count} modules")
    
    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_types}
    all_types["core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"Found {len(all_types['core'])} unique types from nodetool-core")
    
    # List all found types
    for i, cls in enumerate(all_types["core"][:10]):  # Show first 10
        print(f"    {i+1:3d}. {cls.__name__} (from {cls.__module__})")
    if len(all_types["core"]) > 10:
        print(f"    ... and {len(all_types['core']) - 10} more")
    
    # 2. Discover types from installed packages
    print(">>> Discovering types from installed packages...")
    try:
        packages = discover_node_packages()
        print(f"  Found {len(packages)} packages:")
        for p in packages:
            print(f"    - {p.name}: {p.source_folder}")
        
        for package in packages:
            package_types = []
            package_name = package.name.replace("-", "_")
            
            print(f"\n  Processing package: {package.name}")
            print(f"    Source folder: {package.source_folder}")
            print(f"    Has source folder: {bool(package.source_folder and os.path.exists(package.source_folder))}")
            
            if package.source_folder and os.path.exists(package.source_folder):
                # Package has source folder (development install)
                package_src = os.path.join(package.source_folder, "src")
                print(f"    Package src: {package_src}")
                print(f"    Package src exists: {os.path.exists(package_src)}")
                
                if os.path.exists(package_src):
                    sys.path.insert(0, package_src)
                    print(f"    Added {package_src} to Python path")
                    
                    # Walk through package modules
                    try:
                        package_module = importlib.import_module("nodetool")
                        print(f"    Successfully imported nodetool from {package_src}")
                        print(f"    Package module path: {package_module.__path__}")
                        
                        module_count = 0
                        for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                            module_count += 1
                            try:
                                module = importlib.import_module(module_name)
                                class_count = 0
                                for _, obj in inspect.getmembers(module, inspect.isclass):
                                    try:
                                        if (inspect.isclass(obj) and
                                            issubclass(obj, BaseType) and obj is not BaseType and
                                            obj.__module__.startswith("nodetool")):
                                            package_types.append(obj)
                                            class_count += 1
                                    except Exception:
                                        continue
                                if class_count > 0:
                                    print(f"      [OK] {module_name}: {class_count} BaseType subclasses")
                            except Exception as e:
                                print(f"      [ERROR] Could not import {module_name}: {e}")
                                continue
                        
                        print(f"    Scanned {module_count} modules in {package.name}")
                        
                    except Exception as e:
                        print(f"    [ERROR] Error processing package {package.name}: {e}")
                    
                    # Remove package from Python path
                    if package_src in sys.path:
                        sys.path.remove(package_src)
                        print(f"    Removed {package_src} from Python path")
            else:
                # Package is installed in environment (no source folder)
                print(f"    Installed package (no source folder)")
                try:
                    # Try to import the package's nodetool module
                    package_module_name = f"nodetool.{package.name.replace('-', '_')}"
                    print(f"    Trying to import: {package_module_name}")
                    package_module = importlib.import_module(package_module_name)
                    print(f"    Successfully imported {package_module_name}")
                    print(f"    Package module path: {package_module.__path__}")
                    
                    # Walk through the package's modules
                    module_count = 0
                    for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                        module_count += 1
                        try:
                            module = importlib.import_module(module_name)
                            class_count = 0
                            for _, obj in inspect.getmembers(module, inspect.isclass):
                                try:
                                    if (inspect.isclass(obj) and
                                        issubclass(obj, BaseType) and obj is not BaseType and
                                        obj.__module__.startswith("nodetool")):
                                        package_types.append(obj)
                                        class_count += 1
                                except Exception:
                                    continue
                            if class_count > 0:
                                print(f"      [OK] {module_name}: {class_count} BaseType subclasses")
                        except Exception as e:
                            print(f"      [ERROR] Could not import {module_name}: {e}")
                            continue
                    
                    print(f"    Scanned {module_count} modules in {package.name}")
                    
                except Exception as e:
                    print(f"    [ERROR] Error importing package {package.name}: {e}")
            
            # Remove duplicates and sort
            unique_package = {c.__name__: c for c in package_types}
            if unique_package:
                all_types[package_name] = [unique_package[n] for n in sorted(unique_package.keys())]
                print(f"    Found {len(all_types[package_name])} unique types from {package.name}")
                # List found types
                for i, cls in enumerate(all_types[package_name][:5]):  # Show first 5
                    print(f"      {i+1:3d}. {cls.__name__} (from {cls.__module__})")
                if len(all_types[package_name]) > 5:
                    print(f"      ... and {len(all_types[package_name]) - 5} more")
            else:
                print(f"    No BaseType subclasses found in {package.name}")
                        
    except Exception as e:
        print(f"[ERROR] Error discovering packages: {e}")
        import traceback
        traceback.print_exc()
    
    return all_types

def generate_types_for_source(source_name: str, classes: List[type[BaseType]], output_dir: str) -> tuple[int, int]:
    """Generate C# classes for a specific source (core or package)."""
    generated = 0
    errors = 0
    
    # Get clean names
    pkg_name = get_package_name(source_name)
    folder_name = get_folder_name(source_name)
    
    # Create source-specific namespace and directory
    namespace = f"Nodetool.Types.{pkg_name}"  # e.g., Nodetool.Types.Huggingface
    source_dir = os.path.join(output_dir, "Types", folder_name)  # e.g., Types/Huggingface
    
    os.makedirs(source_dir, exist_ok=True)
    
    # Generate individual type files
    for cls in classes:
        try:
            src = generate_class_source(cls, namespace)
            output_file = os.path.join(source_dir, f"{cls.__name__}.cs")
            
            with open(output_file, "w", encoding="utf-8") as f:
                f.write(src)
            
            print(f"Generated: {cls.__name__}.cs ({pkg_name})")
            generated += 1
            
        except Exception as e:
            print(f"[ERROR] Error generating {cls.__name__} from {pkg_name}: {e}")
            errors += 1
    
    # Generate summary file at package level
    if generated > 0:
        summary_path = os.path.join(output_dir, "Types", f"{folder_name}.cs")
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

def discover_all_base_nodes() -> Dict[str, List[type[BaseNode]]]:
    """Discover all BaseNode subclasses from nodetool-core and all packages."""
    all_nodes = {}
    
    # 1. Discover nodes from nodetool-core
    print(">>> Discovering nodes from nodetool-core...")
    core_nodes = []
    import nodetool
    
    # Handle namespace package - nodetool has multiple paths
    nodetool_paths = nodetool.__path__._path if hasattr(nodetool.__path__, '_path') else [nodetool.__path__]
    print(f"  Looking in nodetool paths: {nodetool_paths}")
    
    for base_path in nodetool_paths:
        nodes_path = os.path.join(base_path, "nodes")
        if os.path.exists(nodes_path):
            print(f"  Found nodes directory: {nodes_path}")
            
            # Add base path to Python path
            sys.path.insert(0, os.path.dirname(base_path))
            
            # Walk through all Python files in the nodes directory and its subdirectories
            for root, _, files in os.walk(nodes_path):
                for file in files:
                    if file.endswith(".py") and not file.startswith("__"):
                        module_path = os.path.join(root, file)
                        # Get relative path from base directory
                        module_name = os.path.relpath(module_path, os.path.dirname(base_path))
                        module_name = module_name.replace(os.sep, ".")[:-3]  # Remove .py extension
                        
                        try:
                            module = importlib.import_module(module_name)
                            class_count = 0
                            for _, obj in inspect.getmembers(module, inspect.isclass):
                                try:
                                    if (inspect.isclass(obj) and
                                        issubclass(obj, BaseNode) and obj is not BaseNode and
                                        obj.__module__.startswith("nodetool") and
                                        hasattr(obj, 'is_visible') and obj.is_visible()):
                                        core_nodes.append(obj)
                                        class_count += 1
                                except Exception:
                                    continue
                            if class_count > 0:
                                print(f"    [OK] {module_name}: {class_count} BaseNode subclasses")
                        except Exception as e:
                            print(f"    [ERROR] Could not import {module_name}: {e}")
                            continue
            
            # Remove base path from Python path
            if os.path.dirname(base_path) in sys.path:
                sys.path.remove(os.path.dirname(base_path))
    
    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_nodes}
    all_nodes["core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"Found {len(all_nodes['core'])} unique nodes from nodetool-core")
    
    # 2. Discover nodes from development packages in workspace
    print(">>> Discovering nodes from development packages...")
    try:
        # Look for development packages in the workspace
        workspace_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
        print(f"  Looking for development packages in workspace: {workspace_root}")
        
        for item in os.listdir(workspace_root):
            if item.startswith("nodetool-") and os.path.isdir(os.path.join(workspace_root, item)):
                package_path = os.path.join(workspace_root, item)
                package_src = os.path.join(package_path, "src")
                nodes_path = os.path.join(package_src, "nodetool", "nodes")
                
                if os.path.exists(nodes_path):
                    print(f"\n  Processing development package: {item}")
                    print(f"    Package path: {package_path}")
                    print(f"    Nodes path: {nodes_path}")
                    
                    package_nodes = []
                    package_name = item.replace("-", "_")
                    
                    # Add the package's src directory to Python path
                    sys.path.insert(0, package_src)
                    print(f"    Added {package_src} to Python path")
                    
                    # Walk through all Python files in the nodes directory and its subdirectories
                    for root, _, files in os.walk(nodes_path):
                        for file in files:
                            if file.endswith(".py") and not file.startswith("__"):
                                module_path = os.path.join(root, file)
                                # Get relative path from src directory
                                module_name = os.path.relpath(module_path, package_src)
                                module_name = module_name.replace(os.sep, ".")[:-3]  # Remove .py extension
                                
                                try:
                                    module = importlib.import_module(module_name)
                                    class_count = 0
                                    for _, obj in inspect.getmembers(module, inspect.isclass):
                                        try:
                                            if (inspect.isclass(obj) and
                                                issubclass(obj, BaseNode) and obj is not BaseNode and
                                                hasattr(obj, 'is_visible') and obj.is_visible()):
                                                package_nodes.append(obj)
                                                class_count += 1
                                        except Exception:
                                            continue
                                    if class_count > 0:
                                        print(f"      [OK] {module_name}: {class_count} BaseNode subclasses")
                                except Exception as e:
                                    print(f"      [ERROR] Could not import {module_name}: {e}")
                                    continue
                    
                    # Remove package from Python path
                    if package_src in sys.path:
                        sys.path.remove(package_src)
                        print(f"    Removed {package_src} from Python path")
                    
                    # Remove duplicates and sort
                    unique_package = {c.__name__: c for c in package_nodes}
                    all_nodes[package_name] = [unique_package[n] for n in sorted(unique_package.keys())]
                    print(f"    Found {len(all_nodes[package_name])} unique nodes from {item}")
                else:
                    print(f"    No nodes directory found in {item}")
            
    except Exception as e:
        print(f"  [ERROR] Error discovering packages: {e}")
    
    return all_nodes

def generate_node_class_source(node_cls: type[BaseNode], namespace: str) -> str:
    """Generate C# class source code for a BaseNode subclass."""
    try:
        metadata = node_cls.get_metadata()
        
        lines = [
            "using MessagePack;",
            "using System.Collections.Generic;",
            "using Nodetool.Types;",  # Import types namespace
            "",
            f"namespace {namespace};",  # e.g., Nodetool.Nodes.Huggingface
            "",
            "[MessagePackObject]",
            f"public class {node_cls.__name__}",
            "{"
        ]
        
        # Add properties from node metadata
        index = 0
        
        # Add input properties
        for prop in metadata.properties:
            python_type = prop.type.get_python_type()
            csharp_type = python_type_to_csharp(python_type)
            default = default_value_to_csharp(prop.default)
            lines.append(f"    [Key({index})]")
            if default is not None:
                lines.append(f"    public {csharp_type} {prop.name} {{ get; set; }} = {default};")
            else:
                lines.append(f"    public {csharp_type} {prop.name} {{ get; set; }}")
            index += 1
        
        # Check if we need a return type class for multiple outputs
        if len(metadata.outputs) > 1:
            # Generate a return type class
            return_class_name = f"{node_cls.__name__}Output"
            lines.extend([
                "",
                "    [MessagePackObject]",
                f"    public class {return_class_name}",
                "    {"
            ])
            
            output_index = 0
            for output in metadata.outputs:
                python_type = output.type.get_python_type()
                output_type = python_type_to_csharp(python_type)
                lines.append(f"        [Key({output_index})]")
                lines.append(f"        public {output_type} {output.name} {{ get; set; }}")
                output_index += 1
            
            lines.append("    }")
            
            # Add a method to get the return type
            lines.extend([
                "",
                f"    public {return_class_name} Process()",
                "    {",
                "        // Implementation would be generated based on node logic",
                f"        return new {return_class_name}();",
                "    }"
            ])
        else:
            # Single output or no output
            if metadata.outputs:
                python_type = metadata.outputs[0].type.get_python_type()
                output_type = python_type_to_csharp(python_type)
                lines.extend([
                    "",
                    f"    public {output_type} Process()",
                    "    {",
                    "        // Implementation would be generated based on node logic",
                    f"        return default({output_type});",
                    "    }"
                ])
            else:
                lines.extend([
                    "",
                    "    public void Process()",
                    "    {",
                    "        // Implementation would be generated based on node logic",
                    "    }"
                ])
        
        lines.append("}")
        return "\n".join(lines) + "\n"
        
    except Exception as e:
        # Fall back to generating from class fields if metadata fails
        print(f"Warning: Could not get metadata for {node_cls.__name__}: {e}")
        return generate_fallback_node_class(node_cls, namespace)

def generate_fallback_node_class(node_cls: type[BaseNode], namespace: str) -> str:
    """Generate C# class from BaseNode fields as fallback when metadata fails."""
    lines = [
        "using MessagePack;",
        "using System.Collections.Generic;", 
        "using Nodetool.Types;",
        "",
        f"namespace {namespace};",
        "",
        "[MessagePackObject]",
        f"public class {node_cls.__name__}",
        "{"
    ]
    
    index = 0
    for name, field in node_cls.model_fields.items():
        if name.startswith("_"):  # Skip private fields
            continue
        csharp_type = python_type_to_csharp(field.annotation)
        default = default_value_to_csharp(field.default)
        lines.append(f"    [Key({index})]")
        if default is not None:
            lines.append(f"    public {csharp_type} {name} {{ get; set; }} = {default};")
        else:
            lines.append(f"    public {csharp_type} {name} {{ get; set; }}")
        index += 1
    
    lines.append("}")
    return "\n".join(lines) + "\n"

def generate_nodes_for_source(source_name: str, nodes: List[type[BaseNode]], output_dir: str) -> tuple[int, int]:
    """Generate C# classes for nodes from a specific source."""
    generated = 0
    errors = 0
    
    # Get clean names
    pkg_name = get_package_name(source_name)
    folder_name = get_folder_name(source_name)
    
    # Create namespace-specific directory
    namespace = f"Nodetool.Nodes.{pkg_name}"  # e.g., Nodetool.Nodes.Huggingface
    source_dir = os.path.join(output_dir, "Nodes", folder_name)  # e.g., Nodes/Huggingface
    
    os.makedirs(source_dir, exist_ok=True)
    
    # Generate individual node files
    for node_cls in nodes:
        try:
            src = generate_node_class_source(node_cls, namespace)
            filename = f"{node_cls.__name__}.cs"
            filepath = os.path.join(source_dir, filename)
            
            with open(filepath, "w", encoding="utf-8") as f:
                f.write(src)
            
            print(f"Generated: {filename} ({pkg_name})")
            generated += 1
            
        except Exception as e:
            print(f"[ERROR] Error generating {node_cls.__name__}: {e}")
            errors += 1
    
    # Generate summary file at package level
    if generated > 0:
        summary_path = os.path.join(output_dir, "Nodes", f"{folder_name}.cs")
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

def generate_package_summary(output_dir: str, package_name: str, types: list, nodes: list) -> str:
    """Generate a summary file for a specific package. Returns the generated file name."""
    # Convert package name to proper C# format
    pkg_name = get_package_name(package_name)
    summaries = []
    
    # Generate Types summary
    if types:
        types_summary_path = os.path.join(output_dir, "Types", f"{pkg_name}.cs")
        os.makedirs(os.path.dirname(types_summary_path), exist_ok=True)
        with open(types_summary_path, "w", encoding="utf-8") as f:
            f.write(f"""//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NodeTool SDK Type Generator.
//     Runtime Version: Python
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
{chr(10).join(f'    [MessagePackObject] public class {class_name} {{ ... }}' for class_name in types)}

    // Register types with MessagePack
    internal static void RegisterTypes()
    {{
{chr(10).join(f'        NodeToolTypes.KnownTypes.Add(typeof({class_name}));' for class_name in types)}
    }}
}}
""")
        summaries.append(f"{pkg_name}.cs")
    
    # Generate Nodes summary
    if nodes:
        nodes_summary_path = os.path.join(output_dir, "Nodes", f"{pkg_name}.cs")
        os.makedirs(os.path.dirname(nodes_summary_path), exist_ok=True)
        with open(nodes_summary_path, "w", encoding="utf-8") as f:
            f.write(f"""//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the NodeTool SDK Type Generator.
//     Runtime Version: Python
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
{chr(10).join(f'    [MessagePackObject] public class {class_name} {{ ... }}' for class_name in nodes)}

    // Register types with MessagePack
    internal static void RegisterTypes()
    {{
{chr(10).join(f'        NodeToolTypes.KnownTypes.Add(typeof({class_name}));' for class_name in nodes)}
    }}
}}
""")
        summaries.append(f"{pkg_name}.cs")
    
    return " and ".join(summaries)

def generate_summary_file(output_dir: str) -> None:
    """Generate the main NodeToolTypes.cs file."""
    summary_path = os.path.join(output_dir, "NodeToolTypes.cs")
    with open(summary_path, "w", encoding="utf-8") as f:
        f.write("""//------------------------------------------------------------------------------
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
///    var audio = new Core.AudioRef();
///    var classifier = new Huggingface.AudioClassifier();
///    
/// 3. MessagePack Serialization:
///    var data = MessagePackSerializer.Serialize(obj);
///    var obj = MessagePackSerializer.Deserialize<T>(data);
/// </summary>
public static class NodeToolTypes
{
    private static bool isInitialized = false;
    private static readonly object initLock = new object();
    internal static readonly List<Type> KnownTypes = new();

    public static void Initialize()
    {
        if (isInitialized) return;

        lock (initLock)
        {
            if (isInitialized) return;

            // Register types from all packages
            Types.Core.RegisterTypes();
            Types.Huggingface.RegisterTypes();
            Nodes.Core.RegisterTypes();
            Nodes.Huggingface.RegisterTypes();

            // Configure MessagePack
            var resolver = MessagePack.Resolvers.CompositeResolver.Create(
                MessagePack.Resolvers.StandardResolver.Instance,
                MessagePack.Resolvers.DynamicObjectResolver.Instance
            );

            var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
            MessagePackSerializer.DefaultOptions = options;

            // Register all types with MessagePack
            foreach (var type in KnownTypes)
            {
                MessagePackSerializer.SerializerCache.Get(type, options);
            }

            isInitialized = true;
        }
    }
}
""")

def cleanup_directory(output_dir: str) -> None:
    """Clean up the output directory before generating new files.
    
    Removes:
    /generated/
      ├── Types/              # All type definitions and summaries
      │   ├── CoreTypes.cs    # Core types summary
      │   ├── Core/           # Core type files
      │   │   └── *.cs
      │   ├── {Package}Types.cs  # Package types summary
      │   └── {Package}/      # Package type files
      │       └── *.cs
      ├── Nodes/             # All node definitions and summaries
      │   ├── CoreNodes.cs   # Core nodes summary
      │   ├── Core/          # Core node files
      │   │   └── *.cs
      │   ├── {Package}Nodes.cs  # Package nodes summary
      │   └── {Package}/     # Package node files
      │       └── *.cs
      └── NodeToolTypes.cs   # Global configuration file
    """
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
    print("==========================================")
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
    print("=========================================")
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
            
            # Create source-specific directory
            source_dir = os.path.join(output_dir, "Nodes", source_name.capitalize())
            os.makedirs(source_dir, exist_ok=True)
            
            # Generate each node class
            for node_cls in nodes:
                try:
                    src = generate_node_class_source(node_cls, f"{namespace}.Nodes.{source_name.capitalize()}")
                    filename = f"{node_cls.__name__}.cs"
                    filepath = os.path.join(source_dir, filename)
                    
                    # Create subdirectories if needed
                    os.makedirs(os.path.dirname(filepath), exist_ok=True)
                    
                    with open(filepath, "w", encoding="utf-8") as f:
                        f.write(src)
                    
                    print(f"Generated: {filename} ({source_name})")
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
    print("=== NodeTool SDK Complete Type & Node Generator ===")
    print("==================================================")
    print(f"Output: {output_dir}")
    print(f"Namespace: {namespace}")
    print()
    
    # Clean up output directory first
    cleanup_directory(output_dir)
    
    # Generate types first
    generate_all_types(output_dir, namespace)
    
    print("\n" + "=" * 50)
    
    # Generate nodes second
    generate_all_nodes(output_dir, namespace)
    
    # Generate summary file
    generate_summary_file(output_dir)
    
    print("\nComplete type and node generation completed successfully!")

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Generate C# types and nodes from nodetool-core and all packages")
    parser.add_argument("--output-dir", default=os.path.join(os.path.dirname(__file__), "..", "generated"), 
                       help="Output directory for generated C# files")
    parser.add_argument("--namespace", default="Nodetool.Types", help="C# namespace for generated classes")
    parser.add_argument("--types-only", action="store_true", help="Generate only types (not nodes)")
    parser.add_argument("--nodes-only", action="store_true", help="Generate only nodes (not nodes)")
    
    args = parser.parse_args()
    
    # Convert output_dir to absolute path and ensure it exists
    output_dir = os.path.abspath(args.output_dir)
    os.makedirs(output_dir, exist_ok=True)
    print(f"\nOutput directory: {output_dir}\n")
    
    if args.types_only:
        generate_all_types(output_dir, args.namespace)
    elif args.nodes_only:
        generate_all_nodes(output_dir, args.namespace)
    else:
        generate_all_types_and_nodes(output_dir, args.namespace)

if __name__ == "__main__":
    main() 