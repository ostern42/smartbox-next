using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// PACS C-STORE implementation for sending DICOM files
    /// </summary>
    public class PacsSender
    {
        private readonly ILogger<PacsSender> _logger;
        private readonly AppConfig _config;
        
        public PacsSender(AppConfig config)
        {
            _config = config;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<PacsSender>();
        }
        
        /// <summary>
        /// Test PACS connection using C-ECHO
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.Pacs.ServerHost))
                {
                    _logger.LogWarning("PACS server not configured");
                    return false;
                }
                
                _logger.LogInformation("Testing PACS connection to {Host}:{Port}", 
                    _config.Pacs.ServerHost, _config.Pacs.ServerPort);
                
                var client = DicomClientFactory.Create(
                    _config.Pacs.ServerHost, 
                    _config.Pacs.ServerPort, 
                    _config.Pacs.EnableTls, 
                    _config.Pacs.CallingAeTitle, 
                    _config.Pacs.CalledAeTitle);
                
                client.NegotiateAsyncOps();
                
                var request = new DicomCEchoRequest();
                bool success = false;
                
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.LogInformation("C-ECHO successful");
                        success = true;
                    }
                    else
                    {
                        _logger.LogWarning("C-ECHO failed: {Status}", response.Status);
                    }
                };
                
                await client.AddRequestAsync(request);
                await client.SendAsync();
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PACS connection test failed");
                return false;
            }
        }
        
        /// <summary>
        /// Send DICOM file to PACS using C-STORE
        /// </summary>
        public async Task<SendResult> SendDicomFileAsync(string filePath)
        {
            var result = new SendResult { FilePath = filePath };
            
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"DICOM file not found: {filePath}");
                }
                
                if (string.IsNullOrEmpty(_config.Pacs.ServerHost))
                {
                    throw new InvalidOperationException("PACS server not configured");
                }
                
                _logger.LogInformation("Sending DICOM file to PACS: {File}", Path.GetFileName(filePath));
                
                // Load DICOM file
                var file = await DicomFile.OpenAsync(filePath);
                
                // Create client
                var client = DicomClientFactory.Create(
                    _config.Pacs.ServerHost, 
                    _config.Pacs.ServerPort, 
                    _config.Pacs.EnableTls, 
                    _config.Pacs.CallingAeTitle, 
                    _config.Pacs.CalledAeTitle);
                
                client.NegotiateAsyncOps();
                
                // Create C-STORE request
                var request = new DicomCStoreRequest(file);
                
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        _logger.LogInformation("C-STORE successful for {File}", Path.GetFileName(filePath));
                        result.Success = true;
                        result.Message = "Successfully sent to PACS";
                    }
                    else
                    {
                        _logger.LogWarning("C-STORE failed: {Status} - {Description}", 
                            response.Status.Code, response.Status.Description);
                        result.Success = false;
                        result.Message = $"PACS rejected: {response.Status.Description}";
                    }
                };
                
                // Send with timeout
                await client.AddRequestAsync(request);
                
                var sendTask = client.SendAsync();
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_config.Pacs.Timeout));
                
                var completedTask = await Task.WhenAny(sendTask, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException($"PACS send timeout after {_config.Pacs.Timeout} seconds");
                }
                
                await sendTask; // Ensure any exceptions are thrown
                
                result.Timestamp = DateTime.Now;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send DICOM to PACS");
                
                result.Success = false;
                result.Message = ex.Message;
                result.Exception = ex;
                result.Timestamp = DateTime.Now;
                
                return result;
            }
        }
        
        /// <summary>
        /// Send multiple DICOM files to PACS
        /// </summary>
        public async Task<SendBatchResult> SendDicomBatchAsync(string[] filePaths)
        {
            var batchResult = new SendBatchResult
            {
                TotalFiles = filePaths.Length,
                StartTime = DateTime.Now
            };
            
            foreach (var filePath in filePaths)
            {
                var result = await SendDicomFileAsync(filePath);
                batchResult.Results.Add(result);
                
                if (result.Success)
                {
                    batchResult.SuccessCount++;
                }
                else
                {
                    batchResult.FailureCount++;
                }
                
                // Add delay between sends to avoid overwhelming PACS
                if (filePaths.Length > 1)
                {
                    await Task.Delay(500);
                }
            }
            
            batchResult.EndTime = DateTime.Now;
            return batchResult;
        }
    }
    
    /// <summary>
    /// Result of a single DICOM send operation
    /// </summary>
    public class SendResult
    {
        public string FilePath { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; }
        public int RetryCount { get; set; }
    }
    
    /// <summary>
    /// Result of a batch DICOM send operation
    /// </summary>
    public class SendBatchResult
    {
        public List<SendResult> Results { get; set; } = new();
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public TimeSpan Duration => EndTime - StartTime;
        public bool AllSuccessful => FailureCount == 0;
    }
}