# SmartBox Medical Video System - Detailed Implementation Plan

## Executive Summary
Transform the current browser-based WebM recording into a professional medical-grade video capture system using FFmpeg backend with lossless recording, segmented storage, and real-time preview. This system must support the YUAN SC542N6 medical capture device while maintaining the existing elegant HTML/JavaScript UI.

## Current State Analysis

### Existing Implementation
- **Recording**: Browser MediaRecorder API using WebM with VP8 codec (lossy compression)
- **Storage**: Video blobs kept in JavaScript memory, no file persistence
- **Preview**: Same compressed stream used for both recording and preview
- **Timeline**: Already implemented with intelligent thumbnail recycling (Knuth-inspired)
- **UI**: Elegant touch-optimized interface with jogwheel and transport controls
- **Export**: UI exists but backend implementation is missing

### Technical Debt
- No lossless codec support in browser
- Cannot edit while recording (file locked)
- No YUAN grabber integration
- Missing FFmpeg backend
- No DICOM export capability

## Detailed Architecture

### System Overview
```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│   HTML/JS GUI   │────▶│  C# Web API      │────▶│ FFmpeg Engine   │
│  (Unchanged)    │◀────│  VideoController  │◀────│ (Background)    │
└─────────────────┘     └──────────────────┘     └─────────────────┘
         ↑                       ↑                         ↓
         │                       │                   ┌─────────────┐
    WebSocket               Settings.json            │ File System │
    HLS Preview             Configuration            │  Segments   │
                                                    └─────────────┘
```

### Data Flow
```
Video Input (YUAN/Webcam)
    ↓
FFmpeg Process
    ├── Pre-Record Buffer (RAM, 60s circular)
    ├── Segment Writer (Disk, 10s chunks)
    └── Preview Encoder (HLS stream)
         ↓
    Browser (existing UI)
```

## Detailed Implementation Plan

### Phase 1: Core Video Engine (Days 1-3)

#### 1.1 Video Engine Interfaces
```csharp
// IVideoSource.cs
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

// IVideoEngine.cs
public interface IVideoEngine
{
    // Source Management
    Task<List<IVideoSource>> EnumerateSources();
    Task<bool> SelectSource(string sourceId);
    IVideoSource GetCurrentSource();
    
    // Recording Control
    Task<RecordingSession> StartRecording(RecordingConfig config);
    Task<bool> StopRecording(string sessionId);
    Task<bool> PauseRecording(string sessionId);
    Task<bool> ResumeRecording(string sessionId);
    
    // Pre-Recording Buffer
    Task<bool> EnablePreRecording(int seconds);
    Task<PreRecordStats> GetPreRecordStatus();
    
    // Live Operations
    Task<string> TakeSnapshot(string sessionId, ImageFormat format);
    Task<bool> AddMarker(string sessionId, TimeSpan timestamp, MarkerType type);
    Task<List<VideoSegment>> GetEditableSegments(string sessionId);
    
    // Preview & Monitoring
    Task<StreamInfo> GetPreviewStream(string sessionId);
    Task<byte[]> GetThumbnail(string sessionId, TimeSpan timestamp, int width);
    RecordingStatus GetStatus(string sessionId);
    
    // Events
    event EventHandler<RecordingEventArgs> RecordingEvent;
    event EventHandler<SegmentEventArgs> SegmentCompleted;
    event EventHandler<ErrorEventArgs> Error;
}
```

