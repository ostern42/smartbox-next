# UI Refactoring Plan - Von 3-Schritt-Hölle zu Einfachheit

**Ziel**: Button-Änderungen sollen EINFACH sein - nur HTML ändern, fertig!

## 📋 IMPLEMENTATIONSPLAN

### Phase 1: Zentrales Action-System erstellen

#### 1.1 Neue Datei: `actions.js`
```javascript
// Zentrale Action-Definitionen
const ACTIONS = {
    // App Control
    OPEN_SETTINGS: 'opensettings',
    EXIT_APP: 'exitapp',
    
    // Capture Actions
    CAPTURE_PHOTO: 'capturephoto',
    CAPTURE_VIDEO: 'capturevideo',
    STOP_VIDEO: 'stopvideo',
    
    // Export/Send
    EXPORT_DICOM: 'exportdicom',
    SEND_TO_PACS: 'sendtopacs',
    EXPORT_CAPTURES: 'exportcaptures',
    
    // MWL/Worklist
    LOAD_MWL: 'loadmwl',
    REFRESH_MWL: 'refreshmwl',
    SELECT_PATIENT: 'selectpatient',
    
    // Settings
    SAVE_SETTINGS: 'savesettings',
    TEST_PACS: 'testpacsconnection',
    TEST_MWL: 'testmwlconnection',
    
    // Debug/Utility
    OPEN_LOGS: 'openlogs',
    TEST_WEBVIEW: 'testwebview'
};

// Globale sendToHost Funktion
function sendToHost(action, data = {}) {
    if (!window.chrome?.webview) {
        console.warn('WebView2 not available');
        return;
    }
    
    console.log(`[ACTION] ${action}`, data);
    
    window.chrome.webview.postMessage(JSON.stringify({
        type: action,
        data: data
    }));
}
```

#### 1.2 Neues Action-Handler-System in `app.js`
```javascript
// Am Anfang von app.js
class ActionHandler {
    constructor() {
        this.setupGlobalHandlers();
    }
    
    setupGlobalHandlers() {
        // Automatisches Binding für ALLE Buttons mit data-action
        document.addEventListener('click', (e) => {
            const button = e.target.closest('[data-action]');
            if (!button) return;
            
            e.preventDefault();
            const action = button.dataset.action;
            const actionData = this.collectActionData(button);
            
            console.log(`[BUTTON] ${button.id || 'unnamed'} → ${action}`);
            
            // Spezielle Behandlung für manche Actions
            if (this.needsConfirmation(action)) {
                this.confirmAndExecute(action, actionData);
            } else {
                sendToHost(action, actionData);
            }
        });
    }
    
    collectActionData(button) {
        // Sammle data-* Attribute
        const data = {};
        for (const key in button.dataset) {
            if (key !== 'action') {
                data[key] = button.dataset[key];
            }
        }
        return data;
    }
    
    needsConfirmation(action) {
        return ['exitapp', 'deletecapture'].includes(action);
    }
    
    confirmAndExecute(action, data) {
        // Zeige Bestätigungsdialog
        if (confirm('Sind Sie sicher?')) {
            sendToHost(action, data);
        }
    }
}
```

### Phase 2: HTML Buttons umbauen

#### 2.1 index.html - Alle Buttons bekommen data-action
```html
<!-- ALT -->
<button id="settingsButton">Settings</button>

<!-- NEU -->
<button id="settingsButton" data-action="opensettings">
    <i class="icon-settings"></i>
    Settings
</button>

<!-- Mit zusätzlichen Daten -->
<button id="exportButton" 
        data-action="exportcaptures" 
        data-format="dicom"
        data-include-videos="true">
    Export DICOM
</button>
```

#### 2.2 settings.html - Gleiche Behandlung
```html
<!-- Test Buttons -->
<button id="testPacsButton" 
        data-action="testpacsconnection"
        class="test-button">
    Test PACS Connection
</button>

<button id="testMwlButton" 
        data-action="testmwlconnection"
        class="test-button">
    Test MWL Connection
</button>

<!-- Save Button mit Spezialbehandlung -->
<button id="saveButton" 
        data-action="savesettings"
        data-collect-form="settingsForm">
    Save Settings
</button>
```

