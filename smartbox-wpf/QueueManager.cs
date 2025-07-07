using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SmartBoxNext
{
    /// <summary>
    /// Simple JSON-based queue for PACS uploads
    /// Survives power loss, no database needed!
    /// </summary>
    public class QueueManager
    {
        private readonly ILogger<QueueManager> _logger;
        private readonly AppConfig _config;
        private readonly string _queueFilePath;
        private readonly object _lock = new object();
        private Queue _queue;
        private Timer? _saveTimer;
        
        public QueueManager(AppConfig config)
        {
            _config = config;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<QueueManager>();
            
            // Queue file in the queue directory
            _queueFilePath = Path.Combine(_config.Storage.QueuePath, "pacs_queue.json");
            Directory.CreateDirectory(_config.Storage.QueuePath);
            
            // Load existing queue or create new
            _queue = LoadQueue();
            
            // Auto-save every 5 seconds if dirty
            _saveTimer = new Timer(_ => SaveIfDirty(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }
        
        /// <summary>
        /// Add a DICOM file to the upload queue
        /// </summary>
        public void Enqueue(string dicomFilePath, PatientInfo patientInfo)
        {
            var item = new QueueItem
            {
                Id = Guid.NewGuid().ToString(),
                DicomFilePath = dicomFilePath,
                PatientId = patientInfo.PatientId ?? "Unknown",
                PatientName = patientInfo.GetDicomName(),
                EnqueuedAt = DateTime.Now,
                Status = QueueItemStatus.Pending,
                RetryCount = 0
            };
            
            lock (_lock)
            {
                _queue.Items.Add(item);
                _queue.IsDirty = true;
            }
            
            _logger.LogInformation("Enqueued DICOM for upload: {File} (Patient: {Patient})", 
                Path.GetFileName(dicomFilePath), item.PatientName);
        }
        
        /// <summary>
        /// Get next item to process
        /// </summary>
        public QueueItem? GetNextPending()
        {
            lock (_lock)
            {
                // Get oldest pending item that's ready for retry
                var now = DateTime.Now;
                return _queue.Items
                    .Where(i => i.Status == QueueItemStatus.Pending || i.Status == QueueItemStatus.Failed)
                    .Where(i => i.NextRetryAt == null || i.NextRetryAt <= now)
                    .OrderBy(i => i.EnqueuedAt)
                    .FirstOrDefault();
            }
        }
        
        /// <summary>
        /// Mark item as processing
        /// </summary>
        public void MarkProcessing(string itemId)
        {
            lock (_lock)
            {
                var item = _queue.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.Status = QueueItemStatus.Processing;
                    item.LastAttemptAt = DateTime.Now;
                    _queue.IsDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Mark item as successfully sent
        /// </summary>
        public void MarkSuccess(string itemId)
        {
            lock (_lock)
            {
                var item = _queue.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.Status = QueueItemStatus.Sent;
                    item.CompletedAt = DateTime.Now;
                    _queue.IsDirty = true;
                    
                    _logger.LogInformation("PACS upload successful: {File}", 
                        Path.GetFileName(item.DicomFilePath));
                }
            }
        }
        
        /// <summary>
        /// Mark item as failed with retry logic
        /// </summary>
        public void MarkFailed(string itemId, string error)
        {
            lock (_lock)
            {
                var item = _queue.Items.FirstOrDefault(i => i.Id == itemId);
                if (item != null)
                {
                    item.Status = QueueItemStatus.Failed;
                    item.LastError = error;
                    item.RetryCount++;
                    
                    // Calculate next retry time with exponential backoff
                    if (item.RetryCount < _config.Pacs.MaxRetries)
                    {
                        var delaySeconds = _config.Pacs.RetryDelay * Math.Pow(2, item.RetryCount - 1);
                        item.NextRetryAt = DateTime.Now.AddSeconds(delaySeconds);
                        
                        _logger.LogWarning("PACS upload failed, will retry in {Seconds}s: {Error}", 
                            delaySeconds, error);
                    }
                    else
                    {
                        item.Status = QueueItemStatus.PermanentlyFailed;
                        _logger.LogError("PACS upload permanently failed after {Retries} attempts: {Error}", 
                            item.RetryCount, error);
                    }
                    
                    _queue.IsDirty = true;
                }
            }
        }
        
        /// <summary>
        /// Get queue statistics
        /// </summary>
        public QueueStats GetStats()
        {
            lock (_lock)
            {
                return new QueueStats
                {
                    TotalItems = _queue.Items.Count,
                    Pending = _queue.Items.Count(i => i.Status == QueueItemStatus.Pending),
                    Processing = _queue.Items.Count(i => i.Status == QueueItemStatus.Processing),
                    Sent = _queue.Items.Count(i => i.Status == QueueItemStatus.Sent),
                    Failed = _queue.Items.Count(i => i.Status == QueueItemStatus.Failed),
                    PermanentlyFailed = _queue.Items.Count(i => i.Status == QueueItemStatus.PermanentlyFailed)
                };
            }
        }
        
        /// <summary>
        /// Get all items for display
        /// </summary>
        public List<QueueItem> GetAllItems()
        {
            lock (_lock)
            {
                return _queue.Items.OrderByDescending(i => i.EnqueuedAt).ToList();
            }
        }
        
        /// <summary>
        /// Clean up old sent items
        /// </summary>
        public void CleanupOldItems(int daysToKeep = 7)
        {
            lock (_lock)
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var itemsToRemove = _queue.Items
                    .Where(i => i.Status == QueueItemStatus.Sent && i.CompletedAt < cutoffDate)
                    .ToList();
                
                foreach (var item in itemsToRemove)
                {
                    _queue.Items.Remove(item);
                }
                
                if (itemsToRemove.Any())
                {
                    _queue.IsDirty = true;
                    _logger.LogInformation("Cleaned up {Count} old queue items", itemsToRemove.Count);
                }
            }
        }
        
        private Queue LoadQueue()
        {
            try
            {
                if (File.Exists(_queueFilePath))
                {
                    var json = File.ReadAllText(_queueFilePath);
                    var queue = JsonConvert.DeserializeObject<Queue>(json) ?? new Queue();
                    _logger.LogInformation("Loaded queue with {Count} items", queue.Items.Count);
                    return queue;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load queue, creating new one");
            }
            
            return new Queue();
        }
        
        private void SaveQueue()
        {
            try
            {
                lock (_lock)
                {
                    var json = JsonConvert.SerializeObject(_queue, Formatting.Indented);
                    
                    // Write to temp file first for atomicity
                    var tempFile = _queueFilePath + ".tmp";
                    File.WriteAllText(tempFile, json);
                    
                    // Atomic replace
                    File.Move(tempFile, _queueFilePath, true);
                    
                    _queue.IsDirty = false;
                    _logger.LogDebug("Queue saved with {Count} items", _queue.Items.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save queue");
            }
        }
        
        private void SaveIfDirty()
        {
            lock (_lock)
            {
                if (_queue.IsDirty)
                {
                    SaveQueue();
                }
            }
        }
        
        public void Dispose()
        {
            _saveTimer?.Dispose();
            SaveQueue(); // Final save
        }
    }
    
    /// <summary>
    /// The queue container
    /// </summary>
    public class Queue
    {
        public List<QueueItem> Items { get; set; } = new();
        
        [JsonIgnore]
        public bool IsDirty { get; set; }
    }
    
    /// <summary>
    /// A single queue item
    /// </summary>
    public class QueueItem
    {
        public string Id { get; set; } = "";
        public string DicomFilePath { get; set; } = "";
        public string PatientId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public DateTime EnqueuedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? NextRetryAt { get; set; }
        public QueueItemStatus Status { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }
    
    public enum QueueItemStatus
    {
        Pending,
        Processing,
        Sent,
        Failed,
        PermanentlyFailed
    }
    
    /// <summary>
    /// Queue statistics
    /// </summary>
    public class QueueStats
    {
        public int TotalItems { get; set; }
        public int Pending { get; set; }
        public int Processing { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int PermanentlyFailed { get; set; }
        
        public bool HasWork => Pending > 0 || Failed > 0;
    }
}