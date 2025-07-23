// Video Engine Client - Frontend integration for FFmpeg backend
class VideoEngineClient {
    constructor() {
        this.baseUrl = '/api/video';
        this.currentSession = null;
        this.ws = null;
        this.previewPlayer = null;
        this.eventHandlers = new Map();
        this.hlsConfig = {
            enableWorker: true,
            lowLatencyMode: true,
            liveBackBufferLength: 0,
            liveSyncDuration: 0,
            liveMaxLatencyDuration: 5,
            liveDurationInfinity: true,
            highBufferWatchdogPeriod: 1,
            maxBufferLength: 2,
            maxMaxBufferLength: 5,
            manifestLoadingTimeOut: 10000,
            manifestLoadingMaxRetry: 3,
            manifestLoadingRetryDelay: 500
        };
    }
    
    async initialize() {
        try {
            // Get available sources
            const sources = await this.getVideoSources();
            
            if (sources.length === 0) {
                console.warn('No video sources available');
                return null;
            }
            
            // Auto-select best source
            const preferredSource = sources.find(s => s.type === 'MedicalGrabber' && s.isAvailable) ||
                                   sources.find(s => s.type === 'Webcam' && s.isAvailable) ||
                                   sources.find(s => s.isAvailable) ||
                                   sources[0];
            
            if (preferredSource) {
                await this.selectSource(preferredSource.id);
                console.log('Video engine initialized with source:', preferredSource);
            }
            
            return preferredSource;
        } catch (error) {
            console.error('Failed to initialize video engine:', error);
            throw error;
        }
    }
    
    async getVideoSources() {
        const response = await fetch(`${this.baseUrl}/sources`);
        if (!response.ok) {
            throw new Error(`Failed to get video sources: ${response.statusText}`);
        }
        return await response.json();
    }
    
