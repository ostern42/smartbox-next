@echo off
echo Starting SmartBox Next directly...
echo.

cd /d "%~dp0"

REM Check if in bin directory
if exist "bin\x64\Debug\net8.0-windows10.0.19041.0\SmartBoxNext.exe" (
    echo Starting from project directory...
    cd "bin\x64\Debug\net8.0-windows10.0.19041.0"
)

REM Check for WebView2
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: WebView2 Runtime might not be installed!
    echo Download from: https://go.microsoft.com/fwlink/p/?LinkId=2124703
    echo.
)

REM Check if port 5000 is in use
netstat -an | findstr :5000 >nul 2>&1
if %errorlevel% equ 0 (
    echo WARNING: Port 5000 might be in use!
    echo.
)

echo Starting SmartBoxNext.exe...
start "" "SmartBoxNext.exe"

timeout /t 3 >nul

REM Check if running
tasklist | findstr SmartBoxNext.exe >nul 2>&1
if %errorlevel% equ 0 (
    echo SmartBox is running!
) else (
    echo ERROR: SmartBox failed to start!
    echo.
    echo Check for errors:
    if exist startup_error.log (
        echo.
        type startup_error.log
    )
)

pause