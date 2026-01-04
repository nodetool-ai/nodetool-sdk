param(
    [switch]$SkipGitDiff,
    [switch]$SkipGeneration,
    [switch]$IncludeVL
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$csharpDir = $PSScriptRoot
$typesDir = Join-Path $csharpDir "Nodetool.Types"
$sdkDir = Join-Path $csharpDir "Nodetool.SDK"

Write-Host "=== NodeTool C# regen + verify ===" -ForegroundColor Cyan
Write-Host "Repo root: $root" -ForegroundColor Gray

if (-not (Test-Path $typesDir)) { throw "Missing: $typesDir" }
if (-not (Test-Path $sdkDir)) { throw "Missing: $sdkDir" }

# 1) Regenerate (best-effort)
Write-Host ""
Write-Host ">>> Regenerating C# types/nodes..." -ForegroundColor Cyan

$hasNodeTool = $false
if ($SkipGeneration) {
    Write-Host "Skipping generation: -SkipGeneration was provided." -ForegroundColor Yellow
} else {
    $hasNodeTool = $true
    try {
        python -c "import nodetool" | Out-Null
    } catch {
        $hasNodeTool = $false
        Write-Host "Skipping generation: Python module 'nodetool' is not available in this environment." -ForegroundColor Yellow
        Write-Host "Install nodetool-core (+ packages) in your Python env, then re-run this script." -ForegroundColor Yellow
    }
}

if ($hasNodeTool) {
    Push-Location $typesDir
    try {
        python .\scripts\generate-all-types.py --output-dir .\generated --namespace Nodetool.Types
    } finally {
        Pop-Location
    }
}

# 2) Verify git diff (optional)
if (-not $SkipGitDiff) {
    Write-Host ""
    Write-Host ">>> Verifying generated output is committed (git diff)..." -ForegroundColor Cyan

    $gitOk = $true
    try {
        git --version | Out-Null
    } catch {
        $gitOk = $false
        Write-Host "Skipping git diff: git not found." -ForegroundColor Yellow
    }

    if ($gitOk) {
        # If generation was skipped, this will still detect local drift from other edits.
        git diff --exit-code -- $typesDir\generated
        git diff --exit-code -- $sdkDir
    }
}

# 3) Build
Write-Host ""
Write-Host ">>> Building C# projects..." -ForegroundColor Cyan

dotnet build (Join-Path $typesDir "Nodetool.Types.csproj") -c Release
dotnet build (Join-Path $sdkDir "Nodetool.SDK.csproj") -c Release
dotnet build (Join-Path $sdkDir "TestConsole\Nodetool.SDK.TestConsole.csproj") -c Release

if ($IncludeVL) {
    Write-Host ""
    Write-Host ">>> Building VL project..." -ForegroundColor Cyan
    $vlDir = Join-Path $csharpDir "Nodetool.SDK.VL"
    dotnet build (Join-Path $vlDir "Nodetool.SDK.VL.csproj") -c Release
}

Write-Host ""
Write-Host "✅ Done" -ForegroundColor Green


