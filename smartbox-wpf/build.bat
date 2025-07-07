@echo off
echo Building SmartBox Next WPF...
echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist bin\Debug\net8.0-windows rmdir /s /q bin\Debug\net8.0-windows
if exist bin\Release\net8.0-windows rmdir /s /q bin\Release\net8.0-windows

REM Restore packages
echo Restoring NuGet packages...
dotnet restore

REM Build Debug
echo.
echo Building Debug configuration...
dotnet build -c Debug

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Debug build failed!
    pause
    exit /b 1
)

REM Build Release
echo.
echo Building Release configuration...
dotnet build -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Release build failed!
    pause
    exit /b 1
)

echo.
echo Build completed successfully!
echo.
echo Debug output: bin\Debug\net8.0-windows\
echo Release output: bin\Release\net8.0-windows\
echo.
pause