#!/usr/bin/env python3
"""
NodeTool SDK Type Generator
Generates C# types from nodetool-core BaseType and BaseNode classes.
This script should be run from the nodetool-sdk directory.
"""

import os
import sys
import shutil
import importlib
import inspect
from pathlib import Path
from typing import Any, get_origin, get_args

# Add nodetool-core to Python path
nodetool_core_path = Path(__file__).parent.parent.parent.parent / "nodetool-core"
if nodetool_core_path.exists():
    sys.path.insert(0, str(nodetool_core_path / "src"))
else:
    print("❌ Error: nodetool-core not found at expected location")
    print(f"   Expected: {nodetool_core_path}")
    sys.exit(1)

try:
    from nodetool.metadata.types import BaseType
    from nodetool.workflows.base_node import BaseNode
    from nodetool.packages.discovery import walk_source_modules
except ImportError as e:
    print(f"❌ Error importing from nodetool-core: {e}")
    print("   Make sure nodetool-core is installed or in the Python path")
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

def discover_base_types() -> list[type[BaseType]]:
    """Discover all BaseType subclasses from nodetool-core."""
    import nodetool
    
    classes: list[type[BaseType]] = []
    
    # Walk through all nodetool modules
    for mod in walk_source_modules(nodetool.__path__, nodetool.__name__ + "."):
        try:
            module = importlib.import_module(mod.name)
        except Exception as e:
            print(f"⚠️  Warning: Could not import {mod.name}: {e}")
            continue
            
        for _, obj in inspect.getmembers(module, inspect.isclass):
            try:
                if (inspect.isclass(obj) and
                    issubclass(obj, BaseType) and obj is not BaseType and
                    obj.__module__.startswith("nodetool")):
                    classes.append(obj)
            except Exception:
                continue
    
    # Remove duplicates and sort
    unique = {c.__name__: c for c in classes}
    return [unique[n] for n in sorted(unique.keys())]

def generate_types(output_dir: str, namespace: str = "Nodetool.Types") -> None:
    """Generate C# classes for all BaseType subclasses."""
    print(f"🔍 Discovering BaseType subclasses...")
    classes = discover_base_types()
    print(f"📝 Found {len(classes)} BaseType subclasses")
    
    # Create output directory
    os.makedirs(output_dir, exist_ok=True)
    
    # Track statistics
    generated = 0
    errors = 0
    
    for cls in classes:
        try:
            src = generate_class_source(cls, namespace)
            output_file = os.path.join(output_dir, f"{cls.__name__}.cs")
            
            with open(output_file, "w", encoding="utf-8") as f:
                f.write(src)
            
            print(f"✅ Generated: {cls.__name__}.cs")
            generated += 1
            
        except Exception as e:
            print(f"❌ Error generating {cls.__name__}: {e}")
            errors += 1
    
    print(f"\n📊 Generation Summary:")
    print(f"  ✅ Generated: {generated}")
    print(f"  ❌ Errors: {errors}")
    print(f"  📁 Output: {output_dir}")

def main():
    """Main entry point."""
    import argparse
    
    parser = argparse.ArgumentParser(description="Generate C# types from nodetool-core")
    parser.add_argument("--output-dir", default=".", help="Output directory for generated C# files")
    parser.add_argument("--namespace", default="Nodetool.Types", help="C# namespace for generated classes")
    
    args = parser.parse_args()
    
    print("🚀 NodeTool SDK Type Generator")
    print("==============================")
    print(f"📂 Output: {args.output_dir}")
    print(f"📦 Namespace: {args.namespace}")
    print()
    
    generate_types(args.output_dir, args.namespace)
    
    print("\n🎉 Type generation complete!")

if __name__ == "__main__":
    main() 