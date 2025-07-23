using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartBoxNext.Services.Video
{
    public interface IVideoSource
    {
        string SourceId { get; }
        string DisplayName { get; }
        VideoSourceType Type { get; }
        VideoCapabilities Capabilities { get; }
        
        Task<bool> TestConnection();
        string GetFFmpegInputArgs();
        Dictionary<string, string> GetFFmpegOptions();
    }

    public class VideoCapabilities
    {
        public string MaxResolution { get; set; }
        public int MaxFrameRate { get; set; }
        public string[] SupportedPixelFormats { get; set; }
        public bool SupportsHardwareEncoding { get; set; }
        public VideoLatency Latency { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    public enum VideoSourceType
    {
        Unknown,
        Webcam,
        MedicalGrabber,
        NetworkCamera,
        VirtualCamera,
        ScreenCapture,
        File
    }

    public enum VideoLatency
    {
        UltraLow,   // < 50ms
        Low,        // < 100ms
        Normal,     // < 500ms
        High        // > 500ms
    }

    public abstract class VideoSourceBase : IVideoSource
    {
        public abstract string SourceId { get; }
        public abstract string DisplayName { get; }
        public abstract VideoSourceType Type { get; }
        public abstract VideoCapabilities Capabilities { get; }

        public virtual async Task<bool> TestConnection()
        {
            try
            {
                // Basic test using FFmpeg
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"{GetFFmpegInputArgs()} -t 0.1 -f null -",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public abstract string GetFFmpegInputArgs();
        public abstract Dictionary<string, string> GetFFmpegOptions();
    }
}