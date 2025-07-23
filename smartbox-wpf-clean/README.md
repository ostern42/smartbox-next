# SmartBox Next - WPF Edition

## 🏥 Medical Image Capture System with Yuan SC550N1 Integration

A robust, medical-grade image capture system built with WPF and .NET 8, designed for reliability in clinical environments. Features professional Yuan SC550N1 SDI/HDMI capture card integration with 60 FPS performance and DICOM compliance.

**Latest Update (July 2025)**: Complete Yuan SC550N1 integration with Windows Service architecture for Session 0 compatibility and 60 FPS SharedMemory IPC.

## 🚀 Why WPF over WinUI3?

After extensive testing with WinUI3, we made the strategic decision to migrate to WPF:

- **Stability**: No mysterious WinRT.Runtime exceptions
- **Simplicity**: WebView2 integration "just works"
- **Deployment**: Standard Windows application, no MSIX complexity
- **Reliability**: Proven technology for medical applications

## 🏗️ Hybrid Architecture

```
SmartBoxNext.exe (.NET 8 WPF Shell)
├── WebView2 (Full Window)
├── Local Web Server (Port 8080)
├── Medical Components
│   ├── DICOM Exporter (fo-dicom)
│   ├── PACS Sender (C-STORE)
│   ├── Queue Manager (JSON-based)
│   └── Unified Capture Manager
├── HTML/CSS/JS UI (70 FPS WebRTC)
└── Yuan Capture Integration
    ├── SmartBoxNext.CaptureService (.NET Framework 4.8 Windows Service)
    ├── DirectShow.NET for Yuan SC550N1
    ├── SharedMemory.CircularBuffer (60 FPS IPC)
    ├── YUY2 → RGB → DICOM Pipeline
    └── Named Pipes for Control Commands
```

## ✨ Key Features

### Medical-Grade Reliability
- **Power-loss tolerant**: Queue survives unexpected shutdowns
- **Auto-recovery**: Automatic retry with exponential backoff
- **No data loss**: Persistent JSON queue, atomic file operations
- **Error handling**: Comprehensive logging and error recovery

### Emergency Features
- **Emergency Templates**: Quick patient creation for emergencies
  - Notfall männlich (auto timestamp)
  - Notfall weiblich (auto timestamp)
  - Notfall Kind (with age estimate)
- **Touch Keyboard**: QWERTZ layout optimized for medical gloves
- **Offline Mode**: Full functionality without network

### Technical Features
- **Dual Capture Sources**: 
  - 70 FPS WebRTC (cameras, mobile devices)
  - 60 FPS Yuan SC550N1 (SDI/HDMI professional sources)
- **Professional Video Capture**:
  - Yuan SC550N1 SDI/HDMI capture card support
  - Smart Tee for simultaneous preview/snapshot/recording
  - YUY2 format optimization (54-70% more efficient than MJPEG)
  - Session 0 Windows Service for hardware access
- **Advanced DICOM Pipeline**:
  - OptimizedDicomConverter supporting YUY2, RGB24, JPEG inputs
  - High-resolution snapshot support with enhanced metadata
  - Medical-grade Secondary Capture workflow
- **Unified Management**:
  - Seamless switching between Yuan and WebRTC sources
  - Integrated queue management for both sources
  - Picture-in-Picture (PIP) support
- **High-Performance IPC**:
  - SharedMemory.CircularBuffer for 60 FPS video streaming
  - Named Pipes for control commands
  - Zero-copy frame transfer between service and UI

## 🛠️ Build Requirements

- Windows 10/11 (Windows Service requires admin privileges)
- .NET 8 SDK (for UI application)
- .NET Framework 4.8 (for Yuan capture service)
- Yuan SC550N1 capture card with drivers
- Visual Studio 2022 (recommended)
- PowerShell (for service management scripts)

## 📦 Quick Start

1. **Build the applications**:
   ```cmd
   build.bat
   ```

2. **Install Yuan Capture Service** (run as Administrator):
   ```powershell
   .\SmartBoxNext.CaptureService\install-service.ps1
   ```

3. **Start the main application**:
   ```cmd
   run.bat
   ```

4. **Configure Yuan Capture**:
   - Connect Yuan SC550N1 to video source (SDI/HDMI)
   - Use "Connect Yuan" in the UI
   - Select input source (SDI/HDMI/Component)
   - Switch between Yuan and WebRTC sources

5. **Configure PACS** (optional):
   - Edit `config.json`
   - Set PACS server details
   - Test connection in settings

