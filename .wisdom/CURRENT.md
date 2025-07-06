# SmartBox-Next Current State

## Current Implementation: WinUI3
- **Platform**: WinUI3 (C#/.NET)
- **Location**: smartbox-winui3/
- **Status**: Working prototype with webcam
  - ✅ Patient information form
  - ✅ Webcam preview (20 FPS using timer-based capture)
  - ✅ Image capture with preview dialog
  - ✅ Debug info system
  - ⏳ DICOM export (TODO)
  - ⏳ PACS C-STORE (TODO)

## Technical Details
- **Webcam**: MediaCapture with timer-based preview (50ms interval)
- **Preview**: Updates Image control with captured frames
- **Capture**: Full resolution JPEG to Pictures/SmartBoxNext/
- **Known Issue**: Preview is ~5-10 FPS despite 20 FPS target (capture overhead)

## Next Steps
1. **DICOM Export** - Integrate fo-dicom library
2. **PACS Connection** - Implement C-STORE
3. **Performance** - Consider DirectX preview for better FPS
4. **Worklist** - Add MWL query functionality