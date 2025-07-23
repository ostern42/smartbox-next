/**
 * Professional Video Timeline Component for SmartBox-Next
 * 
 * DEPRECATED: This component has been superseded by UnifiedTimeline
 * @deprecated Use UnifiedTimeline instead for enhanced medical features and performance
 * @see wwwroot/js/unified-timeline.js
 * @migration Use TimelineRefactorMigration.migrateVideoTimelineComponent() for automatic migration
 * 
 * Features: Thumbnail support, adaptive scaling, scrubbing, markers
 * 
 * MIGRATION NOTICE: This file will be removed in a future version.
 * Please migrate to UnifiedTimeline which provides:
 * - Enhanced medical workflow integration
 * - FFmpeg segment awareness
 * - Real-time WebSocket updates
 * - Touch gesture support for tablets
 * - Improved performance and memory management
 * - Critical moment highlighting
 */
class VideoTimelineComponent {
    constructor(containerId, options = {}) {
        // DEPRECATED: Issue deprecation warning
        console.warn(`
🚨 DEPRECATION WARNING: VideoTimelineComponent is deprecated
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⚠️  VideoTimelineComponent will be removed in a future version
✅  Please migrate to UnifiedTimeline for enhanced medical features:
    • Enhanced medical workflow integration
    • FFmpeg segment awareness
    • Real-time WebSocket updates
    • Touch gesture support for tablets
    • Critical moment highlighting
    • Improved performance and memory management

🔧 MIGRATION: Use TimelineRefactorMigration.migrateVideoTimelineComponent()
📖 Documentation: See wwwroot/js/unified-timeline.js
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        `);
        
        this.container = document.getElementById(containerId);
        this.options = {
            height: 200,
            thumbnailWidth: 120,
            thumbnailHeight: 68,
            timeScales: [5, 10, 20, 60], // minutes
            currentScale: 0,
            prerecordingDurations: [60, 30, 10], // seconds
            maxDuration: 3600, // 1 hour max
            tickInterval: 5, // seconds for minor ticks
            majorTickInterval: 30, // seconds for major ticks
            ...options
        };
        
        this.duration = 0;
        this.currentTime = 0;
        this.isRecording = false;
        this.segments = [];
        this.criticalMarkers = [];
        this.thumbnails = [];
        this.isDragging = false;
        this.prerecordingBuffer = 60; // seconds available for retroactive recording
        
        this.init();
    }
    
    init() {
        this.createStructure();
        this.setupEventListeners();
        this.updateScale();
    }
    
    createStructure() {
        this.container.className = 'video-timeline-container';
        this.container.innerHTML = `
            <div class="timeline-header">
                <div class="timeline-info">
                    <span class="recording-status" id="timelineStatus">Bereit</span>
                    <span class="timeline-duration" id="timelineDuration">00:00</span>
                    <span class="timeline-scale" id="timelineScale">5 Min</span>
                </div>
                <div class="timeline-controls">
                    <button class="timeline-btn scale-btn" id="scaleBtn" title="Zeitskala ändern">
                        <i class="ms-Icon ms-Icon--ZoomIn"></i>
                    </button>
                    <button class="timeline-btn clear-btn" id="clearBtn" title="Timeline zurücksetzen">
                        <i class="ms-Icon ms-Icon--Clear"></i>
                    </button>
                </div>
            </div>
            
            <div class="timeline-main">
                <div class="timeline-track-container">
                    <!-- Time scale ruler -->
                    <div class="time-ruler" id="timeRuler">
                        <div class="ruler-marks" id="rulerMarks"></div>
                        <div class="ruler-labels" id="rulerLabels"></div>
                    </div>
                    
                    <!-- Main timeline track -->
                    <div class="timeline-track" id="timelineTrack">
                        <!-- Progress bar background -->
                        <div class="progress-background"></div>
                        
                        <!-- Prerecording buffer indicator -->
                        <div class="prerecording-buffer" id="prerecordingBuffer"></div>
                        
                        <!-- Recording segments -->
                        <div class="recording-segments" id="recordingSegments"></div>
                        
                        <!-- Critical moment markers -->
                        <div class="critical-markers" id="criticalMarkers"></div>
                        
                        <!-- Current time indicator -->
                        <div class="time-indicator" id="timeIndicator">
                            <div class="indicator-line"></div>
                            <div class="indicator-handle"></div>
                        </div>
                        
                        <!-- Storage usage indicator -->
                        <div class="storage-indicator" id="storageIndicator"></div>
                    </div>
                    
                    <!-- Thumbnail strip -->
                    <div class="thumbnail-track" id="thumbnailTrack">
                        <div class="thumbnail-container" id="thumbnailContainer"></div>
                    </div>
                </div>
                
                <!-- Timeline labels and info -->
                <div class="timeline-footer">
                    <div class="buffer-info">
                        <span class="buffer-label">Puffer:</span>
                        <span class="buffer-time" id="bufferTime">60s</span>
                    </div>
                    <div class="storage-info">
                        <span class="storage-label">Speicher:</span>
                        <span class="storage-usage" id="storageUsage">0 MB</span>
                    </div>
                </div>
            </div>
        `;
        
        this.timelineTrack = this.container.querySelector('#timelineTrack');
        this.timeIndicator = this.container.querySelector('#timeIndicator');
        this.thumbnailContainer = this.container.querySelector('#thumbnailContainer');
        this.rulerMarks = this.container.querySelector('#rulerMarks');
        this.rulerLabels = this.container.querySelector('#rulerLabels');
        this.recordingSegments = this.container.querySelector('#recordingSegments');
        this.criticalMarkersEl = this.container.querySelector('#criticalMarkers');
        this.prerecordingBufferEl = this.container.querySelector('#prerecordingBuffer');
    }
    