    async selectSource(sourceId) {
        const response = await fetch(`${this.baseUrl}/sources/${sourceId}/select`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            throw new Error(`Failed to select video source: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    async startRecording(config) {
        const response = await fetch(`${this.baseUrl}/recording/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                patientId: config.patientId || 'default',
                studyId: config.studyId,
                masterCodec: config.lossless ? 'FFV1' : 'H264',
                resolution: config.resolution || '1920x1080',
                frameRate: config.frameRate || 60,
                pixelFormat: config.pixelFormat || 'yuv422p',
                preRecordSeconds: config.preRecordSeconds || 60,
                enablePreview: config.enablePreview !== false,
                previewBitrate: config.previewBitrate || 5000
            })
        });
        
        if (!response.ok) {
            const error = await response.text();
            throw new Error(`Failed to start recording: ${error}`);
        }
        
        const session = await response.json();
        this.currentSession = session;
        
        // Setup preview player if preview is enabled
        if (session.previewUrl) {
            await this.setupPreview(session.previewUrl);
        }
        
        // Connect WebSocket for real-time updates
        if (session.webSocketUrl) {
            this.connectWebSocket(session.webSocketUrl);
        }
        
        // Emit recording started event
        this.emit('recordingStarted', session);
        
        return session;
    }
    
    async stopRecording() {
        if (!this.currentSession) {
            throw new Error('No active recording session');
        }
        
        const response = await fetch(`${this.baseUrl}/recording/${this.currentSession.sessionId}/stop`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            throw new Error(`Failed to stop recording: ${response.statusText}`);
        }
        
        const result = await response.json();
        
        // Clean up
        this.cleanup();
        
        // Emit recording stopped event
        this.emit('recordingStopped', result);
        
        this.currentSession = null;
        
        return result;
    }
    
    async pauseRecording() {
        if (!this.currentSession) {
            throw new Error('No active recording session');
        }
        
        const response = await fetch(`${this.baseUrl}/recording/${this.currentSession.sessionId}/pause`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            throw new Error(`Failed to pause recording: ${response.statusText}`);
        }
        
        this.emit('recordingPaused');
        return await response.json();
    }
    
    async resumeRecording() {
        if (!this.currentSession) {
            throw new Error('No active recording session');
        }
        
        const response = await fetch(`${this.baseUrl}/recording/${this.currentSession.sessionId}/resume`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            throw new Error(`Failed to resume recording: ${response.statusText}`);
        }
        
        this.emit('recordingResumed');
        return await response.json();
    }
    
    async getRecordingStatus() {
        if (!this.currentSession) {
            return null;
        }
        
        const response = await fetch(`${this.baseUrl}/recording/${this.currentSession.sessionId}/status`);
        
        if (!response.ok) {
            throw new Error(`Failed to get recording status: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    async setupPreview(previewUrl) {
        const video = document.getElementById('webcamPreviewLarge');
        if (!video) {
            console.warn('Preview video element not found');
            return;
        }
        
        // Clean up existing player
        if (this.previewPlayer) {
            this.previewPlayer.destroy();
            this.previewPlayer = null;
        }
        
        // Check if HLS.js is supported
        if (typeof Hls !== 'undefined' && Hls.isSupported()) {
            this.previewPlayer = new Hls(this.hlsConfig);
            
            // Handle HLS events
            this.previewPlayer.on(Hls.Events.MANIFEST_PARSED, () => {
                video.play().catch(e => {
                    console.warn('Auto-play prevented:', e);
                    // Show play button or user interaction prompt
                    this.emit('playbackBlocked');
                });
            });
            
            this.previewPlayer.on(Hls.Events.ERROR, (event, data) => {
                if (data.fatal) {
                    switch (data.type) {
                        case Hls.ErrorTypes.NETWORK_ERROR:
                            console.error('Fatal network error, trying to recover...');
                            this.previewPlayer.startLoad();
                            break;
                        case Hls.ErrorTypes.MEDIA_ERROR:
                            console.error('Fatal media error, trying to recover...');
                            this.previewPlayer.recoverMediaError();
                            break;
                        default:
                            console.error('Fatal error, cannot recover:', data);
                            this.previewPlayer.destroy();
                            this.emit('previewError', data);
                            break;
                    }
                }
            });
            
            // Load the stream
            this.previewPlayer.loadSource(previewUrl);
            this.previewPlayer.attachMedia(video);
        } else if (video.canPlayType('application/vnd.apple.mpegurl')) {
            // Native HLS support (Safari)
            video.src = previewUrl;
            video.addEventListener('loadedmetadata', () => {
                video.play().catch(e => {
                    console.warn('Auto-play prevented:', e);
                    this.emit('playbackBlocked');
                });
            });
        } else {
            console.error('HLS is not supported in this browser');
            this.emit('previewError', { message: 'HLS not supported' });
        }
    }
    
    connectWebSocket(url) {
        try {
            this.ws = new WebSocket(url);
            
            this.ws.onopen = () => {
                console.log('Video WebSocket connected');
                this.emit('websocketConnected');
            };
            
            this.ws.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    this.handleWebSocketMessage(data);
                } catch (error) {
                    console.error('Failed to parse WebSocket message:', error);
                }
            };
            
            this.ws.onerror = (error) => {
                console.error('Video WebSocket error:', error);
                this.emit('websocketError', error);
            };
            
            this.ws.onclose = () => {
                console.log('Video WebSocket disconnected');
                this.emit('websocketDisconnected');
            };
        } catch (error) {
            console.error('Failed to connect WebSocket:', error);
            this.emit('websocketError', error);
        }
    }
    
    handleWebSocketMessage(data) {
        switch (data.type) {
            case 'SegmentCompleted':
                this.onSegmentCompleted(data.data);
                break;
            case 'RecordingStatus':
                this.onStatusUpdate(data.data);
                break;
            case 'ThumbnailReady':
                this.onThumbnailReady(data.data);
                break;
            case 'MarkerAdded':
                this.emit('markerAdded', data.data);
                break;
            case 'Error':
                this.onError(data.data);
                break;
            case 'Warning':
                this.emit('warning', data.data);
                break;
            default:
                console.log('Unhandled WebSocket message type:', data.type);
        }
    }
    
    onSegmentCompleted(segment) {
        console.log('Segment completed:', segment);
        this.emit('segmentCompleted', segment);
    }
    
    onStatusUpdate(status) {
        console.log('Status update:', status);
        this.emit('statusUpdate', status);
    }
    
    onThumbnailReady(thumbnail) {
        this.emit('thumbnailReady', thumbnail);
    }
    
    onError(error) {
        console.error('Recording error:', error);
        this.emit('error', error);
    }
    
    async getEditableSegments() {
        if (!this.currentSession) {
            return [];
        }
        
        const response = await fetch(`${this.baseUrl}/segments/${this.currentSession.sessionId}`);
        if (!response.ok) {
            throw new Error(`Failed to get segments: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    async getThumbnail(timestamp, width = 160) {
        if (!this.currentSession) {
            return null;
        }
        
        const response = await fetch(
            `${this.baseUrl}/thumbnail/${this.currentSession.sessionId}/${timestamp}?width=${width}`
        );
        
        if (!response.ok) {
            return null;
        }
        
        const blob = await response.blob();
        return URL.createObjectURL(blob);
    }
    
    async takeSnapshot(format = 'JPEG') {
        if (!this.currentSession) {
            throw new Error('No active recording session');
        }
        
        const response = await fetch(
            `${this.baseUrl}/snapshot/${this.currentSession.sessionId}?format=${format}`,
            { method: 'POST' }
        );
        
        if (!response.ok) {
            throw new Error(`Failed to take snapshot: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    async addMarker(timestamp, type = 'Generic', description = '') {
        if (!this.currentSession) {
            throw new Error('No active recording session');
        }
        
        const response = await fetch(`${this.baseUrl}/marker/${this.currentSession.sessionId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                timestamp: timestamp,
                type: type,
                description: description
            })
        });
        
        if (!response.ok) {
            throw new Error(`Failed to add marker: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    // Event handling
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }
    
    off(event, handler) {
        if (!this.eventHandlers.has(event)) {
            return;
        }
        
        const handlers = this.eventHandlers.get(event);
        const index = handlers.indexOf(handler);
        if (index > -1) {
            handlers.splice(index, 1);
        }
    }
    
    emit(event, data) {
        if (!this.eventHandlers.has(event)) {
            return;
        }
        
        const handlers = this.eventHandlers.get(event);
        handlers.forEach(handler => {
            try {
                handler(data);
            } catch (error) {
                console.error(`Error in event handler for ${event}:`, error);
            }
        });
    }
    
    cleanup() {
        // Close WebSocket
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        
        // Destroy HLS player
        if (this.previewPlayer) {
            this.previewPlayer.destroy();
            this.previewPlayer = null;
        }
    }
    
    // Utility methods
    isRecording() {
        return this.currentSession !== null;
    }
    
    getSessionId() {
        return this.currentSession?.sessionId;
    }
    
    getPreviewUrl() {
        return this.currentSession?.previewUrl;
    }
}

// Export for use in app.js
window.VideoEngineClient = VideoEngineClient;