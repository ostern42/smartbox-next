/**
 * Frame-Accurate Controls for SmartBox-Next
 * Phase 3: Playback Enhancements - Precision Video Control Implementation
 * 
 * Medical-grade frame-accurate video controls with precision seeking,
 * haptic feedback, and specialized speed presets for medical workflows
 */

class FrameAccurateControls {
    constructor(player, options = {}) {
        this.player = player;
        this.video = player.video;
        this.options = {
            frameRate: 30,                    // Default frame rate (will be auto-detected)
            showFrameNumber: true,            // Display current frame number
            enableKeyboardShortcuts: true,    // Enable keyboard navigation
            hapticFeedback: true,             // Vibration feedback on mobile
            visualFeedback: true,             // Visual feedback for frame steps
            medicalMode: false,               // Enhanced precision for medical use
            frameAccurateSeking: true,        // Frame-accurate seeking capability
            speedPresets: {
                surgical: [0.1, 0.25, 0.5, 1],
                review: [0.5, 1, 2, 4],
                scan: [1, 2, 4, 8, 16],
                diagnostic: [0.25, 0.5, 1, 2]
            },
            defaultPreset: 'review',
            customSpeeds: [],                 // Custom speed values
            showSpeedControl: true,           // Show speed control interface
            showFrameStepButtons: true,       // Show frame step buttons
            enableTouchGestures: true,        // Touch gesture support
            autoDetectFrameRate: true,        // Auto-detect video frame rate
            ...options
        };
        
        // State management
        this.currentFrame = 0;
        this.totalFrames = 0;
        this.frameRate = this.options.frameRate;
        this.frameDuration = 1 / this.frameRate;
        this.isInitialized = false;
        this.lastSeekTime = 0;
        
        // UI elements
        this.controlsContainer = null;
        this.frameDisplay = null;
        this.speedControl = null;
        this.prevFrameBtn = null;
        this.nextFrameBtn = null;
        
        // Touch gesture management
        this.touchStartX = 0;
        this.touchStartTime = 0;
        this.gestureThreshold = 50; // pixels
        
        // Performance tracking
        this.seekPerformance = {
            totalSeeks: 0,
            averageSeekTime: 0,
            lastSeekDuration: 0
        };
        
        // Initialize the component
        this.initialize();
    }
    
    /**
     * Initialize frame-accurate controls
     */
    initialize() {
        console.log('🎯 Initializing FrameAccurateControls');
        console.log(`📊 Medical mode: ${this.options.medicalMode ? 'Enabled' : 'Disabled'}`);
        console.log(`⚙️ Frame rate: ${this.frameRate} fps`);
        
        this.setupControls();
        this.attachEventListeners();
        this.setupKeyboardShortcuts();
        
        if (this.options.enableTouchGestures) {
            this.setupTouchGestures();
        }
        
        // Auto-detect frame rate if enabled
        if (this.options.autoDetectFrameRate) {
            this.detectFrameRate();
        }
        
        this.isInitialized = true;
        this.updateDisplay();
        
        // Emit initialization event
        this.player.emit('frameControlsInitialized', {
            frameRate: this.frameRate,
            medicalMode: this.options.medicalMode,
            features: this.getEnabledFeatures()
        });
    }
    
    /**
     * Setup control interface
     */
    setupControls() {
        // Create main controls container
        this.controlsContainer = document.createElement('div');
        this.controlsContainer.className = 'frame-accurate-controls';
        this.controlsContainer.setAttribute('role', 'group');
        this.controlsContainer.setAttribute('aria-label', 'Frame-accurate video controls');
        
        // Add medical mode class if enabled
        if (this.options.medicalMode) {
            this.controlsContainer.classList.add('medical-mode');
        }
        
        // Create frame navigation section
        if (this.options.showFrameStepButtons) {
            this.createFrameNavigationControls();
        }
        
        // Create frame display
        if (this.options.showFrameNumber) {
            this.createFrameDisplay();
        }
        
        // Create speed control
        if (this.options.showSpeedControl) {
            this.createSpeedControl();
        }
        
        // Add to player controls
        if (this.player.controlsContainer) {
            this.player.controlsContainer.appendChild(this.controlsContainer);
        } else {
            // Fallback: add after video element
            this.video.parentNode.insertBefore(this.controlsContainer, this.video.nextSibling);
        }
    }
    
