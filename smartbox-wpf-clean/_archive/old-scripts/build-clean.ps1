# Build script with automatic cleanup
# Usage: .\build-clean.ps1 [Configuration]

param(
    [string]$Configuration = "Debug",
    [switch]$Verbose,
    [switch]$Force
)

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SmartBoxNext Clean Build Script" -ForegroundColor Cyan
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

# Function to run command with error handling
function Run-Command {
    param(
        [string]$Command,
        [string]$Description
    )
    
    Write-ColorOutput "Running: $Description" "Cyan"
    Write-ColorOutput "Command: $Command" "Gray"
    
    try {
        $output = Invoke-Expression $Command 2>&1
        $exitCode = $LASTEXITCODE
        
        if ($exitCode -eq 0) {
            Write-ColorOutput "✓ $Description completed successfully" "Green"
            if ($Verbose -and $output) {
                Write-ColorOutput "Output:" "Gray"
                $output | Write-Host
            }
            return $true
        } else {
            Write-ColorOutput "✗ $Description failed with exit code: $exitCode" "Red"
            if ($output) {
                Write-ColorOutput "Error output:" "Red"
                $output | Write-Host
            }
            return $false
        }
    } catch {
        Write-ColorOutput "✗ $Description failed with exception: $($_.Exception.Message)" "Red"
        return $false
    }
}

# Step 1: Kill processes and clean locks
Write-ColorOutput "Step 1: Cleaning file locks..." "Yellow"
if (Test-Path ".\fix-locks.ps1") {
    & ".\fix-locks.ps1" -Clean -Verbose:$Verbose
} else {
    Write-ColorOutput "Warning: fix-locks.ps1 not found, skipping cleanup" "Yellow"
}

# Step 2: Clean solution
Write-ColorOutput "Step 2: Cleaning solution..." "Yellow"
$success = Run-Command "dotnet clean --configuration $Configuration --verbosity minimal" "Solution clean"
if (-not $success) {
    Write-ColorOutput "Clean failed, but continuing..." "Yellow"
}

# Step 3: Restore packages
Write-ColorOutput "Step 3: Restoring packages..." "Yellow"
$success = Run-Command "dotnet restore --verbosity minimal" "Package restore"
if (-not $success) {
    Write-ColorOutput "Restore failed, aborting build" "Red"
    exit 1
}

# Step 4: Build solution
Write-ColorOutput "Step 4: Building solution..." "Yellow"
$success = Run-Command "dotnet build --configuration $Configuration --no-restore --verbosity minimal" "Solution build"

if ($success) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Build completed successfully in $Configuration configuration" -ForegroundColor Green
    Write-Host "Executable location: .\bin\$Configuration\net8.0-windows\SmartBoxNext.exe" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Build failed. Check the error messages above." -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Try running: .\fix-locks.ps1 -Clean -Force" -ForegroundColor White
    Write-Host "2. Restart Visual Studio if open" -ForegroundColor White
    Write-Host "3. Try building again" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Step 5: Optional - show recent build logs
if ($Verbose) {
    Write-ColorOutput "Recent build artifacts:" "Cyan"
    Get-ChildItem -Path ".\bin\$Configuration" -Recurse -File | Select-Object Name, LastWriteTime, Length | Sort-Object LastWriteTime -Descending | Select-Object -First 10 | Format-Table -AutoSize
}

Write-Host "Build script completed!" -ForegroundColor Green