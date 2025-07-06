@echo off
echo Installing missing dependencies...

go get golang.org/x/sys/windows
go mod tidy

echo Done!
pause