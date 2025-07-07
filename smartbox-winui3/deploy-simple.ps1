# SmartBox Next - Simple Deployment Script
# Creates a cleaner structure while maintaining functionality

param(
    [string]$BuildPath = ".\bin\x64\Debug\net8.0-windows10.0.19041.0",
    [string]$DeployPath = ".\SmartBoxNext-Deploy"
)

Write-Host "SmartBox Next Deployment" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Clean and create deployment directory
if (Test-Path $DeployPath) {
    Remove-Item -Path $DeployPath -Recurse -Force
}
New-Item -ItemType Directory -Path $DeployPath -Force | Out-Null

# Create subdirectories
$dirs = @(
    "Data\Photos",
    "Data\Videos", 
    "Data\DICOM",
    "Data\Queue",
    "Data\Temp",
    "logs",
    "wwwroot"
)

foreach ($dir in $dirs) {
    New-Item -ItemType Directory -Path "$DeployPath\$dir" -Force | Out-Null
}

Write-Host "Copying application files..." -ForegroundColor Green

# Copy everything from build directory
Copy-Item "$BuildPath\*" -Destination $DeployPath -Recurse -Force

# Clean up unnecessary files
$unnecessaryFiles = @(
    "*.pdb",           # Debug symbols (optional)
    "*.xml",           # XML documentation
    "*.deps.json",     # Keep this - needed!
    "*.runtimeconfig.dev.json",
    "*.vshost.*",
    "*.exp",
    "*.lib",
    "*.ilk",
    "*.ipdb",
    "*.iobj"
)

# Remove only truly unnecessary files
foreach ($pattern in $unnecessaryFiles) {
    if ($pattern -ne "*.deps.json") {  # Keep deps.json
        Get-ChildItem -Path $DeployPath -Filter $pattern -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
    }
}

# Create a cleaner config if it doesn't exist
if (-not (Test-Path "$DeployPath\config.json")) {
    @'
{
  "storage": {
    "photosPath": "./Data/Photos",
    "videosPath": "./Data/Videos",
    "dicomPath": "./Data/DICOM",
    "tempPath": "./Data/Temp",
    "queuePath": "./Data/Queue",
    "logsPath": "./logs",
    "useRelativePaths": true,
    "maxStorageSizeMB": 10240,
    "retentionDays": 30
  },
  "pacs": {
    "aeTitle": "SMARTBOX",
    "remoteAeTitle": "PACS_SERVER",
    "remoteHost": "192.168.1.100",
    "remotePort": 104,
    "localPort": 0,
    "timeoutSeconds": 30,
    "useTls": false
  },
  "application": {
    "language": "en-US",
    "theme": "System",
    "showDebugInfo": false,
    "autoStartCapture": true,
    "minimizeToTray": false,
    "startWithWindows": false,
    "defaultModality": "ES"
  },
  "video": {
    "preferredWidth": 1920,
    "preferredHeight": 1080,
    "preferredFps": 60,
    "videoBitrateMbps": 5,
    "videoFormat": "webm",
    "jpegQuality": 95
  },
  "isFirstRun": true
}
'@ | Out-File -FilePath "$DeployPath\config.json" -Encoding UTF8
}

# Create start script
@'
@echo off
title SmartBox Next
cd /d "%~dp0"
echo Starting SmartBox Next...
echo.
echo If the application doesn't start, ensure:
echo - .NET 8 Desktop Runtime is installed
echo - WebView2 Runtime is installed
echo.
start "" "SmartBoxNext.exe"
'@ | Out-File -FilePath "$DeployPath\Start SmartBox.bat" -Encoding ASCII

# Create README
@'
SmartBox Next - Medical Imaging System
======================================

QUICK START:
- Double-click "Start SmartBox.bat" or "SmartBoxNext.exe"

FOLDERS:
- Data\Photos    : Captured images
- Data\Videos    : Recorded videos
- Data\DICOM     : Medical imaging files
- logs           : Application logs (daily files)

SETTINGS:
- Click Settings button in the app
- Or edit config.json directly

REQUIREMENTS:
- Windows 10/11 x64
- .NET 8 Desktop Runtime
- Microsoft Edge WebView2

TROUBLESHOOTING:
- Check logs folder for errors
- Ensure webcam is connected
- Run as Administrator if needed

'@ | Out-File -FilePath "$DeployPath\README.txt" -Encoding ASCII

# Count results
$totalFiles = (Get-ChildItem -Path $DeployPath -File -Recurse).Count
$dllCount = (Get-ChildItem -Path $DeployPath -Filter "*.dll" -Recurse).Count
$totalSize = [math]::Round((Get-ChildItem -Path $DeployPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)

Write-Host "`nDeployment Summary:" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green
Write-Host "Location: $(Resolve-Path $DeployPath)" -ForegroundColor White
Write-Host "Total Files: $totalFiles" -ForegroundColor White
Write-Host "DLL Files: $dllCount" -ForegroundColor White
Write-Host "Total Size: $totalSize MB" -ForegroundColor White
Write-Host "`nMain executable: SmartBoxNext.exe" -ForegroundColor Cyan
Write-Host "Start script: Start SmartBox.bat" -ForegroundColor Cyan

# Show what's in root directory
Write-Host "`nRoot directory contents:" -ForegroundColor Yellow
Get-ChildItem -Path $DeployPath -File | Select-Object -First 10 | ForEach-Object {
    Write-Host "  $($_.Name)" -ForegroundColor Gray
}
if ((Get-ChildItem -Path $DeployPath -File).Count -gt 10) {
    Write-Host "  ... and $((Get-ChildItem -Path $DeployPath -File).Count - 10) more files" -ForegroundColor Gray
}