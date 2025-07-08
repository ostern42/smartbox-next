# Find and kill processes locking SmartBox files
Write-Host "=== Fixing file locks for SmartBox-Next ===" -ForegroundColor Cyan

# Kill common development processes
$processesToKill = @("devenv", "MSBuild", "dotnet", "SmartBoxNext", "VBCSCompiler")

foreach ($proc in $processesToKill) {
    $found = Get-Process -Name $proc -ErrorAction SilentlyContinue
    if ($found) {
        Write-Host "Killing $proc processes..." -ForegroundColor Yellow
        Stop-Process -Name $proc -Force -ErrorAction SilentlyContinue
    }
}

# Wait a moment
Start-Sleep -Seconds 2

# Try to remove obj and bin folders
Write-Host "Removing build folders..." -ForegroundColor Yellow
Remove-Item -Path ".\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".\bin" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Done! Try building again." -ForegroundColor Green