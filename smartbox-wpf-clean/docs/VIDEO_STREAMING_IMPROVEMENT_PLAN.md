# Video Streaming Client Improvement Plan

## Executive Summary

This document outlines a comprehensive plan to improve the video streaming client/website to fully leverage the new FFmpeg video engine implemented in the SmartBox medical capture system. The improvements focus on integrating real-time capabilities, unifying components, and enhancing playback quality while maintaining medical-grade standards.

## 📋 Objectives

1. **Integrate** streaming player with new FFmpeg video engine API
2. **Unify** timeline components and eliminate duplication
3. **Enhance** real-time capabilities with WebSocket integration
4. **Improve** playback quality with adaptive bitrate and buffering
5. **Strengthen** error recovery and resilience
6. **Modernize** codebase structure and patterns

## ✅ Pre-Operation Validation

### Resource Requirements
- **Token usage**: ~25K estimated
- **Complexity score**: 0.75 (High)
- **Risk assessment**: Medium (existing functionality must remain stable)
- **Files to modify**: 8-10 JavaScript files
- **New files to create**: 3-4 enhancement modules

### Compatibility Checks
- ✅ New FFmpeg API is operational
- ✅ WebSocket infrastructure in place
- ✅ HLS.js library compatible
- ⚠️ Multiple timeline implementations need consolidation
- ⚠️ Duplicate streaming services need unification

## 📐 Incremental Implementation Plan

### Phase 1: Foundation Integration (Priority: High)

#### 1.1 Connect Streaming Player to FFmpeg API

**File**: `streaming-player.js`

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

#### 1.2 WebSocket Real-time Updates

**File**: `streaming-websocket-handler.js` (New)

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

#### 1.3 Unified Thumbnail System

**Updates to**: `streaming-player.js`

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

### Phase 2: Timeline Consolidation (Priority: High)

#### 2.1 Create Unified Timeline Component

**File**: `unified-timeline.js` (New)

```javascript
// Unified timeline component combining best features
class UnifiedTimeline extends EventTarget {
    constructor(container, options = {}) {
        super();
        this.container = container;
        this.options = {
            minScale: 30,        // 30 seconds minimum view
            maxScale: 3600,      // 1 hour maximum view
            defaultScale: 300,   // 5 minutes default
            segmentDuration: 10, // FFmpeg segment duration
            thumbnailWidth: 160,
            thumbnailHeight: 90,
            showSegmentBoundaries: true,
            enableTouch: true,
            enableWheel: true,
            enableKeyboard: true,
            ...options
        };
        
        this.segments = [];
        this.markers = [];
        this.thumbnailCache = new Map();
        this.scale = this.options.defaultScale;
        this.position = 0;
        
        this.setupVideoEngineIntegration();
        this.render();
        this.attachEventListeners();
    }
    
    setupVideoEngineIntegration() {
        this.thumbnailSource = 'api'; // Use FFmpeg API for thumbnails
        this.segmentAware = true;     // Show segment boundaries
        this.liveUpdates = true;      // Enable real-time updates
    }
    
    addSegment(segment) {
        this.segments.push({
            number: segment.segmentNumber,
            startTime: segment.startTime,
            duration: segment.duration,
            endTime: segment.startTime + segment.duration,
            isComplete: segment.isComplete,
            canEdit: segment.canEdit
        });
        
        this.renderSegment(segment);
        this.emit('segmentAdded', segment);
    }
    
    renderSegment(segment) {
        const segmentElement = document.createElement('div');
        segmentElement.className = 'timeline-segment';
        segmentElement.dataset.segmentNumber = segment.number;
        
        const startX = this.timeToPixels(segment.startTime);
        const width = this.timeToPixels(segment.duration);
        
        segmentElement.style.left = `${startX}px`;
        segmentElement.style.width = `${width}px`;
        
        if (segment.isComplete) {
            segmentElement.classList.add('complete');
        }
        
        if (segment.canEdit) {
            segmentElement.classList.add('editable');
            segmentElement.addEventListener('click', () => {
                this.emit('segmentClick', segment);
            });
        }
        
        this.segmentContainer.appendChild(segmentElement);
    }
    
    async updateThumbnail(timestamp, url) {
        // Cache thumbnail
        this.thumbnailCache.set(timestamp, url);
        
        // Update visible thumbnails
        const thumbnailElement = this.container.querySelector(
            `[data-timestamp="${timestamp}"]`
        );
        
        if (thumbnailElement) {
            thumbnailElement.style.backgroundImage = `url(${url})`;
            thumbnailElement.classList.add('loaded');
        }
    }
    
    // Intelligent scaling from adaptive-timeline.js
    setScale(newScale) {
        // Constrain scale
        newScale = Math.max(this.options.minScale, 
                   Math.min(this.options.maxScale, newScale));
        
        if (newScale !== this.scale) {
            const oldScale = this.scale;
            this.scale = newScale;
            
            // Adjust position to keep center point stable
            const centerTime = this.position + (this.viewportWidth / 2) / this.pixelsPerSecond;
            this.pixelsPerSecond = this.viewportWidth / this.scale;
            this.position = centerTime - (this.viewportWidth / 2) / this.pixelsPerSecond;
            
            this.render();
            this.emit('scaleChanged', { oldScale, newScale });
        }
    }
    
    // Touch gesture support from timeline-integration.js
    attachEventListeners() {
        if (this.options.enableTouch) {
            this.setupTouchGestures();
        }
        
        if (this.options.enableWheel) {
            this.container.addEventListener('wheel', this.onWheel.bind(this), 
                { passive: false });
        }
        
        if (this.options.enableKeyboard) {
            this.container.addEventListener('keydown', this.onKeyDown.bind(this));
        }
    }
    
    setupTouchGestures() {
        let touches = [];
        let lastDistance = 0;
        
        this.container.addEventListener('touchstart', (e) => {
            touches = Array.from(e.touches);
            if (touches.length === 2) {
                lastDistance = this.getTouchDistance(touches);
            }
        });
        
        this.container.addEventListener('touchmove', (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                const newDistance = this.getTouchDistance(Array.from(e.touches));
                const scale = newDistance / lastDistance;
                this.setScale(this.scale * scale);
                lastDistance = newDistance;
            }
        });
    }
}
```

