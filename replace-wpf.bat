@echo off
echo Replacing locked smartbox-wpf with clean version...

:: Kill any processes that might be locking files
taskkill /F /IM SmartBoxNext.exe 2>nul
taskkill /F /IM dotnet.exe 2>nul
timeout /t 2 /nobreak >nul

:: Rename old directory
echo Renaming old directory...
move smartbox-wpf smartbox-wpf-locked

:: Rename clean directory to original name
echo Renaming clean directory...
move smartbox-wpf-clean smartbox-wpf

echo Done! The clean build is now in smartbox-wpf
echo You can delete smartbox-wpf-locked later when files are unlocked