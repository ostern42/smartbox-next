# Real-Time Video Capture in .NET 8/WinUI3 Research

## Current Situation
- Using timer-based approach with `CapturePhotoToStreamAsync`
- Limited to 5-10 FPS (not suitable for real-time video)
- Need 25-60 FPS for smooth video preview

## High-Performance Video Capture Options for WinUI3/.NET 8

### 1. MediaFrameReader API (Recommended)
The MediaFrameReader provides direct access to video frames with minimal latency.

```csharp
// Initialize MediaFrameReader
var frameSourceInfo = _mediaCapture.FrameSources.Values
    .FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview);

if (frameSourceInfo != null)
{
    var mediaFrameSource = await _mediaCapture.CreateFrameReaderAsync(frameSourceInfo);
    mediaFrameSource.FrameArrived += FrameReader_FrameArrived;
    await mediaFrameSource.StartAsync();
}

// Handle frames
private async void FrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
{
    var frame = sender.TryAcquireLatestFrame();
    if (frame?.VideoMediaFrame != null)
    {
        // Process frame with minimal latency
        var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
        // Update UI on dispatcher thread
    }
}
```

**Advantages:**
- Direct access to video frames
- 30-60 FPS achievable
- Low latency
- Efficient memory usage

### 2. Win2D with MediaCapture (High Performance)
Using Win2D for GPU-accelerated rendering:

```csharp
// Setup Win2D Canvas
var canvasDevice = CanvasDevice.GetSharedDevice();
var canvasSwapChain = new CanvasSwapChain(canvasDevice, width, height, 96);

// In frame handler
using (var canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, softwareBitmap))
using (var drawingSession = canvasSwapChain.CreateDrawingSession(Colors.Black))
{
    drawingSession.DrawImage(canvasBitmap);
}
canvasSwapChain.Present();
```

**Advantages:**
- GPU acceleration
- 60+ FPS possible
- Efficient for effects/overlays
- Lower CPU usage

### 3. MediaPlayerElement with MediaSource
For streaming scenarios:

```csharp
var mediaPlayer = new MediaPlayer();
mediaPlayer.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
mediaPlayer.RealTimePlayback = true;
mediaPlayer.IsVideoFrameServerEnabled = true;

MediaPlayerElement.SetMediaPlayer(mediaPlayer);
```

**Advantages:**
- Built-in optimization
- Good for streaming
- Hardware acceleration

### 4. Direct3D11 Interop (Maximum Performance)
For absolute maximum performance:

```csharp
// Use SharpDX or Win32 interop
// Access Direct3D11 surface directly
// Render to SwapChainPanel
```

**Advantages:**
- Maximum performance
- Full control
- 60+ FPS guaranteed
- Complex implementation

## Recommended Approach for SmartBox-Next

### Phase 1: MediaFrameReader Implementation
1. Replace timer-based capture with MediaFrameReader
2. Use SoftwareBitmap for efficient frame handling
3. Update UI using WriteableBitmap or Win2D

### Phase 2: Performance Optimization
1. Implement frame skipping if needed
2. Use concurrent processing for frame conversion
3. Add performance metrics

### Phase 3: Streaming Preparation
1. Add frame encoding pipeline
2. Implement buffer management
3. Prepare for network streaming

## Implementation Strategy

### Step 1: Basic MediaFrameReader
```csharp
private MediaFrameReader _frameReader;
private SoftwareBitmapSource _bitmapSource = new SoftwareBitmapSource();

private async Task InitializeFrameReaderAsync()
{
    var frameSource = _mediaCapture.FrameSources.Values.FirstOrDefault(
        source => source.Info.MediaStreamType == MediaStreamType.VideoPreview);
    
    if (frameSource != null)
    {
        _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
        _frameReader.FrameArrived += ProcessVideoFrame;
        await _frameReader.StartAsync();
    }
}

private async void ProcessVideoFrame(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
{
    using (var frame = sender.TryAcquireLatestFrame())
    {
        if (frame?.VideoMediaFrame?.SoftwareBitmap != null)
        {
            var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
            
            // Convert to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
            }
            
            // Update UI on dispatcher thread
            await DispatcherQueue.TryEnqueue(async () =>
            {
                await _bitmapSource.SetBitmapAsync(softwareBitmap);
                WebcamPreview.Source = _bitmapSource;
            });
        }
    }
}
```

### Step 2: Performance Monitoring
```csharp
private int _frameCount = 0;
private DateTime _lastFpsUpdate = DateTime.Now;

private void UpdateFpsCounter()
{
    _frameCount++;
    var now = DateTime.Now;
    var elapsed = (now - _lastFpsUpdate).TotalSeconds;
    
    if (elapsed >= 1.0)
    {
        var fps = _frameCount / elapsed;
        AddDebugMessage($"FPS: {fps:F1}");
        _frameCount = 0;
        _lastFpsUpdate = now;
    }
}
```

## Video Streaming Considerations

### For Future Implementation:
1. **WebRTC Integration**
   - Use Microsoft.MixedReality.WebRTC
   - Real-time peer-to-peer streaming
   - Low latency

2. **RTMP Streaming**
   - Use FFMpegCore for encoding
   - Stream to RTMP servers
   - Good for broadcasting

3. **HLS/DASH Streaming**
   - Segment-based streaming
   - Adaptive bitrate
   - Wide compatibility

4. **Custom Protocol**
   - Direct socket streaming
   - Minimal latency
   - Full control

## Performance Benchmarks

| Method | Expected FPS | Latency | CPU Usage | GPU Usage |
|--------|-------------|---------|-----------|-----------|
| CapturePhotoToStreamAsync | 5-10 | High | High | Low |
| MediaFrameReader | 30-60 | Low | Medium | Low |
| Win2D | 60+ | Very Low | Low | Medium |
| Direct3D11 | 60+ | Minimal | Low | High |

## Conclusion

For SmartBox-Next, implementing MediaFrameReader is the best balance of:
- Performance (30-60 FPS achievable)
- Complexity (reasonable implementation effort)
- Compatibility (works with existing WinUI3)
- Future-proofing (easy to add streaming)

The current timer-based approach is a bottleneck and should be replaced with MediaFrameReader for real-time video preview.