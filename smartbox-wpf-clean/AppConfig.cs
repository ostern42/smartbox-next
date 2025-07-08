using System;
using System.IO;

namespace SmartBoxNext
{
    /// <summary>
    /// Application configuration for medical image capture system
    /// </summary>
    public class AppConfig
    {
        public StorageConfig Storage { get; set; } = new();
        public PacsConfig Pacs { get; set; } = new();
        public VideoConfig Video { get; set; } = new();
        public ApplicationConfig Application { get; set; } = new();
        public MwlConfig MwlSettings { get; set; } = new();
        public MultiTargetConfig MultiTarget { get; set; } = new();
        
        // For backward compatibility
        public string LocalAET => Pacs?.CallingAeTitle ?? "SMARTBOX";
        public string StoragePath => Storage?.PhotosPath?.Replace("\\Photos", "") ?? ".";
        
        /// <summary>
        /// Creates a default configuration suitable for medical environments
        /// </summary>
        public static AppConfig CreateDefault()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            return new AppConfig
            {
                Storage = new StorageConfig
                {
                    PhotosPath = Path.Combine(baseDir, "Data", "Photos"),
                    VideosPath = Path.Combine(baseDir, "Data", "Videos"),
                    DicomPath = Path.Combine(baseDir, "Data", "DICOM"),
                    QueuePath = Path.Combine(baseDir, "Data", "Queue"),
                    TempPath = Path.Combine(baseDir, "Data", "Temp"),
                    MaxStorageDays = 30,
                    EnableAutoCleanup = false
                },
                Pacs = new PacsConfig
                {
                    ServerHost = "",
                    ServerPort = 104,
                    CalledAeTitle = "PACS",
                    CallingAeTitle = "SMARTBOX",
                    Timeout = 30,
                    EnableTls = false,
                    MaxRetries = 3,
                    RetryDelay = 5
                },
                Video = new VideoConfig
                {
                    DefaultResolution = "1280x720",
                    DefaultFrameRate = 30,
                    DefaultQuality = 85,
                    EnableHardwareAcceleration = true,
                    PreferredCamera = ""
                },
                Application = new ApplicationConfig
                {
                    Language = "de-DE",
                    Theme = "Light",
                    EnableTouchKeyboard = true,
                    EnableDebugMode = false,
                    AutoStartCapture = true,
                    WebServerPort = 5111,
                    EnableRemoteAccess = false,
                    HideExitButton = false,
                    EnableEmergencyTemplates = true
                },
                MwlSettings = new MwlConfig
                {
                    EnableWorklist = false,
                    MwlServerHost = "localhost",
                    MwlServerPort = 105,
                    MwlServerAET = "ORTHANC",
                    AutoRefreshSeconds = 300,
                    ShowEmergencyFirst = true,
                    CacheExpiryHours = 24
                },
                MultiTarget = MultiTargetConfig.CreateDefault()
            };
        }
    }
    
    public class StorageConfig
    {
        public string PhotosPath { get; set; } = "";
        public string VideosPath { get; set; } = "";
        public string DicomPath { get; set; } = "";
        public string QueuePath { get; set; } = "";
        public string TempPath { get; set; } = "";
        public int MaxStorageDays { get; set; } = 30;
        public bool EnableAutoCleanup { get; set; } = false;
    }
    
    public class PacsConfig
    {
        public string ServerHost { get; set; } = "";
        public int ServerPort { get; set; } = 104;
        public string CalledAeTitle { get; set; } = "PACS";
        public string CallingAeTitle { get; set; } = "SMARTBOX";
        public int Timeout { get; set; } = 30;
        public bool EnableTls { get; set; } = false;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelay { get; set; } = 5;
    }
    
    public class VideoConfig
    {
        public string DefaultResolution { get; set; } = "1280x720";
        public int DefaultFrameRate { get; set; } = 30;
        public int DefaultQuality { get; set; } = 85;
        public bool EnableHardwareAcceleration { get; set; } = true;
        public string PreferredCamera { get; set; } = "";
    }
    
    public class ApplicationConfig
    {
        public string Language { get; set; } = "de-DE";
        public string Theme { get; set; } = "Light";
        public bool EnableTouchKeyboard { get; set; } = true;
        public bool EnableDebugMode { get; set; } = false;
        public bool AutoStartCapture { get; set; } = true;
        public int WebServerPort { get; set; } = 5000;
        public bool EnableRemoteAccess { get; set; } = false;
        public bool HideExitButton { get; set; } = false;
        public bool EnableEmergencyTemplates { get; set; } = true;
        public bool AutoExportDicom { get; set; } = false;
    }
}