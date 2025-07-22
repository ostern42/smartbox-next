# ContinuousRecordingService - Medical-Grade Video Recording

## Overview

The `ContinuousRecordingService` provides medical-grade continuous video recording with retroactive capture capabilities for SmartBox-Next. It enables always-on recording when a patient is selected, allowing operators to capture critical moments that occurred in the past.

## Key Features

### 1. **Continuous Recording** (Up to 4 hours)
- Automatically starts recording when a patient is selected
- Supports recording sessions up to 4 hours
- Seamless segment rotation (5, 10, 30, or 60-minute segments)
- No frame loss during segment transitions (<100ms gap)

### 2. **Retroactive Capture** ("2 Minuten zurück")
- Save the last N minutes of video on demand
- Configurable capture duration (1-60 minutes)
- Instant access to buffered frames
- Audit trail for retroactive captures

### 3. **Memory Management**
- Circular buffer with configurable size (default 4GB)
- Automatic disk offloading when memory threshold reached (>2GB)
- Memory-mapped files for crash resilience
- Progressive cleanup of old frames

### 4. **Audio Support** (Schluckdiagnostik)
- Medical-grade audio recording (48kHz, uncompressed)
- Synchronized with video frames
- Configurable bitrate (192kbps default)
- Lossless capture for diagnostic quality

### 5. **Performance Optimization**
- CPU usage monitoring (<50% target)
- Multi-threaded frame processing
- Hardware acceleration support
- Real-time performance metrics

### 6. **Medical Safety Features**
- Power failure recovery
- Automatic segment finalization
- Data integrity verification
- DICOM export compatibility

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   ContinuousRecordingService                │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────┐│
│  │ CircularBuffer  │  │ SegmentManager   │  │Performance ││
│  │ Manager         │  │                  │  │Monitor     ││
│  │                 │  │ • Rotation       │  │            ││
│  │ • Frame Buffer  │  │ • File Writing   │  │ • CPU      ││
│  │ • Memory Map    │  │ • Transitions    │  │ • Memory   ││
│  │ • Offloading    │  │                  │  │ • Metrics  ││
│  └────────┬────────┘  └────────┬─────────┘  └─────┬──────┘│
│           │                    │                    │       │
│           └────────────────────┴────────────────────┘       │
│                              │                              │
│  ┌───────────────────────────┴──────────────────────────┐  │
│  │              UnifiedCaptureManager                    │  │
│  │  • Yuan Hardware Capture                              │  │
│  │  • WebRTC Browser Capture                            │  │
│  │  • Frame Distribution                                │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Usage Examples

### Basic Setup

```csharp
// Configure services
services.AddSingleton<ContinuousRecordingConfig>(sp => new ContinuousRecordingConfig
{
    SegmentDurationMinutes = 30,      // 30-minute segments
    MaxRecordingDuration = TimeSpan.FromHours(4),
    MemoryThresholdBytes = 2L * 1024 * 1024 * 1024, // 2GB
    CircularBufferSizeMB = 4096,      // 4GB buffer
    EnableAudioRecording = true,
    AudioSampleRate = 48000
});

services.AddSingleton<IContinuousRecordingService, ContinuousRecordingService>();
```

### Start Recording on Patient Selection

```csharp
private async Task OnPatientSelected(PatientInfo patient)
{
    await _recordingService.StartRecordingAsync(patient);
}
```

### Implement "2 Minuten zurück" Button

```csharp
private async Task OnRetroactiveCaptureClicked()
{
    var videoPath = await _recordingService.SaveLastMinutesAsync(
        minutes: 2, 
        reason: "Critical moment captured by operator"
    );
    
    // Optional: Convert to DICOM
    var dicomPath = await _dicomService.ProcessWebMToDicomAsync(videoPath, patient);
}
```

### Monitor Recording Status

```csharp
// In your ViewModel
public string RecordingStatus => _recordingService.IsRecording 
    ? $"Recording: {_recordingService.RecordingDuration:hh\\:mm\\:ss}"
    : "Not Recording";

public string MemoryUsage => 
    $"{_recordingService.GetStatistics().MemoryUsageBytes / (1024.0 * 1024.0):F0} MB";
```

## Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| SegmentDurationMinutes | 30 | Duration of each video segment |
| MaxRecordingDuration | 4 hours | Maximum total recording time |
| MemoryThresholdBytes | 2GB | Memory usage threshold for disk offloading |
| OffloadPercentage | 25% | Percentage of frames to offload when threshold hit |
| CircularBufferSizeMB | 4096 | Size of in-memory circular buffer |
| EnableAudioRecording | true | Enable audio capture |
| AudioSampleRate | 48000 | Audio sample rate (Hz) |
| AudioBitrate | 192000 | Audio bitrate (bps) |

## Events

### RecordingStateChanged
Fired when recording starts or stops.

```csharp
recordingService.RecordingStateChanged += (sender, e) =>
{
    Console.WriteLine($"Recording: {e.IsRecording} for patient {e.Patient?.PatientId}");
};
```

### SegmentCompleted
Fired when a segment is finalized and saved.

```csharp
recordingService.SegmentCompleted += (sender, e) =>
{
    Console.WriteLine($"Segment {e.SegmentNumber} saved: {e.SegmentPath}");
};
```

### MemoryPressureDetected
Fired when memory usage exceeds threshold.

```csharp
recordingService.MemoryPressureDetected += (sender, e) =>
{
    Console.WriteLine($"Memory pressure: {e.MemoryUsageBytes / 1GB:F2} GB");
};
```

## Medical Safety Considerations

1. **Data Integrity**: All segments are written with atomic operations to prevent corruption
2. **Power Failure**: Memory-mapped files ensure data persistence even during unexpected shutdowns
3. **Patient Privacy**: Recordings are automatically stopped on patient change
4. **Audit Trail**: All retroactive captures are logged with timestamp, reason, and operator
5. **Resource Management**: Automatic cleanup prevents disk space exhaustion

## Performance Characteristics

- **Frame Rate**: 30 fps (configurable)
- **Resolution**: Up to 1920x1080
- **Latency**: <100ms for retroactive capture
- **CPU Usage**: <50% on modern hardware
- **Memory Usage**: 2-3GB typical, 4GB maximum
- **Disk I/O**: ~10-15 MB/s for 1080p video

## Integration with DICOM

The service integrates seamlessly with the DICOM export pipeline:

```csharp
// Convert recorded video to DICOM
var dicomPath = await _dicomVideoService.ProcessWebMToDicomAsync(
    videoPath, 
    patientInfo
);
```

## Troubleshooting

### High CPU Usage
- Reduce video resolution
- Increase segment duration
- Disable audio if not needed

### Memory Pressure
- Reduce CircularBufferSizeMB
- Decrease MemoryThresholdBytes
- Increase OffloadPercentage

### Disk Space Issues
- Configure shorter MaxRecordingDuration
- Implement automatic cleanup policy
- Monitor disk usage programmatically

## Future Enhancements

1. **Multiple Camera Support**: Record from multiple sources simultaneously
2. **Compression Options**: H.265/HEVC for better compression
3. **Cloud Backup**: Automatic segment upload to cloud storage
4. **AI Integration**: Real-time anomaly detection during recording
5. **Mobile Support**: Remote monitoring via mobile app