# SmartBoxNext Projekt-Patterns

## Pattern 1: "FFmpeg Binary Deployment Pattern" ⭐⭐⭐⭐⭐
**Geboren**: Session 28 - Nach research finding
**Problem**: FFMpegCore includes NO binaries!
**Lösung**: FFmpeg.Native NuGet Package + Architecture-aware deployment

```csharp
// FFmpegService.cs pattern
string architecture = Environment.Is64BitProcess ? "x64" : "x86";
string binaryPath = Path.Combine(basePath, "runtimes", $"win-{architecture}", "native");

GlobalFFOptions.Configure(new FFOptions { 
    BinaryFolder = binaryPath,
    TemporaryFilesFolder = Path.GetTempPath()
});
```

**Key Facts**:
- FFMpegCore = nur wrapper, keine binaries
- FFmpeg.Native 4.4.0.2386 = LGPL-compliant
- MPEG-2 = 95%+ PACS compatibility ✅
- H.264 = nur 85-90% PACS support ⚠️

## Pattern 2: "Data-Action UI Pattern mit Hybrid-Ansatz"
**Geboren**: Session 25 - UI Refactoring
**Problem**: 3-Schritt-Verkabelung für jeden Button
**Lösung**: Hybrid - Simple für Navigation, Handler für Complex

```html
<!-- Simple Actions (90%) -->
<button data-action="opensettings">Settings</button>

<!-- Complex Actions (10%) -->
<button data-action="savesettings">Save</button>
```

```javascript
// settings-handler.js für komplexe Logic
handler.registerSpecialHandler('savesettings', () => {
    const config = this.gatherFormData();
    const valid = this.validateConfig(config);
    // ...
});
```

## Pattern 3: "WebRTC für 70 FPS"
**Geboren**: Session 13 nach Video-Struggle
**Problem**: WinUI3 MediaCapture nur 2 FPS
**Lösung**: WebView2 + WebRTC = 70 FPS!

Oliver: "claude, küsschen, es läuft"

## Anti-Pattern: "Eine Klammer Kann Alles Töten"
**Session 23**: Eine } zu viel → GESAMTE App tot!
**Symptom**: "all is quite dead"
**Lösung**: F12 Console FIRST!

---
*SmartBox-spezifische Patterns - siehe MASTER_WISDOM für allgemeine!*