#### 1.2 FFmpeg Process Management
```csharp
// FFmpegEngine.cs
public class FFmpegEngine : IVideoEngine
{
    private readonly ConcurrentDictionary<string, FFmpegSession> _sessions;
    private readonly IConfiguration _config;
    private readonly ILogger<FFmpegEngine> _logger;
    
    public async Task<RecordingSession> StartRecording(RecordingConfig config)
    {
        var session = new RecordingSession
        {
            SessionId = GenerateSessionId(),
            Config = config,
            StartTime = DateTime.UtcNow,
            Status = RecordingStatus.Starting
        };
        
        // Create session directory
        var sessionPath = Path.Combine(_config["Storage:TempPath"], session.SessionId);
        Directory.CreateDirectory(sessionPath);
        Directory.CreateDirectory(Path.Combine(sessionPath, "preview"));
        
        // Build FFmpeg command
        var ffmpegArgs = BuildFFmpegArgs(session, sessionPath);
        
        // Start FFmpeg process
        var process = await StartFFmpegProcess(ffmpegArgs);
        
        // Start monitoring
        var ffmpegSession = new FFmpegSession
        {
            Process = process,
            Session = session,
            OutputPath = sessionPath
        };
        
        _sessions[session.SessionId] = ffmpegSession;
        StartMonitoring(ffmpegSession);
        
        return session;
    }
    
    private string BuildFFmpegArgs(RecordingSession session, string outputPath)
    {
        var source = GetCurrentSource();
        var inputArgs = source.GetFFmpegInputArgs();
        var inputOptions = source.GetFFmpegOptions();
        
        // Build complex filter for multiple outputs
        var filterComplex = @"[0:v]split=3[main][preview][thumb];
            [main]null[main_out];
            [preview]scale='min(1280,iw)':'-1'[preview_out];
            [thumb]fps=1,scale=160:-1[thumb_out]";
        
        return $@"
            {inputArgs}
            {string.Join(" ", inputOptions.Select(kv => $"-{kv.Key} {kv.Value}"))}
            -filter_complex ""{filterComplex}""
            
            # Master recording (lossless segments)
            -map '[main_out]' 
            -c:v {GetCodecString(session.Config.MasterCodec)}
            -f segment -segment_time 10 -segment_format matroska
            -segment_list ""{Path.Combine(outputPath, "segments.csv")}""
            -segment_list_type csv -segment_list_flags +live
            -reset_timestamps 1
            ""{Path.Combine(outputPath, "segment_%05d.mkv")}""
            
            # Preview stream (HLS)
            -map '[preview_out]'
            -c:v libx264 -preset veryfast -tune zerolatency
            -b:v {session.Config.PreviewBitrate}k -g 60
            -f hls -hls_time 2 -hls_list_size 5 -hls_flags delete_segments
            -hls_segment_filename ""{Path.Combine(outputPath, "preview", "chunk_%05d.ts")}""
            ""{Path.Combine(outputPath, "preview", "stream.m3u8")}""
            
            # Thumbnail output
            -map '[thumb_out]'
            -f image2 -update 1
            ""{Path.Combine(outputPath, "latest_thumb.jpg")}""
        ";
    }
}
```

#### 1.3 Pre-Recording Buffer
```csharp
// PreRecordingBuffer.cs
public class PreRecordingBuffer : IDisposable
{
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly int _bufferSize;
    private readonly int _frameSize;
    private readonly object _lock = new();
    
    private long _writePosition;
    private long _totalFramesWritten;
    private readonly Queue<FrameMetadata> _frameIndex;
    
    public PreRecordingBuffer(int seconds, int fps, int width, int height)
    {
        // Calculate buffer size (YUV422 = 2 bytes per pixel)
        _frameSize = width * height * 2;
        var totalFrames = seconds * fps;
        _bufferSize = totalFrames * _frameSize;
        
        // Create memory-mapped file
        _mmf = MemoryMappedFile.CreateNew($"PreRecord_{Guid.NewGuid()}", _bufferSize);
        _accessor = _mmf.CreateViewAccessor();
        
        _frameIndex = new Queue<FrameMetadata>(totalFrames);
    }
    
    public void WriteFrame(byte[] frameData, TimeSpan timestamp)
    {
        lock (_lock)
        {
            // Write frame data
            _accessor.WriteArray(_writePosition, frameData, 0, frameData.Length);
            
            // Update index
            var metadata = new FrameMetadata
            {
                Position = _writePosition,
                Timestamp = timestamp,
                FrameNumber = _totalFramesWritten++
            };
            
            _frameIndex.Enqueue(metadata);
            
            // Remove oldest frame if buffer is full
            if (_frameIndex.Count > MaxFrames)
            {
                _frameIndex.Dequeue();
            }
            
            // Circular write
            _writePosition = (_writePosition + _frameSize) % _bufferSize;
        }
    }
    
    public async Task DumpToFile(string outputPath)
    {
        lock (_lock)
        {
            // Write all frames from oldest to newest
            using var output = File.OpenWrite(outputPath);
            
            foreach (var frame in _frameIndex.OrderBy(f => f.FrameNumber))
            {
                var buffer = new byte[_frameSize];
                _accessor.ReadArray(frame.Position, buffer, 0, _frameSize);
                await output.WriteAsync(buffer, 0, buffer.Length);
            }
        }
    }
}
```

