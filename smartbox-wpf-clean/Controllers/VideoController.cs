using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services.Video;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBoxNext.Controllers
{
    [ApiController]
    [Route("api/video")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoEngine _videoEngine;
        private readonly IConfiguration _config;
        private readonly ILogger<VideoController> _logger;
        
        public VideoController(
            IVideoEngine videoEngine,
            IConfiguration config,
            ILogger<VideoController> logger)
        {
            _videoEngine = videoEngine;
            _config = config;
            _logger = logger;
        }
        
        [HttpGet("sources")]
        public async Task<ActionResult<List<VideoSourceDto>>> GetVideoSources()
        {
            try
            {
                var sources = await _videoEngine.EnumerateSources();
                var currentSource = _videoEngine.GetCurrentSource();
                
                var sourceDtos = new List<VideoSourceDto>();
                
                foreach (var source in sources)
                {
                    var isAvailable = await source.TestConnection();
                    sourceDtos.Add(new VideoSourceDto
                    {
                        Id = source.SourceId,
                        Name = source.DisplayName,
                        Type = source.Type.ToString(),
                        Capabilities = new VideoCapabilitiesDto
                        {
                            MaxResolution = source.Capabilities.MaxResolution,
                            MaxFrameRate = source.Capabilities.MaxFrameRate,
                            SupportedPixelFormats = source.Capabilities.SupportedPixelFormats,
                            SupportsHardwareEncoding = source.Capabilities.SupportsHardwareEncoding,
                            Latency = source.Capabilities.Latency.ToString()
                        },
                        IsAvailable = isAvailable,
                        IsSelected = currentSource?.SourceId == source.SourceId
                    });
                }
                
                return Ok(sourceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate video sources");
                return StatusCode(500, new { error = "Failed to enumerate video sources" });
            }
        }
        
        [HttpPost("sources/{sourceId}/select")]
        public async Task<ActionResult> SelectVideoSource(string sourceId)
        {
            try
            {
                var success = await _videoEngine.SelectSource(sourceId);
                if (!success)
                {
                    return BadRequest(new { error = "Failed to select video source" });
                }
                
                return Ok(new { message = "Video source selected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select video source {SourceId}", sourceId);
                return StatusCode(500, new { error = "Failed to select video source" });
            }
        }
        
        [HttpPost("recording/start")]
        public async Task<ActionResult<RecordingStartDto>> StartRecording([FromBody] StartRecordingRequest request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrEmpty(request.PatientId))
                    return BadRequest(new { error = "PatientId is required" });
                
                // Build configuration
                var config = new RecordingConfig
                {
                    PatientId = request.PatientId,
                    StudyId = request.StudyId ?? Guid.NewGuid().ToString(),
                    
                    // Video settings
                    MasterCodec = Enum.TryParse<VideoCodec>(request.MasterCodec, out var codec) 
                        ? codec : VideoCodec.FFV1,
                    Resolution = request.Resolution ?? "1920x1080",
                    FrameRate = request.FrameRate ?? 60,
                    PixelFormat = request.PixelFormat ?? "yuv422p",
                    
                    // Pre-recording
                    PreRecordSeconds = request.PreRecordSeconds ?? 60,
                    
                    // Preview
                    EnablePreview = request.EnablePreview ?? true,
                    PreviewBitrate = request.PreviewBitrate ?? 5000,
                    
                    // Storage
                    OutputDirectory = Path.Combine(
                        _config["SmartBox:Storage:VideoPath"] ?? "D:\\SmartBoxRecordings", 
                        request.PatientId, 
                        DateTime.Now.ToString("yyyyMMdd"))
                };
                
                // Start recording
                var session = await _videoEngine.StartRecording(config);
                
                // Get preview stream info
                var streamInfo = await _videoEngine.GetPreviewStream(session.SessionId);
                
                // Return session info
                return Ok(new RecordingStartDto
                {
                    SessionId = session.SessionId,
                    PreviewUrl = streamInfo?.StreamUrl ?? $"/api/video/preview/{session.SessionId}/stream.m3u8",
                    WebSocketUrl = $"ws://{Request.Host}/ws/video/{session.SessionId}",
                    Status = session.Status.ToString(),
                    StartTime = session.StartTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start recording");
                return StatusCode(500, new { error = "Failed to start recording: " + ex.Message });
            }
        }
        
        [HttpPost("recording/{sessionId}/stop")]
        public async Task<ActionResult<RecordingStopDto>> StopRecording(string sessionId)
        {
            try
            {
                var session = _videoEngine.GetSession(sessionId);
                if (session == null)
                    return NotFound(new { error = "Recording session not found" });
                
                var success = await _videoEngine.StopRecording(sessionId);
                if (!success)
                    return BadRequest(new { error = "Failed to stop recording" });
                
                // Get final session info
                session = _videoEngine.GetSession(sessionId);
                var segments = await _videoEngine.GetEditableSegments(sessionId);
                
                return Ok(new RecordingStopDto
                {
                    SessionId = sessionId,
                    Duration = session.EndTime.HasValue 
                        ? (session.EndTime.Value - session.StartTime).TotalSeconds 
                        : 0,
                    SegmentCount = segments.Count,
                    TotalSize = segments.Sum(s => s.FileSize),
                    OutputPath = session.OutputPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop recording {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to stop recording" });
            }
        }
        
        [HttpPost("recording/{sessionId}/pause")]
        public async Task<ActionResult> PauseRecording(string sessionId)
        {
            try
            {
                var success = await _videoEngine.PauseRecording(sessionId);
                if (!success)
                    return BadRequest(new { error = "Failed to pause recording" });
                
                return Ok(new { message = "Recording paused" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pause recording {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to pause recording" });
            }
        }
        
        [HttpPost("recording/{sessionId}/resume")]
        public async Task<ActionResult> ResumeRecording(string sessionId)
        {
            try
            {
                var success = await _videoEngine.ResumeRecording(sessionId);
                if (!success)
                    return BadRequest(new { error = "Failed to resume recording" });
                
                return Ok(new { message = "Recording resumed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resume recording {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to resume recording" });
            }
        }
        
        [HttpGet("recording/{sessionId}/status")]
        public ActionResult<RecordingStatusDto> GetRecordingStatus(string sessionId)
        {
            try
            {
                var session = _videoEngine.GetSession(sessionId);
                if (session == null)
                    return NotFound(new { error = "Recording session not found" });
                
                var status = _videoEngine.GetStatus(sessionId);
                var preRecordStats = _videoEngine.GetPreRecordStatus().Result;
                
                return Ok(new RecordingStatusDto
                {
                    SessionId = sessionId,
                    Status = status.ToString(),
                    StartTime = session.StartTime,
                    Duration = (DateTime.UtcNow - session.StartTime).TotalSeconds,
                    SegmentCount = session.Segments.Count,
                    PreRecordingEnabled = preRecordStats.Enabled,
                    PreRecordingSeconds = preRecordStats.CurrentSeconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recording status {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to get recording status" });
            }
        }
        
        [HttpGet("preview/{sessionId}/{*filename}")]
        public async Task<IActionResult> GetPreviewFile(string sessionId, string filename)
        {
            try
            {
                var session = _videoEngine.GetSession(sessionId);
                if (session == null)
                    return NotFound();
                
                var filePath = Path.Combine(session.OutputPath, "preview", filename);
                if (!System.IO.File.Exists(filePath))
                    return NotFound();
                
                var contentType = filename.EndsWith(".m3u8") 
                    ? "application/vnd.apple.mpegurl" 
                    : "video/MP2T";
                
                Response.Headers.Add("Cache-Control", "no-cache");
                return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get preview file {SessionId}/{Filename}", sessionId, filename);
                return StatusCode(500);
            }
        }
        
        [HttpGet("segments/{sessionId}")]
        public async Task<ActionResult<List<SegmentDto>>> GetSegments(string sessionId)
        {
            try
            {
                var segments = await _videoEngine.GetEditableSegments(sessionId);
                
                return Ok(segments.Select(s => new SegmentDto
                {
                    SegmentNumber = s.Number,
                    FileName = s.FileName,
                    StartTime = s.StartTime.TotalSeconds,
                    Duration = s.Duration.TotalSeconds,
                    FileSize = s.FileSize,
                    IsComplete = s.IsComplete,
                    CanEdit = s.IsComplete && !s.IsLocked,
                    CreatedAt = s.CreatedAt
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get segments {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to get segments" });
            }
        }
        
        [HttpGet("thumbnail/{sessionId}/{timestamp}")]
        public async Task<IActionResult> GetThumbnail(string sessionId, double timestamp, [FromQuery] int width = 160)
        {
            try
            {
                var thumbnailData = await _videoEngine.GetThumbnail(
                    sessionId, 
                    TimeSpan.FromSeconds(timestamp), 
                    width);
                
                if (thumbnailData == null || thumbnailData.Length == 0)
                    return NotFound();
                
                return File(thumbnailData, "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get thumbnail {SessionId} at {Timestamp}", sessionId, timestamp);
                return StatusCode(500);
            }
        }
        
        [HttpPost("snapshot/{sessionId}")]
        public async Task<ActionResult<SnapshotDto>> TakeSnapshot(string sessionId, [FromQuery] string format = "JPEG")
        {
            try
            {
                if (!Enum.TryParse<ImageFormat>(format, true, out var imageFormat))
                    imageFormat = ImageFormat.JPEG;
                
                var filename = await _videoEngine.TakeSnapshot(sessionId, imageFormat);
                
                return Ok(new SnapshotDto
                {
                    FileName = filename,
                    Timestamp = DateTime.UtcNow,
                    Format = imageFormat.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to take snapshot {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to take snapshot" });
            }
        }
        
        [HttpPost("marker/{sessionId}")]
        public async Task<ActionResult> AddMarker(string sessionId, [FromBody] AddMarkerRequest request)
        {
            try
            {
                if (!Enum.TryParse<MarkerType>(request.Type, true, out var markerType))
                    markerType = MarkerType.Generic;
                
                var success = await _videoEngine.AddMarker(
                    sessionId, 
                    TimeSpan.FromSeconds(request.Timestamp), 
                    markerType);
                
                if (!success)
                    return BadRequest(new { error = "Failed to add marker" });
                
                return Ok(new { message = "Marker added successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add marker {SessionId}", sessionId);
                return StatusCode(500, new { error = "Failed to add marker" });
            }
        }
    }
    
    // DTOs
    public class VideoSourceDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public VideoCapabilitiesDto Capabilities { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsSelected { get; set; }
    }
    
    public class VideoCapabilitiesDto
    {
        public string MaxResolution { get; set; }
        public int MaxFrameRate { get; set; }
        public string[] SupportedPixelFormats { get; set; }
        public bool SupportsHardwareEncoding { get; set; }
        public string Latency { get; set; }
    }
    
    public class StartRecordingRequest
    {
        public string PatientId { get; set; }
        public string StudyId { get; set; }
        public string MasterCodec { get; set; }
        public string Resolution { get; set; }
        public int? FrameRate { get; set; }
        public string PixelFormat { get; set; }
        public int? PreRecordSeconds { get; set; }
        public bool? EnablePreview { get; set; }
        public int? PreviewBitrate { get; set; }
    }
    
    public class RecordingStartDto
    {
        public string SessionId { get; set; }
        public string PreviewUrl { get; set; }
        public string WebSocketUrl { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
    }
    
    public class RecordingStopDto
    {
        public string SessionId { get; set; }
        public double Duration { get; set; }
        public int SegmentCount { get; set; }
        public long TotalSize { get; set; }
        public string OutputPath { get; set; }
    }
    
    public class RecordingStatusDto
    {
        public string SessionId { get; set; }
        public string Status { get; set; }
        public DateTime StartTime { get; set; }
        public double Duration { get; set; }
        public int SegmentCount { get; set; }
        public bool PreRecordingEnabled { get; set; }
        public int PreRecordingSeconds { get; set; }
    }
    
    public class SegmentDto
    {
        public int SegmentNumber { get; set; }
        public string FileName { get; set; }
        public double StartTime { get; set; }
        public double Duration { get; set; }
        public long FileSize { get; set; }
        public bool IsComplete { get; set; }
        public bool CanEdit { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class SnapshotDto
    {
        public string FileName { get; set; }
        public DateTime Timestamp { get; set; }
        public string Format { get; set; }
    }
    
    public class AddMarkerRequest
    {
        public double Timestamp { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
    }
}