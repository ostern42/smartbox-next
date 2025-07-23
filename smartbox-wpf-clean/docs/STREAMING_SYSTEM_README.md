# SmartBox Medical Real-Time Video Streaming System

## Overview

The SmartBox streaming system provides professional-grade real-time video streaming with advanced controls, authentication, and export capabilities designed specifically for medical imaging applications.

## Features

### Core Capabilities
- **Real-time HLS Streaming**: Industry-standard HTTP Live Streaming with adaptive bitrate
- **DVR Functionality**: Time-shift up to 2 hours of live content with seamless navigation
- **Advanced Navigation**: Frame-accurate seeking, variable playback speeds (0.5x-4x)
- **In/Out Marking**: Set precise timecode markers for important segments
- **Client-Side Export**: Export marked ranges directly from the browser
- **Dual Playback**: Freeze frame capability with independent playback control
- **JWT Authentication**: Secure role-based access control
- **WebSocket Updates**: Real-time stream status and notifications

### Technical Specifications
- **Video Codec**: H.264/AVC (configurable)
- **Container**: MPEG-TS segments with HLS playlist
- **Segment Duration**: 6 seconds (optimized for medical use)
- **Latency**: ~15-20 seconds (standard HLS)
- **Resolution**: Up to 1920x1080 (configurable)
- **Bitrate**: 500kbps - 5Mbps (adaptive)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client Browser                           │
│  ┌─────────────────┐  ┌──────────────┐  ┌────────────────┐ │
│  │ Login Component │  │ Video Player │  │ Export Module  │ │
│  └────────┬────────┘  └──────┬───────┘  └───────┬────────┘ │
│           │                   │                   │          │
└───────────┼───────────────────┼───────────────────┼──────────┘
            │                   │                   │
            ▼                   ▼                   ▼
┌───────────────────────────────────────────────────────────┐
│                  Streaming API (Port 5002)                 │
│  ┌──────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │ Auth Service │  │ HLS Service │  │ Export Service  │  │
│  └──────────────┘  └─────────────┘  └─────────────────┘  │
└───────────────────────────────────────────────────────────┘
            │                   │
            ▼                   ▼
┌───────────────────┐  ┌────────────────────┐
│   JWT Token Store │  │ FFmpeg Transcoder  │
└───────────────────┘  └────────────────────┘
```

## Setup and Configuration

### Prerequisites
- .NET 8.0 or later
- FFmpeg installed and accessible
- Modern web browser with HLS support

### Installation

1. **Add NuGet packages**:
```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.3" />
```

2. **Initialize the streaming manager in your application**:
```csharp
// In MainWindow.xaml.cs or App startup
var streamingManager = new StreamingServerManager(
    logger,
    ffmpegService,
    "C:\\StreamingOutput"
);
await streamingManager.StartAsync();
```

3. **Access the streaming player**:
   - Navigate to: http://localhost:8080/streaming-player.html
   - Default login: admin / SmartBox2024!

## API Reference

### Authentication Endpoints

**POST /api/auth/login**
```json
{
  "username": "admin",
  "password": "SmartBox2024!"
}
```

Response:
```json
{
  "access_token": "eyJ...",
  "refresh_token": "abc123...",
  "expires_in": 28800,
  "user": {
    "username": "admin",
    "role": "Administrator",
    "displayName": "Admin User"
  }
}
```

**POST /api/auth/refresh**
```json
{
  "refresh_token": "abc123..."
}
```

### Streaming Endpoints

**POST /api/stream/start**
```json
{
  "inputType": 0,  // 0=Device, 1=RTMP, 2=File
  "deviceName": "Integrated Camera",
  "enableDVR": true,
  "includeAudio": true,
  "videoBitrate": "2000k",
  "framerate": 30,
  "resolution": "1280x720"
}
```

**GET /api/stream/{sessionId}/stream.m3u8**
- Returns HLS master playlist
- Add `?dvr=true` for full DVR playlist

**GET /api/stream/{sessionId}/segment_XXXXX.ts**
- Returns individual video segments

**POST /api/stream/mark/{sessionId}/in**
```json
{
  "timestamp": 123.45,
  "label": "Critical moment"
}
```

**POST /api/stream/mark/{sessionId}/out**
```json
{
  "timestamp": 156.78
}
```

## User Roles and Permissions

### Administrator
- Full system access
- User management
- Stream configuration
- Export capabilities

### Operator
- Start/stop streams
- Mark in/out points
- Export segments
- View all streams

### Viewer
- View live streams
- Basic playback controls
- No marking or export

## Advanced Features

### Frame-Accurate Navigation
```javascript
// Step forward one frame
player.stepFrame(1);

