# Changelog

All notable changes to SmartBox-Next will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Session 8 - 2025-01-07 (Video Capture Deep Dive)

#### Added
- CameraAnalyzer.cs - Deep hardware analysis tool
- DirectShowCapture.cs - Professional capture exploration
- "Analyze Camera" button for detailed hardware report
- Professional video capture research documentation
- Media Foundation + GPU acceleration plan
- NuGet packages: Vortice.Windows, FlashCap, FFmpeg.AutoGen

#### Fixed
- YUY2 to BGRA8 format conversion in OnHighPerfFrameArrived
- Identified root cause of poor performance (format mismatch)

#### Changed
- Understanding that MediaCapture is consumer-grade, not suitable
- Decision to use Media Foundation instead of DirectShow
- Consolidated planning/specs folders into .wisdom
- Added "STRUCTURES FIRST!" principle to wisdom

#### Discovered
- Camera outputs YUY2, SoftwareBitmapSource needs BGRA8
- Media Foundation is actively developed, DirectShow deprecated
- GPU acceleration reduces CPU from 30% to 5-10%
- Professional software uses hardware-level APIs
- 60 FPS is achievable with proper architecture

#### Research Results
- Comprehensive analysis of video capture approaches
- Media Foundation + GPU is optimal solution
- SwapChainPanel for zero-copy display
- Circular buffers for 24/7 operation
- Medical-grade reliability patterns

### Session 6 - 2025-01-06 (WinUI3 Version)

#### Added
- MediaFrameReader implementation for high-performance video preview (25-60 FPS target)
- Automatic fallback mechanism between MediaFrameReader and timer-based preview
- FPS counter for real-time performance monitoring
- Enhanced debug output for frame source troubleshooting
- Comprehensive research documentation for video capture options
- Community help request template for MediaFrameReader issues

#### Fixed
- DicomExporter.cs: Fixed DicomPixelData instantiation using Create method
- MainWindow.xaml.cs: Fixed DateTimeOffset nullable handling for DatePicker
- PacsSettings.cs: Fixed nullable field initialization warning
- MainWindow.xaml.cs: Fixed async/await usage in DispatcherQueue.TryEnqueue

#### Changed
- Video preview now attempts high-performance MediaFrameReader first
- Debug panel now shows current capture mode and FPS
- Improved error handling and logging for video initialization

#### Known Issues
- MediaFrameReader starts successfully but OnFrameArrived events not firing
- Possible conflict when using MediaCapture for both preview and capture
- Preview remains at 5-10 FPS until MediaFrameReader issue resolved

### Session 5 - 2025-01-06 (WinUI3 Version)

#### Added
- DICOM export functionality with full medical imaging compliance
- PACS integration with C-STORE protocol support
- PACS settings dialog with connection testing
- Patient information capture dialog
- Debug info panel with performance metrics

#### Changed
- Improved UI with patient information form
- Added millisecond timestamps to debug messages
- Limited debug messages to prevent UI slowdown

#### Fixed
- Timer-based preview approach for stable 5-10 FPS
- DispatcherQueue usage for UI updates

### Sessions 3-4 - 2025-01-06 (WinUI3 Rewrite)

#### Added
- Complete rewrite in WinUI3/.NET 8
- Basic webcam preview functionality
- Photo capture to Pictures/SmartBoxNext
- Modern Windows 11 UI

#### Changed
- Switched from Go/Wails to C#/WinUI3
- New project structure for Windows-native development

### Original Go/Wails Implementation

#### Added
- PACS integration with C-STORE support (stub implementation)
- Resilient configuration management with multiple backup locations
- Persistent upload queue that survives power loss
- Emergency patient templates (Notfall männlich/weiblich/Kind)
- Queue priority system for emergency cases
- Remote configuration management capability
- Resource monitoring (memory/disk) for resilient operation
- Exponential backoff for failed uploads

#### Technical
- Config stored in ~/SmartBoxNext/config.json with backups
- Queue persisted in ~/SmartBoxNext/Queue/queue.json
- Atomic file writes prevent corruption
- Ready for Go DICOM library integration

## [0.1.0] - 2025-07-06

### Added
- Initial Wails application setup
- Webcam preview with getUserMedia
- Image capture to canvas
- Patient information form (Name, ID, Birth Date, Sex)
- Study information form (Description, Accession Number)
- DICOM export functionality with JPEG compression
- Multiple DICOM writer implementations:
  - simple_dicom.go - Initial MVP
  - jpeg_dicom.go - Working JPEG compression (59KB files)
  - working_dicom.go - RGB conversion approach
  - cambridge_style_dicom.go - Based on CamBridge v1
- Compact UI layout (AppCompact.vue)
- Exit button functionality
- Output directory: ~/SmartBoxNext/DICOM/

### Fixed
- DICOM compatibility with MicroDicom viewer (after 10 attempts!)
- Proper JPEG compression in DICOM format
- Transfer Syntax handling for compressed images

### Technical Details
- Frontend: Vue 3 with Vite
- Backend: Go with Wails v2
- DICOM: Custom implementation (no external DICOM libraries)
- File sizes: ~59KB for compressed JPEG DICOM

### Known Issues
- Camera selection not yet implemented (hardcoded to first camera)
- No overlay functionality yet
- No trigger system integration

### Session Notes
- Session 1 (2025-07-05 evening - 2025-07-06 02:49): Initial development, DICOM struggles, final success
- Session 2 (2025-07-06 afternoon): Documentation, realization that DICOM already works