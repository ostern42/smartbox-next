using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace SmartBoxNext
{
    /// <summary>
    /// Deep camera analysis tool - finds out EVERYTHING about the camera
    /// This is what we should have done from the beginning!
    /// </summary>
    public static class CameraAnalyzer
    {
        public class CameraInfo
        {
            public string Name { get; set; } = "";
            public string Id { get; set; } = "";
            public string Location { get; set; } = "";
            public List<FormatInfo> Formats { get; set; } = new List<FormatInfo>();
            public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        }

        public class FormatInfo
        {
            public string Subtype { get; set; } = "";
            public uint Width { get; set; }
            public uint Height { get; set; }
            public double FrameRate { get; set; }
            public uint Bitrate { get; set; }
            public string Type { get; set; } = "";
            public int Score { get; set; }  // Higher = better for our use case
            
            public override string ToString()
            {
                return $"{Subtype} {Width}x{Height} @ {FrameRate:F1} FPS ({Type})";
            }
        }

        public static async Task<List<CameraInfo>> AnalyzeAllCamerasAsync()
        {
            var cameras = new List<CameraInfo>();
            
            // Get all video capture devices
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            
            foreach (var device in devices)
            {
                var camera = new CameraInfo
                {
                    Name = device.Name,
                    Id = device.Id,
                    Location = device.EnclosureLocation?.Panel.ToString() ?? "Unknown"
                };
                
                // Get device properties
                foreach (var prop in device.Properties)
                {
                    camera.Properties[prop.Key] = prop.Value ?? "null";
                }
                
                // Analyze supported formats
                await AnalyzeCameraFormatsAsync(camera);
                
                cameras.Add(camera);
            }
            
            return cameras;
        }

        private static async Task AnalyzeCameraFormatsAsync(CameraInfo camera)
        {
            MediaCapture? mediaCapture = null;
            
            try
            {
                mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = camera.Id,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };
                
                await mediaCapture.InitializeAsync(settings);
                
                // Get all supported formats for preview
                var previewFormats = mediaCapture.VideoDeviceController
                    .GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview)
                    .OfType<VideoEncodingProperties>()
                    .ToList();
                
                // Get all supported formats for record
                var recordFormats = mediaCapture.VideoDeviceController
                    .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)
                    .OfType<VideoEncodingProperties>()
                    .ToList();
                
                // Combine and analyze
                var allFormats = new HashSet<string>();
                
                foreach (var format in previewFormats)
                {
                    var info = AnalyzeFormat(format, "Preview");
                    var key = $"{info.Subtype}_{info.Width}x{info.Height}_{info.FrameRate}";
                    
                    if (allFormats.Add(key))
                    {
                        camera.Formats.Add(info);
                    }
                }
                
                foreach (var format in recordFormats)
                {
                    var info = AnalyzeFormat(format, "Record");
                    var key = $"{info.Subtype}_{info.Width}x{info.Height}_{info.FrameRate}";
                    
                    if (allFormats.Add(key))
                    {
                        camera.Formats.Add(info);
                    }
                }
                
                // Sort by score (best first)
                camera.Formats = camera.Formats.OrderByDescending(f => f.Score).ToList();
            }
            catch (Exception ex)
            {
                // Add error info
                camera.Properties["Error"] = ex.Message;
            }
            finally
            {
                mediaCapture?.Dispose();
            }
        }

        private static FormatInfo AnalyzeFormat(VideoEncodingProperties format, string type)
        {
            var info = new FormatInfo
            {
                Subtype = format.Subtype,
                Width = format.Width,
                Height = format.Height,
                FrameRate = format.FrameRate?.Numerator > 0 && format.FrameRate?.Denominator > 0 
                    ? (double)format.FrameRate.Numerator / format.FrameRate.Denominator 
                    : 0,
                Bitrate = format.Bitrate,
                Type = type
            };
            
            // Calculate score based on our requirements
            info.Score = CalculateFormatScore(info);
            
            return info;
        }

        private static int CalculateFormatScore(FormatInfo format)
        {
            int score = 0;
            
            // Prefer higher resolution
            if (format.Width >= 1920) score += 1000;
            else if (format.Width >= 1280) score += 500;
            else if (format.Width >= 640) score += 100;
            
            // Prefer higher framerate
            score += (int)(format.FrameRate * 10);
            
            // Format preferences for our use case
            switch (format.Subtype)
            {
                case "NV12":  // Native format for many cameras, GPU friendly
                    score += 500;
                    break;
                case "YUY2":  // Common uncompressed format
                    score += 400;
                    break;
                case "MJPG":  // Compressed but high quality
                    score += 300;
                    break;
                case "RGB24": // Direct RGB, no conversion needed
                    score += 600;
                    break;
                case "RGB32": // Direct RGB with alpha
                    score += 600;
                    break;
            }
            
            // Prefer preview stream (lower latency)
            if (format.Type == "Preview") score += 50;
            
            return score;
        }

        public static string GenerateReport(List<CameraInfo> cameras)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CAMERA HARDWARE ANALYSIS REPORT ===");
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine();
            
            foreach (var camera in cameras)
            {
                sb.AppendLine($"CAMERA: {camera.Name}");
                sb.AppendLine($"ID: {camera.Id}");
                sb.AppendLine($"Location: {camera.Location}");
                sb.AppendLine();
                
                sb.AppendLine("TOP 10 FORMATS (by score):");
                foreach (var format in camera.Formats.Take(10))
                {
                    sb.AppendLine($"  {format} - Score: {format.Score}");
                }
                sb.AppendLine();
                
                // Format statistics
                var formatGroups = camera.Formats.GroupBy(f => f.Subtype);
                sb.AppendLine("FORMAT SUPPORT:");
                foreach (var group in formatGroups)
                {
                    sb.AppendLine($"  {group.Key}: {group.Count()} variations");
                }
                sb.AppendLine();
                
                // Best format for different scenarios
                sb.AppendLine("RECOMMENDED FORMATS:");
                
                var bestHighFps = camera.Formats
                    .Where(f => f.FrameRate >= 30)
                    .OrderByDescending(f => f.Score)
                    .FirstOrDefault();
                if (bestHighFps != null)
                    sb.AppendLine($"  High FPS: {bestHighFps}");
                
                var bestHighRes = camera.Formats
                    .Where(f => f.Width >= 1920)
                    .OrderByDescending(f => f.Score)
                    .FirstOrDefault();
                if (bestHighRes != null)
                    sb.AppendLine($"  High Resolution: {bestHighRes}");
                
                var bestGpuFriendly = camera.Formats
                    .Where(f => f.Subtype == "NV12" || f.Subtype == "RGB32")
                    .OrderByDescending(f => f.Score)
                    .FirstOrDefault();
                if (bestGpuFriendly != null)
                    sb.AppendLine($"  GPU Friendly: {bestGpuFriendly}");
                
                sb.AppendLine();
                sb.AppendLine("DEVICE PROPERTIES:");
                foreach (var prop in camera.Properties.Take(10))
                {
                    sb.AppendLine($"  {prop.Key}: {prop.Value}");
                }
                
                sb.AppendLine();
                sb.AppendLine("=" + new string('=', 50));
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
    }
}