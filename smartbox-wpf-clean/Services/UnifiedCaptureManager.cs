using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SmartBoxNext.Services;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Unified capture manager for Yuan + WebRTC sources
    /// </summary>
    public class UnifiedCaptureManager : IDisposable
    {
        private readonly ILogger<UnifiedCaptureManager> _logger;
        private readonly SharedMemoryClient _sharedMemoryClient;
        
        // Current frame data
        private BitmapSource? _currentYuanFrame;
        private BitmapSource? _currentWebRTCFrame;
        private DateTime _lastYuanFrameTime = DateTime.MinValue;
        private DateTime _lastWebRTCFrameTime = DateTime.MinValue;
        
        // Configuration
        private CaptureSource _activeSource = CaptureSource.WebRTC;
        private bool _isYuanConnected = false;
        private bool _isWebRTCActive = false;

        // Events
        public event EventHandler<FrameUpdatedEventArgs>? FrameUpdated;
        public event EventHandler<CaptureSource>? ActiveSourceChanged;
        public event EventHandler<bool>? YuanConnectionChanged;

        public CaptureSource ActiveSource => _activeSource;
        public bool IsYuanConnected => _isYuanConnected;
        public bool IsWebRTCActive => _isWebRTCActive;
        public BitmapSource? CurrentFrame => _activeSource == CaptureSource.Yuan ? _currentYuanFrame : _currentWebRTCFrame;

        public UnifiedCaptureManager(ILogger<UnifiedCaptureManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Create logger for SharedMemoryClient  
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var clientLogger = loggerFactory.CreateLogger<SharedMemoryClient>();
            _sharedMemoryClient = new SharedMemoryClient(clientLogger);
            
            // Subscribe to Yuan frame events
            _sharedMemoryClient.FrameReceived += OnYuanFrameReceived;
            _sharedMemoryClient.ConnectionStateChanged += OnYuanConnectionChanged;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Unified Capture Manager...");
            
            try
            {
                // Try to connect to Yuan service
                await ConnectToYuanAsync();
                
                _logger.LogInformation("Unified Capture Manager initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Unified Capture Manager");
                throw;
            }
        }

        public async Task<bool> ConnectToYuanAsync()
        {
            _logger.LogInformation("Attempting to connect to Yuan capture service...");
            
            try
            {
                var connected = await _sharedMemoryClient.ConnectAsync(timeoutMs: 5000);
                if (connected)
                {
                    _logger.LogInformation("Connected to Yuan capture service");
                    
                    // Start capture if not already running
                    await _sharedMemoryClient.StartCaptureAsync();
                    
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to connect to Yuan capture service");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to Yuan service");
                return false;
            }
        }

        public async Task DisconnectFromYuanAsync()
        {
            _logger.LogInformation("Disconnecting from Yuan capture service...");
            
            try
            {
                await _sharedMemoryClient.DisconnectAsync();
                _currentYuanFrame = null;
                
                // Switch to WebRTC if Yuan was active
                if (_activeSource == CaptureSource.Yuan)
                {
                    await SetActiveSourceAsync(CaptureSource.WebRTC);
                }
                
                _logger.LogInformation("Disconnected from Yuan capture service");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from Yuan service");
            }
        }

        public async Task SetActiveSourceAsync(CaptureSource source)
        {
            if (_activeSource == source)
            {
                return;
            }

            _logger.LogInformation("Switching active source from {OldSource} to {NewSource}", _activeSource, source);

            // Validate source availability
            if (source == CaptureSource.Yuan && !_isYuanConnected)
            {
                throw new InvalidOperationException("Yuan capture service is not connected");
            }

            _activeSource = source;
            ActiveSourceChanged?.Invoke(this, source);

            // Emit current frame for the new source
            var currentFrame = source == CaptureSource.Yuan ? _currentYuanFrame : _currentWebRTCFrame;
            if (currentFrame != null)
            {
                FrameUpdated?.Invoke(this, new FrameUpdatedEventArgs
                {
                    Frame = currentFrame,
                    Source = source,
                    Timestamp = DateTime.UtcNow
                });
            }

            await Task.CompletedTask;
        }

        public async Task<object?> GetYuanInputsAsync()
        {
            if (!_isYuanConnected)
            {
                throw new InvalidOperationException("Yuan capture service is not connected");
            }

            return await _sharedMemoryClient.GetAvailableInputsAsync();
        }

        public async Task<bool> SelectYuanInputAsync(int inputIndex)
        {
            if (!_isYuanConnected)
            {
                throw new InvalidOperationException("Yuan capture service is not connected");
            }

            return await _sharedMemoryClient.SelectInputAsync(inputIndex);
        }

        public async Task<BitmapSource?> CapturePhotoAsync(CaptureSource? sourceOverride = null)
        {
            var source = sourceOverride ?? _activeSource;
            
            _logger.LogInformation("Capturing photo from {Source}", source);

            try
            {
                switch (source)
                {
                    case CaptureSource.Yuan:
                        if (_currentYuanFrame != null)
                        {
                            // For Yuan, we can use the current frame or request a high-res snapshot
                            var snapshotResult = await _sharedMemoryClient.CaptureSnapshotAsync();
                            
                            // For now, return current frame (in production, process snapshotResult)
                            return _currentYuanFrame.Clone();
                        }
                        break;

                    case CaptureSource.WebRTC:
                        if (_currentWebRTCFrame != null)
                        {
                            return _currentWebRTCFrame.Clone();
                        }
                        break;
                }

                _logger.LogWarning("No frame available for capture from {Source}", source);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo from {Source}", source);
                return null;
            }
        }

        public void UpdateWebRTCFrame(BitmapSource frame)
        {
            _currentWebRTCFrame = frame;
            _lastWebRTCFrameTime = DateTime.UtcNow;
            _isWebRTCActive = true;

            // If WebRTC is the active source, emit the frame
            if (_activeSource == CaptureSource.WebRTC)
            {
                FrameUpdated?.Invoke(this, new FrameUpdatedEventArgs
                {
                    Frame = frame,
                    Source = CaptureSource.WebRTC,
                    Timestamp = _lastWebRTCFrameTime
                });
            }
        }

        public void SetWebRTCActive(bool active)
        {
            if (_isWebRTCActive != active)
            {
                _isWebRTCActive = active;
                _logger.LogInformation("WebRTC active state changed: {Active}", active);

                if (!active)
                {
                    _currentWebRTCFrame = null;
                    
                    // Switch to Yuan if WebRTC becomes inactive and Yuan is available
                    if (_activeSource == CaptureSource.WebRTC && _isYuanConnected)
                    {
                        _ = SetActiveSourceAsync(CaptureSource.Yuan);
                    }
                }
            }
        }

        private void OnYuanFrameReceived(object? sender, FrameReceivedEventArgs e)
        {
            try
            {
                // Convert YUY2 frame to BitmapSource
                var bitmapSource = ConvertYUY2ToBitmapSource(e.FrameData, e.Header.Width, e.Header.Height);
                
                _currentYuanFrame = bitmapSource;
                _lastYuanFrameTime = e.ReceivedAt;

                // If Yuan is the active source, emit the frame
                if (_activeSource == CaptureSource.Yuan)
                {
                    FrameUpdated?.Invoke(this, new FrameUpdatedEventArgs
                    {
                        Frame = bitmapSource,
                        Source = CaptureSource.Yuan,
                        Timestamp = _lastYuanFrameTime
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Yuan frame");
            }
        }

        private void OnYuanConnectionChanged(object? sender, bool connected)
        {
            _isYuanConnected = connected;
            YuanConnectionChanged?.Invoke(this, connected);

            _logger.LogInformation("Yuan connection state changed: {Connected}", connected);

            if (!connected)
            {
                _currentYuanFrame = null;
                
                // Switch to WebRTC if Yuan was active
                if (_activeSource == CaptureSource.Yuan && _isWebRTCActive)
                {
                    _ = SetActiveSourceAsync(CaptureSource.WebRTC);
                }
            }
        }

        private BitmapSource ConvertYUY2ToBitmapSource(byte[] yuy2Data, int width, int height)
        {
            try
            {
                // Convert YUY2 to BGRA32 for WPF
                var bgraData = YUY2Converter.ConvertToBGRA32(yuy2Data, width, height);
                
                // Create BitmapSource
                var bitmap = BitmapSource.Create(
                    pixelWidth: width,
                    pixelHeight: height,
                    dpiX: 96,
                    dpiY: 96,
                    pixelFormat: PixelFormats.Bgra32,
                    palette: null,
                    pixels: bgraData,
                    stride: width * 4);

                // Freeze for cross-thread access
                bitmap.Freeze();
                
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting YUY2 frame to BitmapSource");
                throw;
            }
        }

        public async Task<object?> GetYuanStatisticsAsync()
        {
            if (!_isYuanConnected)
            {
                return null;
            }

            try
            {
                return await _sharedMemoryClient.GetServiceStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Yuan statistics");
                return null;
            }
        }

        public CaptureStatistics GetStatistics()
        {
            return new CaptureStatistics
            {
                ActiveSource = _activeSource,
                IsYuanConnected = _isYuanConnected,
                IsWebRTCActive = _isWebRTCActive,
                LastYuanFrameTime = _lastYuanFrameTime,
                LastWebRTCFrameTime = _lastWebRTCFrameTime,
                YuanFramesReceived = _sharedMemoryClient.FramesReceived,
                YuanFramesDropped = _sharedMemoryClient.FramesDropped
            };
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing Unified Capture Manager...");
            
            try
            {
                _sharedMemoryClient?.Dispose();
                _currentYuanFrame = null;
                _currentWebRTCFrame = null;
                
                _logger.LogInformation("Unified Capture Manager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Unified Capture Manager");
            }
        }
    }

    /// <summary>
    /// Capture source enumeration
    /// </summary>
    public enum CaptureSource
    {
        WebRTC,
        Yuan
    }

    /// <summary>
    /// Frame updated event arguments
    /// </summary>
    public class FrameUpdatedEventArgs : EventArgs
    {
        public BitmapSource Frame { get; set; } = null!;
        public CaptureSource Source { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Capture statistics
    /// </summary>
    public class CaptureStatistics
    {
        public CaptureSource ActiveSource { get; set; }
        public bool IsYuanConnected { get; set; }
        public bool IsWebRTCActive { get; set; }
        public DateTime LastYuanFrameTime { get; set; }
        public DateTime LastWebRTCFrameTime { get; set; }
        public long YuanFramesReceived { get; set; }
        public long YuanFramesDropped { get; set; }
    }
}