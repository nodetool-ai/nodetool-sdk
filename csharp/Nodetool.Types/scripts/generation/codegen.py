"""
C# code generation functions.
"""
from typing import List
from .utils import python_type_to_csharp, default_value_to_csharp, get_package_name, csharp_identifier

try:
    from nodetool.metadata.types import BaseType
    from nodetool.workflows.base_node import BaseNode
except ImportError:
    class BaseType: pass
    class BaseNode:
        model_fields = {}
        def get_metadata(self):
            raise NotImplementedError

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
    # Use model_fields for pydantic v2
    fields = getattr(cls, 'model_fields', {})
    for name, field in sorted(fields.items()):
        prop_name = csharp_identifier(name)
        csharp_type = python_type_to_csharp(field.annotation)
        default = default_value_to_csharp(field.default)
        lines.append(f"    [Key({index})]")
        if default is not None:
            lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }} = {default};")
        else:
            lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }}")
        index += 1
    
    lines.append("}")
    return "\n".join(lines) + "\n"

def generate_node_class_source(node_cls: type[BaseNode], package_name: str) -> str:
    """Generate C# class source code for a BaseNode subclass."""
    try:
        metadata = node_cls.get_metadata()
        
        # Convert to proper namespace format
        pkg_name = get_package_name(package_name)
        namespace = f"Nodetool.Nodes.{pkg_name}"
        
        lines = [
            "using MessagePack;",
            "using System.Collections.Generic;",
            "using Nodetool.Types;",  # Import types namespace
            "",
            f"namespace {namespace};",  # e.g., Nodetool.Nodes.Huggingface or Nodetool.Nodes.Lib.Audio
            "",
            "[MessagePackObject]",
            f"public class {node_cls.__name__}",
            "{"
        ]
        
        # Add properties from node metadata
        index = 0
        
        # Add input properties
        for prop in sorted(metadata.properties, key=lambda p: p.name):
            python_type = prop.type.get_python_type()
            csharp_type = python_type_to_csharp(python_type)
            default = default_value_to_csharp(prop.default)
            prop_name = csharp_identifier(prop.name)
            lines.append(f"    [Key({index})]")
            if default is not None:
                lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }} = {default};")
            else:
                lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }}")
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
            for output in sorted(metadata.outputs, key=lambda o: o.name):
                python_type = output.type.get_python_type()
                output_type = python_type_to_csharp(python_type)
                output_name = csharp_identifier(output.name)
                lines.append(f"        [Key({output_index})]")
                lines.append(f"        public {output_type} {output_name} {{ get; set; }}")
                output_index += 1
            
            lines.append("    }")
            
            # Add a method to get the return type
            lines.extend([
                "",
                f"    public {return_class_name} Process()",
                "    {",
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
                    f"        return default({output_type});",
                    "    }"
                ])
            else:
                lines.extend([
                    "",
                    "    public void Process()",
                    "    {",
                    "    }"
                ])
        
        lines.append("}")
        return "\n".join(lines) + "\n"
        
    except Exception as e:
        # Fall back to generating from class fields if metadata fails
        print(f"Warning: Could not get metadata for {node_cls.__name__}: {e}")
        return generate_fallback_node_class(node_cls, package_name)

def generate_fallback_node_class(node_cls: type[BaseNode], package_name: str) -> str:
    """Generate C# class from BaseNode fields as fallback when metadata fails."""
    # Clean up package name to avoid namespace issues
    # Strip any 'nodetool.' prefix and '.types.nodes' parts from the module path
    if package_name.startswith("nodetool."):
        package_name = package_name.replace("nodetool.", "", 1)
    package_name = package_name.replace(".types.nodes.", "")
    
    # Convert to proper namespace format (e.g., 'huggingface' -> 'Huggingface')
    pkg_name = get_package_name(package_name)
    namespace = f"Nodetool.Nodes.{pkg_name}"  # e.g., Nodetool.Nodes.Huggingface
    
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
    for name, field in sorted(node_cls.model_fields.items()):
        if name.startswith("_"):  # Skip private fields
            continue
        prop_name = csharp_identifier(name)
        csharp_type = python_type_to_csharp(field.annotation)
        default = default_value_to_csharp(field.default)
        lines.append(f"    [Key({index})]")
        if default is not None:
            lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }} = {default};")
        else:
            lines.append(f"    public {csharp_type} {prop_name} {{ get; set; }}")
        index += 1
    
    lines.append("}")
    return "\n".join(lines) + "\n" 