### Phase 2: Video Source Implementations (Days 3-4)

#### 2.1 Source Detection and Management
```csharp
// VideoSourceManager.cs
public class VideoSourceManager
{
    private readonly List<IVideoSource> _sources = new();
    
    public async Task<List<IVideoSource>> EnumerateSources()
    {
        _sources.Clear();
        
        // 1. DirectShow sources (Windows)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await EnumerateDirectShowDevices();
        }
        
        // 2. Check for known devices
        await CheckKnownDevices();
        
        // 3. Network sources
        await EnumerateNetworkSources();
        
        return _sources;
    }
    
    private async Task EnumerateDirectShowDevices()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-list_devices true -f dshow -i dummy",
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        
        process.Start();
        var output = await process.StandardError.ReadToEndAsync();
        
        // Parse output for video devices
        var videoDevices = ParseDirectShowOutput(output);
        
        foreach (var device in videoDevices)
        {
            // Check if it's YUAN
            if (device.Contains("YUAN") || device.Contains("SC542"))
            {
                _sources.Add(new YuanGrabberSource(device));
            }
            else
            {
                _sources.Add(new DirectShowSource(device));
            }
        }
    }
}

// YuanGrabberSource.cs
public class YuanGrabberSource : IVideoSource
{
    public string SourceId => $"yuan_{DeviceName.GetHashCode()}";
    public string DisplayName => "YUAN SC542N6 Medical Capture";
    public VideoSourceType Type => VideoSourceType.MedicalGrabber;
    
    public VideoCapabilities Capabilities => new()
    {
        MaxResolution = "1920x1080",
        MaxFrameRate = 60,
        SupportedPixelFormats = new[] { "yuyv422", "nv12" },
        SupportsHardwareEncoding = true,
        Latency = VideoLatency.UltraLow
    };
    
    public string GetFFmpegInputArgs()
    {
        return $@"-f dshow -video_size 1920x1080 -framerate 60 
                  -pixel_format yuyv422 -rtbufsize 2048M 
                  -i video=""{DeviceName}""";
    }
    
    public Dictionary<string, string> GetFFmpegOptions()
    {
        return new()
        {
            ["thread_queue_size"] = "512",
            ["flags"] = "low_delay",
            ["fflags"] = "nobuffer+fastseek",
            ["analyzeduration"] = "0",
            ["probesize"] = "32"
        };
    }
}
```

### Phase 3: Web API Integration (Days 4-5)

