# SmartBox Next - Comprehensive Context Map
**Generated**: 2025-07-22
**Project Version**: 2.0.0
**Medical Device Class**: FDA Class IIa

## 🏥 Executive Summary

SmartBox Next is a **FDA Class IIa medical imaging system** designed for emergency department use. It provides real-time video capture and DICOM-compliant medical imaging through a hybrid architecture combining:

- **.NET 8 WPF** UI host with WebView2 for modern web-based interface
- **Yuan SC550N1** hardware capture integration via SharedMemory IPC
- **WebRTC** browser-based camera capture as fallback
- **DICOM 3.0** compliant image/video storage and PACS integration
- **IEC 62304** medical device software safety compliance
- **HIPAA** patient data protection

## 🏗️ System Architecture

### Technology Stack
```
┌─────────────────────────────────────────────────────────────┐
│ UI Layer: HTML/CSS/JavaScript in WebView2                  │
├─────────────────────────────────────────────────────────────┤
│ Host Layer: .NET 8 WPF (MainWindowMinimal)                 │
├─────────────────────────────────────────────────────────────┤
│ Service Layer:                                              │
│ - UnifiedCaptureManager (Video capture orchestration)       │
│ - DicomVideoService (Video to DICOM conversion)            │
│ - FFmpegService (Video processing)                          │
│ - SharedMemoryClient (Yuan hardware IPC)                   │
├─────────────────────────────────────────────────────────────┤
│ External Components:                                        │
│ - Yuan SC550N1 Capture Service (Separate process)          │
│ - PACS Server (DICOM storage)                             │
│ - MWL Server (Modality Worklist)                          │
└─────────────────────────────────────────────────────────────┘
```

### Communication Architecture

#### JavaScript ↔ C# Bridge (WebView2)
```javascript
// JavaScript to C#
window.chrome.webview.postMessage(JSON.stringify({
    action: "saveSettings",
    data: { /* settings */ }
}));

// C# to JavaScript  
webView.CoreWebView2.PostWebMessageAsString(JSON.stringify({
    type: "settingsSaved",
    success: true,
    message: "Settings saved"
}));
```

#### C# ↔ Yuan Hardware (SharedMemory IPC)
- **Video Frames**: CircularBuffer "SmartBoxNextVideo" (high-performance)
- **Control**: NamedPipe "SmartBoxNextControl" (commands/responses)
- **Protocol**: FrameHeader struct + YUY2 video data

## 📋 Medical Compliance Requirements

### FDA Class IIa Requirements
- **Device ID**: SmartBox-Next-2.0
- **Manufacturer**: CIRSS Medical Systems
- **Software Version**: 2.0.0 (FDA traceable)
- **Audit Trail**: 7-year retention (2555 days)
- **Electronic Records**: 21 CFR Part 11 compliant

### IEC 62304 Safety Requirements
- **Safety Class**: B (Non-life-supporting)
- **Patient Data Timeout**: 5 seconds max
- **Critical Operation Timeout**: 3 seconds max
- **Emergency Response**: 1 second max
- **Auto-logout**: 30 minutes patient session

### DICOM 3.0 Compliance
- **Version**: DICOM 3.0
- **Modalities**: XC (External Camera), ES (Endoscopy), OT (Other)
- **Transfer Syntaxes**: 
  - MPEG-2 Main Profile (95% PACS compatibility)
  - H.264/MPEG-4 (85-90% compatibility)
  - Motion JPEG (98% legacy support)
- **SOP Classes**: 
  - VL Photographic Image Storage
  - Video Photographic Image Storage

### HIPAA Privacy Requirements
- **Encryption**: AES-256 (NIST approved)
- **Hash Algorithm**: SHA-256
- **Password Length**: 12 characters minimum
- **Failed Login Lockout**: 3 attempts
- **Data Retention**: 7 years patient data, 10 years imaging

## 🔧 Key Services & Components

### 1. UnifiedCaptureManager
**Purpose**: Orchestrates video capture from multiple sources
- Manages Yuan SC550N1 hardware capture
- Falls back to WebRTC browser capture
- Provides unified frame stream to UI
- Handles source switching and error recovery

### 2. SharedMemoryClient  
**Purpose**: IPC communication with Yuan capture service
- Connects to shared memory buffer for video frames
- Sends control commands via named pipe
- Handles connection management and error recovery
- Implements medical safety disposal patterns

### 3. DicomVideoService
**Purpose**: Converts video to DICOM format
- Supports multiple video encoding formats
- Creates DICOM-compliant metadata
- Handles patient information embedding
- Optimizes for PACS compatibility (MPEG-2 preferred)

### 4. FFmpegService
**Purpose**: Video processing and format conversion
- Manages FFmpeg binary configuration
- Converts WebM to MPEG-2/H.264
- Extracts frames for multiframe DICOM
- Handles architecture-specific binaries

### 5. WebSocketServer
**Purpose**: Real-time admin control interface
- Runs on port 5001
- Provides status monitoring
- Enables remote configuration
- Supports real-time updates

## 📊 Configuration Structure

