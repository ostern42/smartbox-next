@echo off
echo Starting SmartBox HTML UI Test...
echo.
echo Opening test page in browser...
start http://localhost:5000/test.html
echo.
echo Opening main UI in browser...
start http://localhost:5000/
echo.
echo To test in the WinUI3 app, run the application from Visual Studio.
echo.
pause