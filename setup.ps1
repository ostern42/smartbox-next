# SmartBox Next - Automated Setup Script
# Führe dieses Script in PowerShell aus (nicht WSL!)

Write-Host "🚀 SmartBox Next Setup Starting..." -ForegroundColor Green

# Schritt 1: Ins richtige Verzeichnis wechseln
$projectPath = "C:\Users\oliver.stern\source\repos\smartbox-next"
Set-Location $projectPath
Write-Host "📁 Working in: $projectPath" -ForegroundColor Yellow

# Schritt 2: Wails Projekt initialisieren
Write-Host "`n📦 Initializing Wails project..." -ForegroundColor Green
wails init -n smartbox-next -t vue -y

# Warte kurz
Start-Sleep -Seconds 2

# Schritt 3: Backend-Struktur erstellen
Write-Host "`n📁 Creating backend structure..." -ForegroundColor Green
New-Item -ItemType Directory -Force -Path @(
    "smartbox-next\backend\capture",
    "smartbox-next\backend\dicom",
    "smartbox-next\backend\overlay",
    "smartbox-next\backend\license",
    "smartbox-next\backend\api",
    "smartbox-next\backend\trigger"
)

# Schritt 4: In Projekt-Verzeichnis wechseln
Set-Location "smartbox-next"

# Schritt 5: Go Dependencies installieren
Write-Host "`n📦 Installing Go dependencies..." -ForegroundColor Green
go get github.com/suyashkumar/dicom
go get github.com/gin-gonic/gin
go get github.com/gorilla/websocket
go get github.com/kbinani/win

# Schritt 6: Frontend Dependencies
Write-Host "`n📦 Installing Frontend dependencies..." -ForegroundColor Green
Set-Location "frontend"
npm install
npm install pinia @vueuse/core
npm install -D @types/node tailwindcss autoprefixer postcss

# Zurück zum Hauptverzeichnis
Set-Location ..

Write-Host "`n✅ Setup Complete!" -ForegroundColor Green
Write-Host "📝 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Run 'wails dev' to start development server" -ForegroundColor White
Write-Host "   2. Open http://localhost:34115 in your browser" -ForegroundColor White
Write-Host "`n🎉 Happy coding!" -ForegroundColor Magenta