#### 2.2 Migration Strategy

**File**: `timeline-migration.js` (Temporary)

```javascript
// Helper to migrate from old timeline implementations
class TimelineMigration {
    static migrateFromAdaptiveTimeline(adaptiveTimeline) {
        const unified = new UnifiedTimeline(adaptiveTimeline.container, {
            minScale: adaptiveTimeline.minScale,
            maxScale: adaptiveTimeline.maxScale,
            defaultScale: adaptiveTimeline.currentScale
        });
        
        // Transfer state
        unified.position = adaptiveTimeline.currentPosition;
        unified.markers = [...adaptiveTimeline.markers];
        
        // Transfer event listeners
        adaptiveTimeline.eventListeners.forEach((listeners, event) => {
            listeners.forEach(listener => {
                unified.addEventListener(event, listener);
            });
        });
        
        return unified;
    }
}
```

### Phase 3: Playback Enhancements (Priority: Medium)

#### 3.1 Adaptive Bitrate Switching

**File**: `adaptive-bitrate-manager.js` (New)

```javascript
class AdaptiveBitrateManager {
    constructor(player, hlsInstance) {
        this.player = player;
        this.hls = hlsInstance;
        this.measurements = [];
        this.qualityLevels = [];
        this.autoMode = true;
        this.setupMonitoring();
    }
    
    setupMonitoring() {
        // Monitor bandwidth and buffer health
        this.hls.on(Hls.Events.FRAG_LOADED, this.onFragmentLoaded.bind(this));
        this.hls.on(Hls.Events.LEVEL_LOADED, this.onLevelLoaded.bind(this));
        
        // Periodic quality evaluation
        this.evaluationInterval = setInterval(() => {
            if (this.autoMode) {
                this.evaluateQuality();
            }
        }, 2000);
    }
    
    onFragmentLoaded(event, data) {
        // Collect bandwidth measurements
        const loadTime = data.stats.tload - data.stats.trequest;
        const bandwidth = (data.stats.total * 8) / loadTime; // bits per second
        
        this.measurements.push({
            timestamp: Date.now(),
            bandwidth: bandwidth,
            latency: data.stats.tfirst - data.stats.trequest
        });
        
        // Keep only recent measurements (last 30 seconds)
        const cutoff = Date.now() - 30000;
        this.measurements = this.measurements.filter(m => m.timestamp > cutoff);
    }
    
    evaluateQuality() {
        const avgBandwidth = this.getAverageBandwidth();
        const bufferHealth = this.getBufferHealth();
        const currentLevel = this.hls.currentLevel;
        
        // Calculate optimal level based on bandwidth and buffer
        const optimalLevel = this.calculateOptimalLevel(avgBandwidth, bufferHealth);
        
        if (optimalLevel !== currentLevel && optimalLevel !== -1) {
            console.log(`Switching quality from ${currentLevel} to ${optimalLevel}`);
            this.hls.currentLevel = optimalLevel;
            
            this.player.emit('qualityChanged', {
                previousLevel: currentLevel,
                newLevel: optimalLevel,
                auto: true,
                bandwidth: avgBandwidth,
                bufferLength: bufferHealth.length
            });
        }
    }
    
    getAverageBandwidth() {
        if (this.measurements.length === 0) return 0;
        
        // Weighted average giving more weight to recent measurements
        let weightedSum = 0;
        let weightSum = 0;
        
        this.measurements.forEach((m, index) => {
            const weight = (index + 1) / this.measurements.length;
            weightedSum += m.bandwidth * weight;
            weightSum += weight;
        });
        
        return weightedSum / weightSum;
    }
    
    getBufferHealth() {
        const buffered = this.player.video.buffered;
        const currentTime = this.player.video.currentTime;
        
        let bufferLength = 0;
        let bufferHoles = 0;
        
        for (let i = 0; i < buffered.length; i++) {
            const start = buffered.start(i);
            const end = buffered.end(i);
            
            if (start <= currentTime && currentTime <= end) {
                bufferLength = end - currentTime;
            }
            
            if (i > 0 && start > buffered.end(i - 1)) {
                bufferHoles++;
            }
        }
        
        return {
            length: bufferLength,
            holes: bufferHoles,
            isHealthy: bufferLength > 10 && bufferHoles === 0
        };
    }
    
    calculateOptimalLevel(bandwidth, bufferHealth) {
        const levels = this.hls.levels;
        
        // Safety margin - use 70% of available bandwidth
        const safeBandwidth = bandwidth * 0.7;
        
        // Find highest quality that fits within bandwidth
        let optimalLevel = -1;
        
        for (let i = levels.length - 1; i >= 0; i--) {
            if (levels[i].bitrate <= safeBandwidth) {
                optimalLevel = i;
                break;
            }
        }
        
        // Buffer-based adjustments
        if (!bufferHealth.isHealthy) {
            // Drop quality if buffer is unhealthy
            optimalLevel = Math.max(0, optimalLevel - 1);
        } else if (bufferHealth.length > 30) {
            // Try higher quality if buffer is very healthy
            optimalLevel = Math.min(levels.length - 1, optimalLevel + 1);
        }
        
        return optimalLevel;
    }
    
    // Manual quality control
    setQualityLevel(level) {
        this.autoMode = false;
        this.hls.currentLevel = level;
        
        this.player.emit('qualityChanged', {
            previousLevel: this.hls.currentLevel,
            newLevel: level,
            auto: false
        });
    }
    
    enableAutoQuality() {
        this.autoMode = true;
        this.hls.currentLevel = -1; // Auto mode
    }
    
    destroy() {
        if (this.evaluationInterval) {
            clearInterval(this.evaluationInterval);
        }
    }
}
```

