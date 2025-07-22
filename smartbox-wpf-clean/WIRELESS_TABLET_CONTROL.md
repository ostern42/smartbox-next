# SmartBox-Next Wireless Tablet Control Interface

This document describes the wireless tablet control interface for SmartBox-Next, enabling remote administration and control of the medical imaging system via tablet or other wireless devices.

## Overview

The wireless tablet control interface provides comprehensive remote control capabilities for medical professionals who need to operate SmartBox-Next from a distance. This is particularly useful in sterile environments, during procedures where the operator cannot directly access the main system, or for supervisory oversight.

## Architecture

### Components

1. **WebSocketServer.cs** - Real-time communication server
2. **admin.html** - Main admin control interface  
3. **admin-control.js** - JavaScript client-side logic
4. **admin-demo.html** - Demonstration interface with simulated data

### Communication Flow

```
Tablet/Wireless Device (admin.html)
    ↕ WebSocket Connection (Port 5001)
SmartBox Main Application (MainWindow.xaml.cs)
    ↕ Internal Communication
Medical Capture Services & Hardware
```

## Features

### 1. Live Stream Monitoring
- **Real-time video feed display** with status overlays
- **Recording indicators** showing current capture state
- **Stream quality information** and resolution details
- **Recording duration** and file size tracking

### 2. Patient Management
- **Patient worklist display** from MWL service integration
- **Remote patient selection** and switching
- **Patient information overview** with modality details
- **Refresh patient list** functionality

### 3. Recording Controls
- **Start/Stop recording** with quality settings
- **Photo capture** triggering
- **Critical moment marking** for timeline annotation
- **Quality adjustment** (video resolution, audio levels)
- **Emergency stop** functionality

### 4. System Monitoring
- **Real-time system status**:
  - CPU usage monitoring
  - Memory consumption tracking
  - Storage space utilization
  - Recording metrics (duration, file size, frame rate)
- **Connection health** monitoring
- **Service status** indicators

### 5. Queue Management
- **Export queue monitoring** with status indicators
- **Queue processing** control
- **PACS upload** status tracking
- **Queue statistics** display

### 6. Administrative Controls
- **System diagnostics** execution
- **Cache management** (clear temporary files)
- **Service restart** capabilities
- **System shutdown** (with confirmation)
- **Export operations** triggering

## Technical Implementation

### WebSocket Communication

#### Message Protocol
All communication uses JSON messages with the following structure:

```json
{
  "type": "message_type",
  "data": { /* message-specific data */ },
  "timestamp": "2025-01-13T10:30:00.000Z"
}
```

#### Message Types

**Admin → System:**
- `get_system_status` - Request current system status
- `get_patient_list` - Request patient worklist
- `select_patient` - Switch to specified patient
- `start_recording` - Begin recording with quality settings
- `stop_recording` - Stop current recording
- `capture_photo` - Trigger photo capture
- `mark_critical` - Mark critical moment in timeline
- `emergency_stop` - Emergency stop all operations
- `process_queue` - Start queue processing
- `run_diagnostics` - Execute system diagnostics

**System → Admin:**
- `system_status` - System metrics and status
- `recording_state` - Current recording information
- `patient_list` - Available patients from MWL
- `queue_update` - Export queue status
- `status_update` - Real-time status changes
- `ack` - Command acknowledgment

### Security Considerations

1. **Network Security**:
   - WebSocket connections on localhost only by default
   - Can be configured for specific network interfaces
   - No authentication required for localhost connections
   - Consider VPN or secure network for remote access

2. **Medical Grade Controls**:
   - Emergency stop functionality
   - Confirmation dialogs for critical operations
   - System health monitoring
   - Graceful error handling

3. **Data Privacy**:
   - No patient data transmitted in clear text
   - Minimal patient identifiers in communications
   - DICOM data remains on local system

## User Interface Design

### Layout (Optimized for Tablets)

```
┌─────────────────────────────────────────────────────────┐
│ Header: Title + Connection Status                       │
├─────────────────┬───────────┬───────────┬──────────────┤
│                 │           │ System    │ Export       │
│  Live Stream    │ Patient & │ Status    │ Queue        │
│  View           │ Controls  │ Monitor   │ Management   │
│                 │           │           │              │
├─────────────────┼───────────┼───────────┼──────────────┤
│ System          │           │ Recording │ Quick        │
│ Monitor         │           │ Controls  │ Actions      │
│ Details         │           │           │              │
└─────────────────┴───────────┴───────────┴──────────────┘
```

### Responsive Design

- **Landscape tablets (1024px+)**: Full grid layout
- **Portrait tablets (768px)**: Stacked 2-column layout  
- **Mobile phones**: Single column layout

### Touch-Friendly Interface

- **Large touch targets** (48px minimum)
- **Medical glove compatibility**
- **Clear visual feedback** for all interactions
- **Emergency controls** with prominent styling
- **Status indicators** with color coding

## Installation and Setup

### 1. Server Configuration

The WebSocket server starts automatically with the main SmartBox application:

