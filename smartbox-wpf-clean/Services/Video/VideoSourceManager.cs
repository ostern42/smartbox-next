using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services.Video
{
    public class VideoSourceManager
    {
        private readonly ILogger<VideoSourceManager> _logger;
        private readonly IConfiguration _config;
        private readonly List<IVideoSource> _sources = new();
        
        public VideoSourceManager(ILogger<VideoSourceManager> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }
        
        public async Task<List<IVideoSource>> EnumerateSources()
        {
            _sources.Clear();
            
            try
            {
                // 1. DirectShow sources (Windows)
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    await EnumerateDirectShowDevices();
                }
                
                // 2. Check for known devices
                await CheckKnownDevices();
                
                // 3. Network sources
                await EnumerateNetworkSources();
                
                // 4. Add test/dummy source for development
                if (_config.GetValue<bool>("SmartBox:VideoEngine:EnableTestSource", false))
                {
                    _sources.Add(new TestVideoSource());
                }
                
                _logger.LogInformation("Found {Count} video sources", _sources.Count);
                
                return _sources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate video sources");
                
                // Return at least a test source if enumeration fails
                return new List<IVideoSource> { new TestVideoSource() };
            }
        }
        
        private async Task EnumerateDirectShowDevices()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = "-list_devices true -f dshow -i dummy",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                // Parse output for video devices
                var videoDevices = ParseDirectShowOutput(output);
                
                foreach (var device in videoDevices)
                {
                    // Check if it's YUAN
                    if (device.Contains("YUAN", StringComparison.OrdinalIgnoreCase) || 
                        device.Contains("SC542", StringComparison.OrdinalIgnoreCase))
                    {
                        _sources.Add(new YuanGrabberSource(device));
                        _logger.LogInformation("Found YUAN medical grabber: {Device}", device);
                    }
                    else
                    {
                        _sources.Add(new DirectShowSource(device));
                        _logger.LogInformation("Found DirectShow device: {Device}", device);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate DirectShow devices");
            }
        }
        
        private List<string> ParseDirectShowOutput(string output)
        {
            var devices = new List<string>();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var inVideoDevices = false;
            
            foreach (var line in lines)
            {
                if (line.Contains("DirectShow video devices"))
                {
                    inVideoDevices = true;
                    continue;
                }
                
                if (line.Contains("DirectShow audio devices"))
                {
                    inVideoDevices = false;
                    continue;
                }
                
                if (inVideoDevices)
                {
                    // Extract device name from lines like:
                    // [dshow @ 0x...] "Integrated Camera"
                    var match = Regex.Match(line, @"\""(.+)\""");
                    if (match.Success)
                    {
                        devices.Add(match.Groups[1].Value);
                    }
                }
            }
            
            return devices;
        }
        
        private async Task CheckKnownDevices()
        {
            // Check for specific known devices by path or identifier
            var knownDevices = _config.GetSection("SmartBox:VideoEngine:KnownDevices").Get<List<KnownDevice>>() ?? new();
            
            foreach (var known in knownDevices)
            {
                try
                {
                    IVideoSource source = known.Type switch
                    {
                        "YUAN" => new YuanGrabberSource(known.Name, known.Path),
                        "Network" => new NetworkCameraSource(known.Name, known.Path),
                        "File" => new FileVideoSource(known.Name, known.Path),
                        _ => null
                    };
                    
                    if (source != null && await source.TestConnection())
                    {
                        _sources.Add(source);
                        _logger.LogInformation("Added known device: {Name}", known.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add known device: {Name}", known.Name);
                }
            }
        }
        
        private async Task EnumerateNetworkSources()
        {
            // Check for network cameras (RTSP, HTTP streams)
            var networkSources = _config.GetSection("SmartBox:VideoEngine:NetworkSources").Get<List<NetworkSource>>() ?? new();
            
            foreach (var netSource in networkSources)
            {
                try
                {
                    var source = new NetworkCameraSource(netSource.Name, netSource.Url);
                    if (await source.TestConnection())
                    {
                        _sources.Add(source);
                        _logger.LogInformation("Added network source: {Name}", netSource.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add network source: {Name}", netSource.Name);
                }
            }
        }
        
        private class KnownDevice
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Path { get; set; }
        }
        
        private class NetworkSource
        {
            public string Name { get; set; }
            public string Url { get; set; }
        }
    }
    
    // YUAN Medical Grabber Implementation
    public class YuanGrabberSource : VideoSourceBase
    {
        private readonly string _deviceName;
        private readonly string _devicePath;
        
        public YuanGrabberSource(string deviceName, string devicePath = null)
        {
            _deviceName = deviceName;
            _devicePath = devicePath;
        }
        
        public override string SourceId => $"yuan_{_deviceName.GetHashCode():X8}";
        public override string DisplayName => "YUAN SC542N6 Medical Capture";
        public override VideoSourceType Type => VideoSourceType.MedicalGrabber;
        
        public override VideoCapabilities Capabilities => new()
        {
            MaxResolution = "1920x1080",
            MaxFrameRate = 60,
            SupportedPixelFormats = new[] { "yuyv422", "nv12", "bgr0" },
            SupportsHardwareEncoding = true,
            Latency = VideoLatency.UltraLow,
            Properties = new Dictionary<string, string>
            {
                ["DeviceModel"] = "SC542N6",
                ["InputTypes"] = "SDI,HDMI,DVI,VGA",
                ["ColorSpace"] = "BT.709",
                ["BitDepth"] = "10-bit"
            }
        };
        
        public override string GetFFmpegInputArgs()
        {
            return $@"-f dshow -video_size 1920x1080 -framerate 60 
                      -pixel_format yuyv422 -rtbufsize 2048M 
                      -i video=""{_deviceName}""";
        }
        
        public override Dictionary<string, string> GetFFmpegOptions()
        {
            return new()
            {
                ["thread_queue_size"] = "1024",
                ["flags"] = "low_delay",
                ["fflags"] = "nobuffer+fastseek",
                ["analyzeduration"] = "0",
                ["probesize"] = "32",
                ["sync"] = "ext"
            };
        }
    }
    
    // Generic DirectShow Source
    public class DirectShowSource : VideoSourceBase
    {
        private readonly string _deviceName;
        
        public DirectShowSource(string deviceName)
        {
            _deviceName = deviceName;
        }
        
        public override string SourceId => $"dshow_{_deviceName.GetHashCode():X8}";
        public override string DisplayName => _deviceName;
        public override VideoSourceType Type => VideoSourceType.Webcam;
        
        public override VideoCapabilities Capabilities => new()
        {
            MaxResolution = "1920x1080",
            MaxFrameRate = 30,
            SupportedPixelFormats = new[] { "yuyv422", "mjpeg", "nv12" },
            SupportsHardwareEncoding = false,
            Latency = VideoLatency.Low
        };
        
        public override string GetFFmpegInputArgs()
        {
            return $@"-f dshow -i video=""{_deviceName}""";
        }
        
        public override Dictionary<string, string> GetFFmpegOptions()
        {
            return new()
            {
                ["thread_queue_size"] = "512",
                ["rtbufsize"] = "1024M"
            };
        }
    }
    
    // Network Camera Source
    public class NetworkCameraSource : VideoSourceBase
    {
        private readonly string _name;
        private readonly string _url;
        
        public NetworkCameraSource(string name, string url)
        {
            _name = name;
            _url = url;
        }
        
        public override string SourceId => $"network_{_url.GetHashCode():X8}";
        public override string DisplayName => _name;
        public override VideoSourceType Type => VideoSourceType.NetworkCamera;
        
        public override VideoCapabilities Capabilities => new()
        {
            MaxResolution = "1920x1080",
            MaxFrameRate = 30,
            SupportedPixelFormats = new[] { "yuv420p", "yuvj420p" },
            SupportsHardwareEncoding = false,
            Latency = VideoLatency.Normal
        };
        
        public override string GetFFmpegInputArgs()
        {
            var protocol = _url.StartsWith("rtsp://") ? "rtsp" : "http";
            return $@"-f {protocol} -i ""{_url}""";
        }
        
        public override Dictionary<string, string> GetFFmpegOptions()
        {
            return new()
            {
                ["rtsp_transport"] = "tcp",
                ["buffer_size"] = "1024000",
                ["max_delay"] = "500000",
                ["reorder_queue_size"] = "1000"
            };
        }
    }
    
    // File Video Source (for testing)
    public class FileVideoSource : VideoSourceBase
    {
        private readonly string _name;
        private readonly string _filePath;
        
        public FileVideoSource(string name, string filePath)
        {
            _name = name;
            _filePath = filePath;
        }
        
        public override string SourceId => $"file_{_filePath.GetHashCode():X8}";
        public override string DisplayName => _name;
        public override VideoSourceType Type => VideoSourceType.File;
        
        public override VideoCapabilities Capabilities => new()
        {
            MaxResolution = "1920x1080",
            MaxFrameRate = 60,
            SupportedPixelFormats = new[] { "yuv420p", "yuv422p", "yuv444p" },
            SupportsHardwareEncoding = false,
            Latency = VideoLatency.High
        };
        
        public override string GetFFmpegInputArgs()
        {
            return $@"-re -i ""{_filePath}"" -loop 1";
        }
        
        public override Dictionary<string, string> GetFFmpegOptions()
        {
            return new();
        }
        
        public override async Task<bool> TestConnection()
        {
            return await Task.FromResult(System.IO.File.Exists(_filePath));
        }
    }
    
    // Test Video Source (generates test pattern)
    public class TestVideoSource : VideoSourceBase
    {
        public override string SourceId => "test_pattern";
        public override string DisplayName => "Test Pattern Generator";
        public override VideoSourceType Type => VideoSourceType.VirtualCamera;
        
        public override VideoCapabilities Capabilities => new()
        {
            MaxResolution = "1920x1080",
            MaxFrameRate = 60,
            SupportedPixelFormats = new[] { "yuv420p", "yuv422p", "rgb24" },
            SupportsHardwareEncoding = false,
            Latency = VideoLatency.UltraLow
        };
        
        public override string GetFFmpegInputArgs()
        {
            return @"-f lavfi -i ""testsrc2=size=1920x1080:rate=60""";
        }
        
        public override Dictionary<string, string> GetFFmpegOptions()
        {
            return new();
        }
        
        public override async Task<bool> TestConnection()
        {
            return await Task.FromResult(true); // Always available
        }
    }
}