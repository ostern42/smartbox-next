# SmartBox Next - Fixed Setup Script
# Führe dieses Script in PowerShell aus

Write-Host "🚀 SmartBox Next Setup Starting..." -ForegroundColor Green

# Schritt 1: Ins richtige Verzeichnis wechseln
$projectPath = "C:\Users\oliver.stern\source\repos\smartbox-next"
Set-Location $projectPath
Write-Host "📁 Working in: $projectPath" -ForegroundColor Yellow

# Schritt 2: Wails Projekt initialisieren (ohne -y flag)
Write-Host "`n📦 Initializing Wails project..." -ForegroundColor Green
wails init -n smartbox-next -t vue

# Warte kurz
Start-Sleep -Seconds 2

# Schritt 3: Backend-Struktur erstellen
Write-Host "`n📁 Creating backend structure..." -ForegroundColor Green
$dirs = @(
    "smartbox-next\backend\capture",
    "smartbox-next\backend\dicom",
    "smartbox-next\backend\overlay",
    "smartbox-next\backend\license",
    "smartbox-next\backend\api",
    "smartbox-next\backend\trigger"
)

foreach ($dir in $dirs) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

# Schritt 4: In Projekt-Verzeichnis wechseln
Set-Location "smartbox-next"

# Schritt 5: Go mod initialisieren (das fehlte!)
Write-Host "`n📦 Initializing Go module..." -ForegroundColor Green
go mod init smartbox-next
go mod tidy

# Schritt 6: Go Dependencies installieren
Write-Host "`n📦 Installing Go dependencies..." -ForegroundColor Green
go get github.com/suyashkumar/dicom
go get github.com/gin-gonic/gin
go get github.com/gorilla/websocket

# Schritt 7: Frontend Dependencies
Write-Host "`n📦 Installing Frontend dependencies..." -ForegroundColor Green
Set-Location "frontend"
npm install
npm install pinia @vueuse/core
npm install -D @types/node tailwindcss autoprefixer postcss

# Zurück zum Hauptverzeichnis
Set-Location ..

Write-Host "`n✅ Setup Complete!" -ForegroundColor Green
Write-Host "📝 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Copy app.go.template to app.go" -ForegroundColor White
Write-Host "   2. Copy App.vue.template to frontend/src/App.vue" -ForegroundColor White
Write-Host "   3. Run 'wails dev' to start development server" -ForegroundColor White
Write-Host "`n🎉 Happy coding!" -ForegroundColor Magenta