param(
    [string]$OutputDirectory,
    [switch]$SkipBuild,
    [switch]$NoRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$deploymentDirectory = $PSScriptRoot
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $deploymentDirectory "..\.."))
$vlProject = Join-Path $repoRoot "csharp\Nodetool.SDK.VL\Nodetool.SDK.VL.csproj"
$nuspecPath = Join-Path $deploymentDirectory "VL.Nodetool.nuspec"
$nugetPath = Join-Path $deploymentDirectory "nuget.exe"
$versionPropsPath = Join-Path $repoRoot "csharp\Directory.Build.props"
$sourceVlDocument = Join-Path $repoRoot "vvvv\VL.Nodetool.vl"
$stagingDirectory = Join-Path $deploymentDirectory ".pack"
$stagedVlDocument = Join-Path $stagingDirectory "VL.Nodetool.vl"

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $deploymentDirectory "out"
}

$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

if (-not $SkipBuild) {
    $buildArguments = @("build", $vlProject, "-c", "Release")
    if ($NoRestore) {
        $buildArguments += "--no-restore"
    }
    dotnet @buildArguments
    if ($LASTEXITCODE -ne 0) {
        throw "VL project build failed with exit code $LASTEXITCODE."
    }
}

New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null
$vlDocumentText = [System.IO.File]::ReadAllText($sourceVlDocument)
$developmentAssemblyPrefix = "../csharp/Nodetool.SDK.VL/bin/Release/net8.0/"
$packageAssemblyPrefix = "lib/net8.0/"
if (-not $vlDocumentText.Contains($developmentAssemblyPrefix)) {
    throw "The source VL document no longer contains the expected development assembly prefix."
}
$packagedVlDocumentText = $vlDocumentText.Replace(
    $developmentAssemblyPrefix,
    $packageAssemblyPrefix)
[System.IO.File]::WriteAllText(
    $stagedVlDocument,
    $packagedVlDocumentText,
    [System.Text.UTF8Encoding]::new($false))

[xml]$versionProps = Get-Content -LiteralPath $versionPropsPath
$packageVersion = [string]$versionProps.Project.PropertyGroup.NodetoolSdkVersion
if ([string]::IsNullOrWhiteSpace($packageVersion)) {
    throw "NodetoolSdkVersion is missing from $versionPropsPath."
}

& $nugetPath pack $nuspecPath -Version $packageVersion -OutputDirectory $resolvedOutputDirectory -NonInteractive
if ($LASTEXITCODE -ne 0) {
    throw "VL.Nodetool package creation failed with exit code $LASTEXITCODE."
}

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

    $vlEntry = $archive.Entries |
        Where-Object { $_.FullName -eq "VL.Nodetool.vl" } |
        Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($vlEntry.Open())
    try {
        $packagedDocument = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    $assemblyLocations = @(
        [regex]::Matches(
            $packagedDocument,
            '<PlatformDependency\b[^>]*\bLocation="([^"]+)"') |
            ForEach-Object { $_.Groups[1].Value.Replace("\", "/") }
    )
    if ($assemblyLocations.Count -eq 0) {
        throw "Packaged VL document declares no managed assembly dependencies."
    }
    $missingAssemblyLocations = @(
        $assemblyLocations |
            Where-Object { $_ -notin $entryNames }
    )
    if ($missingAssemblyLocations.Count -gt 0) {
        throw "Packaged VL document references missing assemblies: $($missingAssemblyLocations -join ', ')"
    }

    $readmeEntry = $archive.Entries |
        Where-Object { $_.FullName -eq "docs/README.md" } |
        Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($readmeEntry.Open())
    try {
        $packagedReadme = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    if ($packagedReadme.Contains("NODETOOL_ENABLE_SDK_")) {
        throw "Packaged README still contains a retired positive SDK enable flag."
    }
    if (-not $packagedReadme.Contains(
            "SDK workflow discovery and lifecycle preflight are enabled by default")) {
        throw "Packaged README does not document the default-on SDK server behavior."
    }

    $packageMetadataEntry = $archive.Entries |
        Where-Object { $_.FullName -like "*.nuspec" } |
        Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($packageMetadataEntry.Open())
    try {
        $packageMetadata = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }
    if (-not $packageMetadata.Contains(
            "https://github.com/nodetool-ai/nodetool-sdk")) {
        throw "Package metadata does not reference the NodeTool SDK repository."
    }
}
finally {
    $archive.Dispose()
}

Write-Host "Verified package: $packagePath" -ForegroundColor Green