    setupEventListeners() {
        // Scale button
        this.container.querySelector('#scaleBtn').addEventListener('click', () => {
            this.nextScale();
        });
        
        // Clear button
        this.container.querySelector('#clearBtn').addEventListener('click', () => {
            this.clear();
        });
        
        // Timeline scrubbing
        this.timelineTrack.addEventListener('mousedown', this.onScrubStart.bind(this));
        this.timelineTrack.addEventListener('touchstart', this.onScrubStart.bind(this));
        
        document.addEventListener('mousemove', this.onScrubMove.bind(this));
        document.addEventListener('touchmove', this.onScrubMove.bind(this));
        
        document.addEventListener('mouseup', this.onScrubEnd.bind(this));
        document.addEventListener('touchend', this.onScrubEnd.bind(this));
        
        // Prevent context menu on timeline
        this.timelineTrack.addEventListener('contextmenu', (e) => e.preventDefault());
    }
    
    onScrubStart(e) {
        e.preventDefault();
        this.isDragging = true;
        this.timelineTrack.classList.add('scrubbing');
        this.updateTimeFromPosition(e);
        
        // Emit seek event
        this.emit('seek', { time: this.currentTime });
    }
    
    onScrubMove(e) {
        if (!this.isDragging) return;
        e.preventDefault();
        this.updateTimeFromPosition(e);
        
        // Emit seek event (throttled)
        clearTimeout(this.seekTimeout);
        this.seekTimeout = setTimeout(() => {
            this.emit('seek', { time: this.currentTime });
        }, 16); // ~60fps
    }
    
    onScrubEnd(e) {
        if (!this.isDragging) return;
        e.preventDefault();
        this.isDragging = false;
        this.timelineTrack.classList.remove('scrubbing');
        
        // Final seek event
        this.emit('seek', { time: this.currentTime });
    }
    
    updateTimeFromPosition(e) {
        const rect = this.timelineTrack.getBoundingClientRect();
        const clientX = e.clientX || (e.touches && e.touches[0].clientX);
        const position = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
        const maxTime = this.options.timeScales[this.options.currentScale] * 60;
        
        this.currentTime = position * maxTime;
        this.updateTimeIndicator();
        this.updateTimeDisplay();
    }
    
    nextScale() {
        this.options.currentScale = (this.options.currentScale + 1) % this.options.timeScales.length;
        this.updateScale();
        this.emit('scaleChanged', { 
            scale: this.options.timeScales[this.options.currentScale] 
        });
    }
    
    updateScale() {
        const scale = this.options.timeScales[this.options.currentScale];
        this.container.querySelector('#timelineScale').textContent = `${scale} Min`;
        this.renderTimeRuler();
        this.updateTimeIndicator();
        this.updateThumbnails();
        this.updateSegments();
        this.updateCriticalMarkers();
    }
    