### Application Configuration (config.json)
```json
{
  "Application": {
    "Title": "SmartBox Next",
    "WebServerPort": 8080,
    "AutoStartCapture": false,
    "EnableDebugLogging": false
  },
  "Storage": {
    "PhotosPath": "./Photos",
    "VideosPath": "./Videos", 
    "DicomPath": "./DicomOutput",
    "RetentionDays": 30
  },
  "Pacs": {
    "ServerHost": "192.168.1.100",
    "ServerPort": 11112,
    "CalledAeTitle": "PACS",
    "CallingAeTitle": "SMARTBOX"
  },
  "MwlSettings": {
    "EnableWorklist": true,
    "MwlServerHost": "192.168.1.100",
    "MwlServerPort": 105,
    "MwlServerAET": "ORTHANC"
  },
  "Video": {
    "MaxRecordingMinutes": 30,
    "VideoCodec": "h264",
    "VideoFramerate": 30,
    "VideoResolution": "1920x1080"
  },
  "Dicom": {
    "StationName": "SMARTBOX-ED",
    "AeTitle": "SMARTBOX",
    "Modality": "XC"
  }
}
```

## 🌐 Network Architecture

### Port Assignments
| Service | Port | Protocol | Purpose |
|---------|------|----------|---------|
| WebServer | 8080 | HTTP | Web UI hosting |
| WebSocket | 5001 | WS | Admin control |
| PACS | 11112 | DICOM | Image storage |
| MWL | 105 | DICOM | Worklist queries |

### Network Timeouts (Medical Grade)
- PACS Connection: 10 seconds
- MWL Query: 15 seconds  
- Emergency PACS: 5 seconds
- WebView Navigation: 30 seconds

## 🎨 User Interface Architecture

### Web Technologies
- **Framework**: Vanilla JavaScript with WebView2 bridge
- **Styling**: Modern medical themes (medical-blue, medical-teal, dark, night)
- **Accessibility**: WCAG 2.1 AA compliant
- **Touch Support**: 44px minimum target size

### Key UI Files
- `wwwroot/index.html` - Main medical interface
- `wwwroot/settings.html` - Configuration interface
- `wwwroot/js/smartbox.js` - Core application logic
- `wwwroot/js/message-safety.js` - Medical safety validation
- `wwwroot/js/settings-handler.js` - Settings management

## 🔒 Security Architecture

### Medical Device Security
- **Authentication**: Windows integrated
- **Authorization**: Role-based (future)
- **Encryption**: AES-256 for patient data
- **Audit Logging**: All patient access tracked
- **Session Management**: 30-minute auto-logout

### Network Security
- **DICOM over TLS**: Port 2762 (when enabled)
- **Secure MWL**: Port 11112
- **Certificate Management**: X.509 support
- **Firewall Rules**: Medical subnet isolation

## 📈 Performance Characteristics

### Video Capture Performance
- **Yuan SC550N1**: 60 FPS at 1920x1080
- **WebRTC**: 30 FPS (browser limited)
- **Frame Buffer**: Shared memory for zero-copy
- **Latency**: <50ms frame delivery

### Resource Requirements
- **Memory**: 2GB minimum, 4GB recommended
- **Storage**: 50GB for image/video cache
- **CPU**: Dual-core minimum for video encoding
- **Network**: 100Mbps for PACS transfers

## 🚀 Development Workflow

### Build Configuration
- **Platform**: x64 (primary), x86 (legacy)
- **Framework**: .NET 8.0 Windows
- **Output**: `bin/x64/Release/net8.0-windows/`

### Key Dependencies
- **Microsoft.Web.WebView2**: 1.0.2792.45
- **fo-dicom**: 5.1.2 (DICOM processing)
- **FFMpegCore**: 4.8.0 (Video processing)
- **SharedMemory**: 2.3.2 (IPC)
- **Newtonsoft.Json**: 13.0.3

### Testing Infrastructure
- **Unit Tests**: xUnit 2.6.1
- **Integration Tests**: Medical workflow validation
- **Performance Tests**: NBomber 5.5.0
- **Security Tests**: HIPAA compliance validation

## 🔄 Current Project Status

### Implemented Features ✅
- WebView2 UI hosting
- Basic DICOM file creation
- WebRTC camera capture
- Settings persistence
- PACS/MWL test connections
- Medical compliance constants

### In Development 🚧
- Yuan SC550N1 integration (SharedMemory IPC ready)
- DICOM video conversion pipeline
- Continuous recording service
- Advanced medical workflows

### Planned Features 📋
- AI-enhanced image analysis
- Cloud synchronization
- Mobile device integration
- HL7 integration
- Cross-platform support

## 📚 Documentation References

### Project Documentation
- `/ARCHITECTURE.md` - System architecture details
- `/MEDICAL_COMPLIANCE_STRUCTURE.md` - FDA/HIPAA requirements
- `/WEBVIEW2_COMMUNICATION_GUIDE.md` - JS-C# bridge reference
- `/PROJECT_INVENTORY.md` - Complete variable inventory
- `/PROJECT_STANDARDS.md` - Development standards

### API Documentation
- C#-JavaScript message protocol
- DICOM service interfaces
- Video capture API
- Configuration management

## 🎯 Key Integration Points

### For New Features
1. **UI Changes**: Modify wwwroot files, update message handlers
2. **Capture Sources**: Extend UnifiedCaptureManager
3. **DICOM Types**: Add to DicomVideoService
4. **Settings**: Update AppConfig and config.json
5. **Medical Compliance**: Use MedicalConstants

### For Maintenance
1. **Logs**: Check `%AppData%/SmartBoxNext/logs/`
2. **Config**: `%AppData%/SmartBoxNext/config.json`
3. **DICOM Output**: Configured output directory
4. **Temp Files**: System temp directory

---
*This context map provides a comprehensive overview of the SmartBox Next medical imaging system architecture, compliance requirements, and implementation details.*