# SmartBox Next - WPF Edition

## 🏥 Medical Image Capture System

A robust, medical-grade image capture system built with WPF and .NET 8, designed for reliability in clinical environments.

## 🚀 Why WPF over WinUI3?

After extensive testing with WinUI3, we made the strategic decision to migrate to WPF:

- **Stability**: No mysterious WinRT.Runtime exceptions
- **Simplicity**: WebView2 integration "just works"
- **Deployment**: Standard Windows application, no MSIX complexity
- **Reliability**: Proven technology for medical applications

## 🏗️ Architecture

```
SmartBoxNext.exe (WPF Shell)
├── WebView2 (Full Window)
├── Local Web Server (Port 5000)
├── Medical Components
│   ├── DICOM Exporter (fo-dicom)
│   ├── PACS Sender (C-STORE)
│   └── Queue Manager (JSON-based)
└── HTML/CSS/JS UI (70 FPS WebRTC)
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
- **70 FPS WebRTC**: Hardware-accelerated video capture
- **DICOM Export**: Full DICOM compliance with fo-dicom
- **PACS Integration**: C-STORE with queue management
- **Remote Access**: Optional web interface for remote management

## 🛠️ Build Requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 (optional)

## 📦 Quick Start

1. **Build the application**:
   ```cmd
   build.bat
   ```

2. **Run the application**:
   ```cmd
   run.bat
   ```

3. **Configure PACS** (optional):
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
smartbox-wpf/
├── App.xaml.cs              # Application entry point
├── MainWindow.xaml.cs       # Main window with WebView2
├── WebServer.cs             # Local web server
├── DicomExporter.cs         # DICOM export functionality
├── PacsSender.cs            # PACS C-STORE implementation
├── QueueManager.cs          # Persistent queue (JSON-based)
├── QueueProcessor.cs        # Background queue processor
├── Logger.cs                # File-based logging
├── AppConfig.cs             # Configuration models
└── wwwroot/                 # HTML/CSS/JS UI
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
- Check Windows Firewall for port 5000
- Look in `logs/` folder for errors

### PACS connection failing?
- Verify AE Titles match PACS configuration
- Check network connectivity to PACS server
- Use "Test Connection" in settings

### Queue not processing?
- Check PACS configuration in `config.json`
- Look for errors in `logs/` folder
- Verify DICOM files exist in queue

## 📝 License

Copyright © 2025 CIRSS Medical Systems. All rights reserved.

---

*Built with ❤️ for medical professionals who need reliable software*