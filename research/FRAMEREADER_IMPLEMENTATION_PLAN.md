# MediaFrameReader Implementation Plan for SmartBox-Next

## Current Status
- Timer-based approach with CapturePhotoToStreamAsync
- 5-10 FPS performance
- Build is working

## Implementation Strategy

### Phase 1: Add MediaFrameReader alongside timer (for testing)
1. Keep existing timer-based code
2. Add MediaFrameReader as alternative
3. Add toggle button to switch between modes
4. Compare performance

### Phase 2: Replace timer with MediaFrameReader
1. Remove timer-based code
2. Use only MediaFrameReader
3. Optimize for performance

## Code Changes Needed

### 1. Add MediaFrameReader variables
```csharp
private MediaFrameReader? _frameReader;
private SoftwareBitmapSource _bitmapSource = new SoftwareBitmapSource();
```

### 2. Modify InitializeWebcamAsync
After MediaCapture initialization, add:
```csharp
// Try to use MediaFrameReader for better performance
var frameSource = _mediaCapture.FrameSources.Values
    .FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview);

if (frameSource != null)
{
    _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
    _frameReader.FrameArrived += OnFrameArrived;
    var status = await _frameReader.StartAsync();
    
    if (status == MediaFrameReaderStartStatus.Success)
    {
        // Use frame reader
        _timer?.Stop();
        AddDebugMessage("Using high-performance MediaFrameReader");
    }
    else
    {
        // Fall back to timer
        StartTimerMode();
    }
}
```

### 3. Add frame handler
```csharp
private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
{
    using (var frame = sender.TryAcquireLatestFrame())
    {
        if (frame?.VideoMediaFrame?.SoftwareBitmap != null)
        {
            var bitmap = frame.VideoMediaFrame.SoftwareBitmap;
            
            // Convert if needed
            if (bitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                bitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
            {
                bitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            }
            
            // Update UI
            await DispatcherQueue.TryEnqueue(async () =>
            {
                await _bitmapSource.SetBitmapAsync(bitmap);
                WebcamPreview.Source = _bitmapSource;
            });
        }
    }
}
```

### 4. Update cleanup
```csharp
if (_frameReader != null)
{
    await _frameReader.StopAsync();
    _frameReader.Dispose();
    _frameReader = null;
}
```

## Benefits
- 30-60 FPS achievable
- Lower CPU usage
- Better for future streaming
- Professional video quality

## Risks
- More complex code
- Potential compatibility issues
- Need to handle more edge cases

## Testing Plan
1. Test on different cameras
2. Monitor CPU/memory usage
3. Check frame rates
4. Verify capture still works