    /**
     * Create frame navigation controls
     */
    createFrameNavigationControls() {
        const navSection = document.createElement('div');
        navSection.className = 'frame-navigation';
        
        // Previous frame button
        this.prevFrameBtn = this.createButton(
            '⏮', 
            'Previous Frame (←)', 
            () => this.stepFrames(-1),
            'prev-frame-btn'
        );
        
        // Next frame button  
        this.nextFrameBtn = this.createButton(
            '⏭', 
            'Next Frame (→)', 
            () => this.stepFrames(1),
            'next-frame-btn'
        );
        
        // Multi-frame step buttons for medical mode
        if (this.options.medicalMode) {
            const prevTenBtn = this.createButton(
                '⏪', 
                'Previous 10 Frames (Shift+←)', 
                () => this.stepFrames(-10),
                'prev-ten-frame-btn'
            );
            
            const nextTenBtn = this.createButton(
                '⏩', 
                'Next 10 Frames (Shift+→)', 
                () => this.stepFrames(10),
                'next-ten-frame-btn'
            );
            
            navSection.appendChild(prevTenBtn);
        }
        
        navSection.appendChild(this.prevFrameBtn);
        navSection.appendChild(this.nextFrameBtn);
        
        if (this.options.medicalMode) {
            const nextTenBtn = this.createButton(
                '⏩', 
                'Next 10 Frames (Shift+→)', 
                () => this.stepFrames(10),
                'next-ten-frame-btn'
            );
            navSection.appendChild(nextTenBtn);
        }
        
        this.controlsContainer.appendChild(navSection);
    }
    
    /**
     * Create frame number display
     */
    createFrameDisplay() {
        const displaySection = document.createElement('div');
        displaySection.className = 'frame-display-section';
        
        this.frameDisplay = document.createElement('div');
        this.frameDisplay.className = 'frame-display';
        this.frameDisplay.setAttribute('aria-live', 'polite');
        this.frameDisplay.setAttribute('aria-label', 'Current frame information');
        
        // Time display
        const timeDisplay = document.createElement('div');
        timeDisplay.className = 'time-display';
        this.timeDisplay = timeDisplay;
        
        // Frame number display
        const frameNumber = document.createElement('div');
        frameNumber.className = 'frame-number';
        this.frameNumber = frameNumber;
        
        // Medical mode enhancements
        if (this.options.medicalMode) {
            const precisionDisplay = document.createElement('div');
            precisionDisplay.className = 'precision-display';
            this.precisionDisplay = precisionDisplay;
            displaySection.appendChild(precisionDisplay);
        }
        
        displaySection.appendChild(timeDisplay);
        displaySection.appendChild(frameNumber);
        displaySection.appendChild(this.frameDisplay);
        
        this.controlsContainer.appendChild(displaySection);
    }
    
    /**
     * Create speed control interface
     */
    createSpeedControl() {
        this.speedControl = new SpeedControl(this.player, {
            presets: this.options.speedPresets,
            defaultPreset: this.options.defaultPreset,
            customSpeeds: this.options.customSpeeds,
            medicalMode: this.options.medicalMode,
            frameAccurateControls: this
        });
        
        this.controlsContainer.appendChild(this.speedControl.element);
    }
    
    /**
     * Create a control button
     */
    createButton(icon, title, clickHandler, className = '') {
        const button = document.createElement('button');
        button.className = `frame-control-btn ${className}`;
        button.innerHTML = icon;
        button.title = title;
        button.setAttribute('aria-label', title);
        button.addEventListener('click', clickHandler);
        
        // Add visual feedback
        button.addEventListener('mousedown', () => {
            button.classList.add('active');
        });
        
        button.addEventListener('mouseup', () => {
            button.classList.remove('active');
        });
        
        return button;
    }
    
