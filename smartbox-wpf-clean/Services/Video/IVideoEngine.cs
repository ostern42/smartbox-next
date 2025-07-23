using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartBoxNext.Services.Video
{
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
        
        // Session Management
        RecordingSession GetSession(string sessionId);
        List<RecordingSession> GetActiveSessions();
        
        // Events
        event EventHandler<RecordingEventArgs> RecordingEvent;
        event EventHandler<SegmentEventArgs> SegmentCompleted;
        event EventHandler<ErrorEventArgs> Error;
    }

    public class RecordingConfig
    {
        public string PatientId { get; set; }
        public string StudyId { get; set; }
        
        // Video settings
        public VideoCodec MasterCodec { get; set; } = VideoCodec.FFV1;
        public string Resolution { get; set; } = "1920x1080";
        public int FrameRate { get; set; } = 60;
        public string PixelFormat { get; set; } = "yuv422p";
        
        // Pre-recording
        public int PreRecordSeconds { get; set; } = 60;
        
        // Storage
        public string OutputDirectory { get; set; }
        
        // Preview settings
        public int PreviewBitrate { get; set; } = 5000;
        public bool EnablePreview { get; set; } = true;
    }

    public class RecordingSession
    {
        public string SessionId { get; set; }
        public RecordingConfig Config { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public RecordingStatus Status { get; set; }
        public string OutputPath { get; set; }
        public List<VideoSegment> Segments { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class VideoSegment
    {
        public int Number { get; set; }
        public string FileName { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public long FileSize { get; set; }
        public bool IsComplete { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StreamInfo
    {
        public string StreamUrl { get; set; }
        public string Protocol { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Bitrate { get; set; }
        public double Latency { get; set; }
    }

    public class PreRecordStats
    {
        public bool Enabled { get; set; }
        public int BufferSeconds { get; set; }
        public int CurrentSeconds { get; set; }
        public long MemoryUsageBytes { get; set; }
        public int FrameCount { get; set; }
    }

    public enum RecordingStatus
    {
        Idle,
        Starting,
        Recording,
        Paused,
        Stopping,
        Stopped,
        Error
    }

    public enum VideoCodec
    {
        FFV1,
        H264,
        H264_Lossless,
        H265,
        ProRes,
        VP9
    }

    public enum ImageFormat
    {
        JPEG,
        PNG,
        BMP
    }

    public enum MarkerType
    {
        Generic,
        Important,
        Error,
        Start,
        End,
        Annotation
    }

    public class RecordingEventArgs : EventArgs
    {
        public string SessionId { get; set; }
        public RecordingEventType EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public object Data { get; set; }
    }

    public enum RecordingEventType
    {
        Started,
        Stopped,
        Paused,
        Resumed,
        SegmentStarted,
        SegmentCompleted,
        ThumbnailReady,
        MarkerAdded,
        Error,
        Warning,
        StatusUpdate
    }

    public class SegmentEventArgs : EventArgs
    {
        public string SessionId { get; set; }
        public VideoSegment Segment { get; set; }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string SessionId { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public ErrorSeverity Severity { get; set; }
    }

    public enum ErrorSeverity
    {
        Warning,
        Error,
        Critical
    }
}