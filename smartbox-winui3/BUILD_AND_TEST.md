# SmartBoxNext - Build und Test in Visual Studio

## 1. Visual Studio öffnen

### Option A: Über die Solution-Datei
```bash
# Doppelklick auf:
smartbox-winui3/SmartBoxNext.sln
```

### Option B: Über Visual Studio
1. Visual Studio 2022 starten
2. "Open a project or solution"
3. Navigate zu: `smartbox-next/smartbox-winui3/SmartBoxNext.sln`

## 2. Build Configuration

### Wichtige Einstellungen:
- **Configuration**: Debug (für Tests)
- **Platform**: x64 (WICHTIG! Nicht "Any CPU")
- **Target**: SmartBoxNext (Packaging)

### Build-Schritte:
1. **Menü**: Build → Clean Solution
2. **Menü**: Build → Build Solution
   - Oder: `Ctrl+Shift+B`

## 3. Starten und Testen

### Debug starten:
1. **F5** drücken (Start Debugging)
   - Oder: Grüner Play-Button in der Toolbar

### Was du testen solltest:

#### A. Webcam-Initialisierung
1. Click "Initialize Webcam"
2. Schaue im Debug-Output nach:
   - "Trying SimpleVideoCapture..."
   - "SIMPLE VIDEO CAPTURE ACTIVE!"
   - "Video FPS: XX.X"

#### B. Video-Performance
- Im Debug-Fenster solltest du sehen:
  - "Video FPS: 30.0" (oder höher)
  - Nicht: "Using timer-based preview (5-10 FPS)"

#### C. Bild aufnehmen
1. Click "Capture Image"
2. Bild sollte in < 100ms aufgenommen werden
3. Preview-Dialog zeigt das Bild

## 4. Troubleshooting

### Build-Fehler: "Microsoft.Build.Packaging.Pri.Tasks.dll not found"
**Lösung**: 
- Visual Studio Installer öffnen
- Modify → Workloads → ".NET Multi-platform App UI development"
- Installieren/Reparieren

### Webcam wird nicht gefunden
**Lösung**:
1. Windows Settings → Privacy → Camera
2. "Let apps access your camera" → ON
3. SmartBoxNext in der Liste → ON

### Nur 5-10 FPS
**Das bedeutet**: MediaFrameReader funktioniert nicht
**Debug-Schritte**:
1. Schaue nach "SimpleVideoCapture failed: ..."
2. Check welches Format die Kamera unterstützt
3. Eventuell fällt es auf Timer-based zurück

### Visual Studio Tipps

#### Output Windows anzeigen:
- View → Output
- Show output from: "Debug"

#### Breakpoints setzen:
Wichtige Stellen für Breakpoints:
```csharp
// In MainWindow.xaml.cs:
- Zeile ~140: if (await _simpleVideoCapture.InitializeAsync(_mediaCapture))
- Zeile ~365: OnFrameArrived

// In SimpleVideoCapture.cs:
- Zeile ~85: OnFrameArrived
```

#### Performance Profiler:
1. Debug → Performance Profiler
2. CPU Usage auswählen
3. Start
4. Webcam initialisieren
5. Stop und analysieren

## 5. Quick Test Commands

### In Package Manager Console (Tools → NuGet Package Manager → Package Manager Console):
```powershell
# Projekt cleanen
dotnet clean

# Dependencies wiederherstellen
dotnet restore

# Build (funktioniert nur teilweise ohne VS)
msbuild SmartBoxNext.csproj /p:Configuration=Debug /p:Platform=x64
```

### Direkt aus VS Code Terminal:
```bash
# Visual Studio Command Line Build
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" SmartBoxNext.sln /build Debug
```

## 6. Expected Results

### Erfolgreiche Video-Streaming:
```
[10:23:45.123] Trying SimpleVideoCapture for video streaming...
[10:23:45.234] Using source: \\?\USB#VID_046D&PID_0825...
[10:23:45.345] Available: YUY2 640x480 @ 30.0 FPS
[10:23:45.456] Selected: YUY2 @ 30.0 FPS
[10:23:45.567] Start status: Success
[10:23:45.678] SIMPLE VIDEO CAPTURE ACTIVE! Real video streaming!
[10:23:46.789] Video FPS: 29.8
[10:23:47.890] Video FPS: 30.1
```

### Performance Metriken:
- **Ziel**: 25-60 FPS
- **CPU**: < 15% für Video-Anzeige
- **Latenz**: < 50ms Frame-zu-Display

## 7. Nächste Schritte nach erfolgreichem Test

1. **DICOM Export** testen (wenn implementiert)
2. **PACS Settings** konfigurieren
3. **Persistent Queue** aktivieren
4. **Silk.NET GPU Acceleration** testen (Test Silk.NET Button)

---

**Tipp**: Lass Visual Studio im Debug-Modus laufen und beobachte die Debug-Ausgabe. Dort siehst du genau, welche Capture-Methode verwendet wird und welche FPS erreicht werden.