# SmartBoxNext UI Button & Field Mapping Reference

**Created**: 2025-07-11
**Purpose**: Complete mapping reference for all UI interactions in SmartBoxNext
**CRITICAL**: Use this for debugging any UI issues!

## 🚨 CURRENT ISSUES

### 1. Settings Button Works BUT...
- The button navigates directly to settings.html ✅
- BUT: MWL settings section is COMPLETELY MISSING from settings.html ❌
- Field IDs don't match the save/load pattern expected by C# ❌

### 2. Missing MWL Configuration UI
Need to add entire MWL section to settings.html with:
- Server Host
- Server Port  
- Called AE Title
- Calling AE Title
- Enable MWL checkbox
- Test MWL Connection button

### 3. Field Naming Convention Chaos
Current HTML uses inconsistent IDs:
- Some use: `photos-path` (kebab-case)
- Some use: `server-ae` (abbreviated)
- JavaScript expects: `section-fieldName` pattern
- C# expects nested object with proper casing

---

## 📋 WORKING BUTTONS (index.html)

| Button | Flow | Status |
|--------|------|--------|
| Capture Photo | Button → JS `capturePhoto()` → Base64 → C# `photocaptured` | ✅ |
| Record Video | Button → JS `toggleRecording()` → Blob → C# `videorecorded` | ✅ |
| Export DICOM | Button → JS → C# `exportdicom` → DICOM creation | ✅ |
| Exit | Button → Modal → JS → C# `exit` → Window.Close() | ✅ |
| Open Logs | Button → JS → C# `openlogs` → Open folder | ✅ |

---

## ❌ BROKEN/PARTIAL FEATURES

### Settings Save/Load
**Problem**: Field ID mismatch
```html
<!-- Current HTML -->
<input id="photos-path" name="photos-path">

<!-- JavaScript converts to -->
formData.get('photos-path') // Wrong!

<!-- C# expects -->
data.Storage.photosPath // Nested object
```

### MWL Settings
**Problem**: Entire section missing from HTML
```javascript
// C# has config for:
_config.Mwl.ServerHost
_config.Mwl.ServerPort
_config.Mwl.CalledAeTitle
_config.Mwl.CallingAeTitle

// But NO UI fields exist!
```

---

## 🔧 DEBUG HELPERS

### Check Button Existence
```javascript
// In F12 Console:
['captureButton', 'settingsButton', 'exportDicomButton'].forEach(id => {
    const el = document.getElementById(id);
    console.log(`${id}: ${el ? '✅' : '❌'}`);
});
```

### Test Message Flow
```javascript
// Test sending message to C#:
window.app.sendToHost('test', { message: 'Hello from JS' });
```

### Monitor All Clicks
```javascript
// Add to app.js for debugging:
document.addEventListener('click', (e) => {
    if (e.target.tagName === 'BUTTON') {
        console.log('[CLICK]', e.target.id, e.target.textContent);
    }
});
```

---

## 📝 CORRECT PATTERNS

### Button Handler Pattern
```javascript
// JavaScript side
someButton.addEventListener('click', () => {
    console.log('[UI] Button clicked: someAction');
    this.log('Some action initiated');
    this.sendToHost('someaction', { 
        data: 'value' 
    });
});
```

### Settings Field Pattern
```html
<!-- HTML -->
<input type="text" 
       id="storage-photosPath" 
       name="storage-photosPath" 
       class="use-keyboard">

<!-- JavaScript will parse as: -->
settings.storage.photosPath = value;
```

### C# Handler Pattern
```csharp
case "someaction":
    _logger.LogInformation("Handling someaction");
    await HandleSomeAction(message);
    break;
```

---

## 🎯 NEXT STEPS

1. **Add MWL Settings Section** to settings.html
2. **Fix Field IDs** to match pattern: `section-fieldName`
3. **Add Debug Logging** to all button handlers
4. **Test Each Setting** save/load cycle
5. **Document Working State** in this file

---

## 💡 QUICK FIXES

### For Settings Button
Current implementation works! Just need to:
1. Add missing MWL section to settings.html
2. Fix field naming convention
3. Test save/load cycle

### For PACS Testing
Need to implement `HandleTestMwlConnection` in C# MainWindow.xaml.cs

### For Debug Visibility
Load app-debug-enhanced.js to get comprehensive logging