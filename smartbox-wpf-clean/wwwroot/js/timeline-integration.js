/**
 * Timeline Integration Manager for SmartBox-Next
 * Bridges the VideoTimelineComponent with the existing recording system
 */
class TimelineIntegrationManager {
    constructor(app) {
        this.app = app;
        this.timeline = null;
        this.isTimelineEnabled = false;
        this.thumbnailCaptureInterval = null;
        this.criticalMoments = [];
        this.prerecordingMode = 60; // Default prerecording duration
        this.storageUsage = 0;
        
        this.init();
    }
    
    init() {
        // Get timeline container
        const timelineContainer = document.querySelector('#videoTimeline .timeline-container');
        if (!timelineContainer) {
            console.error('TimelineIntegrationManager: Timeline container not found');
            return;
        }
        
        // Create playhead controls container
        const controlsContainer = document.createElement('div');
        controlsContainer.className = 'playhead-controls-container';
        timelineContainer.parentElement.insertBefore(controlsContainer, timelineContainer);
        
        // Initialize playhead controls
        this.playheadControls = new PlayheadControls(controlsContainer, {
            onSeek: (time) => this.adaptiveTimeline?.seek(time),
            onPlay: () => this.app.onStartVideoRecording?.(),
            onPause: () => this.app.onStopVideoRecording?.(),
            onPlaybackRateChange: (rate) => console.log('Playback rate:', rate)
        });
        
        // Initialize adaptive timeline
        this.adaptiveTimeline = new AdaptiveTimeline(timelineContainer, {
            height: 100,
            thumbnailWidth: 160,
            fps: 25,
            enableWaveform: true,
            enableThumbnails: true,
            enableMotionTracking: true
        });
        
        // Connect timeline to playhead controls
        this.adaptiveTimeline.onSeek = (time) => {
            this.playheadControls.updateTime(time, this.adaptiveTimeline.state.duration);
        };
        
        // Initialize jogwheel
        this.initializeJogwheel();
        
        // Keep reference to old timeline for compatibility
        this.timeline = {
            currentTime: 0,
            duration: 0,
            segments: [],
            thumbnails: [],
            on: (event, callback) => {
                // Adapter for old events
                if (event === 'seek') {
                    this.adaptiveTimeline.onSeek = (time) => callback({ detail: { time } });
                }
            },
            setScale: (scale) => {
                // Adapter for scale changes
                this.adaptiveTimeline.recalculateTimeScale();
            },
            addCriticalMarker: (time) => {
                // Add marker to adaptive timeline
                const markerId = 'marker_' + Date.now();
                this.adaptiveTimeline.state.markers.push({ id: markerId, time, type: 'critical' });
                this.adaptiveTimeline.render();
                return markerId;
            },
            addThumbnail: (thumbnailData, time, type) => {
                // Store thumbnail data for compatibility
                this.timeline.thumbnails.push({ data: thumbnailData, time, type });
                console.log('Thumbnail added at time:', time, 'type:', type);
            },
            startRecording: () => {
                this.recordingStartTime = Date.now();
                console.log('Timeline: Recording started');
            },
            stopRecording: () => {
                const duration = (Date.now() - this.recordingStartTime) / 1000;
                this.timeline.duration = duration;
                console.log('Timeline: Recording stopped, duration:', duration);
            },
            updateTime: (time) => {
                this.timeline.currentTime = time;
                if (this.adaptiveTimeline) {
                    this.adaptiveTimeline.updateTime(time);
                }
                if (this.playheadControls) {
                    this.playheadControls.updateTime(time, this.timeline.duration);
                }
            },
            updateStorageUsage: (usage) => {
                console.log('Storage usage:', usage, 'MB');
            },
            updatePrerecordingBuffer: () => {
                console.log('Prerecording buffer updated');
            },
            clear: () => {
                this.timeline.thumbnails = [];
                this.timeline.segments = [];
                this.timeline.currentTime = 0;
                this.timeline.duration = 0;
                if (this.adaptiveTimeline) {
                    this.adaptiveTimeline.state.markers = [];
                    this.adaptiveTimeline.thumbnailCache.clear();
                    this.adaptiveTimeline.render();
                }
            }
        };
        
        this.setupTimelineEventListeners();
        this.setupPrerecordingButtons();
        this.setupIntegrationEventListeners();
        
        console.log('TimelineIntegrationManager: Initialized with Adaptive Timeline');
    }
    
