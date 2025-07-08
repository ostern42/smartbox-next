using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Microsoft.UI.Dispatching;
using System.Linq;

namespace SmartBoxNext
{
    /// <summary>
    /// Simple video capture using MediaFrameReader - minimal approach
    /// </summary>
    public sealed partial class SimpleVideoCapture : IDisposable
    {
        public event Action<SoftwareBitmap>? FrameArrived;
        public event Action<string>? DebugMessage;
        
        private MediaCapture? _mediaCapture;
        private MediaFrameReader? _frameReader;
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        
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
                
                // Find best format (prefer 30+ FPS)
                var formats = frameSource.SupportedFormats.OrderByDescending(format =>
                {
                    var fps = format.FrameRate.Numerator / (double)format.FrameRate.Denominator;
                    return fps;
                }).ToList();
                
                MediaFrameFormat? selectedFormat = null;
                foreach (var format in formats)
                {
                    var fps = format.FrameRate.Numerator / (double)format.FrameRate.Denominator;
                    DebugMessage?.Invoke($"Available: {format.Subtype} {format.VideoFormat.Width}x{format.VideoFormat.Height} @ {fps:F1} FPS");
                    
                    if (fps >= 25 && selectedFormat == null)
                    {
                        selectedFormat = format;
                    }
                }
                
                if (selectedFormat != null)
                {
                    await frameSource.SetFormatAsync(selectedFormat);
                    var fps = selectedFormat.FrameRate.Numerator / (double)selectedFormat.FrameRate.Denominator;
                    DebugMessage?.Invoke($"Selected: {selectedFormat.Subtype} @ {fps:F1} FPS");
                }
                
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
                
                // Debug first frame
                if (_frameCount == 1)
                {
                    var firstFrameBitmap = frame.VideoMediaFrame.SoftwareBitmap;
                    DebugMessage?.Invoke($"First frame in SimpleVideoCapture! Format: {firstFrameBitmap.BitmapPixelFormat}, Size: {firstFrameBitmap.PixelWidth}x{firstFrameBitmap.PixelHeight}");
                }
                
                // Update FPS
                var now = DateTime.Now;
                if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
                {
                    var fps = _frameCount / (now - _lastFpsUpdate).TotalSeconds;
                    DebugMessage?.Invoke($"Video FPS: {fps:F1} (Total frames: {_frameCount})");
                    _frameCount = 0;
                    _lastFpsUpdate = now;
                }
                
                // Get the frame
                var sourceBitmap = frame.VideoMediaFrame.SoftwareBitmap;
                
                // Convert if needed
                if (sourceBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                    sourceBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    var convertedBitmap = SoftwareBitmap.Convert(sourceBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                    FrameArrived?.Invoke(convertedBitmap);
                    convertedBitmap.Dispose();
                }
                else
                {
                    // Create a copy since the frame will be disposed
                    var copy = new SoftwareBitmap(sourceBitmap.BitmapPixelFormat, sourceBitmap.PixelWidth, sourceBitmap.PixelHeight, sourceBitmap.BitmapAlphaMode);
                    sourceBitmap.CopyTo(copy);
                    FrameArrived?.Invoke(copy);
                }
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