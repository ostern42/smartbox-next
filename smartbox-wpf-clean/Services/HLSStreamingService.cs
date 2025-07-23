using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// HLS (HTTP Live Streaming) service with DVR functionality for medical video streaming
    /// </summary>
    public class HLSStreamingService : IDisposable
    {
        private readonly ILogger<HLSStreamingService> _logger;
        private readonly FFmpegService _ffmpegService;
        private readonly string _outputDirectory;
        private readonly TimeSpan _segmentDuration = TimeSpan.FromSeconds(6);
        private readonly int _playlistSize = 10; // Number of segments in playlist
        private readonly int _dvrWindowMinutes = 120; // 2-hour DVR window
        
        private readonly ConcurrentDictionary<string, StreamingSession> _sessions = new();
        private readonly ConcurrentDictionary<string, SegmentInfo> _segments = new();
        private readonly Timer _cleanupTimer;
        private bool _disposed;
        
        public event EventHandler<StreamingEventArgs>? StreamingEvent;
        
        public HLSStreamingService(ILogger<HLSStreamingService> logger, FFmpegService ffmpegService, string outputDirectory)
        {
            _logger = logger;
            _ffmpegService = ffmpegService;
            _outputDirectory = Path.Combine(outputDirectory, "hls");
            
            // Ensure output directory exists
            Directory.CreateDirectory(_outputDirectory);
            
            // Start cleanup timer for old segments
            _cleanupTimer = new Timer(CleanupOldSegments, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
        
        /// <summary>
        /// Start a new HLS streaming session
        /// </summary>
        public async Task<StreamingSession> StartStreamingSessionAsync(string sessionId, StreamInput input, StreamingOptions options)
        {
            if (_sessions.ContainsKey(sessionId))
            {
                throw new InvalidOperationException($"Session {sessionId} already exists");
            }
            
            var session = new StreamingSession
            {
                SessionId = sessionId,
                StartTime = DateTime.UtcNow,
                Options = options,
                OutputPath = Path.Combine(_outputDirectory, sessionId),
                IsActive = true
            };
            
            // Create session directory
            Directory.CreateDirectory(session.OutputPath);
            
            // Start FFmpeg process for HLS encoding
            var cancellationToken = session.CancellationTokenSource.Token;
            
            session.StreamingTask = Task.Run(async () =>
            {
                await StartFFmpegStreamingAsync(session, input, cancellationToken);
            }, cancellationToken);
            
            _sessions[sessionId] = session;
            
            // Generate initial playlist
            await UpdatePlaylistAsync(session);
            
            StreamingEvent?.Invoke(this, new StreamingEventArgs
            {
                SessionId = sessionId,
                EventType = StreamingEventType.SessionStarted,
                Timestamp = DateTime.UtcNow
            });
            
            return session;
        }
        
        /// <summary>
        /// Stop a streaming session
        /// </summary>
        public async Task StopStreamingSessionAsync(string sessionId)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                return;
            }
            
            session.IsActive = false;
            session.CancellationTokenSource.Cancel();
            
            try
            {
                if (session.StreamingTask != null)
                {
                    await session.StreamingTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch (TimeoutException)
            {
                _logger.LogWarning($"Streaming task for session {sessionId} did not complete in time");
            }
            
            // Keep session data for DVR playback
            session.EndTime = DateTime.UtcNow;
            
            StreamingEvent?.Invoke(this, new StreamingEventArgs
            {
                SessionId = sessionId,
                EventType = StreamingEventType.SessionStopped,
                Timestamp = DateTime.UtcNow
            });
        }
        
        /// <summary>
        /// Get HLS playlist for a session
        /// </summary>
        public async Task<string> GetPlaylistAsync(string sessionId, bool isLive = true)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }
            
            var playlist = new StringBuilder();
            playlist.AppendLine("#EXTM3U");
            playlist.AppendLine("#EXT-X-VERSION:6");
            playlist.AppendLine($"#EXT-X-TARGETDURATION:{(int)_segmentDuration.TotalSeconds}");
            
            var segments = GetSessionSegments(sessionId)
                .OrderBy(s => s.Index)
                .ToList();
            
            if (segments.Any())
            {
                // Add DVR support
                if (session.Options.EnableDVR)
                {
                    playlist.AppendLine("#EXT-X-PLAYLIST-TYPE:EVENT");
                    var firstSegment = segments.First();
                    playlist.AppendLine($"#EXT-X-MEDIA-SEQUENCE:{firstSegment.Index}");
                    
                    // Program date time for scrubbing
                    playlist.AppendLine($"#EXT-X-PROGRAM-DATE-TIME:{firstSegment.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}");
                }
                else
                {
                    // Live playlist with sliding window
                    var windowSegments = segments.TakeLast(_playlistSize).ToList();
                    if (windowSegments.Any())
                    {
                        playlist.AppendLine($"#EXT-X-MEDIA-SEQUENCE:{windowSegments.First().Index}");
                    }
                    segments = windowSegments;
                }
                
                // Add segments
                foreach (var segment in segments)
                {
                    playlist.AppendLine($"#EXTINF:{segment.Duration:F3},");
                    playlist.AppendLine($"{segment.FileName}");
                    
                    // Add byte range if using single file
                    if (session.Options.UseSingleFile && segment.ByteRange != null)
                    {
                        playlist.AppendLine($"#EXT-X-BYTERANGE:{segment.ByteRange.Length}@{segment.ByteRange.Offset}");
                    }
                }
                
                if (!session.IsActive)
                {
                    playlist.AppendLine("#EXT-X-ENDLIST");
                }
            }
            
            return playlist.ToString();
        }
        
        /// <summary>
        /// Get segment data
        /// </summary>
        public async Task<byte[]?> GetSegmentAsync(string sessionId, string segmentName)
        {
            var segmentPath = Path.Combine(_outputDirectory, sessionId, segmentName);
            
            if (!File.Exists(segmentPath))
            {
                return null;
            }
            
            try
            {
                return await File.ReadAllBytesAsync(segmentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading segment {segmentName} for session {sessionId}");
                return null;
            }
        }
        
        /// <summary>
        /// Mark in/out points for a session
        /// </summary>
        public void MarkInPoint(string sessionId, TimeSpan timestamp)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                session.MarkedRanges.Add(new MarkedRange { InPoint = timestamp });
                
                StreamingEvent?.Invoke(this, new StreamingEventArgs
                {
                    SessionId = sessionId,
                    EventType = StreamingEventType.InPointMarked,
                    Timestamp = DateTime.UtcNow,
                    Data = timestamp
                });
            }
        }
        
        public void MarkOutPoint(string sessionId, TimeSpan timestamp)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                var lastRange = session.MarkedRanges.LastOrDefault();
                if (lastRange != null && lastRange.OutPoint == null)
                {
                    lastRange.OutPoint = timestamp;
                    
                    StreamingEvent?.Invoke(this, new StreamingEventArgs
                    {
                        SessionId = sessionId,
                        EventType = StreamingEventType.OutPointMarked,
                        Timestamp = DateTime.UtcNow,
                        Data = timestamp
                    });
                }
            }
        }
        
        /// <summary>
        /// Get marked ranges for export
        /// </summary>
        public List<MarkedRange> GetMarkedRanges(string sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                return session.MarkedRanges.Where(r => r.OutPoint.HasValue).ToList();
            }
            return new List<MarkedRange>();
        }
        
        private async Task StartFFmpegStreamingAsync(StreamingSession session, StreamInput input, CancellationToken cancellationToken)
        {
            try
            {
                var outputPath = Path.Combine(session.OutputPath, "stream.m3u8");
                var segmentPath = Path.Combine(session.OutputPath, "segment_%05d.ts");
                
                // Build FFmpeg arguments for HLS
                var arguments = new List<string>();
                
                // Input
                if (input.InputType == StreamInputType.Device)
                {
                    arguments.AddRange(new[] { "-f", "dshow", "-i", $"video={input.DeviceName}" });
                }
                else if (input.InputType == StreamInputType.RTMP)
                {
                    arguments.AddRange(new[] { "-i", input.Url });
                }
                else if (input.InputType == StreamInputType.File)
                {
                    arguments.AddRange(new[] { "-re", "-i", input.FilePath });
                }
                
                // Video encoding
                arguments.AddRange(new[]
                {
                    "-c:v", session.Options.VideoCodec,
                    "-preset", session.Options.EncodingPreset,
                    "-b:v", session.Options.VideoBitrate,
                    "-maxrate", session.Options.VideoBitrate,
                    "-bufsize", $"{int.Parse(session.Options.VideoBitrate.TrimEnd('k')) * 2}k",
                    "-g", $"{session.Options.Framerate * 2}", // GOP size
                    "-r", session.Options.Framerate.ToString()
                });
                
                // Audio encoding
                if (session.Options.IncludeAudio)
                {
                    arguments.AddRange(new[]
                    {
                        "-c:a", "aac",
                        "-b:a", session.Options.AudioBitrate
                    });
                }
                else
                {
                    arguments.Add("-an");
                }
                
                // HLS specific
                arguments.AddRange(new[]
                {
                    "-f", "hls",
                    "-hls_time", ((int)_segmentDuration.TotalSeconds).ToString(),
                    "-hls_list_size", _playlistSize.ToString(),
                    "-hls_flags", "append_list+delete_segments+program_date_time",
                    "-hls_segment_filename", segmentPath,
                    outputPath
                });
                
                // Start FFmpeg process
                var ffmpegPath = GlobalFFOptions.GetFFMpegBinaryPath();
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = string.Join(" ", arguments),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _logger.LogDebug($"FFmpeg output: {e.Data}");
                };
                
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogDebug($"FFmpeg: {e.Data}");
                        ParseFFmpegOutput(session, e.Data);
                    }
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Monitor segment creation
                _ = Task.Run(async () =>
                {
                    await MonitorSegmentCreationAsync(session, cancellationToken);
                }, cancellationToken);
                
                // Wait for process to exit or cancellation
                await process.WaitForExitAsync(cancellationToken);
                
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in FFmpeg streaming for session {session.SessionId}");
                throw;
            }
        }
        
        private async Task MonitorSegmentCreationAsync(StreamingSession session, CancellationToken cancellationToken)
        {
            var lastSegmentIndex = -1;
            
            while (!cancellationToken.IsCancellationRequested && session.IsActive)
            {
                try
                {
                    var segmentFiles = Directory.GetFiles(session.OutputPath, "segment_*.ts")
                        .OrderBy(f => f)
                        .ToList();
                    
                    foreach (var file in segmentFiles)
                    {
                        var fileName = Path.GetFileName(file);
                        var indexStr = fileName.Replace("segment_", "").Replace(".ts", "");
                        
                        if (int.TryParse(indexStr, out var index) && index > lastSegmentIndex)
                        {
                            var fileInfo = new FileInfo(file);
                            var segment = new SegmentInfo
                            {
                                SessionId = session.SessionId,
                                FileName = fileName,
                                Index = index,
                                Duration = _segmentDuration.TotalSeconds,
                                Timestamp = session.StartTime.AddSeconds(index * _segmentDuration.TotalSeconds),
                                FilePath = file,
                                FileSize = fileInfo.Length
                            };
                            
                            var key = $"{session.SessionId}_{fileName}";
                            _segments[key] = segment;
                            lastSegmentIndex = index;
                            
                            // Update playlist
                            await UpdatePlaylistAsync(session);
                            
                            StreamingEvent?.Invoke(this, new StreamingEventArgs
                            {
                                SessionId = session.SessionId,
                                EventType = StreamingEventType.SegmentCreated,
                                Timestamp = DateTime.UtcNow,
                                Data = segment
                            });
                        }
                    }
                    
                    await Task.Delay(500, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error monitoring segments");
                }
            }
        }
        
        private async Task UpdatePlaylistAsync(StreamingSession session)
        {
            try
            {
                var playlistPath = Path.Combine(session.OutputPath, "stream.m3u8");
                var playlist = await GetPlaylistAsync(session.SessionId);
                await File.WriteAllTextAsync(playlistPath, playlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating playlist");
            }
        }
        
        private void ParseFFmpegOutput(StreamingSession session, string output)
        {
            // Parse FFmpeg progress
            if (output.Contains("frame="))
            {
                // Extract encoding stats
                var parts = output.Split(new[] { ' ', '=' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (parts[i] == "frame" && int.TryParse(parts[i + 1], out var frame))
                    {
                        session.EncodedFrames = frame;
                    }
                    else if (parts[i] == "fps" && double.TryParse(parts[i + 1], out var fps))
                    {
                        session.CurrentFps = fps;
                    }
                    else if (parts[i] == "bitrate" && parts[i + 1].EndsWith("kbits/s"))
                    {
                        if (double.TryParse(parts[i + 1].Replace("kbits/s", ""), out var bitrate))
                        {
                            session.CurrentBitrate = bitrate * 1000;
                        }
                    }
                }
            }
        }
        
        private IEnumerable<SegmentInfo> GetSessionSegments(string sessionId)
        {
            return _segments.Values.Where(s => s.SessionId == sessionId);
        }
        
        private void CleanupOldSegments(object? state)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.AddMinutes(-_dvrWindowMinutes);
                
                foreach (var session in _sessions.Values)
                {
                    if (!session.Options.EnableDVR)
                        continue;
                    
                    var oldSegments = GetSessionSegments(session.SessionId)
                        .Where(s => s.Timestamp < cutoffTime)
                        .ToList();
                    
                    foreach (var segment in oldSegments)
                    {
                        try
                        {
                            if (File.Exists(segment.FilePath))
                            {
                                File.Delete(segment.FilePath);
                            }
                            
                            var key = $"{segment.SessionId}_{segment.FileName}";
                            _segments.TryRemove(key, out _);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error deleting old segment {segment.FileName}");
                        }
                    }
                }
                
                // Cleanup inactive sessions
                var inactiveSessions = _sessions.Values
                    .Where(s => !s.IsActive && s.EndTime.HasValue && 
                           s.EndTime.Value < DateTime.UtcNow.AddHours(-4))
                    .ToList();
                
                foreach (var session in inactiveSessions)
                {
                    try
                    {
                        if (Directory.Exists(session.OutputPath))
                        {
                            Directory.Delete(session.OutputPath, true);
                        }
                        _sessions.TryRemove(session.SessionId, out _);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error cleaning up session {session.SessionId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup timer");
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            _cleanupTimer?.Dispose();
            
            // Stop all active sessions
            var activeSessions = _sessions.Values.Where(s => s.IsActive).ToList();
            foreach (var session in activeSessions)
            {
                StopStreamingSessionAsync(session.SessionId).Wait(TimeSpan.FromSeconds(5));
            }
            
            _disposed = true;
        }
    }
    
    // Data models
    public class StreamingSession
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public StreamingOptions Options { get; set; } = new();
        public Task? StreamingTask { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; } = new();
        public List<MarkedRange> MarkedRanges { get; } = new();
        
        // Stats
        public int EncodedFrames { get; set; }
        public double CurrentFps { get; set; }
        public double CurrentBitrate { get; set; }
    }
    
    public class StreamingOptions
    {
        public bool EnableDVR { get; set; } = true;
        public bool IncludeAudio { get; set; } = true;
        public bool UseSingleFile { get; set; } = false;
        public string VideoCodec { get; set; } = "libx264";
        public string VideoBitrate { get; set; } = "2000k";
        public string AudioBitrate { get; set; } = "128k";
        public int Framerate { get; set; } = 30;
        public string EncodingPreset { get; set; } = "veryfast";
        public string Resolution { get; set; } = "1280x720";
    }
    
    public class StreamInput
    {
        public StreamInputType InputType { get; set; }
        public string? DeviceName { get; set; }
        public string? Url { get; set; }
        public string? FilePath { get; set; }
    }
    
    public enum StreamInputType
    {
        Device,
        RTMP,
        File
    }
    
    public class SegmentInfo
    {
        public string SessionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int Index { get; set; }
        public double Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public ByteRange? ByteRange { get; set; }
    }
    
    public class ByteRange
    {
        public long Offset { get; set; }
        public long Length { get; set; }
    }
    
    public class MarkedRange
    {
        public TimeSpan InPoint { get; set; }
        public TimeSpan? OutPoint { get; set; }
        public string? Label { get; set; }
    }
    
    public class StreamingEventArgs : EventArgs
    {
        public string SessionId { get; set; } = string.Empty;
        public StreamingEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
    }
    
    public enum StreamingEventType
    {
        SessionStarted,
        SessionStopped,
        SegmentCreated,
        InPointMarked,
        OutPointMarked,
        Error
    }
}