    initializeJogwheel() {
        // Create jogwheel container inside timeline
        const timelineEl = document.getElementById('videoTimeline');
        if (!timelineEl) return;
        
        const jogwheelContainer = document.createElement('div');
        jogwheelContainer.className = 'jogwheel-container';
        timelineEl.appendChild(jogwheelContainer);
        
        // Initialize jogwheel
        this.jogwheel = new JogwheelControl(jogwheelContainer, {
            size: 160,
            sensitivity: 0.5,
            hapticFeedback: true
        });
        
        // Always show the jogwheel
        this.jogwheel.show();
        
        // Jogwheel events
        this.jogwheel.on('scrub', (amount) => {
            if (this.adaptiveTimeline) {
                const currentTime = this.adaptiveTimeline.state.currentTime;
                const newTime = currentTime + amount;
                this.adaptiveTimeline.seek(newTime);
                
                // Update playhead controls
                if (this.playheadControls) {
                    this.playheadControls.updateTime(newTime, this.adaptiveTimeline.state.duration);
                }
            }
        });
        
        // Touch activation for enhanced interaction
        timelineEl.addEventListener('touchstart', (e) => {
            if (e.touches.length === 2) {
                // Two-finger touch could activate special mode
                e.preventDefault();
            }
        });
    }
    
    setupTimelineEventListeners() {
        // Timeline events
        this.timeline.on('seek', (e) => this.onTimelineSeek(e.detail));
        this.timeline.on('scaleChanged', (e) => this.onScaleChanged(e.detail));
        this.timeline.on('criticalMarkerAdded', (e) => this.onCriticalMarkerFromTimeline(e.detail));
        this.timeline.on('cleared', () => this.onTimelineCleared());
        
        console.log('TimelineIntegrationManager: Timeline event listeners set up');
    }
    
