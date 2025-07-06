@echo off
echo Building SmartBox Next (WinUI 3)...
echo.

dotnet build -c Debug

if errorlevel 1 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo Run the app from: bin\x64\Debug\net8.0-windows10.0.19041.0\SmartBoxNext.exe
echo.
pause