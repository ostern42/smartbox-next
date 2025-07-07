using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using Windows.Media;
using System.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace SmartBoxNext
{
    /// <summary>
    /// Professional video streaming capture using MediaFrameReader
    /// Supports simultaneous preview, recording, and frame capture
    /// </summary>
    public sealed partial class VideoStreamCapture : IDisposable
    {
        // Events
        public event Action<SoftwareBitmap>? FrameArrived;
        public event Action<string>? DebugMessage;
        public event Action<double>? FpsUpdated;
        
        // Core objects
        private MediaCapture? _mediaCapture;
        private MediaFrameReader? _frameReader;
        private MediaFrameSource? _frameSource;
        private readonly DispatcherQueue _dispatcherQueue;
        
        // Performance tracking
        private int _frameCount;
        private int _droppedFrames;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private double _currentFps;
        
        // State
        private bool _isInitialized;
        private bool _isStreaming;
        
        // Frame capture
        private TaskCompletionSource<SoftwareBitmap>? _frameCaptureTask;
        private readonly SemaphoreSlim _captureSemaphore = new SemaphoreSlim(1, 1);
        
        public double CurrentFps => _currentFps;
        public bool IsStreaming => _isStreaming;
        public bool IsInitialized => _isInitialized;
        
        public VideoStreamCapture(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue;
        }
        
        public async Task<bool> InitializeAsync(MediaCapture mediaCapture)
        {
            if (_isInitialized) return true;
            
            try
            {
                _mediaCapture = mediaCapture;
                
                // Find the best video source
                MediaFrameSource? bestSource = null;
                MediaFrameFormat? bestFormat = null;
                double bestScore = 0;
                
                foreach (var source in _mediaCapture.FrameSources.Values)
                {
                    if (source.Info.SourceKind != MediaFrameSourceKind.Color)
                        continue;
                    
                    DebugMessage?.Invoke($"Evaluating source: {source.Info.Id}");
                    
                    foreach (var format in source.SupportedFormats)
                    {
                        var frameRate = format.FrameRate.Numerator / (double)format.FrameRate.Denominator;
                        var pixels = format.VideoFormat.Width * format.VideoFormat.Height;
                        
                        // Prefer higher FPS and resolution
                        // Prefer YUY2/NV12 for hardware acceleration, but accept RGB too
                        var formatScore = 1.0;
                        var subtype = format.Subtype;
                        if (subtype == "YUY2" || subtype == "NV12")
                            formatScore = 1.5; // Prefer hardware-accelerated formats
                        else if (subtype == "RGB24" || subtype == "RGB32")
                            formatScore = 1.2; // RGB is okay too
                        else if (subtype == "MJPG")
                            formatScore = 1.1; // MJPEG is compressed but usable
                        
                        var score = frameRate * formatScore + (pixels / 1000000.0); // FPS is most important
                        
                        DebugMessage?.Invoke($"  Format: {subtype} {format.VideoFormat.Width}x{format.VideoFormat.Height} @ {frameRate:F1} FPS (Score: {score:F2})");
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestSource = source;
                            bestFormat = format;
                        }
                    }
                }
                
                if (bestSource == null || bestFormat == null)
                {
                    DebugMessage?.Invoke("No suitable video source found!");
                    return false;
                }
                
                // Set the format
                await bestSource.SetFormatAsync(bestFormat);
                _frameSource = bestSource;
                
                var selectedFps = bestFormat.FrameRate.Numerator / (double)bestFormat.FrameRate.Denominator;
                DebugMessage?.Invoke($"Selected: {bestFormat.Subtype} {bestFormat.VideoFormat.Width}x{bestFormat.VideoFormat.Height} @ {selectedFps:F1} FPS");
                
                // Create frame reader
                _frameReader = await _mediaCapture.CreateFrameReaderAsync(bestSource);
                
                // Configure for real-time streaming
                _frameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
                
                _frameReader.FrameArrived += OnFrameArrived;
                
                _isInitialized = true;
                DebugMessage?.Invoke("Video stream capture initialized!");
                return true;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Initialization error: {ex.Message}");
                return false;
            }
        }
        
        public async Task<bool> StartStreamingAsync()
        {
            if (!_isInitialized || _isStreaming) return _isStreaming;
            
            try
            {
                var status = await _frameReader!.StartAsync();
                _isStreaming = status == MediaFrameReaderStartStatus.Success;
                
                if (_isStreaming)
                {
                    _frameCount = 0;
                    _droppedFrames = 0;
                    _lastFpsUpdate = DateTime.Now;
                    DebugMessage?.Invoke("Video streaming started!");
                }
                else
                {
                    DebugMessage?.Invoke($"Failed to start streaming: {status}");
                }
                
                return _isStreaming;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Start streaming error: {ex.Message}");
                return false;
            }
        }
        
        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame?.VideoMediaFrame?.SoftwareBitmap == null)
                {
                    Interlocked.Increment(ref _droppedFrames);
                    return;
                }
                
                var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
                
                // Convert to BGRA8 if needed for display
                SoftwareBitmap? processedBitmap = null;
                bool needsDispose = false;
                
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                    softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    processedBitmap = SoftwareBitmap.Convert(
                        softwareBitmap, 
                        BitmapPixelFormat.Bgra8, 
                        BitmapAlphaMode.Premultiplied);
                    needsDispose = true;
                }
                else
                {
                    processedBitmap = softwareBitmap;
                }
                
                // Check if someone is waiting for a frame capture
                if (_frameCaptureTask != null && !_frameCaptureTask.Task.IsCompleted)
                {
                    // Create a copy for the capture
                    var captureCopy = new SoftwareBitmap(
                        processedBitmap.BitmapPixelFormat,
                        processedBitmap.PixelWidth,
                        processedBitmap.PixelHeight,
                        processedBitmap.BitmapAlphaMode);
                    
                    processedBitmap.CopyTo(captureCopy);
                    _frameCaptureTask.TrySetResult(captureCopy);
                }
                
                // Raise frame event on dispatcher queue
                _dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        // Create a copy for the event to avoid disposal issues
                        var eventCopy = new SoftwareBitmap(
                            processedBitmap.BitmapPixelFormat,
                            processedBitmap.PixelWidth,
                            processedBitmap.PixelHeight,
                            processedBitmap.BitmapAlphaMode);
                        
                        processedBitmap.CopyTo(eventCopy);
                        FrameArrived?.Invoke(eventCopy);
                        
                        UpdateFps();
                    }
                    finally
                    {
                        if (needsDispose)
                        {
                            processedBitmap?.Dispose();
                        }
                    }
                });
            }
        }
        
        private void UpdateFps()
        {
            _frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - _lastFpsUpdate).TotalSeconds;
            
            if (elapsed >= 1.0)
            {
                _currentFps = _frameCount / elapsed;
                _frameCount = 0;
                _lastFpsUpdate = now;
                
                FpsUpdated?.Invoke(_currentFps);
                DebugMessage?.Invoke($"Stream FPS: {_currentFps:F1} (Dropped: {_droppedFrames})");
            }
        }
        
        /// <summary>
        /// Capture a single frame while streaming continues
        /// </summary>
        public async Task<SoftwareBitmap?> CaptureFrameAsync()
        {
            if (!_isStreaming) return null;
            
            await _captureSemaphore.WaitAsync();
            try
            {
                _frameCaptureTask = new TaskCompletionSource<SoftwareBitmap>();
                
                // Wait up to 1 second for a frame
                var captureTask = _frameCaptureTask.Task;
                var timeoutTask = Task.Delay(1000);
                
                var completedTask = await Task.WhenAny(captureTask, timeoutTask);
                
                if (completedTask == captureTask)
                {
                    return await captureTask;
                }
                else
                {
                    DebugMessage?.Invoke("Frame capture timeout");
                    return null;
                }
            }
            finally
            {
                _frameCaptureTask = null;
                _captureSemaphore.Release();
            }
        }
        
        /// <summary>
        /// Capture a frame as JPEG bytes for saving/sending
        /// </summary>
        public async Task<byte[]?> CaptureFrameAsJpegAsync(int quality = 90)
        {
            var bitmap = await CaptureFrameAsync();
            if (bitmap == null) return null;
            
            try
            {
                using (var stream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                    encoder.SetSoftwareBitmap(bitmap);
                    encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                    encoder.IsThumbnailGenerated = false;
                    
                    // Set JPEG quality
                    var propertySet = new BitmapPropertySet();
                    var qualityValue = new BitmapTypedValue((double)quality / 100.0, Windows.Foundation.PropertyType.Single);
                    propertySet.Add("ImageQuality", qualityValue);
                    
                    await encoder.BitmapProperties.SetPropertiesAsync(propertySet);
                    await encoder.FlushAsync();
                    
                    // Read bytes
                    var bytes = new byte[stream.Size];
                    stream.Seek(0);
                    using (var reader = new Windows.Storage.Streams.DataReader(stream))
                    {
                        await reader.LoadAsync((uint)stream.Size);
                        reader.ReadBytes(bytes);
                    }
                    
                    return bytes;
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }
        
        public async Task StopStreamingAsync()
        {
            if (!_isStreaming) return;
            
            _isStreaming = false;
            
            if (_frameReader != null)
            {
                await _frameReader.StopAsync();
            }
            
            DebugMessage?.Invoke($"Streaming stopped. Total dropped frames: {_droppedFrames}");
        }
        
        public void Dispose()
        {
            StopStreamingAsync().Wait();
            
            _frameReader?.Dispose();
            _frameReader = null;
            
            _captureSemaphore?.Dispose();
        }
    }
}