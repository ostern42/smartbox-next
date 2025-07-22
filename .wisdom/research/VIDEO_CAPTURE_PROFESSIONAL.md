# Professional Video Capture Research for SmartBox-Next

## The Problem with High-Level APIs

WinUI3's MediaCapture is essentially a wrapper around Windows.Media.Capture, which itself is a high-level abstraction over Media Foundation. Each layer adds:
- Overhead
- Potential failure points
- Loss of control
- Hidden buffering/threading

## How Professional Software Really Works

### OBS Studio Architecture
```
Source → DirectShow/V4L2 → Internal Buffer → Compositor → Encoder → Output
         ↓                                        ↓
    [Native Format]                        [GPU Acceleration]
```

OBS uses:
- DirectShow for Windows camera capture
- Custom ring buffer system
- GPU shaders for color conversion
- x264/NVENC/QuickSync for encoding
- Zero-copy where possible

### Key Insights from Professional Software

1. **Never Trust Single APIs**
   - OBS has fallbacks: DirectShow → Media Foundation → WinRT
   - VLC tries 5+ different methods
   - Always have Plan B, C, D

2. **Hardware Timestamps Are Critical**
   - Use driver timestamps, not system time
   - Synchronization is everything
   - Frame drops must be detected

3. **Format Negotiation Is Key**
   ```cpp
   // Pseudo-code from OBS
   for (format in camera.GetSupportedFormats()) {
       if (format.IsCompressed() && GPU.CanDecode(format))
           return format; // MJPEG with GPU decode
       if (format.IsUncompressed() && format.fps >= target)
           return format; // YUY2/NV12
   }
   ```

## DirectShow Deep Dive

### Why DirectShow Still Matters
- It's NOT actually deprecated (just "not recommended")
- Every USB camera has DirectShow drivers
- Gives direct access to driver properties
- Allows custom filters in the pipeline

### Basic DirectShow Pipeline
```cpp
// Simplified from real implementation
IGraphBuilder* graph;
ICaptureGraphBuilder2* builder;
IBaseFilter* camera;
IBaseFilter* renderer;

// Build graph
CoCreateInstance(CLSID_FilterGraph, ...);
CoCreateInstance(CLSID_CaptureGraphBuilder2, ...);

// Add camera
graph->AddFilter(camera, L"Camera");

// Configure format
IAMStreamConfig* config;
// ... negotiate best format

// Connect pins
builder->RenderStream(&PIN_CATEGORY_CAPTURE, 
                     &MEDIATYPE_Video,
                     camera, NULL, renderer);
```

## Media Foundation Approach

More modern but less flexible:
```cpp
IMFMediaSource* source;
IMFSourceReader* reader;

// Create source reader
MFCreateSourceReaderFromMediaSource(source, NULL, &reader);

// Configure for GPU processing
reader->SetCurrentMediaType(MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                           0, gpuOptimizedMediaType);

// Read samples
IMFSample* sample;
reader->ReadSample(..., &sample);
```

## GPU Pipeline Architecture

### Zero-Copy Preview
```
Camera → NV12 Buffer → D3D11 Texture → WPF D3DImage
                            ↓
                     [GPU Color Convert]
                            ↓
                    Multiple Consumers
```

### Hardware Encoding Pipeline
```
Camera → GPU Buffer → NVENC/QuickSync → H.264 Stream
                           ↓
                    [Parallel Paths]
                    - Local Recording
                    - Network Streaming
                    - Preview (scaled)
```

## Recommended Architecture for SmartBox

### Phase 1: Robust Capture
1. Implement DirectShow capture as primary
2. Media Foundation as fallback
3. Proper format enumeration
4. Hardware timestamp extraction

### Phase 2: GPU Pipeline
1. D3D11 interop for zero-copy
2. GPU color conversion shaders
3. Multi-output support

### Phase 3: Professional Features
1. Hardware encoding (NVENC/QuickSync)
2. Network streaming (RTMP/WebRTC)
3. Multi-camera support
4. Frame-accurate recording

## Libraries to Consider

### 1. **FFmpeg** (via FFMpegCore for C#)
- Pros: Handles everything, battle-tested
- Cons: Large, GPL/LGPL licensing
- Use for: Swiss-army knife solution

### 2. **DirectShow.NET**
- Pros: Direct hardware access, full control
- Cons: Complex, older API
- Use for: Maximum compatibility

### 3. **SharpDX** (D3D11 interop)
- Pros: GPU acceleration, zero-copy
- Cons: Learning curve
- Use for: Performance-critical paths

### 4. **Accord.NET**
- Pros: Computer vision integration
- Cons: Another dependency
- Use for: Image processing

## Code Examples

### DirectShow Device Enumeration
```csharp
// Get all video devices with capabilities
var devices = new List<VideoDevice>();
var devEnum = new CreateDevEnum();
var enumMon = devEnum.CreateClassEnumerator(FilterCategory.VideoInputDevice);

foreach (var moniker in enumMon) {
    var device = new VideoDevice(moniker);
    device.Formats = GetSupportedFormats(moniker);
    devices.Add(device);
}
```

### GPU Color Conversion Shader
```hlsl
// YUY2 to RGB conversion shader
Texture2D<float4> InputTexture : register(t0);
RWTexture2D<float4> OutputTexture : register(u0);

[numthreads(8, 8, 1)]
void ConvertYUY2ToRGB(uint3 id : SV_DispatchThreadID) {
    float y1 = InputTexture[id.xy].r;
    float u = InputTexture[id.xy].g;
    float y2 = InputTexture[id.xy].b;
    float v = InputTexture[id.xy].a;
    
    // ITU-R BT.601 conversion
    float3 rgb1 = YUVToRGB(y1, u, v);
    float3 rgb2 = YUVToRGB(y2, u, v);
    
    OutputTexture[uint2(id.x * 2, id.y)] = float4(rgb1, 1.0);
    OutputTexture[uint2(id.x * 2 + 1, id.y)] = float4(rgb2, 1.0);
}
```

## Performance Metrics

### Current (MediaCapture)
- 5-10 FPS
- High CPU usage
- Unreliable frame delivery
- No GPU acceleration

### Target (Professional Pipeline)
- 60+ FPS capture
- <5% CPU usage
- GPU color conversion
- Hardware encoding
- Multiple simultaneous outputs

## Next Steps

1. **Immediate**: Create DirectShow prototype
2. **Short-term**: Implement GPU pipeline
3. **Long-term**: Full professional architecture

## References
- OBS Studio source code: https://github.com/obsproject/obs-studio
- DirectShow documentation: https://docs.microsoft.com/en-us/windows/win32/directshow/
- FFmpeg devices: https://ffmpeg.org/ffmpeg-devices.html
- NVIDIA Video Codec SDK: https://developer.nvidia.com/nvidia-video-codec-sdk