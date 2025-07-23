@echo off
echo SmartBox API Diagnostics
echo ========================
echo.

:: Check if running as admin
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Running with Administrator privileges
) else (
    echo [WARNING] Not running as Administrator
    echo Some tests may fail. Consider running as admin.
)

echo.
echo Checking port 5002...
netstat -an | findstr :5002
if %errorLevel% == 0 (
    echo [OK] Port 5002 is being used
) else (
    echo [ERROR] Port 5002 is NOT listening
)

echo.
echo Checking URL reservations...
netsh http show urlacl | findstr 5002
if %errorLevel% == 0 (
    echo [OK] URL reservations found for port 5002
) else (
    echo [WARNING] No URL reservations for port 5002
    echo Run setup-http-listener.bat as Administrator
)

echo.
echo Checking firewall rules...
netsh advfirewall firewall show rule name="SmartBox API Port 5002" >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Firewall rule exists for SmartBox API
) else (
    echo [WARNING] No firewall rule for SmartBox API
)

echo.
echo Testing API connectivity...
powershell -Command "try { $r = Invoke-WebRequest -Uri 'http://localhost:5002/api/health' -TimeoutSec 5 -UseBasicParsing; Write-Host '[OK] API is responding' -ForegroundColor Green } catch { Write-Host '[ERROR] API is not responding' -ForegroundColor Red; Write-Host $_.Exception.Message -ForegroundColor Gray }"

echo.
echo Checking for conflicting processes on port 5002...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5002 ^| findstr LISTENING') do (
    echo Process using port 5002 has PID: %%a
    tasklist /fi "pid eq %%a" /fo list
)

echo.
echo ========================================
echo Diagnostics complete.
echo.
echo If API is not working:
echo 1. Run setup-http-listener.bat as Administrator
echo 2. Restart SmartBoxNext.exe as Administrator
echo 3. Check console output for errors
echo ========================================
echo.
pause