    renderTimeRuler() {
        const scale = this.options.timeScales[this.options.currentScale];
        const totalSeconds = scale * 60;
        const trackWidth = this.timelineTrack.offsetWidth;
        
        // Clear existing marks
        this.rulerMarks.innerHTML = '';
        this.rulerLabels.innerHTML = '';
        
        // Calculate tick intervals based on scale
        let majorInterval, minorInterval;
        if (scale <= 5) {
            majorInterval = 60; // 1 minute
            minorInterval = 15; // 15 seconds
        } else if (scale <= 20) {
            majorInterval = 300; // 5 minutes
            minorInterval = 60; // 1 minute
        } else {
            majorInterval = 600; // 10 minutes
            minorInterval = 300; // 5 minutes
        }
        
        // Render ticks and labels
        for (let time = 0; time <= totalSeconds; time += minorInterval) {
            const position = (time / totalSeconds) * 100;
            const isMajor = time % majorInterval === 0;
            
            // Create tick mark
            const tick = document.createElement('div');
            tick.className = `ruler-tick ${isMajor ? 'major' : 'minor'}`;
            tick.style.left = `${position}%`;
            this.rulerMarks.appendChild(tick);
            
            // Create label for major ticks
            if (isMajor) {
                const label = document.createElement('div');
                label.className = 'ruler-label';
                label.style.left = `${position}%`;
                label.textContent = this.formatTime(time);
                this.rulerLabels.appendChild(label);
            }
        }
    }
    
    updateTimeIndicator() {
        const scale = this.options.timeScales[this.options.currentScale];
        const totalSeconds = scale * 60;
        const position = Math.min(100, (this.currentTime / totalSeconds) * 100);
        
        this.timeIndicator.style.left = `${position}%`;
        this.updateTimeDisplay();
    }
    
    updateTimeDisplay() {
        const durationEl = this.container.querySelector('#timelineDuration');
        durationEl.textContent = this.formatTime(this.currentTime);
    }
    
    formatTime(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }
    
    // Public API methods
    
    startRecording() {
        this.isRecording = true;
        this.container.querySelector('#timelineStatus').textContent = 'Aufnahme läuft';
        this.container.classList.add('recording');
        
        // Start a new segment
        this.startNewSegment();
        
        this.emit('recordingStarted');
    }
    
    stopRecording() {
        this.isRecording = false;
        this.container.querySelector('#timelineStatus').textContent = 'Gestoppt';
        this.container.classList.remove('recording');
        
        // End current segment
        this.endCurrentSegment();
        
        this.emit('recordingStopped');
    }
    
    pauseRecording() {
        this.isRecording = false;
        this.container.querySelector('#timelineStatus').textContent = 'Pausiert';
        this.container.classList.add('paused');
        
        this.emit('recordingPaused');
    }
    
    resumeRecording() {
        this.isRecording = true;
        this.container.querySelector('#timelineStatus').textContent = 'Aufnahme läuft';
        this.container.classList.remove('paused');
        
        this.emit('recordingResumed');
    }
    
    updateTime(currentTime, duration = null) {
        this.currentTime = currentTime;
        if (duration !== null) {
            this.duration = duration;
        }
        
        this.updateTimeIndicator();
        
        // Auto-scale if we exceed current scale
        const currentScale = this.options.timeScales[this.options.currentScale] * 60;
        if (currentTime > currentScale * 0.9 && this.options.currentScale < this.options.timeScales.length - 1) {
            this.nextScale();
        }
    }
    
    addCriticalMarker(time = null) {
        const markerTime = time || this.currentTime;
        const markerId = `marker_${Date.now()}`;
        
        this.criticalMarkers.push({
            id: markerId,
            time: markerTime,
            timestamp: new Date()
        });
        
        this.updateCriticalMarkers();
        this.emit('criticalMarkerAdded', { time: markerTime, id: markerId });
        
        return markerId;
    }
    
    updateCriticalMarkers() {
        const scale = this.options.timeScales[this.options.currentScale] * 60;
        
        this.criticalMarkersEl.innerHTML = '';
        
        this.criticalMarkers.forEach(marker => {
            if (marker.time <= scale) {
                const position = (marker.time / scale) * 100;
                const markerEl = document.createElement('div');
                markerEl.className = 'critical-marker';
                markerEl.style.left = `${position}%`;
                markerEl.title = `Kritischer Moment: ${this.formatTime(marker.time)}`;
                markerEl.innerHTML = '<div class="marker-flag"></div>';
                this.criticalMarkersEl.appendChild(markerEl);
            }
        });
    }
    
