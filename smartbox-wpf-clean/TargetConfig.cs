using System.Collections.Generic;

namespace SmartBoxNext
{
    /// <summary>
    /// Configuration for multi-target export architecture
    /// </summary>
    public class TargetConfig
    {
        public string Type { get; set; } = "C-STORE"; // C-STORE, FTP, FileShare, HTTP
        public string Name { get; set; } = "";
        public int Priority { get; set; } = 1;
        public List<string> Rules { get; set; } = new List<string> { "*" };
        public bool Enabled { get; set; } = true;
        
        // Connection settings
        public string Host { get; set; } = "";
        public int Port { get; set; } = 104;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        
        // DICOM specific
        public string CalledAeTitle { get; set; } = "";
        public string CallingAeTitle { get; set; } = "SMARTBOX";
        
        // File share specific
        public string Path { get; set; } = "";
        
        // Retry settings
        public int MaxRetries { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;
        public int TimeoutSeconds { get; set; } = 30;
        
        /// <summary>
        /// Check if this target should handle the given context
        /// </summary>
        public bool ShouldHandle(string context)
        {
            if (!Enabled) return false;
            
            foreach (var rule in Rules)
            {
                if (rule == "*") return true;
                if (rule == context) return true;
                if (context.Contains(rule)) return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Extended configuration with multi-target support
    /// </summary>
    public class MultiTargetConfig
    {
        public List<TargetConfig> Targets { get; set; } = new List<TargetConfig>();
        
        /// <summary>
        /// Create default multi-target configuration
        /// </summary>
        public static MultiTargetConfig CreateDefault()
        {
            return new MultiTargetConfig
            {
                Targets = new List<TargetConfig>
                {
                    new TargetConfig
                    {
                        Type = "C-STORE",
                        Name = "Primary PACS",
                        Host = "localhost",
                        Port = 104,
                        CalledAeTitle = "PACS",
                        Priority = 1,
                        Rules = new List<string> { "*" }
                    },
                    new TargetConfig
                    {
                        Type = "FileShare",
                        Name = "Local Backup",
                        Path = @".\Data\Backup",
                        Priority = 2,
                        Rules = new List<string> { "Always" },
                        Enabled = false
                    }
                }
            };
        }
    }
}