# Phase 3: Playback Enhancements (Priority: Medium)

This phase focuses on improving playback quality through adaptive bitrate switching, enhanced buffering strategies, and frame-accurate controls for medical-grade video review.

## 3.1 Adaptive Bitrate Switching

**File**: `adaptive-bitrate-manager.js` (New)

### Intelligent Bitrate Management

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
}
```

### Bandwidth Analysis

```javascript
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
```

## 3.2 Enhanced Buffering Strategy

**File**: `medical-buffering-config.js` (New)

### Medical-Grade Buffering Configuration

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

## 3.3 Frame-Accurate Controls

**File**: `frame-accurate-controls.js` (New)

### Precision Video Control Implementation

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
}
```

### Frame Stepping Logic

```javascript
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
```

### Keyboard Shortcuts

```javascript
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
```

### Speed Control Component

```javascript
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
}
```

## Key Features

### Adaptive Bitrate
- Weighted bandwidth averaging
- Buffer health monitoring
- Safety margin calculations
- Automatic quality switching

### Medical Buffering
- Extended back buffer for scrubbing
- Ultra-precise seeking
- Aggressive prefetching
- Enhanced error recovery

### Frame Controls
- Single frame stepping
- Multi-frame jumps (Shift+Arrow)
- Medical-specific speed presets
- Keyboard shortcuts
- Visual and haptic feedback
- Segoe UI Icons where applicable


## Navigation

- [← Phase 2: Timeline Consolidation](02-phase2-timeline-consolidation.md)
- [Phase 4: Error Recovery →](04-phase4-error-recovery.md)