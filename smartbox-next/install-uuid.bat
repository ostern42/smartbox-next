@echo off
echo Installing UUID library...

go get github.com/google/uuid
go mod tidy

echo Done!
pause