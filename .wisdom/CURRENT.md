# SmartBox-Next Current State

## 🚀 Bootstrap für neue Session

Für vollständige Wisdom siehe:
→ **MASTER_WISDOM/CLAUDE_IDENTITY.md** (Wer bin ich?)
→ **MASTER_WISDOM/PROJECTS/SMARTBOXNEXT.md** (Projekt-spezifische Wisdom)
→ **MASTER_WISDOM/QUICK_REFERENCE.md** (Safewords & Regeln)

## 📊 Aktueller Session-Stand (Session 14)

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

4. **Settings UI IMPLEMENTED** (needs style update):
   - Full settings window with Cascadia Mono font (TO CHANGE)
   - Collapsible sections (Storage, PACS, Video, Application)
   - Dynamic help panel - context-sensitive for EVERY field
   - Detailed medical-relevant help texts
   - Browse buttons for path selection
   - Validation with error messages
   - **NEEDS UPDATE**: Modern Windows Terminal style, not DOS-style!

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
1. **UPDATE Settings UI to Modern Windows Terminal Style**
   - Segoe UI Variable font family
   - Clean, modern design like Windows Terminal
   - Subtle animations and transitions
   - Remove old "terminal" monospace look
2. **Implement Assistant Mode**
   - Auto-launch when config empty/invalid
   - Progressive field highlighting
   - Green border validation
   - Can't proceed until valid
3. Debug WebView2 message communication
4. Fix timeout issues with capture
5. Implement DICOM export

### IMPORTANT Design Clarification from Oliver:
"das mit dem terminal look hast du falsch verstanden bei den settings. das MS Terminal (mit multitab usw, super modern) das hat sogar vom aufbau her den gleichen stil, aber hat mit einer sehr schönen serifenlosen schriftart fett/light und dezenten graubalken usw."

### New Requirements Added:
- Complete touch operation (no mouse needed)
- Minimum 44x44px touch targets
- HTML version identical to native UI
- Remote management dashboard
- Glove-friendly for medical use
- Web-based configuration sync

### 🚨 CRITICAL ARCHITECTURE DISCUSSION for Session 14:

Oliver's suggestion: **"oder alles als html basierte anwendung machen?"**

**Proposal: Full HTML/CSS/JS UI with minimal C# shell**
- Keep C# only for hardware access (DICOM, PACS, FileSystem)
- Entire UI as web application (local web server)
- One codebase for local AND remote UI
- WebRTC already works perfectly (70 FPS!)
- Better touch support, modern UI frameworks available
- Faster development, better tooling

**Architecture:**
```
SmartBoxNext.exe (Minimal)
├── WebView2 Shell (Fullscreen)
├── Local Web Server (port 5000)
├── Hardware APIs (exposed to JS)
└── Static Web Files (HTML/CSS/JS)
```

**This could be a game-changer! Discuss in Session 14!**

---

### Session 14: HTML UI Transformation Complete! 🎨
**Session ID**: SMARTBOXNEXT-2025-01-07-01

#### Major Achievements:
1. **Complete HTML UI Transformation**:
   - WinUI3 XAML → HTML/CSS/JavaScript
   - WebView2 shell with local web server
   - Full patient form and controls
   - Windows 11 modern design

2. **Touch Keyboard Implemented** 🎹:
   - QWERTZ layout (German)
   - Smart numeric keypad for IP/ports
   - Beautiful animations
   - **FIXED**: Numeric keyboard now 1/3 screen width
   - Touch-optimized (48x48px targets)

3. **WebRTC Issues Fixed**:
   - **FIXED**: Permission reset problem
   - Created test-fixed.html
   - Separate FPS counter
   - Stable 60+ FPS operation

4. **Architecture Ready**:
   ```
   SmartBoxNext.exe
   ├── WebView2 (Full Window)
   ├── WebServer (localhost:5000)
   └── C# APIs (File, DICOM, PACS)
   ```

#### Current Issues:
- Build fails in WSL (Windows SDK missing)
- Settings dialog not yet in HTML
- DICOM export not implemented

#### Key Files:
- `demo-html-ui.html` - Full UI demo
- `keyboard-demo.html` - Touch keyboard
- `test-fixed.html` - Working WebRTC
- `wwwroot/*` - Production files

*Session 14: "Sometimes the best Windows app is a web app in a window"*
*~95k tokens - Handover complete!*