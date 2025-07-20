# NodeTool SDK Type Aggregation Script
# Aggregates C# types from all installed nodetool packages

param(
    [string]$OutputDir = ".",
    [switch]$Verbose
)

Write-Host "🔍 NodeTool SDK Type Aggregator" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan

# Check if Python is available
try {
    $pythonVersion = python --version 2>&1
    Write-Host "🐍 Python: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Python not found in PATH" -ForegroundColor Red
    Write-Host "   Please install Python and ensure it's in your PATH" -ForegroundColor Yellow
    exit 1
}

# Run the Python aggregation script
Write-Host "🔧 Running type aggregation..." -ForegroundColor Yellow
$pythonScript = "aggregate-types.py"
$pythonArgs = @(
    "--output-dir", $OutputDir
)

if ($Verbose) {
    $pythonArgs += "--verbose"
}

try {
    python $pythonScript @pythonArgs
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Type aggregation completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Type aggregation failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host "❌ Error running type aggregation: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Show aggregated files
$typesDir = Join-Path $OutputDir "types"
$nodesDir = Join-Path $OutputDir "nodes"

if (Test-Path $typesDir) {
    $typeFiles = Get-ChildItem -Path $typesDir -Filter "*.cs" | Sort-Object Name
    if ($typeFiles) {
        Write-Host ""
        Write-Host "📝 Aggregated Types:" -ForegroundColor Cyan
        foreach ($file in $typeFiles) {
            Write-Host "  ✅ $($file.Name)" -ForegroundColor Green
        }
        Write-Host "  📊 Total: $($typeFiles.Count) type files" -ForegroundColor Cyan
    }
}

if (Test-Path $nodesDir) {
    $nodeFiles = Get-ChildItem -Path $nodesDir -Filter "*.cs" | Sort-Object Name
    if ($nodeFiles) {
        Write-Host ""
        Write-Host "🔧 Aggregated Nodes:" -ForegroundColor Cyan
        foreach ($file in $nodeFiles) {
            Write-Host "  ✅ $($file.Name)" -ForegroundColor Green
        }
        Write-Host "  📊 Total: $($nodeFiles.Count) node files" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "🎉 Type aggregation process complete!" -ForegroundColor Green 