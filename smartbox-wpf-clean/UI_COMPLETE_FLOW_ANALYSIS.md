# SmartBoxNext - KOMPLETTE UI FLOW ANALYSE

**Stand**: 2025-07-11
**Problem**: 3-Schritt-Verkabelung ist zu komplex und fehleranfällig
**Ziel**: Jede UI-Änderung sollte EINFACH sein!

## 🔴 DAS PROBLEM: 3-SCHRITT-HÖLLE

Für JEDEN Button müssen 3 Stellen synchron sein:
1. **HTML**: Button mit ID
2. **JavaScript**: EventListener + postMessage mit type
3. **C#**: Switch case mit EXAKT dem gleichen type (lowercase!)

**Beispiel Settings Button:**
```html
<!-- 1. HTML -->
<button id="settingsButton">Settings</button>

<!-- 2. JavaScript -->
settingsButton.addEventListener('click', () => {
    window.chrome.webview.postMessage(JSON.stringify({
        type: 'openSettings',  // MUSS mit C# übereinstimmen!
        data: {}
    }));
});

// 3. C# 
case "opensettings":  // MUSS lowercase sein!
    await OpenSettings();
    break;
```

## 📊 AKTUELLE BUTTON MAPPINGS

### Hauptseite (index.html)

| Button | HTML ID | JS Event | Message Type | C# Handler | Funktioniert? |
|--------|---------|----------|--------------|------------|---------------|
| Settings | `settingsButton` | click → postMessage | `openSettings` | `case "opensettings"` | ✅ |
| Exit | `exitButton` | click → Dialog → postMessage | `exitApp` | `case "exitapp"` | ✅ |
| Export | `exportButton` | click → postMessage | `exportCaptures` | `case "exportcaptures"` | ✅ |
| Back | `backToPatientSelection` | click → modeManager | N/A (JS only) | N/A | ✅ |

### Touch Gestures (Capture Area)

| Gesture | Trigger | JS Event | Message Type | C# Handler | Funktioniert? |
|---------|---------|----------|--------------|------------|---------------|
| Single Tap | Touch < 200ms | `capturePhoto` event | `capturePhoto` | `case "photocaptured"` | ❓ MISMATCH! |
| Long Press | Touch > 500ms | `startVideoRecording` | `captureVideo` | `case "videorecorded"` | ❓ MISMATCH! |
| Swipe Up | Touch move up | `mwlRefresh` event | `loadMWL` | `case "queryworklist"` | ❓ MISMATCH! |

### Settings Page (settings.html)

| Button | HTML ID | JS Event | Message Type | C# Handler | Funktioniert? |
|--------|---------|----------|--------------|------------|---------------|
| Save | `saveButton` | click → collectData → postMessage | `saveSettings` | `case "savesettings"` | ✅ |
| Test PACS | `test-pacs` | click → postMessage | `testPacsConnection` | `case "testpacsconnection"` | ✅ |
| Test MWL | `test-mwl` | click → postMessage | `testMwlConnection` | `case "testmwlconnection"` | ✅ |
| Back | `backButton` | click → navigate | N/A | N/A | ✅ |

## 🚨 GEFUNDENE PROBLEME

### 1. Message Type Mismatches
- JS sendet: `capturePhoto` → C# erwartet: `photocaptured`
- JS sendet: `captureVideo` → C# erwartet: `videorecorded`
- JS sendet: `openSettings` → C# hat ZWEI cases: `opensettings` UND `openSettings`!

### 2. Duplicate Case Handlers
```csharp
case "opensettings":    // Zeile 366
    await OpenSettings();
    break;
    
case "opensettings":    // Zeile 494 - DUPLIKAT!
    await OpenSettings();
    break;
```

### 3. Inkonsistente Naming
- Manchmal camelCase: `openSettings`
- Manchmal lowercase: `opensettings`
- Manchmal mit Unterstrich: `export_captures`

## 🛠️ WIE MAN EINEN NEUEN BUTTON HINZUFÜGT

### Aktueller Prozess (zu komplex!):

1. **HTML hinzufügen:**
```html
<button id="myNewButton">My Action</button>
```

2. **JavaScript EventListener:**
```javascript
const myNewButton = document.getElementById('myNewButton');
myNewButton.addEventListener('click', () => {
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(JSON.stringify({
            type: 'myNewAction',  // GENAU MERKEN!
            data: { /* optional data */ }
        }));
    }
});
```

3. **C# Handler hinzufügen:**
```csharp
case "mynewaction":  // MUSS lowercase sein!
    await HandleMyNewAction(message);
    break;

// Und die Methode:
private async Task HandleMyNewAction(JObject message)
{
    // Implementation
}
```

## 💡 VERBESSERUNGSVORSCHLÄGE

### 1. Zentrale Action Registry
```javascript
// actions.js
const ACTIONS = {
    OPEN_SETTINGS: 'openSettings',
    CAPTURE_PHOTO: 'capturePhoto',
    EXIT_APP: 'exitApp'
    // etc...
};
```

### 2. Automatisches Button Binding
```javascript
// Statt manuell jeden Button:
document.querySelectorAll('[data-action]').forEach(button => {
    button.addEventListener('click', () => {
        const action = button.dataset.action;
        sendToHost(action, {});
    });
});
```

### 3. C# Action Dictionary statt Switch
```csharp
private readonly Dictionary<string, Func<JObject, Task>> _handlers = new()
{
    ["opensettings"] = HandleOpenSettings,
    ["capturePhoto"] = HandleCapturePhoto,
    // etc...
};
```

## 📋 WARTUNGS-CHECKLISTE

Wenn ein Button nicht funktioniert:

1. **F12 Console** → Wird der Click Event gefeuert?
2. **Console Log** → Wird postMessage aufgerufen?
3. **Message Type** → Stimmt der type String überein?
4. **C# Case** → Ist es lowercase?
5. **Duplicate Cases** → Gibt es den case mehrfach?

## 🎯 QUICK FIX LISTE

### Photo Capture Fix:
```javascript
// ALT: type: 'capturePhoto'
// NEU: type: 'photocaptured'
```

### Video Recording Fix:
```javascript
// ALT: type: 'captureVideo'
// NEU: type: 'videorecorded'
```

### Settings Duplicate Fix:
C# MainWindow.xaml.cs - Zeile 494 löschen (duplicate case)

---

**FAZIT**: Die aktuelle Architektur macht jede UI-Änderung zu einem 3-Schritt-Tanz. Ein zentrales Action-System würde das MASSIV vereinfachen!