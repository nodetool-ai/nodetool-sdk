#!/usr/bin/env pwsh
<#
.SYNOPSIS
    NodeTool SDK Type Generator - Main Entry Point
    
.DESCRIPTION
    This script is the main entry point for generating C# types from NodeTool packages.
    It calls the actual generation script located in the scripts/ directory.
    
.PARAMETER OutputDir
    Output directory for generated C# files (default: current directory)
    
.PARAMETER Namespace
    Base namespace for generated types (default: "Nodetool.Types")
    
.PARAMETER Clean
    Clean existing .cs files before generation
    
.EXAMPLE
    .\generate-types.ps1 -Clean
    Generate all types with clean output directory
    
.EXAMPLE
    .\generate-types.ps1 -OutputDir ".\generated" -Namespace "MyApp.Types"
    Generate types to custom directory with custom namespace
#>

param(
    [Parameter(Position=0)]
    [string]$OutputDir = ".",
    [Parameter(Position=1)]
    [string]$Namespace = "Nodetool.Types",
    [switch]$Clean
)

# Get the directory where this script is located
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScriptsDir = Join-Path $ScriptDir "scripts"

# Check if scripts directory exists
if (-not (Test-Path $ScriptsDir)) {
    Write-Host "❌ Error: Scripts directory not found at: $ScriptsDir" -ForegroundColor Red
    exit 1
}

# Call the actual generation script
$GenerateScript = Join-Path $ScriptsDir "generate-all-types.ps1"

if (-not (Test-Path $GenerateScript)) {
    Write-Host "❌ Error: Generation script not found at: $GenerateScript" -ForegroundColor Red
    exit 1
}

# Execute the generation script
Write-Host "🚀 NodeTool SDK Type Generator" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Calling generation script: $GenerateScript" -ForegroundColor Gray
Write-Host ""

# Call the script with proper parameter passing
if ($Clean) {
    & $GenerateScript -Clean
} else {
    & $GenerateScript
} 