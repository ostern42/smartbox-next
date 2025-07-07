using System;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Dispatching;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Capture.Frames;
using Windows.Media;
using System.Threading;
using System.Collections.Concurrent;

namespace SmartBoxNext
{
    /// <summary>
    /// Enterprise-grade high-performance video capture
    /// Uses optimized MediaCapture with proper threading and buffering
    /// Target: 30-60 FPS for medical imaging
    /// </summary>
    public sealed class HighPerformanceCapture : IDisposable
    {
        // Events
        public event Action<SoftwareBitmap>? FrameArrived;
        public event Action<string>? DebugMessage;
        public event Action<double>? FpsUpdated;

        // Core objects
        private MediaCapture? _mediaCapture;
        private LowLagPhotoCapture? _lowLagCapture;
        private VideoEncodingProperties? _videoProperties;
        private SoftwareBitmapSource _bitmapSource = new SoftwareBitmapSource();
        
        // Threading
        private readonly DispatcherQueue _dispatcherQueue;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _captureTask;
        private readonly SemaphoreSlim _captureSemaphore = new SemaphoreSlim(1, 1);
        
        // Frame buffering
        private readonly ConcurrentQueue<SoftwareBitmap> _frameQueue = new ConcurrentQueue<SoftwareBitmap>();
        private const int MAX_FRAME_QUEUE = 3;
        
        // Performance tracking
        private int _frameCount;
        private int _droppedFrames;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private double _currentFps;
        private readonly object _fpsLock = new object();
        
        // State
        private bool _isInitialized;
        private bool _isCapturing;
        
        // Configuration
        private readonly int _targetFps;
        private readonly int _captureDelayMs;

        public double CurrentFps => _currentFps;
        public bool IsCapturing => _isCapturing;
        public bool IsInitialized => _isInitialized;
        public int DroppedFrames => _droppedFrames;

        public HighPerformanceCapture(DispatcherQueue dispatcherQueue, int targetFps = 30)
        {
            _dispatcherQueue = dispatcherQueue;
            _targetFps = targetFps;
            _captureDelayMs = Math.Max(1, 1000 / targetFps);
            
            DebugMessage?.Invoke($"High-performance capture initialized. Target: {_targetFps} FPS");
        }

        public async Task<bool> InitializeAsync(MediaCapture mediaCapture)
        {
            if (_isInitialized) return true;

            try
            {
                _mediaCapture = mediaCapture;
                
                // Get video properties for optimal capture
                var videoProperties = _mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
                
                VideoEncodingProperties? bestProperties = null;
                int bestScore = 0;
                
                foreach (VideoEncodingProperties props in videoProperties)
                {
                    if (props.Subtype == "YUY2" || props.Subtype == "NV12" || props.Subtype == "RGB24")
                    {
                        var fps = props.FrameRate.Numerator / (double)props.FrameRate.Denominator;
                        var score = (int)(fps * 100) + (props.Width > 1280 ? 1000 : 0);
                        
                        DebugMessage?.Invoke($"Format: {props.Subtype} {props.Width}x{props.Height} @ {fps:F1} FPS (Score: {score})");
                        
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestProperties = props;
                        }
                    }
                }
                
                if (bestProperties != null)
                {
                    await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, bestProperties);
                    _videoProperties = bestProperties;
                    
                    var fps = bestProperties.FrameRate.Numerator / (double)bestProperties.FrameRate.Denominator;
                    DebugMessage?.Invoke($"Selected format: {bestProperties.Subtype} {bestProperties.Width}x{bestProperties.Height} @ {fps:F1} FPS");
                }
                
                // Try to initialize low-lag capture for better performance
                try
                {
                    // Try to use the native format first
                    var lowLagFormat = ImageEncodingProperties.CreateUncompressed(MediaPixelFormat.Nv12);
                    if (bestProperties != null)
                    {
                        lowLagFormat.Width = bestProperties.Width;
                        lowLagFormat.Height = bestProperties.Height;
                    }
                    
                    _lowLagCapture = await _mediaCapture.PrepareLowLagPhotoCaptureAsync(lowLagFormat);
                    DebugMessage?.Invoke("Low-lag capture initialized for optimal performance");
                }
                catch (Exception ex)
                {
                    DebugMessage?.Invoke($"Low-lag capture not available: {ex.Message}");
                    // Continue without low-lag capture
                }
                
