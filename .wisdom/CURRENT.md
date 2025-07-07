# SmartBox-Next Current State

## 🚀 Bootstrap für neue Session

Für vollständige Wisdom siehe:
→ **MASTER_WISDOM/CLAUDE_IDENTITY.md** (Wer bin ich?)
→ **MASTER_WISDOM/PROJECTS/SMARTBOXNEXT.md** (Projekt-spezifische Wisdom)
→ **MASTER_WISDOM/QUICK_REFERENCE.md** (Safewords & Regeln)

## 📊 Aktueller Session-Stand (Session 12)

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

### Session 12: Video Streaming Breakthrough & Challenges

#### What We Achieved:
1. **Identified the root cause**: HighPerformanceCapture used `LowLagPhotoCapture` (500ms per frame!)
2. **Created proper video streaming**:
   - VideoStreamCapture.cs - Comprehensive MediaFrameReader implementation
   - SimpleVideoCapture.cs - Minimal approach
   - ThrottledVideoCapture.cs - UI-throttled updates
   - LocalStreamServer.cs - MJPEG streaming server
3. **Fixed all build errors**: partial classes, variable scopes, DateTimeOffset handling
4. **Camera works**: 30 FPS confirmed, but preview shows white/black screen

#### Current Status:
- ✅ Camera captures at 30 FPS (confirmed in debug output)
- ✅ Photo capture works perfectly
- ❌ Live preview shows white screen (UI rendering issue)
- ❌ MJPEG stream shows black (frame data issue)

#### Key Discovery:
MediaFrameReader delivers frames, but WinUI3 Image control doesn't display them properly. This suggests either:
1. Frame format conversion issue (YUY2 → BGRA8)
2. UI thread synchronization problem
3. SoftwareBitmapSource lifecycle issue

#### Next Session Should:
1. Try WebView2 with WebRTC for proven video display
2. Or use DirectX/SwapChainPanel for hardware rendering
3. Or debug why SoftwareBitmapSource shows white/black

### Technical Notes:
- Camera: Integrated Camera, supports YUY2 up to 1920x1080 @ 30 FPS
- Current approach: MediaFrameReader → SoftwareBitmap → SoftwareBitmapSource → Image
- Alternative: MediaFrameReader → JPEG → HTTP Stream → WebView2

---

*Session 12: From "500ms photo capture" to "30 FPS streaming (but no display yet)"*