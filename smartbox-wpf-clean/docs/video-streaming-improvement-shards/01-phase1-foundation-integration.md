# Phase 1: Foundation Integration (Priority: High)

This phase establishes the core connection between the streaming player and the new FFmpeg video engine API, implementing real-time capabilities and unified thumbnail management.

## 1.1 Connect Streaming Player to FFmpeg API

**File**: `streaming-player.js`

### Enhanced Video Engine Integration

```javascript
// Enhanced video-engine integration
class EnhancedStreamingPlayer extends StreamingPlayer {
    constructor(container, options) {
        super(container, options);
        this.videoEngine = new VideoEngineClient();
        this.setupEngineIntegration();
    }
    
    async setupEngineIntegration() {
        // Connect to FFmpeg engine
        this.videoEngine.on('segmentCompleted', this.onSegmentCompleted.bind(this));
        this.videoEngine.on('thumbnailReady', this.onThumbnailReady.bind(this));
        
        // Initialize WebSocket for real-time updates
        if (this.options.sessionId) {
            await this.connectToSession(this.options.sessionId);
        }
    }
    
    async connectToSession(sessionId) {
        // Use video engine's WebSocket support
        this.videoEngine.connectWebSocket(`ws://${window.location.host}/ws/video/${sessionId}`);
    }
    
    onSegmentCompleted(segment) {
        // Update timeline with new segment
        this.timeline.addSegment(segment);
        
        // Update HLS playlist if in live mode
        if (this.isLive) {
            this.refreshPlaylist();
        }
    }
    
    onThumbnailReady(data) {
        // Update timeline thumbnail
        this.timeline.updateThumbnail(data.timestamp, data.url);
    }
}
```

### Key Features
- Direct integration with VideoEngineClient
- Event-driven architecture for real-time updates
- Session-based WebSocket connection
- Automatic playlist refresh for live streaming

## 1.2 WebSocket Real-time Updates

**File**: `streaming-websocket-handler.js` (New)

### Real-time Streaming Updates Handler

```javascript
// Real-time streaming updates handler
class StreamingWebSocketHandler {
    constructor(player) {
        this.player = player;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000;
    }
    
    connect(sessionId) {
        this.sessionId = sessionId;
        this.url = `ws://${window.location.host}/ws/video/${sessionId}`;
        this.establishConnection();
    }
    
    establishConnection() {
        this.ws = new WebSocket(this.url);
        
        this.ws.onopen = () => {
            console.log('Streaming WebSocket connected');
            this.reconnectAttempts = 0;
            this.player.emit('connected');
        };
        
        this.ws.onmessage = (event) => {
            const message = JSON.parse(event.data);
            this.handleMessage(message);
        };
        
        this.ws.onerror = (error) => {
            console.error('WebSocket error:', error);
            this.player.emit('error', error);
        };
        
        this.ws.onclose = () => {
            console.log('WebSocket disconnected');
            this.handleDisconnection();
        };
    }
    
    handleMessage(message) {
        switch (message.type) {
            case 'SegmentCompleted':
                this.player.onNewSegment(message.data);
                break;
            case 'RecordingStatus':
                this.player.updateRecordingStatus(message.data);
                break;
            case 'ThumbnailReady':
                this.player.timeline.updateThumbnail(message.data);
                break;
            case 'MarkerAdded':
                this.player.timeline.addMarker(message.data);
                break;
            case 'Error':
                this.player.handleStreamError(message.data);
                break;
        }
    }
    
    handleDisconnection() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
            
            setTimeout(() => {
                console.log(`Attempting reconnection ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
                this.establishConnection();
            }, delay);
        } else {
            this.player.emit('connectionFailed');
        }
    }
}
```

### Key Features
- Automatic reconnection with exponential backoff
- Message type handling for various streaming events
- Error handling and recovery
- Connection state management

## 1.3 Unified Thumbnail System

**Updates to**: `streaming-player.js`

### FFmpeg Engine Thumbnail API Integration

```javascript
// Use FFmpeg engine's thumbnail API
async loadThumbnail(timestamp) {
    if (this.videoEngine && this.sessionId) {
        try {
            // Use API endpoint for thumbnails
            const thumbnailUrl = await this.videoEngine.getThumbnail(timestamp, 160);
            return thumbnailUrl;
        } catch (error) {
            console.error('Failed to load thumbnail from API:', error);
            // Fallback to video frame extraction
            return this.extractVideoFrame(timestamp);
        }
    }
    
    // Legacy thumbnail generation
    return this.generateThumbnailFromVideo(timestamp);
}
```

### Key Features
- Direct API integration for thumbnail generation
- Automatic fallback to video frame extraction
- Session-aware thumbnail loading
- Consistent thumbnail sizing (160px width)

## Implementation Checklist

- [ ] Extend StreamingPlayer class with video engine integration
- [ ] Create StreamingWebSocketHandler for real-time updates
- [ ] Implement message handling for all streaming events
- [ ] Add automatic reconnection logic
- [ ] Integrate thumbnail API with fallback support
- [ ] Test WebSocket connection stability
- [ ] Verify segment update flow
- [ ] Validate thumbnail loading performance

## Dependencies

- `VideoEngineClient` class
- WebSocket API
- HLS.js library
- Existing StreamingPlayer implementation

## Testing Points

1. **Connection Management**
   - WebSocket connection establishment
   - Automatic reconnection on failure
   - Session persistence

2. **Real-time Updates**
   - Segment completion notifications
   - Recording status updates
   - Thumbnail availability

3. **Error Handling**
   - Connection failures
   - API errors
   - Fallback mechanisms

## Navigation

- [← Overview](00-overview-foundation.md)
- [Phase 2: Timeline Consolidation →](02-phase2-timeline-consolidation.md)