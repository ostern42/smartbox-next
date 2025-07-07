using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// Background service that processes the PACS upload queue
    /// </summary>
    public class QueueProcessor : IDisposable
    {
        private readonly ILogger<QueueProcessor> _logger;
        private readonly QueueManager _queueManager;
        private readonly PacsSender _pacsSender;
        private readonly AppConfig _config;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _processingTask;
        private bool _isRunning;
        
        public QueueProcessor(AppConfig config, QueueManager queueManager, PacsSender pacsSender)
        {
            _config = config;
            _queueManager = queueManager;
            _pacsSender = pacsSender;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<QueueProcessor>();
        }
        
        /// <summary>
        /// Start processing the queue
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Queue processor is already running");
                return;
            }
            
            _logger.LogInformation("Starting queue processor");
            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token));
        }
        
        /// <summary>
        /// Stop processing the queue
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }
            
            _logger.LogInformation("Stopping queue processor");
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            
            if (_processingTask != null)
            {
                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
            
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
        
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queue processor started");
            
            // Initial delay to let the application start up
            await Task.Delay(5000, cancellationToken);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if PACS is configured
                    if (string.IsNullOrEmpty(_config.Pacs.ServerHost))
                    {
                        _logger.LogDebug("PACS not configured, skipping queue processing");
                        await Task.Delay(30000, cancellationToken); // Check every 30 seconds
                        continue;
                    }
                    
                    // Get next item to process
                    var item = _queueManager.GetNextPending();
                    if (item == null)
                    {
                        // No work, wait a bit
                        await Task.Delay(5000, cancellationToken);
                        continue;
                    }
                    
                    // Process the item
                    await ProcessItemAsync(item, cancellationToken);
                    
                    // Small delay between items
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue processor");
                    await Task.Delay(5000, cancellationToken); // Wait before retry
                }
            }
            
            _logger.LogInformation("Queue processor stopped");
        }
        
        private async Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing queue item: {Id} (File: {File})", 
                item.Id, System.IO.Path.GetFileName(item.DicomFilePath));
            
            // Mark as processing
            _queueManager.MarkProcessing(item.Id);
            
            try
            {
                // Check if file still exists
                if (!System.IO.File.Exists(item.DicomFilePath))
                {
                    _logger.LogWarning("DICOM file no longer exists: {File}", item.DicomFilePath);
                    _queueManager.MarkFailed(item.Id, "File not found");
                    return;
                }
                
                // Send to PACS
                var result = await _pacsSender.SendDicomFileAsync(item.DicomFilePath);
                
                if (result.Success)
                {
                    _queueManager.MarkSuccess(item.Id);
                    
                    // Optionally delete the file after successful upload
                    // (based on configuration - not implemented yet)
                }
                else
                {
                    _queueManager.MarkFailed(item.Id, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process queue item");
                _queueManager.MarkFailed(item.Id, ex.Message);
            }
        }
        
        public void Dispose()
        {
            StopAsync().Wait(TimeSpan.FromSeconds(10));
        }
    }
}