// Step backward one frame
player.stepFrame(-1);

// Jump to specific timecode
player.currentTime(123.45);
```

### Dual Playback Mode
The freeze frame feature allows operators to:
1. Freeze the current frame in a secondary player
2. Continue watching the live stream
3. Compare different time points
4. Export from either view

### Client-Side Export
Export functionality uses the MediaSource Extensions API to:
1. Extract marked segments
2. Concatenate multiple ranges
3. Download as WebM file
4. Maintain frame accuracy

### Buffering Strategy
The player implements intelligent buffering:
- Pre-buffer 3 segments ahead
- Maintain 30-second buffer
- Adaptive quality switching
- Seamless seek optimization

## Performance Optimization

### Server-Side
- Hardware-accelerated encoding when available
- Segment caching with automatic cleanup
- Connection pooling for concurrent streams
- Efficient memory-mapped file handling

### Client-Side
- Progressive loading of segments
- Web Worker for export processing
- RequestAnimationFrame for smooth playback
- Efficient DOM updates

## Security Considerations

### Authentication
- JWT tokens with 8-hour expiration
- Refresh tokens with 7-day expiration
- Secure password hashing (PBKDF2)
- Role-based access control

### Network Security
- CORS configuration
- HTTPS recommended for production
- Token rotation on refresh
- Session isolation

## Troubleshooting

### Common Issues

**Stream not starting**
- Check FFmpeg is installed: `ffmpeg -version`
- Verify camera permissions
- Check firewall settings for ports 5002

**Playback stuttering**
- Reduce video bitrate
- Check network bandwidth
- Enable hardware acceleration

**Authentication failures**
- Clear browser localStorage
- Check token expiration
- Verify API endpoint

### Debug Mode
Enable detailed logging:
```csharp
LogLevel = LogLevel.Debug
```

View browser console for player events:
```javascript
player.on('error', (e) => console.error('Player error:', e));
```

## Integration Examples

### Starting a stream from C#
```csharp
var input = new StreamInput
{
    InputType = StreamInputType.Device,
    DeviceName = "Yuan SC550N1"
};

var options = new StreamingOptions
{
    EnableDVR = true,
    VideoBitrate = "3000k",
    Resolution = "1920x1080"
};

var session = await streamingService.StartStreamingSessionAsync(
    Guid.NewGuid().ToString(),
    input,
    options
);
```

### Embedding the player
```html
<iframe 
  src="http://localhost:8080/streaming-player.html" 
  width="100%" 
  height="600"
  frameborder="0">
</iframe>
```

### WebSocket integration
```javascript
const ws = new WebSocket('ws://localhost:8081');
ws.onmessage = (event) => {
  const message = JSON.parse(event.data);
  if (message.type === 'segmentCreated') {
    console.log('New segment:', message.data);
  }
};
```

## Future Enhancements

- WebRTC for sub-second latency
- Multi-bitrate adaptive streaming
- Cloud storage integration
- AI-powered scene detection
- Automated highlight generation
- DICOM video export
- Multi-camera support
- Real-time collaboration tools

## License

This streaming system is part of the SmartBox Next medical imaging platform and is subject to medical device regulations and compliance requirements.