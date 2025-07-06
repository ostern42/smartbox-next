@echo off
echo Installing missing dependencies...

REM Remove the problematic dependency first
go mod edit -droprequire github.com/kristianvalind/go-netdicom-port

REM Try to add it with latest version
go get github.com/kristianvalind/go-netdicom-port@latest

REM If that fails, we'll handle it manually
if errorlevel 1 (
    echo.
    echo Note: go-netdicom-port installation failed. We'll use a stub for now.
)

go get golang.org/x/sys/windows
go mod tidy

echo Done!
pause