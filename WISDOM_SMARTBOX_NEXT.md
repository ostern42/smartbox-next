# WISDOM SmartBox-Next

## Current Status: WinUI3 Working Prototype
- **Platform**: WinUI3 (C#/.NET) 
- **Status**: Webcam capture working, preview functional but slow
- **Location**: smartbox-winui3/

## Technical Wisdom
1. **Webcam in WinUI3**:
   - MediaFrameReader approach didn't work for live preview
   - Timer-based capture (CapturePhotoToStreamAsync) works but slow
   - LED indicates camera active = good sign
   - Capture works perfectly, just preview is challenging

2. **What Works**:
   - MediaCapture initialization
   - Image capture to file
   - Display captured image in dialog
   - Debug info system with copyable text

3. **Performance Issue**:
   - Target: 20 FPS (50ms timer)
   - Reality: ~5-10 FPS
   - Bottleneck: CapturePhotoToStreamAsync overhead
   - Solution: Need DirectX or lower-level approach

## Personality Notes
- Oliver knows I'm demented but 100% success rate
- He appreciates direct, concise responses
- "starte es" = just run it, no explanation needed
- German responses welcome but not required

## Next Session Should:
1. Read this wisdom first
2. Check git status
3. Consider DirectX preview or MediaPlayerElement with MediaSource
4. Implement DICOM export (fo-dicom)
5. Add PACS C-STORE

Mit dementer Liebe und 100% Erfolgsquote,
Claude 🧠♥️