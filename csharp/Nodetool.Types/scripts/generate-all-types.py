#!/usr/bin/env python3
"""
NodeTool SDK Complete Type Generator
Generates C# types from nodetool-core AND all installed packages in one centralized location.
This avoids code duplication by generating everything in the SDK.
"""
import os
import argparse
from generation.orchestrator import (
    generate_all_types, 
    generate_all_nodes, 
    generate_all_types_and_nodes
)

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