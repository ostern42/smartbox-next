# SmartBox Current State - July 10, 2025, 13:20

## Project Status
Medical imaging capture system with Yuan SC550N1 integration, DICOM export, and PACS connectivity.

## Working Features
✅ Yuan SC550N1 capture card integration (Phase 0-5 complete)
✅ WebView2 UI with settings management
✅ DICOM export with patient context
✅ PACS C-STORE implementation with queue
✅ MWL (Modality Worklist) query support
✅ Diagnostic connection testing windows
✅ Multi-theme and layout system foundation
✅ Folder selection dialogs
✅ Touch keyboard support

## Configuration
```json
{
  "Window": "1920x1080",
  "Layout": "1:2 (preview:worklist)",
  "AutoExportDicom": true,
  "PACS": "localhost:104",
  "MWL": "localhost:105",
  "Theme": "Default Light"
}
```

## Known Issues
🔴 **Patient info form** - Still single column (CSS ready, HTML needs update)
🔴 **PACS upload trigger** - May not be working on photo capture
🟡 **Worklist format** - Requires DICOM .wl files, not JSON
🟡 **Save settings error** - Intermittent "Unknown error" messages

## File Structure
```
smartbox-wpf-clean/
├── MainWindow.xaml.cs       # Main application logic
├── DiagnosticWindow.xaml    # Connection testing UI
├── wwwroot/
│   ├── index.html          # Main UI
│   ├── settings.html       # Settings UI
│   ├── styles.css          # Main styles
│   ├── settings.css        # Settings styles
│   └── js/
│       └── layout-manager.js # Layout system
├── Services/
│   ├── UnifiedCaptureManager.cs
│   ├── IntegratedQueueManager.cs
│   └── SharedMemoryClient.cs
└── bin/Debug/              # Build output
```

## Quick Commands
```bash
# Build and run
dotnet build
dotnet run

# Test Orthanc
curl http://localhost:8042/system

# Add test patient
add-test-patient-to-orthanc.bat
```

## Immediate TODO
1. Apply form-row divs to patient info HTML
2. Debug PACS upload trigger in HandlePhotoCaptured
3. Add console logging for queue operations
4. Test full capture → DICOM → PACS flow