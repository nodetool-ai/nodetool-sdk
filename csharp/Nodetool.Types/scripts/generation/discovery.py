"""
Discovery functions for finding BaseType and BaseNode subclasses.
"""
import os
import sys
import importlib
import inspect
import pkgutil
from typing import Dict, List

try:
    from nodetool.metadata.types import BaseType
    from nodetool.workflows.base_node import BaseNode
    from nodetool.packages.registry import discover_node_packages
except ImportError:
    # Define dummy classes if nodetool is not available
    class BaseType: pass
    class BaseNode: pass
    def discover_node_packages(): return []

from .utils import get_package_name

def discover_all_base_types() -> Dict[str, List[type[BaseType]]]:
    """Discover all BaseType subclasses from nodetool-core and all packages."""
    all_types = {}
    
    # 1. Discover types from nodetool-core
    print(">>> Discovering types...")
    core_types = []
    try:
        import nodetool
        
        # Walk through nodetool modules
        for _, module_name, _ in pkgutil.walk_packages(nodetool.__path__, nodetool.__name__ + "."):
            try:
                module = importlib.import_module(module_name)
                for _, obj in inspect.getmembers(module, inspect.isclass):
                    try:
                        if (inspect.isclass(obj) and
                            issubclass(obj, BaseType) and obj is not BaseType and
                            obj.__module__.startswith("nodetool")):
                            core_types.append(obj)
                    except Exception:
                        continue
            except Exception:
                continue
    except ImportError:
        print("[WARNING] nodetool-core not found. Skipping core type discovery.")

    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_types}
    all_types["Core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"Found {len(all_types['Core'])} types from Core")
    
    # 2. Discover types from installed packages
    try:
        packages = discover_node_packages()
        for package in packages:
            package_types = []
            package_name = get_package_name(package.name)
            
            if package.source_folder and os.path.exists(package.source_folder):
                # Package has source folder (development install)
                package_src = os.path.join(package.source_folder, "src")
                if os.path.exists(package_src):
                    sys.path.insert(0, package_src)
                    try:
                        package_module = importlib.import_module("nodetool")
                        for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                            try:
                                module = importlib.import_module(module_name)
                                for _, obj in inspect.getmembers(module, inspect.isclass):
                                    try:
                                        if (inspect.isclass(obj) and
                                            issubclass(obj, BaseType) and obj is not BaseType and
                                            obj.__module__.startswith("nodetool")):
                                            package_types.append(obj)
                                    except Exception:
                                        continue
                            except Exception:
                                continue
                    except Exception:
                        pass
                    if package_src in sys.path:
                        sys.path.remove(package_src)
            else:
                # Package is installed in environment
                try:
                    package_module_name = f"nodetool.{package.name.replace('-', '_')}"
                    package_module = importlib.import_module(package_module_name)
                    for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                        try:
                            module = importlib.import_module(module_name)
                            for _, obj in inspect.getmembers(module, inspect.isclass):
                                try:
                                    if (inspect.isclass(obj) and
                                        issubclass(obj, BaseType) and obj is not BaseType and
                                        obj.__module__.startswith("nodetool")):
                                        package_types.append(obj)
                                except Exception:
                                    continue
                        except Exception:
                            continue
                except Exception:
                    pass
            
            # Remove duplicates and sort
            unique_package = {c.__name__: c for c in package_types}
            if unique_package:
                all_types[package_name] = [unique_package[n] for n in sorted(unique_package.keys())]
                print(f"Found {len(all_types[package_name])} types from {package_name}")
                        
    except Exception as e:
        print(f"[ERROR] Error discovering packages: {e}")
    
    return all_types

def discover_all_base_nodes() -> Dict[str, List[type[BaseNode]]]:
    """Discover all BaseNode subclasses from nodetool-core and all packages."""
    all_nodes = {}
    
    # 1. Discover nodes from nodetool-core
    print(">>> Discovering nodes from nodetool-core...")
    core_nodes = []
    try:
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
    except ImportError:
        print("[WARNING] nodetool-core not found. Skipping core node discovery.")

    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_nodes}
    all_nodes["Core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"Found {len(all_nodes['Core'])} unique nodes from nodetool-core")
    
    # 2. Discover nodes from development packages in workspace
    print(">>> Discovering nodes from development packages...")
    try:
        # Look for development packages in the workspace
        workspace_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
        print(f"  Looking for development packages in workspace: {workspace_root}")
        
        for item in os.listdir(workspace_root):
            if item.startswith("nodetool-") and os.path.isdir(os.path.join(workspace_root, item)):
                package_path = os.path.join(workspace_root, item)
                package_src = os.path.join(package_path, "src")
                nodes_path = os.path.join(package_src, "nodetool", "nodes")
                
                if os.path.exists(nodes_path):
                    # Convert package name to proper format (e.g., 'nodetool-huggingface' -> 'Huggingface')
                    package_name = get_package_name(item)
                    print(f"\n  Processing development package: {item} (as {package_name})")
                    print(f"    Package path: {package_path}")
                    print(f"    Nodes path: {nodes_path}")
                    
                    package_nodes = []
                    
                    # Add the package's src directory to Python path
                    sys.path.insert(0, package_src)
                    print(f"    Added {package_src} to Python path")
                    
                    # Walk through all Python files in the nodes directory and its subdirectories
                    for root, _, files in os.walk(nodes_path):
                        for file in files:
                            if file.endswith(".py") and not file.startswith("__"):
                                module_path = os.path.join(root, file)
                                # Get relative path from src directory
                                # Get the module name relative to the package source
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
                                                
                                                # Set the package name directly from the discovery process
                                                obj.__module__ = package_name
                                                package_nodes.append(obj)
                                                class_count += 1
                                        except Exception as e:
                                            print(f"      [ERROR] Could not process class: {e}")
                                            continue
                                    
                                    if class_count > 0:
                                        print(f"      [OK] {module_name}: {class_count} BaseNode subclasses")
                                except Exception as e:
                                    print(f"      [ERROR] Could not import {module_name}: {e}")
                                    continue
                    
                    # Remove package from Python path
                    if package_src in sys.path:
                        sys.path.remove(package_src)
                    
                    # Remove duplicates and sort
                    unique_package = {c.__name__: c for c in package_nodes}
                    all_nodes[package_name] = [unique_package[n] for n in sorted(unique_package.keys())]
                    print(f"    Found {len(all_nodes[package_name])} unique nodes from {item}")
                else:
                    print(f"    No nodes directory found in {item}")
            
    except Exception as e:
        print(f"  [ERROR] Error discovering packages: {e}")
    
    return all_nodes 