# SmartBox Next - Detaillierte Anleitung ab Schritt 3

## 🚀 Schritt 3: Wails installieren (Ausführlich)

### Was ist Wails?
Wails ist ein Framework, das Go (Backend) mit modernen Web-Technologien (Frontend) verbindet. Es erstellt native Windows-Anwendungen OHNE Electron - also klein und schnell!

### Installation:

1. **PowerShell als Administrator öffnen**:
   - Windows-Taste drücken
   - "PowerShell" tippen
   - Rechtsklick → "Als Administrator ausführen"

2. **Wails CLI installieren**:
   ```powershell
   go install github.com/wailsapp/wails/v2/cmd/wails@latest
   ```
   
   Was passiert hier?
   - `go install` lädt den Source Code herunter
   - Kompiliert ihn zu einer .exe
   - Legt sie in `C:\Users\[DeinName]\go\bin\wails.exe`

3. **PATH Problem lösen** (falls "wails" nicht gefunden wird):
   
   Option A - Temporär (nur für diese Session):
   ```powershell
   $env:Path += ";$env:USERPROFILE\go\bin"
   ```
   
   Option B - Permanent (empfohlen):
   ```powershell
   # Aktuellen PATH anzeigen
   echo $env:Path
   
   # System-Eigenschaften öffnen
   sysdm.cpl
   # → Erweitert → Umgebungsvariablen
   # → Bei "Path" (Benutzervariablen) → Bearbeiten
   # → Neu → C:\Users\oliver.stern\go\bin
   # → OK → OK → OK
   
   # PowerShell neu starten!
   ```

4. **Wails verifizieren**:
   ```powershell
   wails version
   # Output: Wails v2.7.1 (oder neuer)
   
   # System-Check (sehr wichtig!):
   wails doctor
   ```
   
   Der `wails doctor` Befehl prüft:
   - ✓ Go Version
   - ✓ Node Version  
   - ✓ NPM Version
   - ✓ WebView2 (Windows Komponente)
   - ✓ Build Tools

   Falls WebView2 fehlt:
   ```powershell
   # Wails installiert es automatisch beim ersten Build
   # Oder manuell: https://developer.microsoft.com/en-us/microsoft-edge/webview2/
   ```

## 🚀 Schritt 4: SmartBox Next Projekt erstellen (Ausführlich)

1. **Zum richtigen Verzeichnis navigieren**:
   ```powershell
   # Falls das Verzeichnis noch nicht existiert:
   cd C:\Users\oliver.stern\source\repos
   mkdir smartbox-next
   cd smartbox-next
   ```

2. **Wails Projekt initialisieren**:
   ```powershell
   wails init -n smartbox-next -t vue
   ```
   
   Was bedeuten die Parameter?
   - `-n smartbox-next` = Name des Projekts
   - `-t vue` = Template (wir nutzen Vue 3)
   
   Wails erstellt jetzt:
   - Go Backend Struktur
   - Vue 3 Frontend
   - Build-Konfiguration
   - Beispiel-Code

3. **Was wurde erstellt?**:
   ```
   smartbox-next/
   ├── app.go           # Haupt-Go-Datei (Backend)
   ├── build/           # Build-Configs und Icons
   ├── embed.go         # Bettet Frontend in .exe ein
   ├── go.mod          # Go Dependency Management
   ├── wails.json      # Wails Konfiguration
   └── frontend/       # Vue 3 App
       ├── index.html
       ├── package.json # NPM Dependencies
       ├── src/
       │   ├── App.vue     # Haupt Vue Component
       │   ├── main.js     # Vue Entry Point
       │   └── style.css   # Global Styles
       └── wailsjs/    # Auto-generiert (Go↔JS Bridge)
   ```

## 🚀 Schritt 5: Projekt-Struktur erweitern (Ausführlich)

1. **Backend-Struktur erstellen**:
   ```powershell
   # Im smartbox-next Verzeichnis:
   
   # Windows mkdir kann mehrere auf einmal:
   mkdir backend
   cd backend
   mkdir capture, dicom, overlay, license, api
   cd ..
   ```
   
   Oder alles auf einmal:
   ```powershell
   mkdir backend\capture, backend\dicom, backend\overlay, backend\license, backend\api
   ```

2. **Warum diese Struktur?**:
   - `capture/` → Webcam, USB-Grabber Code
   - `dicom/` → DICOM Erstellung und Export
   - `overlay/` → Patient-Info Einblendungen
   - `license/` → Lizenzverwaltung
   - `api/` → REST/WebSocket für Remote

## 🚀 Schritt 6: Dependencies installieren (Ausführlich)

