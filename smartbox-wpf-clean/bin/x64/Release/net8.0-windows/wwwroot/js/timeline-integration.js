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
        // Initialize timeline component
        this.timeline = new VideoTimelineComponent('videoTimeline', {
            height: 200,
            timeScales: [5, 10, 20, 60], // minutes
            prerecordingDurations: [60, 30, 10] // seconds
        });
        
        this.setupTimelineEventListeners();
        this.setupPrerecordingButtons();
        this.setupIntegrationEventListeners();
        
        console.log('TimelineIntegrationManager: Initialized');
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
        this.timeline.prerecordingBuffer = duration;
        this.timeline.updatePrerecordingBuffer();
        
        console.log(`TimelineIntegrationManager: Prerecording mode set to ${duration}s`);
    }
    
    enableTimeline() {
        if (this.isTimelineEnabled) return;
        
        this.isTimelineEnabled = true;
        const timelineContainer = document.getElementById('videoTimeline');
        if (timelineContainer) {
            timelineContainer.style.display = 'flex';
        }
        
        // Start thumbnail capture interval when timeline is enabled
        this.startThumbnailCapture();
        
        console.log('TimelineIntegrationManager: Timeline enabled');
    }
    
    disableTimeline() {
        if (!this.isTimelineEnabled) return;
        
        this.isTimelineEnabled = false;
        const timelineContainer = document.getElementById('videoTimeline');
        if (timelineContainer) {
            timelineContainer.style.display = 'none';
        }
        
        // Stop thumbnail capture
        this.stopThumbnailCapture();
        
        // Clear timeline
        this.timeline.clear();
        this.criticalMoments = [];
        
        console.log('TimelineIntegrationManager: Timeline disabled');
    }
    
    startThumbnailCapture() {
        // Capture thumbnails every 5 seconds during video preview
        this.thumbnailCaptureInterval = setInterval(() => {
            if (this.isTimelineEnabled) {
                this.captureThumbnailFrame();
            }
        }, 5000);
    }
    
    stopThumbnailCapture() {
        if (this.thumbnailCaptureInterval) {
            clearInterval(this.thumbnailCaptureInterval);
            this.thumbnailCaptureInterval = null;
        }
    }
    
    captureThumbnailFrame() {
        try {
            const video = document.getElementById('webcamPreviewLarge');
            if (!video || video.videoWidth === 0) return;
            
            const canvas = document.createElement('canvas');
            canvas.width = 120;
            canvas.height = 68;
            
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            
            const thumbnailData = canvas.toDataURL('image/jpeg', 0.7);
            const currentTime = this.timeline.currentTime;
            
            this.timeline.addThumbnail(thumbnailData, currentTime, 'frame');
            
        } catch (error) {
            console.warn('TimelineIntegrationManager: Thumbnail capture failed:', error);
        }
    }
    
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
        
        // Capture initial thumbnail
        setTimeout(() => {
            this.captureThumbnailFrame();
        }, 1000);
        
        // Start capturing thumbnails more frequently during recording
        this.stopThumbnailCapture();
        this.thumbnailCaptureInterval = setInterval(() => {
            this.captureThumbnailFrame();
        }, 2000); // Every 2 seconds during recording
        
        console.log('TimelineIntegrationManager: Recording started on timeline');
    }
    
    onRecordingStopped() {
        if (!this.isTimelineEnabled) return;
        
        this.timeline.stopRecording();
        this.stopRecordingTimer();
        
        // Return to normal thumbnail capture rate
        this.stopThumbnailCapture();
        this.startThumbnailCapture();
        
        // Capture final thumbnail
        setTimeout(() => {
            this.captureThumbnailFrame();
        }, 500);
        
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
            const mins = Math.floor(seconds / 60);
            const secs = Math.floor(seconds % 60);
            return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        };
        
        // Could update other UI elements here
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