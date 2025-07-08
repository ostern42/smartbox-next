@echo off
echo Fixing file locks...

:: Kill any running SmartBox processes
taskkill /F /IM SmartBoxNext.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul

:: Wait a moment
timeout /t 2 /nobreak >nul

:: Force delete locked files
del /F /Q "bin\Debug\net8.0-windows\runtimes\win-x86\native\WebView2Loader.dll" 2>nul
del /F /Q "bin\Debug\net8.0-windows\runtimes\win-x64\native\WebView2Loader.dll" 2>nul
del /F /Q "bin\Debug\net8.0-windows\runtimes\win-arm64\native\WebView2Loader.dll" 2>nul
del /F /Q "obj\project.nuget.cache" 2>nul

echo File locks cleared!