#### 3.2 Enhanced Buffering Strategy

**File**: `medical-buffering-config.js` (New)

```javascript
// Medical-grade buffering configuration
const MedicalBufferingConfig = {
    // Development/Testing configuration
    development: {
        backBufferLength: 30,    // 30 seconds back buffer
        maxBufferLength: 30,     // 30 seconds forward buffer
        maxMaxBufferLength: 120, // 2 minutes total
        
        // Frame-accurate seeking
        nudgeOffset: 0.001,
        maxFragLookUpTolerance: 0.001,
        
        // Network settings
        manifestLoadingTimeOut: 10000,
        manifestLoadingMaxRetry: 3,
        manifestLoadingRetryDelay: 500,
        
        // Performance
        startFragPrefetch: true,
        testBandwidth: true,
        progressive: true
    },
    
    // Production configuration for medical use
    production: {
        backBufferLength: 120,   // 2 minutes back buffer for scrubbing
        maxBufferLength: 60,     // 1 minute forward buffer
        maxMaxBufferLength: 600, // 10 minutes total
        
        // Ultra-precise seeking for medical review
        nudgeOffset: 0.0001,
        maxFragLookUpTolerance: 0.0001,
        
        // Aggressive loading for reliability
        manifestLoadingTimeOut: 20000,
        manifestLoadingMaxRetry: 6,
        manifestLoadingRetryDelay: 1000,
        
        // Maximum performance
        startFragPrefetch: true,
        testBandwidth: true,
        progressive: true,
        
        // Low latency for live streaming
        liveBackBufferLength: 5,
        liveSyncDuration: 3,
        liveMaxLatencyDuration: 10,
        
        // Memory management
        maxBufferSize: 600 * 1000 * 1000, // 600 MB
        maxBufferHole: 0.5,
        
        // Error recovery
        fragLoadingTimeOut: 20000,
        fragLoadingMaxRetry: 6,
        fragLoadingRetryDelay: 1000,
        
        enableSoftwareAES: false,
        stretchShortVideoTrack: false,
        forceKeyFrameOnDiscontinuity: true
    },
    
    // Get configuration based on environment
    getConfig(environment = 'production') {
        return this[environment] || this.production;
    }
};

// Export for use in HLS initialization
export default MedicalBufferingConfig;
```

