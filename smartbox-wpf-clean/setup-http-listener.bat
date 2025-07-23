@echo off
echo SmartBox HTTP Listener Setup
echo ===========================
echo.
echo This script will configure Windows to allow the SmartBox API to listen on port 5002.
echo You must run this as Administrator.
echo.

:: Check for admin privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with Administrator privileges...
) else (
    echo ERROR: This script must be run as Administrator!
    echo Right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo.
echo Adding URL ACL reservations...

:: Add URL reservations for HttpListener
netsh http add urlacl url=http://+:5002/ user=Everyone
netsh http add urlacl url=http://localhost:5002/ user=Everyone
netsh http add urlacl url=http://127.0.0.1:5002/ user=Everyone

echo.
echo Checking existing URL reservations...
netsh http show urlacl | findstr 5002

echo.
echo Adding firewall rules...

:: Add firewall rule
netsh advfirewall firewall add rule name="SmartBox API Port 5002" dir=in action=allow protocol=TCP localport=5002

echo.
echo Checking firewall rules...
netsh advfirewall firewall show rule name="SmartBox API Port 5002"

echo.
echo Setup complete!
echo.
echo You may need to restart the SmartBox application for changes to take effect.
echo.
pause