#### 3.1 API Controller
```csharp
[ApiController]
[Route("api/video")]
public class VideoController : ControllerBase
{
    private readonly IVideoEngine _videoEngine;
    private readonly IVideoSourceManager _sourceManager;
    private readonly IConfiguration _config;
    
    [HttpGet("sources")]
    public async Task<ActionResult<List<VideoSourceDto>>> GetVideoSources()
    {
        var sources = await _sourceManager.EnumerateSources();
        return Ok(sources.Select(s => new VideoSourceDto
        {
            Id = s.SourceId,
            Name = s.DisplayName,
            Type = s.Type.ToString(),
            Capabilities = new
            {
                s.Capabilities.MaxResolution,
                s.Capabilities.MaxFrameRate,
                s.Capabilities.SupportedPixelFormats,
                s.Capabilities.SupportsHardwareEncoding
            },
            IsAvailable = s.TestConnection().Result
        }));
    }
    
    [HttpPost("recording/start")]
    public async Task<ActionResult<RecordingStartDto>> StartRecording([FromBody] StartRecordingRequest request)
    {
        // Validate request
        if (string.IsNullOrEmpty(request.PatientId))
            return BadRequest("PatientId is required");
        
        // Build configuration
        var config = new RecordingConfig
        {
            PatientId = request.PatientId,
            StudyId = request.StudyId ?? Guid.NewGuid().ToString(),
            
            // Video settings
            MasterCodec = Enum.Parse<VideoCodec>(request.MasterCodec ?? "FFV1"),
            Resolution = request.Resolution ?? "1920x1080",
            FrameRate = request.FrameRate ?? 60,
            PixelFormat = request.PixelFormat ?? "yuv422p",
            
            // Pre-recording
            PreRecordSeconds = request.PreRecordSeconds ?? 60,
            
            // Storage
            OutputDirectory = Path.Combine(_config["Storage:RecordingPath"], 
                request.PatientId, DateTime.Now.ToString("yyyyMMdd"))
        };
        
        // Start recording
        var session = await _videoEngine.StartRecording(config);
        
        // Return session info
        return Ok(new RecordingStartDto
        {
            SessionId = session.SessionId,
            PreviewUrl = $"/api/video/preview/{session.SessionId}/stream.m3u8",
            WebSocketUrl = $"ws://{Request.Host}/ws/video/{session.SessionId}",
            Status = "recording"
        });
    }
    
    [HttpGet("preview/{sessionId}/{filename}")]
    public async Task<IActionResult> GetPreviewFile(string sessionId, string filename)
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
        
        return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
    }
    
    [HttpGet("segments/{sessionId}")]
    public async Task<ActionResult<List<SegmentDto>>> GetSegments(string sessionId)
    {
        var segments = await _videoEngine.GetEditableSegments(sessionId);
        
        return Ok(segments.Select(s => new SegmentDto
        {
            SegmentNumber = s.Number,
            FileName = s.FileName,
            StartTime = s.StartTime,
            Duration = s.Duration,
            FileSize = s.FileSize,
            IsComplete = s.IsComplete,
            CanEdit = s.IsComplete && !s.IsLocked
        }));
    }
}
```

#### 3.2 WebSocket Handler
```csharp
// VideoWebSocketHandler.cs
public class VideoWebSocketHandler
{
    private readonly IVideoEngine _videoEngine;
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
    
    public async Task HandleConnection(HttpContext context, WebSocket webSocket, string sessionId)
    {
        _sockets[sessionId] = webSocket;
        
        // Subscribe to events
        _videoEngine.RecordingEvent += async (s, e) => 
        {
            if (e.SessionId == sessionId)
                await SendUpdate(sessionId, e);
        };
        
        // Keep connection alive
        var buffer = new byte[1024 * 4];
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
            
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await HandleMessage(sessionId, message);
            }
        }
        
        _sockets.TryRemove(sessionId, out _);
    }
    
    private async Task SendUpdate(string sessionId, RecordingEventArgs args)
    {
        if (_sockets.TryGetValue(sessionId, out var socket))
        {
            var update = new
            {
                type = args.EventType.ToString(),
                timestamp = args.Timestamp,
                data = args.Data
            };
            
            var json = JsonSerializer.Serialize(update);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            await socket.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
    }
}
```

### Phase 4: Frontend Integration (Days 5-6)

