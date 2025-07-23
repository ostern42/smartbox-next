# SmartBox Medical Video System Brief

## Vision
Transform SmartBox's video capture system from browser-based recording to a professional medical-grade solution using FFmpeg backend, enabling lossless recording, real-time editing, and DICOM compliance.

## Current State
- **Technology**: Browser MediaRecorder API with WebM/VP8 codec
- **Quality**: Lossy compression, limited to browser capabilities
- **Storage**: In-memory blobs, no file persistence during recording
- **Editing**: Cannot edit while recording (file locked)
- **Device Support**: Webcam only, no medical grabber support
- **Export**: UI exists but backend not implemented

## Desired State
- **Technology**: FFmpeg backend with professional codecs
- **Quality**: Lossless recording (FFV1, H.264 Hi444PP, ProRes)
- **Storage**: Segmented files (10s chunks) for live editing
- **Editing**: Edit completed segments while recording continues
- **Device Support**: YUAN SC542N6 medical grabber + webcam fallback
- **Export**: Full DICOM video encapsulation support

## Key Requirements

### Functional Requirements
- Maintain existing HTML/JS UI without changes
- 60 FPS capture for medical procedures
- Pre-recording buffer (60 seconds)
- Real-time HLS preview stream
- Segment-based recording for live editing
- Professional codec support (lossless)
- YUAN SC542N6 DirectShow integration
- Automatic device detection and fallback

### Non-Functional Requirements
- Preview latency <100ms
- Memory usage <1GB for 60min recording
- CPU usage <30% on modern hardware
- Graceful degradation to browser recording
- Zero data loss on crashes (segment recovery)

## Constraints

### Technical Constraints
- Must work on Windows 10/11
- Existing C# WPF application hosts WebView2
- Cannot modify existing UI components
- Must support both 32-bit and 64-bit systems

### Business Constraints
- Implementation timeline: 7-10 days
- Must not break existing installations
- Feature flag for gradual rollout
- Training materials required for new features

## Success Metrics
- Zero frame drops at 60 FPS
- <100ms preview latency
- 100% segment recovery after crashes
- User satisfaction score >4.5/5
- Support ticket reduction by 50%

## Risk Assessment
- **High Risk**: YUAN driver compatibility issues
- **Medium Risk**: FFmpeg process management complexity
- **Low Risk**: HLS streaming browser compatibility
- **Mitigation**: Comprehensive fallback mechanisms