#### 3.3 Frame-Accurate Controls

**File**: `frame-accurate-controls.js` (New)

```javascript
class FrameAccurateControls {
    constructor(player, options = {}) {
        this.player = player;
        this.video = player.video;
        this.options = {
            frameRate: 30, // Default frame rate
            showFrameNumber: true,
            enableKeyboardShortcuts: true,
            hapticFeedback: true,
            visualFeedback: true,
            ...options
        };
        
        this.currentFrame = 0;
        this.totalFrames = 0;
        
        this.setupControls();
        this.attachEventListeners();
    }
    
    setupControls() {
        // Create frame control UI
        this.controlsContainer = document.createElement('div');
        this.controlsContainer.className = 'frame-accurate-controls';
        
        // Frame step buttons
        this.prevFrameBtn = this.createButton('⏮', 'Previous Frame', 
            () => this.stepFrames(-1));
        this.nextFrameBtn = this.createButton('⏭', 'Next Frame', 
            () => this.stepFrames(1));
        
        // Frame number display
        if (this.options.showFrameNumber) {
            this.frameDisplay = document.createElement('div');
            this.frameDisplay.className = 'frame-display';
            this.updateFrameDisplay();
            this.controlsContainer.appendChild(this.frameDisplay);
        }
        
        // Speed control with medical presets
        this.speedControl = new SpeedControl(this.player, {
            presets: {
                surgical: [0.1, 0.25, 0.5, 1],
                review: [0.5, 1, 2, 4],
                scan: [1, 2, 4, 8, 16]
            },
            defaultPreset: 'review'
        });
        
        this.controlsContainer.appendChild(this.prevFrameBtn);
        this.controlsContainer.appendChild(this.nextFrameBtn);
        this.controlsContainer.appendChild(this.speedControl.element);
        
        this.player.controlsContainer.appendChild(this.controlsContainer);
    }
    
    createButton(icon, title, onClick) {
        const button = document.createElement('button');
        button.className = 'frame-control-button';
        button.innerHTML = icon;
        button.title = title;
        button.setAttribute('aria-label', title);
        button.addEventListener('click', onClick);
        return button;
    }
    
    stepFrames(frames) {
        const frameDuration = 1 / this.options.frameRate;
        const currentTime = this.video.currentTime;
        const newTime = currentTime + (frames * frameDuration);
        
        // Ensure we don't go beyond bounds
        const clampedTime = Math.max(0, Math.min(newTime, this.video.duration));
        
        // Set time with high precision
        this.seekToTime(clampedTime);
        
        // Visual feedback
        if (this.options.visualFeedback) {
            this.showFrameStepFeedback(frames);
        }
        
        // Haptic feedback (if supported)
        if (this.options.hapticFeedback && 'vibrate' in navigator) {
            navigator.vibrate(10);
        }
    }
    
    seekToTime(time) {
        // Use precision seeking for medical accuracy
        this.video.currentTime = time;
        
        // For HLS streams, ensure frame accuracy
        if (this.player.hls) {
            // Force fragment loading at exact position
            this.player.hls.startLoad(time);
        }
        
        this.updateFrameDisplay();
        this.player.emit('frameSeeked', {
            frame: this.currentFrame,
            time: time
        });
    }
    
    showFrameStepFeedback(direction) {
        const feedback = document.createElement('div');
        feedback.className = 'frame-step-feedback';
        feedback.textContent = direction > 0 ? '+1' : '-1';
        
        this.controlsContainer.appendChild(feedback);
        
        // Animate and remove
        requestAnimationFrame(() => {
            feedback.classList.add('animate');
            setTimeout(() => feedback.remove(), 500);
        });
    }
    
    updateFrameDisplay() {
        if (!this.options.showFrameNumber) return;
        
        this.currentFrame = Math.floor(this.video.currentTime * this.options.frameRate);
        this.totalFrames = Math.floor(this.video.duration * this.options.frameRate);
        
        this.frameDisplay.textContent = `Frame: ${this.currentFrame} / ${this.totalFrames}`;
        this.frameDisplay.setAttribute('aria-label', 
            `Frame ${this.currentFrame} of ${this.totalFrames}`);
    }
    
    attachEventListeners() {
        // Update frame display on time changes
        this.video.addEventListener('timeupdate', () => this.updateFrameDisplay());
        
        // Keyboard shortcuts
        if (this.options.enableKeyboardShortcuts) {
            document.addEventListener('keydown', (e) => {
                if (e.target.tagName === 'INPUT') return;
                
                switch(e.key) {
                    case 'ArrowLeft':
                        if (e.shiftKey) {
                            this.stepFrames(-10); // 10 frames back
                        } else {
                            this.stepFrames(-1);  // 1 frame back
                        }
                        e.preventDefault();
                        break;
                        
                    case 'ArrowRight':
                        if (e.shiftKey) {
                            this.stepFrames(10);  // 10 frames forward
                        } else {
                            this.stepFrames(1);   // 1 frame forward
                        }
                        e.preventDefault();
                        break;
                        
                    case ',':
                        this.stepFrames(-1);
                        e.preventDefault();
                        break;
                        
                    case '.':
                        this.stepFrames(1);
                        e.preventDefault();
                        break;
                }
            });
        }
    }
}

// Speed control component
class SpeedControl {
    constructor(player, options) {
        this.player = player;
        this.video = player.video;
        this.options = options;
        this.currentPreset = options.defaultPreset;
        
        this.element = this.createSpeedControl();
    }
    
    createSpeedControl() {
        const container = document.createElement('div');
        container.className = 'speed-control';
        
        // Preset selector
        const presetSelector = document.createElement('select');
        presetSelector.className = 'speed-preset-selector';
        
        Object.keys(this.options.presets).forEach(preset => {
            const option = document.createElement('option');
            option.value = preset;
            option.textContent = preset.charAt(0).toUpperCase() + preset.slice(1);
            presetSelector.appendChild(option);
        });
        
        presetSelector.value = this.currentPreset;
        presetSelector.addEventListener('change', (e) => {
            this.currentPreset = e.target.value;
            this.updateSpeedButtons();
        });
        
        // Speed buttons
        this.speedButtons = document.createElement('div');
        this.speedButtons.className = 'speed-buttons';
        this.updateSpeedButtons();
        
        container.appendChild(presetSelector);
        container.appendChild(this.speedButtons);
        
        return container;
    }
    
    updateSpeedButtons() {
        this.speedButtons.innerHTML = '';
        
        const speeds = this.options.presets[this.currentPreset];
        speeds.forEach(speed => {
            const button = document.createElement('button');
            button.className = 'speed-button';
            button.textContent = `${speed}x`;
            button.setAttribute('aria-label', `Playback speed ${speed}x`);
            
            if (this.video.playbackRate === speed) {
                button.classList.add('active');
            }
            
            button.addEventListener('click', () => {
                this.video.playbackRate = speed;
                this.updateActiveButton();
                this.player.emit('speedChanged', { speed, preset: this.currentPreset });
            });
            
            this.speedButtons.appendChild(button);
        });
    }
    
    updateActiveButton() {
        this.speedButtons.querySelectorAll('.speed-button').forEach(btn => {
            btn.classList.toggle('active', 
                parseFloat(btn.textContent) === this.video.playbackRate);
        });
    }
}
```