#### 4.1 Video Engine Client
```javascript
// video-engine-client.js
class VideoEngineClient {
    constructor() {
        this.baseUrl = '/api/video';
        this.currentSession = null;
        this.ws = null;
        this.previewPlayer = null;
    }
    
    async initialize() {
        // Get available sources
        const sources = await this.getVideoSources();
        
        // Auto-select best source
        const preferredSource = sources.find(s => s.type === 'MedicalGrabber') ||
                               sources.find(s => s.type === 'Webcam') ||
                               sources[0];
        
        if (preferredSource) {
            await this.selectSource(preferredSource.id);
        }
        
        return preferredSource;
    }
    
    async getVideoSources() {
        const response = await fetch(`${this.baseUrl}/sources`);
        if (!response.ok) throw new Error('Failed to get video sources');
        return await response.json();
    }
    
    async startRecording(config) {
        const response = await fetch(`${this.baseUrl}/recording/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                patientId: config.patientId,
                studyId: config.studyId,
                masterCodec: config.lossless ? 'FFV1' : 'H264',
                resolution: config.resolution || '1920x1080',
                frameRate: config.frameRate || 60,
                pixelFormat: config.pixelFormat || 'yuv422p',
                preRecordSeconds: config.preRecordSeconds || 60
            })
        });
        
        if (!response.ok) throw new Error('Failed to start recording');
        
        const session = await response.json();
        this.currentSession = session;
        
        // Setup preview player
        await this.setupPreview(session.previewUrl);
        
        // Connect WebSocket for real-time updates
        this.connectWebSocket(session.webSocketUrl);
        
        return session;
    }
    
    async setupPreview(previewUrl) {
        const video = document.getElementById('webcamPreviewLarge');
        
        if (Hls.isSupported()) {
            if (this.previewPlayer) {
                this.previewPlayer.destroy();
            }
            
            this.previewPlayer = new Hls({
                enableWorker: true,
                lowLatencyMode: true,
                liveBackBufferLength: 0,
                liveSyncDuration: 0,
                liveMaxLatencyDuration: 5,
                liveDurationInfinity: true,
                highBufferWatchdogPeriod: 1
            });
            
            this.previewPlayer.loadSource(previewUrl);
            this.previewPlayer.attachMedia(video);
            
            this.previewPlayer.on(Hls.Events.MANIFEST_PARSED, () => {
                video.play();
            });
        }
    }
    
    connectWebSocket(url) {
        this.ws = new WebSocket(url);
        
        this.ws.onopen = () => {
            console.log('Video WebSocket connected');
        };
        
        this.ws.onmessage = (event) => {
            const data = JSON.parse(event.data);
            this.handleWebSocketMessage(data);
        };
        
        this.ws.onerror = (error) => {
            console.error('Video WebSocket error:', error);
        };
    }
    
    handleWebSocketMessage(data) {
        switch (data.type) {
            case 'SegmentCompleted':
                this.onSegmentCompleted(data.data);
                break;
            case 'RecordingStatus':
                this.onStatusUpdate(data.data);
                break;
            case 'ThumbnailReady':
                this.onThumbnailReady(data.data);
                break;
            case 'Error':
                this.onError(data.data);
                break;
        }
    }
    
    async getEditableSegments() {
        if (!this.currentSession) return [];
        
        const response = await fetch(`${this.baseUrl}/segments/${this.currentSession.sessionId}`);
        if (!response.ok) throw new Error('Failed to get segments');
        
        return await response.json();
    }
    
    async stopRecording() {
        if (!this.currentSession) return;
        
        const response = await fetch(`${this.baseUrl}/recording/stop`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                sessionId: this.currentSession.sessionId
            })
        });
        
        if (!response.ok) throw new Error('Failed to stop recording');
        
        // Clean up
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        
        if (this.previewPlayer) {
            this.previewPlayer.destroy();
            this.previewPlayer = null;
        }
        
        const result = await response.json();
        this.currentSession = null;
        
        return result;
    }
}
```

#### 4.2 Integration with Existing App
```javascript
// Modifications to app.js
class SmartBoxTouchApp {
    constructor() {
        // ... existing code ...
        this.videoEngine = new VideoEngineClient();
        this.useFFmpegBackend = true; // Feature flag
    }
    