                _isInitialized = true;
                DebugMessage?.Invoke("High-performance capture ready!");
                return true;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Initialization error: {ex.Message}");
                return false;
            }
        }

        public async Task StartCaptureAsync()
        {
            if (!_isInitialized || _isCapturing) return;

            _isCapturing = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _frameCount = 0;
            _droppedFrames = 0;
            _lastFpsUpdate = DateTime.Now;

            DebugMessage?.Invoke($"Starting high-performance capture at {_targetFps} FPS");

            // Start capture loop on background thread
            _captureTask = Task.Run(() => CaptureLoopAsync(_cancellationTokenSource.Token));
            
            // Start frame processing on UI thread
            _ = Task.Run(() => ProcessFramesAsync(_cancellationTokenSource.Token));
        }

        private async Task CaptureLoopAsync(CancellationToken cancellationToken)
        {
            var frameTimer = new System.Diagnostics.Stopwatch();
            
            while (!cancellationToken.IsCancellationRequested && _isCapturing)
            {
                frameTimer.Restart();
                
                try
                {
                    await _captureSemaphore.WaitAsync(cancellationToken);
                    
                    try
                    {
                        SoftwareBitmap? frame = null;
                        
                        if (_lowLagCapture != null)
                        {
                            // Use low-lag capture for best performance
                            var photo = await _lowLagCapture.CaptureAsync();
                            frame = photo.Frame.SoftwareBitmap;
                        }
                        else if (_mediaCapture != null)
                        {
                            // Fallback to regular capture with JPEG (most compatible)
                            var stream = new InMemoryRandomAccessStream();
                            await _mediaCapture.CapturePhotoToStreamAsync(
                                ImageEncodingProperties.CreateJpeg(), 
                                stream);
                            
                            stream.Seek(0);
                            var decoder = await BitmapDecoder.CreateAsync(stream);
                            frame = await decoder.GetSoftwareBitmapAsync();
                        }
                        
                        if (frame != null)
                        {
                            // Add to queue if not full
                            if (_frameQueue.Count < MAX_FRAME_QUEUE)
                            {
                                _frameQueue.Enqueue(frame);
                            }
                            else
                            {
                                // Drop frame if queue is full
                                frame.Dispose();
                                Interlocked.Increment(ref _droppedFrames);
                            }
                        }
                    }
                    finally
                    {
                        _captureSemaphore.Release();
                    }
                    
                    // Calculate delay to maintain target FPS
                    var elapsedMs = frameTimer.ElapsedMilliseconds;
                    var delayMs = Math.Max(0, _captureDelayMs - (int)elapsedMs);
                    
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugMessage?.Invoke($"Capture error: {ex.Message}");
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private async Task ProcessFramesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isCapturing)
            {
                try
                {
                    if (_frameQueue.TryDequeue(out var frame))
                    {
                        // Raise event on UI thread
                        _dispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                FrameArrived?.Invoke(frame);
                                UpdateFps();
                            }
                            finally
                            {
                                frame.Dispose();
                            }
                        });
                    }
                    else
                    {
                        // No frames available, wait a bit
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugMessage?.Invoke($"Frame processing error: {ex.Message}");
                }
            }
        }

        private void UpdateFps()
        {
            lock (_fpsLock)
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
                    DebugMessage?.Invoke($"FPS: {_currentFps:F1} (Target: {_targetFps}, Dropped: {_droppedFrames})");
                }
            }
        }

        public async Task StopCaptureAsync()
        {
            if (!_isCapturing) return;

            _isCapturing = false;
            _cancellationTokenSource?.Cancel();

            // Wait for capture task to complete
            if (_captureTask != null)
            {
                try
                {
                    await _captureTask;
                }
                catch (TaskCanceledException) { }
            }

            // Clear frame queue
            while (_frameQueue.TryDequeue(out var frame))
            {
                frame.Dispose();
            }

            DebugMessage?.Invoke($"Capture stopped. Total dropped frames: {_droppedFrames}");
        }

        public void Dispose()
        {
            StopCaptureAsync().Wait();
            
            _lowLagCapture?.FinishAsync().AsTask().Wait();
            _lowLagCapture = null;
            
            _captureSemaphore?.Dispose();

        }
    }
}