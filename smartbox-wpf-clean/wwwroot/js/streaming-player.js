/**
 * SmartBox Medical Streaming Player
 * Advanced HLS player with DVR, marking, and export capabilities
 */

class StreamingPlayer {
    constructor(container, options = {}) {
        // Use centralized configuration
        this.config = window.StreamingConfig;
        this.authManager = window.AuthManager;
        this.exportManager = window.MedicalExportManager;
        
        // Container and options for enhanced integration
        this.container = container;
        this.options = {
            sessionId: null,
            enableFFmpegIntegration: true,
            enableWebSocketUpdates: true,
            enableUnifiedThumbnails: true,
            ...options
        };
        
        // Initialize properties
        this.currentSessionId = this.options.sessionId;
        this.primaryPlayer = null;
        this.freezePlayer = null;
        this.primaryHLS = null;
        this.freezeHLS = null;
        this.ws = null;
        this.isDvrMode = false;
        this.marks = [];
        this.currentMark = { in: null, out: null };
        this.playbackRate = 1;
        this.frameRate = this.config.get('medical.frameRate', 30);
        this.highContrastMode = false;
        
        // Enhanced Phase 1 features
        this.videoEngine = null;
        this.wsHandler = null;
        this.thumbnailCache = new Map();
        this.isLive = false;
        this.errorRecovery = null;
        
        // WebSocket reconnection
        this.wsReconnectAttempts = 0;
        this.wsReconnectTimer = null;
        
        this.init();
    }
    
    async init() {
        this.setupEventListeners();
        await this.setupEngineIntegration();
        this.checkAuthentication();
    }
    
    async setupEngineIntegration() {
        if (!this.options.enableFFmpegIntegration) {
            return;
        }
        
        try {
            // Initialize error recovery system
            this.errorRecovery = new Phase1ErrorRecovery(this);
            
            // Initialize video engine client
            this.videoEngine = new VideoEngineClient();
            
            // Setup event listeners for FFmpeg integration
            this.videoEngine.on('segmentCompleted', this.onSegmentCompleted.bind(this));
            this.videoEngine.on('thumbnailReady', this.onThumbnailReady.bind(this));
            this.videoEngine.on('statusUpdate', this.onEngineStatusUpdate.bind(this));
            this.videoEngine.on('error', this.onEngineError.bind(this));
            
            // Connect to session if specified
            if (this.options.sessionId && this.options.enableWebSocketUpdates) {
                await this.connectToSession(this.options.sessionId);
            }
            
            console.log('FFmpeg engine integration initialized');
        } catch (error) {
            console.error('Failed to setup FFmpeg integration:', error);
            
            // Use error recovery system
            if (this.errorRecovery) {
                await this.errorRecovery.handleError(error, { context: 'setupEngineIntegration' });
            }
        }
    }
    
    async connectToSession(sessionId) {
        if (!this.videoEngine) {
            throw new Error('Video engine not initialized');
        }
        
        try {
            this.currentSessionId = sessionId;
            
            // Use enhanced WebSocket handler for better reconnection and error handling
            if (this.options.enableWebSocketUpdates) {
                this.wsHandler = new StreamingWebSocketHandler(this);
                this.wsHandler.connect(sessionId);
            }
            
            console.log(`Connected to session: ${sessionId}`);
        } catch (error) {
            console.error('Failed to connect to session:', error);
            throw error;
        }
    }
    
    onSegmentCompleted(segment) {
        console.log('New segment completed:', segment);
        
        // Update timeline with new segment (if timeline exists)
        if (this.timeline) {
            this.timeline.addSegment(segment);
        }
        
        // Update HLS playlist if in live mode
        if (this.isLive && this.primaryHLS) {
            this.refreshPlaylist();
        }
        
        // Emit event for external listeners
        this.emit('segmentCompleted', segment);
    }
    
    onThumbnailReady(data) {
        console.log('Thumbnail ready:', data);
        
        // Cache thumbnail
        this.thumbnailCache.set(data.timestamp, data.url);
        
        // Update timeline thumbnail (if timeline exists)
        if (this.timeline) {
            this.timeline.updateThumbnail(data.timestamp, data.url);
        }
        
        // Emit event for external listeners
        this.emit('thumbnailReady', data);
    }
    
    onEngineStatusUpdate(status) {
        console.log('Engine status update:', status);
        this.updateRecordingStatus(status);
    }
    
    onEngineError(error) {
        console.error('Engine error:', error);
        this.handleStreamError(error);
    }
    
    async loadThumbnail(timestamp, width = 160) {
        // Normalize timestamp for caching
        const cacheKey = `${timestamp}_${width}`;
        
        // Check cache first
        if (this.thumbnailCache.has(cacheKey)) {
            return this.thumbnailCache.get(cacheKey);
        }
        
        let thumbnailUrl = null;
        
        // Strategy 1: Use FFmpeg engine's thumbnail API if available
        if (this.videoEngine && this.currentSessionId && this.options.enableUnifiedThumbnails) {
            try {
                thumbnailUrl = await this.videoEngine.getThumbnail(timestamp, width);
                if (thumbnailUrl) {
                    this.thumbnailCache.set(cacheKey, thumbnailUrl);
                    console.log(`Loaded thumbnail from API: ${timestamp}s`);
                    return thumbnailUrl;
                }
            } catch (error) {
                console.warn('Failed to load thumbnail from API:', error);
            }
        }
        
        // Strategy 2: Fallback to video frame extraction
        try {
            thumbnailUrl = await this.extractVideoFrame(timestamp, width);
            if (thumbnailUrl) {
                this.thumbnailCache.set(cacheKey, thumbnailUrl);
                console.log(`Generated thumbnail from video: ${timestamp}s`);
                return thumbnailUrl;
            }
        } catch (error) {
            console.warn('Failed to extract video frame:', error);
        }
        
        // Strategy 3: Use placeholder or cached segment thumbnail
        return this.getFallbackThumbnail(timestamp);
    }
    
    async extractVideoFrame(timestamp, width = 160) {
        return new Promise((resolve, reject) => {
            if (!this.primaryPlayer || !this.primaryPlayer.videoWidth) {
                reject(new Error('Video player not available'));
                return;
            }
            
            const video = this.primaryPlayer;
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            // Calculate dimensions maintaining aspect ratio
            const aspectRatio = video.videoWidth / video.videoHeight;
            canvas.width = width;
            canvas.height = width / aspectRatio;
            
            // Seek to timestamp and capture frame
            const originalTime = video.currentTime;
            
            const onSeeked = () => {
                try {
                    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                    canvas.toBlob((blob) => {
                        if (blob) {
                            const url = URL.createObjectURL(blob);
                            resolve(url);
                        } else {
                            reject(new Error('Failed to create thumbnail blob'));
                        }
                        
                        // Restore original time
                        video.currentTime = originalTime;
                    }, 'image/jpeg', 0.8);
                } catch (error) {
                    reject(error);
                    video.currentTime = originalTime;
                } finally {
                    video.removeEventListener('seeked', onSeeked);
                }
            };
            
            video.addEventListener('seeked', onSeeked);
            video.currentTime = timestamp;
        });
    }
    
    getFallbackThumbnail(timestamp) {
        // Return a placeholder or last known good thumbnail
        const placeholderUrl = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYwIiBoZWlnaHQ9IjkwIiB2aWV3Qm94PSIwIDAgMTYwIDkwIiBmaWxsPSJub25lIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPgo8cmVjdCB3aWR0aD0iMTYwIiBoZWlnaHQ9IjkwIiBmaWxsPSIjZjBmMGYwIi8+CjxwYXRoIGQ9Ik04MCA0NUw2MCAzMEgxMDBMODAgNDVaIiBmaWxsPSIjY2NjY2NjIi8+Cjx0ZXh0IHg9IjgwIiB5PSI2NSIgZm9udC1mYW1pbHk9InNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTAiIGZpbGw9IiM5OTk5OTkiIHRleHQtYW5jaG9yPSJtaWRkbGUiPk5vIFRodW1ibmFpbDwvdGV4dD4KPHN2Zz4K';
        
        console.log(`Using fallback thumbnail for ${timestamp}s`);
        return placeholderUrl;
    }
    
