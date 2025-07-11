# UI Maintenance Guide - SmartBoxNext

**Für Oliver**: Damit du nie wieder F12 Debug brauchst!

## 🚀 SCHNELL-REFERENZ

### Button funktioniert nicht? Check diese 3 Dinge:

1. **JavaScript sendet den richtigen Type?**
```javascript
// In app.js suchen:
window.chrome.webview.postMessage(JSON.stringify({
    type: 'DER_TYPE_HIER',  // <-- Diesen String merken!
    data: {}
}));
```

2. **C# hat den EXAKT gleichen Type (lowercase)?**
```csharp
// In MainWindow.xaml.cs suchen:
case "der_type_hier":  // <-- MUSS lowercase sein!
    await HandleDerTypeHier(message);
    break;
```

3. **Keine Tippfehler?**
- `openSettings` ≠ `opensettings` ≠ `open_settings`
- C# Switch ist IMMER lowercase!

## 🔧 HÄUFIGE PROBLEME & LÖSUNGEN

### Problem 1: "Button macht gar nichts"
**Check**: F12 → Console → Errors?
**Lösung**: EventListener fehlt wahrscheinlich

### Problem 2: "Message kommt nicht in C# an"
**Check**: Ist WebView2 verfügbar?
```javascript
console.log('WebView2 available:', !!window.chrome?.webview);
```

### Problem 3: "C# Handler wird nicht aufgerufen"
**Check**: 
- Type String EXAKT gleich?
- C# case ist lowercase?
- Kein duplicate case?

## 📝 NEUEN BUTTON HINZUFÜGEN - SCHRITT FÜR SCHRITT

### 1. HTML Button
```html
<button id="meinNeuerButton">Meine Aktion</button>
```

### 2. JavaScript (in app.js)
```javascript
// Bei setupUIEventListeners() hinzufügen:
const meinNeuerButton = document.getElementById('meinNeuerButton');
if (meinNeuerButton) {
    meinNeuerButton.addEventListener('click', () => {
        console.log('Mein Button clicked!'); // Debug
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'meineAktion',  // WICHTIG: Diesen String merken!
                data: {}
            }));
        }
    });
}
```

### 3. C# Handler (in MainWindow.xaml.cs)
```csharp
// Im switch statement:
case "meineaktion":  // IMMER lowercase!
    await HandleMeineAktion(message);
    break;

// Neue Methode:
private async Task HandleMeineAktion(JObject message)
{
    _logger.LogInformation("Meine Aktion ausgeführt");
    // Deine Logik hier
}
```

## 🐛 DEBUG HELPERS

### JavaScript Debug Mode aktivieren
```javascript
// Am Anfang von app.js:
window.DEBUG_MODE = true;

// Dann überall:
if (window.DEBUG_MODE) {
    console.log('Button clicked:', buttonId);
    console.log('Sending message:', messageType);
}
```

### C# Logging erhöhen
```csharp
// In WebView_WebMessageReceived:
_logger.LogInformation($"Received message type: '{action}'");
_logger.LogInformation($"Full message: {messageJson}");
```

## 🎯 DIE WICHTIGSTEN MAPPINGS

| Was du willst | JS Message Type | C# Case |
|---------------|-----------------|----------|
| Settings öffnen | `openSettings` | `"opensettings"` |
| Foto aufnehmen | `capturePhoto` | `"photocaptured"` ⚠️ |
| Video aufnehmen | `captureVideo` | `"videorecorded"` ⚠️ |
| DICOM exportieren | `exportCaptures` | `"exportcaptures"` |
| App beenden | `exitApp` | `"exitapp"` |
| PACS testen | `testPacsConnection` | `"testpacsconnection"` |
| MWL testen | `testMwlConnection` | `"testmwlconnection"` |

⚠️ = Achtung: JS und C# Namen stimmen NICHT überein!

## 💡 VEREINFACHUNGS-IDEE

Statt 3 Stellen zu pflegen, könnten wir data-attributes nutzen:

```html
<!-- Einfacher: -->
<button data-action="openSettings">Settings</button>

<!-- JavaScript einmal für alle: -->
document.querySelectorAll('[data-action]').forEach(btn => {
    btn.addEventListener('click', (e) => {
        const action = e.target.dataset.action;
        sendToHost(action, {});
    });
});
```

Dann musst du nur noch HTML und C# synchron halten!

---

**Remember**: Bei Problemen IMMER zuerst F12 Console checken! Da steht meist schon der Fehler.