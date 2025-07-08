# SmartBox Next - Portable Edition

## Quick Start
1. Run "Start SmartBox.bat" or SmartBoxNext.exe
2. The application will start in fullscreen mode
3. Click "Init Webcam" to begin

## Directory Structure
- `/Data/Photos` - Captured photos
- `/Data/Videos` - Recorded videos  
- `/Data/DICOM` - DICOM exports
- `/logs` - Application logs
- `/assets` - Runtime dependencies (do not modify)
- `/wwwroot` - Web interface files

## Configuration
Edit `config.json` to change settings or use the Settings button in the app.

## Requirements
- Windows 10/11 (64-bit)
- .NET 8 Runtime (included)
- WebView2 Runtime (auto-downloads if needed)
- Webcam device

## Support
Logs are stored in the `/logs` folder with daily rotation.
