param(
    [switch]$SkipGitDiff,
    [switch]$SkipGeneration,
    [switch]$IncludeVL,
    [switch]$IncludeVLTests,
    [switch]$VerifySdkPackage,
    [switch]$VerifyVLPackage,
    [switch]$NoRestore,
    [string]$OutputDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$csharpDir = $PSScriptRoot
$typesDir = Join-Path $csharpDir "Nodetool.Types"
$sdkDir = Join-Path $csharpDir "Nodetool.SDK"
$testsDir = Join-Path $csharpDir "Nodetool.SDK.Tests"
$vlUnitTestsDir = Join-Path $csharpDir "Nodetool.SDK.VL.UnitTests"
$vlTestsDir = Join-Path $csharpDir "Nodetool.SDK.VL.Tests"
$noRestoreArguments = if ($NoRestore) { @("--no-restore") } else { @() }

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if ($IncludeVLTests -and -not $IncludeVL) {
    throw "-IncludeVLTests requires -IncludeVL so the current VL assemblies are built first."
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $csharpDir "_vvvv_builds\Release\net8.0"
}
$resolvedOutputDir = [System.IO.Path]::GetFullPath($OutputDir)
if (-not (Test-Path $resolvedOutputDir)) {
    New-Item -ItemType Directory -Path $resolvedOutputDir -Force | Out-Null
}

Write-Host "=== NodeTool C# regen + verify ===" -ForegroundColor Cyan
Write-Host "Repo root: $root" -ForegroundColor Gray
Write-Host "Build output: $resolvedOutputDir" -ForegroundColor Gray

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
        if ($LASTEXITCODE -ne 0) {
            throw "Python could not import nodetool."
        }
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
        if ($LASTEXITCODE -ne 0) {
            throw "C# type generation failed with exit code $LASTEXITCODE."
        }
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
        if ($LASTEXITCODE -ne 0) {
            throw "Generated Nodetool.Types output differs from the committed files."
        }
        git diff --exit-code -- $sdkDir
        if ($LASTEXITCODE -ne 0) {
            throw "Nodetool.SDK differs from the committed files."
        }
    }
}

# 3) Build
Write-Host ""
Write-Host ">>> Building C# projects..." -ForegroundColor Cyan

Invoke-DotNet (@("build", (Join-Path $typesDir "Nodetool.Types.csproj"), "-c", "Release", "-o", $resolvedOutputDir) + $noRestoreArguments)
Invoke-DotNet (@("build", (Join-Path $sdkDir "Nodetool.SDK.csproj"), "-c", "Release", "-o", $resolvedOutputDir) + $noRestoreArguments)
Invoke-DotNet (@("build", (Join-Path $sdkDir "TestConsole\Nodetool.SDK.TestConsole.csproj"), "-c", "Release", "-o", $resolvedOutputDir) + $noRestoreArguments)
Invoke-DotNet (@("test", (Join-Path $testsDir "Nodetool.SDK.Tests.csproj"), "-c", "Release") + $noRestoreArguments)

if ($VerifySdkPackage) {
    Write-Host ""
    Write-Host ">>> Creating and verifying portable C# SDK package..." -ForegroundColor Cyan
    $packageDir = Join-Path $root "artifacts\sdk-package-verify"
    if (-not (Test-Path $packageDir)) {
        New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    }

    Invoke-DotNet @("pack", (Join-Path $sdkDir "Nodetool.SDK.csproj"), "-c", "Release", "--no-restore", "-o", $packageDir)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $sdkPackage = Get-ChildItem -LiteralPath $packageDir -Filter "Nodetool.SDK.*.nupkg" |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $sdkPackage) {
        throw "Nodetool.SDK package was not created."
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($sdkPackage.FullName)
    try {
        $nuspecEntry = $archive.Entries |
            Where-Object { $_.FullName -eq "Nodetool.SDK.nuspec" } |
            Select-Object -First 1
        if ($null -eq $nuspecEntry) {
            throw "Nodetool.SDK.nuspec is missing from $($sdkPackage.Name)."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            $nuspec = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }
        if ($nuspec -match '<dependency id="Nodetool\.Types"') {
            throw "Portable Nodetool.SDK package unexpectedly depends on Nodetool.Types."
        }
        if ($archive.Entries.FullName -contains "lib/net8.0/Nodetool.Types.dll") {
            throw "Portable Nodetool.SDK package unexpectedly contains Nodetool.Types.dll."
        }
    } finally {
        $archive.Dispose()
    }
}

if ($IncludeVL) {
    Write-Host ""
    Write-Host ">>> Building and unit-testing VL adapter..." -ForegroundColor Cyan
    $vlDir = Join-Path $csharpDir "Nodetool.SDK.VL"
    Invoke-DotNet (@("build", (Join-Path $vlDir "Nodetool.SDK.VL.csproj"), "-c", "Release", "-o", $resolvedOutputDir) + $noRestoreArguments)
    Invoke-DotNet (@("test", (Join-Path $vlUnitTestsDir "Nodetool.SDK.VL.UnitTests.csproj"), "-c", "Release") + $noRestoreArguments)
}

if ($VerifyVLPackage) {
    Write-Host ""
    Write-Host ">>> Creating and verifying VL.Nodetool package..." -ForegroundColor Cyan
    $packageScript = Join-Path $root "vvvv\deployment\pack-and-verify.ps1"
    & $packageScript
}

if ($IncludeVLTests) {
    Write-Host ""
    Write-Host ">>> Running source and isolated-package VL document tests..." -ForegroundColor Cyan
    Invoke-DotNet (@("test", (Join-Path $vlTestsDir "Nodetool.SDK.VL.Tests.csproj"), "-c", "Release") + $noRestoreArguments)
}

Write-Host ""
Write-Host "Done" -ForegroundColor Green
