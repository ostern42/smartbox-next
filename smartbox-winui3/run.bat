@echo off
title SmartBox Next Launcher
echo Starting SmartBox Next...
echo ================================
echo.

cd /d "%~dp0"

REM Check if we need to go to bin directory
if exist "bin\x64\Debug\net8.0-windows10.0.19041.0\SmartBoxNext.exe" (
    echo Navigating to build directory...
    cd "bin\x64\Debug\net8.0-windows10.0.19041.0"
) else if not exist "SmartBoxNext.exe" (
    echo ERROR: SmartBoxNext.exe not found!
    echo Please build the project in Visual Studio first.
    pause
    exit /b 1
)

REM Check for WebView2
echo Checking dependencies...
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo WARNING: WebView2 Runtime might not be installed!
    echo The application requires Microsoft Edge WebView2 Runtime.
    echo.
    echo Download from: https://go.microsoft.com/fwlink/p/?LinkId=2124703
    echo.
    choice /C YN /M "Continue anyway"
    if errorlevel 2 exit /b 1
)

REM Check if wwwroot exists
if not exist "wwwroot\index.html" (
    echo ERROR: wwwroot folder not found!
    echo The web interface files are missing.
    pause
    exit /b 1
)

REM Start the application
echo.
echo Launching SmartBox Next...
echo.

REM Run directly (not with start) to see any error messages
SmartBoxNext.exe

REM Check exit code
if %ERRORLEVEL% neq 0 (
    echo.
    echo ==========================================
    echo Application exited with error code: %ERRORLEVEL%
    echo ==========================================
    
    REM Check for logs
    if exist "logs" (
        echo.
        echo Checking logs...
        for /f "delims=" %%i in ('dir /b /od logs\*.log 2^>nul') do set "latest=%%i"
        if defined latest (
            echo Latest log file: logs\%latest%
            echo.
            echo Last 10 lines:
            echo --------------
            powershell -Command "Get-Content logs\%latest% -Tail 10"
        )
    )
    
    REM Check for startup error log
    if exist "startup_error.log" (
        echo.
        echo Startup Error Log:
        echo ------------------
        type startup_error.log
    )
)

echo.
pause