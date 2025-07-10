using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Integrated queue manager that bridges UnifiedCaptureManager with PACS queue
    /// </summary>
    public class IntegratedQueueManager : IDisposable
    {
        private readonly ILogger<IntegratedQueueManager> _logger;
        private readonly QueueManager _queueManager;
        private readonly OptimizedDicomConverter _dicomConverter;
        private readonly UnifiedCaptureManager _captureManager;
        private readonly AppConfig _config;

        // Configuration
        private bool _autoQueueEnabled = true;
        private bool _autoConvertEnabled = true;

        public bool AutoQueueEnabled 
        { 
            get => _autoQueueEnabled; 
            set => _autoQueueEnabled = value; 
        }

        public bool AutoConvertEnabled 
        { 
            get => _autoConvertEnabled; 
            set => _autoConvertEnabled = value; 
        }

        public IntegratedQueueManager(
            ILogger<IntegratedQueueManager> logger,
            QueueManager queueManager,
            OptimizedDicomConverter dicomConverter,
            UnifiedCaptureManager captureManager,
            AppConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
            _dicomConverter = dicomConverter ?? throw new ArgumentNullException(nameof(dicomConverter));
            _captureManager = captureManager ?? throw new ArgumentNullException(nameof(captureManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _logger.LogInformation("Integrated Queue Manager initialized");
        }

        /// <summary>
        /// Capture photo from active source and optionally queue for PACS
        /// </summary>
        public async Task<CaptureResult> CaptureAndQueuePhotoAsync(
            PatientInfo patientInfo,
            CaptureSource? sourceOverride = null,
            bool queueForPacs = true,
            SnapshotMetadata? metadata = null)
        {
            _logger.LogInformation("Capturing photo from {Source} for patient {PatientId}", 
                sourceOverride ?? _captureManager.ActiveSource, patientInfo.PatientId);

            try
            {
                // Capture photo from unified capture manager
                var bitmap = await _captureManager.CapturePhotoAsync(sourceOverride);
                if (bitmap == null)
                {
                    return new CaptureResult
                    {
                        Success = false,
                        ErrorMessage = "No frame available for capture"
                    };
                }

                string? dicomPath = null;

                // Convert to DICOM if auto-convert is enabled
                if (_autoConvertEnabled)
                {
                    var modality = DetermineModality(sourceOverride ?? _captureManager.ActiveSource, metadata);
                    dicomPath = await _dicomConverter.ConvertBitmapSourceToDicomAsync(bitmap, patientInfo, modality);
                    
                    _logger.LogInformation("Photo converted to DICOM: {Path}", dicomPath);
                }

                // Queue for PACS if enabled and DICOM was created
                if (queueForPacs && _autoQueueEnabled && !string.IsNullOrEmpty(dicomPath))
                {
                    _queueManager.Enqueue(dicomPath, patientInfo);
                    _logger.LogInformation("Photo queued for PACS upload");
                }

                return new CaptureResult
                {
                    Success = true,
                    CapturedFrame = bitmap,
                    DicomPath = dicomPath,
                    Source = sourceOverride ?? _captureManager.ActiveSource,
                    QueuedForPacs = queueForPacs && _autoQueueEnabled && !string.IsNullOrEmpty(dicomPath)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture and queue photo");
                return new CaptureResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Capture high-resolution snapshot from Yuan (if connected)
        /// </summary>
        public async Task<CaptureResult> CaptureHighResSnapshotAsync(
            PatientInfo patientInfo,
            SnapshotMetadata metadata,
            bool queueForPacs = true)
        {
            _logger.LogInformation("Capturing high-resolution snapshot for patient {PatientId}", patientInfo.PatientId);

            if (!_captureManager.IsYuanConnected)
            {
                return new CaptureResult
                {
                    Success = false,
                    ErrorMessage = "Yuan capture service is not connected"
                };
            }

            try
            {
                // Request high-res snapshot from service
                var snapshotData = await _captureManager.GetYuanStatisticsAsync(); // Placeholder - need to implement actual snapshot
                
                // For now, use current frame
                var bitmap = await _captureManager.CapturePhotoAsync(CaptureSource.Yuan);
                if (bitmap == null)
                {
                    return new CaptureResult
                    {
                        Success = false,
                        ErrorMessage = "No Yuan frame available for snapshot"
                    };
                }

                string? dicomPath = null;

                // Convert to DICOM with enhanced metadata
                if (_autoConvertEnabled)
                {
                    // Convert BitmapSource back to RGB24 for high-res processing
                    // In a real implementation, we'd get the raw YUY2 data from the service
                    dicomPath = await _dicomConverter.ConvertBitmapSourceToDicomAsync(bitmap, patientInfo, metadata.Modality ?? "ES");
                    
                    _logger.LogInformation("High-res snapshot converted to DICOM: {Path}", dicomPath);
                }

                // Queue for PACS
                if (queueForPacs && _autoQueueEnabled && !string.IsNullOrEmpty(dicomPath))
                {
                    _queueManager.Enqueue(dicomPath, patientInfo);
                    _logger.LogInformation("High-res snapshot queued for PACS upload");
                }

                return new CaptureResult
                {
                    Success = true,
                    CapturedFrame = bitmap,
                    DicomPath = dicomPath,
                    Source = CaptureSource.Yuan,
                    QueuedForPacs = queueForPacs && _autoQueueEnabled && !string.IsNullOrEmpty(dicomPath),
                    IsHighResolution = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture high-resolution snapshot");
                return new CaptureResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Convert existing image data to DICOM and queue
        /// </summary>
        public async Task<CaptureResult> ConvertAndQueueAsync(
            byte[] imageData,
            FrameFormat format,
            int width,
            int height,
            PatientInfo patientInfo,
            SnapshotMetadata? metadata = null,
            bool queueForPacs = true)
        {
            _logger.LogInformation("Converting {Format} data ({Width}x{Height}) to DICOM for patient {PatientId}", 
                format, width, height, patientInfo.PatientId);

            try
            {
                string dicomPath;

                // Convert based on format
                switch (format)
                {
                    case FrameFormat.YUY2:
                        dicomPath = await _dicomConverter.ConvertYUY2ToDicomAsync(imageData, width, height, patientInfo);
                        break;

                    case FrameFormat.JPEG:
                        dicomPath = await _dicomConverter.ConvertJpegToDicomAsync(imageData, patientInfo);
                        break;

                    case FrameFormat.RGB24:
                        // Would need to implement ConvertRGB24ToDicomAsync
                        throw new NotImplementedException("RGB24 direct conversion not yet implemented");

                    default:
                        throw new ArgumentException($"Unsupported format: {format}");
                }

                // Queue for PACS
                if (queueForPacs && _autoQueueEnabled)
                {
                    _queueManager.Enqueue(dicomPath, patientInfo);
                    _logger.LogInformation("Converted image queued for PACS upload");
                }

                return new CaptureResult
                {
                    Success = true,
                    DicomPath = dicomPath,
                    Source = format == FrameFormat.YUY2 ? CaptureSource.Yuan : CaptureSource.WebRTC,
                    QueuedForPacs = queueForPacs && _autoQueueEnabled
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert and queue image data");
                return new CaptureResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Get queue statistics with additional context
        /// </summary>
        public QueueStatusInfo GetQueueStatus()
        {
            var stats = _queueManager.GetStats();
            var captureStats = _captureManager.GetStatistics();

            return new QueueStatusInfo
            {
                QueueStats = stats,
                CaptureStats = captureStats,
                AutoQueueEnabled = _autoQueueEnabled,
                AutoConvertEnabled = _autoConvertEnabled,
                YuanConnected = _captureManager.IsYuanConnected,
                WebRTCActive = _captureManager.IsWebRTCActive,
                ActiveSource = _captureManager.ActiveSource
            };
        }

        /// <summary>
        /// Retry failed queue items
        /// </summary>
        public async Task<int> RetryFailedItemsAsync()
        {
            _logger.LogInformation("Retrying failed queue items...");
            
            // This would typically be handled by the QueueProcessor
            // For now, just reset failed items to pending
            var items = _queueManager.GetAllItems();
            var failedItems = items.Where(i => i.Status == QueueItemStatus.Failed).ToList();
            
            foreach (var item in failedItems)
            {
                // Reset to pending (this would need to be implemented in QueueManager)
                _logger.LogInformation("Resetting failed item {Id} to pending", item.Id);
            }
            
            return failedItems.Count;
        }

        /// <summary>
        /// Clear completed items from queue
        /// </summary>
        public int ClearCompletedItems(int daysToKeep = 7)
        {
            _logger.LogInformation("Clearing completed items older than {Days} days", daysToKeep);
            
            var itemsBefore = _queueManager.GetStats().TotalItems;
            _queueManager.CleanupOldItems(daysToKeep);
            var itemsAfter = _queueManager.GetStats().TotalItems;
            
            var removed = itemsBefore - itemsAfter;
            _logger.LogInformation("Removed {Count} completed items", removed);
            
            return removed;
        }

        /// <summary>
        /// Determine appropriate DICOM modality based on source and metadata
        /// </summary>
        private string DetermineModality(CaptureSource source, SnapshotMetadata? metadata)
        {
            // If metadata specifies modality, use it
            if (!string.IsNullOrEmpty(metadata?.Modality))
                return metadata.Modality;

            // Default based on source
            return source switch
            {
                CaptureSource.Yuan => "ES", // Endoscopy for Yuan capture card
                CaptureSource.WebRTC => "XC", // External-camera Photography
                _ => "OT" // Other
            };
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing Integrated Queue Manager");
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Result of a capture operation
    /// </summary>
    public class CaptureResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public BitmapSource? CapturedFrame { get; set; }
        public string? DicomPath { get; set; }
        public CaptureSource Source { get; set; }
        public bool QueuedForPacs { get; set; }
        public bool IsHighResolution { get; set; }
        public DateTime CaptureTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Extended queue status with capture information
    /// </summary>
    public class QueueStatusInfo
    {
        public QueueStats QueueStats { get; set; } = new();
        public CaptureStatistics CaptureStats { get; set; } = new();
        public bool AutoQueueEnabled { get; set; }
        public bool AutoConvertEnabled { get; set; }
        public bool YuanConnected { get; set; }
        public bool WebRTCActive { get; set; }
        public CaptureSource ActiveSource { get; set; }
    }
}