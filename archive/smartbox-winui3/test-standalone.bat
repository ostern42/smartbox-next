@echo off
title SmartBox Standalone Test
echo SmartBox Next - Standalone Execution Test
echo =========================================
echo.

cd /d "%~dp0"

REM Navigate to debug directory
cd "bin\x64\Debug\net8.0-windows10.0.19041.0"

echo Current directory: %CD%
echo.

REM Check for required files
echo Checking required files...
echo --------------------------

if exist "SmartBoxNext.exe" (
    echo [OK] SmartBoxNext.exe found
) else (
    echo [ERROR] SmartBoxNext.exe NOT FOUND!
)

if exist "wwwroot\index.html" (
    echo [OK] wwwroot\index.html found
) else (
    echo [ERROR] wwwroot\index.html NOT FOUND!
)

if exist "config.json" (
    echo [OK] config.json found
) else (
    echo [WARNING] config.json not found - will use defaults
)

if exist "Microsoft.Web.WebView2.Core.dll" (
    echo [OK] WebView2 Core DLL found
) else (
    echo [ERROR] WebView2 Core DLL NOT FOUND!
)

echo.
echo Checking WebView2 Runtime...
echo ----------------------------
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] WebView2 Runtime is installed
) else (
    echo [WARNING] WebView2 Runtime might not be installed
)

echo.
echo Starting SmartBoxNext with verbose output...
echo -------------------------------------------
echo.

REM Create a startup log
echo Starting at %DATE% %TIME% > startup_test.log

REM Run with output capture
SmartBoxNext.exe 2>&1 | tee -a startup_test.log

echo.
echo Exit code: %ERRORLEVEL%
echo.

if %ERRORLEVEL% neq 0 (
    echo Application failed to start properly.
    echo Check startup_test.log for details.
    
    if exist "logs" (
        echo.
        echo Latest log entries:
        powershell -Command "Get-Content (Get-ChildItem logs\*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 20"
    )
)

echo.
pause