    async initializeVideo() {
        if (this.useFFmpegBackend) {
            try {
                // Initialize FFmpeg backend
                const source = await this.videoEngine.initialize();
                console.log('Video engine initialized with source:', source);
                
                // Update UI to show selected source
                if (source) {
                    this.updateVideoSourceDisplay(source);
                }
            } catch (error) {
                console.error('Failed to initialize video engine:', error);
                // Fall back to browser MediaRecorder
                this.useFFmpegBackend = false;
                await this.setupWebcam();
            }
        } else {
            // Use existing MediaRecorder implementation
            await this.setupWebcam();
        }
    }
    
    async onStartVideoRecording() {
        this.updateUI('recording', true);
        
        if (this.useFFmpegBackend) {
            try {
                // Start FFmpeg recording
                const session = await this.videoEngine.startRecording({
                    patientId: this.currentPatient.id,
                    studyId: this.currentPatient.studyId,
                    lossless: true,
                    preRecordSeconds: this.prerecordingMode || 60
                });
                
                console.log('FFmpeg recording started:', session);
                
                // Timeline works normally
                this.timeline.startRecording();
                
                // Update UI
                this.isRecording = true;
                this.recordingStartTime = Date.now();
                this.startRecordingTimer();
                
            } catch (error) {
                console.error('Failed to start FFmpeg recording:', error);
                this.showError('Recording failed to start');
            }
        } else {
            // Use existing MediaRecorder code
            this.startMediaRecorderRecording();
        }
    }
    
    async onStopVideoRecording() {
        if (this.useFFmpegBackend) {
            try {
                const result = await this.videoEngine.stopRecording();
                console.log('FFmpeg recording stopped:', result);
                
                // Get final segments for the capture list
                const segments = await this.videoEngine.getEditableSegments();
                
                // Add to captures
                this.captures.push({
                    id: result.sessionId,
                    type: 'video',
                    duration: result.duration,
                    segments: segments,
                    thumbnail: result.thumbnail,
                    timestamp: new Date()
                });
                
                this.updateCapturesList();
                
            } catch (error) {
                console.error('Failed to stop FFmpeg recording:', error);
            }
        } else {
            // Use existing MediaRecorder code
            this.stopMediaRecorderRecording();
        }
        
        this.updateUI('recording', false);
        this.isRecording = false;
        this.stopRecordingTimer();
        this.timeline.stopRecording();
    }
}
```

### Phase 5: Configuration UI (Day 7)

#### 5.1 Settings Extension
```html
<!-- Add to settings.html -->
<section id="video-engine-section" class="settings-section">
    <h2>Video Engine Settings</h2>
    
    <!-- Engine Selection -->
    <div class="setting-group">
        <label for="video-engine-type">Video Engine</label>
        <select id="video-engine-type" class="use-keyboard">
            <option value="ffmpeg">FFmpeg (Professional)</option>
            <option value="browser">Browser (Compatibility)</option>
        </select>
        <p class="help-text">FFmpeg provides lossless recording and better quality</p>
    </div>
    
    <!-- Source Selection -->
    <div class="setting-group">
        <label for="video-source">Video Source</label>
        <div class="source-selector">
            <select id="video-source" class="use-keyboard">
                <option value="auto">Auto-Detect</option>
            </select>
            <button type="button" id="refresh-sources" class="inline-button">
                <i class="ms-Icon ms-Icon--Refresh"></i>
            </button>
            <button type="button" id="test-source" class="inline-button">
                <i class="ms-Icon ms-Icon--TestBeaker"></i>
            </button>
        </div>
        <p class="help-text">Select video capture device</p>
    </div>
    
    <!-- Recording Quality -->
    <div class="settings-subsection">
        <h3>Recording Quality</h3>
        
        <div class="setting-group">
            <label for="video-quality-preset">Quality Preset</label>
            <select id="video-quality-preset" class="use-keyboard">
                <option value="medical">Medical (Lossless)</option>
                <option value="high">High Quality</option>
                <option value="standard">Standard</option>
                <option value="custom">Custom...</option>
            </select>
        </div>
        
        <div id="custom-quality-settings" style="display: none;">
            <div class="setting-group">
                <label for="video-codec">Video Codec</label>
                <select id="video-codec" class="use-keyboard">
                    <option value="FFV1">FFV1 (Lossless)</option>
                    <option value="H264_Lossless">H.264 Lossless</option>
                    <option value="ProRes">ProRes 4444</option>
                    <option value="H264">H.264</option>
                    <option value="H265">H.265/HEVC</option>
                </select>
            </div>
            
            <div class="setting-group">
                <label for="video-pixel-format">Color Format</label>
                <select id="video-pixel-format" class="use-keyboard">
                    <option value="yuv420p">YUV 4:2:0</option>
                    <option value="yuv422p">YUV 4:2:2 (Medical)</option>
                    <option value="yuv444p">YUV 4:4:4</option>
                </select>
            </div>
        </div>
    </div>
    
    <!-- Storage -->
    <div class="setting-group">
        <label for="video-storage-path">Video Storage Path</label>
        <div class="path-input-group">
            <input type="text" id="video-storage-path" class="use-keyboard" 
                   value="D:\SmartBoxRecordings">
            <button type="button" class="browse-button" data-action="browsefolder" 
                    data-for="video-storage-path">
                <i class="ms-Icon ms-Icon--FolderOpen"></i>
            </button>
        </div>
        <p class="help-text">Where to save video recordings</p>
    </div>
