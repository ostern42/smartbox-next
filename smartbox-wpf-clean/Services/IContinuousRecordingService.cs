using System;
using System.Threading.Tasks;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Interface for continuous recording service with retroactive capture
    /// </summary>
    public interface IContinuousRecordingService : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Gets whether recording is currently active
        /// </summary>
        bool IsRecording { get; }
        
        /// <summary>
        /// Gets the current recording duration
        /// </summary>
        TimeSpan RecordingDuration { get; }
        
        /// <summary>
        /// Gets the total bytes recorded across all segments
        /// </summary>
        long TotalBytesRecorded { get; }
        
        /// <summary>
        /// Gets the current segment number
        /// </summary>
        int CurrentSegmentNumber { get; }
        
        /// <summary>
        /// Raised when recording state changes
        /// </summary>
        event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
        
        /// <summary>
        /// Raised when a segment is completed and saved
        /// </summary>
        event EventHandler<SegmentCompletedEventArgs>? SegmentCompleted;
        
        /// <summary>
        /// Raised when memory pressure is detected
        /// </summary>
        event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;
        
        /// <summary>
        /// Start continuous recording for a patient
        /// </summary>
        /// <param name="patient">Patient information</param>
        Task StartRecordingAsync(PatientInfo patient);
        
        /// <summary>
        /// Stop continuous recording
        /// </summary>
        Task StopRecordingAsync();
        
        /// <summary>
        /// Save the last N minutes of recording retroactively
        /// </summary>
        /// <param name="minutes">Number of minutes to save (1-60)</param>
        /// <param name="reason">Reason for retroactive capture</param>
        /// <param name="outputPath">Optional output path for the video file</param>
        /// <returns>Path to the saved video file</returns>
        Task<string> SaveLastMinutesAsync(int minutes, string reason, string? outputPath = null);
        
        /// <summary>
        /// Get current recording statistics
        /// </summary>
        RecordingStatistics GetStatistics();
    }
}