### Phase 3: JavaScript Event Listener entfernen

#### 3.1 Alte Event Listener löschen
```javascript
// LÖSCHEN - Nicht mehr nötig!
settingsButton.addEventListener('click', () => {
    window.chrome.webview.postMessage(JSON.stringify({
        type: 'openSettings',
        data: {}
    }));
});

// Wird ersetzt durch data-action="opensettings"
```

#### 3.2 Spezialbehandlung für komplexe Actions
```javascript
// Für Actions die Daten sammeln müssen
class SpecialHandlers {
    static savesettings(button) {
        const formId = button.dataset.collectForm;
        const formData = collectFormData(formId);
        sendToHost('savesettings', formData);
    }
    
    static capturephoto(button) {
        const imageData = captureFromVideo();
        const patient = getCurrentPatient();
        sendToHost('capturephoto', {
            imageData,
            patient,
            timestamp: new Date().toISOString()
        });
    }
}
```

### Phase 4: C# Handler vereinheitlichen

#### 4.1 Duplicate Cases entfernen
```csharp
// Zeile 494 - LÖSCHEN (Duplikat von Zeile 366)
case "opensettings":
    await OpenSettings();
    break;
```

#### 4.2 Message Type Fixes
```csharp
// ALT
case "photocaptured":  // Passt nicht zu JS "capturePhoto"

// NEU
case "capturephoto":   // Jetzt konsistent!
```

### Phase 5: Touch Gestures anpassen

#### 5.1 Touch Events auch über data-action
```javascript
// In TouchGestureManager
onSingleTap(e) {
    const target = e.target;
    // Simuliere Button Click mit data-action
    const virtualButton = {
        dataset: { action: 'capturephoto' }
    };
    document.dispatchEvent(new CustomEvent('action', {
        detail: { action: 'capturephoto', data: {} }
    }));
}
```

### Phase 6: Testing & Migration

#### 6.1 Test-Checkliste
- [ ] Settings Button → Settings öffnet sich
- [ ] Exit Button → Bestätigung → App schließt
- [ ] Export Button → Export Dialog
- [ ] Test PACS → Connection Test läuft
- [ ] Test MWL → Connection Test läuft
- [ ] Save Settings → Settings werden gespeichert
- [ ] Touch: Single Tap → Foto
- [ ] Touch: Long Press → Video Start
- [ ] Touch: Swipe Up → MWL Refresh

#### 6.2 Schrittweise Migration
1. Erst actions.js hinzufügen
2. ActionHandler in app.js einbauen
3. Ein Button nach dem anderen umstellen
4. Alten Code auskommentieren (nicht löschen)
5. Testen
6. Wenn alles läuft → Alten Code entfernen

## 🎯 ENDERGEBNIS

### Neuer Button hinzufügen - SO EINFACH:
```html
<!-- NUR NOCH DAS: -->
<button data-action="meineneuefunktion">
    Meine Neue Funktion
</button>
```

### C# Handler (einmalig):
```csharp
case "meineneuefunktion":
    await HandleMeineNeueFunktion(message);
    break;
```

**Das war's! Kein JavaScript Event Listener mehr nötig!**

## 📊 VORTEILE

1. **Weniger Code**: ~200 Zeilen Event Listener weg
2. **Weniger Fehler**: Nur noch 2 Stellen statt 3
3. **Einfacher**: HTML-Entwickler können Buttons hinzufügen
4. **Konsistent**: Alle Actions an einem Ort definiert
5. **Debugbar**: Ein console.log zeigt ALLE Actions

## ⚠️ RISIKEN & MITIGATION

1. **Risiko**: Bestehende Funktionalität kaputt
   **Mitigation**: Schrittweise, Button für Button

2. **Risiko**: Touch Gestures brechen
   **Mitigation**: Touch Events über gleichen Mechanismus

3. **Risiko**: Spezielle Button-Logik geht verloren
   **Mitigation**: SpecialHandlers für komplexe Cases

---

**Soll ich mit der Implementierung beginnen?**