    setupPrerecordingButtons() {
        const overlay = document.getElementById('prerecordingOverlay');
        if (!overlay) return;
        
        const buttons = overlay.querySelectorAll('.prerecording-btn');
        buttons.forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                
                const duration = parseInt(btn.dataset.duration);
                this.setPrerecordingMode(duration);
                
                // Update UI
                buttons.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
            });
        });
        
        // Set default active button
        const defaultBtn = overlay.querySelector('[data-duration="60"]');
        if (defaultBtn) {
            defaultBtn.classList.add('active');
        }
        
        console.log('TimelineIntegrationManager: Prerecording buttons set up');
    }
    
    setupIntegrationEventListeners() {
        // Listen to app recording events
        document.addEventListener('startVideoRecording', () => this.onRecordingStarted());
        document.addEventListener('stopVideoRecording', () => this.onRecordingStopped());
        document.addEventListener('criticalMomentMarked', (e) => this.onCriticalMomentMarked(e));
        
        // Mode changes
        document.addEventListener('modeChanged', (e) => this.onModeChanged(e.detail));
        
        console.log('TimelineIntegrationManager: Integration event listeners set up');
    }
    
    setPrerecordingMode(duration) {
        this.prerecordingMode = duration;
        if (this.timeline) {
            this.timeline.prerecordingBuffer = duration;
            this.timeline.updatePrerecordingBuffer();
        }
        
        console.log(`TimelineIntegrationManager: Prerecording mode set to ${duration}s`);
    }
    
    enableTimeline() {
        if (this.isTimelineEnabled) return;
        
        this.isTimelineEnabled = true;
        const timelineContainer = document.getElementById('videoTimeline');
        if (timelineContainer) {
            timelineContainer.style.display = 'block';
            
            // Initialize adaptive timeline with video if available
            const video = document.getElementById('webcamPreviewLarge');
            if (video && this.adaptiveTimeline) {
                // For live recording, set duration to 0 to trigger 30-second initial view
                this.adaptiveTimeline.setVideo(video, 0);
                
                // Start updating the timeline
                this.updateInterval = setInterval(() => {
                    if (video.currentTime > 0) {
                        this.adaptiveTimeline.updateTime(video.currentTime);
                    }
                }, 100);
            }
        }
        
        console.log('TimelineIntegrationManager: Timeline enabled with adaptive thumbnails');
    }
    
    disableTimeline() {
        if (!this.isTimelineEnabled) return;
        
        this.isTimelineEnabled = false;
        const timelineContainer = document.getElementById('videoTimeline');
        if (timelineContainer) {
            timelineContainer.style.display = 'none';
        }
        
        // Stop update interval
        if (this.updateInterval) {
            clearInterval(this.updateInterval);
            this.updateInterval = null;
        }
        
        // Clear timeline
        this.timeline.clear();
        this.criticalMoments = [];
        
        // Hide jogwheel
        if (this.jogwheel) {
            this.jogwheel.hide();
        }
        
        console.log('TimelineIntegrationManager: Timeline disabled');
    }
    
    // Thumbnail capture is now handled by AdaptiveTimeline
    
    // Event Handlers
    
    onModeChanged(detail) {
        if (detail.currentMode === 'recording') {
            this.enableTimeline();
        } else {
            this.disableTimeline();
        }
    }
    
    onRecordingStarted() {
        if (!this.isTimelineEnabled) return;
        
        this.timeline.startRecording();
        this.startRecordingTimer();
        
        // Set adaptive timeline to live recording mode
        if (this.adaptiveTimeline) {
            this.adaptiveTimeline.setLiveRecording(true);
        }
        
        // No need for manual thumbnail capture - adaptive timeline handles it
        console.log('TimelineIntegrationManager: Recording started on timeline with live thumbnails');
    }
    
    onRecordingStopped() {
        if (!this.isTimelineEnabled) return;
        
        this.timeline.stopRecording();
        this.stopRecordingTimer();
        
        // Exit live recording mode
        if (this.adaptiveTimeline) {
            this.adaptiveTimeline.setLiveRecording(false);
        }
        
        console.log('TimelineIntegrationManager: Recording stopped on timeline');
    }
    
    onCriticalMomentMarked(event) {
        if (!this.isTimelineEnabled || !this.app.isRecording) return;
        
        const currentTime = this.timeline.currentTime;
        const markerId = this.timeline.addCriticalMarker(currentTime);
        
        // Store metadata
        this.criticalMoments.push({
            id: markerId,
            time: currentTime,
            timestamp: new Date(),
            description: 'Kritischer Moment'
        });
        
        // Capture special thumbnail for critical moment
        setTimeout(() => {
            this.captureCriticalThumbnail(currentTime);
        }, 100);
        
        console.log('TimelineIntegrationManager: Critical moment marked at', currentTime);
    }
    
    captureCriticalThumbnail(time) {
        try {
            const video = document.getElementById('webcamPreviewLarge');
            if (!video || video.videoWidth === 0) return;
            
            const canvas = document.createElement('canvas');
            canvas.width = 120;
            canvas.height = 68;
            
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            
            // Add red border for critical moments
            ctx.strokeStyle = '#d83b01';
            ctx.lineWidth = 3;
            ctx.strokeRect(0, 0, canvas.width, canvas.height);
            
            const thumbnailData = canvas.toDataURL('image/jpeg', 0.8);
            this.timeline.addThumbnail(thumbnailData, time, 'key');
            
        } catch (error) {
            console.warn('TimelineIntegrationManager: Critical thumbnail capture failed:', error);
        }
    }
    
    onCriticalMarkerFromTimeline(detail) {
        // Handle critical marker added directly from timeline
        console.log('TimelineIntegrationManager: Critical marker from timeline at', detail.time);
    }
    
    onTimelineSeek(detail) {
        // Handle seeking in timeline
        console.log('TimelineIntegrationManager: Seek to', detail.time);
        
        // Could implement video scrubbing here if we had a recorded video
        // For now, just update the display
        this.updateTimeDisplay(detail.time);
    }
    
    onScaleChanged(detail) {
        console.log('TimelineIntegrationManager: Scale changed to', detail.scale, 'minutes');
    }
    
    onTimelineCleared() {
        this.criticalMoments = [];
        this.storageUsage = 0;
        this.timeline.updateStorageUsage(0);
        console.log('TimelineIntegrationManager: Timeline cleared');
    }
    
    // Recording Timer
    
    startRecordingTimer() {
        this.recordingStartTime = Date.now();
        this.recordingTimer = setInterval(() => {
            const elapsed = (Date.now() - this.recordingStartTime) / 1000;
            this.timeline.updateTime(elapsed);
            this.updateStorageEstimate(elapsed);
        }, 100); // Update every 100ms for smooth animation
    }
    
    stopRecordingTimer() {
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }
    
    updateTimeDisplay(time) {
        // Update any additional time displays
        const formatTime = (seconds) => {
            if (!isFinite(seconds) || seconds < 0) {
                seconds = 0;
            }
            const mins = Math.floor(seconds / 60);
            const secs = Math.floor(seconds % 60);
            return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        };
        
        // Update playhead controls if available
        if (this.playheadControls) {
            const duration = this.app.isRecording ? time : this.timeline.duration;
            this.playheadControls.updateTime(time, duration);
        }
    }
    
    updateStorageEstimate(recordingTime) {
        // Estimate storage usage based on recording time
        // Rough estimate: 1MB per 10 seconds of video
        const estimatedMB = (recordingTime / 10) * 1;
        this.storageUsage = Math.max(this.storageUsage, estimatedMB);
        this.timeline.updateStorageUsage(this.storageUsage);
    }
    
    // Public API
    
    addPhotoThumbnail(imageData) {
        if (!this.isTimelineEnabled) return;
        
        const currentTime = this.timeline.currentTime;
        this.timeline.addThumbnail(imageData, currentTime, 'photo');
        
        console.log('TimelineIntegrationManager: Photo thumbnail added');
    }
    
    getCriticalMoments() {
        return this.criticalMoments;
    }
    
    getTimelineData() {
        return {
            segments: this.timeline.segments,
            criticalMoments: this.criticalMoments,
            thumbnails: this.timeline.thumbnails,
            currentTime: this.timeline.currentTime,
            duration: this.timeline.duration,
            storageUsage: this.storageUsage
        };
    }
    
    exportTimelineData() {
        const data = this.getTimelineData();
        return JSON.stringify(data, null, 2);
    }
}

// Export for use in app.js
window.TimelineIntegrationManager = TimelineIntegrationManager;