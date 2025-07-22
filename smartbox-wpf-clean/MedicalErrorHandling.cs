using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// Medical error severity classification for patient safety
    /// </summary>
    public enum MedicalErrorSeverity
    {
        /// <summary>Informational message, no patient impact</summary>
        Info = 0,
        
        /// <summary>Warning condition, monitor but continue</summary>
        Warning = 1,
        
        /// <summary>Error that may impact medical workflow</summary>
        MedicalWorkflow = 2,
        
        /// <summary>Error affecting patient data integrity</summary>
        PatientDataIntegrity = 3,
        
        /// <summary>Critical error requiring immediate attention</summary>
        Critical = 4,
        
        /// <summary>Life-threatening error requiring emergency response</summary>
        Emergency = 5
    }

    /// <summary>
    /// Medical error category for proper handling and recovery
    /// </summary>
    public enum MedicalErrorCategory
    {
        /// <summary>Patient identification or data validation errors</summary>
        PatientData,
        
        /// <summary>DICOM generation or transmission errors</summary>
        DicomProcessing,
        
        /// <summary>PACS connectivity or transmission errors</summary>
        PacsConnectivity,
        
        /// <summary>MWL (Modality Worklist) query or selection errors</summary>
        WorklistManagement,
        
        /// <summary>Video/image capture device errors</summary>
        CaptureDevice,
        
        /// <summary>File system or storage errors</summary>
        StorageSystem,
        
        /// <summary>Network connectivity errors</summary>
        NetworkConnectivity,
        
        /// <summary>Authentication or authorization errors</summary>
        Security,
        
        /// <summary>System resource exhaustion</summary>
        ResourceExhaustion,
        
        /// <summary>Software or configuration errors</summary>
        Configuration
    }

    /// <summary>
    /// Structured medical error for comprehensive handling
    /// </summary>
    public class MedicalError
    {
        public string ErrorId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public MedicalErrorSeverity Severity { get; set; }
        public MedicalErrorCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TechnicalDetails { get; set; }
        public string? PatientContext { get; set; }
        public string? StudyContext { get; set; }
        public Exception? Exception { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public List<string> RecoveryActions { get; set; } = new();
        public bool RequiresUserAction { get; set; }
        public bool IsRecoverable { get; set; } = true;
    }

    /// <summary>
    /// Recovery action delegate for medical error handling
    /// </summary>
    public delegate Task<bool> MedicalErrorRecoveryAction(MedicalError error);

    /// <summary>
    /// Medical error handler with patient safety focus
    /// </summary>
    public static class MedicalErrorHandler
    {
        private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole())
            .CreateLogger(typeof(MedicalErrorHandler));

        private static readonly Dictionary<MedicalErrorCategory, List<MedicalErrorRecoveryAction>> RecoveryActions = new();

        /// <summary>
        /// Register recovery action for specific error category
        /// </summary>
        public static void RegisterRecoveryAction(MedicalErrorCategory category, MedicalErrorRecoveryAction action)
        {
            if (!RecoveryActions.ContainsKey(category))
            {
                RecoveryActions[category] = new List<MedicalErrorRecoveryAction>();
            }
            RecoveryActions[category].Add(action);
        }

        /// <summary>
        /// Handle medical error with appropriate severity response
        /// </summary>
        public static async Task<bool> HandleErrorAsync(MedicalError error)
        {
            // Log the error with appropriate severity
            LogMedicalError(error);

            // Critical and Emergency errors require immediate attention
            if (error.Severity >= MedicalErrorSeverity.Critical)
            {
                await HandleCriticalErrorAsync(error);
            }

            // Attempt recovery if possible
            if (error.IsRecoverable)
            {
                return await AttemptRecoveryAsync(error);
            }

            return false;
        }

        /// <summary>
        /// Create medical error from exception
        /// </summary>
        public static MedicalError FromException(Exception ex, MedicalErrorCategory category, 
            string? patientContext = null, string? studyContext = null)
        {
            var severity = DetermineSeverity(ex, category);
            
            return new MedicalError
            {
                Severity = severity,
                Category = category,
                Message = ex.Message,
                TechnicalDetails = ex.ToString(),
                PatientContext = patientContext,
                StudyContext = studyContext,
                Exception = ex,
                RequiresUserAction = severity >= MedicalErrorSeverity.MedicalWorkflow,
                IsRecoverable = DetermineRecoverability(ex, category)
            };
        }

        /// <summary>
        /// Create patient data validation error
        /// </summary>
        public static MedicalError PatientDataError(string message, string? patientId = null, 
            string? fieldName = null)
        {
            var error = new MedicalError
            {
                Severity = MedicalErrorSeverity.PatientDataIntegrity,
                Category = MedicalErrorCategory.PatientData,
                Message = message,
                PatientContext = patientId,
                RequiresUserAction = true,
                IsRecoverable = true
            };

            if (fieldName != null)
            {
                error.Context["FieldName"] = fieldName;
            }

            error.RecoveryActions.Add("Verify patient information");
            error.RecoveryActions.Add("Check worklist selection");
            error.RecoveryActions.Add("Manually enter patient data");

            return error;
        }

        /// <summary>
        /// Create PACS connectivity error
        /// </summary>
        public static MedicalError PacsConnectivityError(string message, string? serverHost = null, 
            int? serverPort = null)
        {
            var error = new MedicalError
            {
                Severity = MedicalErrorSeverity.MedicalWorkflow,
                Category = MedicalErrorCategory.PacsConnectivity,
                Message = message,
                RequiresUserAction = true,
                IsRecoverable = true
            };

            if (serverHost != null)
            {
                error.Context["ServerHost"] = serverHost;
            }
            if (serverPort != null)
            {
                error.Context["ServerPort"] = serverPort;
            }

            error.RecoveryActions.Add("Check network connectivity");
            error.RecoveryActions.Add("Verify PACS server configuration");
            error.RecoveryActions.Add("Retry connection");
            error.RecoveryActions.Add("Save DICOM locally for later transmission");

            return error;
        }

        /// <summary>
        /// Create capture device error
        /// </summary>
        public static MedicalError CaptureDeviceError(string message, string? deviceName = null)
        {
            var error = new MedicalError
            {
                Severity = MedicalErrorSeverity.Critical,
                Category = MedicalErrorCategory.CaptureDevice,
                Message = message,
                RequiresUserAction = true,
                IsRecoverable = true
            };

            if (deviceName != null)
            {
                error.Context["DeviceName"] = deviceName;
            }

            error.RecoveryActions.Add("Check device connections");
            error.RecoveryActions.Add("Restart capture service");
            error.RecoveryActions.Add("Switch to alternative capture source");
            error.RecoveryActions.Add("Contact technical support if persistent");

            return error;
        }

        private static void LogMedicalError(MedicalError error)
        {
            var logMessage = $"[MEDICAL ERROR] {error.Category}: {error.Message}";
            
            if (!string.IsNullOrEmpty(error.PatientContext))
            {
                logMessage += $" | Patient: {error.PatientContext}";
            }
            
            if (!string.IsNullOrEmpty(error.StudyContext))
            {
                logMessage += $" | Study: {error.StudyContext}";
            }

            switch (error.Severity)
            {
                case MedicalErrorSeverity.Info:
                    Logger.LogInformation(logMessage);
                    break;
                case MedicalErrorSeverity.Warning:
                    Logger.LogWarning(logMessage);
                    break;
                case MedicalErrorSeverity.MedicalWorkflow:
                    Logger.LogWarning("[WORKFLOW IMPACT] {Message}", logMessage);
                    break;
                case MedicalErrorSeverity.PatientDataIntegrity:
                    Logger.LogError("[PATIENT DATA] {Message}", logMessage);
                    break;
                case MedicalErrorSeverity.Critical:
                    Logger.LogCritical("[CRITICAL MEDICAL ERROR] {Message}", logMessage);
                    break;
                case MedicalErrorSeverity.Emergency:
                    Logger.LogCritical("[EMERGENCY] {Message}", logMessage);
                    break;
            }

            // Log technical details separately for debugging
            if (!string.IsNullOrEmpty(error.TechnicalDetails))
            {
                Logger.LogDebug("[TECHNICAL DETAILS] {ErrorId}: {Details}", error.ErrorId, error.TechnicalDetails);
            }
        }

        private static async Task HandleCriticalErrorAsync(MedicalError error)
        {
            // Critical errors require immediate logging and potential system alerts
            Logger.LogCritical("[CRITICAL MEDICAL ERROR] ID: {ErrorId}, Category: {Category}, Message: {Message}",
                error.ErrorId, error.Category, error.Message);

            // TODO: Implement critical error notifications (email, SMS, etc.)
            // TODO: Implement automatic system diagnostics collection
            
            await Task.CompletedTask;
        }

        private static async Task<bool> AttemptRecoveryAsync(MedicalError error)
        {
            if (!RecoveryActions.ContainsKey(error.Category))
            {
                Logger.LogWarning("No recovery actions registered for category: {Category}", error.Category);
                return false;
            }

            foreach (var recoveryAction in RecoveryActions[error.Category])
            {
                try
                {
                    Logger.LogInformation("Attempting recovery for error {ErrorId} using action: {Action}", 
                        error.ErrorId, recoveryAction.Method.Name);

                    if (await recoveryAction(error))
                    {
                        Logger.LogInformation("Recovery successful for error {ErrorId}", error.ErrorId);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Recovery action failed for error {ErrorId}", error.ErrorId);
                }
            }

            Logger.LogWarning("All recovery attempts failed for error {ErrorId}", error.ErrorId);
            return false;
        }

        private static MedicalErrorSeverity DetermineSeverity(Exception ex, MedicalErrorCategory category)
        {
            // Determine severity based on exception type and category
            return category switch
            {
                MedicalErrorCategory.PatientData => MedicalErrorSeverity.PatientDataIntegrity,
                MedicalErrorCategory.CaptureDevice => MedicalErrorSeverity.Critical,
                MedicalErrorCategory.Security => MedicalErrorSeverity.Critical,
                MedicalErrorCategory.ResourceExhaustion => MedicalErrorSeverity.Critical,
                MedicalErrorCategory.DicomProcessing => MedicalErrorSeverity.MedicalWorkflow,
                MedicalErrorCategory.PacsConnectivity => MedicalErrorSeverity.MedicalWorkflow,
                MedicalErrorCategory.WorklistManagement => MedicalErrorSeverity.Warning,
                MedicalErrorCategory.StorageSystem => MedicalErrorSeverity.MedicalWorkflow,
                MedicalErrorCategory.NetworkConnectivity => MedicalErrorSeverity.Warning,
                MedicalErrorCategory.Configuration => MedicalErrorSeverity.Warning,
                _ => MedicalErrorSeverity.Warning
            };
        }

        private static bool DetermineRecoverability(Exception ex, MedicalErrorCategory category)
        {
            // Most medical errors are recoverable with appropriate action
            return category switch
            {
                MedicalErrorCategory.ResourceExhaustion => false, // May require restart
                MedicalErrorCategory.Configuration => false,      // Requires manual config fix
                _ => true
            };
        }
    }

    /// <summary>
    /// Extension methods for easy medical error handling
    /// </summary>
    public static class MedicalErrorExtensions
    {
        /// <summary>
        /// Handle exception as medical error
        /// </summary>
        public static async Task<bool> HandleAsMedicalErrorAsync(this Exception ex, 
            MedicalErrorCategory category, string? patientContext = null, string? studyContext = null)
        {
            var error = MedicalErrorHandler.FromException(ex, category, patientContext, studyContext);
            return await MedicalErrorHandler.HandleErrorAsync(error);
        }

        /// <summary>
        /// Add patient context to error
        /// </summary>
        public static MedicalError WithPatientContext(this MedicalError error, string patientId, string? studyInstanceUID = null)
        {
            error.PatientContext = patientId;
            error.StudyContext = studyInstanceUID;
            return error;
        }

        /// <summary>
        /// Add recovery action to error
        /// </summary>
        public static MedicalError WithRecoveryAction(this MedicalError error, string action)
        {
            error.RecoveryActions.Add(action);
            return error;
        }
    }
}