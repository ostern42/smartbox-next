# SmartBox Next - Development Setup Guide

## 🚀 Schritt 1: Go installieren

### Windows (Native - WICHTIG: Nicht in WSL!)

1. **Download Go**:
   - Gehe zu: https://go.dev/dl/
   - Lade herunter: `go1.21.6.windows-amd64.msi` (oder neuer)
   
2. **Installation**:
   - Doppelklick auf MSI
   - Standard-Pfad: `C:\Program Files\Go`
   - Installer fügt automatisch zu PATH hinzu

3. **Verifizieren** (in Windows Terminal/PowerShell):
   ```powershell
   go version
   # Sollte zeigen: go version go1.21.6 windows/amd64
   ```

## 🚀 Schritt 2: Node.js installieren

1. **Download Node.js**:
   - Gehe zu: https://nodejs.org/
   - Lade LTS Version (20.x oder neuer)
   - `node-v20.11.0-x64.msi`

2. **Installation**:
   - Doppelklick auf MSI
   - ✅ "Automatically install necessary tools" ankreuzen
   - Standard-Installation

3. **Verifizieren**:
   ```powershell
   node --version
   # v20.11.0
   
   npm --version
   # 10.2.4
   ```

## 🚀 Schritt 3: Wails installieren

In PowerShell (als Admin):

```powershell
# Wails CLI installieren
go install github.com/wailsapp/wails/v2/cmd/wails@latest

# Verifizieren
wails version
# Sollte zeigen: Wails v2.7.1

# System-Check
wails doctor
```

Falls `wails` nicht gefunden wird:
```powershell
# Go bin zu PATH hinzufügen
$env:Path += ";$env:USERPROFILE\go\bin"
# Oder permanent in Systemeinstellungen
```

## 🚀 Schritt 4: SmartBox Next Projekt erstellen

```powershell
# In dein Projekt-Verzeichnis wechseln
cd C:\Users\oliver.stern\source\repos\smartbox-next

# Wails Projekt initialisieren
wails init -n smartbox-next -t vue

# Ins Projekt wechseln
cd smartbox-next
```

## 🚀 Schritt 5: Projekt-Struktur anpassen

```powershell
# Zusätzliche Verzeichnisse erstellen
mkdir backend\capture
mkdir backend\dicom  
mkdir backend\overlay
mkdir backend\license
mkdir backend\api
```

## 🚀 Schritt 6: Basis-Dependencies

### Backend (Go)
Erstelle `go.mod` falls nicht vorhanden:
```powershell
go mod init smartbox-next
```

Dann Dependencies hinzufügen:
```powershell
# DICOM Library
go get github.com/suyashkumar/dicom

# Weitere nützliche Libraries
go get github.com/gin-gonic/gin
go get github.com/gorilla/websocket
```

### Frontend
```powershell
cd frontend
npm install

# Zusätzliche Vue Dependencies
npm install pinia @vueuse/core
npm install -D @types/node
```

## 🚀 Schritt 7: Erstes Test-Run

```powershell
# Zurück zum Root
cd ..

# Development Mode starten
wails dev

# Browser sollte sich öffnen mit Wails+Vue Template
```

## 📁 Finale Projekt-Struktur

```
smartbox-next/
├── app.go              # Wails App Hauptdatei
├── build/              # Build-Konfiguration
├── embed.go            # Embedded Files
├── go.mod              # Go Dependencies
├── go.sum
├── wails.json          # Wails Konfiguration
├── backend/            # Go Backend Code
│   ├── capture/        # Video/Bild Capture
│   ├── dicom/          # DICOM Handling
│   ├── overlay/        # Overlay Rendering
│   ├── license/        # Lizenzmanagement
│   └── api/            # REST/WebSocket API
└── frontend/           # Vue 3 Frontend
    ├── index.html
    ├── package.json
    ├── src/
    │   ├── App.vue
    │   ├── main.js
    │   └── style.css
    └── wailsjs/        # Auto-generierte Wails Bindings

## 🔧 Nächste Schritte

1. **Webcam Test** implementieren
2. **DICOM Basis** aufsetzen
3. **UI Prototyp** entwickeln

## ⚠️ Wichtige Hinweise

- **Immer in Windows PowerShell/Terminal arbeiten** (nicht WSL!)
- **Admin-Rechte** für manche Installationen nötig
- **Windows Defender** könnte bei go install warnen → Erlauben
- **Firewall** könnte Wails Dev Server blockieren → Erlauben

## 🆘 Troubleshooting

### "wails: command not found"
```powershell
# Go bin Pfad prüfen
echo $env:USERPROFILE\go\bin
# Manuell zu PATH hinzufügen
```

### "missing dependencies"
```powershell
wails doctor
# Zeigt was fehlt
```

### Build-Fehler
```powershell
# Clean Build
wails build -clean
```