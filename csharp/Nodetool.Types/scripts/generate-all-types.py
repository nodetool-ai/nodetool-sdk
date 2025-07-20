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
    print(f"❌ Error importing nodetool modules: {e}")
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
    print("🔍 Discovering types from nodetool-core...")
    core_types = []
    import nodetool
    
    # Use pkgutil to walk through nodetool modules
    for _, module_name, _ in pkgutil.walk_packages(nodetool.__path__, nodetool.__name__ + "."):
        try:
            module = importlib.import_module(module_name)
        except Exception as e:
            print(f"⚠️  Warning: Could not import {module_name}: {e}")
            continue
            
        for _, obj in inspect.getmembers(module, inspect.isclass):
            try:
                if (inspect.isclass(obj) and
                    issubclass(obj, BaseType) and obj is not BaseType and
                    obj.__module__.startswith("nodetool")):
                    core_types.append(obj)
            except Exception:
                continue
    
    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_types}
    all_types["core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"✅ Found {len(all_types['core'])} types from nodetool-core")
    
    # 2. Discover types from installed packages
    print("🔍 Discovering types from installed packages...")
    try:
        packages = discover_node_packages()
        for package in packages:
            if package.source_folder and os.path.exists(package.source_folder):
                package_types = []
                package_name = package.name.replace("-", "_")
                
                # Add package source to Python path
                package_src = os.path.join(package.source_folder, "src")
                if os.path.exists(package_src):
                    sys.path.insert(0, package_src)
                    
                    # Walk through package modules
                    try:
                        package_module = importlib.import_module("nodetool")
                        for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                            try:
                                module = importlib.import_module(module_name)
                            except Exception:
                                continue
                                
                            for _, obj in inspect.getmembers(module, inspect.isclass):
                                try:
                                    if (inspect.isclass(obj) and
                                        issubclass(obj, BaseType) and obj is not BaseType and
                                        obj.__module__.startswith("nodetool")):
                                        package_types.append(obj)
                                except Exception:
                                    continue
                    except Exception as e:
                        print(f"⚠️  Warning: Could not process package {package.name}: {e}")
                    
                    # Remove duplicates and sort
                    unique_package = {c.__name__: c for c in package_types}
                    if unique_package:
                        all_types[package_name] = [unique_package[n] for n in sorted(unique_package.keys())]
                        print(f"✅ Found {len(all_types[package_name])} types from {package.name}")
                    
                    # Remove package from Python path
                    if package_src in sys.path:
                        sys.path.remove(package_src)
                        
    except Exception as e:
        print(f"⚠️  Warning: Could not discover packages: {e}")
    
    return all_types

def generate_types_for_source(source_name: str, classes: List[type[BaseType]], output_dir: str, base_namespace: str) -> int:
    """Generate C# classes for a specific source (core or package)."""
    generated = 0
    errors = 0
    
    # Create source-specific namespace
    if source_name == "core":
        namespace = base_namespace
    else:
        namespace = f"{base_namespace}.{source_name}"
    
    for cls in classes:
        try:
            src = generate_class_source(cls, namespace)
            output_file = os.path.join(output_dir, f"{cls.__name__}.cs")
            
            with open(output_file, "w", encoding="utf-8") as f:
                f.write(src)
            
            print(f"✅ Generated: {cls.__name__}.cs ({source_name})")
            generated += 1
            
        except Exception as e:
            print(f"❌ Error generating {cls.__name__} from {source_name}: {e}")
            errors += 1
    
    return generated, errors

def generate_all_types(output_dir: str, namespace: str = "Nodetool.Types") -> None:
    """Generate C# classes for all BaseType subclasses from all sources."""
    print("🚀 NodeTool SDK Complete Type Generator")
    print("========================================")
    print(f"📂 Output: {output_dir}")
    print(f"📦 Namespace: {namespace}")
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
            print(f"\n📦 Generating types from {source_name}...")
            generated, errors = generate_types_for_source(source_name, classes, output_dir, namespace)
            total_generated += generated
            total_errors += errors
    
    print(f"\n📊 Generation Summary:")
    print(f"  ✅ Generated: {total_generated}")
    print(f"  ❌ Errors: {total_errors}")
    print(f"  📁 Output: {output_dir}")
    
    if total_errors == 0:
        print("\n🎉 Type generation completed successfully!")
    else:
        print(f"\n⚠️  Generation completed with {total_errors} errors.")

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Generate C# types from nodetool-core and all packages")
    parser.add_argument("--output-dir", default=".", help="Output directory for generated C# files")
    parser.add_argument("--namespace", default="Nodetool.Types", help="C# namespace for generated classes")
    
    args = parser.parse_args()
    
    generate_all_types(args.output_dir, args.namespace)

if __name__ == "__main__":
    main() 