### Backend (Go) Dependencies:

1. **DICOM Library hinzufügen**:
   ```powershell
   go get github.com/suyashkumar/dicom
   ```
   
   Was macht `go get`?
   - Lädt die Library herunter
   - Fügt sie zu go.mod hinzu
   - Cached sie in `%USERPROFILE%\go\pkg\mod`

2. **Weitere wichtige Libraries**:
   ```powershell
   # Web Framework (für API)
   go get github.com/gin-gonic/gin
   
   # WebSocket Support
   go get github.com/gorilla/websocket
   
   # Windows-spezifisch für Webcam (später)
   go get github.com/kbinani/win
   ```

### Frontend Dependencies:

1. **In Frontend-Verzeichnis wechseln**:
   ```powershell
   cd frontend
   ```

2. **Basis-Dependencies installieren**:
   ```powershell
   # Installiert alles aus package.json
   npm install
   ```

3. **Zusätzliche Vue Libraries**:
   ```powershell
   # State Management
   npm install pinia
   
   # Utility Functions
   npm install @vueuse/core
   
   # TypeScript Types (für bessere IDE-Unterstützung)
   npm install -D @types/node
   
   # UI Framework (optional)
   npm install -D tailwindcss autoprefixer postcss
   ```

## 🚀 Schritt 7: Erstes Test-Run (Ausführlich)

1. **Zurück zum Hauptverzeichnis**:
   ```powershell
   cd ..
   # Du solltest jetzt in C:\...\smartbox-next sein
   ```

2. **Development Server starten**:
   ```powershell
   wails dev
   ```
   
   Was passiert jetzt?
   - Go Backend wird kompiliert
   - Vue Frontend wird gebaut
   - Hot-Reload Server startet
   - Browser öffnet sich (http://localhost:34115)
   - Ein Windows-Fenster öffnet sich mit der App

3. **Was du sehen solltest**:
   - Ein Fenster mit Wails+Vue Demo
   - Im Browser: Gleiche App
   - Änderungen im Code → Automatischer Reload

4. **Wichtige Tastenkombinationen**:
   - `F12` im App-Fenster → Developer Tools
   - `Ctrl+C` im Terminal → Server stoppen

## 🔧 Nächster Schritt: Erster Code

Lass uns die Demo durch unseren ersten SmartBox Code ersetzen:

1. **Backend (app.go) anpassen**:
   ```go
   package main

   import (
       "context"
       "embed"
       "github.com/wailsapp/wails/v2"
       "github.com/wailsapp/wails/v2/pkg/options"
   )

   //go:embed all:frontend/dist
   var assets embed.FS

   type App struct {
       ctx context.Context
   }

   func NewApp() *App {
       return &App{}
   }

   func (a *App) startup(ctx context.Context) {
       a.ctx = ctx
   }

   // Unsere erste Funktion!
   func (a *App) GetCameras() []string {
       // TODO: Echte Kamera-Liste
       return []string{"Webcam 1", "USB Grabber", "Virtual Camera"}
   }

   func main() {
       app := NewApp()

       err := wails.Run(&options.App{
           Title:  "SmartBox Next",
           Width:  1024,
           Height: 768,
           Assets: assets,
           OnStartup: app.startup,
           Bind: []interface{}{
               app,
           },
       })

       if err != nil {
           println("Error:", err.Error())
       }
   }
   ```

2. **Frontend (App.vue) anpassen**:
   ```vue
   <template>
     <div class="container">
       <h1>SmartBox Next</h1>
       <button @click="loadCameras">Kameras laden</button>
       <ul>
         <li v-for="cam in cameras" :key="cam">{{ cam }}</li>
       </ul>
     </div>
   </template>

   <script setup>
   import { ref } from 'vue'
   import { GetCameras } from '../wailsjs/go/main/App'

   const cameras = ref([])

   async function loadCameras() {
     cameras.value = await GetCameras()
   }
   </script>
   ```

3. **Testen**:
   ```powershell
   wails dev
   ```

## ❓ Häufige Probleme & Lösungen

### "go: command not found"
→ Go Installation prüfen, PATH checken

### "wails: command not found"  
→ `$env:Path += ";$env:USERPROFILE\go\bin"` ausführen

### "npm: command not found"
→ Node.js Installation prüfen

### WebView2 Fehler
→ Windows Update durchführen oder manuell installieren

### Firewall blockiert
→ Windows Defender Firewall → Wails erlauben

## 🎉 Geschafft!

Wenn `wails dev` funktioniert und du die App siehst, ist alles bereit für die echte Entwicklung!