    /**
     * Step frames forward or backward
     */
    stepFrames(frames) {
        const startTime = performance.now();
        const currentTime = this.video.currentTime;
        const frameDuration = 1 / this.frameRate;
        const newTime = currentTime + (frames * frameDuration);
        
        // Ensure we don't go beyond bounds
        const clampedTime = Math.max(0, Math.min(newTime, this.video.duration || 0));
        
        // Perform the seek
        this.seekToTime(clampedTime);
        
        // Update performance metrics
        const seekDuration = performance.now() - startTime;
        this.updateSeekPerformance(seekDuration);
        
        // Visual feedback
        if (this.options.visualFeedback) {
            this.showFrameStepFeedback(frames);
        }
        
        // Haptic feedback (if supported)
        if (this.options.hapticFeedback && 'vibrate' in navigator) {
            const vibrationPattern = Math.abs(frames) > 1 ? [10, 50, 10] : [10];
            navigator.vibrate(vibrationPattern);
        }
        
        // Log for medical mode
        if (this.options.medicalMode) {
            console.log(`🎯 Frame step: ${frames} frames, Time: ${clampedTime.toFixed(4)}s, Seek duration: ${seekDuration.toFixed(2)}ms`);
        }
        
        // Emit frame step event
        this.player.emit('frameStep', {
            frames: frames,
            currentFrame: this.currentFrame,
            currentTime: clampedTime,
            seekDuration: seekDuration
        });
    }
    
    /**
     * Seek to specific time with high precision
     */
    seekToTime(time) {
        const seekStart = performance.now();
        
        // Set time with high precision
        this.video.currentTime = time;
        this.lastSeekTime = time;
        
        // For HLS streams, ensure frame accuracy
        if (this.player.hls && this.options.frameAccurateSeking) {
            // Force fragment loading at exact position
            this.player.hls.startLoad(time);
        }
        
        // Update displays
        this.updateDisplay();
        
        // Emit seek event
        this.player.emit('frameSeeked', {
            frame: this.currentFrame,
            time: time,
            seekDuration: performance.now() - seekStart
        });
    }
    
    /**
     * Update all displays
     */
    updateDisplay() {
        const currentTime = this.video.currentTime;
        const duration = this.video.duration || 0;
        
        // Calculate current frame
        this.currentFrame = Math.round(currentTime * this.frameRate);
        this.totalFrames = Math.round(duration * this.frameRate);
        
        // Update frame number display
        if (this.frameNumber) {
            this.frameNumber.textContent = `Frame: ${this.currentFrame} / ${this.totalFrames}`;
        }
        
        // Update time display
        if (this.timeDisplay) {
            this.timeDisplay.textContent = `Time: ${this.formatTime(currentTime)} / ${this.formatTime(duration)}`;
        }
        
        // Update main frame display
        if (this.frameDisplay) {
            this.frameDisplay.innerHTML = `
                <div class="frame-info">
                    <span class="current-frame">${this.currentFrame}</span>
                    <span class="frame-separator">/</span>
                    <span class="total-frames">${this.totalFrames}</span>
                </div>
                <div class="time-info">
                    <span class="current-time">${this.formatTime(currentTime, true)}</span>
                </div>
            `;
        }
        
        // Update precision display for medical mode
        if (this.precisionDisplay && this.options.medicalMode) {
            const frameAccuracy = Math.abs(currentTime - (this.currentFrame / this.frameRate));
            this.precisionDisplay.innerHTML = `
                <div class="precision-info">
                    <span>Accuracy: ±${(frameAccuracy * 1000).toFixed(1)}ms</span>
                    <span>Rate: ${this.frameRate}fps</span>
                </div>
            `;
        }
    }
    
