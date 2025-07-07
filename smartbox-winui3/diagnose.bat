@echo off
echo SmartBox Next - Diagnostics
echo ===========================
echo.

cd /d "%~dp0"

echo Current Directory: %CD%
echo.

REM Check build output
if exist "bin\x64\Debug\net8.0-windows10.0.19041.0" (
    echo Build output found.
    cd "bin\x64\Debug\net8.0-windows10.0.19041.0"
    echo Working directory: %CD%
) else (
    echo Build output NOT found!
    echo Please build the project first.
    pause
    exit /b 1
)

echo.
echo Checking required files:
echo ------------------------

REM Check executable
if exist "SmartBoxNext.exe" (
    echo [OK] SmartBoxNext.exe
    for %%I in (SmartBoxNext.exe) do echo      Size: %%~zI bytes
) else (
    echo [MISSING] SmartBoxNext.exe
)

REM Check critical DLLs
for %%f in (SmartBoxNext.dll Microsoft.Web.WebView2.Core.dll WebView2Loader.dll) do (
    if exist "%%f" (
        echo [OK] %%f
    ) else (
        echo [MISSING] %%f
    )
)

REM Check wwwroot
if exist "wwwroot\index.html" (
    echo [OK] wwwroot\index.html
) else (
    echo [MISSING] wwwroot\index.html
)

REM Check config
if exist "config.json" (
    echo [OK] config.json
) else (
    echo [WARNING] config.json not found (will use defaults)
)

echo.
echo Checking WebView2:
echo ------------------
reg query "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] WebView2 Runtime is installed
) else (
    echo [MISSING] WebView2 Runtime NOT installed!
    echo.
    echo Download from: https://go.microsoft.com/fwlink/p/?LinkId=2124703
)

echo.
echo Checking .NET Runtime:
echo ----------------------
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8" >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] .NET 8 Desktop Runtime found
) else (
    echo [WARNING] .NET 8 Desktop Runtime might be missing
)

echo.
echo Checking ports:
echo ---------------
netstat -an | findstr :5000 >nul 2>&1
if %errorlevel% equ 0 (
    echo [WARNING] Port 5000 is already in use!
) else (
    echo [OK] Port 5000 is available
)

echo.
echo Recent logs:
echo ------------
if exist "logs" (
    for /f "delims=" %%i in ('dir /b /od logs\*.log 2^>nul') do set "latest=%%i"
    if defined latest (
        echo Latest log: logs\%latest%
        echo.
        powershell -Command "Get-Content logs\%latest% -Tail 5"
    ) else (
        echo No log files found
    )
) else (
    echo Logs directory not found
)

echo.
echo ===========================
echo Diagnostics complete.
echo.
echo If all checks pass but the app won't start:
echo 1. Try running from Visual Studio
echo 2. Check Event Viewer for application errors
echo 3. Run with administrator privileges
echo.
pause