## 🔧 Configuration

The application uses a simple JSON configuration file:

```json
{
  "Storage": {
    "PhotosPath": "./Data/Photos",
    "VideosPath": "./Data/Videos",
    "DicomPath": "./Data/DICOM",
    "QueuePath": "./Data/Queue"
  },
  "Pacs": {
    "ServerHost": "your-pacs-server",
    "ServerPort": 104,
    "CalledAeTitle": "PACS",
    "CallingAeTitle": "SMARTBOX"
  }
}
```

## 📁 Project Structure

```
smartbox-wpf-clean/
├── App.xaml.cs                           # Application entry point
├── MainWindow.xaml.cs                    # Main window with WebView2 + Yuan integration
├── WebServer.cs                          # Local web server
├── DicomExporter.cs                      # Legacy DICOM export
├── PacsSender.cs                         # PACS C-STORE implementation
├── QueueManager.cs                       # Persistent queue (JSON-based)
├── QueueProcessor.cs                     # Background queue processor
├── Logger.cs                             # File-based logging
├── AppConfig.cs                          # Configuration models
├── Helpers/
│   └── ProcessHelper.cs                  # WebView2 cleanup utilities
├── Services/                             # New unified capture system
│   ├── UnifiedCaptureManager.cs          # Manages Yuan + WebRTC sources
│   ├── OptimizedDicomConverter.cs        # Enhanced DICOM converter
│   ├── IntegratedQueueManager.cs         # Bridges capture with PACS queue
│   ├── SharedMemoryClient.cs             # IPC with Yuan service
│   └── YUY2Converter.cs                  # High-performance YUY2 conversion
├── SmartBoxNext.CaptureService/          # Yuan capture Windows Service
│   ├── Services/
│   │   ├── CaptureService.cs             # Main Windows Service
│   │   ├── SharedMemoryManager.cs        # IPC producer (60 FPS)
│   │   ├── ControlPipeServer.cs          # Command interface
│   │   ├── YuanCaptureGraph.cs           # DirectShow graph for Yuan
│   │   └── FrameProcessor.cs             # Frame processing pipeline
│   └── install-service.ps1               # Service installation script
├── PowerShell Scripts/
│   ├── fix-locks.ps1                     # WebView2 cleanup
│   ├── build-clean.ps1                   # Clean build helper
│   └── restart-and-build.ps1             # Full restart workflow
└── wwwroot/                              # HTML/CSS/JS UI
    ├── index.html
    ├── app.js
    ├── styles.css
    └── keyboard.js
```

## 🏥 Medical Compliance

- **IEC 62304**: Designed with medical software standards in mind
- **DICOM Compliance**: Full DICOM 3.0 support
- **Audit Trail**: Comprehensive logging for compliance
- **Data Protection**: Patient data never in logs

## 🚨 Troubleshooting

### WebView2 not loading?
- Ensure WebView2 Runtime is installed
- Check Windows Firewall for port 8080
- Look in `logs/` folder for errors

### PACS connection failing?
- Verify AE Titles match PACS configuration
- Check network connectivity to PACS server
- Use "Test Connection" in settings

### Queue not processing?
- Check PACS configuration in `config.json`
- Look for errors in `logs/` folder
- Verify DICOM files exist in queue

### Yuan capture not working?
- Ensure Yuan SC550N1 drivers are installed
- Check if CaptureService is running (Services.msc)
- Verify video source is connected to Yuan card
- Try different input sources (SDI/HDMI)
- Check Windows Event Log for service errors

### Performance issues?
- Monitor CPU usage (target <60% for dual capture)
- Check SharedMemory buffer statistics
- Verify 60 FPS sustained performance
- Consider YUY2 vs RGB format settings

## 🎯 Yuan SC550N1 Key Benefits

- **Professional Input Support**: SDI, HDMI, Component, Composite
- **Medical-Grade Quality**: Uncompressed capture with medical metadata
- **Real-time Performance**: 60 FPS with <10ms latency
- **Session 0 Compatibility**: Works in Windows Service environment
- **DICOM Integration**: Direct YUY2 → DICOM pipeline
- **Simultaneous Operations**: Preview + snapshot + recording

## 📚 Documentation

- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) - Complete implementation roadmap
- [YUAN_INTEGRATION.md](YUAN_INTEGRATION.md) - Technical Yuan integration details
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture overview

## 📝 License

Copyright © 2025 CIRSS Medical Systems. All rights reserved.

---

*Built with ❤️ for medical professionals who need reliable software with professional capture capabilities*