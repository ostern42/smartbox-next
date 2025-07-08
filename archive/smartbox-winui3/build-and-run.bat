@echo off
echo SmartBox Next - Build and Run
echo =============================
echo.

REM Check for MSBuild or dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found in PATH!
    echo Please install .NET 8 SDK or run from Developer Command Prompt
    pause
    exit /b 1
)

REM Restore packages
echo Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Package restore failed!
    pause
    exit /b 1
)

REM Build the project
echo.
echo Building SmartBox Next...
dotnet build -c Debug
if %errorlevel% neq 0 (
    echo ERROR: Build failed!
    echo.
    echo Try building in Visual Studio for more detailed error messages.
    pause
    exit /b 1
)

REM Run the application
echo.
echo Build successful! Starting application...
echo.
call run.bat