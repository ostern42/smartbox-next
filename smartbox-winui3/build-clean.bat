@echo off
echo === SmartBox-Next Clean Build ===
echo.

echo Cleaning previous build...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo Building project...
dotnet build SmartBoxNext.csproj -p:Platform=x64

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed! Error code: %ERRORLEVEL%
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
echo Run with: run.bat
pause