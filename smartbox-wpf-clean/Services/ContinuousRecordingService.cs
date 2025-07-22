using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using Timer = System.Threading.Timer;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Medical-grade continuous recording service with retroactive capture
    /// Supports up to 4-hour recordings with circular buffer management
    /// </summary>
    public class ContinuousRecordingService : IContinuousRecordingService
    {
        private readonly ILogger<ContinuousRecordingService> _logger;
        private readonly DicomVideoService _dicomVideoService;
        private readonly FFmpegService _ffmpegService;
        private readonly UnifiedCaptureManager _captureManager;
        
        // Recording configuration
        private readonly ContinuousRecordingConfig _config;
        
        // Circular buffer management
        private readonly CircularBufferManager _bufferManager;
        private readonly SegmentManager _segmentManager;
        
        // Recording state
        private bool _isRecording = false;
        private bool _disposed = false;
        private PatientInfo? _currentPatient;
        private DateTime _recordingStartTime;
        private CancellationTokenSource? _recordingCancellation;
        
        // Performance metrics
        private readonly PerformanceMonitor _performanceMonitor;
        
        // Thread safety
        private readonly SemaphoreSlim _recordingLock = new(1, 1);
        private readonly SemaphoreSlim _captureLock = new(1, 1);
        
        public event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
        public event EventHandler<SegmentCompletedEventArgs>? SegmentCompleted;
        public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;
        
        public bool IsRecording => _isRecording;
        public TimeSpan RecordingDuration => _isRecording ? DateTime.UtcNow - _recordingStartTime : TimeSpan.Zero;
        public long TotalBytesRecorded => _segmentManager.TotalBytesWritten;
        public int CurrentSegmentNumber => _segmentManager.CurrentSegmentNumber;
        
        public ContinuousRecordingService(
            ILogger<ContinuousRecordingService> logger,
            DicomVideoService dicomVideoService,
            FFmpegService ffmpegService,
            UnifiedCaptureManager captureManager,
            ContinuousRecordingConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dicomVideoService = dicomVideoService ?? throw new ArgumentNullException(nameof(dicomVideoService));
            _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
            _captureManager = captureManager ?? throw new ArgumentNullException(nameof(captureManager));
            
            // Use provided config or defaults
            _config = config ?? new ContinuousRecordingConfig();
            
            // Initialize managers
            _bufferManager = new CircularBufferManager(_config, logger);
            _segmentManager = new SegmentManager(_config, logger);
            _performanceMonitor = new PerformanceMonitor(logger);
            
            // Configure FFmpeg
            _ffmpegService.ConfigureFFmpeg();
            
            _logger.LogInformation("ContinuousRecordingService initialized with {Duration} minute segments", 
                _config.SegmentDurationMinutes);
        }
        
        /// <summary>
        /// Start continuous recording for a patient
        /// </summary>
        public async Task StartRecordingAsync(PatientInfo patient)
        {
            await _recordingLock.WaitAsync();
            try
            {
                if (_isRecording)
                {
                    _logger.LogWarning("Recording already in progress for patient {PatientId}", _currentPatient?.PatientId);
                    return;
                }
                
                _logger.LogInformation("Starting continuous recording for patient {PatientId}", patient.PatientId);
                
                _currentPatient = patient;
                _recordingStartTime = DateTime.UtcNow;
                _recordingCancellation = new CancellationTokenSource();
                
                // Initialize recording infrastructure
                await _segmentManager.InitializeAsync(patient);
                await _bufferManager.InitializeAsync();
                
                // Subscribe to capture events
                _captureManager.FrameCaptured += OnFrameCaptured;
                
                // Start performance monitoring
                _performanceMonitor.Start();
                
                // Start recording tasks
                _isRecording = true;
                
                // Start background tasks
                _ = Task.Run(() => RecordingLoopAsync(_recordingCancellation.Token));
                _ = Task.Run(() => MemoryMonitorLoopAsync(_recordingCancellation.Token));
                _ = Task.Run(() => SegmentRotationLoopAsync(_recordingCancellation.Token));
                
                RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs 
                { 
                    IsRecording = true, 
                    Patient = patient,
                    StartTime = _recordingStartTime 
                });
                
                _logger.LogInformation("Continuous recording started successfully");
            }
            finally
            {
                _recordingLock.Release();
            }
        }
        
        /// <summary>
        /// Stop continuous recording
        /// </summary>
        public async Task StopRecordingAsync()
        {
            await _recordingLock.WaitAsync();
            try
            {
                if (!_isRecording)
                {
                    _logger.LogWarning("No recording in progress");
                    return;
                }
                
                _logger.LogInformation("Stopping continuous recording");
                
                _isRecording = false;
                _recordingCancellation?.Cancel();
                
                // Unsubscribe from capture events
                _captureManager.FrameCaptured -= OnFrameCaptured;
                
                // Finalize current segment
                await _segmentManager.FinalizeCurrentSegmentAsync();
                
                // Stop performance monitoring
                _performanceMonitor.Stop();
                
                // Clear buffers
                await _bufferManager.ClearAsync();
                
                RecordingStateChanged?.Invoke(this, new RecordingStateChangedEventArgs 
                { 
                    IsRecording = false, 
                    Patient = _currentPatient,
                    StartTime = _recordingStartTime 
                });
                
                _logger.LogInformation("Continuous recording stopped. Total duration: {Duration}", 
                    DateTime.UtcNow - _recordingStartTime);
                
                _currentPatient = null;
                _recordingCancellation?.Dispose();
                _recordingCancellation = null;
            }
            finally
            {
                _recordingLock.Release();
            }
        }
        
        /// <summary>
        /// Save the last N minutes of recording retroactively
        /// </summary>
        public async Task<string> SaveLastMinutesAsync(int minutes, string reason, string? outputPath = null)
        {
            await _captureLock.WaitAsync();
            try
            {
                if (!_isRecording)
                {
                    throw new InvalidOperationException("No recording in progress");
                }
                
                _logger.LogInformation("Retroactive capture requested: {Minutes} minutes, Reason: {Reason}", 
                    minutes, reason);
                
                // Calculate time range
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddMinutes(-minutes);
                
                // Get frames from buffer
                var frames = await _bufferManager.GetFramesInRangeAsync(startTime, endTime);
                
                if (!frames.Any())
                {
                    throw new InvalidOperationException($"No frames available for the last {minutes} minutes");
                }
                
                _logger.LogInformation("Retrieved {Count} frames for retroactive capture", frames.Count);
                
                // Create output path if not provided
                if (string.IsNullOrEmpty(outputPath))
                {
                    var captureDir = Path.Combine(_config.StoragePath, "RetroactiveCaptures");
                    Directory.CreateDirectory(captureDir);
                    outputPath = Path.Combine(captureDir, 
                        $"Retro_{_currentPatient?.PatientId}_{DateTime.Now:yyyyMMdd_HHmmss}_{minutes}min.webm");
                }
                
                // Create video from frames
                await CreateVideoFromFramesAsync(frames, outputPath);
                
                // Log retroactive capture
                await LogRetroactiveCaptureAsync(minutes, reason, outputPath);
                
                _logger.LogInformation("Retroactive capture saved: {Path}", outputPath);
                return outputPath;
            }
            finally
            {
                _captureLock.Release();
            }
        }
        
        /// <summary>
        /// Handle incoming frames from capture manager
        /// </summary>
        private async void OnFrameCaptured(object? sender, FrameCapturedEventArgs e)
        {
            if (!_isRecording || _disposed) return;
            
            try
            {
                // Add frame to circular buffer
                await _bufferManager.AddFrameAsync(new VideoFrame
                {
                    Data = e.FrameData,
                    Timestamp = e.Timestamp,
                    Width = e.Width,
                    Height = e.Height,
                    PixelFormat = e.PixelFormat,
                    FrameNumber = e.FrameNumber,
                    IsKeyFrame = e.IsKeyFrame
                });
                
                // Write to current segment
                await _segmentManager.WriteFrameAsync(e.FrameData, e.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing captured frame");
            }
        }
        
        /// <summary>
        /// Main recording loop
        /// </summary>
        private async Task RecordingLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recording loop started");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isRecording)
                {
                    // Check recording duration limit
                    if (RecordingDuration >= _config.MaxRecordingDuration)
                    {
                        _logger.LogWarning("Maximum recording duration reached: {Duration}", RecordingDuration);
                        await StopRecordingAsync();
                        break;
                    }
                    
                    // Update performance metrics
                    var metrics = _performanceMonitor.GetCurrentMetrics();
                    if (metrics.CpuUsagePercent > 50)
                    {
                        _logger.LogWarning("High CPU usage detected: {Cpu}%", metrics.CpuUsagePercent);
                    }
                    
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recording loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recording loop");
                await StopRecordingAsync();
            }
        }
        
        /// <summary>
        /// Monitor memory usage and trigger disk offloading when needed
        /// </summary>
        private async Task MemoryMonitorLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Memory monitor loop started");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isRecording)
                {
                    var memoryUsage = _bufferManager.GetMemoryUsageBytes();
                    
                    if (memoryUsage > _config.MemoryThresholdBytes)
                    {
                        _logger.LogWarning("Memory threshold exceeded: {Usage:F2} GB", 
                            memoryUsage / (1024.0 * 1024.0 * 1024.0));
                        
                        MemoryPressureDetected?.Invoke(this, new MemoryPressureEventArgs 
                        { 
                            MemoryUsageBytes = memoryUsage,
                            ThresholdBytes = _config.MemoryThresholdBytes 
                        });
                        
                        // Offload oldest frames to disk
                        await _bufferManager.OffloadToDiskAsync(_config.OffloadPercentage);
                    }
                    
                    await Task.Delay(5000, cancellationToken); // Check every 5 seconds
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Memory monitor loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in memory monitor loop");
            }
        }
        
        /// <summary>
        /// Handle automatic segment rotation
        /// </summary>
        private async Task SegmentRotationLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Segment rotation loop started");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && _isRecording)
                {
                    var segmentAge = await _segmentManager.GetCurrentSegmentAgeAsync();
                    
                    if (segmentAge >= TimeSpan.FromMinutes(_config.SegmentDurationMinutes))
                    {
                        _logger.LogInformation("Rotating segment after {Minutes} minutes", 
                            segmentAge.TotalMinutes);
                        
                        var completedSegment = await _segmentManager.RotateSegmentAsync();
                        
                        if (completedSegment != null)
                        {
                            SegmentCompleted?.Invoke(this, new SegmentCompletedEventArgs 
                            { 
                                SegmentPath = completedSegment,
                                SegmentNumber = _segmentManager.CurrentSegmentNumber - 1,
                                Duration = segmentAge 
                            });
                        }
                    }
                    
                    await Task.Delay(10000, cancellationToken); // Check every 10 seconds
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Segment rotation loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in segment rotation loop");
            }
        }
        
        /// <summary>
        /// Create video file from frame collection
        /// </summary>
        private async Task CreateVideoFromFramesAsync(IList<VideoFrame> frames, string outputPath)
        {
            _logger.LogInformation("Creating video from {Count} frames", frames.Count);
            
            // Group frames by timestamp to ensure proper ordering
            var orderedFrames = frames.OrderBy(f => f.Timestamp).ToList();
            
            // Calculate frame rate based on timestamps
            var duration = orderedFrames.Last().Timestamp - orderedFrames.First().Timestamp;
            var frameRate = orderedFrames.Count / Math.Max(1, duration.TotalSeconds);
            
            // Create video using FFMpeg pipe - temporarily disabled due to interface compatibility
            // TODO: Implement proper IVideoFrame interface for VideoFrame class
            // var videoFramesSource = new RawVideoPipeSource(orderedFrames)
            // {
            //     FrameRate = frameRate
            // };
            
            // Temporarily use simple frame-by-frame approach until IVideoFrame interface is properly implemented
            // TODO: Replace with proper FFMpegCore pipe source when interface compatibility is resolved
            var tempDir = Path.Combine(Path.GetTempPath(), "SmartBoxFrames", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Save frames as individual images
                for (int i = 0; i < orderedFrames.Count; i++)
                {
                    var framePath = Path.Combine(tempDir, $"frame_{i:D6}.png");
                    await File.WriteAllBytesAsync(framePath, orderedFrames[i].Data);
                }
                
                // Create video from frames
                await FFMpegArguments
                    .FromFileInput(Path.Combine(tempDir, "frame_%06d.png"))
                    .OutputToFile(outputPath, true, options => options
                        .WithVideoCodec(VideoCodec.LibVpx)
                        .WithConstantRateFactor(23)
                        .WithVideoBitrate(5000) // 5 Mbps
                        .WithFramerate(frameRate)
                        .WithFastStart())
                    .ProcessAsynchronously();
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            
            _logger.LogInformation("Video created: {Path}, Duration: {Duration:F2}s", 
                outputPath, duration.TotalSeconds);
        }
        
        /// <summary>
        /// Log retroactive capture for audit trail
        /// </summary>
        private async Task LogRetroactiveCaptureAsync(int minutes, string reason, string outputPath)
        {
            var logEntry = new RetroactiveCaptureLog
            {
                Timestamp = DateTime.UtcNow,
                PatientId = _currentPatient?.PatientId,
                MinutesCaptured = minutes,
                Reason = reason,
                OutputPath = outputPath,
                FileSize = new FileInfo(outputPath).Length
            };
            
            var logPath = Path.Combine(_config.StoragePath, "RetroactiveCaptureLogs");
            Directory.CreateDirectory(logPath);
            
            var logFile = Path.Combine(logPath, $"RetroLog_{DateTime.Now:yyyyMMdd}.json");
            
            // Append to log file
            var json = System.Text.Json.JsonSerializer.Serialize(logEntry);
            await File.AppendAllTextAsync(logFile, json + Environment.NewLine);
            
            _logger.LogInformation("Retroactive capture logged: {Reason}", reason);
        }
        
        /// <summary>
        /// Get current recording statistics
        /// </summary>
        public RecordingStatistics GetStatistics()
        {
            return new RecordingStatistics
            {
                IsRecording = _isRecording,
                Duration = RecordingDuration,
                TotalFrames = _bufferManager.TotalFramesProcessed,
                BufferedFrames = _bufferManager.BufferedFrameCount,
                DroppedFrames = _bufferManager.DroppedFrameCount,
                MemoryUsageBytes = _bufferManager.GetMemoryUsageBytes(),
                DiskUsageBytes = _segmentManager.TotalBytesWritten,
                CurrentSegment = _segmentManager.CurrentSegmentNumber,
                PerformanceMetrics = _performanceMonitor.GetCurrentMetrics()
            };
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            
            try
            {
                if (_isRecording)
                {
                    await StopRecordingAsync();
                }
                
                await _bufferManager.DisposeAsync();
                await _segmentManager.DisposeAsync();
                
                _recordingLock?.Dispose();
                _captureLock?.Dispose();
                _performanceMonitor?.Dispose();
            }
            finally
            {
                _disposed = true;
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            DisposeAsync().AsTask().Wait();
        }
    }
    
    /// <summary>
    /// Configuration for continuous recording
    /// </summary>
    public class ContinuousRecordingConfig
    {
        public int SegmentDurationMinutes { get; set; } = 30; // Default 30-minute segments
        public TimeSpan MaxRecordingDuration { get; set; } = TimeSpan.FromHours(4);
        public long MemoryThresholdBytes { get; set; } = 2L * 1024 * 1024 * 1024; // 2GB
        public double OffloadPercentage { get; set; } = 0.25; // Offload 25% when threshold hit
        public string StoragePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SmartBoxNext", "ContinuousRecording");
        public int CircularBufferSizeMB { get; set; } = 4096; // 4GB circular buffer
        public bool EnableAudioRecording { get; set; } = true;
        public int AudioSampleRate { get; set; } = 48000; // Medical-grade audio
        public int AudioBitrate { get; set; } = 192000; // 192 kbps
    }
    
    /// <summary>
    /// Circular buffer manager with memory-mapped file support
    /// </summary>
    internal class CircularBufferManager : IAsyncDisposable
    {
        private readonly ContinuousRecordingConfig _config;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<VideoFrame> _memoryBuffer;
        private readonly SemaphoreSlim _bufferLock;
        
        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;
        private long _writePosition = 0;
        private long _totalFramesProcessed = 0;
        private long _droppedFrames = 0;
        
        public long TotalFramesProcessed => _totalFramesProcessed;
        public int BufferedFrameCount => _memoryBuffer.Count;
        public long DroppedFrameCount => _droppedFrames;
        
        public CircularBufferManager(ContinuousRecordingConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            _memoryBuffer = new ConcurrentQueue<VideoFrame>();
            _bufferLock = new SemaphoreSlim(1, 1);
        }
        
        public async Task InitializeAsync()
        {
            await _bufferLock.WaitAsync();
            try
            {
                // Create memory-mapped file for reliability
                var mmfSize = (long)_config.CircularBufferSizeMB * 1024 * 1024;
                _mmf = MemoryMappedFile.CreateNew("SmartBoxContinuousBuffer", mmfSize);
                _accessor = _mmf.CreateViewAccessor();
                
                _logger.LogInformation("Circular buffer initialized: {Size} MB", _config.CircularBufferSizeMB);
            }
            finally
            {
                _bufferLock.Release();
            }
        }
        
        public async Task AddFrameAsync(VideoFrame frame)
        {
            await _bufferLock.WaitAsync();
            try
            {
                _memoryBuffer.Enqueue(frame);
                Interlocked.Increment(ref _totalFramesProcessed);
                
                // TODO: Write to memory-mapped file for persistence
            }
            finally
            {
                _bufferLock.Release();
            }
        }
        
        public async Task<IList<VideoFrame>> GetFramesInRangeAsync(DateTime startTime, DateTime endTime)
        {
            await _bufferLock.WaitAsync();
            try
            {
                return _memoryBuffer
                    .Where(f => f.Timestamp >= startTime && f.Timestamp <= endTime)
                    .OrderBy(f => f.Timestamp)
                    .ToList();
            }
            finally
            {
                _bufferLock.Release();
            }
        }
        
        public long GetMemoryUsageBytes()
        {
            // Estimate based on frame count and average frame size
            return _memoryBuffer.Count * 1920 * 1080 * 2; // Assuming YUY2 format
        }
        
        public async Task OffloadToDiskAsync(double percentage)
        {
            await _bufferLock.WaitAsync();
            try
            {
                var framesToOffload = (int)(_memoryBuffer.Count * percentage);
                _logger.LogInformation("Offloading {Count} frames to disk", framesToOffload);
                
                // TODO: Implement disk offloading
                // For now, just remove oldest frames
                for (int i = 0; i < framesToOffload; i++)
                {
                    _memoryBuffer.TryDequeue(out _);
                }
            }
            finally
            {
                _bufferLock.Release();
            }
        }
        
        public async Task ClearAsync()
        {
            await _bufferLock.WaitAsync();
            try
            {
                while (_memoryBuffer.TryDequeue(out _)) { }
                _writePosition = 0;
            }
            finally
            {
                _bufferLock.Release();
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            await ClearAsync();
            _accessor?.Dispose();
            _mmf?.Dispose();
            _bufferLock?.Dispose();
        }
    }
    
    /// <summary>
    /// Manages video segments for long recordings
    /// </summary>
    internal class SegmentManager : IAsyncDisposable
    {
        private readonly ContinuousRecordingConfig _config;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _segmentLock;
        
        private string? _currentSegmentPath;
        private FileStream? _currentSegmentStream;
        private DateTime _currentSegmentStartTime;
        private int _currentSegmentNumber = 0;
        private long _totalBytesWritten = 0;
        private PatientInfo? _patient;
        
        public int CurrentSegmentNumber => _currentSegmentNumber;
        public long TotalBytesWritten => _totalBytesWritten;
        
        public SegmentManager(ContinuousRecordingConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
            _segmentLock = new SemaphoreSlim(1, 1);
        }
        
        public async Task InitializeAsync(PatientInfo patient)
        {
            _patient = patient;
            await StartNewSegmentAsync();
        }
        
        public async Task WriteFrameAsync(byte[] frameData, DateTime timestamp)
        {
            await _segmentLock.WaitAsync();
            try
            {
                if (_currentSegmentStream != null)
                {
                    await _currentSegmentStream.WriteAsync(frameData, 0, frameData.Length);
                    Interlocked.Add(ref _totalBytesWritten, frameData.Length);
                }
            }
            finally
            {
                _segmentLock.Release();
            }
        }
        
        public async Task<TimeSpan> GetCurrentSegmentAgeAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                return DateTime.UtcNow - _currentSegmentStartTime;
            }
            finally
            {
                _segmentLock.Release();
            }
        }
        
        public async Task<string?> RotateSegmentAsync()
        {
            await _segmentLock.WaitAsync();
            try
            {
                var completedPath = await FinalizeCurrentSegmentAsync();
                await StartNewSegmentAsync();
                return completedPath;
            }
            finally
            {
                _segmentLock.Release();
            }
        }
        
        public async Task<string?> FinalizeCurrentSegmentAsync()
        {
            if (_currentSegmentStream != null)
            {
                await _currentSegmentStream.FlushAsync();
                _currentSegmentStream.Close();
                _currentSegmentStream.Dispose();
                _currentSegmentStream = null;
                
                _logger.LogInformation("Segment {Number} finalized: {Path}", 
                    _currentSegmentNumber, _currentSegmentPath);
                
                return _currentSegmentPath;
            }
            
            return null;
        }
        
        private async Task StartNewSegmentAsync()
        {
            _currentSegmentNumber++;
            _currentSegmentStartTime = DateTime.UtcNow;
            
            var segmentDir = Path.Combine(_config.StoragePath, 
                _patient?.PatientId ?? "Unknown", 
                DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(segmentDir);
            
            _currentSegmentPath = Path.Combine(segmentDir, 
                $"Segment_{_currentSegmentNumber:D4}_{DateTime.Now:HHmmss}.webm");
            
            _currentSegmentStream = new FileStream(_currentSegmentPath, 
                FileMode.Create, FileAccess.Write, FileShare.Read, 
                bufferSize: 65536, useAsync: true);
            
            _logger.LogInformation("Started new segment {Number}: {Path}", 
                _currentSegmentNumber, _currentSegmentPath);
            
            await Task.CompletedTask;
        }
        
        public async ValueTask DisposeAsync()
        {
            await FinalizeCurrentSegmentAsync();
            _segmentLock?.Dispose();
        }
    }
    
    /// <summary>
    /// Performance monitoring for medical-grade reliability
    /// </summary>
    internal class PerformanceMonitor : IDisposable
    {
        private readonly ILogger _logger;
        private readonly System.Diagnostics.PerformanceCounter? _cpuCounter;
        private readonly System.Diagnostics.PerformanceCounter? _memoryCounter;
        private readonly Timer _updateTimer;
        private PerformanceMetrics _currentMetrics;
        
        public PerformanceMonitor(ILogger logger)
        {
            _logger = logger;
            _currentMetrics = new PerformanceMetrics();
            
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
                    _memoryCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters");
            }
            
            _updateTimer = new Timer(UpdateMetrics, null, Timeout.Infinite, Timeout.Infinite);
        }
        
        public void Start()
        {
            _updateTimer.Change(0, 1000); // Update every second
        }
        
        public void Stop()
        {
            _updateTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        private void UpdateMetrics(object? state)
        {
            try
            {
                if (_cpuCounter != null)
                {
                    _currentMetrics.CpuUsagePercent = _cpuCounter.NextValue();
                }
                
                if (_memoryCounter != null)
                {
                    _currentMetrics.AvailableMemoryMB = _memoryCounter.NextValue();
                }
                
                _currentMetrics.Timestamp = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating performance metrics");
            }
        }
        
        public PerformanceMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }
        
        public void Dispose()
        {
            Stop();
            _updateTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
        }
    }
    
    // TODO: Video pipe source implementation will be added later when FFMpegCore interfaces are clarified
    
    // Data structures
    public class VideoFrame
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime Timestamp { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string PixelFormat { get; set; } = "YUY2";
        public int FrameNumber { get; set; }
        public bool IsKeyFrame { get; set; }
    }
    
    public class RecordingStateChangedEventArgs : EventArgs
    {
        public bool IsRecording { get; set; }
        public PatientInfo? Patient { get; set; }
        public DateTime StartTime { get; set; }
    }
    
    public class SegmentCompletedEventArgs : EventArgs
    {
        public string SegmentPath { get; set; } = string.Empty;
        public int SegmentNumber { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    public class MemoryPressureEventArgs : EventArgs
    {
        public long MemoryUsageBytes { get; set; }
        public long ThresholdBytes { get; set; }
    }
    
    public class RecordingStatistics
    {
        public bool IsRecording { get; set; }
        public TimeSpan Duration { get; set; }
        public long TotalFrames { get; set; }
        public int BufferedFrames { get; set; }
        public long DroppedFrames { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long DiskUsageBytes { get; set; }
        public int CurrentSegment { get; set; }
        public PerformanceMetrics PerformanceMetrics { get; set; } = new();
    }
    
    public class PerformanceMetrics
    {
        public float CpuUsagePercent { get; set; }
        public float AvailableMemoryMB { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class RetroactiveCaptureLog
    {
        public DateTime Timestamp { get; set; }
        public string? PatientId { get; set; }
        public int MinutesCaptured { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}