    // Enhanced thumbnail cache management
    clearThumbnailCache() {
        // Revoke object URLs to prevent memory leaks
        for (const [key, url] of this.thumbnailCache.entries()) {
            if (url.startsWith('blob:')) {
                URL.revokeObjectURL(url);
            }
        }
        this.thumbnailCache.clear();
        console.log('Thumbnail cache cleared');
    }
    
    getThumbnailCacheSize() {
        return this.thumbnailCache.size;
    }
    
    purgeThumbnailCache(maxAge = 300000) { // 5 minutes default
        const now = Date.now();
        const keysToDelete = [];
        
        for (const [key, value] of this.thumbnailCache.entries()) {
            if (value.timestamp && (now - value.timestamp) > maxAge) {
                keysToDelete.push(key);
                if (value.url && value.url.startsWith('blob:')) {
                    URL.revokeObjectURL(value.url);
                }
            }
        }
        
        keysToDelete.forEach(key => this.thumbnailCache.delete(key));
        
        if (keysToDelete.length > 0) {
            console.log(`Purged ${keysToDelete.length} old thumbnails from cache`);
        }
    }
    
    // Enhanced methods for WebSocket integration
    onNewSegment(segment) {
        console.log('Processing new segment:', segment);
        
        // Add to segments list if it doesn't exist
        if (!this.segments) {
            this.segments = [];
        }
        
        // Check if segment already exists
        const existingIndex = this.segments.findIndex(s => s.number === segment.number);
        if (existingIndex >= 0) {
            this.segments[existingIndex] = segment;
        } else {
            this.segments.push(segment);
            this.segments.sort((a, b) => a.number - b.number);
        }
        
        // Update timeline if available
        if (this.timeline) {
            this.timeline.addSegment(segment);
        }
        
        // Update UI elements
        this.updateSegmentIndicator(segment);
        
        // Emit for external listeners
        this.emit('newSegment', segment);
    }
    
    updateRecordingStatus(status) {
        console.log('Recording status update:', status);
        
        // Update internal state
        this.recordingStatus = status;
        
        // Update UI indicators
        this.updateStatusIndicators(status);
        
        // Handle status-specific logic
        switch (status.status) {
            case 'Recording':
                this.isRecording = true;
                this.updateRecordingIndicator(true);
                break;
            case 'Stopped':
                this.isRecording = false;
                this.updateRecordingIndicator(false);
                break;
            case 'Paused':
                this.isPaused = true;
                this.updatePauseIndicator(true);
                break;
            case 'Error':
                this.handleRecordingError(status.error);
                break;
        }
        
        this.emit('statusUpdate', status);
    }
    
    async handleStreamError(error) {
        console.error('Stream error:', error);
        
        // Log error for debugging
        this.lastError = {
            timestamp: Date.now(),
            error: error,
            type: 'stream'
        };
        
        // Update UI with error state
        this.showErrorIndicator(error.message || 'Stream error occurred');
        
        // Use enhanced error recovery system
        if (this.errorRecovery) {
            await this.errorRecovery.handleError(error, { context: 'streamError' });
        } else {
            // Fallback to basic recovery
            this.attemptErrorRecovery(error);
        }
        
        this.emit('streamError', error);
    }
    
    handleRecordingError(error) {
        console.error('Recording error:', error);
        
        this.lastError = {
            timestamp: Date.now(),
            error: error,
            type: 'recording'
        };
        
        this.showErrorIndicator(`Recording error: ${error.message || 'Unknown error'}`);
        this.emit('recordingError', error);
    }
    
    attemptErrorRecovery(error) {
        if (!error || !error.code) return;
        
        switch (error.code) {
            case 'NETWORK_ERROR':
                console.log('Attempting network error recovery');
                setTimeout(() => {
                    if (this.wsHandler) {
                        this.wsHandler.resetReconnection();
                        this.wsHandler.establishConnection();
                    }
                }, 2000);
                break;
                
            case 'MEDIA_ERROR':
                console.log('Attempting media error recovery');
                if (this.primaryHLS) {
                    this.primaryHLS.recoverMediaError();
                }
                break;
                
            case 'BUFFER_ERROR':
                console.log('Attempting buffer error recovery');
                this.clearBuffers();
                break;
                
            default:
                console.log('No specific recovery available for error:', error.code);
        }
    }
    
    clearBuffers() {
        if (this.primaryHLS) {
            try {
                this.primaryHLS.destroy();
                this.initializeHLS();
            } catch (e) {
                console.error('Failed to clear buffers:', e);
            }
        }
    }
    
    // UI update methods
    updateSegmentIndicator(segment) {
        const indicator = document.getElementById('segmentIndicator');
        if (indicator) {
            indicator.textContent = `Segment ${segment.number}`;
            indicator.className = segment.isComplete ? 'segment-complete' : 'segment-processing';
        }
    }
    
    updateStatusIndicators(status) {
        const statusElement = document.getElementById('recordingStatus');
        if (statusElement) {
            statusElement.textContent = status.status;
            statusElement.className = `status-${status.status.toLowerCase()}`;
        }
        
        const timeElement = document.getElementById('recordingTime');
        if (timeElement && status.duration) {
            timeElement.textContent = this.formatTime(status.duration);
        }
    }
    
    updateRecordingIndicator(isRecording) {
        const indicator = document.getElementById('recordingIndicator');
        if (indicator) {
            indicator.classList.toggle('recording', isRecording);
            indicator.textContent = isRecording ? 'REC' : '';
        }
    }
    
    updatePauseIndicator(isPaused) {
        const indicator = document.getElementById('pauseIndicator');
        if (indicator) {
            indicator.classList.toggle('paused', isPaused);
            indicator.textContent = isPaused ? 'PAUSED' : '';
        }
    }
    
    showErrorIndicator(message) {
        const indicator = document.getElementById('errorIndicator');
        if (indicator) {
            indicator.textContent = message;
            indicator.classList.add('visible');
            
            // Auto-hide after 5 seconds
            setTimeout(() => {
                indicator.classList.remove('visible');
            }, 5000);
        }
    }
    
    formatTime(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = Math.floor(seconds % 60);
        
        if (hours > 0) {
            return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        } else {
            return `${minutes}:${secs.toString().padStart(2, '0')}`;
        }
    }
    
    // Cleanup method
    cleanup() {
        // Clear thumbnail cache
        this.clearThumbnailCache();
        
        // Disconnect WebSocket
        if (this.wsHandler) {
            this.wsHandler.disconnect();
            this.wsHandler = null;
        }
        
        // Cleanup video engine
        if (this.videoEngine) {
            this.videoEngine.cleanup();
            this.videoEngine = null;
        }
        
        // Cleanup error recovery
        if (this.errorRecovery) {
            this.errorRecovery.cleanup();
            this.errorRecovery = null;
        }
        
        // Clear timers
        if (this.wsReconnectTimer) {
            clearTimeout(this.wsReconnectTimer);
            this.wsReconnectTimer = null;
        }
        
        console.log('StreamingPlayer cleanup completed');
    }
    
    async refreshPlaylist() {
        if (this.primaryHLS && this.primaryHLS.url) {
            try {
                this.primaryHLS.loadSource(this.primaryHLS.url);
            } catch (error) {
                console.error('Failed to refresh playlist:', error);
            }
        }
    }
    
