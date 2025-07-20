#!/usr/bin/env python3
"""
NodeTool SDK Type Aggregator
Aggregates C# types from all installed nodetool packages into the SDK.
"""

import os
import sys
import shutil
import json
from pathlib import Path
from typing import List, Dict, Any, Optional
import argparse

def find_nodetool_packages() -> List[Path]:
    """Find all installed nodetool packages."""
    packages = []
    
    # Check common Python package locations
    python_paths = sys.path + [os.path.expanduser("~/.local/lib/python*/site-packages")]
    
    for path in python_paths:
        if os.path.exists(path):
            # Look for nodetool-* packages
            for item in os.listdir(path):
                if item.startswith("nodetool-") and os.path.isdir(os.path.join(path, item)):
                    package_path = Path(path) / item
                    packages.append(package_path)
    
    # Also check for packages in development mode (editable installs)
    for path in sys.path:
        if "src" in path and "nodetool" in path:
            parent = Path(path).parent
            if parent.name.startswith("nodetool-"):
                packages.append(parent)
    
    return packages

def find_csharp_types_in_package(package_path: Path) -> Dict[str, List[Path]]:
    """Find C# types and nodes in a package."""
    types = {
        "types": [],
        "nodes": []
    }
    
    # Look for csharp_types directory
    csharp_types_dir = package_path / "csharp_types"
    if csharp_types_dir.exists():
        for file in csharp_types_dir.glob("*.cs"):
            types["types"].append(file)
    
    # Look for csharp_nodes directory
    csharp_nodes_dir = package_path / "csharp_nodes"
    if csharp_nodes_dir.exists():
        for file in csharp_nodes_dir.glob("*.cs"):
            types["nodes"].append(file)
    
    return types

def copy_and_namespace_types(source_files: List[Path], target_dir: Path, package_name: str, file_type: str) -> int:
    """Copy C# files and update their namespace to include package name."""
    copied = 0
    
    for source_file in source_files:
        try:
            # Read source content
            with open(source_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Update namespace to include package name
            # Replace "namespace Nodetool.Types;" with "namespace Nodetool.Types.{package_name};"
            if "namespace Nodetool.Types;" in content:
                content = content.replace(
                    "namespace Nodetool.Types;", 
                    f"namespace Nodetool.Types.{package_name.replace('-', '_')};"
                )
            
            # Create target file path
            target_file = target_dir / f"{package_name}_{source_file.name}"
            
            # Write to target
            with open(target_file, 'w', encoding='utf-8') as f:
                f.write(content)
            
            copied += 1
            print(f"✅ Copied {file_type}: {source_file.name} -> {target_file.name}")
            
        except Exception as e:
            print(f"❌ Error copying {source_file}: {e}")
    
    return copied

def aggregate_types(output_dir: str, verbose: bool = False) -> None:
    """Aggregate C# types from all installed nodetool packages."""
    print("🔍 NodeTool SDK Type Aggregator")
    print("===============================")
    
    # Find all nodetool packages
    packages = find_nodetool_packages()
    if verbose:
        print(f"📦 Found {len(packages)} nodetool packages:")
        for pkg in packages:
            print(f"  - {pkg.name}")
    
    # Create output directories
    output_path = Path(output_dir)
    types_dir = output_path / "types"
    nodes_dir = output_path / "nodes"
    
    os.makedirs(types_dir, exist_ok=True)
    os.makedirs(nodes_dir, exist_ok=True)
    
    # Track statistics
    stats = {
        "packages_processed": 0,
        "types_copied": 0,
        "nodes_copied": 0,
        "errors": 0
    }
    
    # Process each package
    for package_path in packages:
        package_name = package_path.name
        
        try:
            if verbose:
                print(f"\n📦 Processing package: {package_name}")
            
            # Find C# types in this package
            csharp_files = find_csharp_types_in_package(package_path)
            
            # Copy types
            if csharp_files["types"]:
                types_copied = copy_and_namespace_types(
                    csharp_files["types"], 
                    types_dir, 
                    package_name, 
                    "types"
                )
                stats["types_copied"] += types_copied
            
            # Copy nodes
            if csharp_files["nodes"]:
                nodes_copied = copy_and_namespace_types(
                    csharp_files["nodes"], 
                    nodes_dir, 
                    package_name, 
                    "nodes"
                )
                stats["nodes_copied"] += nodes_copied
            
            if csharp_files["types"] or csharp_files["nodes"]:
                stats["packages_processed"] += 1
                
        except Exception as e:
            print(f"❌ Error processing package {package_name}: {e}")
            stats["errors"] += 1
    
    # Create summary
    print(f"\n📊 Aggregation Summary:")
    print(f"  📦 Packages processed: {stats['packages_processed']}")
    print(f"  📝 Types copied: {stats['types_copied']}")
    print(f"  🔧 Nodes copied: {stats['nodes_copied']}")
    print(f"  ❌ Errors: {stats['errors']}")
    print(f"  📁 Output: {output_dir}")
    
    if stats["errors"] == 0:
        print("\n🎉 Type aggregation completed successfully!")
    else:
        print(f"\n⚠️  Aggregation completed with {stats['errors']} errors.")

def main():
    """Main entry point."""
    parser = argparse.ArgumentParser(description="Aggregate C# types from all installed nodetool packages")
    parser.add_argument("--output-dir", default=".", help="Output directory for aggregated types")
    parser.add_argument("--verbose", "-v", action="store_true", help="Enable verbose output")
    
    args = parser.parse_args()
    
    aggregate_types(args.output_dir, args.verbose)

if __name__ == "__main__":
    main() 