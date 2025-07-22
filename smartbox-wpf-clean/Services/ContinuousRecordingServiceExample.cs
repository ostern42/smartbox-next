using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Example integration of ContinuousRecordingService with SmartBox-Next
    /// </summary>
    public class ContinuousRecordingServiceExample
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContinuousRecordingServiceExample> _logger;
        
        public ContinuousRecordingServiceExample(IServiceProvider serviceProvider, ILogger<ContinuousRecordingServiceExample> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        /// <summary>
        /// Example: Configure services for dependency injection
        /// </summary>
        public static void ConfigureServices(IServiceCollection services)
        {
            // Configure continuous recording with custom settings
            services.AddSingleton<ContinuousRecordingConfig>(sp => new ContinuousRecordingConfig
            {
                SegmentDurationMinutes = 30, // 30-minute segments
                MaxRecordingDuration = TimeSpan.FromHours(4), // 4-hour maximum
                MemoryThresholdBytes = 2L * 1024 * 1024 * 1024, // 2GB
                OffloadPercentage = 0.25, // Offload 25% when threshold hit
                CircularBufferSizeMB = 4096, // 4GB buffer
                EnableAudioRecording = true,
                AudioSampleRate = 48000,
                AudioBitrate = 192000
            });
            
            // Register services
            services.AddSingleton<IContinuousRecordingService, ContinuousRecordingService>();
            services.AddSingleton<DicomVideoService>();
            services.AddSingleton<FFmpegService>();
            services.AddSingleton<UnifiedCaptureManager>();
        }
        
        /// <summary>
        /// Example: Start continuous recording on patient selection
        /// </summary>
        public async Task OnPatientSelectedAsync(PatientInfo patient)
        {
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            // Subscribe to events
            recordingService.RecordingStateChanged += OnRecordingStateChanged;
            recordingService.SegmentCompleted += OnSegmentCompleted;
            recordingService.MemoryPressureDetected += OnMemoryPressureDetected;
            
            try
            {
                // Start recording
                await recordingService.StartRecordingAsync(patient);
                _logger.LogInformation("Continuous recording started for patient {PatientId}", patient.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start continuous recording");
            }
        }
        
        /// <summary>
        /// Example: Implement the "2 Minuten zurück" button
        /// </summary>
        public async Task OnRetroactiveCaptureClickedAsync()
        {
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            if (!recordingService.IsRecording)
            {
                _logger.LogWarning("No recording in progress for retroactive capture");
                return;
            }
            
            try
            {
                // Save last 2 minutes with reason
                var outputPath = await recordingService.SaveLastMinutesAsync(
                    minutes: 2, 
                    reason: "Critical moment captured by operator");
                
                _logger.LogInformation("Retroactive capture saved: {Path}", outputPath);
                
                // Optionally convert to DICOM
                await ConvertToDicomAsync(outputPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save retroactive capture");
            }
        }
        
        /// <summary>
        /// Example: Handle Schluckdiagnostik with audio
        /// </summary>
        public async Task OnSchluckdiagnostikStartAsync(PatientInfo patient)
        {
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            // Ensure audio is enabled for Schluckdiagnostik
            var config = _serviceProvider.GetRequiredService<ContinuousRecordingConfig>();
            config.EnableAudioRecording = true;
            config.AudioSampleRate = 48000; // Medical-grade audio
            
            await recordingService.StartRecordingAsync(patient);
            _logger.LogInformation("Schluckdiagnostik recording started with audio");
        }
        
        /// <summary>
        /// Example: Capture photo during video recording
        /// </summary>
        public async Task OnPhotoCaptureClickedAsync()
        {
            var captureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            // Capture photo doesn't interrupt video recording
            var photo = await captureManager.CapturePhotoAsync();
            
            if (photo != null)
            {
                _logger.LogInformation("Photo captured during continuous recording");
                
                // Save photo separately
                // Photo capture is independent of video recording
            }
        }
        
        /// <summary>
        /// Example: Monitor recording statistics
        /// </summary>
        public void DisplayRecordingStatistics()
        {
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            if (recordingService.IsRecording)
            {
                var stats = recordingService.GetStatistics();
                
                _logger.LogInformation(
                    "Recording Stats - Duration: {Duration}, Frames: {Frames}, Memory: {Memory:F2} GB, Segment: {Segment}",
                    stats.Duration,
                    stats.TotalFrames,
                    stats.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0),
                    stats.CurrentSegment);
                
                // Update UI with statistics
                // Show CPU usage warning if > 50%
                if (stats.PerformanceMetrics.CpuUsagePercent > 50)
                {
                    _logger.LogWarning("High CPU usage: {Cpu}%", stats.PerformanceMetrics.CpuUsagePercent);
                }
            }
        }
        
        /// <summary>
        /// Example: Stop recording on patient change
        /// </summary>
        public async Task OnPatientChangingAsync()
        {
            var recordingService = _serviceProvider.GetRequiredService<IContinuousRecordingService>();
            
            if (recordingService.IsRecording)
            {
                await recordingService.StopRecordingAsync();
                _logger.LogInformation("Stopped recording due to patient change");
            }
        }
        
        // Event handlers
        private void OnRecordingStateChanged(object? sender, RecordingStateChangedEventArgs e)
        {
            _logger.LogInformation("Recording state changed: {IsRecording} for patient {PatientId}",
                e.IsRecording, e.Patient?.PatientId);
        }
        
        private void OnSegmentCompleted(object? sender, SegmentCompletedEventArgs e)
        {
            _logger.LogInformation("Segment {Number} completed: {Path}, Duration: {Duration}",
                e.SegmentNumber, e.SegmentPath, e.Duration);
            
            // Optionally process completed segments (e.g., backup, compress)
            _ = Task.Run(() => ProcessCompletedSegmentAsync(e.SegmentPath));
        }
        
        private void OnMemoryPressureDetected(object? sender, MemoryPressureEventArgs e)
        {
            _logger.LogWarning("Memory pressure detected: {Usage:F2} GB / {Threshold:F2} GB",
                e.MemoryUsageBytes / (1024.0 * 1024.0 * 1024.0),
                e.ThresholdBytes / (1024.0 * 1024.0 * 1024.0));
        }
        
        private async Task ConvertToDicomAsync(string videoPath)
        {
            var dicomService = _serviceProvider.GetRequiredService<DicomVideoService>();
            
            // Get current patient info
            var patient = new PatientInfo
            {
                PatientId = "12345",
                FirstName = "Test",
                LastName = "Patient",
                StudyDescription = "Retroactive Capture"
            };
            
            var dicomPath = await dicomService.ProcessWebMToDicomAsync(videoPath, patient);
            _logger.LogInformation("Video converted to DICOM: {Path}", dicomPath);
        }
        
        private async Task ProcessCompletedSegmentAsync(string segmentPath)
        {
            // Example: Compress or backup completed segments
            await Task.Delay(1); // Placeholder for actual processing
            _logger.LogInformation("Processed completed segment: {Path}", segmentPath);
        }
    }
    
    /// <summary>
    /// Example: WPF ViewModel integration
    /// </summary>
    public class ContinuousRecordingViewModel
    {
        private readonly IContinuousRecordingService _recordingService;
        private readonly ILogger<ContinuousRecordingViewModel> _logger;
        
        public bool IsRecording => _recordingService.IsRecording;
        public string RecordingDuration => _recordingService.RecordingDuration.ToString(@"hh\:mm\:ss");
        public string MemoryUsage => $"{_recordingService.GetStatistics().MemoryUsageBytes / (1024.0 * 1024.0):F0} MB";
        
        public ContinuousRecordingViewModel(
            IContinuousRecordingService recordingService,
            ILogger<ContinuousRecordingViewModel> logger)
        {
            _recordingService = recordingService;
            _logger = logger;
        }
        
        public async Task SaveLastTwoMinutesAsync()
        {
            try
            {
                var path = await _recordingService.SaveLastMinutesAsync(2, "User requested retroactive capture");
                _logger.LogInformation("Saved retroactive capture: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save retroactive capture");
            }
        }
    }
}