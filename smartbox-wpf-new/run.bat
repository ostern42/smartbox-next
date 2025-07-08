@echo off
echo Starting SmartBox Next WPF...
echo.

if not exist bin\Debug\net8.0-windows\SmartBoxNext.exe (
    echo ERROR: Application not built. Run build.bat first!
    pause
    exit /b 1
)

cd bin\Debug\net8.0-windows
start SmartBoxNext.exe
cd ..\..\..

echo SmartBox Next started.
pause