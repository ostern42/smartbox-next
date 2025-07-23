@echo off
echo Testing SmartBox API...
echo =====================

echo.
echo 1. Testing API Health Check...
curl -s http://localhost:5002/api/health
echo.

echo.
echo 2. Testing Login...
curl -s -X POST http://localhost:5002/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"SmartBox2024!\"}"
echo.

echo.
echo 3. Testing Invalid Login...
curl -s -X POST http://localhost:5002/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"test\",\"password\":\"wrong\"}"
echo.

echo.
echo Test complete!
pause