# SmartBox Settings Location Info

## Neuer Speicherort (ab jetzt):
```
C:\Users\oliver.stern\AppData\Roaming\SmartBoxNext\config.json
```

## Alter Speicherort (nicht mehr verwendet):
```
bin\Debug\net8.0-windows\config.json
bin\Release\net8.0-windows\config.json
```

## So findest du die config.json:
1. Windows-Taste + R
2. Eingeben: `%APPDATA%\SmartBoxNext`
3. Enter drücken

## Automatische Migration:
- Beim ersten Start mit der neuen Version wird die alte config.json automatisch kopiert
- Danach wird nur noch der neue Speicherort verwendet

## Debugging:
- Die Logs zeigen immer den exakten Pfad an:
  - "Configuration loaded from: C:\Users\..."
  - "Settings saved to: C:\Users\..."

## Vorteile:
✅ Settings überleben App-Updates
✅ Settings überleben Debug/Release Wechsel
✅ Settings sind immer am gleichen Ort
✅ Kein Administrator-Zugriff nötig