    // Event emitter methods
    emit(event, data) {
        if (this.eventHandlers && this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            handlers.forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }
    
    on(event, handler) {
        if (!this.eventHandlers) {
            this.eventHandlers = new Map();
        }
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }
    
    off(event, handler) {
        if (!this.eventHandlers || !this.eventHandlers.has(event)) {
            return;
        }
        
        const handlers = this.eventHandlers.get(event);
        const index = handlers.indexOf(handler);
        if (index > -1) {
            handlers.splice(index, 1);
        }
    }
    
    setupEventListeners() {
        // Login form
        document.getElementById('loginForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleLogin();
        });
        
        // Logout
        document.getElementById('logoutButton').addEventListener('click', () => {
            this.logout();
        });
        
        // Stream controls
        document.getElementById('startStreamBtn').addEventListener('click', () => {
            this.startStream();
        });
        
        document.getElementById('stopStreamBtn').addEventListener('click', () => {
            this.stopStream();
        });
        
        document.getElementById('toggleDvrBtn').addEventListener('click', () => {
            this.toggleDvrMode();
        });
        
        document.getElementById('goLiveBtn').addEventListener('click', () => {
            this.goLive();
        });
        
        // Navigation controls
        document.getElementById('jumpBack30').addEventListener('click', () => {
            this.jump(-30);
        });
        
        document.getElementById('jumpBack10').addEventListener('click', () => {
            this.jump(-10);
        });
        
        document.getElementById('frameBack').addEventListener('click', () => {
            this.stepFrame(-1);
        });
        
        document.getElementById('playPauseBtn').addEventListener('click', () => {
            this.togglePlayPause();
        });
        
        document.getElementById('frameForward').addEventListener('click', () => {
            this.stepFrame(1);
        });
        
        document.getElementById('jumpForward10').addEventListener('click', () => {
            this.jump(10);
        });
        
        document.getElementById('jumpForward30').addEventListener('click', () => {
            this.jump(30);
        });
        
        // Marking controls
        document.getElementById('markInBtn').addEventListener('click', () => {
            this.markIn();
        });
        
        document.getElementById('markOutBtn').addEventListener('click', () => {
            this.markOut();
        });
        
        document.getElementById('clearMarksBtn').addEventListener('click', () => {
            this.clearMarks();
        });
        
        document.getElementById('exportRangeBtn').addEventListener('click', () => {
            this.showExportModal();
        });
        
        // Advanced controls
        document.getElementById('freezeFrameBtn').addEventListener('click', () => {
            this.toggleFreezeFrame();
        });
        
        // Speed controls
        const speedButtons = ['speed05x', 'speed1x', 'speed2x', 'speed4x'];
        const speeds = [0.5, 1, 2, 4];
        
        speedButtons.forEach((id, index) => {
            document.getElementById(id).addEventListener('click', (e) => {
                this.setPlaybackSpeed(speeds[index]);
                document.querySelectorAll('.control-button').forEach(btn => {
                    btn.classList.remove('active');
                });
                e.target.classList.add('active');
            });
        });
        
        // Timeline interaction
        const timeline = document.getElementById('timeline');
        timeline.addEventListener('click', (e) => {
            this.seekToPosition(e);
        });
        
        // Export modal
        document.getElementById('cancelExportBtn').addEventListener('click', () => {
            this.hideExportModal();
        });
        
        document.getElementById('confirmExportBtn').addEventListener('click', () => {
            this.exportRange();
        });
    }
    
    // Authentication methods
    
    async checkAuthentication() {
        // Use AuthManager for authentication check
        if (this.authManager.isAuthenticated()) {
            this.showPlayer();
            this.connectWebSocket();
        } else {
            // Try to load stored credentials
            if (this.authManager.loadStoredCredentials()) {
                this.showPlayer();
                this.connectWebSocket();
            } else {
                this.showLogin();
            }
        }
        
        // Listen for auth expiry
        window.addEventListener('authExpired', () => {
            this.handleAuthExpired();
        });
    }
    
    async handleLogin() {
        const username = document.getElementById('username').value;
        const password = document.getElementById('password').value;
        const loginButton = document.getElementById('loginButton');
        const loginError = document.getElementById('loginError');
        
        loginButton.disabled = true;
        loginError.textContent = '';
        
        try {
            // Use AuthManager for login
            const result = await this.authManager.login(username, password);
            
            if (result.success) {
                this.showPlayer();
                this.connectWebSocket();
            } else {
                loginError.textContent = result.error || 'Login failed';
            }
        } catch (error) {
            loginError.textContent = 'Connection error';
            console.error('Login error:', error);
        } finally {
            loginButton.disabled = false;
        }
    }
    
    async logout() {
        try {
            // Stop any active streams
            if (this.currentSessionId) {
                await this.stopStream();
            }
            
            // Close WebSocket
            if (this.ws) {
                this.ws.close();
            }
            
            // Use AuthManager for logout
            await this.authManager.logout();
            
            // Clean up players
            if (this.primaryHLS) {
                this.primaryHLS.destroy();
                this.primaryHLS = null;
            }
            if (this.freezeHLS) {
                this.freezeHLS.destroy();
                this.freezeHLS = null;
            }
            
            this.showLogin();
        } catch (error) {
            console.error('Logout error:', error);
            // Show login anyway
            this.showLogin();
        }
    }
    
    showLogin() {
        document.getElementById('loginContainer').style.display = 'flex';
        document.getElementById('playerContainer').style.display = 'none';
    }
    
    showPlayer() {
        document.getElementById('loginContainer').style.display = 'none';
        document.getElementById('playerContainer').style.display = 'flex';
        
        const user = this.authManager.getCurrentUser();
        if (user) {
            document.getElementById('userDisplay').textContent = `${user.displayName} (${user.role})`;
        }
        
        // Initialize video players
        this.initializePlayers();
        
        // Apply medical UI compliance
        this.applyMedicalUICompliance();
    }
    
    // WebSocket connection
    
    connectWebSocket() {
        const wsUrl = this.config.get('wsUrl');
        
        try {
            this.ws = new WebSocket(wsUrl);
            
            this.ws.onopen = () => {
                console.log('WebSocket connected');
                this.wsReconnectAttempts = 0;
                
                // Send authentication
                this.ws.send(JSON.stringify({
                    type: 'auth',
                    token: this.authManager.authToken
                }));
            };
            
            this.ws.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this.handleWebSocketMessage(message);
                } catch (e) {
                    console.error('Failed to parse WebSocket message:', e);
                }
            };
            
            this.ws.onerror = (error) => {
                console.error('WebSocket error:', error);
            };
            
