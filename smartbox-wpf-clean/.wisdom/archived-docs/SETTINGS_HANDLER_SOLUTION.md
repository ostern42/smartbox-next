# Settings Handler - Saubere Lösung für komplexe Actions

## Problem
Die Settings-Speicherung hat komplexe Logik:
- Daten aus vielen Feldern sammeln
- Validierung
- Bestätigungen anzeigen
- Fehlerbehandlung

Das einfache Action-System kann das nicht abbilden.

## Lösung: settings-handler.js

Ein dedizierter Handler der:
1. Die komplette Logik behält
2. Mit dem Action-System zusammenarbeitet
3. Alle Features erhält

## Features

### 1. Vollständige Datensammlung
```javascript
gatherFormData() {
    // Sammelt aus allen Input-Feldern
    // Konvertiert IDs zu Property-Namen
    // Behandelt verschiedene Input-Typen
}
```

### 2. Validierung
```javascript
validateConfig(config) {
    // PACS Validierung
    // MWL Validierung
    // Pfad-Validierung
    return { valid: true/false, message: '...' };
}
```

### 3. Schöne Notifications
- Success: Grün mit Auto-Hide
- Error: Rot, bleibt sichtbar
- Info: Blau für "Testing..."
- Mit Close-Button

### 4. Test-Funktionen
- Test PACS: Sammelt nur PACS-Daten
- Test MWL: Sammelt nur MWL-Daten
- Zeigt "Testing..." während des Tests

### 5. Browse Folder
- Kennt das Ziel-Input
- Sendet targetInputId an C#

## Integration

### HTML (bleibt einfach):
```html
<button data-action="savesettings">Save</button>
<button data-action="testpacsconnection">Test PACS</button>
```

### JavaScript (settings-handler.js):
```javascript
// Registriert automatisch alle Handler
handler.registerSpecialHandler('savesettings', () => {
    this.handleSaveSettings(); // Komplette Logik!
});
```

### C# (MainWindow.xaml.cs):
```csharp
case "savesettings":
    // Erhält vollständiges Config-Objekt
    await SaveConfiguration(message.data);
    break;
```

## Vorteile

1. **Keine Logik verloren**: Alles funktioniert wie vorher
2. **Saubere Trennung**: UI ↔ Logic ↔ Backend
3. **Erweiterbar**: Neue Features einfach hinzufügen
4. **Testbar**: Jede Funktion isoliert
5. **Wiederverwendbar**: Pattern für andere komplexe Actions

## Pattern für neue komplexe Actions

1. Erstelle `feature-handler.js`
2. Implementiere komplette Logik
3. Registriere beim Action-System
4. HTML bleibt simpel mit data-action

## Fazit

Das Action-System macht Buttons einfach. Aber komplexe Logik gehört in dedizierte Handler. So haben wir das Beste aus beiden Welten:
- Einfache HTML-Buttons
- Vollständige Funktionalität
- Keine Kompromisse

**Kein Kreis mehr bei neuen Buttons!** 🎉