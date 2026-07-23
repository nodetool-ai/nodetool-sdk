param(
    [string]$OutputDirectory,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$deploymentDirectory = $PSScriptRoot
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $deploymentDirectory "..\.."))
$vlProject = Join-Path $repoRoot "csharp\Nodetool.SDK.VL\Nodetool.SDK.VL.csproj"
$nuspecPath = Join-Path $deploymentDirectory "VL.Nodetool.nuspec"
$nugetPath = Join-Path $deploymentDirectory "nuget.exe"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $deploymentDirectory "out"
}

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

if (-not $SkipBuild) {
    dotnet build $vlProject -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "VL project build failed with exit code $LASTEXITCODE."
    }
}

& $nugetPath pack $nuspecPath -OutputDirectory $resolvedOutputDirectory -NonInteractive
if ($LASTEXITCODE -ne 0) {
    throw "VL.Nodetool package creation failed with exit code $LASTEXITCODE."
}

[xml]$nuspec = Get-Content -LiteralPath $nuspecPath
$packageVersion = $nuspec.package.metadata.version
$packagePath = Join-Path $resolvedOutputDirectory "VL.Nodetool.$packageVersion.nupkg"
if (-not (Test-Path -LiteralPath $packagePath)) {
    throw "Expected package was not created: $packagePath"
}

$requiredEntries = @(
    "VL.Nodetool.vl",
    "help/help.xml",
    "help/Nodetool_Help.vl",
    "lib/net8.0/Nodetool.SDK.VL.dll",
    "lib/net8.0/Nodetool.SDK.dll",
    "lib/net8.0/Nodetool.Types.dll",
    "docs/README.md",
    "icon/nugeticon.png"
)

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entryNames = @($archive.Entries | ForEach-Object { $_.FullName })
    $missingEntries = @($requiredEntries | Where-Object { $_ -notin $entryNames })
    if ($missingEntries.Count -gt 0) {
        throw "Package is missing required entries: $($missingEntries -join ', ')"
    }

    $emptyAssemblies = @(
        $archive.Entries |
            Where-Object { $_.FullName -like "lib/net8.0/*.dll" -and $_.Length -eq 0 } |
            ForEach-Object { $_.FullName }
    )
    if ($emptyAssemblies.Count -gt 0) {
        throw "Package contains empty assemblies: $($emptyAssemblies -join ', ')"
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Verified package: $packagePath" -ForegroundColor Green
