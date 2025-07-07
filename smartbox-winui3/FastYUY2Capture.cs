using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Dispatching;

namespace SmartBoxNext
{
    /// <summary>
    /// Optimized YUY2 capture using MediaFrameReader
    /// Achieves 30 FPS with YUY2 cameras
    /// </summary>
    public sealed class FastYUY2Capture : IDisposable
    {
        public event Action<SoftwareBitmap>? FrameArrived;
        public event Action<string>? DebugMessage;
        
        private MediaCapture? _mediaCapture;
        private MediaFrameReader? _frameReader;
        private readonly DispatcherQueue _dispatcherQueue;
        private int _frameCount;
        private DateTime _lastFpsUpdate = DateTime.Now;
        
        public FastYUY2Capture(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }
        
        public async Task<bool> InitializeAsync(MediaCapture mediaCapture)
        {
            try
            {
                _mediaCapture = mediaCapture;
                
                // Find the best YUY2 source
                MediaFrameSource? bestSource = null;
                foreach (var source in _mediaCapture.FrameSources.Values)
                {
                    DebugMessage?.Invoke($"Checking source: {source.Info.Id}, Kind: {source.Info.SourceKind}");
                    
                    if (source.Info.SourceKind == MediaFrameSourceKind.Color)
                    {
                        // Check if it supports YUY2
                        var formats = source.SupportedFormats;
                        foreach (var format in formats)
                        {
                            if (format.VideoFormat.Subtype == "YUY2")
                            {
                                DebugMessage?.Invoke($"Found YUY2 format: {format.VideoFormat.Width}x{format.VideoFormat.Height} @ {format.FrameRate.Numerator}/{format.FrameRate.Denominator} FPS");
                                bestSource = source;
                                
                                // Set this format
                                await source.SetFormatAsync(format);
                                break;
                            }
                        }
                    }
                }
                
                if (bestSource == null)
                {
                    DebugMessage?.Invoke("No YUY2 source found!");
                    return false;
                }
                
                // Create frame reader
                _frameReader = await _mediaCapture.CreateFrameReaderAsync(bestSource);
                _frameReader.FrameArrived += OnFrameArrived;
                
                // Start reader
                var status = await _frameReader.StartAsync();
                DebugMessage?.Invoke($"FrameReader start status: {status}");
                
                return status == MediaFrameReaderStartStatus.Success;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Initialize error: {ex.Message}");
                return false;
            }
        }
        
        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var frame = sender.TryAcquireLatestFrame();
            if (frame?.VideoMediaFrame?.SoftwareBitmap == null) return;
            
            _frameCount++;
            
            // Update FPS every second
            var now = DateTime.Now;
            if ((now - _lastFpsUpdate).TotalSeconds >= 1.0)
            {
                var fps = _frameCount / (now - _lastFpsUpdate).TotalSeconds;
                DebugMessage?.Invoke($"YUY2 Capture FPS: {fps:F1}");
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
            
            // Get the frame
            var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
            
            // Convert YUY2 to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                // This conversion is optimized in Windows
                var convertedBitmap = SoftwareBitmap.Convert(
                    softwareBitmap, 
                    BitmapPixelFormat.Bgra8, 
                    BitmapAlphaMode.Premultiplied);
                    
                FrameArrived?.Invoke(convertedBitmap);
                convertedBitmap.Dispose();
            }
            else
            {
                FrameArrived?.Invoke(softwareBitmap);
            }
            
            frame.Dispose();
        }
        
        public void Dispose()
        {
            _frameReader?.Dispose();
            _frameReader = null;
        }
    }
}