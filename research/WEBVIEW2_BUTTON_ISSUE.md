# WebView2 Button Communication Issue

## Problem
- Buttons (Open Logs, Browse Folder) funktionieren nicht
- WebView2 postMessage scheint nicht anzukommen
- Gleicher Code funktioniert in anderen Projekten

## Mögliche Ursachen

### 1. Navigation Timing
WebView2 messages können verloren gehen, wenn sie gesendet werden bevor die Navigation abgeschlossen ist.

### 2. JSON Serialization
Die C# WebMessage Klasse erwartet bestimmte Properties. Wenn die JSON-Struktur nicht passt, schlägt die Deserialisierung fehl.

### 3. ExecuteScriptAsync für Antworten
Die Antwort vom C# Host verwendet `ExecuteScriptAsync` mit `window.postMessage`. Das könnte in einem iframe anders funktionieren.

## Debug-Strategie

### 1. Direkte String-Verarbeitung
Statt JSON zu deserialisieren, erst mal den raw string loggen und manuell parsen.

### 2. NavigationCompleted Event
Sicherstellen, dass Messages erst nach NavigationCompleted gesendet werden.

### 3. CoreWebView2.PostWebMessageAsJson
Verwende PostWebMessageAsJson statt ExecuteScriptAsync für Antworten.

## Lösungsansatz

```csharp
// Statt komplexer Deserialisierung:
var messageJson = e.TryGetWebMessageAsString();
_logger.LogInfo($"Raw message: {messageJson}");

// Simple string parsing für Debug:
if (messageJson.Contains("\"action\":\"openLogs\""))
{
    await HandleOpenLogs();
}
```

## Alternative: Direct Invoke
Statt postMessage könnte man auch direkte Funktionen exponieren:

```csharp
await WebView.CoreWebView2.AddHostObjectToScriptAsync("smartbox", new HostObject());
```

```javascript
// Im JavaScript:
window.chrome.webview.hostObjects.smartbox.openLogs();
```