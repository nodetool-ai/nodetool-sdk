# NodeTool SDK Complete Type Generation Script
# Generates C# types from nodetool-core AND all installed packages in one place

param(
    [string]$OutputDir = ".",
    [string]$Namespace = "Nodetool.Types",
    [switch]$Clean
)

Write-Host "NodeTool SDK Complete Type Generator" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Check if Python is available
try {
    $pythonVersion = python --version 2>&1
    Write-Host "Python: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Python not found in PATH" -ForegroundColor Red
    Write-Host "   Please install Python and ensure it's in your PATH" -ForegroundColor Yellow
    exit 1
}

# Check if nodetool is available
try {
    $nodetoolVersion = python -c "import nodetool; print('nodetool available')" 2>&1
    Write-Host "✅ nodetool package is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: nodetool package not found" -ForegroundColor Red
    Write-Host "   Please install nodetool-core: pip install -e ../../../nodetool-core" -ForegroundColor Yellow
    exit 1
}

# Clean output directory if requested
if ($Clean) {
    Write-Host "🧹 Cleaning output directory..." -ForegroundColor Yellow
    if (Test-Path $OutputDir) {
        Get-ChildItem -Path $OutputDir -Filter "*.cs" | Remove-Item -Force
        Write-Host "✅ Cleaned existing .cs files" -ForegroundColor Green
    }
}

# Run the Python type generator
Write-Host "Running complete type generator..." -ForegroundColor Yellow
$pythonScript = Join-Path $PSScriptRoot "generate-all-types.py"
$pythonArgs = @(
    "--output-dir"
    $OutputDir
    "--namespace"
    $Namespace
)

try {
    $command = "python $pythonScript --output-dir `"$OutputDir`" --namespace `"$Namespace`""
    Write-Host "Executing: $command" -ForegroundColor Gray
    Invoke-Expression $command
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Complete type generation completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "❌ Complete type generation failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host "❌ Error running complete type generator: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Show generated files
$generatedFiles = Get-ChildItem -Path $OutputDir -Filter "*.cs" | Sort-Object Name
if ($generatedFiles) {
    Write-Host ""
    Write-Host "Generated Files:" -ForegroundColor Cyan
    foreach ($file in $generatedFiles) {
        Write-Host "  ✅ $($file.Name)" -ForegroundColor Green
    }
    Write-Host ""
    Write-Host "Total: $($generatedFiles.Count) C# type files" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "⚠️  No C# files were generated" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Complete type generation process complete!" -ForegroundColor Green 