@echo off
REM SmartBox Next - Manual Setup Commands
REM Falls PowerShell Script nicht funktioniert, diese Befehle einzeln ausführen

echo === SmartBox Next Setup ===
echo.

cd C:\Users\oliver.stern\source\repos\smartbox-next

echo Step 1: Initialize Wails Project
wails init -n smartbox-next -t vue -y
timeout /t 3

echo.
echo Step 2: Create Backend Structure
cd smartbox-next
mkdir backend\capture
mkdir backend\dicom
mkdir backend\overlay
mkdir backend\license
mkdir backend\api
mkdir backend\trigger

echo.
echo Step 3: Install Go Dependencies
go get github.com/suyashkumar/dicom
go get github.com/gin-gonic/gin
go get github.com/gorilla/websocket

echo.
echo Step 4: Install Frontend Dependencies
cd frontend
call npm install
call npm install pinia @vueuse/core
call npm install -D @types/node tailwindcss autoprefixer postcss

cd ..

echo.
echo === Setup Complete! ===
echo Run 'wails dev' to start
pause