            this.ws.onclose = (event) => {
                console.log('WebSocket disconnected:', event.code, event.reason);
                
                // Reconnect with exponential backoff
                if (this.authManager.isAuthenticated() && 
                    this.wsReconnectAttempts < this.config.get('maxReconnectAttempts', 10)) {
                    
                    const delay = Math.min(
                        this.config.get('reconnectDelay', 5000) * Math.pow(2, this.wsReconnectAttempts),
                        30000 // Max 30 seconds
                    );
                    
                    this.wsReconnectAttempts++;
                    console.log(`Reconnecting WebSocket in ${delay}ms (attempt ${this.wsReconnectAttempts})`);
                    
                    this.wsReconnectTimer = setTimeout(() => {
                        this.connectWebSocket();
                    }, delay);
                }
            };
        } catch (error) {
            console.error('Failed to create WebSocket:', error);
        }
    }
    
    handleWebSocketMessage(message) {
        switch (message.type) {
            case 'streamUpdate':
                this.updateStreamStatus(message.data);
                break;
            case 'segmentCreated':
                this.onSegmentCreated(message.data);
                break;
            case 'error':
                console.error('WebSocket error:', message.data);
                break;
        }
    }
    
    // Video player initialization
    
    initializePlayers() {
        // Initialize VideoJS players with medical-grade settings
        const videojsOptions = {
            ...this.config.get('videojs'),
            // Medical compliance additions
            userActions: {
                hotkeys: true,
                doubleClick: false // Prevent accidental actions
            },
            playbackRates: [0.25, 0.5, 1, 2, 4, 8],
            controlBar: {
                volumePanel: {
                    inline: false
                },
                pictureInPictureToggle: false // Disable for medical compliance
            }
        };
        
        // Primary player
        this.primaryPlayer = videojs('primaryVideo', videojsOptions);
        
        // Initialize HLS manager for primary player
        const primaryVideo = document.getElementById('primaryVideo');
        this.primaryHLS = new HLSManager(primaryVideo, this.config);
        
        // Set up HLS event handlers
        this.primaryHLS.onError = (error) => {
            console.error('HLS Error:', error);
            this.showError('Streaming error occurred. Please try again.');
        };
        
        this.primaryHLS.onManifestLoaded = (data) => {
            // Update UI with stream info
            this.updateStreamInfo(data);
        };
        
        // Freeze frame player
        this.freezePlayer = videojs('freezeVideo', videojsOptions);
        const freezeVideo = document.getElementById('freezeVideo');
        this.freezeHLS = new HLSManager(freezeVideo, this.config);
        
        // Player event listeners
        this.primaryPlayer.on('timeupdate', () => {
            this.updateTimeline();
        });
        
        this.primaryPlayer.on('loadedmetadata', () => {
            this.updateTimeDisplay();
            // Extract accurate frame rate
            if (this.primaryHLS) {
                this.frameRate = this.primaryHLS.frameRate || 30;
            }
        });
        
        this.primaryPlayer.on('progress', () => {
            this.updateBufferedRange();
        });
        
        // Keyboard shortcuts for medical use
        this.setupKeyboardShortcuts();
    }
    
    // Streaming methods
    
    async startStream() {
        const startBtn = document.getElementById('startStreamBtn');
        const stopBtn = document.getElementById('stopStreamBtn');
        
        startBtn.disabled = true;
        this.showLoading(true);
        
        try {
            // Use authenticated fetch
            const response = await this.authManager.authenticatedFetch(
                `${this.config.apiUrl}/stream/start`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        inputType: 0, // Device
                        deviceName: 'Integrated Camera', // Default camera
                        enableDVR: this.isDvrMode,
                        includeAudio: true,
                        videoBitrate: '4000k', // Higher quality for medical
                        framerate: this.frameRate,
                        resolution: '1920x1080', // Full HD for medical clarity
                        options: {
                            preserveFrameAccuracy: true,
                            lowLatency: true,
                            medicalGrade: true
                        }
                    })
                }
            );
            
            const data = await response.json();
            
            if (response.ok) {
                this.currentSessionId = data.sessionId;
                const streamUrl = `${this.config.apiUrl}${data.streamUrl}`;
                
                // Load stream using HLS manager
                this.primaryHLS.loadStream(streamUrl);
                
                // Auto-play
                this.primaryPlayer.play().catch(e => {
                    console.warn('Autoplay blocked:', e);
                    // Show play button overlay
                    this.primaryPlayer.bigPlayButton.show();
                });
                
                startBtn.disabled = true;
                stopBtn.disabled = false;
                
                // Enable controls
                this.enableStreamControls(true);
                
                // Update UI
                this.updateStreamStatus('live');
            } else {
                this.showError('Failed to start stream: ' + (data.error || 'Unknown error'));
                startBtn.disabled = false;
            }
        } catch (error) {
            console.error('Start stream error:', error);
            this.showError('Failed to start stream: ' + error.message);
            startBtn.disabled = false;
        } finally {
            this.showLoading(false);
        }
    }
    
    async stopStream() {
        if (!this.currentSessionId) return;
        
        const stopBtn = document.getElementById('stopStreamBtn');
        stopBtn.disabled = true;
        
        try {
            await this.authManager.authenticatedFetch(
                `${this.config.apiUrl}/stream/stop`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        sessionId: this.currentSessionId
                    })
                }
            );
            
            // Stop playback
            this.primaryPlayer.pause();
            
            // Destroy HLS instance
            if (this.primaryHLS) {
                this.primaryHLS.destroy();
                this.primaryHLS = new HLSManager(document.getElementById('primaryVideo'), this.config);
            }
            
            this.currentSessionId = null;
            
            document.getElementById('startStreamBtn').disabled = false;
            document.getElementById('stopStreamBtn').disabled = true;
            
            this.enableStreamControls(false);
            this.updateStreamStatus('stopped');
            
        } catch (error) {
            console.error('Stop stream error:', error);
            stopBtn.disabled = false;
        }
    }
    
    toggleDvrMode() {
        this.isDvrMode = !this.isDvrMode;
        const btn = document.getElementById('toggleDvrBtn');
        btn.classList.toggle('active', this.isDvrMode);
        
        // Update HLS manager DVR mode
        if (this.primaryHLS) {
            this.primaryHLS.setDVRMode(this.isDvrMode);
        }
        
        // Update UI to show DVR indicator
        this.updateDVRIndicator();
    }
    
    goLive() {
        if (this.primaryHLS) {
            this.primaryHLS.goToLive();
            this.updateLiveIndicator(true);
        }
    }
    
    // Navigation methods
    
    jump(seconds) {
        if (!this.primaryPlayer) return;
        
        const currentTime = this.primaryPlayer.currentTime();
        const newTime = Math.max(0, currentTime + seconds);
        this.primaryPlayer.currentTime(newTime);
    }
    
    stepFrame(direction) {
        if (!this.primaryHLS) return;
        
        // Pause if playing
        if (!this.primaryPlayer.paused()) {
            this.primaryPlayer.pause();
        }
        
        // Use HLS manager for frame-accurate stepping
        this.primaryHLS.stepFrame(direction);
        
        // Update frame indicator
        this.updateFrameIndicator();
    }
    
    togglePlayPause() {
        if (!this.primaryPlayer) return;
        
        if (this.primaryPlayer.paused()) {
            this.primaryPlayer.play();
        } else {
            this.primaryPlayer.pause();
        }
    }
    
    setPlaybackSpeed(speed) {
        if (!this.primaryPlayer) return;
        
        this.playbackRate = speed;
        this.primaryPlayer.playbackRate(speed);
        
        if (this.freezePlayer && !this.freezePlayer.paused()) {
            this.freezePlayer.playbackRate(speed);
        }
    }
    
    // Marking methods
    
    async markIn() {
        if (!this.primaryPlayer) return;
        
        const currentTime = this.primaryPlayer.currentTime();
        this.currentMark.in = currentTime;
        
        // Send to server
        if (this.currentSessionId) {
            try {
                await fetch(`${this.apiUrl}/stream/mark/${this.currentSessionId}/in`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.authToken}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        timestamp: currentTime
                    })
                });
                
                this.addTimelineMarker(currentTime, 'in');
            } catch (error) {
                console.error('Mark in error:', error);
            }
        }
        
        // Enable mark out button
        document.getElementById('markOutBtn').disabled = false;
    }
    
    async markOut() {
        if (!this.primaryPlayer || this.currentMark.in === null) return;
        
        const currentTime = this.primaryPlayer.currentTime();
        
        if (currentTime <= this.currentMark.in) {
            alert('Out point must be after in point');
            return;
        }
        
        this.currentMark.out = currentTime;
        
        // Send to server
        if (this.currentSessionId) {
            try {
                await fetch(`${this.apiUrl}/stream/mark/${this.currentSessionId}/out`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.authToken}`,
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        timestamp: currentTime
                    })
                });
                
                this.addTimelineMarker(currentTime, 'out');
                
                // Add to marks list
                this.marks.push({
                    in: this.currentMark.in,
                    out: this.currentMark.out
                });
                
                // Reset current mark
                this.currentMark = { in: null, out: null };
                document.getElementById('markOutBtn').disabled = true;
                
                // Enable export button
                document.getElementById('exportRangeBtn').disabled = false;
            } catch (error) {
                console.error('Mark out error:', error);
            }
        }
    }
    
    clearMarks() {
        this.marks = [];
        this.currentMark = { in: null, out: null };
        
        // Clear timeline markers
        const markersContainer = document.getElementById('timelineMarkers');
        markersContainer.innerHTML = '';
        
        // Disable buttons
        document.getElementById('markOutBtn').disabled = true;
        document.getElementById('exportRangeBtn').disabled = true;
    }
    
    addTimelineMarker(time, type) {
        const timeline = document.getElementById('timeline');
        const markersContainer = document.getElementById('timelineMarkers');
        const duration = this.primaryPlayer.duration();
        
        if (!duration || duration === Infinity) return;
        
        const position = (time / duration) * 100;
        
        const marker = document.createElement('div');
        marker.className = `timeline-marker ${type}`;
        marker.style.left = `${position}%`;
        marker.title = `${type.toUpperCase()}: ${this.formatTime(time)}`;
        
        marker.addEventListener('click', (e) => {
            e.stopPropagation();
            this.primaryPlayer.currentTime(time);
        });
        
        markersContainer.appendChild(marker);
    }
    
    // Freeze frame functionality
    
    toggleFreezeFrame() {
        const secondaryVideo = document.getElementById('secondaryVideo');
        const btn = document.getElementById('freezeFrameBtn');
        
        if (secondaryVideo.classList.contains('active')) {
            // Close freeze frame
            secondaryVideo.classList.remove('active');
            btn.classList.remove('active');
            this.freezePlayer.pause();
        } else {
            // Create freeze frame
            if (!this.primaryPlayer || !this.primaryPlayer.src()) return;
            
            secondaryVideo.classList.add('active');
            btn.classList.add('active');
            
            // Clone current video state
            const currentTime = this.primaryPlayer.currentTime();
            const currentSrc = this.primaryPlayer.src();
            
            this.freezePlayer.src(currentSrc);
            this.freezePlayer.currentTime(currentTime);
            
            // Sync playback rate
            this.freezePlayer.playbackRate(this.playbackRate);
        }
    }
    
    // Timeline methods
    
    updateTimeline() {
        if (!this.primaryPlayer) return;
        
        const currentTime = this.primaryPlayer.currentTime();
        const duration = this.primaryPlayer.duration();
        
        if (!duration || duration === Infinity) return;
        
        const percentage = (currentTime / duration) * 100;
        
        document.getElementById('timelinePlayed').style.width = `${percentage}%`;
        document.getElementById('timelinePlayhead').style.left = `${percentage}%`;
        document.getElementById('currentTime').textContent = this.formatTime(currentTime);
    }
    
    updateTimeDisplay() {
        if (!this.primaryPlayer) return;
        
        const duration = this.primaryPlayer.duration();
        
        if (duration && duration !== Infinity) {
            document.getElementById('totalTime').textContent = this.formatTime(duration);
        }
    }
    
    updateBufferedRange() {
        if (!this.primaryPlayer) return;
        
        const buffered = this.primaryPlayer.buffered();
        const duration = this.primaryPlayer.duration();
        
        if (!buffered.length || !duration || duration === Infinity) return;
        
        // Show the furthest buffered point
        let maxBuffered = 0;
        for (let i = 0; i < buffered.length; i++) {
            maxBuffered = Math.max(maxBuffered, buffered.end(i));
        }
        
        const percentage = (maxBuffered / duration) * 100;
        document.getElementById('timelineBuffered').style.width = `${percentage}%`;
    }
    
    seekToPosition(event) {
        if (!this.primaryPlayer) return;
        
        const timeline = document.getElementById('timeline');
        const rect = timeline.getBoundingClientRect();
        const x = event.clientX - rect.left;
        const percentage = x / rect.width;
        const duration = this.primaryPlayer.duration();
        
        if (duration && duration !== Infinity) {
            const time = percentage * duration;
            this.primaryPlayer.currentTime(time);
        }
    }
    
    // Export functionality
    
    showExportModal() {
        const modal = document.getElementById('exportModal');
        const optionsContainer = document.getElementById('exportOptions');
        
        // Clear previous options
        optionsContainer.innerHTML = '';
        
        // Add export options for each marked range
        this.marks.forEach((mark, index) => {
            const option = document.createElement('div');
            option.className = 'export-option';
            option.innerHTML = `
                <span>Range ${index + 1}: ${this.formatTime(mark.in)} - ${this.formatTime(mark.out)}</span>
                <input type="checkbox" checked data-index="${index}">
            `;
            optionsContainer.appendChild(option);
        });
        
        modal.style.display = 'block';
    }
    
    hideExportModal() {
        document.getElementById('exportModal').style.display = 'none';
    }
    
    async exportRange() {
        // Get selected ranges
        const checkboxes = document.querySelectorAll('#exportOptions input[type="checkbox"]:checked');
        const selectedRanges = Array.from(checkboxes).map(cb => {
            const index = parseInt(cb.dataset.index);
            return this.marks[index];
        });
        
        if (selectedRanges.length === 0) {
            alert('Please select at least one range to export');
            return;
        }
        
        // In a real implementation, this would trigger server-side processing
        // For client-side export, we would use MediaRecorder API
        
        try {
            // Create a blob from the selected ranges
            const exportBlob = await this.createExportBlob(selectedRanges);
            
            // Download the file
            const url = URL.createObjectURL(exportBlob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `export_${Date.now()}.webm`;
            a.click();
            
            URL.revokeObjectURL(url);
            
            this.hideExportModal();
        } catch (error) {
            console.error('Export error:', error);
            alert('Export failed: ' + error.message);
        }
    }
    
    async exportRange() {
        // Get selected ranges
        const checkboxes = document.querySelectorAll('#exportOptions input[type="checkbox"]:checked');
        const selectedRanges = Array.from(checkboxes).map(cb => {
            const index = parseInt(cb.dataset.index);
            return this.marks[index];
        });
        
        if (selectedRanges.length === 0) {
            alert('Please select at least one range to export');
            return;
        }
        
        // Get export format
        const format = document.querySelector('input[name="exportFormat"]:checked')?.value || 'mp4';
        
        this.hideExportModal();
        this.showExportProgress(true);
        
        try {
            // Use medical export manager
            const exportId = await this.exportManager.exportRange({
                sessionId: this.currentSessionId,
                ranges: selectedRanges,
                format: format,
                quality: 'high',
                includeMetadata: true,
                frameRate: this.frameRate,
                patientId: this.getPatientId(),
                studyDescription: 'Medical Recording Export',
                exportReason: 'Medical Record',
                onProgress: (progress) => {
                    this.updateExportProgress(progress);
                },
                onComplete: (result) => {
                    this.showExportProgress(false);
                    this.showSuccess('Export completed successfully');
                },
                onError: (error) => {
                    this.showExportProgress(false);
                    this.showError('Export failed: ' + error.message);
                }
            });
            
            console.log('Export started with ID:', exportId);
            
        } catch (error) {
            console.error('Export error:', error);
            this.showExportProgress(false);
            this.showError('Export failed: ' + error.message);
        }
    }
    
    // Utility methods
    
    formatTime(seconds) {
        const h = Math.floor(seconds / 3600);
        const m = Math.floor((seconds % 3600) / 60);
        const s = Math.floor(seconds % 60);
        
        return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    }
    
    enableStreamControls(enabled) {
        const controls = [
            'toggleDvrBtn', 'goLiveBtn', 'jumpBack30', 'jumpBack10',
            'frameBack', 'playPauseBtn', 'frameForward', 'jumpForward10',
            'jumpForward30', 'markInBtn', 'freezeFrameBtn'
        ];
        
        controls.forEach(id => {
            document.getElementById(id).disabled = !enabled;
        });
    }
}

    // New helper methods for medical UI compliance
    
    applyMedicalUICompliance() {
        // Ensure minimum touch target sizes (44x44px)
        const buttons = document.querySelectorAll('.control-button, button');
        buttons.forEach(btn => {
            const rect = btn.getBoundingClientRect();
            if (rect.width < 44 || rect.height < 44) {
                btn.style.minWidth = '44px';
                btn.style.minHeight = '44px';
            }
        });
        
        // Add ARIA labels for accessibility
        this.addAriaLabels();
        
        // Enable high contrast mode if needed
        if (this.config.get('medical.enableHighContrast', false)) {
            this.toggleHighContrast(true);
        }
        
        // Add medical gestures support
        this.setupMedicalGestures();
    }
    
    addAriaLabels() {
        const ariaLabels = {
            'startStreamBtn': 'Start video stream',
            'stopStreamBtn': 'Stop video stream',
            'toggleDvrBtn': 'Toggle DVR recording mode',
            'goLiveBtn': 'Go to live stream edge',
            'jumpBack30': 'Jump back 30 seconds',
            'jumpBack10': 'Jump back 10 seconds',
            'frameBack': 'Step back one frame',
            'playPauseBtn': 'Play or pause video',
            'frameForward': 'Step forward one frame',
            'jumpForward10': 'Jump forward 10 seconds',
            'jumpForward30': 'Jump forward 30 seconds',
            'markInBtn': 'Mark in point for export',
            'markOutBtn': 'Mark out point for export',
            'clearMarksBtn': 'Clear all marked points',
            'exportRangeBtn': 'Export marked video ranges',
            'freezeFrameBtn': 'Toggle freeze frame view',
            'speed05x': 'Set playback speed to 0.5x',
            'speed1x': 'Set playback speed to 1x',
            'speed2x': 'Set playback speed to 2x',
            'speed4x': 'Set playback speed to 4x'
        };
        
        Object.entries(ariaLabels).forEach(([id, label]) => {
            const element = document.getElementById(id);
            if (element) {
                element.setAttribute('aria-label', label);
                element.setAttribute('role', 'button');
            }
        });
        
        // Add ARIA live regions for status updates
        const statusRegion = document.createElement('div');
        statusRegion.id = 'statusRegion';
        statusRegion.setAttribute('aria-live', 'polite');
        statusRegion.setAttribute('aria-atomic', 'true');
        statusRegion.className = 'sr-only';
        document.body.appendChild(statusRegion);
    }
    
    toggleHighContrast(enable) {
        this.highContrastMode = enable;
        document.body.classList.toggle('high-contrast', enable);
        
        // Store preference
        this.config.set('medical.enableHighContrast', enable);
    }
    
    setupMedicalGestures() {
        // Two-finger tap for play/pause (medical glove friendly)
        let touchCount = 0;
        let touchTimer = null;
        
        const videoArea = document.querySelector('.video-area');
        
        videoArea.addEventListener('touchstart', (e) => {
            if (e.touches.length === 2) {
                touchCount++;
                
                if (touchTimer) clearTimeout(touchTimer);
                
                touchTimer = setTimeout(() => {
                    if (touchCount >= 2) {
                        // Double two-finger tap
                        this.togglePlayPause();
                    }
                    touchCount = 0;
                }, 300);
            }
        });
        
        // Pinch to zoom for frame inspection
        let initialDistance = 0;
        let currentScale = 1;
        
        videoArea.addEventListener('touchstart', (e) => {
            if (e.touches.length === 2) {
                initialDistance = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY
                );
            }
        });
        
        videoArea.addEventListener('touchmove', (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                
                const currentDistance = Math.hypot(
                    e.touches[0].clientX - e.touches[1].clientX,
                    e.touches[0].clientY - e.touches[1].clientY
                );
                
                currentScale = Math.min(3, Math.max(1, currentDistance / initialDistance));
                
                if (this.primaryPlayer) {
                    this.primaryPlayer.el().style.transform = `scale(${currentScale})`;
                }
            }
        });
        
        videoArea.addEventListener('touchend', () => {
            if (currentScale === 1 && this.primaryPlayer) {
                this.primaryPlayer.el().style.transform = '';
            }
        });
    }
    
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Only handle shortcuts when not typing
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
                return;
            }
            
            switch(e.key) {
                case ' ':
                    e.preventDefault();
                    this.togglePlayPause();
                    break;
                case 'ArrowLeft':
                    e.preventDefault();
                    if (e.shiftKey) {
                        this.stepFrame(-1);
                    } else {
                        this.jump(-10);
                    }
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    if (e.shiftKey) {
                        this.stepFrame(1);
                    } else {
                        this.jump(10);
                    }
                    break;
                case 'i':
                case 'I':
                    e.preventDefault();
                    this.markIn();
                    break;
                case 'o':
                case 'O':
                    e.preventDefault();
                    this.markOut();
                    break;
                case 'l':
                case 'L':
                    e.preventDefault();
                    this.goLive();
                    break;
                case 'f':
                case 'F':
                    e.preventDefault();
                    this.toggleFreezeFrame();
                    break;
                case 'h':
                case 'H':
                    e.preventDefault();
                    this.toggleHighContrast(!this.highContrastMode);
                    break;
            }
        });
    }
    
    handleAuthExpired() {
        this.showError('Session expired. Please login again.');
        this.logout();
    }
    
    showLoading(show) {
        const primaryLoading = document.getElementById('primaryLoading');
        if (primaryLoading) {
            primaryLoading.style.display = show ? 'flex' : 'none';
        }
    }
    
    showError(message) {
        // Update status region for screen readers
        const statusRegion = document.getElementById('statusRegion');
        if (statusRegion) {
            statusRegion.textContent = 'Error: ' + message;
        }
        
        // Show visual error
        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-notification';
        errorDiv.textContent = message;
        errorDiv.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: #d83b01;
            color: white;
            padding: 12px 20px;
            border-radius: 4px;
            z-index: 1000;
            max-width: 400px;
        `;
        
        document.body.appendChild(errorDiv);
        
        setTimeout(() => {
            errorDiv.remove();
        }, 5000);
    }
    
    showSuccess(message) {
        // Update status region for screen readers
        const statusRegion = document.getElementById('statusRegion');
        if (statusRegion) {
            statusRegion.textContent = 'Success: ' + message;
        }
        
        // Show visual success
        const successDiv = document.createElement('div');
        successDiv.className = 'success-notification';
        successDiv.textContent = message;
        successDiv.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: #107c10;
            color: white;
            padding: 12px 20px;
            border-radius: 4px;
            z-index: 1000;
            max-width: 400px;
        `;
        
        document.body.appendChild(successDiv);
        
        setTimeout(() => {
            successDiv.remove();
        }, 3000);
    }
    
    updateStreamStatus(status) {
        const statusElement = document.getElementById('streamStatus');
        if (statusElement) {
            statusElement.textContent = status;
            statusElement.className = `stream-status ${status}`;
        }
    }
    
    updateStreamInfo(data) {
        // Update UI with stream information
        console.log('Stream info:', data);
    }
    
    updateDVRIndicator() {
        const dvrIndicator = document.getElementById('dvrIndicator');
        if (dvrIndicator) {
            dvrIndicator.style.display = this.isDvrMode ? 'block' : 'none';
        }
    }
    
    updateLiveIndicator(isLive) {
        const liveIndicator = document.getElementById('liveIndicator');
        if (liveIndicator) {
            liveIndicator.classList.toggle('live', isLive);
        }
    }
    
    updateFrameIndicator() {
        if (!this.primaryPlayer) return;
        
        const currentTime = this.primaryPlayer.currentTime();
        const currentFrame = Math.floor(currentTime * this.frameRate);
        
        const frameIndicator = document.getElementById('frameIndicator');
        if (frameIndicator) {
            frameIndicator.textContent = `Frame: ${currentFrame}`;
        }
    }
    
    showExportModal() {
        const modal = document.getElementById('exportModal');
        const optionsContainer = document.getElementById('exportOptions');
        
        // Clear previous options
        optionsContainer.innerHTML = '';
        
        // Add export format options
        const formatOptions = `
            <div class="export-format">
                <h3>Export Format:</h3>
                <label><input type="radio" name="exportFormat" value="mp4" checked> MP4 (H.264)</label>
                <label><input type="radio" name="exportFormat" value="webm"> WebM</label>
                <label><input type="radio" name="exportFormat" value="dicom"> DICOM</label>
            </div>
        `;
        optionsContainer.innerHTML = formatOptions;
        
        // Add export options for each marked range
        this.marks.forEach((mark, index) => {
            const option = document.createElement('div');
            option.className = 'export-option';
            option.innerHTML = `
                <span>Range ${index + 1}: ${this.formatTime(mark.in)} - ${this.formatTime(mark.out)}</span>
                <input type="checkbox" checked data-index="${index}">
            `;
            optionsContainer.appendChild(option);
        });
        
        modal.style.display = 'block';
    }
    
    showExportProgress(show) {
        let progressDiv = document.getElementById('exportProgress');
        
        if (show) {
            if (!progressDiv) {
                progressDiv = document.createElement('div');
                progressDiv.id = 'exportProgress';
                progressDiv.innerHTML = `
                    <div class="export-progress-content">
                        <h3>Exporting Video...</h3>
                        <div class="progress-bar">
                            <div class="progress-fill" id="exportProgressFill"></div>
                        </div>
                        <div class="progress-text" id="exportProgressText">0%</div>
                    </div>
                `;
                progressDiv.style.cssText = `
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0, 0, 0, 0.8);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 2000;
                `;
                document.body.appendChild(progressDiv);
            }
        } else if (progressDiv) {
            progressDiv.remove();
        }
    }
    
    updateExportProgress(progress) {
        const fill = document.getElementById('exportProgressFill');
        const text = document.getElementById('exportProgressText');
        
        if (fill && text) {
            fill.style.width = `${progress}%`;
            text.textContent = `${Math.round(progress)}%`;
        }
    }
    
    getPatientId() {
        // In a real implementation, this would get the actual patient ID
        // from the current context or user input
        return 'PATIENT_' + Date.now();
    }
    
    jump(seconds) {
        if (this.primaryHLS) {
            this.primaryHLS.jump(seconds);
        }
    }
}

/**
 * Enhanced Streaming Player
 * Phase 1 Foundation Integration - Enhanced video-engine integration
 * Extends StreamingPlayer with direct VideoEngineClient integration
 */
class EnhancedStreamingPlayer extends StreamingPlayer {
    constructor(container, options = {}) {
        super(container, options);
        
        // Initialize VideoEngineClient directly
        this.videoEngine = new VideoEngineClient();
        
        // Enhanced integration setup
        this.setupEngineIntegration();
    }
    
    async setupEngineIntegration() {
        // Connect to FFmpeg engine events
        this.videoEngine.on('segmentCompleted', this.onSegmentCompleted.bind(this));
        this.videoEngine.on('thumbnailReady', this.onThumbnailReady.bind(this));
        this.videoEngine.on('recordingStarted', this.onRecordingStarted.bind(this));
        this.videoEngine.on('recordingStopped', this.onRecordingStopped.bind(this));
        this.videoEngine.on('recordingPaused', this.onRecordingPaused.bind(this));
        this.videoEngine.on('recordingResumed', this.onRecordingResumed.bind(this));
        this.videoEngine.on('statusUpdate', this.onEngineStatusUpdate.bind(this));
        this.videoEngine.on('error', this.onEngineError.bind(this));
        this.videoEngine.on('websocketConnected', this.onWebSocketConnected.bind(this));
        this.videoEngine.on('websocketDisconnected', this.onWebSocketDisconnected.bind(this));
        this.videoEngine.on('websocketError', this.onWebSocketError.bind(this));
        this.videoEngine.on('markerAdded', this.onMarkerAdded.bind(this));
        this.videoEngine.on('warning', this.onEngineWarning.bind(this));
        
        // Initialize WebSocket for real-time updates if session is specified
        if (this.options.sessionId) {
            await this.connectToSession(this.options.sessionId);
        }
        
        console.log('Enhanced video engine integration setup completed');
    }
    
    async connectToSession(sessionId) {
        // Use video engine's WebSocket support
        const wsUrl = `ws://${window.location.host}/ws/video/${sessionId}`;
        this.videoEngine.connectWebSocket(wsUrl);
        
        // Update current session
        this.currentSessionId = sessionId;
        
        console.log(`Enhanced player connected to session: ${sessionId}`);
    }
    
    onSegmentCompleted(segment) {
        console.log('Enhanced player: Segment completed', segment);
        
        // Update timeline with new segment
        if (this.timeline) {
            this.timeline.addSegment(segment);
        }
        
        // Update HLS playlist if in live mode
        if (this.isLive) {
            this.refreshPlaylist();
        }
        
        // Update segment indicator
        this.updateSegmentIndicator(segment);
        
        // Emit for external listeners
        this.emit('segmentCompleted', segment);
    }
    
    onThumbnailReady(data) {
        console.log('Enhanced player: Thumbnail ready', data);
        
        // Update timeline thumbnail
        if (this.timeline) {
            this.timeline.updateThumbnail(data.timestamp, data.url);
        }
        
        // Cache thumbnail
        const cacheKey = `${data.timestamp}_160`;
        this.thumbnailCache.set(cacheKey, data.url);
        
        // Emit for external listeners
        this.emit('thumbnailReady', data);
    }
    
    onRecordingStarted(session) {
        console.log('Enhanced player: Recording started', session);
        
        this.currentSessionId = session.sessionId;
        this.isRecording = true;
        this.updateRecordingIndicator(true);
        
        // Update UI state
        this.enableStreamControls(true);
        this.showRecordingControls(true);
        
        // Emit for external listeners
        this.emit('recordingStarted', session);
    }
    
    onRecordingStopped(result) {
        console.log('Enhanced player: Recording stopped', result);
        
        this.isRecording = false;
        this.updateRecordingIndicator(false);
        
        // Update UI state
        this.showRecordingControls(false);
        
        // Emit for external listeners
        this.emit('recordingStopped', result);
    }
    
    onRecordingPaused() {
        console.log('Enhanced player: Recording paused');
        
        this.isPaused = true;
        this.updatePauseIndicator(true);
        
        // Emit for external listeners
        this.emit('recordingPaused');
    }
    
    onRecordingResumed() {
        console.log('Enhanced player: Recording resumed');
        
        this.isPaused = false;
        this.updatePauseIndicator(false);
        
        // Emit for external listeners
        this.emit('recordingResumed');
    }
    
    onWebSocketConnected() {
        console.log('Enhanced player: WebSocket connected');
        
        // Update connection status
        this.wsConnected = true;
        
        // Enable real-time features
        this.enableRealTimeFeatures(true);
        
        // Emit for external listeners
        this.emit('websocketConnected');
    }
    
    onWebSocketDisconnected() {
        console.log('Enhanced player: WebSocket disconnected');
        
        // Update connection status
        this.wsConnected = false;
        
        // Show connection status
        this.showConnectionStatus('Reconnecting...', 'warning');
        
        // Emit for external listeners
        this.emit('websocketDisconnected');
    }
    
    onWebSocketError(error) {
        console.error('Enhanced player: WebSocket error', error);
        
        // Show error status
        this.showConnectionStatus('Connection Error', 'error');
        
        // Emit for external listeners
        this.emit('websocketError', error);
    }
    
    onMarkerAdded(marker) {
        console.log('Enhanced player: Marker added', marker);
        
        // Add to marks array
        this.marks.push(marker);
        
        // Update timeline markers
        if (this.timeline) {
            this.timeline.addMarker(marker);
        }
        
        // Emit for external listeners
        this.emit('markerAdded', marker);
    }
    
    onEngineWarning(warning) {
        console.warn('Enhanced player: Engine warning', warning);
        
        // Show warning indicator
        this.showWarningIndicator(warning.message);
        
        // Emit for external listeners
        this.emit('engineWarning', warning);
    }
    
    // Enhanced recording controls
    async startRecording(config = {}) {
        if (!this.videoEngine) {
            throw new Error('Video engine not available');
        }
        
        try {
            const recordingConfig = {
                patientId: config.patientId || this.getPatientId(),
                studyId: config.studyId || this.getStudyId(),
                lossless: config.lossless || true,
                frameRate: config.frameRate || this.frameRate,
                preRecordSeconds: config.preRecordSeconds || 60,
                enablePreview: config.enablePreview !== false,
                ...config
            };
            
            const session = await this.videoEngine.startRecording(recordingConfig);
            
            console.log('Enhanced player: Recording started with session', session);
            
            return session;
            
        } catch (error) {
            console.error('Enhanced player: Failed to start recording', error);
            
            // Use error recovery if available
            if (this.errorRecovery) {
                await this.errorRecovery.handleError(error, { context: 'startRecording' });
            }
            
            throw error;
        }
    }
    
    async stopRecording() {
        if (!this.videoEngine || !this.videoEngine.isRecording()) {
            console.warn('No active recording to stop');
            return null;
        }
        
        try {
            const result = await this.videoEngine.stopRecording();
            
            console.log('Enhanced player: Recording stopped', result);
            
            return result;
            
        } catch (error) {
            console.error('Enhanced player: Failed to stop recording', error);
            
            // Use error recovery if available
            if (this.errorRecovery) {
                await this.errorRecovery.handleError(error, { context: 'stopRecording' });
            }
            
            throw error;
        }
    }
    
    async pauseRecording() {
        if (!this.videoEngine || !this.videoEngine.isRecording()) {
            console.warn('No active recording to pause');
            return false;
        }
        
        try {
            await this.videoEngine.pauseRecording();
            return true;
        } catch (error) {
            console.error('Enhanced player: Failed to pause recording', error);
            return false;
        }
    }
    
    async resumeRecording() {
        if (!this.videoEngine) {
            console.warn('Video engine not available');
            return false;
        }
        
        try {
            await this.videoEngine.resumeRecording();
            return true;
        } catch (error) {
            console.error('Enhanced player: Failed to resume recording', error);
            return false;
        }
    }
    
    async takeSnapshot(format = 'JPEG') {
        if (!this.videoEngine || !this.videoEngine.isRecording()) {
            console.warn('No active recording for snapshot');
            return null;
        }
        
        try {
            const snapshot = await this.videoEngine.takeSnapshot(format);
            
            console.log('Enhanced player: Snapshot taken', snapshot);
            
            // Emit event
            this.emit('snapshotTaken', snapshot);
            
            return snapshot;
            
        } catch (error) {
            console.error('Enhanced player: Failed to take snapshot', error);
            return null;
        }
    }
    
    async addMarker(timestamp, type = 'Generic', description = '') {
        if (!this.videoEngine || !this.videoEngine.isRecording()) {
            console.warn('No active recording for marker');
            return null;
        }
        
        try {
            const marker = await this.videoEngine.addMarker(timestamp, type, description);
            
            console.log('Enhanced player: Marker added', marker);
            
            return marker;
            
        } catch (error) {
            console.error('Enhanced player: Failed to add marker', error);
            return null;
        }
    }
    
    // Enhanced thumbnail loading using VideoEngine API
    async loadThumbnail(timestamp, width = 160) {
        // Check cache first
        const cacheKey = `${timestamp}_${width}`;
        if (this.thumbnailCache.has(cacheKey)) {
            return this.thumbnailCache.get(cacheKey);
        }
        
        // Use VideoEngine API for thumbnail generation
        if (this.videoEngine && this.currentSessionId) {
            try {
                const thumbnailUrl = await this.videoEngine.getThumbnail(timestamp, width);
                if (thumbnailUrl) {
                    this.thumbnailCache.set(cacheKey, thumbnailUrl);
                    console.log(`Enhanced player: Loaded thumbnail from API: ${timestamp}s`);
                    return thumbnailUrl;
                }
            } catch (error) {
                console.warn('Enhanced player: API thumbnail failed, using fallback', error);
            }
        }
        
        // Fallback to parent class implementation
        return super.loadThumbnail(timestamp, width);
    }
    
    // Enhanced status and UI methods
    enableRealTimeFeatures(enabled) {
        this.realTimeFeaturesEnabled = enabled;
        
        if (enabled) {
            console.log('Enhanced player: Real-time features enabled');
        } else {
            console.log('Enhanced player: Real-time features disabled');
        }
    }
    
    showConnectionStatus(message, type = 'info') {
        const statusElement = document.getElementById('connectionStatus');
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `connection-status ${type}`;
            statusElement.style.display = 'block';
            
            // Auto-hide info messages
            if (type === 'info') {
                setTimeout(() => {
                    statusElement.style.display = 'none';
                }, 3000);
            }
        }
    }
    
    showWarningIndicator(message) {
        const warningElement = document.getElementById('warningIndicator');
        if (warningElement) {
            warningElement.textContent = message;
            warningElement.classList.add('visible');
            
            // Auto-hide after 5 seconds
            setTimeout(() => {
                warningElement.classList.remove('visible');
            }, 5000);
        }
    }
    
    showRecordingControls(show) {
        const controls = document.getElementById('recordingControlsPanel');
        if (controls) {
            controls.style.display = show ? 'block' : 'none';
        }
        
        // Show/hide enhanced controls
        const enhancedControls = ['markCriticalMomentButton'];
        enhancedControls.forEach(controlId => {
            const control = document.getElementById(controlId);
            if (control) {
                control.classList.toggle('hidden', !show);
            }
        });
    }
    
    // Enhanced cleanup
    async cleanup() {
        console.log('Enhanced player: Starting cleanup');
        
        // Stop any active recording
        if (this.videoEngine && this.videoEngine.isRecording()) {
            try {
                await this.videoEngine.stopRecording();
            } catch (error) {
                console.error('Enhanced player: Error stopping recording during cleanup', error);
            }
        }
        
        // Call parent cleanup
        super.cleanup();
        
        console.log('Enhanced player: Cleanup completed');
    }
    
    // Utility methods
    getStudyId() {
        // This would typically come from the current medical study context
        return 'STUDY_' + Date.now();
    }
    
    // Enhanced refresh playlist with VideoEngine integration
    async refreshPlaylist() {
        if (this.primaryHLS && this.primaryHLS.url) {
            try {
                // Get latest segments from VideoEngine if available
                if (this.videoEngine && this.currentSessionId) {
                    const segments = await this.videoEngine.getEditableSegments();
                    if (segments && segments.length > 0) {
                        console.log(`Enhanced player: Found ${segments.length} segments for playlist refresh`);
                    }
                }
                
                // Refresh HLS playlist
                this.primaryHLS.loadSource(this.primaryHLS.url);
                
            } catch (error) {
                console.error('Enhanced player: Failed to refresh playlist', error);
            }
        }
    }
}

