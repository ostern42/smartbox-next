@echo off
echo.
echo SmartBox Next - Creating Portable Deployment
echo ============================================
echo.

REM Check if PowerShell is available
where pwsh >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    pwsh -ExecutionPolicy Bypass -File deploy.ps1
) else (
    powershell -ExecutionPolicy Bypass -File deploy.ps1
)

echo.
echo Press any key to exit...
pause >nul