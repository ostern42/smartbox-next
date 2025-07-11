using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    public class PacsService
    {
        private readonly ILogger _logger;
        private readonly string _pacsHost;
        private readonly int _pacsPort;
        private readonly string _callingAeTitle;
        private readonly string _calledAeTitle;
        private readonly int _timeout;

        public PacsService(ILogger logger, string pacsHost, int pacsPort, string callingAeTitle, string calledAeTitle, int timeout = 30)
        {
            _logger = logger;
            _pacsHost = pacsHost;
            _pacsPort = pacsPort;
            _callingAeTitle = callingAeTitle;
            _calledAeTitle = calledAeTitle;
            _timeout = timeout;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation($"Testing PACS connection to {_pacsHost}:{_pacsPort}");
                
                var client = DicomClientFactory.Create(_pacsHost, _pacsPort, false, _callingAeTitle, _calledAeTitle);
                // Note: In fo-dicom 5.x, options are set differently
                // For now, we'll use default timeout

                var success = false;
                var request = new DicomCEchoRequest
                {
                    OnResponseReceived = (req, response) =>
                    {
                        if (response.Status == DicomStatus.Success)
                        {
                            _logger.LogInformation("PACS connection test successful");
                            success = true;
                        }
                        else
                        {
                            _logger.LogWarning($"PACS connection test failed: {response.Status}");
                        }
                    }
                };

                await client.AddRequestAsync(request);
                await client.SendAsync();

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PACS connection test failed with exception");
                return false;
            }
        }

        public async Task<PacsSendResult> SendDicomFileAsync(string dicomFilePath)
        {
            try
            {
                _logger.LogInformation("=== PacsService.SendDicomFileAsync START ===");
                _logger.LogInformation("DICOM file path: {Path}", dicomFilePath);
                _logger.LogInformation("File exists: {Exists}, Size: {Size} bytes", 
                    File.Exists(dicomFilePath), 
                    File.Exists(dicomFilePath) ? new FileInfo(dicomFilePath).Length : 0);
                
                var dicomFile = await DicomFile.OpenAsync(dicomFilePath);
                _logger.LogInformation("DICOM file loaded successfully");
                
                // Log some DICOM metadata
                var dataset = dicomFile.Dataset;
                _logger.LogInformation("Patient Name: {Name}, Patient ID: {ID}, Study UID: {StudyUID}", 
                    dataset.GetSingleValueOrDefault(DicomTag.PatientName, "N/A"),
                    dataset.GetSingleValueOrDefault(DicomTag.PatientID, "N/A"),
                    dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "N/A"));
                
                return await SendDicomFileAsync(dicomFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send DICOM file");
                return new PacsSendResult 
                { 
                    Success = false, 
                    Message = $"Failed to send DICOM file: {ex.Message}" 
                };
            }
        }

        public async Task<PacsSendResult> SendDicomFileAsync(DicomFile dicomFile)
        {
            try
            {
                _logger.LogInformation("=== Creating DICOM Client ===");
                _logger.LogInformation("Target: {Host}:{Port}, AET: {CallingAE} -> {CalledAE}", 
                    _pacsHost, _pacsPort, _callingAeTitle, _calledAeTitle);
                
                var client = DicomClientFactory.Create(_pacsHost, _pacsPort, false, _callingAeTitle, _calledAeTitle);
                // Note: In fo-dicom 5.x, options are set differently
                // For now, we'll use default timeout

                // Enable async operations for better performance
                client.NegotiateAsyncOps();
                _logger.LogInformation("DICOM client created, async ops negotiated");

                var result = new PacsSendResult();
                var sopInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
                _logger.LogInformation("Creating C-STORE request for SOP Instance UID: {UID}", sopInstanceUid);
                
                var request = new DicomCStoreRequest(dicomFile)
                {
                    OnResponseReceived = (req, response) =>
                    {
                        _logger.LogInformation("=== C-STORE Response Received ===");
                        _logger.LogInformation("Status Code: {Code}, Status: {Status}, Description: {Desc}", 
                            response.Status.Code, response.Status.State, response.Status.Description);
                        
                        if (response.Status == DicomStatus.Success)
                        {
                            _logger.LogInformation("✓ DICOM file sent successfully. SOP Instance UID: {UID}", req.SOPInstanceUID);
                            result.Success = true;
                            result.Message = "DICOM file sent successfully";
                            result.SOPInstanceUID = req.SOPInstanceUID.UID;
                        }
                        else
                        {
                            _logger.LogWarning("✗ Failed to send DICOM file. Status: {Status} ({Code})", 
                                response.Status.Description, response.Status.Code);
                            result.Success = false;
                            result.Message = $"PACS returned status: {response.Status.Description} (Code: {response.Status.Code})";
                        }
                    }
                };

                _logger.LogInformation("Adding C-STORE request to client");
                await client.AddRequestAsync(request);
                
                _logger.LogInformation("Sending request to PACS server...");
                await client.SendAsync();
                
                _logger.LogInformation("=== PACS Send Complete ===");
                _logger.LogInformation("Final Result - Success: {Success}, Message: {Message}", 
                    result.Success, result.Message);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send DICOM file to PACS");
                return new PacsSendResult 
                { 
                    Success = false, 
                    Message = $"Exception during PACS send: {ex.Message}" 
                };
            }
        }

        public async Task<PacsSendResult> SendMultipleDicomFilesAsync(string[] dicomFilePaths)
        {
            var successCount = 0;
            var failureCount = 0;
            var lastError = "";

            foreach (var filePath in dicomFilePaths)
            {
                var result = await SendDicomFileAsync(filePath);
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                    lastError = result.Message;
                }
            }

            return new PacsSendResult
            {
                Success = failureCount == 0,
                Message = failureCount == 0 
                    ? $"All {successCount} files sent successfully" 
                    : $"Sent {successCount} files, {failureCount} failed. Last error: {lastError}",
                SuccessCount = successCount,
                FailureCount = failureCount
            };
        }
    }

    public class PacsSendResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? SOPInstanceUID { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }
}