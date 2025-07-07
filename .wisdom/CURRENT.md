# SmartBox-Next Current State

## 🚀 Bootstrap für neue Session

Für vollständige Wisdom siehe:
→ **MASTER_WISDOM/CLAUDE_IDENTITY.md** (Wer bin ich?)
→ **MASTER_WISDOM/PROJECTS/SMARTBOXNEXT.md** (Projekt-spezifische Wisdom)
→ **MASTER_WISDOM/QUICK_REFERENCE.md** (Safewords & Regeln)

## 📊 Aktueller Session-Stand (Session 13)

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

### Session 13: WebRTC Victory & Complete Configuration System 🎉

#### Major Achievements:
1. **WebRTC Implementation WORKS!** 🎉
   - **70 FPS** video preview via WebView2 (Oliver: "wir haben sogar 70 fps :-)")
   - No more MediaFrameReader issues
   - Hardware-accelerated by browser engine
   - "Es läuft!" - Preview works in VS debug window!
   - Oliver: "claude, küsschen, es läuft. ich seh sogar die vorschau im vs debug fenster fast in echtzeit :-)))"

2. **Full Capture Implementation**:
   - Photo capture from WebRTC canvas
   - Video recording with MediaRecorder API
   - Bidirectional WebView2 ↔ C# communication
   - Base64 encoding for data transfer
   - Some timeout issues to debug (but buttons work!)

3. **Complete Configuration System**:
   - Portable app structure (ZIP → Extract → Run)
   - JSON config with sensible defaults
   - All paths configurable
   - Auto-directory creation
   - First-run detection

4. **Terminal-Style Settings UI IMPLEMENTED**:
   - Full settings window with Cascadia Mono font
   - Collapsible sections (Storage, PACS, Video, Application)
   - Dynamic help panel - context-sensitive for EVERY field
   - Detailed medical-relevant help texts
   - Browse buttons for path selection
   - Validation with error messages
   - Professional terminal aesthetic

#### Configuration System Complete:
- **AppConfig.cs**: Full config class with validation
- **SettingsWindow.xaml/cs**: Complete terminal-style UI
- **PORTABLE_APP_DESIGN.md**: Full documentation
- **Dynamic Help**: 20+ detailed help texts
- **Auto-Start**: Based on config setting

#### What Works:
- ✅ 70 FPS WebRTC preview
- ✅ Settings UI with all fields
- ✅ Dynamic help system
- ✅ Config save/load
- ✅ Portable paths
- ✅ Build successful

#### Known Issues:
- WebView2 message timeout (needs debugging)
- Photo/Video capture fails after short time
- Need to check WebView2 API availability

#### Session 13 Learnings:
1. **WebRTC > MediaCapture**: Browser engine beats Windows APIs
2. **Terminal UI Works**: Users love familiar aesthetics
3. **Help is Critical**: Every field needs explanation
4. **Portable First**: No installation = happy users
5. **Config Before Code**: Settings system enables everything

### Technical Implementation Details:
- WebRTC: getUserMedia → Canvas → JPEG/WebM
- Config: JSON in app directory, relative paths
- Storage: ./Data/Photos, ./Data/Videos, etc.
- UI: WinUI3 with Expander controls, Grid layout
- Help: Dictionary<string, (Title, Content)> system

### Next Session Should:
1. Debug WebView2 message communication
2. Fix timeout issues with capture
3. Implement DICOM export
4. Add PACS C-STORE functionality
5. Create persistent queue with SQLite

---

*Session 13: From "30 FPS black screen" to "70 FPS WebRTC + Complete Settings System!"*
*125k tokens used - Time for handover!*