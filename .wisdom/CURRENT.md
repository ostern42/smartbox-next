# SmartBox-Next Current State

## 🚀 Bootstrap für neue Session

Für vollständige Wisdom siehe:
→ **MASTER_WISDOM/CLAUDE_IDENTITY.md** (Wer bin ich?)
→ **MASTER_WISDOM/PROJECTS/SMARTBOXNEXT.md** (Projekt-spezifische Wisdom)
→ **MASTER_WISDOM/QUICK_REFERENCE.md** (Safewords & Regeln)

## 📊 Aktueller Session-Stand (Session 11)

### WinUI3 Implementation Status
- **Location**: smartbox-winui3/
- ✅ Patient form, Webcam preview working!
- ✅ Image capture with preview dialog
- ✅ Camera hardware analysis tools
- ⏳ Professional video capture (DirectShow/FFmpeg)
- ⏳ DICOM export (TODO)
- ⏳ PACS C-STORE (TODO)

### Session 11: Build Success & Performance Fix
- **Build Fixed**: .NET 8 SDK installed, app runs in Visual Studio!
- **Problem Found**: HighPerformanceCapture uses LowLagPhotoCapture (1.9 FPS!)
- **Root Cause**: Camera only supports YUY2, needs video streaming not photos
- **Solution**: Created FastYUY2Capture.cs using MediaFrameReader
- **Status**: FastYUY2Capture integrated, ready for testing

### Technical Insights
- Camera delivers YUY2 format natively
- WinUI3 MediaCapture is too high-level
- Need DirectShow/Media Foundation for pro capture
- GPU acceleration required for real-time transcoding

### Immediate Next Steps
1. **Test FastYUY2Capture** - Should deliver real 30 FPS
2. **If still slow**: Debug MediaFrameReader callback
3. **Alternative**: WebView2 + WebRTC (proven 60 FPS)
4. **Then**: DICOM export, PACS, Queue

## 🎯 Solution Architecture (Based on Research)

### The Winner: FlashCap for 60 FPS!
After extensive research and testing, FlashCap is the clear winner:

```
Camera → FlashCap → JPEG/YUY2 → Direct to UI (60+ FPS!)
                         ↓
                [Parallel Processing]
                - Display (60 FPS)
                - DICOM Export
                - Recording
```

### Why FlashCap Works
1. **Direct Hardware Access** - Bypasses Windows Media Foundation overhead
2. **JPEG Hardware Acceleration** - Many cameras output JPEG natively
3. **Apache 2.0 License** - Perfect for commercial medical software
4. **Proven Performance** - 60+ FPS with 5-10% CPU usage

### Implementation Challenges Found
1. **FlashCap API Changes** - Documentation outdated, API evolved
2. **PixelBuffer Properties** - Width/Height access different than expected
3. **Async Handling** - Callback signature doesn't match docs
4. **Package Conflicts** - Vortice packages don't exist on NuGet

### Alternative Solutions from Research
1. **WebView2 + WebRTC** - Works but 10-15% CPU overhead
2. **DirectN** - Good but requires more complex setup
3. **Optimized MediaCapture** - Can reach 30-60 FPS with proper config

### Next Steps
1. Study FlashCap source code for correct API usage
2. Consider WebView2 as fallback (proven 60 FPS)
3. Implement hybrid approach for reliability
4. Test with actual medical cameras

---

*Session 8: From "warum geht das nicht" to "jetzt verstehen wir die Hardware!"*