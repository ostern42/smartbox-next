# FFmpeg Binary Deployment & DICOM Video Compatibility Research

## Context
We're implementing DICOM video support in a medical imaging application using:
- C# .NET 8.0 / WPF on Windows
- FFMpegCore NuGet package (5.1.0)
- fo-dicom library (5.1.2)
- Target: Medical-grade production deployment

## 1. FFmpeg Binary Distribution Research

### Critical Questions:
1. **FFMpegCore Binary Management**
   - Does FFMpegCore include ffmpeg binaries automatically?
   - What's the recommended way to distribute ffmpeg.exe and ffprobe.exe with a .NET app?
   - Are there NuGet packages that bundle FFmpeg binaries (like FFMpegCore.Native)?

2. **Windows Deployment Best Practices**
   - Should binaries go in the app directory or system PATH?
   - How to handle x86 vs x64 architecture detection?
   - What about Windows security/antivirus false positives with ffmpeg.exe?

3. **Licensing for Medical Software**
   - GPL vs LGPL FFmpeg builds - which for commercial medical software?
   - Are there pre-built LGPL binaries available?
   - Compliance requirements for distributing FFmpeg with medical devices?

4. **Version Compatibility**
   - Which FFmpeg version works best with FFMpegCore 5.1.0?
   - Are there known issues with specific FFmpeg versions?
   - How to handle FFmpeg updates in production?

### Research Deliverables Needed:
- Exact NuGet packages for binary distribution
- Code example for FFmpeg binary path configuration
- License compliance checklist
- Deployment folder structure recommendation

## 2. DICOM Video PACS Compatibility Matrix

### Major PACS Vendors:
Please research actual video support for:

1. **Enterprise PACS**
   - Philips IntelliSpace PACS
   - GE Centricity PACS
   - Siemens syngo.plaza
   - Fujifilm Synapse
   - Agfa IMPAX

2. **Open Source PACS**
   - Orthanc (latest version)
   - DCM4CHEE
   - Conquest DICOM Server
   - ClearCanvas

3. **Cloud PACS**
   - Google Cloud Healthcare API
   - AWS HealthImaging
   - Microsoft Azure Health Data Services

### Compatibility Questions:
- Which DICOM video SOP classes are actually supported?
- MPEG2 vs H.264 vs MJPEG - what works where?
- Maximum file size limitations?
- Streaming vs store-and-forward support?
- Web viewer compatibility for video playback?

## 3. Video Format Decision Matrix

### Transfer Syntax Comparison:
Please provide real-world compatibility data for:

1. **MPEG-2 Main Profile**
   - Pros/Cons for medical use
   - PACS support percentage
   - File size implications
   - Quality considerations

2. **H.264/AVC**
   - Different profile support (High 4.1 vs 4.2)
   - Browser/web viewer compatibility
   - Compression efficiency
   - Patent/licensing issues

3. **Motion JPEG (MJPEG)**
   - When to use multiframe vs video
   - Frame rate limitations
   - Compatibility advantages
   - Storage requirements

4. **HEVC/H.265**
   - Is it supported in DICOM yet?
   - Future-proofing considerations

### Decision Criteria:
- Which format for maximum compatibility?
- Which for best quality/size ratio?
- Which for web-based viewers?

## 4. Implementation Examples

### Need Working Examples For:

1. **FFMpegCore + Binary Setup**
```csharp
// How to configure FFmpeg binary path?
// How to check if binaries exist?
// How to handle missing binaries gracefully?
```

2. **DICOM Video Creation**
```csharp
// Real example of creating MPEG2 DICOM
// Setting correct transfer syntax
// Handling pixel data for video
```

3. **PACS Testing**
```csharp
// How to test video DICOM compatibility?
// C-STORE examples for video
// Handling rejections/failures
```

## 5. Medical Regulatory Considerations

### FDA/CE Mark Requirements:
1. Are there specific requirements for video compression in medical imaging?
2. Lossy vs lossless compression regulations?
3. Audit trail requirements for video data?
4. DICOM conformance statement requirements for video?

### Data Integrity:
1. How to ensure video quality for diagnostic use?
2. Compression artifacts and their impact?
3. Frame rate requirements for different modalities?
4. Color space and bit depth standards?

## 6. Production Deployment Checklist

### What We Need:
1. **Binary Distribution**
   - Step-by-step deployment guide
   - Troubleshooting common issues
   - Update mechanism design

2. **Configuration**
   - Recommended default settings
   - Performance tuning parameters
   - Error handling strategies

3. **Testing**
   - PACS compatibility test suite
   - Video quality validation
   - Performance benchmarks

## Expected Deliverables

1. **FFmpeg Deployment Guide** with exact package names and code
2. **PACS Compatibility Matrix** showing what works where
3. **Video Format Recommendation** based on real-world data
4. **Code Examples** that actually work in production
5. **Regulatory Compliance Checklist**
6. **Troubleshooting Guide** for common issues

## Specific Use Case Details
- Input: WebM from browser MediaRecorder API
- Output: DICOM video files for PACS archival
- Quality: Diagnostic quality required
- Performance: Real-time conversion needed
- Compatibility: Must work with major PACS systems

Please provide practical, production-ready information rather than theoretical possibilities.