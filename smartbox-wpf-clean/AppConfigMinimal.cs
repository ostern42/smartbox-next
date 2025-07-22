using System;
using System.IO;
using System.Text.Json;
using SmartBoxNext.Medical;

namespace SmartBoxNext
{
    public class AppConfig
    {
        // NEW: Medical compliance section (FDA 21 CFR Part 820)
        public MedicalConfig Medical { get; set; } = new();
        
        // EXISTING: Keep for backward compatibility
        public ApplicationSettings Application { get; set; } = new();
        public DicomSettings Dicom { get; set; } = new();
        public StorageSettings Storage { get; set; } = new();
        public PacsSettings Pacs { get; set; } = new();
        public MwlSettings MwlSettings { get; set; } = new();
        public VideoSettings Video { get; set; } = new();
        
        // Local AE Title for DICOM communication (now using medical constants)
        public string LocalAET { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;

        public static AppConfig LoadFromFile(string configPath = "config.json")
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Config load error: {ex.Message}");
            }
            return new AppConfig();
        }
        
        public static string GetConfigPath()
        {
            // Use AppData for persistent storage across app updates
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SmartBoxNext"
            );
            
            // Ensure directory exists
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            
            return Path.Combine(appDataPath, "config.json");
        }
    }

    public class ApplicationSettings
    {
        public int WebServerPort { get; set; } = MedicalConstants.SMARTBOX_WEB_PORT;
        public string Title { get; set; } = "SmartBox Next";
        public bool AutoStartCapture { get; set; } = false;
        public string PreferredDisplay { get; set; } = "primary";
        public string KioskPassword { get; set; } = "1234";
        public bool EnableDebugLogging { get; set; } = false;
    }

    public class DicomSettings
    {
        public string OutputDirectory { get; set; } = "DicomOutput";
        public string StationName { get; set; } = MedicalConstants.DEFAULT_STATION_NAME;
        public string AeTitle { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;
        public string Modality { get; set; } = MedicalConstants.MODALITY_EXTERNAL_CAMERA;
        public string PatientIdPrefix { get; set; } = "SB";
        public bool AutoGenerateAccessionNumber { get; set; } = true;
        public string ImplementationClassUID { get; set; } = "1.2.826.0.1.3680043.8.1055.1";
        public string ImplementationVersionName { get; set; } = "SMARTBOX_V1";
    }

    public class StorageSettings
    {
        public string PhotosPath { get; set; } = "C:\\SmartBox\\Photos";
        public string VideosPath { get; set; } = "C:\\SmartBox\\Videos";
        public string TempPath { get; set; } = "C:\\SmartBox\\Temp";
        public string DicomPath { get; set; } = "C:\\SmartBox\\DICOM";
        public bool EnableAutoCleanup { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
        public bool CompressOldFiles { get; set; } = false;
    }

    public class PacsSettings
    {
        public bool Enabled { get; set; } = false;
        public string ServerHost { get; set; } = "";
        public int ServerPort { get; set; } = MedicalConstants.DICOM_DEFAULT_PORT;
        public string CalledAeTitle { get; set; } = "PACS";
        public string CallingAeTitle { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;
        public int Timeout { get; set; } = MedicalConstants.PACS_CONNECTION_TIMEOUT_S;
        public bool UseSecureConnection { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public bool AutoSendOnCapture { get; set; } = false;
        public bool SendInBackground { get; set; } = true;
    }

    public class MwlSettings
    {
        public bool EnableWorklist { get; set; } = false;
        public string MwlServerHost { get; set; } = "";
        public int MwlServerPort { get; set; } = MedicalConstants.MWL_DEFAULT_PORT;
        public string MwlServerAET { get; set; } = "MWL_SCP";
        public string LocalAET { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;
        public int CacheExpiryHours { get; set; } = 24;
        public int AutoRefreshSeconds { get; set; } = 300;
        public bool ShowEmergencyFirst { get; set; } = true;
        public string ScheduledStationAETitle { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;
        public string ScheduledStationName { get; set; } = MedicalConstants.DEFAULT_STATION_NAME;
        public string DefaultQueryPeriod { get; set; } = "3days";  // today, 3days, week, custom
        public int QueryDaysBefore { get; set; } = 1;  // For custom period
        public int QueryDaysAfter { get; set; } = 1;   // For custom period
    }

    public class VideoSettings
    {
        public int MaxRecordingMinutes { get; set; } = MedicalConstants.MAX_PATIENT_SESSION_MINUTES;
        public string VideoCodec { get; set; } = "H264";
        public int VideoBitrate { get; set; } = 5000;
        public int VideoFramerate { get; set; } = 30;
        public string VideoResolution { get; set; } = "1920x1080";
        public bool EnableAudioCapture { get; set; } = false;
        public int AudioBitrate { get; set; } = 128;
        public bool ShowRecordingIndicator { get; set; } = true;
        public bool AutoStopOnInactivity { get; set; } = true;
        public int InactivityTimeout { get; set; } = 10;
    }
}