// Export both classes for external use
window.StreamingPlayer = StreamingPlayer;
window.EnhancedStreamingPlayer = EnhancedStreamingPlayer;

// Add high contrast styles
const style = document.createElement('style');
style.textContent = `
    .high-contrast {
        filter: contrast(1.5);
    }
    
    .high-contrast .control-button {
        border: 2px solid white;
    }
    
    .high-contrast .control-button:focus {
        outline: 3px solid yellow;
        outline-offset: 2px;
    }
    
    .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        padding: 0;
        margin: -1px;
        overflow: hidden;
        clip: rect(0, 0, 0, 0);
        white-space: nowrap;
        border: 0;
    }
    
    .progress-bar {
        width: 300px;
        height: 20px;
        background: #333;
        border-radius: 10px;
        overflow: hidden;
        margin: 20px 0;
    }
    
    .progress-fill {
        height: 100%;
        background: #0078d4;
        transition: width 0.3s ease;
    }
    
    .export-progress-content {
        background: white;
        padding: 30px;
        border-radius: 8px;
        text-align: center;
    }
    
    .export-progress-content h3 {
        color: #333;
        margin-bottom: 20px;
    }
    
    .progress-text {
        color: #666;
        font-size: 18px;
        font-weight: bold;
    }
`;
document.head.appendChild(style);

// Initialize player when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // Load required scripts first
    const scripts = [
        'js/streaming-config.js',
        'js/auth-manager.js',
        'js/hls-manager.js',
        'js/medical-export-manager.js'
    ];
    
    Promise.all(scripts.map(src => {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    })).then(() => {
        // Initialize player after all dependencies are loaded
        window.streamingPlayer = new StreamingPlayer();
    }).catch(error => {
        console.error('Failed to load dependencies:', error);
    });
});