using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services.Video
{
    public class FFmpegVideoEngine : IVideoEngine, IDisposable
    {
        private readonly ConcurrentDictionary<string, FFmpegSession> _sessions;
        private readonly IConfiguration _config;
        private readonly ILogger<FFmpegVideoEngine> _logger;
        private readonly VideoSourceManager _sourceManager;
        private readonly string _ffmpegPath;
        private IVideoSource _currentSource;
        private PreRecordingBuffer _preRecordBuffer;
        
        public event EventHandler<RecordingEventArgs> RecordingEvent;
        public event EventHandler<SegmentEventArgs> SegmentCompleted;
        public event EventHandler<ErrorEventArgs> Error;
        
        public FFmpegVideoEngine(
            IConfiguration config,
            ILogger<FFmpegVideoEngine> logger,
            VideoSourceManager sourceManager)
        {
            _config = config;
            _logger = logger;
            _sourceManager = sourceManager;
            _sessions = new ConcurrentDictionary<string, FFmpegSession>();
            
            // Get FFmpeg path from config or use system default
            _ffmpegPath = _config["SmartBox:FFmpeg:BinaryPath"] ?? "ffmpeg";
            
            // Initialize FFMpegCore
            GlobalFFOptions.Configure(options =>
            {
                options.BinaryFolder = Path.GetDirectoryName(_ffmpegPath);
                options.TemporaryFilesFolder = _config["SmartBox:Storage:TempPath"] ?? Path.GetTempPath();
            });
        }
        
        public async Task<List<IVideoSource>> EnumerateSources()
        {
            return await _sourceManager.EnumerateSources();
        }
        
        public async Task<bool> SelectSource(string sourceId)
        {
            var sources = await EnumerateSources();
            var source = sources.FirstOrDefault(s => s.SourceId == sourceId);
            
            if (source == null)
            {
                _logger.LogWarning("Video source {SourceId} not found", sourceId);
                return false;
            }
            
            var isAvailable = await source.TestConnection();
            if (!isAvailable)
            {
                _logger.LogWarning("Video source {SourceId} is not available", sourceId);
                return false;
            }
            
            _currentSource = source;
            _logger.LogInformation("Selected video source: {DisplayName} ({SourceId})", 
                source.DisplayName, source.SourceId);
            
            return true;
        }
        
        public IVideoSource GetCurrentSource()
        {
            return _currentSource;
        }
        
        public async Task<RecordingSession> StartRecording(RecordingConfig config)
        {
            if (_currentSource == null)
            {
                // Auto-select first available source
                var sources = await EnumerateSources();
                foreach (var source in sources)
                {
                    if (await source.TestConnection())
                    {
                        _currentSource = source;
                        break;
                    }
                }
                
                if (_currentSource == null)
                {
                    throw new InvalidOperationException("No video source available");
                }
            }
            
            var session = new RecordingSession
            {
                SessionId = GenerateSessionId(),
                Config = config,
                StartTime = DateTime.UtcNow,
                Status = RecordingStatus.Starting
            };
            
            // Create session directory
            var sessionPath = Path.Combine(
                _config["SmartBox:Storage:TempPath"] ?? Path.GetTempPath(), 
                "recordings", 
                session.SessionId);
            
            Directory.CreateDirectory(sessionPath);
            Directory.CreateDirectory(Path.Combine(sessionPath, "preview"));
            Directory.CreateDirectory(Path.Combine(sessionPath, "segments"));
            
            session.OutputPath = sessionPath;
            
            // Build FFmpeg command
            var ffmpegArgs = BuildFFmpegArgs(session, sessionPath);
            
            // Start FFmpeg process
            var process = await StartFFmpegProcess(ffmpegArgs);
            
            // Create session wrapper
            var ffmpegSession = new FFmpegSession
            {
                Session = session,
                Process = process,
                CancellationTokenSource = new CancellationTokenSource(),
                SegmentListPath = Path.Combine(sessionPath, "segments.csv")
            };
            
            _sessions[session.SessionId] = ffmpegSession;
            
            // Start monitoring
            _ = Task.Run(() => MonitorSession(ffmpegSession));
            
            // Start segment watcher
            _ = Task.Run(() => WatchSegments(ffmpegSession));
            
            session.Status = RecordingStatus.Recording;
            
            RaiseRecordingEvent(session.SessionId, RecordingEventType.Started, session);
            
            _logger.LogInformation("Started recording session {SessionId} with source {SourceId}", 
                session.SessionId, _currentSource.SourceId);
            
            return session;
        }
        
        public async Task<bool> StopRecording(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
            {
                _logger.LogWarning("Session {SessionId} not found", sessionId);
                return false;
            }
            
            try
            {
                ffmpegSession.Session.Status = RecordingStatus.Stopping;
                
                // Send 'q' to FFmpeg for graceful shutdown
                if (!ffmpegSession.Process.HasExited)
                {
                    await ffmpegSession.Process.StandardInput.WriteAsync("q");
                    await ffmpegSession.Process.StandardInput.FlushAsync();
                    
                    // Wait for process to exit (max 5 seconds)
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await ffmpegSession.Process.WaitForExitAsync(cts.Token);
                }
                
                ffmpegSession.CancellationTokenSource.Cancel();
                ffmpegSession.Session.EndTime = DateTime.UtcNow;
                ffmpegSession.Session.Status = RecordingStatus.Stopped;
                
                // Move files to final location
                await FinalizeRecording(ffmpegSession);
                
                RaiseRecordingEvent(sessionId, RecordingEventType.Stopped, ffmpegSession.Session);
                
                _logger.LogInformation("Stopped recording session {SessionId}", sessionId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop recording {SessionId}", sessionId);
                ffmpegSession.Session.Status = RecordingStatus.Error;
                RaiseError(sessionId, "Failed to stop recording", ex, ErrorSeverity.Error);
                return false;
            }
        }
        
        public async Task<bool> PauseRecording(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return false;
            
            // FFmpeg doesn't support pause directly, so we'll stop the process
            // and track the pause state to resume with a new process
            ffmpegSession.Session.Status = RecordingStatus.Paused;
            ffmpegSession.PauseTime = DateTime.UtcNow;
            
            // Kill the FFmpeg process
            if (!ffmpegSession.Process.HasExited)
            {
                ffmpegSession.Process.Kill();
            }
            
            RaiseRecordingEvent(sessionId, RecordingEventType.Paused, null);
            
            return true;
        }
        
        public async Task<bool> ResumeRecording(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return false;
            
            if (ffmpegSession.Session.Status != RecordingStatus.Paused)
                return false;
            
            // Start a new FFmpeg process that appends to existing segments
            var ffmpegArgs = BuildFFmpegArgs(ffmpegSession.Session, ffmpegSession.Session.OutputPath, true);
            var process = await StartFFmpegProcess(ffmpegArgs);
            
            ffmpegSession.Process = process;
            ffmpegSession.Session.Status = RecordingStatus.Recording;
            ffmpegSession.PauseTime = null;
            
            RaiseRecordingEvent(sessionId, RecordingEventType.Resumed, null);
            
            return true;
        }
        
        public async Task<bool> EnablePreRecording(int seconds)
        {
            try
            {
                if (_preRecordBuffer != null)
                {
                    _preRecordBuffer.Dispose();
                }
                
                // Get current video properties
                var resolution = _config["SmartBox:VideoEngine:Recording:Resolution"] ?? "1920x1080";
                var parts = resolution.Split('x');
                var width = int.Parse(parts[0]);
                var height = int.Parse(parts[1]);
                var fps = int.Parse(_config["SmartBox:VideoEngine:Recording:FrameRate"] ?? "60");
                
                _preRecordBuffer = new PreRecordingBuffer(seconds, fps, width, height);
                
                _logger.LogInformation("Pre-recording buffer enabled for {Seconds} seconds", seconds);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable pre-recording");
                return false;
            }
        }
        
        public async Task<PreRecordStats> GetPreRecordStatus()
        {
            if (_preRecordBuffer == null)
            {
                return new PreRecordStats { Enabled = false };
            }
            
            return _preRecordBuffer.GetStats();
        }
        
        public async Task<string> TakeSnapshot(string sessionId, ImageFormat format)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                throw new InvalidOperationException("Session not found");
            
            var timestamp = DateTime.UtcNow - ffmpegSession.Session.StartTime;
            var outputFile = Path.Combine(
                ffmpegSession.Session.OutputPath, 
                $"snapshot_{DateTime.UtcNow:yyyyMMddHHmmss}.{format.ToString().ToLower()}");
            
            // Use the latest thumbnail as source
            var latestThumb = Path.Combine(ffmpegSession.Session.OutputPath, "latest_thumb.jpg");
            
            if (File.Exists(latestThumb))
            {
                if (format == ImageFormat.JPEG)
                {
                    File.Copy(latestThumb, outputFile);
                }
                else
                {
                    // Convert format using FFmpeg
                    await FFMpegArguments
                        .FromFileInput(latestThumb)
                        .OutputToFile(outputFile, true, options => options
                            .WithCustomArgument($"-f {format.ToString().ToLower()}"))
                        .ProcessAsynchronously();
                }
                
                return outputFile;
            }
            
            throw new InvalidOperationException("No snapshot available");
        }
        
        public async Task<bool> AddMarker(string sessionId, TimeSpan timestamp, MarkerType type)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return false;
            
            var marker = new VideoMarker
            {
                Timestamp = timestamp,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };
            
            ffmpegSession.Markers.Add(marker);
            
            RaiseRecordingEvent(sessionId, RecordingEventType.MarkerAdded, marker);
            
            return true;
        }
        
        public async Task<List<VideoSegment>> GetEditableSegments(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return new List<VideoSegment>();
            
            return ffmpegSession.Session.Segments
                .Where(s => s.IsComplete)
                .ToList();
        }
        
        public async Task<StreamInfo> GetPreviewStream(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return null;
            
            var streamUrl = $"/api/video/preview/{sessionId}/stream.m3u8";
            
            return new StreamInfo
            {
                StreamUrl = streamUrl,
                Protocol = "HLS",
                Width = 1280,
                Height = 720,
                Bitrate = ffmpegSession.Session.Config.PreviewBitrate,
                Latency = 2.0 // HLS typically has 2-3 second latency
            };
        }
        
        public async Task<byte[]> GetThumbnail(string sessionId, TimeSpan timestamp, int width)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return null;
            
            // Find the segment that contains this timestamp
            var segment = ffmpegSession.Session.Segments
                .FirstOrDefault(s => s.StartTime <= timestamp && 
                                   s.StartTime + s.Duration >= timestamp);
            
            if (segment == null || !segment.IsComplete)
                return null;
            
            var segmentFile = Path.Combine(
                ffmpegSession.Session.OutputPath, 
                "segments", 
                segment.FileName);
            
            if (!File.Exists(segmentFile))
                return null;
            
            // Generate thumbnail using FFmpeg
            var outputPath = Path.GetTempFileName() + ".jpg";
            var offsetInSegment = timestamp - segment.StartTime;
            
            try
            {
                await FFMpegArguments
                    .FromFileInput(segmentFile, true, options => options
                        .Seek(offsetInSegment))
                    .OutputToFile(outputPath, true, options => options
                        .WithVideoCodec("mjpeg")
                        .WithFrameOutputCount(1)
                        .Resize(width, -1))
                    .ProcessAsynchronously();
                
                var thumbnailData = await File.ReadAllBytesAsync(outputPath);
                
                return thumbnailData;
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }
        
        public RecordingStatus GetStatus(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var ffmpegSession))
                return RecordingStatus.Idle;
            
            return ffmpegSession.Session.Status;
        }
        
        public RecordingSession GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var ffmpegSession) 
                ? ffmpegSession.Session 
                : null;
        }
        
        public List<RecordingSession> GetActiveSessions()
        {
            return _sessions.Values
                .Select(s => s.Session)
                .Where(s => s.Status == RecordingStatus.Recording || 
                           s.Status == RecordingStatus.Paused)
                .ToList();
        }
        
        private string BuildFFmpegArgs(RecordingSession session, string outputPath, bool resume = false)
        {
            var source = _currentSource;
            var inputArgs = source.GetFFmpegInputArgs();
            var inputOptions = source.GetFFmpegOptions();
            
            // Build complex filter for multiple outputs
            var filterComplex = @"[0:v]split=3[main][preview][thumb];
                [main]null[main_out];
                [preview]scale='min(1280,iw)':'-1'[preview_out];
                [thumb]fps=1,scale=160:-1[thumb_out]";
            
            var segmentStartNumber = resume ? session.Segments.Count : 0;
            
            var args = new StringBuilder();
            
            // Input options
            foreach (var opt in inputOptions)
            {
                args.Append($"-{opt.Key} {opt.Value} ");
            }
            
            // Input
            args.Append($"{inputArgs} ");
            
            // Complex filter
            args.Append($"-filter_complex \"{filterComplex}\" ");
            
            // Master recording (lossless segments)
            args.Append("-map '[main_out]' ");
            args.Append($"-c:v {GetCodecString(session.Config.MasterCodec)} ");
            args.Append($"-pix_fmt {session.Config.PixelFormat} ");
            args.Append("-f segment -segment_time 10 -segment_format matroska ");
            args.Append($"-segment_list \"{Path.Combine(outputPath, "segments.csv")}\" ");
            args.Append("-segment_list_type csv -segment_list_flags +live ");
            args.Append($"-segment_start_number {segmentStartNumber} ");
            args.Append("-reset_timestamps 1 ");
            args.Append($"\"{Path.Combine(outputPath, "segments", "segment_%05d.mkv")}\" ");
            
            if (session.Config.EnablePreview)
            {
                // Preview stream (HLS)
                args.Append("-map '[preview_out]' ");
                args.Append("-c:v libx264 -preset veryfast -tune zerolatency ");
                args.Append($"-b:v {session.Config.PreviewBitrate}k -g 60 ");
                args.Append("-f hls -hls_time 2 -hls_list_size 5 -hls_flags delete_segments ");
                args.Append($"-hls_segment_filename \"{Path.Combine(outputPath, "preview", "chunk_%05d.ts")}\" ");
                args.Append($"\"{Path.Combine(outputPath, "preview", "stream.m3u8")}\" ");
                
                // Thumbnail output
                args.Append("-map '[thumb_out]' ");
                args.Append("-f image2 -update 1 ");
                args.Append($"\"{Path.Combine(outputPath, "latest_thumb.jpg")}\"");
            }
            
            return args.ToString();
        }
        
        private string GetCodecString(VideoCodec codec)
        {
            return codec switch
            {
                VideoCodec.FFV1 => "ffv1 -level 3 -coder 1 -context 1 -slices 24 -slicecrc 1",
                VideoCodec.H264_Lossless => "libx264 -preset ultrafast -qp 0",
                VideoCodec.H264 => "libx264 -preset fast -crf 23",
                VideoCodec.H265 => "libx265 -preset fast -crf 23",
                VideoCodec.ProRes => "prores_ks -profile:v 4",
                VideoCodec.VP9 => "libvpx-vp9 -lossless 1",
                _ => "libx264 -preset fast -crf 23"
            };
        }
        
        private async Task<Process> StartFFmpegProcess(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    _logger.LogDebug("FFmpeg: {Data}", e.Data);
                }
            };
            
            process.Start();
            process.BeginErrorReadLine();
            
            return process;
        }
        
        private async Task MonitorSession(FFmpegSession session)
        {
            try
            {
                await session.Process.WaitForExitAsync(session.CancellationTokenSource.Token);
                
                if (session.Process.ExitCode != 0 && session.Session.Status == RecordingStatus.Recording)
                {
                    session.Session.Status = RecordingStatus.Error;
                    RaiseError(session.Session.SessionId, 
                        $"FFmpeg exited with code {session.Process.ExitCode}", 
                        null, 
                        ErrorSeverity.Error);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring session {SessionId}", session.Session.SessionId);
                RaiseError(session.Session.SessionId, "Monitoring error", ex, ErrorSeverity.Error);
            }
        }
        
        private async Task WatchSegments(FFmpegSession session)
        {
            var segmentPath = Path.Combine(session.Session.OutputPath, "segments");
            var processedSegments = new HashSet<string>();
            
            while (!session.CancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Check for new segment files
                    var segmentFiles = Directory.GetFiles(segmentPath, "segment_*.mkv")
                        .Where(f => !processedSegments.Contains(Path.GetFileName(f)))
                        .OrderBy(f => f)
                        .ToList();
                    
                    foreach (var segmentFile in segmentFiles)
                    {
                        var fileName = Path.GetFileName(segmentFile);
                        
                        // Wait for file to be complete (not being written)
                        if (await IsFileComplete(segmentFile))
                        {
                            var fileInfo = new FileInfo(segmentFile);
                            var segmentNumber = int.Parse(
                                Path.GetFileNameWithoutExtension(fileName)
                                    .Replace("segment_", ""));
                            
                            var segment = new VideoSegment
                            {
                                Number = segmentNumber,
                                FileName = fileName,
                                StartTime = TimeSpan.FromSeconds(segmentNumber * 10),
                                Duration = TimeSpan.FromSeconds(10),
                                FileSize = fileInfo.Length,
                                IsComplete = true,
                                IsLocked = false,
                                CreatedAt = fileInfo.CreationTimeUtc
                            };
                            
                            session.Session.Segments.Add(segment);
                            processedSegments.Add(fileName);
                            
                            RaiseSegmentCompleted(session.Session.SessionId, segment);
                        }
                    }
                    
                    await Task.Delay(500, session.CancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error watching segments for session {SessionId}", 
                        session.Session.SessionId);
                }
            }
        }
        
        private async Task<bool> IsFileComplete(string filePath)
        {
            try
            {
                // Try to open file exclusively - if it fails, file is still being written
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
        
        private async Task FinalizeRecording(FFmpegSession session)
        {
            try
            {
                // Create final output directory
                var finalPath = session.Session.Config.OutputDirectory;
                Directory.CreateDirectory(finalPath);
                
                // Move segments
                var segmentSource = Path.Combine(session.Session.OutputPath, "segments");
                var segmentDest = Path.Combine(finalPath, "segments");
                
                if (Directory.Exists(segmentSource))
                {
                    Directory.CreateDirectory(segmentDest);
                    
                    foreach (var file in Directory.GetFiles(segmentSource))
                    {
                        var destFile = Path.Combine(segmentDest, Path.GetFileName(file));
                        File.Move(file, destFile, true);
                    }
                }
                
                // Copy metadata
                var metadataFile = Path.Combine(finalPath, "recording_metadata.json");
                var metadata = new
                {
                    SessionId = session.Session.SessionId,
                    StartTime = session.Session.StartTime,
                    EndTime = session.Session.EndTime,
                    Duration = (session.Session.EndTime ?? DateTime.UtcNow) - session.Session.StartTime,
                    Config = session.Session.Config,
                    Segments = session.Session.Segments,
                    Markers = session.Markers,
                    Source = new
                    {
                        _currentSource?.SourceId,
                        _currentSource?.DisplayName,
                        Type = _currentSource?.Type.ToString()
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(metadata, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                await File.WriteAllTextAsync(metadataFile, json);
                
                // Update session output path
                session.Session.OutputPath = finalPath;
                
                _logger.LogInformation("Finalized recording to {Path}", finalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to finalize recording {SessionId}", 
                    session.Session.SessionId);
                throw;
            }
        }
        
        private string GenerateSessionId()
        {
            return $"rec_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}".Substring(0, 32);
        }
        
        private void RaiseRecordingEvent(string sessionId, RecordingEventType eventType, object data)
        {
            RecordingEvent?.Invoke(this, new RecordingEventArgs
            {
                SessionId = sessionId,
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Data = data
            });
        }
        
        private void RaiseSegmentCompleted(string sessionId, VideoSegment segment)
        {
            SegmentCompleted?.Invoke(this, new SegmentEventArgs
            {
                SessionId = sessionId,
                Segment = segment
            });
        }
        
        private void RaiseError(string sessionId, string message, Exception exception, ErrorSeverity severity)
        {
            Error?.Invoke(this, new ErrorEventArgs
            {
                SessionId = sessionId,
                Message = message,
                Exception = exception,
                Severity = severity
            });
        }
        
        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.CancellationTokenSource?.Cancel();
                    
                    if (!session.Process?.HasExited ?? false)
                    {
                        session.Process.Kill();
                    }
                    
                    session.Process?.Dispose();
                    session.CancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing session {SessionId}", session.Session.SessionId);
                }
            }
            
            _sessions.Clear();
            _preRecordBuffer?.Dispose();
        }
        
        private class FFmpegSession
        {
            public RecordingSession Session { get; set; }
            public Process Process { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public string SegmentListPath { get; set; }
            public DateTime? PauseTime { get; set; }
            public List<VideoMarker> Markers { get; set; } = new();
        }
        
        private class VideoMarker
        {
            public TimeSpan Timestamp { get; set; }
            public MarkerType Type { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}