@echo off
echo Aggressive file lock fix...

:: Kill ALL related processes
taskkill /F /IM SmartBoxNext.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul
taskkill /F /IM msedgewebview2.exe 2>nul
taskkill /F /IM devenv.exe 2>nul
taskkill /F /IM MSBuild.exe 2>nul
taskkill /F /IM vbcscompiler.exe 2>nul

:: Wait longer
timeout /t 5 /nobreak >nul

:: Try to rename bin folder (if it works, we can delete it)
move bin bin_old 2>nul
if exist bin_old (
    rmdir /s /q bin_old
    echo Successfully removed bin folder
) else (
    echo WARNING: bin folder still locked!
)

:: Same for obj
move obj obj_old 2>nul
if exist obj_old (
    rmdir /s /q obj_old
    echo Successfully removed obj folder
) else (
    echo WARNING: obj folder still locked!
)

echo Done!