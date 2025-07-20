"""
Utility functions for C# type generation.
"""
from typing import Any, get_origin, get_args

try:
    from nodetool.metadata.types import BaseType
except ImportError:
    # Define a dummy BaseType if nodetool is not available
    class BaseType:
        pass

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
    """Convert raw package name to proper C# namespace name."""
    # First clean up any module path format
    name = raw_name.lower()

    # Handle module paths (e.g., nodetool.types.nodes.huggingface)
    if '.' in name:
        parts = name.split('.')
        # Find the relevant package part (usually after nodetool. or before .nodes)
        for part in parts:
            if part not in ['nodetool', 'types', 'nodes']:
                name = part
                break

    # Remove nodetool prefix if present
    if name.startswith("nodetool-") or name.startswith("nodetool_"):
        name = name[9:]

    # Special case for libaudio -> Lib.Audio
    if any(name.replace("-", "_").replace(".", "_") == variant.replace("-", "_").replace(".", "_")
           for variant in ['libaudio', 'lib_audio', 'lib-audio', 'lib.audio']):
        return 'Lib.Audio'

    # Convert to PascalCase
    parts = name.replace("-", "_").split("_")
    return "".join(part.capitalize() for part in parts) 