</section>
```

## Configuration Schema

```json
{
  "SmartBox": {
    "VideoEngine": {
      "Type": "FFmpeg",
      "FFmpegPath": "C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe",
      "Source": {
        "PreferredType": "MedicalGrabber",
        "PreferredDevice": "YUAN SC542N6",
        "FallbackToWebcam": true
      },
      "Recording": {
        "QualityPreset": "Medical",
        "MasterCodec": "FFV1",
        "Resolution": "1920x1080",
        "FrameRate": 60,
        "PixelFormat": "yuv422p",
        "PreRecordSeconds": 60,
        "SegmentDuration": 10
      },
      "Preview": {
        "Codec": "H264",
        "Bitrate": 5000,
        "Protocol": "HLS",
        "Latency": "UltraLow"
      },
      "Storage": {
        "RecordingPath": "D:\\SmartBoxRecordings",
        "TempPath": "D:\\SmartBoxTemp",
        "SegmentNaming": "segment_{0:D5}.mkv",
        "AutoCleanup": true,
        "RetentionDays": 30
      }
    }
  }
}
```

## Testing Strategy

### Test Scenarios
1. **Webcam Recording** - Basic functionality test
2. **YUAN Device** - Medical device integration
3. **Long Recording** - 30+ minute stability test
4. **Edit While Recording** - Segment accessibility
5. **Crash Recovery** - Resume from segments
6. **Export Chain** - DICOM conversion

### Performance Metrics
- Preview latency: <100ms
- Segment completion: <500ms after 10s
- Memory usage: <1GB for 60min recording
- CPU usage: <30% on modern hardware

## Migration Checklist

- [ ] Install FFmpeg on target machines
- [ ] Configure firewall for HLS streaming
- [ ] Test with actual YUAN device
- [ ] Verify segment permissions during recording
- [ ] Validate DICOM export pipeline
- [ ] Train users on new features

## SuperClaude Commands

For implementation in new chat:

```
/implement video-engine --framework dotnet --test-first
```

Or for comprehensive analysis first:

```
/analyze @VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md --focus architecture --think-hard
```

Or to build iteratively:

```
/build ffmpeg-video-engine --incremental --validate
```

## Success Criteria

1. **Lossless Recording** - FFV1 or H.264 lossless
2. **60 FPS Capture** - No frame drops
3. **Live Editing** - Access completed segments
4. **Low Latency** - <100ms preview delay
5. **YUAN Support** - Native integration
6. **Graceful Degradation** - Fallback to browser
7. **Zero UI Changes** - Existing interface works