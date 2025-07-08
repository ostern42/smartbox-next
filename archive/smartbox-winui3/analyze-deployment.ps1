# Analyze what files are in the deployment

param(
    [string]$Path = ".\bin\x64\Debug\net8.0-windows10.0.19041.0"
)

Write-Host "Analyzing SmartBox Deployment..." -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Group files by type
$files = Get-ChildItem -Path $Path -File -Recurse

$groups = $files | Group-Object -Property Extension | Sort-Object -Property Count -Descending

Write-Host "`nFile Type Summary:" -ForegroundColor Yellow
Write-Host "Type`tCount`tSize (MB)`tDescription" -ForegroundColor Gray
Write-Host "----`t-----`t---------`t-----------" -ForegroundColor Gray

foreach ($group in $groups) {
    $totalSize = [math]::Round(($group.Group | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
    $desc = switch ($group.Name) {
        ".dll" { "Runtime libraries (REQUIRED)" }
        ".exe" { "Executables (REQUIRED)" }
        ".json" { "Configuration files (REQUIRED)" }
        ".pdb" { "Debug symbols (OPTIONAL - can remove)" }
        ".xml" { "Documentation (OPTIONAL - can remove)" }
        ".pri" { "Package resources (REQUIRED)" }
        ".png" { "Images" }
        ".html" { "Web interface" }
        ".js" { "JavaScript" }
        ".css" { "Stylesheets" }
        default { "Other" }
    }
    
    Write-Host "$($group.Name)`t$($group.Count)`t$totalSize`t$desc"
}

# List large DLLs
Write-Host "`nLargest DLLs:" -ForegroundColor Yellow
$dlls = Get-ChildItem -Path $Path -Filter "*.dll" -Recurse | 
    Sort-Object -Property Length -Descending | 
    Select-Object -First 20

foreach ($dll in $dlls) {
    $sizeMB = [math]::Round($dll.Length / 1MB, 2)
    Write-Host "  $($dll.Name) - $sizeMB MB" -ForegroundColor Gray
}

# Removable files
Write-Host "`nFiles that can be safely removed:" -ForegroundColor Green
$removable = @("*.pdb", "*.xml", "*.ipdb", "*.iobj", "*.exp", "*.lib")
$removableSize = 0

foreach ($pattern in $removable) {
    $files = Get-ChildItem -Path $Path -Filter $pattern -Recurse -ErrorAction SilentlyContinue
    if ($files) {
        $size = [math]::Round(($files | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
        $removableSize += ($files | Measure-Object -Property Length -Sum).Sum
        Write-Host "  $pattern - $($files.Count) files, $size MB" -ForegroundColor Gray
    }
}

$totalSizeMB = [math]::Round($removableSize / 1MB, 2)
Write-Host "`nTotal space that can be saved: $totalSizeMB MB" -ForegroundColor Cyan

# Essential files
Write-Host "`nEssential files (DO NOT REMOVE):" -ForegroundColor Red
$essential = @(
    "SmartBoxNext.exe",
    "SmartBoxNext.dll", 
    "SmartBoxNext.deps.json",
    "SmartBoxNext.runtimeconfig.json",
    "WebView2Loader.dll",
    "Microsoft.Web.WebView2.Core.dll",
    "Microsoft.Windows.SDK.NET.dll",
    "WinRT.Runtime.dll",
    "FellowOakDicom.Core.dll"
)

foreach ($file in $essential) {
    if (Test-Path "$Path\$file") {
        Write-Host "  ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file (NOT FOUND)" -ForegroundColor Yellow
    }
}