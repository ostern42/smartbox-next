# WebView2 Communication Guide for SmartBox Next

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [API Reference](#api-reference)
4. [Naming Conventions](#naming-conventions)
5. [Common Issues & Solutions](#common-issues--solutions)
6. [Example Implementations](#example-implementations)
7. [Troubleshooting Guide](#troubleshooting-guide)

## Overview

SmartBox Next uses Microsoft WebView2 to host a web-based medical interface within a WPF application. This guide documents the communication protocol between JavaScript (frontend) and C# (backend) to prevent naming conflicts and communication issues.

### Key Components
- **WebView2 Control**: Hosts the HTML/JavaScript interface
- **JavaScript Bridge**: `window.chrome.webview.postMessage()` for JS→C# communication
- **C# Event Handler**: `CoreWebView2.WebMessageReceived` for receiving messages
- **Message Protocol**: JSON-based message format with action-based routing

## Architecture

### Communication Flow
```
JavaScript (Frontend)          C# (Backend)
     |                              |
     |  window.chrome.webview       |
     |      .postMessage()          |
     |----------------------------->|
     |                              | OnWebMessageReceived()
     |                              | Parse JSON
     |                              | Route by action
     |                              | Execute handler
     |                              |
     |  window.addEventListener     |
     |      ('message', ...)        |
     |<-----------------------------|
     |  CoreWebView2.PostWebMessageAsString()
     |                              |
```

## API Reference

### JavaScript → C# Communication

#### Sending Messages from JavaScript

```javascript
// CORRECT: Always send as JSON string
function sendToBackend(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
        const message = JSON.stringify({
            action: action,
            ...data
        });
        window.chrome.webview.postMessage(message);
    } else {
        console.error('WebView2 bridge not available');
    }
}
```

**Important Notes:**
- Always check if `window.chrome.webview` exists before sending
- Always send messages as JSON strings (not objects)
- Use the `action` field to specify the operation

#### Available Actions

| Action | Description | Required Data | Response |
|--------|-------------|---------------|----------|
| `saveSettings` | Save configuration settings | `settings` object with configuration fields | `{type: "settingsSaved", success: boolean, error?: string}` |
| `sendToPacs` | Send DICOM files to PACS server | None | `{type: "pacsSent", success: boolean, message?: string, error?: string}` |
| `runDiagnostics` | Run system diagnostics | None | `{type: "diagnosticsComplete", success: boolean, data?: object, error?: string}` |
| `validateDicom` | Validate DICOM files | None | `{type: "dicomValidated", success: boolean, data?: object, error?: string}` |
| `loadWorklist` | Load modality worklist | None | `{type: "worklistLoaded", success: boolean, data?: array, error?: string}` |
| `exitApplication` | Close the application | None | No response (app closes) |

### C# → JavaScript Communication

#### Sending Messages from C#

```csharp
// Send response back to JavaScript
webView.CoreWebView2.PostWebMessageAsString(
    "{\"type\":\"responseType\",\"success\":true,\"data\":{...}}"
);
```

#### Receiving Messages in JavaScript

```javascript
// Listen for messages from C#
window.addEventListener('message', function(event) {
    try {
        const response = typeof event.data === 'string' 
            ? JSON.parse(event.data) 
            : event.data;
        handleBackendResponse(response);
    } catch (error) {
        console.error('Error parsing backend response:', error);
    }
});
```

## Naming Conventions

### Message Structure
```javascript
{
    // From JavaScript to C#
    "action": "actionName",      // Required: specifies the C# handler
    "data": {},                  // Optional: action-specific data
    "timestamp": "ISO-8601"      // Optional: for logging/debugging
}

{
    // From C# to JavaScript  
    "type": "responseType",      // Required: specifies the response type
    "success": true/false,       // Required: operation success status
    "data": {},                  // Optional: response data
    "message": "...",           // Optional: success message
    "error": "..."              // Optional: error message (when success=false)
}
```

### Action Naming Guidelines
- Use camelCase for action names (e.g., `saveSettings`, `loadWorklist`)
- Use descriptive, verb-based names that indicate the operation
- Group related actions with common prefixes (e.g., `dicom*`, `pacs*`)

### Response Type Naming
- Response types should match the action name + result (e.g., `settingsSaved`, `worklistLoaded`)
- Use past tense to indicate completion
- Keep consistent with the triggering action

## Common Issues & Solutions

### Issue 1: Tab Navigation Not Working

**Problem**: Tab clicks with `onclick='showTab("tabname")'` fail due to string parsing issues.

**Solution**: Use proper event listeners or data attributes:
```javascript
// WRONG: Inline onclick with nested quotes
<button onclick='showTab("settings")'>Settings</button>

// CORRECT: Use data attributes
<button data-tab='settings' onclick='showTab("settings")'>Settings</button>

// BETTER: Use event delegation
document.addEventListener('click', function(e) {
    if (e.target.dataset.tab) {
        showTab(e.target.dataset.tab);
    }
});
```

### Issue 2: WebView2 Bridge Not Available

**Problem**: `window.chrome.webview` is undefined.

**Solution**: 
1. Ensure WebView2 is fully initialized before loading content
2. Add initialization check:
```javascript
function waitForWebView(callback) {
    if (window.chrome && window.chrome.webview) {
        callback();
    } else {
        setTimeout(() => waitForWebView(callback), 100);
    }
}

waitForWebView(() => {
    console.log('WebView2 ready');
    // Initialize your app
});
```

### Issue 3: AppConfig Class Conflicts

**Problem**: Multiple `AppConfig` classes in the same namespace.

**Solution**: 
1. Use unique class names (`AppConfig` vs `AppConfigMinimal`)
2. Or use different namespaces
3. Or consolidate into a single configuration class

## Example Implementations

### Example 1: Adding a New Action

**JavaScript Side:**
```javascript
// Add new function to trigger backup
function triggerBackup() {
    sendToBackend('createBackup', {
        includeImages: true,
        includeSettings: true,
        backupPath: 'C:\\Backups'
    });
}

// Add handler for response
function handleBackendResponse(response) {
    switch(response.type) {
        case 'backupComplete':
            if (response.success) {
                alert('Backup completed: ' + response.data.backupFile);
            } else {
                alert('Backup failed: ' + response.error);
            }
            break;
        // ... other cases
    }
}
```

**C# Side:**
```csharp
private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
{
    try
    {
        var message = e.TryGetWebMessageAsString();
        var jsonDoc = System.Text.Json.JsonDocument.Parse(message);
        var action = jsonDoc.RootElement.GetProperty("action").GetString();
        
        switch (action)
        {
            case "createBackup":
                await HandleCreateBackup(jsonDoc.RootElement);
                break;
            // ... other cases
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling web message");
    }
}

private async Task HandleCreateBackup(System.Text.Json.JsonElement data)
{
    try
    {
        var backupData = data.GetProperty("data");
        var includeImages = backupData.GetProperty("includeImages").GetBoolean();
        var includeSettings = backupData.GetProperty("includeSettings").GetBoolean();
        var backupPath = backupData.GetProperty("backupPath").GetString();
        
        // Perform backup...
        var backupFile = await CreateBackup(includeImages, includeSettings, backupPath);
        
        // Send success response
        var response = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "backupComplete",
            success = true,
            data = new { backupFile = backupFile }
        });
        
        webView.CoreWebView2.PostWebMessageAsString(response);
    }
    catch (Exception ex)
    {
        // Send error response
        webView.CoreWebView2.PostWebMessageAsString(
            $"{{\"type\":\"backupComplete\",\"success\":false,\"error\":\"{ex.Message}\"}}"
        );
    }
}
```

### Example 2: Implementing Two-Way Communication

**JavaScript:** Request current status
```javascript
function requestSystemStatus() {
    sendToBackend('getSystemStatus');
    
    // Show loading indicator
    document.getElementById('statusDisplay').innerHTML = 'Loading...';
}
```

**C#:** Process request and send data
```csharp
case "getSystemStatus":
    var status = new
    {
        type = "systemStatus",
        success = true,
        data = new
        {
            cpuUsage = GetCpuUsage(),
            memoryUsage = GetMemoryUsage(),
            diskSpace = GetDiskSpace(),
            serviceStatus = "Running",
            lastUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        }
    };
    
    var statusJson = System.Text.Json.JsonSerializer.Serialize(status);
    webView.CoreWebView2.PostWebMessageAsString(statusJson);
    break;
```

**JavaScript:** Handle response
```javascript
function handleBackendResponse(response) {
    if (response.type === 'systemStatus' && response.success) {
        const status = response.data;
        document.getElementById('statusDisplay').innerHTML = `
            <div>CPU: ${status.cpuUsage}%</div>
            <div>Memory: ${status.memoryUsage}%</div>
            <div>Disk: ${status.diskSpace}</div>
            <div>Service: ${status.serviceStatus}</div>
            <div>Updated: ${status.lastUpdate}</div>
        `;
    }
}
```

## Troubleshooting Guide

### Debug Checklist

1. **WebView2 Initialization**
   - Is WebView2 runtime installed?
   - Is `EnsureCoreWebView2Async()` called before using the bridge?
   - Check initialization logs for errors

2. **Message Format**
   - Are you sending JSON strings (not objects)?
   - Is the JSON properly formatted (use JSON.stringify)?
   - Does the message contain the required `action` field?

3. **Event Handlers**
   - Is `WebMessageReceived` event handler attached?
   - Is the JavaScript `message` event listener registered?
   - Check browser console for JavaScript errors

4. **Common Errors**

   | Error | Cause | Solution |
   |-------|-------|----------|
   | "window.chrome.webview is undefined" | WebView2 not initialized or running in regular browser | Wait for initialization or check runtime |
   | "JSON parse error" | Invalid JSON format | Use JSON.stringify() and escape special characters |
   | "Unknown action" | Action not implemented in C# switch | Add handler for the action |
   | "Property not found" | Missing required field in message | Validate message structure before sending |

### Debug Tools

1. **JavaScript Console Debugging**
   ```javascript
   // Add to your JavaScript to log all communication
   const originalPostMessage = window.chrome.webview.postMessage;
   window.chrome.webview.postMessage = function(message) {
       console.log('Sending to C#:', message);
       originalPostMessage.call(this, message);
   };
   
   window.addEventListener('message', (event) => {
       console.log('Received from C#:', event.data);
   });
   ```

2. **C# Logging**
   ```csharp
   private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
   {
       var message = e.TryGetWebMessageAsString();
       _logger.LogInformation($"Received from JS: {message}");
       // ... rest of handler
   }
   ```

3. **Debug HTML Page**
   - Use the provided `debug-webview.html` for testing communication
   - Located at: `/wwwroot/debug-webview.html`

### Performance Considerations

1. **Message Size**: Keep messages under 1MB for optimal performance
2. **Frequency**: Batch multiple operations when possible
3. **Async Operations**: Use async/await in C# handlers for long-running operations
4. **Error Handling**: Always wrap handlers in try-catch blocks

### Security Best Practices

1. **Validate Input**: Always validate data received from JavaScript
2. **Sanitize Output**: Escape special characters in responses
3. **Limit Actions**: Only expose necessary functionality through the bridge
4. **Path Validation**: Validate file paths to prevent directory traversal

## Version History

- **v1.0** (2024-12-14): Initial documentation
  - Documented core WebView2 communication protocol
  - Added troubleshooting guide
  - Included example implementations
  - Addressed known issues with tab navigation and naming conflicts