    addThumbnail(imageData, time = null, type = 'frame') {
        const thumbnailTime = time || this.currentTime;
        const thumbnailId = `thumb_${Date.now()}`;
        
        this.thumbnails.push({
            id: thumbnailId,
            time: thumbnailTime,
            imageData,
            type // 'frame', 'photo', 'key'
        });
        
        this.updateThumbnails();
        this.emit('thumbnailAdded', { time: thumbnailTime, id: thumbnailId });
        
        return thumbnailId;
    }
    
    updateThumbnails() {
        const scale = this.options.timeScales[this.options.currentScale] * 60;
        
        this.thumbnailContainer.innerHTML = '';
        
        this.thumbnails.forEach(thumbnail => {
            if (thumbnail.time <= scale) {
                const position = (thumbnail.time / scale) * 100;
                const thumbEl = document.createElement('div');
                thumbEl.className = `timeline-thumbnail ${thumbnail.type}`;
                thumbEl.style.left = `${position}%`;
                
                const img = document.createElement('img');
                img.src = thumbnail.imageData;
                img.alt = `Frame at ${this.formatTime(thumbnail.time)}`;
                
                thumbEl.appendChild(img);
                this.thumbnailContainer.appendChild(thumbEl);
            }
        });
    }
    
    startNewSegment() {
        const segmentId = `segment_${Date.now()}`;
        const segment = {
            id: segmentId,
            startTime: this.currentTime,
            endTime: null,
            isActive: true
        };
        
        this.segments.push(segment);
        this.updateSegments();
        
        return segmentId;
    }
    
    endCurrentSegment() {
        const activeSegment = this.segments.find(s => s.isActive);
        if (activeSegment) {
            activeSegment.endTime = this.currentTime;
            activeSegment.isActive = false;
            this.updateSegments();
        }
    }
    
    updateSegments() {
        const scale = this.options.timeScales[this.options.currentScale] * 60;
        
        this.recordingSegments.innerHTML = '';
        
        this.segments.forEach(segment => {
            const startPos = (segment.startTime / scale) * 100;
            const endTime = segment.endTime || this.currentTime;
            const endPos = (endTime / scale) * 100;
            const width = endPos - startPos;
            
            if (startPos < 100) {
                const segmentEl = document.createElement('div');
                segmentEl.className = `recording-segment ${segment.isActive ? 'active' : 'completed'}`;
                segmentEl.style.left = `${startPos}%`;
                segmentEl.style.width = `${Math.min(width, 100 - startPos)}%`;
                
                this.recordingSegments.appendChild(segmentEl);
            }
        });
    }
    
    updatePrerecordingBuffer() {
        const scale = this.options.timeScales[this.options.currentScale] * 60;
        const bufferWidth = (this.prerecordingBuffer / scale) * 100;
        
        this.prerecordingBufferEl.style.width = `${Math.min(bufferWidth, 100)}%`;
        
        const bufferTimeEl = this.container.querySelector('#bufferTime');
        bufferTimeEl.textContent = `${this.prerecordingBuffer}s`;
    }
    
    updateStorageUsage(usageMB) {
        const storageEl = this.container.querySelector('#storageUsage');
        storageEl.textContent = `${usageMB.toFixed(1)} MB`;
        
        // Update storage indicator on timeline
        const maxStorage = 1000; // MB
        const usagePercent = (usageMB / maxStorage) * 100;
        
        this.container.querySelector('#storageIndicator').style.width = `${usagePercent}%`;
    }
    
    clear() {
        this.currentTime = 0;
        this.duration = 0;
        this.segments = [];
        this.criticalMarkers = [];
        this.thumbnails = [];
        
        this.updateTimeIndicator();
        this.updateSegments();
        this.updateCriticalMarkers();
        this.updateThumbnails();
        
        this.container.querySelector('#timelineStatus').textContent = 'Bereit';
        this.container.classList.remove('recording', 'paused');
        
        this.emit('cleared');
    }
    
    // Event system
    emit(eventType, data = {}) {
        const event = new CustomEvent(`timeline:${eventType}`, {
            detail: data
        });
        this.container.dispatchEvent(event);
    }
    
    on(eventType, callback) {
        this.container.addEventListener(`timeline:${eventType}`, callback);
    }
    
    off(eventType, callback) {
        this.container.removeEventListener(`timeline:${eventType}`, callback);
    }
}

// Export for use in other modules
window.VideoTimelineComponent = VideoTimelineComponent;