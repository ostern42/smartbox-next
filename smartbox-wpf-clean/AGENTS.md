# SmartBoxNext WPF Medical Imaging Application

## Overview
SmartBoxNext is a WPF-based medical imaging application with a modern web UI powered by WebView2. It's designed for touch-first interaction while maintaining full mouse/keyboard support.

## Key Architecture Points
- WPF application with WebView2 for modern web UI
- Touch-first design that works with mouse input
- DICOM/PACS integration using fo-dicom library
- Consolidated single codebase (no separate touch files)
- Real-time webcam capture for medical imaging
- Modality Worklist (MWL) integration

## Important Files
- `/wwwroot/index.html` - Main UI (NOT index_touch.html)
- `/wwwroot/styles.css` - All styles including touch
- `/wwwroot/app.js` - Main application logic
- `/wwwroot/js/mode_manager.js` - Handles app modes and patient state
- `/wwwroot/js/touch_dialogs.js` - Touch-optimized dialog system
- `/Services/DicomService.cs` - DICOM conversion
- `/Services/PacsService.cs` - PACS communication
- `/MainWindow.xaml.cs` - WebView2 message handling
- `/AppConfig.cs` - Configuration management

## Common Issues & Solutions

### 1. Missing Icons
- **Problem**: Segoe Fluent Icons may not load
- **Solution**: Use Unicode symbols as fallback (✕ for close, ⚙ for settings)

### 2. Dialog Button Order
- **Problem**: Exit dialogs need special button arrangement in German UIs
- **Solution**: Use `showExitConfirmation()` which places "Beenden" (danger) on LEFT

### 3. Duplicate Event Handlers
- **Problem**: Multiple components may register same events
- **Solution**: Check both app.js and mode_manager.js for event listeners

### 4. WebView2 Messages
- **Problem**: Communication between C# and JavaScript
- **Solution**: Always use `JSON.stringify()` and correct message types:
  - `exitApp` (not `exit`)
  - `openSettings`
  - `exportCaptures`

### 5. Export Not Finding Photos
- **Problem**: Export reports "0 Aufnahmen erfolgreich exportiert"
- **Solution**: Check:
  - Photos exist in `./Data/Photos/` directory
  - PatientInfo is properly passed
  - Capture type matches photo filenames

## Testing Commands
- **Build**: Use Visual Studio or `dotnet build`
- **Debug**: F12 Developer Console in the app
- **PACS Testing**: 
  1. Enable PACS in settings
  2. Set correct server details
  3. Check Data/Photos directory for images
  4. Monitor logs for DICOM conversion

## Message Flow
1. JavaScript (app.js) → C# (MainWindow.xaml.cs): User actions
2. C# → JavaScript: Results and state updates
3. Important message types:
   - `loadMWL` - Request patient worklist
   - `capturePhoto` - Save photo capture
   - `exportCaptures` - Send to PACS
   - `openSettings` - Navigate to settings
   - `exitApp` - Close application

## UI Conventions (German Medical Software)
- Dangerous actions (Exit/Delete) go on the LEFT
- Confirmation/Safe actions go on the RIGHT
- Red color for destructive actions
- Proper grammar for singular/plural (1 Aufnahme vs. 2 Aufnahmen)

## Recent Changes (July 2025)
- Implemented real PACS export functionality
- Consolidated touch and non-touch UI files
- Fixed exit dialog button arrangement
- Added Settings button to header
- Fixed various compilation errors and UI bugs