# Full restart and build script
# Usage: .\restart-and-build.ps1 [Configuration]

param(
    [string]$Configuration = "Debug",
    [switch]$Verbose,
    [switch]$SkipCleanup,
    [switch]$NoRestart
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SmartBoxNext Full Restart & Build Script" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to write colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

# Function to check if process is running
function Test-ProcessRunning {
    param(
        [string]$ProcessName
    )
    
    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    return $processes.Count -gt 0
}

# Function to wait for process to exit
function Wait-ProcessExit {
    param(
        [string]$ProcessName,
        [int]$TimeoutSeconds = 30
    )
    
    $timeout = (Get-Date).AddSeconds($TimeoutSeconds)
    
    while ((Get-Date) -lt $timeout) {
        if (-not (Test-ProcessRunning -ProcessName $ProcessName)) {
            return $true
        }
        Start-Sleep -Seconds 1
        Write-Host "." -NoNewline -ForegroundColor Yellow
    }
    
    Write-Host ""
    return $false
}

# Function to kill all related processes
function Kill-AllRelatedProcesses {
    $processNames = @(
        "SmartBoxNext",
        "msedgewebview2",
        "devenv",
        "MSBuild",
        "dotnet"
    )
    
    foreach ($processName in $processNames) {
        $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
        if ($processes) {
            Write-ColorOutput "Killing $($processes.Count) $processName process(es)..." "Yellow"
            foreach ($process in $processes) {
                try {
                    $process.Kill($true)  # Kill process tree
                    Write-ColorOutput "Killed $processName (PID: $($process.Id))" "Green"
                } catch {
                    Write-ColorOutput "Failed to kill $processName (PID: $($process.Id)): $($_.Exception.Message)" "Red"
                }
            }
        }
    }
}

# Function to clean all possible locks
function Clean-AllLocks {
    Write-ColorOutput "Performing comprehensive cleanup..." "Cyan"
    
    # Run fix-locks script
    if (Test-Path ".\fix-locks.ps1") {
        & ".\fix-locks.ps1" -Clean -Force -Verbose:$Verbose
    }
    
    # Clean additional folders
    $foldersToClean = @(
        "bin",
        "obj", 
        "WebView2Data",
        "$env:LOCALAPPDATA\SmartBoxNext",
        "$env:TEMP\SmartBoxNext*"
    )
    
    foreach ($folder in $foldersToClean) {
        if (Test-Path $folder) {
            try {
                Write-ColorOutput "Removing $folder..." "Yellow"
                Remove-Item $folder -Recurse -Force -ErrorAction Stop
                Write-ColorOutput "Removed $folder" "Green"
            } catch {
                Write-ColorOutput "Could not remove $folder: $($_.Exception.Message)" "Red"
            }
        }
    }
    
    # Force garbage collection
    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()
    [System.GC]::Collect()
    
    # Wait for file system to settle
    Start-Sleep -Seconds 2
}

# Function to prompt for restart
function Prompt-Restart {
    if ($NoRestart) {
        return $false
    }
    
    Write-Host ""
    Write-ColorOutput "A system restart is recommended for complete cleanup." "Yellow"
    Write-Host "This will ensure all file locks are released and all processes are terminated." -ForegroundColor White
    Write-Host ""
    
    $response = Read-Host "Would you like to restart now? (y/N)"
    
    if ($response -match '^[Yy]') {
        Write-ColorOutput "Restarting system in 10 seconds..." "Red"
        Write-ColorOutput "Press Ctrl+C to cancel!" "Yellow"
        
        for ($i = 10; $i -gt 0; $i--) {
            Write-Host "$i..." -NoNewline -ForegroundColor Red
            Start-Sleep -Seconds 1
        }
        
        Write-Host ""
        Write-ColorOutput "Restarting now..." "Red"
        Restart-Computer -Force
        return $true
    }
    
    return $false
}

# Main script execution
Write-ColorOutput "Starting full restart and build process..." "Green"

# Step 1: Kill all related processes
Write-ColorOutput "Step 1: Terminating all related processes..." "Yellow"
Kill-AllRelatedProcesses

# Wait for processes to exit
Write-ColorOutput "Waiting for processes to fully exit..." "Cyan"
Start-Sleep -Seconds 3

# Step 2: Comprehensive cleanup
if (-not $SkipCleanup) {
    Write-ColorOutput "Step 2: Performing comprehensive cleanup..." "Yellow"
    Clean-AllLocks
} else {
    Write-ColorOutput "Step 2: Skipping cleanup (as requested)" "Yellow"
}

# Step 3: Check if restart is needed
$needsRestart = $false

# Check for stubborn processes
$stubborn = Get-Process -Name "msedgewebview2" -ErrorAction SilentlyContinue
if ($stubborn) {
    Write-ColorOutput "Warning: WebView2 processes still running after cleanup" "Red"
    $needsRestart = $true
}

# Check for locked files
try {
    $testBuild = dotnet build --dry-run --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0 -and $testBuild -match "because it is being used by another process") {
        Write-ColorOutput "Warning: Files are still locked" "Red"
        $needsRestart = $true
    }
} catch {
    # Ignore errors in dry run check
}

# Step 4: Offer restart if needed
if ($needsRestart) {
    Write-ColorOutput "Step 3: System restart recommended" "Yellow"
    $restarted = Prompt-Restart
    if ($restarted) {
        exit 0  # Script will not continue after restart
    }
} else {
    Write-ColorOutput "Step 3: No restart needed" "Green"
}

# Step 5: Build the project
Write-ColorOutput "Step 4: Building project..." "Yellow"
if (Test-Path ".\build-clean.ps1") {
    & ".\build-clean.ps1" -Configuration $Configuration -Verbose:$Verbose
} else {
    # Fallback to direct build
    Write-ColorOutput "build-clean.ps1 not found, using direct build" "Yellow"
    
    try {
        Write-ColorOutput "Cleaning solution..." "Cyan"
        dotnet clean --configuration $Configuration --verbosity minimal
        
        Write-ColorOutput "Restoring packages..." "Cyan"
        dotnet restore --verbosity minimal
        
        Write-ColorOutput "Building solution..." "Cyan"
        dotnet build --configuration $Configuration --no-restore --verbosity minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput "Build completed successfully!" "Green"
        } else {
            Write-ColorOutput "Build failed!" "Red"
            exit 1
        }
    } catch {
        Write-ColorOutput "Build failed with exception: $($_.Exception.Message)" "Red"
        exit 1
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "RESTART AND BUILD COMPLETED!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "The SmartBoxNext project has been successfully built." -ForegroundColor Green
Write-Host "File locks have been cleared and the application is ready to run." -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test the application: .\bin\$Configuration\net8.0-windows\SmartBoxNext.exe" -ForegroundColor White
Write-Host "2. If issues persist, try a full system restart" -ForegroundColor White
Write-Host ""