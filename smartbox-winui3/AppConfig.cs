using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartBoxNext
{
    public class AppConfig
    {
        private static readonly string ConfigFileName = "config.json";
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        
        // Storage Settings
        public StorageSettings Storage { get; set; } = new StorageSettings();
        
        // PACS Settings
        public PacsConfiguration Pacs { get; set; } = new PacsConfiguration();
        
        // Application Settings
        public ApplicationSettings Application { get; set; } = new ApplicationSettings();
        
        // Video Settings
        public VideoSettings Video { get; set; } = new VideoSettings();
        
        // First Run
        public bool IsFirstRun { get; set; } = true;
        public DateTime? LastConfigUpdate { get; set; }
        
        public class StorageSettings
        {
            public string PhotosPath { get; set; } = "./Data/Photos";
            public string VideosPath { get; set; } = "./Data/Videos";
            public string DicomPath { get; set; } = "./Data/DICOM";
            public string TempPath { get; set; } = "./Data/Temp";
            public string QueuePath { get; set; } = "./Data/Queue";
            public bool UseRelativePaths { get; set; } = true;
            public long MaxStorageSizeMB { get; set; } = 10240; // 10 GB default
            public int RetentionDays { get; set; } = 30;
        }
        
        public class PacsConfiguration
        {
            public string AeTitle { get; set; } = "SMARTBOX";
            public string RemoteAeTitle { get; set; } = "PACS";
            public string RemoteHost { get; set; } = "localhost";
            public int RemotePort { get; set; } = 104;
            public int LocalPort { get; set; } = 0; // 0 = auto
            public int TimeoutSeconds { get; set; } = 30;
            public bool UseTls { get; set; } = false;
        }
        
        public class ApplicationSettings
        {
            public string Language { get; set; } = "en-US";
            public string Theme { get; set; } = "System"; // Light, Dark, System
            public bool ShowDebugInfo { get; set; } = false;
            public bool AutoStartCapture { get; set; } = true;
            public bool MinimizeToTray { get; set; } = true;
            public bool StartWithWindows { get; set; } = false;
            public string DefaultModality { get; set; } = "ES"; // Endoscopy
        }
        
        public class VideoSettings
        {
            public int PreferredWidth { get; set; } = 1920;
            public int PreferredHeight { get; set; } = 1080;
            public int PreferredFps { get; set; } = 60;
            public int VideoBitrateMbps { get; set; } = 5;
            public string VideoFormat { get; set; } = "webm"; // webm, mp4
            public int JpegQuality { get; set; } = 95;
        }
        
        // Load configuration
        public static async Task<AppConfig> LoadAsync()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = await File.ReadAllTextAsync(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, GetJsonOptions());
                    
                    if (config != null)
                    {
                        // Ensure all paths exist
                        await config.EnsureDirectoriesExistAsync();
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash - return defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
            
            // Return default config
            var defaultConfig = new AppConfig();
            await defaultConfig.SaveAsync(); // Save defaults
            await defaultConfig.EnsureDirectoriesExistAsync();
            return defaultConfig;
        }
        
        // Save configuration
        public async Task SaveAsync()
        {
            try
            {
                LastConfigUpdate = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(this, GetJsonOptions());
                await File.WriteAllTextAsync(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
                throw;
            }
        }
        
        // Ensure all directories exist
        private async Task EnsureDirectoriesExistAsync()
        {
            await Task.Run(() =>
            {
                EnsureDirectoryExists(GetFullPath(Storage.PhotosPath));
                EnsureDirectoryExists(GetFullPath(Storage.VideosPath));
                EnsureDirectoryExists(GetFullPath(Storage.DicomPath));
                EnsureDirectoryExists(GetFullPath(Storage.TempPath));
                EnsureDirectoryExists(GetFullPath(Storage.QueuePath));
            });
        }
        
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        
        // Get full path (handle relative paths)
        public string GetFullPath(string path)
        {
            if (Storage.UseRelativePaths && !Path.IsPathRooted(path))
            {
                return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));
            }
            return path;
        }
        
        // Get JSON serialization options
        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }
        
        // Validate configuration
        public (bool IsValid, string[] Errors) Validate()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            // Validate PACS settings
            if (string.IsNullOrWhiteSpace(Pacs.AeTitle))
                errors.Add("PACS AE Title is required");
            if (string.IsNullOrWhiteSpace(Pacs.RemoteHost))
                errors.Add("PACS Remote Host is required");
            if (Pacs.RemotePort <= 0 || Pacs.RemotePort > 65535)
                errors.Add("PACS Remote Port must be between 1 and 65535");
                
            // Validate storage settings
            if (Storage.MaxStorageSizeMB < 100)
                errors.Add("Maximum storage size must be at least 100 MB");
            if (Storage.RetentionDays < 1)
                errors.Add("Retention days must be at least 1");
                
            // Validate video settings
            if (Video.PreferredWidth < 320 || Video.PreferredHeight < 240)
                errors.Add("Video resolution too small");
            if (Video.PreferredFps < 15 || Video.PreferredFps > 120)
                errors.Add("FPS must be between 15 and 120");
            if (Video.JpegQuality < 50 || Video.JpegQuality > 100)
                errors.Add("JPEG quality must be between 50 and 100");
                
            return (errors.Count == 0, errors.ToArray());
        }
    }
}