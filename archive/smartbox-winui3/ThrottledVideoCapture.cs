using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Graphics.Imaging;
using Microsoft.UI.Dispatching;
using System.Linq;

namespace SmartBoxNext
{
    /// <summary>
    /// Throttled video capture - updates UI at reasonable rate
    /// </summary>
    public sealed partial class ThrottledVideoCapture : IDisposable
    {
        public event Action<SoftwareBitmap>? FrameArrived;
        public event Action<string>? DebugMessage;
        
        private MediaCapture? _mediaCapture;
        private MediaFrameReader? _frameReader;
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private DateTime _lastFrameUpdate = DateTime.Now;
        private const int TARGET_UI_FPS = 15; // Update UI at 15 FPS max
        
        public async Task<bool> InitializeAsync(MediaCapture mediaCapture)
        {
            try
            {
                _mediaCapture = mediaCapture;
                
                // Find the first color video source
                var frameSource = _mediaCapture.FrameSources.Values
                    .FirstOrDefault(source => source.Info.SourceKind == MediaFrameSourceKind.Color);
                
                if (frameSource == null)
                {
                    DebugMessage?.Invoke("No color video source found!");
                    return false;
                }
                
                DebugMessage?.Invoke($"Using source: {frameSource.Info.Id}");
                
                // Create frame reader
                _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
                _frameReader.FrameArrived += OnFrameArrived;
                
                return true;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Initialize error: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> StartAsync()
        {
            if (_frameReader == null) return false;
            
            var status = await _frameReader.StartAsync();
            DebugMessage?.Invoke($"Start status: {status}");
            return status == MediaFrameReaderStartStatus.Success;
        }
        
        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame?.VideoMediaFrame?.SoftwareBitmap == null) return;
                
                _frameCount++;
                
                // Update FPS counter
                var now = DateTime.Now;
                if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
                {
                    var fps = _frameCount / (now - _lastFpsUpdate).TotalSeconds;
                    DebugMessage?.Invoke($"Throttled Video FPS: {fps:F1}");
                    _frameCount = 0;
                    _lastFpsUpdate = now;
                }
                
                // Throttle UI updates
                var timeSinceLastUpdate = (now - _lastFrameUpdate).TotalMilliseconds;
                if (timeSinceLastUpdate < (1000.0 / TARGET_UI_FPS))
                {
                    return; // Skip this frame
                }
                _lastFrameUpdate = now;
                
                // Get the frame
                var bitmap = frame.VideoMediaFrame.SoftwareBitmap;
                
                // Always convert to ensure compatibility
                var convertedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                
                // Create a copy for the event
                var copy = new SoftwareBitmap(convertedBitmap.BitmapPixelFormat, convertedBitmap.PixelWidth, convertedBitmap.PixelHeight, convertedBitmap.BitmapAlphaMode);
                convertedBitmap.CopyTo(copy);
                convertedBitmap.Dispose();
                
                FrameArrived?.Invoke(copy);
            }
        }
        
        public async Task StopAsync()
        {
            if (_frameReader != null)
            {
                await _frameReader.StopAsync();
            }
        }
        
        public void Dispose()
        {
            _frameReader?.Dispose();
            _frameReader = null;
        }
    }
}