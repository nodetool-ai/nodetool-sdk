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

def _discover_types_in_package_src(package_src: str) -> List[type[BaseType]]:
    """
    Discover BaseType subclasses defined in a given package's src tree.

    We avoid scanning the entire `nodetool` namespace package (which can span many repos)
    by walking Python files under `package_src/nodetool` and importing them directly.
    """
    types: List[type[BaseType]] = []
    nodetool_dir = os.path.join(package_src, "nodetool")
    if not os.path.exists(nodetool_dir):
        return types

    sys.path.insert(0, package_src)
    try:
        for root, _, files in os.walk(nodetool_dir):
            for file in files:
                if not (file.endswith(".py") and not file.startswith("__")):
                    continue
                module_path = os.path.join(root, file)
                module_name = os.path.relpath(module_path, package_src)
                module_name = module_name.replace(os.sep, ".")[:-3]  # strip .py
                try:
                    module = importlib.import_module(module_name)
                except Exception:
                    continue

                for _, obj in inspect.getmembers(module, inspect.isclass):
                    try:
                        if (
                            inspect.isclass(obj)
                            and issubclass(obj, BaseType)
                            and obj is not BaseType
                            and obj.__module__ == module.__name__
                        ):
                            types.append(obj)
                    except Exception:
                        continue
    finally:
        if package_src in sys.path:
            sys.path.remove(package_src)

    # Deduplicate by (module, class) to be safe
    unique = {(t.__module__, t.__name__): t for t in types}
    return [unique[k] for k in sorted(unique.keys(), key=lambda x: (x[0], x[1]))]

def discover_all_base_types() -> Dict[str, List[type[BaseType]]]:
    """Discover all BaseType subclasses from nodetool-core and all packages."""
    all_types = {}
    
    # 1. Discover types from nodetool-core (only)
    print(">>> Discovering types...")
    core_types: List[type[BaseType]] = []
    try:
        # Use the location of nodetool.metadata.types as the anchor for core.
        import nodetool.metadata.types as core_types_module
        core_nodetool_dir = os.path.dirname(os.path.dirname(core_types_module.__file__))  # .../nodetool
        core_src = os.path.dirname(core_nodetool_dir)  # .../src
        core_types = _discover_types_in_package_src(core_src)
    except ImportError:
        print("[WARNING] nodetool-core not found. Skipping core type discovery.")

    # Remove duplicates and sort
    unique_core = {c.__name__: c for c in core_types}
    all_types["Core"] = [unique_core[n] for n in sorted(unique_core.keys())]
    print(f"Found {len(all_types['Core'])} types from Core")
    
    # 2. Discover types from installed packages (registry)
    try:
        packages = discover_node_packages()
        for package in packages:
            package_types: List[type[BaseType]] = []
            package_name = get_package_name(package.name)
            
            if package.source_folder and os.path.exists(package.source_folder):
                # Package has source folder (development install)
                package_src = os.path.join(package.source_folder, "src")
                if os.path.exists(package_src):
                    package_types = _discover_types_in_package_src(package_src)
            else:
                # Package is installed in environment
                try:
                    package_module_name = f"nodetool.{package.name.replace('-', '_')}"
                    package_module = importlib.import_module(package_module_name)
                    for _, module_name, _ in pkgutil.walk_packages(package_module.__path__, package_module.__name__ + "."):
                        try:
                            module = importlib.import_module(module_name)
                        except Exception:
                            continue
                        mod_file = getattr(module, "__file__", None)
                        if not mod_file:
                            continue
                        # Filter to modules that live under this package's module path
                        if not os.path.abspath(mod_file).startswith(os.path.abspath(os.path.dirname(package_module.__file__))):
                            continue
                        for _, obj in inspect.getmembers(module, inspect.isclass):
                            try:
                                if (
                                    inspect.isclass(obj)
                                    and issubclass(obj, BaseType)
                                    and obj is not BaseType
                                    and obj.__module__ == module.__name__
                                ):
                                    package_types.append(obj)
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
    
    # 3. Discover types from local workspace repos (best-effort, even if not installed)
    try:
        workspace_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
        for item in os.listdir(workspace_root):
            if not (item.startswith("nodetool-") and os.path.isdir(os.path.join(workspace_root, item))):
                continue
            pkg_src = os.path.join(workspace_root, item, "src")
            if not os.path.exists(pkg_src):
                continue
            pkg_name = get_package_name(item)
            # Don't re-scan if registry already found it
            if pkg_name in all_types:
                continue
            pkg_types = _discover_types_in_package_src(pkg_src)
            if pkg_types:
                unique_pkg = {c.__name__: c for c in pkg_types}
                all_types[pkg_name] = [unique_pkg[n] for n in sorted(unique_pkg.keys())]
                print(f"Found {len(all_types[pkg_name])} types from {pkg_name} (workspace)")
    except Exception as e:
        print(f"[WARNING] Workspace type discovery skipped: {e}")

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