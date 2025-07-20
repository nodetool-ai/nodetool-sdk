# NodeTool Types Import and Cleanup Script
# Imports and cleans generated types from the current directory

param(
    [string]$SourcePath = ".",
    [string]$TargetPath = ".",
    [switch]$Force
)

Write-Host "🧹 NodeTool Types Import & Cleanup Script" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# Source and target paths
$sourcePath = Resolve-Path $SourcePath -ErrorAction SilentlyContinue
if (-not $sourcePath) {
    Write-Host "⚠️  Source path not found: $SourcePath" -ForegroundColor Yellow
    Write-Host "📝 No types to import. Run generate-types.ps1 first to create types." -ForegroundColor Yellow
    exit 0
}

Write-Host "📂 Source: $sourcePath" -ForegroundColor Green
Write-Host "📂 Target: $TargetPath" -ForegroundColor Green

# Known problematic files to skip or fix
$skipFiles = @(
    "OpenAIModel.cs",    # Has 'object' property name conflict
    "RecordType.cs"      # References undefined ColumnDef type
)

$fixFiles = @{
    "OpenAIModel.cs" = @{
        # Fix reserved keyword conflicts
        'public string object ' = 'public string @object '
    }
}

# Track statistics
$stats = @{
    Copied = 0
    Fixed = 0
    Skipped = 0
    Errors = 0
}

# Get all C# files from source
$sourceFiles = Get-ChildItem -Path $sourcePath -Filter "*.cs"
Write-Host "📝 Found $($sourceFiles.Count) C# files in source" -ForegroundColor Yellow

foreach ($file in $sourceFiles) {
    $fileName = $file.Name
    $targetFile = Join-Path $TargetPath $fileName
    
    try {
        # Check if file should be skipped
        if ($skipFiles -contains $fileName) {
            Write-Host "⏭️  Skipping problematic file: $fileName" -ForegroundColor Yellow
            $stats.Skipped++
            continue
        }
        
        # Read source content
        $content = Get-Content -Path $file.FullName -Raw
        
        # Apply known fixes
        $wasFixed = $false
        if ($fixFiles.ContainsKey($fileName)) {
            Write-Host "🔧 Applying fixes to: $fileName" -ForegroundColor Yellow
            foreach ($fix in $fixFiles[$fileName].GetEnumerator()) {
                if ($content -match [regex]::Escape($fix.Key)) {
                    $content = $content -replace [regex]::Escape($fix.Key), $fix.Value
                    $wasFixed = $true
                }
            }
        }
        
        # Apply global fixes
        $globalFixes = @{
            # Fix common issues
            'namespace Nodetool\.Types;' = 'namespace Nodetool.Types;'
            # Add any other global fixes here
        }
        
        foreach ($fix in $globalFixes.GetEnumerator()) {
            $content = $content -replace $fix.Key, $fix.Value
        }
        
        # Write to target
        Set-Content -Path $targetFile -Value $content -NoNewline
        
        if ($wasFixed) {
            Write-Host "✅ Fixed and copied: $fileName" -ForegroundColor Green
            $stats.Fixed++
        } else {
            Write-Host "✅ Copied: $fileName" -ForegroundColor Green
            $stats.Copied++
        }
        
    } catch {
        Write-Host "❌ Error processing $fileName : $($_.Exception.Message)" -ForegroundColor Red
        $stats.Errors++
    }
}

# Create missing types that are referenced but not defined
Write-Host "🔨 Creating missing referenced types..." -ForegroundColor Yellow

# Create ColumnDef type that's referenced by RecordType
$columnDefContent = @"
using MessagePack;

namespace Nodetool.Types;

/// <summary>
/// Column definition for RecordType (auto-generated to resolve missing reference)
/// </summary>
[MessagePackObject]
public class ColumnDef
{
    [Key(0)]
    public string name { get; set; } = "";
    
    [Key(1)]
    public string type { get; set; } = "";
    
    [Key(2)]
    public bool nullable { get; set; } = true;
}
"@

Set-Content -Path (Join-Path $TargetPath "ColumnDef.cs") -Value $columnDefContent
Write-Host "✅ Created missing type: ColumnDef.cs" -ForegroundColor Green

# Now copy RecordType with fix
try {
    $recordTypeFile = Join-Path $sourcePath "RecordType.cs"
    if (Test-Path $recordTypeFile) {
        $content = Get-Content -Path $recordTypeFile -Raw
        # Fix the List<object> to List<ColumnDef>
        $content = $content -replace 'List<object>\(\)', 'List<ColumnDef>()'
        Set-Content -Path (Join-Path $TargetPath "RecordType.cs") -Value $content -NoNewline
        Write-Host "✅ Fixed and copied: RecordType.cs" -ForegroundColor Green
        $stats.Fixed++
    }
} catch {
    Write-Host "❌ Error processing RecordType.cs: $($_.Exception.Message)" -ForegroundColor Red
    $stats.Errors++
}

# Summary
Write-Host ""
Write-Host "📊 Import Summary:" -ForegroundColor Cyan
Write-Host "  ✅ Copied: $($stats.Copied)" -ForegroundColor Green
Write-Host "  🔧 Fixed: $($stats.Fixed)" -ForegroundColor Yellow  
Write-Host "  ⏭️  Skipped: $($stats.Skipped)" -ForegroundColor Yellow
Write-Host "  ❌ Errors: $($stats.Errors)" -ForegroundColor Red

$total = $stats.Copied + $stats.Fixed
Write-Host "  📦 Total imported: $total types" -ForegroundColor Cyan

if ($stats.Errors -eq 0) {
    Write-Host ""
    Write-Host "🎉 Import completed successfully! Ready to build." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "⚠️  Import completed with errors. Check the output above." -ForegroundColor Yellow
} 