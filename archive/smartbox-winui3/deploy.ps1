# SmartBox Next Deployment Script
# Creates a clean, portable application structure

param(
    [string]$SourcePath = ".\bin\x64\Debug\net8.0-windows10.0.19041.0",
    [string]$TargetPath = ".\SmartBoxNext-Portable",
    [switch]$IncludeDebugFiles = $false
)

Write-Host "SmartBox Next Deployment Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Clean target directory
if (Test-Path $TargetPath) {
    Write-Host "Cleaning existing deployment directory..." -ForegroundColor Yellow
    Remove-Item -Path $TargetPath -Recurse -Force
}

# Create directory structure
Write-Host "Creating deployment structure..." -ForegroundColor Green
$directories = @(
    "$TargetPath",
    "$TargetPath\assets",
    "$TargetPath\assets\runtime",
    "$TargetPath\wwwroot",
    "$TargetPath\Data",
    "$TargetPath\Data\Photos",
    "$TargetPath\Data\Videos",
    "$TargetPath\Data\DICOM",
    "$TargetPath\Data\Queue",
    "$TargetPath\Data\Temp",
    "$TargetPath\logs"
)

foreach ($dir in $directories) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# Copy main executable and config
Write-Host "Copying main application files..." -ForegroundColor Green
Copy-Item "$SourcePath\SmartBoxNext.exe" -Destination $TargetPath
Copy-Item "$SourcePath\SmartBoxNext.exe.manifest" -Destination $TargetPath -ErrorAction SilentlyContinue
Copy-Item "$SourcePath\app.manifest" -Destination $TargetPath -ErrorAction SilentlyContinue

# Copy or create config
if (Test-Path "$SourcePath\config.json") {
    Copy-Item "$SourcePath\config.json" -Destination $TargetPath
} else {
    # Create default config
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
    "remoteAeTitle": "PACS",
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
    "minimizeToTray": true,
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
'@ | Out-File -FilePath "$TargetPath\config.json" -Encoding UTF8
}

# Copy wwwroot
Write-Host "Copying web assets..." -ForegroundColor Green
Copy-Item "$SourcePath\wwwroot\*" -Destination "$TargetPath\wwwroot" -Recurse

# Move all DLLs to assets/runtime
Write-Host "Organizing runtime dependencies..." -ForegroundColor Green
$essentialDlls = @(
    "SmartBoxNext.dll",
    "SmartBoxNext.deps.json",
    "SmartBoxNext.runtimeconfig.json"
)

# Copy essential files to root
foreach ($file in $essentialDlls) {
    if (Test-Path "$SourcePath\$file") {
        Copy-Item "$SourcePath\$file" -Destination $TargetPath
    }
}

# Move all other DLLs to assets/runtime
Get-ChildItem -Path $SourcePath -Filter "*.dll" | ForEach-Object {
    if ($_.Name -ne "SmartBoxNext.dll") {
        Copy-Item $_.FullName -Destination "$TargetPath\assets\runtime"
    }
}

# Copy runtime files
Get-ChildItem -Path $SourcePath -Filter "*.json" | ForEach-Object {
    if ($_.Name -notin @("config.json", "SmartBoxNext.deps.json", "SmartBoxNext.runtimeconfig.json")) {
        Copy-Item $_.FullName -Destination "$TargetPath\assets\runtime"
    }
}

# Copy WebView2 loader if exists
if (Test-Path "$SourcePath\WebView2Loader.dll") {
    Copy-Item "$SourcePath\WebView2Loader.dll" -Destination "$TargetPath\assets\runtime"
}

# Copy runtimes folder if exists
if (Test-Path "$SourcePath\runtimes") {
    Write-Host "Copying platform-specific runtimes..." -ForegroundColor Green
    Copy-Item "$SourcePath\runtimes" -Destination "$TargetPath\assets" -Recurse
}

# Copy Microsoft.Windows.SDK.NET folder if exists
if (Test-Path "$SourcePath\Microsoft.Windows.SDK.NET") {
    Copy-Item "$SourcePath\Microsoft.Windows.SDK.NET" -Destination "$TargetPath\assets" -Recurse
}

# Create app.exe.config to redirect DLL loading
Write-Host "Creating runtime configuration..." -ForegroundColor Green
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <probing privatePath="assets\runtime;assets\runtimes" />
    </assemblyBinding>
  </runtime>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" />
  </startup>
</configuration>
'@ | Out-File -FilePath "$TargetPath\SmartBoxNext.exe.config" -Encoding UTF8

# Create start.bat
Write-Host "Creating launcher script..." -ForegroundColor Green
@'
@echo off
title SmartBox Next - Medical Imaging System
echo Starting SmartBox Next...
start "" "%~dp0SmartBoxNext.exe"
exit
'@ | Out-File -FilePath "$TargetPath\Start SmartBox.bat" -Encoding ASCII

# Create README
Write-Host "Creating documentation..." -ForegroundColor Green
@'
# SmartBox Next - Portable Edition

## Quick Start
1. Run "Start SmartBox.bat" or SmartBoxNext.exe
2. The application will start in fullscreen mode
3. Click "Init Webcam" to begin

## Directory Structure
- `/Data/Photos` - Captured photos
- `/Data/Videos` - Recorded videos  
- `/Data/DICOM` - DICOM exports
- `/logs` - Application logs
- `/assets` - Runtime dependencies (do not modify)
- `/wwwroot` - Web interface files

## Configuration
Edit `config.json` to change settings or use the Settings button in the app.

## Requirements
- Windows 10/11 (64-bit)
- .NET 8 Runtime (included)
- WebView2 Runtime (auto-downloads if needed)
- Webcam device

## Support
Logs are stored in the `/logs` folder with daily rotation.
'@ | Out-File -FilePath "$TargetPath\README.txt" -Encoding UTF8

# Count files
$fileCount = (Get-ChildItem -Path $TargetPath -Recurse -File).Count
$size = [math]::Round((Get-ChildItem -Path $TargetPath -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)

Write-Host "`nDeployment Complete!" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host "Location: $TargetPath" -ForegroundColor Cyan
Write-Host "Total Files: $fileCount" -ForegroundColor Cyan
Write-Host "Total Size: $size MB" -ForegroundColor Cyan
Write-Host "`nDirectory Structure:" -ForegroundColor Yellow

# Show tree structure
tree $TargetPath /F | Select-Object -First 30

Write-Host "`nTo run the application:" -ForegroundColor Yellow
Write-Host "1. Navigate to: $TargetPath" -ForegroundColor White
Write-Host "2. Run: Start SmartBox.bat" -ForegroundColor White