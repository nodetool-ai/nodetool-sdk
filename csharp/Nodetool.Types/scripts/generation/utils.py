"""
Utility functions for C# type generation.
"""
import json
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

CSHARP_KEYWORDS = {
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
    "checked", "class", "const", "continue", "decimal", "default", "delegate",
    "do", "double", "else", "enum", "event", "explicit", "extern", "false",
    "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
    "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
    "new", "null", "object", "operator", "out", "override", "params", "private",
    "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
    "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
    "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
    "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
}

_KNOWN_CSHARP_TYPE_NAMES: dict[type, str] = {}

def set_known_csharp_type_names(mapping: dict[type, str]) -> None:
    """
    Provide a mapping from Python BaseType subclasses to fully-qualified C# type names.

    This is needed to generate correct cross-namespace references for types/nodes.
    """
    global _KNOWN_CSHARP_TYPE_NAMES
    _KNOWN_CSHARP_TYPE_NAMES = dict(mapping)

def csharp_identifier(name: str) -> str:
    """
    Return a C# identifier that compiles.

    We intentionally keep the original field name (often snake_case) to
    preserve wire compatibility for map-keyed formats if/when enabled.
    """
    if not name:
        return "_"
    if name in CSHARP_KEYWORDS:
        # Avoid @-escaped identifiers because some source generators (notably MessagePack)
        # can emit invalid code when the underlying member name is a reserved keyword.
        return f"{name}_"
    if name[0].isdigit():
        return "_" + name
    return name

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
                    # Prefer fully-qualified names when available (cross-package references).
                    if tp in _KNOWN_CSHARP_TYPE_NAMES:
                        return _KNOWN_CSHARP_TYPE_NAMES[tp]
                    # Fallback: best-effort (works if consumer has a using or types are in base namespace).
                    return tp.__name__
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
        # Use a verbatim string literal to safely represent multiline prompts and avoid
        # needing to escape backslashes. Double quotes must be doubled in verbatim strings.
        escaped = value.replace('"', '""')
        return f'@"{escaped}"'
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (int, float)):
        return str(value)
    if isinstance(value, list):
        # Caller should use target-typed new() when the property is a generic list.
        return "new()"
    if isinstance(value, dict):
        return "new()"
    if isinstance(value, BaseType):
        tp = type(value)
        if tp in _KNOWN_CSHARP_TYPE_NAMES:
            return f"new {_KNOWN_CSHARP_TYPE_NAMES[tp]}()"
        return f"new {tp.__name__}()"
    return None

def get_package_name(raw_name: str) -> str:
    """Convert raw package name to proper C# namespace name."""
    name = raw_name.lower().replace(".", "_")

    if name.startswith("nodetool-"):
        name = name[len("nodetool-"):]
    elif name.startswith("nodetool_"):
        name = name[len("nodetool_"):]

    # Handle lib packages as a special case e.g. lib-audio -> Lib.Audio, libaudio -> Lib.Audio
    if name.startswith("lib"):
        lib_content = ""
        if name.startswith("lib-"):
            lib_content = name[len("lib-"):]
        elif name.startswith("lib_"):
            lib_content = name[len("lib_"):]
        else:  # for cases like 'libaudio'
            lib_content = name[len("lib"):]

        if lib_content:
            # convert to PascalCase and prepend Lib.
            pascal_name = "".join(part.capitalize() for part in lib_content.replace("-", "_").split("_"))
            return f"Lib.{pascal_name}"

    # for other packages, just PascalCase
    return "".join(part.capitalize() for part in name.replace("-", "_").split("_")) 