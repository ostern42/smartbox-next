# Test if SmartBoxNext.exe is running
$process = Get-Process -Name "SmartBoxNext" -ErrorAction SilentlyContinue

if ($process) {
    Write-Host "SmartBoxNext is running!" -ForegroundColor Green
    Write-Host "Process ID: $($process.Id)"
    Write-Host "Window Title: $($process.MainWindowTitle)"
    Write-Host "Has Window: $($process.MainWindowHandle -ne 0)"
    
    if ($process.MainWindowHandle -eq 0) {
        Write-Host "`nWARNING: Process is running but has no visible window!" -ForegroundColor Yellow
        Write-Host "This could mean:"
        Write-Host "- The window is minimized"
        Write-Host "- The window failed to initialize"
        Write-Host "- The app is running in background"
    }
} else {
    Write-Host "SmartBoxNext is not running" -ForegroundColor Red
}

# Check for error logs
$errorLog = Join-Path (Get-Location) "startup_error.log"
if (Test-Path $errorLog) {
    Write-Host "`nStartup error log found:" -ForegroundColor Red
    Get-Content $errorLog
}

# Check logs directory
$logsDir = Join-Path (Get-Location) "logs"
if (Test-Path $logsDir) {
    $latestLog = Get-ChildItem $logsDir -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "`nLatest log entries:" -ForegroundColor Cyan
        Get-Content $latestLog.FullName | Select-Object -Last 20
    }
}