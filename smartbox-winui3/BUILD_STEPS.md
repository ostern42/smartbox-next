# Build-Schritte nach Fixes

## 1. Visual Studio neu starten
- Schließe Visual Studio komplett
- Öffne SmartBoxNext.sln neu

## 2. Clean Build durchführen
1. **Build** → **Clean Solution**
2. Warte bis fertig
3. **Build** → **Rebuild Solution**

## 3. Falls immer noch Fehler "file locked":
```powershell
# In PowerShell als Admin:
cd C:\Users\oliver.stern\source\repos\smartbox-next\smartbox-winui3
.\fix-locks.ps1
```

## 4. Alternative: Manuell aufräumen
```powershell
# Alle Build-Ordner löschen
Remove-Item -Recurse -Force bin, obj
```

## 5. In Visual Studio:
- Configuration: **Debug**
- Platform: **x64**
- Start: **F5**

## Was wurde gefixt:
✅ SimpleVideoCapture und VideoStreamCapture sind jetzt `partial class`
✅ OnFrameArrived ist nicht mehr async (war unnötig)
✅ Build-Locks wurden entfernt

## Erwartetes Ergebnis:
- Build sollte erfolgreich sein
- Beim Start sollte "Initialize Webcam" funktionieren
- Debug-Output zeigt: "Video FPS: 30.0" oder höher