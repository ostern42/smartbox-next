@echo off
echo Starting SmartBox Next directly...
cd /d "%~dp0"
start "" "bin\x64\Debug\net8.0-windows10.0.19041.0\SmartBoxNext.exe"
echo.
echo App should be running now!
timeout /t 3