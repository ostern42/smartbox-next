# SmartBox DICOM Test Script
# Nutzt dcmtk um generierte DICOMs zu validieren

param(
    [string]$DicomPath = "./bin/Debug/net8.0-windows/Data/DICOM",
    [switch]$Verbose
)

Write-Host "SmartBox DICOM Validation Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if dcmtk is installed
$dcmdump = Get-Command dcmdump -ErrorAction SilentlyContinue
if (-not $dcmdump) {
    Write-Host "ERROR: dcmtk not found! Please install dcmtk first." -ForegroundColor Red
    Write-Host "Install with: choco install dcmtk" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nUsing dcmtk from: $($dcmdump.Source)" -ForegroundColor Green

# Check if DICOM directory exists
if (-not (Test-Path $DicomPath)) {
    Write-Host "`nDICOM directory not found: $DicomPath" -ForegroundColor Yellow
    Write-Host "No DICOM files to validate yet." -ForegroundColor Yellow
    exit 0
}

# Find all DICOM files
$dicomFiles = Get-ChildItem -Path $DicomPath -Filter "*.dcm" -Recurse
if ($dicomFiles.Count -eq 0) {
    Write-Host "`nNo DICOM files found in: $DicomPath" -ForegroundColor Yellow
    exit 0
}

Write-Host "`nFound $($dicomFiles.Count) DICOM file(s) to validate:" -ForegroundColor Green

$validCount = 0
$errorCount = 0

foreach ($file in $dicomFiles) {
    Write-Host "`n----------------------------------------"
    Write-Host "File: $($file.Name)" -ForegroundColor Cyan
    Write-Host "Path: $($file.FullName)"
    Write-Host "Size: $([math]::Round($file.Length / 1KB, 2)) KB"
    Write-Host "Created: $($file.CreationTime)"
    
    # Validate with dcmftest (DICOM file tester)
    Write-Host "`nValidating structure..." -NoNewline
    $validation = & dcmftest $file.FullName 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " VALID" -ForegroundColor Green
        $validCount++
        
        if ($Verbose) {
            Write-Host "`nDICOM Header (first 20 tags):" -ForegroundColor Yellow
            & dcmdump $file.FullName | Select-Object -First 20
        } else {
            # Show key tags
            Write-Host "`nKey DICOM Tags:" -ForegroundColor Yellow
            
            # Patient info
            $patientName = & dcmdump $file.FullName | Select-String "\(0010,0010\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            $patientID = & dcmdump $file.FullName | Select-String "\(0010,0020\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            $studyDate = & dcmdump $file.FullName | Select-String "\(0008,0020\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            $modality = & dcmdump $file.FullName | Select-String "\(0008,0060\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            
            Write-Host "  Patient Name: $patientName"
            Write-Host "  Patient ID: $patientID"
            Write-Host "  Study Date: $studyDate"
            Write-Host "  Modality: $modality"
            
            # Image info
            $rows = & dcmdump $file.FullName | Select-String "\(0028,0010\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            $cols = & dcmdump $file.FullName | Select-String "\(0028,0011\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            $photometric = & dcmdump $file.FullName | Select-String "\(0028,0004\)" | ForEach-Object { $_ -replace '.*\[(.+)\].*', '$1' }
            
            Write-Host "  Image Size: ${cols}x${rows}"
            Write-Host "  Photometric: $photometric"
        }
        
        # Test if image can be extracted
        Write-Host "`nTesting image extraction..." -NoNewline
        $tempJpg = [System.IO.Path]::GetTempFileName() + ".jpg"
        & dcmj2pnm +oj $file.FullName $tempJpg 2>&1 | Out-Null
        if (Test-Path $tempJpg) {
            Write-Host " SUCCESS" -ForegroundColor Green
            Remove-Item $tempJpg -Force
        } else {
            Write-Host " FAILED" -ForegroundColor Yellow
            Write-Host "  (Image might be in unsupported format)"
        }
        
    } else {
        Write-Host " INVALID" -ForegroundColor Red
        $errorCount++
        Write-Host "Validation errors:" -ForegroundColor Red
        $validation | Where-Object { $_ -match "Error" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
}

Write-Host "`n========================================"
Write-Host "Validation Summary:" -ForegroundColor Cyan
Write-Host "  Total files: $($dicomFiles.Count)"
Write-Host "  Valid: $validCount" -ForegroundColor Green
if ($errorCount -gt 0) {
    Write-Host "  Invalid: $errorCount" -ForegroundColor Red
}

# Test PACS connectivity if configured
Write-Host "`n========================================"
Write-Host "PACS Connectivity Test:" -ForegroundColor Cyan

$configPath = "./bin/Debug/net8.0-windows/config.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    if ($config.Pacs.ServerHost) {
        Write-Host "PACS configured: $($config.Pacs.ServerHost):$($config.Pacs.ServerPort)"
        Write-Host "Testing C-ECHO..." -NoNewline
        
        $result = & echoscu -aet $config.Pacs.CallingAeTitle -aec $config.Pacs.CalledAeTitle $config.Pacs.ServerHost $config.Pacs.ServerPort 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host " SUCCESS" -ForegroundColor Green
        } else {
            Write-Host " FAILED" -ForegroundColor Red
            Write-Host $result
        }
    } else {
        Write-Host "No PACS configured" -ForegroundColor Yellow
    }
} else {
    Write-Host "Config file not found" -ForegroundColor Yellow
}

Write-Host "`nDone!" -ForegroundColor Green