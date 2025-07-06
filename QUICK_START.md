# SmartBox Next - Quick Start Guide

## 🚀 Automatisches Setup

1. **PowerShell als Administrator öffnen**

2. **Setup Script ausführen**:
   ```powershell
   cd C:\Users\oliver.stern\source\repos\smartbox-next
   .\setup.ps1
   ```

   Falls PowerShell Scripts blockiert sind:
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   ```

3. **Alternative: Batch-Datei**:
   ```cmd
   setup-manual.bat
   ```

## 📝 Nach dem Setup

Das Script hat folgendes gemacht:
- ✅ Wails Projekt initialisiert
- ✅ Backend-Struktur erstellt
- ✅ Go Dependencies installiert
- ✅ Frontend Dependencies installiert

## 🔧 Dateien anpassen

Nach dem Setup musst du zwei Dateien ersetzen:

1. **Backend** (`smartbox-next/app.go`):
   - Lösche den Inhalt
   - Kopiere alles aus `app.go.template`

2. **Frontend** (`smartbox-next/frontend/src/App.vue`):
   - Lösche den Inhalt  
   - Kopiere alles aus `App.vue.template`

## 🎮 Starten

```powershell
cd smartbox-next
wails dev
```

## 🎉 Fertig!

Du solltest jetzt sehen:
- Ein dunkles SmartBox Next Interface
- 3 Demo-Kameras
- Capture Button (zeigt nur Meldung)

## 🔥 Nächste Schritte

1. **Echte Webcam Integration**
2. **DICOM Export**
3. **Touch-optimierte UI**

Viel Spaß und schönen Feierabend! 🍺