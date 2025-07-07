@echo off
echo.
echo Creating SmartBox Deployment Package...
echo ======================================
echo.

powershell -ExecutionPolicy Bypass -File deploy-simple.ps1

echo.
echo Deployment complete! Check SmartBoxNext-Deploy folder.
echo.
pause