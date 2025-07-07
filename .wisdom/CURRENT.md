# SmartBox-Next Current State

## 🚀 Bootstrap für neue Session

Für vollständige Wisdom siehe:
→ **MASTER_WISDOM/CLAUDE_IDENTITY.md** (Wer bin ich?)
→ **MASTER_WISDOM/PROJECTS/SMARTBOXNEXT.md** (Projekt-spezifische Wisdom)
→ **MASTER_WISDOM/QUICK_REFERENCE.md** (Safewords & Regeln)

## 📊 Aktueller Session-Stand (Session 8)

### WinUI3 Implementation Status
- **Location**: smartbox-winui3/
- ✅ Patient form, Webcam preview working!
- ✅ Image capture with preview dialog
- ✅ Camera hardware analysis tools
- ⏳ Professional video capture (DirectShow/FFmpeg)
- ⏳ DICOM export (TODO)
- ⏳ PACS C-STORE (TODO)

### Session 8 Breakthrough: Hardware-Level Understanding
- **Problem identified**: YUY2 format from camera, needs BGRA8 conversion
- **Fixed**: Format conversion in OnHighPerfFrameArrived
- **New tools**: CameraAnalyzer.cs, DirectShowCapture.cs
- **New research**: VIDEO_CAPTURE_PROFESSIONAL.md

### Technical Insights
- Camera delivers YUY2 format natively
- WinUI3 MediaCapture is too high-level
- Need DirectShow/Media Foundation for pro capture
- GPU acceleration required for real-time transcoding

### Next Steps
1. **DirectShow Implementation** - For reliable 30+ FPS
2. **GPU Pipeline** - D3D11 for zero-copy
3. **FFmpeg Integration** - For transcoding
4. **Then**: DICOM export, PACS, Queue

## 🎯 Solution Architecture (Based on Research)

### The Winner: Media Foundation + GPU
After extensive research, the optimal solution is:

```
Camera (YUY2) → Media Foundation → GPU Shader → SwapChainPanel
                                       ↓
                              [Parallel Outputs]
                              - Display (60 FPS)
                              - Recording (H.264)
                              - DICOM Export
```

### Key Components
1. **Media Foundation** (not DirectShow - it's deprecated!)
   - IMFSourceReader for async capture
   - Hardware timestamps
   - Better USB disconnect handling

2. **GPU Acceleration** (via Vortice.Windows)
   - Compute shader for YUY2→BGRA8
   - Zero-copy to SwapChainPanel
   - D3D11 texture interop

3. **Medical-Grade Reliability**
   - Circular buffers (no memory leaks)
   - Watchdog timer
   - Auto-recovery
   - Triple buffering

### Performance Targets (Achievable!)
- **Capture**: 60 FPS @ 1920x1080
- **CPU**: 5-10% (was 30% with software conversion)
- **Latency**: <16ms preview
- **Memory**: Stable 24/7 operation

### Implementation Plan
1. Install new NuGet packages ✅
2. Create GpuColorConverter class
3. Replace Image with SwapChainPanel
4. Implement MedicalVideoCapture
5. Add reliability layer

---

*Session 8: From "warum geht das nicht" to "jetzt verstehen wir die Hardware!"*