using System;
using System.IO;
using System.Runtime.InteropServices;
using FFMpegCore;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Service for FFmpeg binary management and configuration
    /// Based on research: FFmpeg.Native provides LGPL-compliant binaries
    /// </summary>
    public class FFmpegService
    {
        private readonly ILogger _logger;
        private bool _isConfigured = false;
        
        public FFmpegService(ILogger logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Configure FFmpeg binaries based on architecture
        /// Uses FFmpeg.Native package binaries
        /// </summary>
        public void ConfigureFFmpeg()
        {
            if (_isConfigured) return;
            
            try
            {
                // Determine architecture
                string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                _logger.LogInformation($"Configuring FFmpeg for {architecture} architecture");
                
                // Build path to binaries
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string binaryPath = Path.Combine(basePath, "runtimes", $"win-{architecture}", "native");
                
                // Check if binaries exist in expected location
                if (!Directory.Exists(binaryPath))
                {
                    // Fallback to bin directory structure
                    binaryPath = Path.Combine(basePath, "bin", architecture);
                    
                    if (!Directory.Exists(binaryPath))
                    {
                        // Create directory and log warning
                        Directory.CreateDirectory(binaryPath);
                        _logger.LogWarning($"FFmpeg binary directory not found. Please ensure FFmpeg.Native package is installed correctly.");
                    }
                }
                
                // Configure FFMpegCore with binary path
                GlobalFFOptions.Configure(new FFOptions 
                { 
                    BinaryFolder = binaryPath,
                    TemporaryFilesFolder = Path.GetTempPath(),
                    LogLevel = FFMpegCore.Arguments.FFMpegLogLevel.Warning
                });
                
                // Verify FFmpeg is available
                try
                {
                    var version = FFMpegArguments.GetVersion();
                    _logger.LogInformation($"FFmpeg configured successfully. Version: {version}");
                    _isConfigured = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"FFmpeg verification failed: {ex.Message}");
                    throw new InvalidOperationException("FFmpeg binaries not found. Please ensure FFmpeg.Native package is installed.", ex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to configure FFmpeg: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Check if FFmpeg is properly configured and available
        /// </summary>
        public bool IsFFmpegAvailable()
        {
            try
            {
                if (!_isConfigured)
                {
                    ConfigureFFmpeg();
                }
                
                // Try to get version as availability check
                var version = FFMpegArguments.GetVersion();
                return !string.IsNullOrEmpty(version);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get FFmpeg configuration info for diagnostics
        /// </summary>
        public FFmpegInfo GetFFmpegInfo()
        {
            var info = new FFmpegInfo
            {
                IsAvailable = IsFFmpegAvailable(),
                Architecture = Environment.Is64BitProcess ? "x64" : "x86",
                BinaryPath = GlobalFFOptions.Current.BinaryFolder
            };
            
            try
            {
                if (info.IsAvailable)
                {
                    info.Version = FFMpegArguments.GetVersion();
                }
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }
            
            return info;
        }
    }
    
    /// <summary>
    /// FFmpeg configuration information
    /// </summary>
    public class FFmpegInfo
    {
        public bool IsAvailable { get; set; }
        public string Architecture { get; set; } = "";
        public string BinaryPath { get; set; } = "";
        public string Version { get; set; } = "";
        public string Error { get; set; } = "";
    }
}