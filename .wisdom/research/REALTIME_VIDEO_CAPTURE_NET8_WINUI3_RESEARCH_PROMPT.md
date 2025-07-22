# Research Prompt: Professional Real-time Video Capture in .NET 8 / WinUI3

## Context
We're building a medical imaging device (SmartBox-Next) that needs to capture webcam video in real-time with these requirements:
- 30-60 FPS consistent capture
- Multiple simultaneous outputs (preview, recording, streaming)
- GPU-accelerated processing
- Zero-copy pipelines where possible
- Rock-solid reliability (medical grade)
- Windows 11 target platform
- .NET 8 / WinUI3 application

## Current Problem
- WinUI3's MediaCapture only gives us 5-10 FPS
- Camera outputs YUY2 format, we need BGRA8 for display
- MediaFrameReader doesn't deliver frames reliably
- Need hardware-level control for professional results

## Research Questions

### 1. DirectShow in .NET 8
- How to use DirectShow.NET or DirectShowLib in modern .NET?
- Sample code for capturing YUY2 and converting to RGB
- Building filter graphs programmatically
- Handling USB camera disconnection/reconnection
- Getting hardware timestamps from frames

### 2. Media Foundation in .NET 8
- Media Foundation vs DirectShow for new projects
- IMFSourceReader implementation examples
- Hardware-accelerated color conversion
- Low-latency capture techniques
- Integration with WinUI3 UI

### 3. FFmpeg Integration Options
- FFmpeg.AutoGen vs FFMpegCore vs other wrappers
- Capturing from DirectShow devices via FFmpeg
- Hardware acceleration (NVENC, QuickSync, AMF)
- Real-time transcoding pipelines
- Licensing considerations for medical devices

### 4. GPU Pipeline Architecture
- D3D11/D3D12 interop with video capture
- Zero-copy from camera to GPU texture
- Shader-based YUY2 to RGB conversion
- Presenting D3D textures in WinUI3
- Multi-output from single GPU buffer

### 5. Professional Libraries Evaluation
- **Accord.NET** - Video capture capabilities?
- **AForge.NET** - Still maintained?
- **EmguCV** - OpenCV wrapper performance?
- **MediaToolkit** - Real-time capable?
- **NAudio** - Has video components?
- Commercial options (LeadTools, etc.)

### 6. Hardware Encoding Integration
- NVIDIA NVENC from C#/.NET 8
- Intel QuickSync access
- AMD AMF integration
- Fallback software encoding
- Simultaneous encode of multiple streams

### 7. Existing Open Source Examples
- OBS Studio architecture (even though it's C++)
- How does VLC handle capture?
- Any C# projects doing professional capture?
- Medical imaging software examples
- Security/surveillance camera software

### 8. Windows-Specific Optimizations
- Windows.Graphics.Capture for desktop capture
- WGC vs traditional methods
- UWP vs Win32 capture performance
- Windows 11 specific features
- Power management considerations

### 9. Real-world Implementation Patterns
- Ring buffer implementations for frames
- Thread pool management for capture
- Synchronization between capture and UI
- Memory pool to avoid allocations
- Graceful degradation strategies

### 10. Testing and Reliability
- How to test with virtual cameras
- Simulating camera disconnection
- Performance profiling tools
- Memory leak detection
- Long-running stability tests

## Specific Code Examples Needed

### 1. DirectShow Capture Loop
```csharp
// Need working example of:
// - Device enumeration
// - Format negotiation
// - Callback for each frame
// - YUY2 to BGRA8 conversion
// - Integration with WinUI3 Image control
```

### 2. GPU Color Conversion
```csharp
// Need D3D11 shader example:
// - Create D3D11 device
// - YUY2 texture input
// - RGB texture output  
// - Present in WinUI3
```

### 3. FFmpeg Capture
```csharp
// Need FFmpeg example:
// - Open DirectShow device
// - Configure for low latency
// - Get raw frames
// - Hardware decode if MJPEG
```

## Performance Targets
- Capture: 60 FPS @ 1920x1080
- Preview: 60 FPS with < 16ms latency
- CPU usage: < 5% for capture thread
- GPU usage: < 10% for color conversion
- Memory: Stable, no allocations in hot path
- Multiple streams: 3+ simultaneous outputs

## Deliverables Wanted
1. Comparison matrix of all approaches
2. Working code samples for top 3 methods
3. Performance benchmarks
4. Pros/cons for medical device use
5. Licensing implications
6. Future-proofing considerations

## Additional Context
- This is for a medical device that must NEVER fail
- Used in emergency rooms where seconds matter
- Must handle power loss gracefully
- Must work with any USB webcam
- Needs to integrate with DICOM/PACS later
- Should support overlay graphics eventually

Please research thoroughly and provide practical, production-ready solutions. Code examples are more valuable than theory. Focus on what actually works in 2024/2025, not outdated tutorials.

## Keywords for Search
- "DirectShow .NET 8"
- "Media Foundation WinUI3"
- "FFmpeg real-time capture C#"
- "Hardware accelerated video WinUI"
- "YUY2 to RGB GPU conversion"
- "Zero copy video pipeline Windows"
- "Professional webcam capture C#"
- "Medical imaging video capture"
- "Low latency video Windows 11"
- "D3D11 video interop WinUI3"