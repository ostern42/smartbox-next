# PowerShell script to fix WebView2 file locks
# Usage: .\fix-locks.ps1

param(
    [switch]$Force,
    [switch]$Clean,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SmartBoxNext File Lock Fix Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    if ($Verbose) {
        Write-Host $Message -ForegroundColor $Color
    } else {
        Write-Host $Message -ForegroundColor $Color
    }
}

# Function to find WebView2 processes
function Find-WebView2Processes {
    $processes = @()
    
    # Find all msedgewebview2 processes
    $webViewProcesses = Get-Process -Name "msedgewebview2" -ErrorAction SilentlyContinue
    
    if ($webViewProcesses) {
        foreach ($process in $webViewProcesses) {
            try {
                # Try to get command line
                $commandLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($process.Id)").CommandLine
                
                if ($commandLine -and $commandLine.Contains("SmartBoxNext")) {
                    $processes += $process
                    Write-ColorOutput "Found WebView2 process: $($process.Id) - $($process.ProcessName)" "Yellow"
                }
            } catch {
                # If we can't get command line, add it anyway if it's recent
                if ($process.StartTime -and $process.StartTime -gt (Get-Date).AddHours(-1)) {
                    $processes += $process
                    Write-ColorOutput "Found potential WebView2 process: $($process.Id) - $($process.ProcessName)" "Yellow"
                }
            }
        }
    }
    
    return $processes
}

# Function to kill processes with timeout
function Kill-ProcessWithTimeout {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutMs = 5000
    )
    
    try {
        Write-ColorOutput "Attempting to kill process $($Process.Id) ($($Process.ProcessName))" "Red"
        
        # Try graceful shutdown first
        $Process.CloseMainWindow()
        
        # Wait for graceful shutdown
        if ($Process.WaitForExit($TimeoutMs)) {
            Write-ColorOutput "Process $($Process.Id) exited gracefully" "Green"
            return $true
        } else {
            Write-ColorOutput "Process $($Process.Id) did not exit gracefully, forcing kill" "Yellow"
            $Process.Kill($true)  # Kill entire process tree
            
            # Wait a bit more
            Start-Sleep -Milliseconds 500
            
            if ($Process.HasExited) {
                Write-ColorOutput "Process $($Process.Id) force killed successfully" "Green"
                return $true
            } else {
                Write-ColorOutput "Failed to kill process $($Process.Id)" "Red"
                return $false
            }
        }
    } catch {
        Write-ColorOutput "Error killing process $($Process.Id): $($_.Exception.Message)" "Red"
        return $false
    }
}

# Function to check for locked files using handle.exe (if available)
function Check-LockedFiles {
    param(
        [string]$Path
    )
    
    # Check if handle.exe is available
    $handlePath = Get-Command "handle.exe" -ErrorAction SilentlyContinue
    if (-not $handlePath) {
        Write-ColorOutput "handle.exe not found, skipping locked file check" "Yellow"
        return
    }
    
    try {
        Write-ColorOutput "Checking for locked files in $Path" "Cyan"
        $output = & handle.exe $Path 2>&1
        
        if ($output) {
            Write-ColorOutput "Locked files found:" "Red"
            $output | Write-Host
        } else {
            Write-ColorOutput "No locked files found" "Green"
        }
    } catch {
        Write-ColorOutput "Error checking locked files: $($_.Exception.Message)" "Red"
    }
}

# Function to clean bin/obj folders
function Clean-BuildFolders {
    param(
        [string]$ProjectPath = "."
    )
    
    Write-ColorOutput "Cleaning build folders..." "Cyan"
    
    $foldersToClean = @("bin", "obj")
    
    foreach ($folder in $foldersToClean) {
        $folderPath = Join-Path $ProjectPath $folder
        
        if (Test-Path $folderPath) {
            try {
                Write-ColorOutput "Removing $folderPath" "Yellow"
                Remove-Item $folderPath -Recurse -Force -ErrorAction Stop
                Write-ColorOutput "Removed $folderPath" "Green"
            } catch {
                Write-ColorOutput "Error removing $folderPath: $($_.Exception.Message)" "Red"
                
                # Try to remove files individually
                if (Test-Path $folderPath) {
                    Get-ChildItem $folderPath -Recurse -File | ForEach-Object {
                        try {
                            Remove-Item $_.FullName -Force -ErrorAction Stop
                        } catch {
                            Write-ColorOutput "Could not remove $($_.FullName): $($_.Exception.Message)" "Red"
                        }
                    }
                }
            }
        }
    }
}

# Function to clean WebView2 user data
function Clean-WebView2UserData {
    $userDataPaths = @(
        "$env:LOCALAPPDATA\SmartBoxNext\WebView2",
        ".\WebView2Data"
    )
    
    foreach ($path in $userDataPaths) {
        if (Test-Path $path) {
            Write-ColorOutput "Cleaning WebView2 user data: $path" "Cyan"
            
            try {
                # Kill any processes using this folder
                $processes = Find-WebView2Processes
                foreach ($process in $processes) {
                    Kill-ProcessWithTimeout -Process $process
                }
                
                # Wait a bit
                Start-Sleep -Seconds 1
                
                # Remove the folder
                Remove-Item $path -Recurse -Force -ErrorAction Stop
                Write-ColorOutput "Removed WebView2 user data: $path" "Green"
            } catch {
                Write-ColorOutput "Error cleaning WebView2 user data: $($_.Exception.Message)" "Red"
            }
        }
    }
}

# Main script execution
Write-ColorOutput "Starting file lock fix process..." "Green"

# Step 1: Find and kill WebView2 processes
Write-ColorOutput "Step 1: Finding WebView2 processes..." "Cyan"
$webViewProcesses = Find-WebView2Processes

if ($webViewProcesses.Count -eq 0) {
    Write-ColorOutput "No WebView2 processes found" "Green"
} else {
    Write-ColorOutput "Found $($webViewProcesses.Count) WebView2 processes" "Yellow"
    
    foreach ($process in $webViewProcesses) {
        Kill-ProcessWithTimeout -Process $process
    }
    
    # Wait for processes to fully exit
    Write-ColorOutput "Waiting for processes to exit..." "Cyan"
    Start-Sleep -Seconds 2
}

# Step 2: Check for locked files (if handle.exe is available)
Write-ColorOutput "Step 2: Checking for locked files..." "Cyan"
Check-LockedFiles -Path "."

# Step 3: Clean bin/obj folders
Write-ColorOutput "Step 3: Cleaning build folders..." "Cyan"
Clean-BuildFolders

# Step 4: Clean WebView2 user data (if requested)
if ($Clean) {
    Write-ColorOutput "Step 4: Cleaning WebView2 user data..." "Cyan"
    Clean-WebView2UserData
}

# Step 5: Force garbage collection
Write-ColorOutput "Step 5: Forcing garbage collection..." "Cyan"
[System.GC]::Collect()
[System.GC]::WaitForPendingFinalizers()
[System.GC]::Collect()

# Wait a bit more
Start-Sleep -Seconds 1

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "File lock fix process completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now try to build the project:" -ForegroundColor White
Write-Host "  dotnet build" -ForegroundColor Yellow
Write-Host ""
Write-Host "If you still have issues, try:" -ForegroundColor White
Write-Host "  .\fix-locks.ps1 -Clean -Force" -ForegroundColor Yellow
Write-Host ""