    /**
     * Format time for display
     */
    formatTime(seconds, showMilliseconds = false) {
        if (!seconds || seconds < 0) return showMilliseconds ? '00:00.000' : '00:00';
        
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = Math.floor(seconds % 60);
        const milliseconds = Math.floor((seconds % 1) * 1000);
        
        let timeString = '';
        
        if (hours > 0) {
            timeString += `${hours.toString().padStart(2, '0')}:`;
        }
        
        timeString += `${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        
        if (showMilliseconds) {
            timeString += `.${milliseconds.toString().padStart(3, '0')}`;
        }
        
        return timeString;
    }
    
    /**
     * Show visual feedback for frame stepping
     */
    showFrameStepFeedback(frames) {
        // Create feedback element
        const feedback = document.createElement('div');
        feedback.className = 'frame-step-feedback';
        feedback.textContent = frames > 0 ? `+${frames}` : `${frames}`;
        
        if (Math.abs(frames) > 1) {
            feedback.classList.add('multi-frame');
        }
        
        // Position relative to video
        feedback.style.position = 'absolute';
        feedback.style.top = '50%';
        feedback.style.left = '50%';
        feedback.style.transform = 'translate(-50%, -50%)';
        feedback.style.zIndex = '1000';
        
        this.video.parentNode.appendChild(feedback);
        
        // Animate and remove
        setTimeout(() => {
            feedback.style.opacity = '0';
            setTimeout(() => {
                if (feedback.parentNode) {
                    feedback.parentNode.removeChild(feedback);
                }
            }, 300);
        }, 800);
    }
    
    /**
     * Setup keyboard shortcuts
     */
    setupKeyboardShortcuts() {
        if (!this.options.enableKeyboardShortcuts) return;
        
        document.addEventListener('keydown', (e) => {
            // Only handle if video player is focused or no input is focused
            if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;
            
            switch(e.key) {
                case 'ArrowLeft':
                    if (e.shiftKey) {
                        this.stepFrames(-10); // 10 frames back
                    } else if (e.ctrlKey) {
                        this.stepFrames(-60); // 60 frames back (2 seconds at 30fps)
                    } else {
                        this.stepFrames(-1);  // 1 frame back
                    }
                    e.preventDefault();
                    break;
                    
                case 'ArrowRight':
                    if (e.shiftKey) {
                        this.stepFrames(10);  // 10 frames forward
                    } else if (e.ctrlKey) {
                        this.stepFrames(60);  // 60 frames forward (2 seconds at 30fps)
                    } else {
                        this.stepFrames(1);   // 1 frame forward
                    }
                    e.preventDefault();
                    break;
                    
                case ',':
                case '<':
                    this.stepFrames(-1);
                    e.preventDefault();
                    break;
                    
                case '.':
                case '>':
                    this.stepFrames(1);
                    e.preventDefault();
                    break;
                    
                case 'j':
                case 'J':
                    this.stepFrames(-10);
                    e.preventDefault();
                    break;
                    
                case 'l':
                case 'L':
                    this.stepFrames(10);
                    e.preventDefault();
                    break;
                    
                // Speed control shortcuts
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                    if (this.speedControl) {
                        const speedIndex = parseInt(e.key) - 1;
                        this.speedControl.setSpeedByIndex(speedIndex);
                    }
                    e.preventDefault();
                    break;
            }
        });
    }
    
    /**
     * Setup touch gestures for mobile devices
     */
    setupTouchGestures() {
        const gestureElement = this.video.parentNode;
        
        gestureElement.addEventListener('touchstart', (e) => {
            if (e.touches.length === 1) {
                this.touchStartX = e.touches[0].clientX;
                this.touchStartTime = Date.now();
            }
        }, { passive: true });
        
        gestureElement.addEventListener('touchend', (e) => {
            if (e.changedTouches.length === 1) {
                const touchEndX = e.changedTouches[0].clientX;
                const touchDuration = Date.now() - this.touchStartTime;
                const deltaX = touchEndX - this.touchStartX;
                
                // Only handle quick swipes
                if (touchDuration < 300 && Math.abs(deltaX) > this.gestureThreshold) {
                    const frames = deltaX > 0 ? 1 : -1;
                    this.stepFrames(frames);
                    e.preventDefault();
                }
            }
        }, { passive: false });
    }
    
    /**
     * Auto-detect video frame rate
     */
    async detectFrameRate() {
        // Try to get frame rate from video metadata
        if (this.video.videoTracks && this.video.videoTracks.length > 0) {
            const track = this.video.videoTracks[0];
            if (track.getSettings) {
                const settings = track.getSettings();
                if (settings.frameRate) {
                    this.setFrameRate(settings.frameRate);
                    return;
                }
            }
        }
        
        // Fallback: try to detect from HLS metadata
        if (this.player.hls && this.player.hls.levels) {
            const level = this.player.hls.levels[this.player.hls.currentLevel];
            if (level && level.frameRate) {
                this.setFrameRate(level.frameRate);
                return;
            }
        }
        
        // Default frame rate detection via requestVideoFrameCallback (if available)
        if (this.video.requestVideoFrameCallback) {
            this.detectFrameRateViaCallback();
        }
    }
    
    /**
     * Detect frame rate using requestVideoFrameCallback
     */
    detectFrameRateViaCallback() {
        let frameCount = 0;
        let startTime = null;
        const sampleFrames = 60; // Sample 60 frames
        
        const frameCallback = (now, metadata) => {
            if (!startTime) {
                startTime = now;
            }
            
            frameCount++;
            
            if (frameCount < sampleFrames) {
                this.video.requestVideoFrameCallback(frameCallback);
            } else {
                const duration = (now - startTime) / 1000; // Convert to seconds
                const detectedFrameRate = frameCount / duration;
                
                // Round to common frame rates
                const commonRates = [23.976, 24, 25, 29.97, 30, 50, 59.94, 60];
                const closest = commonRates.reduce((prev, curr) => 
                    Math.abs(curr - detectedFrameRate) < Math.abs(prev - detectedFrameRate) ? curr : prev
                );
                
                this.setFrameRate(closest);
                console.log(`🎯 Auto-detected frame rate: ${closest} fps`);
            }
        };
        
        if (this.video.currentTime > 0 && !this.video.paused) {
            this.video.requestVideoFrameCallback(frameCallback);
        }
    }
    
    /**
     * Set frame rate and update calculations
     */
    setFrameRate(frameRate) {
        this.frameRate = frameRate;
        this.frameDuration = 1 / frameRate;
        this.options.frameRate = frameRate;
        
        console.log(`🎯 Frame rate updated: ${frameRate} fps`);
        
        // Update displays
        this.updateDisplay();
        
        // Emit frame rate change event
        this.player.emit('frameRateChanged', { frameRate: this.frameRate });
    }
    
    /**
     * Attach event listeners
     */
    attachEventListeners() {
        // Update frame display on time changes
        this.video.addEventListener('timeupdate', () => {
            if (this.isInitialized) {
                this.updateDisplay();
            }
        });
        
        // Update on duration change
        this.video.addEventListener('durationchange', () => {
            if (this.isInitialized) {
                this.updateDisplay();
            }
        });
        
        // Handle metadata loaded
        this.video.addEventListener('loadedmetadata', () => {
            if (this.options.autoDetectFrameRate) {
                this.detectFrameRate();
            }
            this.updateDisplay();
        });
        
        // Player events
        if (this.player.on) {
            this.player.on('seek', () => this.updateDisplay());
            this.player.on('play', () => this.updateDisplay());
            this.player.on('pause', () => this.updateDisplay());
        }
    }
    
    /**
     * Update seek performance metrics
     */
    updateSeekPerformance(duration) {
        this.seekPerformance.totalSeeks++;
        this.seekPerformance.lastSeekDuration = duration;
        
        // Calculate rolling average
        this.seekPerformance.averageSeekTime = 
            (this.seekPerformance.averageSeekTime * (this.seekPerformance.totalSeeks - 1) + duration) / 
            this.seekPerformance.totalSeeks;
    }
    
    /**
     * Get enabled features list
     */
    getEnabledFeatures() {
        return {
            frameNavigation: this.options.showFrameStepButtons,
            frameDisplay: this.options.showFrameNumber,
            speedControl: this.options.showSpeedControl,
            keyboardShortcuts: this.options.enableKeyboardShortcuts,
            touchGestures: this.options.enableTouchGestures,
            hapticFeedback: this.options.hapticFeedback,
            visualFeedback: this.options.visualFeedback,
            medicalMode: this.options.medicalMode,
            autoFrameRateDetection: this.options.autoDetectFrameRate
        };
    }
    
    /**
     * Get current status and metrics
     */
    getStatus() {
        return {
            currentFrame: this.currentFrame,
            totalFrames: this.totalFrames,
            frameRate: this.frameRate,
            currentTime: this.video.currentTime,
            duration: this.video.duration,
            frameDuration: this.frameDuration,
            seekPerformance: { ...this.seekPerformance },
            isInitialized: this.isInitialized,
            enabledFeatures: this.getEnabledFeatures()
        };
    }
    
    /**
     * Cleanup and destroy
     */
    destroy() {
        console.log('🗑️ Destroying FrameAccurateControls');
        
        // Remove event listeners
        if (this.video) {
            this.video.removeEventListener('timeupdate', this.updateDisplay);
            this.video.removeEventListener('durationchange', this.updateDisplay);
            this.video.removeEventListener('loadedmetadata', this.updateDisplay);
        }
        
        // Destroy speed control
        if (this.speedControl && this.speedControl.destroy) {
            this.speedControl.destroy();
        }
        
        // Remove UI elements
        if (this.controlsContainer && this.controlsContainer.parentNode) {
            this.controlsContainer.parentNode.removeChild(this.controlsContainer);
        }
        
        // Clear references
        this.player = null;
        this.video = null;
        this.controlsContainer = null;
        this.speedControl = null;
    }
}

/**
 * Speed Control Component for Frame-Accurate Controls
 */
class SpeedControl {
    constructor(player, options) {
        this.player = player;
        this.video = player.video;
        this.options = {
            presets: {
                surgical: [0.1, 0.25, 0.5, 1],
                review: [0.5, 1, 2, 4],
                scan: [1, 2, 4, 8, 16],
                diagnostic: [0.25, 0.5, 1, 2]
            },
            defaultPreset: 'review',
            customSpeeds: [],
            medicalMode: false,
            frameAccurateControls: null,
            ...options
        };
        
        this.currentPreset = this.options.defaultPreset;
        this.element = this.createSpeedControl();
        this.updateSpeedButtons();
    }
    
    /**
     * Create speed control interface
     */
    createSpeedControl() {
        const container = document.createElement('div');
        container.className = 'speed-control';
        
        if (this.options.medicalMode) {
            container.classList.add('medical-mode');
        }
        
        // Preset selector
        const presetSection = document.createElement('div');
        presetSection.className = 'preset-section';
        
        const presetLabel = document.createElement('label');
        presetLabel.textContent = 'Mode:';
        presetLabel.className = 'preset-label';
        
        const presetSelector = document.createElement('select');
        presetSelector.className = 'speed-preset-selector';
        presetSelector.setAttribute('aria-label', 'Speed preset mode');
        
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
            
            // Emit preset change event
            this.player.emit('speedPresetChanged', { 
                preset: this.currentPreset,
                speeds: this.options.presets[this.currentPreset]
            });
        });
        
        presetSection.appendChild(presetLabel);
        presetSection.appendChild(presetSelector);
        
        // Speed buttons container
        this.speedButtons = document.createElement('div');
        this.speedButtons.className = 'speed-buttons';
        this.speedButtons.setAttribute('role', 'group');
        this.speedButtons.setAttribute('aria-label', 'Playback speed buttons');
        
        container.appendChild(presetSection);
        container.appendChild(this.speedButtons);
        
        return container;
    }
    
    /**
     * Update speed buttons based on current preset
     */
    updateSpeedButtons() {
        this.speedButtons.innerHTML = '';
        
        const speeds = this.options.presets[this.currentPreset] || [];
        
        speeds.forEach((speed, index) => {
            const button = document.createElement('button');
            button.className = 'speed-button';
            button.textContent = `${speed}×`;
            button.setAttribute('aria-label', `Set playback speed to ${speed}x`);
            button.setAttribute('data-speed', speed);
            button.setAttribute('data-index', index);
            
            // Mark current speed as active
            if (Math.abs(this.video.playbackRate - speed) < 0.01) {
                button.classList.add('active');
            }
            
            button.addEventListener('click', () => {
                this.setSpeed(speed);
            });
            
            this.speedButtons.appendChild(button);
        });
        
        // Add custom speeds if any
        this.options.customSpeeds.forEach(speed => {
            if (!speeds.includes(speed)) {
                const button = document.createElement('button');
                button.className = 'speed-button custom';
                button.textContent = `${speed}×`;
                button.setAttribute('aria-label', `Set playback speed to ${speed}x (custom)`);
                button.setAttribute('data-speed', speed);
                
                if (Math.abs(this.video.playbackRate - speed) < 0.01) {
                    button.classList.add('active');
                }
                
                button.addEventListener('click', () => {
                    this.setSpeed(speed);
                });
                
                this.speedButtons.appendChild(button);
            }
        });
    }
    
    /**
     * Set playback speed
     */
    setSpeed(speed) {
        const previousSpeed = this.video.playbackRate;
        this.video.playbackRate = speed;
        
        this.updateActiveButton();
        
        // Log for medical mode
        if (this.options.medicalMode) {
            console.log(`🎯 Speed changed: ${previousSpeed}× → ${speed}×`);
        }
        
        // Emit speed change event
        this.player.emit('speedChanged', { 
            speed: speed, 
            previousSpeed: previousSpeed,
            preset: this.currentPreset 
        });
        
        // Update frame accurate controls if available
        if (this.options.frameAccurateControls) {
            this.options.frameAccurateControls.updateDisplay();
        }
    }
    
    /**
     * Set speed by button index
     */
    setSpeedByIndex(index) {
        const speeds = this.options.presets[this.currentPreset] || [];
        if (index >= 0 && index < speeds.length) {
            this.setSpeed(speeds[index]);
        }
    }
    
    /**
     * Update active button state
     */
    updateActiveButton() {
        const buttons = this.speedButtons.querySelectorAll('.speed-button');
        const currentSpeed = this.video.playbackRate;
        
        buttons.forEach(button => {
            const buttonSpeed = parseFloat(button.getAttribute('data-speed'));
            if (Math.abs(buttonSpeed - currentSpeed) < 0.01) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });
    }
    
    /**
     * Add custom speed
     */
    addCustomSpeed(speed) {
        if (!this.options.customSpeeds.includes(speed)) {
            this.options.customSpeeds.push(speed);
            this.options.customSpeeds.sort((a, b) => a - b);
            this.updateSpeedButtons();
        }
    }
    
    /**
     * Get current status
     */
    getStatus() {
        return {
            currentSpeed: this.video.playbackRate,
            currentPreset: this.currentPreset,
            availableSpeeds: this.options.presets[this.currentPreset],
            customSpeeds: this.options.customSpeeds
        };
    }
    
    /**
     * Destroy speed control
     */
    destroy() {
        if (this.element && this.element.parentNode) {
            this.element.parentNode.removeChild(this.element);
        }
        
        this.player = null;
        this.video = null;
        this.element = null;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { FrameAccurateControls, SpeedControl };
} else if (typeof window !== 'undefined') {
    window.FrameAccurateControls = FrameAccurateControls;
    window.SpeedControl = SpeedControl;
}