### Phase 4: Error Recovery & Resilience (Priority: Medium)

#### 4.1 Robust Error Handling

**File**: `stream-error-recovery.js` (New)

```javascript
class StreamErrorRecovery {
    constructor(player) {
        this.player = player;
        this.retryCount = 0;
        this.maxRetries = 5;
        this.backoffMultiplier = 2;
        this.baseDelay = 1000;
        
        this.setupErrorHandlers();
    }
    
    setupErrorHandlers() {
        // HLS.js error handling
        if (this.player.hls) {
            this.player.hls.on(Hls.Events.ERROR, (event, data) => {
                this.handleHlsError(data);
            });
        }
        
        // Video element error handling
        this.player.video.addEventListener('error', (e) => {
            this.handleVideoError(e);
        });
        
        // Network monitoring
        window.addEventListener('online', () => this.handleNetworkRestore());
        window.addEventListener('offline', () => this.handleNetworkLoss());
    }
    
    async handleHlsError(data) {
        console.error('HLS Error:', data);
        
        if (data.fatal) {
            switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    await this.handleNetworkError(data);
                    break;
                    
                case Hls.ErrorTypes.MEDIA_ERROR:
                    await this.handleMediaError(data);
                    break;
                    
                default:
                    await this.handleFatalError(data);
                    break;
            }
        } else {
            // Non-fatal errors - log and continue
            console.warn('Non-fatal HLS error:', data);
            this.player.emit('warning', {
                type: 'hls_error',
                details: data.details,
                fatal: false
            });
        }
    }
    
    async handleNetworkError(data) {
        console.log(`Network error, retry ${this.retryCount + 1}/${this.maxRetries}`);
        
        if (this.retryCount < this.maxRetries) {
            this.retryCount++;
            
            // Calculate delay with exponential backoff and jitter
            const delay = this.baseDelay * Math.pow(this.backoffMultiplier, this.retryCount - 1);
            const jitter = Math.random() * 1000;
            const totalDelay = delay + jitter;
            
            this.player.emit('retrying', {
                attempt: this.retryCount,
                delay: totalDelay,
                error: data
            });
            
            await this.wait(totalDelay);
            
            // Try recovery strategies in order
            const recovered = await this.tryRecoveryStrategies([
                () => this.reloadManifest(),
                () => this.switchToAlternativeStream(),
                () => this.downgradeQuality(),
                () => this.fallbackToLowerProtocol()
            ]);
            
            if (recovered) {
                this.retryCount = 0;
                this.player.emit('recovered', { strategy: recovered });
            } else {
                await this.handleNetworkError(data); // Recursive retry
            }
        } else {
            await this.handleFatalError(data);
        }
    }
    
    async handleMediaError(data) {
        console.log('Media error, attempting recovery');
        
        try {
            // First try: Recover media error
            this.player.hls.recoverMediaError();
            
            // Monitor if recovery worked
            await this.wait(2000);
            
            if (this.player.video.error) {
                // Second try: Swap codecs
                console.log('Media recovery failed, swapping codecs');
                this.player.hls.swapAudioCodec();
                this.player.hls.recoverMediaError();
                
                await this.wait(2000);
                
                if (this.player.video.error) {
                    // Final attempt: Full reload
                    await this.reloadPlayer();
                }
            }
        } catch (error) {
            console.error('Media recovery failed:', error);
            await this.handleFatalError(data);
        }
    }
    
    async tryRecoveryStrategies(strategies) {
        for (const strategy of strategies) {
            try {
                const result = await strategy();
                if (result) {
                    return result;
                }
            } catch (error) {
                console.warn('Recovery strategy failed:', error);
            }
        }
        return null;
    }
    
    async reloadManifest() {
        console.log('Reloading manifest...');
        this.player.hls.startLoad();
        await this.wait(2000);
        return !this.player.video.error ? 'manifest_reload' : null;
    }
    
    async switchToAlternativeStream() {
        if (this.player.alternativeUrls && this.player.alternativeUrls.length > 0) {
            console.log('Switching to alternative stream...');
            const altUrl = this.player.alternativeUrls.shift();
            this.player.loadStream(altUrl);
            await this.wait(3000);
            return !this.player.video.error ? 'alternative_stream' : null;
        }
        return null;
    }
    
    async downgradeQuality() {
        if (this.player.hls.levels.length > 1) {
            console.log('Downgrading quality...');
            const currentLevel = this.player.hls.currentLevel;
            if (currentLevel > 0) {
                this.player.hls.currentLevel = 0; // Lowest quality
                await this.wait(1000);
                return !this.player.video.error ? 'quality_downgrade' : null;
            }
        }
        return null;
    }
    
    async fallbackToLowerProtocol() {
        // Implement protocol fallback if available
        // e.g., HTTPS -> HTTP, HLS -> Progressive download
        return null;
    }
    
    async reloadPlayer() {
        console.log('Performing full player reload...');
        
        const currentTime = this.player.video.currentTime;
        const wasPlaying = !this.player.video.paused;
        
        // Destroy current instance
        this.player.destroy();
        
        // Recreate player
        await this.player.initialize();
        
        // Restore state
        this.player.seek(currentTime);
        if (wasPlaying) {
            this.player.play();
        }
        
        return 'full_reload';
    }
    
    handleNetworkLoss() {
        console.log('Network connection lost');
        this.player.emit('offline');
        
        // Pause playback to prevent errors
        this.player.pause();
        
        // Show offline message
        this.player.showMessage('Network connection lost. Playback paused.', 'warning');
    }
    
    handleNetworkRestore() {
        console.log('Network connection restored');
        this.player.emit('online');
        
        // Reload manifest and resume
        this.player.hls.startLoad();
        this.player.hideMessage();
    }
    
    handleFatalError(data) {
        console.error('Fatal error, cannot recover:', data);
        
        this.player.emit('fatalError', data);
        this.player.showMessage(
            'Playback error. Please refresh the page or try again later.',
            'error'
        );
        
        // Disable player
        this.player.disable();
    }
    
    wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
    
    reset() {
        this.retryCount = 0;
    }
}
```