```csharp
// In MainWindow.xaml.cs InitializeApplication()
var webSocketLogger = _loggerFactory.CreateLogger<WebSocketServer>();
_webSocketServer = new WebSocketServer(webSocketLogger, _config.Application.WebServerPort + 1);
_webSocketServer.AdminMessageReceived += OnAdminMessageReceived;
await _webSocketServer.StartAsync();
```

### 2. Network Access

**Default Configuration:**
- WebSocket Server: `ws://localhost:5001`
- Admin Interface: `http://localhost:5000/admin.html`
- Demo Interface: `http://localhost:5000/admin-demo.html`

**For Network Access:**
1. Update server bindings in `WebSocketServer.cs`
2. Configure firewall rules for ports 5000-5001
3. Ensure network security policies allow access

### 3. Tablet Setup

1. **Open web browser** on tablet device
2. **Navigate to** `http://[smartbox-ip]:5000/admin.html`
3. **Verify connection** (green indicator in header)
4. **Test controls** with non-critical operations first

## Usage Instructions

### 1. Initial Connection

1. Start SmartBox-Next main application
2. Open admin interface on tablet
3. Verify "Connected" status in header
4. Refresh patient list if needed

### 2. Patient Selection

1. View patient list in Controls panel
2. Click desired patient to select
3. Confirm selection in main application
4. Begin imaging workflow

### 3. Recording Operations

1. Adjust quality settings if needed
2. Click "Start Recording" to begin
3. Monitor recording status in Live Stream panel
4. Use "Capture Photo" for still images
5. "Mark Critical" for important moments
6. "Stop Recording" when complete

### 4. System Monitoring

- **Monitor system resources** in Status panel
- **Check recording metrics** in Monitor panel
- **Watch export queue** in Queue panel
- **Use diagnostics** if issues arise

### 5. Emergency Procedures

- **Emergency Stop**: Immediately stops all recording
- **System Shutdown**: Graceful system shutdown
- **Service Restart**: Restart capture services

## Troubleshooting

### Connection Issues

**Symptom**: Red connection indicator
**Solutions**:
1. Check main SmartBox application is running
2. Verify network connectivity
3. Check firewall settings
4. Restart WebSocket server

**Symptom**: Interface not responsive
**Solutions**:
1. Refresh browser page
2. Check console for JavaScript errors
3. Verify WebSocket port (5001) is accessible
4. Check server logs for errors

### Control Issues

**Symptom**: Commands not executing
**Solutions**:
1. Verify connection status
2. Check main application logs
3. Ensure proper permissions
4. Try emergency stop and restart

### Performance Issues

**Symptom**: Slow response times
**Solutions**:
1. Check network latency
2. Monitor system resources
3. Reduce update frequency if needed
4. Check for background processes

## Demo Mode

For testing and demonstration purposes, use `admin-demo.html`:

- **Simulated data** updates automatically
- **All controls functional** with visual feedback
- **No backend connection** required
- **Ideal for training** and interface testing

Access via: `http://localhost:5000/admin-demo.html`

## Integration Points

### MWL Service Integration

```csharp
// In HandleGetPatientList()
if (_mwlService != null)
{
    var worklistItems = await _mwlService.GetWorklistAsync();
    // Convert to admin interface format
}
```

### Recording Service Integration

```csharp
// In HandleStartRecording()
if (_unifiedCaptureManager != null)
{
    await _unifiedCaptureManager.StartRecordingAsync(settings);
}
```

### Queue Service Integration

```csharp
// In HandleProcessQueue()
if (_queueProcessor != null)
{
    _queueProcessor.Start();
}
```

## Development Notes

### Adding New Controls

1. **Add message type** in `AdminMessage` handling
2. **Implement handler** in `MainWindow.xaml.cs`
3. **Add UI control** in `admin.html`
4. **Add JavaScript handler** in `admin-control.js`

### Extending Monitoring

1. **Add system metric** in `GetSystemStatus()`
2. **Update status display** in admin interface
3. **Configure update frequency** as needed

### Custom Integrations

The WebSocket server can be extended to integrate with:
- **Third-party PACS systems**
- **Hospital information systems**
- **Custom medical devices**
- **Workflow management systems**

## Future Enhancements

### Planned Features

1. **Multi-user support** with role-based access
2. **Historical data viewing** and analysis
3. **Remote diagnostic tools** expansion
4. **Mobile app development** for iOS/Android
5. **Voice control integration** for hands-free operation
6. **Audit logging** for regulatory compliance

### Technical Improvements

1. **Enhanced security** with authentication
2. **Bandwidth optimization** for slow networks
3. **Offline mode** support
4. **Real-time video streaming** integration
5. **Performance analytics** and reporting

## Support and Maintenance

### Log Files

Monitor these log sources for admin interface issues:
- **Main application logs**: `logs/smartbox-*.log`
- **Browser console**: Developer tools in tablet browser
- **WebSocket server logs**: Included in main application logs

### Regular Maintenance

1. **Monitor connection stability**
2. **Check system resource usage**
3. **Update browser software** on tablets
4. **Test emergency procedures** regularly
5. **Backup configuration settings**

---

**Document Version**: 1.0  
**Last Updated**: January 13, 2025  
**Author**: SmartBox Development Team