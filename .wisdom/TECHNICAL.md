# SmartBox-Next Technical Wisdom

## WinUI3 Implementation (Current)

### Video Capture Performance (Session 6)

#### MediaFrameReader Issues
**Problem**: MediaFrameReader starts successfully but OnFrameArrived never fires
**Symptoms**:
- FrameSources found
- CreateFrameReaderAsync succeeds
- StartAsync returns Success
- No frames delivered

**Possible Causes**:
1. Parallel MediaCapture usage (timer + FrameReader)
2. Missing initialization parameters
3. WinUI3 specific requirements differ from UWP

**Current Workaround**: Timer-based with CapturePhotoToStreamAsync (5-10 FPS)

#### Working Approaches
1. **Timer-based**: Reliable but slow (5-10 FPS)
2. **CapturePhotoToStorageFileAsync**: Works perfectly for single captures

### Build Issues Fixed

#### DateTimeOffset in WinUI3
DatePicker.Date is nullable DateTimeOffset?
```csharp
// Correct handling
DateTime? birthDate = null;
if (BirthDate.Date != null)
{
    birthDate = BirthDate.Date.Value.DateTime;
}
```

#### Async in DispatcherQueue
Don't await DispatcherQueue.TryEnqueue:
```csharp
// Wrong
await DispatcherQueue.TryEnqueue(async () => {});

// Correct
DispatcherQueue.TryEnqueue(async () => {});
```

#### DicomPixelData Creation
```csharp
// Wrong
dataset.Add(new DicomPixelData(dataset) { ... });

// Correct
var dicomPixelData = DicomPixelData.Create(dataset, true);
dicomPixelData.AddFrame(pixelDataBuffer);
```

### Performance Targets
- Current: 5-10 FPS (timer-based)
- Target: 25-60 FPS (MediaFrameReader)
- Capture latency: <100ms
- DICOM export: <2s

### Architecture Decisions
- WinUI3 over UWP for modern Windows development
- .NET 8 for latest features
- fo-dicom for DICOM handling
- MediaCapture for camera access

## Original Go/Wails Implementation

## DICOM Implementation Learnings

### What Doesn't Work
1. **Direct JPEG embedding** - MicroDicom rejects it
2. **Implicit VR with JPEG** - Violates DICOM standard
3. **Complex implementations** - More code = more problems

### What Works (Maybe)
1. **RGB conversion** - JPEG → RGB pixel data
2. **Explicit VR throughout** - Consistency is key
3. **MINIMAL approach** - 50 lines > 500 lines

### The 10 DICOM Attempts
1. simple_dicom.go - MVP with JPEG+Metadata ❌
2. real_dicom.go - RGB conversion (deleted) ❌
3. dicom_writer.go - Too complex (deleted) ❌
4. jpeg_dicom.go - JPEG direct ❌
5. minimal_dicom.go - RGB minimal (deleted) ❌
6. smart_dicom.go - With overlay (deleted) ❌
7. simple_jpeg_dicom.go - Implicit VR (deleted) ❌
8. microdicom_compatible.go - Explicit VR (deleted) ❌
9. cambridge_style_dicom.go - Like v1 ❌
10. working_dicom.go - MINIMAL RGB ⏳

### Critical DICOM Tags for MicroDicom
- Transfer Syntax: 1.2.840.10008.1.2.1 (Explicit VR Little Endian)
- Photometric: RGB (not MONOCHROME2)
- Samples per Pixel: 3
- Planar Configuration: 0
- Bits Allocated/Stored: 8
- High Bit: 7

### Go DICOM Library Limitations
- suyashkumar/dicom: Good for parsing, bad for creating
- No native JPEG compression support
- No encapsulated pixel data writers
- Manual binary writing often required

## Architecture Decisions

### Why Wails?
- Native Go backend
- Modern web frontend
- No Electron bloat
- Direct hardware access

### Project Structure
```
smartbox-next/
├── backend/
│   ├── api/
│   ├── capture/
│   ├── dicom/      # The graveyard of attempts
│   ├── license/
│   ├── overlay/
│   └── trigger/
├── frontend/
│   └── src/
│       ├── AppCompact.vue  # Current UI
│       └── (many backups)
└── app.go          # Main application
```

### Frontend Evolution
- App.vue → LiveVideo.vue → DicomApp.vue → AppCompact.vue
- Each iteration more focused
- Compact layout works best for medical UI

## Debugging Tips

### DICOM Not Opening?
1. Check file size (should be > 1MB for 640x480 RGB)
2. Use hex editor to verify structure
3. Check Transfer Syntax matches data
4. Verify RGB conversion worked
5. Try external validator (dcmtk)

### Common Errors
- "234kb" = JPEG data, not RGB
- "CS0117" = Wrong property name (Session 87!)
- "Invalid DICOM" = Structure issue
- "Cannot display" = Pixel data format wrong

## External Tools Fallback
If Go implementation fails:
1. Create uncompressed DICOM
2. Use dcmtk to compress: `dcmcjpeg input.dcm output.dcm`
3. Or use Python with pydicom
4. Or shell out to CamBridge v1

## Performance Considerations
- RGB conversion is expensive
- Consider caching converted data
- Lazy loading for previews
- Background DICOM creation