### Phase 5: Advanced Features (Priority: Low)

#### 5.1 Collaborative Features

**File**: `collaborative-features.js` (New)

```javascript
class CollaborativeFeatures {
    constructor(player, websocket) {
        this.player = player;
        this.ws = websocket;
        this.users = new Map();
        this.localUser = {
            id: this.generateUserId(),
            name: 'User',
            color: this.generateUserColor()
        };
        
        this.setupCollaboration();
    }
    
    setupCollaboration() {
        // Listen for remote events
        this.ws.addEventListener('message', (event) => {
            const message = JSON.parse(event.data);
            if (message.type.startsWith('collab:')) {
                this.handleCollaborativeMessage(message);
            }
        });
        
        // Announce presence
        this.announcePresence();
        
        // Set up heartbeat
        this.heartbeatInterval = setInterval(() => {
            this.sendHeartbeat();
        }, 30000);
    }
    
    handleCollaborativeMessage(message) {
        const { userId, data } = message;
        
        switch (message.type) {
            case 'collab:userJoined':
                this.addUser(data.user);
                break;
                
            case 'collab:userLeft':
                this.removeUser(userId);
                break;
                
            case 'collab:marker':
                this.showRemoteMarker(userId, data.marker);
                break;
                
            case 'collab:annotation':
                this.showRemoteAnnotation(userId, data.annotation);
                break;
                
            case 'collab:playbackSync':
                this.handlePlaybackSync(userId, data);
                break;
                
            case 'collab:cursor':
                this.updateRemoteCursor(userId, data.position);
                break;
        }
    }
    
    announcePresence() {
        this.broadcast('collab:userJoined', {
            user: this.localUser
        });
    }
    
    addUser(user) {
        this.users.set(user.id, user);
        this.player.emit('userJoined', user);
        
        // Show user indicator
        this.showUserIndicator(user);
    }
    
    removeUser(userId) {
        const user = this.users.get(userId);
        if (user) {
            this.users.delete(userId);
            this.player.emit('userLeft', user);
            
            // Remove user indicator
            this.hideUserIndicator(userId);
        }
    }
    
    // Marker collaboration
    addMarker(timestamp, type, description) {
        const marker = {
            id: this.generateId(),
            timestamp: timestamp,
            type: type,
            description: description,
            userId: this.localUser.id,
            userColor: this.localUser.color,
            createdAt: Date.now()
        };
        
        // Add locally
        this.player.timeline.addMarker(marker);
        
        // Broadcast to others
        this.broadcast('collab:marker', { marker });
        
        return marker;
    }
    
    showRemoteMarker(userId, marker) {
        const user = this.users.get(userId);
        if (user) {
            // Add marker with user info
            marker.userName = user.name;
            marker.userColor = user.color;
            marker.isRemote = true;
            
            this.player.timeline.addMarker(marker);
        }
    }
    
    // Annotation collaboration
    addAnnotation(timestamp, text, position) {
        const annotation = {
            id: this.generateId(),
            timestamp: timestamp,
            text: text,
            position: position,
            userId: this.localUser.id,
            userColor: this.localUser.color,
            createdAt: Date.now()
        };
        
        // Add locally
        this.player.annotationLayer.addAnnotation(annotation);
        
        // Broadcast to others
        this.broadcast('collab:annotation', { annotation });
        
        return annotation;
    }
    
    // Synchronized playback
    enableSyncedPlayback(masterId) {
        this.syncMaster = masterId;
        this.syncEnabled = true;
        
        if (masterId === this.localUser.id) {
            // As master, broadcast playback state
            this.player.on('play', () => this.broadcastPlaybackState());
            this.player.on('pause', () => this.broadcastPlaybackState());
            this.player.on('seek', () => this.broadcastPlaybackState());
        }
    }
    
    broadcastPlaybackState() {
        if (this.syncEnabled && this.syncMaster === this.localUser.id) {
            this.broadcast('collab:playbackSync', {
                playing: !this.player.video.paused,
                currentTime: this.player.video.currentTime,
                playbackRate: this.player.video.playbackRate
            });
        }
    }
    
    handlePlaybackSync(userId, data) {
        if (this.syncEnabled && userId === this.syncMaster) {
            // Sync to master's state
            this.player.seek(data.currentTime);
            this.player.video.playbackRate = data.playbackRate;
            
            if (data.playing && this.player.video.paused) {
                this.player.play();
            } else if (!data.playing && !this.player.video.paused) {
                this.player.pause();
            }
        }
    }
    
    // Helper methods
    broadcast(type, data) {
        this.ws.send(JSON.stringify({
            type: type,
            userId: this.localUser.id,
            data: data
        }));
    }
    
    generateUserId() {
        return `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    generateUserColor() {
        const colors = [
            '#FF6B6B', '#4ECDC4', '#45B7D1', '#F9CA24',
            '#6C5CE7', '#A29BFE', '#FD79A8', '#FDCB6E'
        ];
        return colors[Math.floor(Math.random() * colors.length)];
    }
    
    generateId() {
        return `${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    destroy() {
        if (this.heartbeatInterval) {
            clearInterval(this.heartbeatInterval);
        }
        
        // Announce departure
        this.broadcast('collab:userLeft', {});
    }
}
```

## Implementation Schedule

### Week 1
- ✅ Phase 1.1: Connect streaming player to FFmpeg API
- ✅ Phase 1.2: Implement WebSocket integration
- ✅ Phase 1.3: Unified thumbnail system

### Week 2
- ✅ Phase 2.1: Create unified timeline component
- ✅ Phase 2.2: Migrate existing timeline code
- ✅ Phase 3.1: Implement adaptive bitrate

### Week 3
- ✅ Phase 3.2: Enhanced buffering configuration
- ✅ Phase 3.3: Frame-accurate controls
- ✅ Phase 4.1: Error recovery system

### Week 4
- ✅ Phase 4.2: Connection resilience
- ✅ Phase 5.1: Collaborative features
- ✅ Testing and refinement

## Testing Strategy

### Unit Tests
- Timeline component functionality
- Adaptive bitrate calculations
- Error recovery mechanisms
- Frame accuracy validation

### Integration Tests
- FFmpeg API integration
- WebSocket communication
- HLS playback with new features
- Cross-browser compatibility

### Performance Tests
- Buffering efficiency
- Memory usage monitoring
- CPU usage during playback
- Network bandwidth optimization

### User Acceptance Tests
- Medical professional workflow validation
- Touch gesture responsiveness
- Error recovery user experience
- Collaborative feature usability

## Success Metrics

1. **Performance Metrics**
   - Playback start time < 2 seconds
   - Segment update latency < 500ms
   - Frame seeking accuracy ± 1 frame
   - Buffer underruns < 0.1%

2. **Reliability Metrics**
   - Error recovery success rate > 95%
   - WebSocket reconnection rate > 99%
   - Stream availability > 99.9%
   - Crash rate < 0.01%

3. **User Experience Metrics**
   - Timeline responsiveness < 16ms
   - Touch gesture recognition > 99%
   - Quality switching time < 1 second
   - Thumbnail load time < 100ms

## Risk Mitigation

1. **Backward Compatibility**
   - Maintain fallback to original implementation
   - Feature flags for gradual rollout
   - Extensive testing before deployment

2. **Performance Impact**
   - Profile and optimize critical paths
   - Implement lazy loading strategies
   - Monitor memory usage closely

3. **Network Reliability**
   - Multiple fallback strategies
   - Offline capability for cached content
   - Progressive enhancement approach

## Conclusion

This improvement plan provides a comprehensive roadmap for enhancing the video streaming client to fully leverage the new FFmpeg video engine. The phased approach ensures stability while progressively adding advanced features. The focus on medical-grade quality, reliability, and user experience will result in a best-in-class video streaming solution for medical professionals.