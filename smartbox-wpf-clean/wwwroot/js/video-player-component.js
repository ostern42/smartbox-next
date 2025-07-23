/**
 * Medical Video Player Component for SmartBox Next
 * Touch-optimized video playback with frame-accurate control
 */
class MedicalVideoPlayer {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.options = {
            touchTargetSize: 44, // Medical UI standard
            frameRate: 30,
            enableFrameStep: true,
            enableSpeedControl: true,
            enableAnnotations: true,
            maxPlaybackSpeed: 4,
            ...options
        };
        
        this.video = null;
        this.isPlaying = false;
        this.currentSpeed = 1;
        this.duration = 0;
        this.currentTime = 0;
        this.isLoading = false;
        this.videoBlob = null;
        this.annotations = [];
        this.activeSegment = null;
        this.isLiveRecording = false;
        
        this.init();
    }
    
    init() {
        this.createStructure();
        this.setupEventListeners();
        console.log('MedicalVideoPlayer: Initialized');
    }
    
    createStructure() {
        this.container.className = 'medical-video-player';
        this.container.innerHTML = `
            <div class="video-container">
                <video id="videoElement" playsinline webkit-playsinline></video>
                <canvas id="annotationCanvas" class="annotation-layer"></canvas>
                
                <!-- Loading overlay -->
                <div class="video-loading" id="videoLoading">
                    <div class="loading-spinner"></div>
                    <span>Video wird geladen...</span>
                </div>
                
                <!-- Touch overlay for gestures -->
                <div class="video-touch-overlay" id="videoTouchOverlay">
                    <div class="touch-hint" id="touchHint">
                        <i class="ms-Icon ms-Icon--PlaySolid"></i>
                        <span>Antippen zum Abspielen</span>
                    </div>
                </div>
                
                <!-- Time display overlay -->
                <div class="video-time-overlay">
                    <span class="current-time" id="currentTimeDisplay">00:00</span>
                    <span class="time-separator">/</span>
                    <span class="total-time" id="totalTimeDisplay">00:00</span>
                </div>
            </div>
            
            <!-- Playback controls -->
            <div class="video-controls">
                <div class="control-row primary-controls">
                    <!-- Frame step backward -->
                    <button class="control-btn frame-step" id="frameBackBtn" title="Frame zurück">
                        <i class="ms-Icon ms-Icon--Previous"></i>
                        <span class="frame-label">-1</span>
                    </button>
                    
                    <!-- Play/Pause -->
                    <button class="control-btn play-pause" id="playPauseBtn" title="Abspielen/Pause">
                        <i class="ms-Icon ms-Icon--PlaySolid" id="playIcon"></i>
                        <i class="ms-Icon ms-Icon--Pause hidden" id="pauseIcon"></i>
                    </button>
                    
                    <!-- Frame step forward -->
                    <button class="control-btn frame-step" id="frameForwardBtn" title="Frame vorwärts">
                        <i class="ms-Icon ms-Icon--Next"></i>
                        <span class="frame-label">+1</span>
                    </button>
                </div>
                
                <!-- Progress bar -->
                <div class="progress-container" id="progressContainer">
                    <div class="progress-track">
                        <div class="progress-buffered" id="progressBuffered"></div>
                        <div class="progress-played" id="progressPlayed"></div>
                        
                        <!-- Frame markers for critical moments -->
                        <div class="frame-markers" id="frameMarkers"></div>
                        
                        <!-- Scrubber handle -->
                        <div class="progress-handle" id="progressHandle">
                            <div class="handle-tooltip" id="handleTooltip">00:00</div>
                        </div>
                    </div>
                    
                    <!-- Frame counter -->
                    <div class="frame-info">
                        <span>Frame: </span>
                        <span id="currentFrame">0</span>
                        <span>/</span>
                        <span id="totalFrames">0</span>
                    </div>
                </div>
                
                <!-- Secondary controls -->
                <div class="control-row secondary-controls">
                    <!-- Speed control -->
                    <div class="speed-control">
                        <button class="control-btn speed-btn" id="speedBtn" title="Geschwindigkeit">
                            <span id="speedLabel">1x</span>
                        </button>
                        <div class="speed-menu hidden" id="speedMenu">
                            <button class="speed-option" data-speed="0.25">0.25x</button>
                            <button class="speed-option" data-speed="0.5">0.5x</button>
                            <button class="speed-option active" data-speed="1">1x</button>
                            <button class="speed-option" data-speed="1.5">1.5x</button>
                            <button class="speed-option" data-speed="2">2x</button>
                            <button class="speed-option" data-speed="4">4x</button>
                        </div>
                    </div>
                    
                    <!-- Jump controls -->
                    <button class="control-btn jump-btn" id="jump10sBack" title="10s zurück">
                        <i class="ms-Icon ms-Icon--Undo"></i>
                        <span>10s</span>
                    </button>
                    
                    <button class="control-btn jump-btn" id="jump10sForward" title="10s vorwärts">
                        <span>10s</span>
                        <i class="ms-Icon ms-Icon--Redo"></i>
                    </button>
                    
                    <!-- Annotation toggle -->
                    <button class="control-btn toggle-btn" id="annotationToggle" title="Annotationen">
                        <i class="ms-Icon ms-Icon--EditNote"></i>
                    </button>
                    
                    <!-- Fullscreen -->
                    <button class="control-btn" id="fullscreenBtn" title="Vollbild">
                        <i class="ms-Icon ms-Icon--FullScreen"></i>
                    </button>
                </div>
            </div>
            
            <!-- Touch gesture hints -->
            <div class="gesture-hints" id="gestureHints">
                <div class="hint-item">
                    <i class="ms-Icon ms-Icon--TouchPointer"></i>
                    <span>Tippen: Play/Pause</span>
                </div>
                <div class="hint-item">
                    <i class="ms-Icon ms-Icon--SwipeRight"></i>
                    <span>Wischen: Vor/Zurück</span>
                </div>
                <div class="hint-item">
                    <i class="ms-Icon ms-Icon--Pinch"></i>
                    <span>Pinch: Zoom</span>
                </div>
            </div>
        `;
        
        // Get references
        this.video = this.container.querySelector('#videoElement');
        this.annotationCanvas = this.container.querySelector('#annotationCanvas');
        this.progressHandle = this.container.querySelector('#progressHandle');
        this.progressPlayed = this.container.querySelector('#progressPlayed');
        this.touchOverlay = this.container.querySelector('#videoTouchOverlay');
    }
    
    setupEventListeners() {
        // Video events
        this.video.addEventListener('loadstart', () => this.onLoadStart());
        this.video.addEventListener('loadedmetadata', () => this.onLoadedMetadata());
        this.video.addEventListener('canplay', () => this.onCanPlay());
        this.video.addEventListener('timeupdate', () => this.onTimeUpdate());
        this.video.addEventListener('ended', () => this.onEnded());
        this.video.addEventListener('error', (e) => this.onError(e));
        
        // Control buttons
        this.container.querySelector('#playPauseBtn').addEventListener('click', () => this.togglePlayPause());
        this.container.querySelector('#frameBackBtn').addEventListener('click', () => this.stepFrame(-1));
        this.container.querySelector('#frameForwardBtn').addEventListener('click', () => this.stepFrame(1));
        this.container.querySelector('#jump10sBack').addEventListener('click', () => this.jump(-10));
        this.container.querySelector('#jump10sForward').addEventListener('click', () => this.jump(10));
        this.container.querySelector('#fullscreenBtn').addEventListener('click', () => this.toggleFullscreen());
        
        // Speed control
        this.setupSpeedControl();
        
        // Progress bar interaction
        this.setupProgressBarInteraction();
        
        // Touch gestures
        this.setupTouchGestures();
        
        // Annotation toggle
        this.container.querySelector('#annotationToggle').addEventListener('click', () => this.toggleAnnotations());
    }
    
    setupSpeedControl() {
        const speedBtn = this.container.querySelector('#speedBtn');
        const speedMenu = this.container.querySelector('#speedMenu');
        
        speedBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            speedMenu.classList.toggle('hidden');
        });
        
        speedMenu.addEventListener('click', (e) => {
            const option = e.target.closest('.speed-option');
            if (option) {
                const speed = parseFloat(option.dataset.speed);
                this.setPlaybackSpeed(speed);
                
                // Update UI
                speedMenu.querySelectorAll('.speed-option').forEach(opt => {
                    opt.classList.remove('active');
                });
                option.classList.add('active');
                speedMenu.classList.add('hidden');
            }
        });
        
        // Close menu on outside click
        document.addEventListener('click', () => {
            speedMenu.classList.add('hidden');
        });
    }
    
    setupProgressBarInteraction() {
        const progressContainer = this.container.querySelector('#progressContainer');
        let isDragging = false;
        
        const updateProgress = (e) => {
            const rect = progressContainer.getBoundingClientRect();
            const x = (e.clientX || e.touches[0].clientX) - rect.left;
            const percentage = Math.max(0, Math.min(1, x / rect.width));
            const time = percentage * this.duration;
            
            this.seek(time);
            this.updateTooltip(time, x);
        };
        
        // Mouse events
        progressContainer.addEventListener('mousedown', (e) => {
            isDragging = true;
            updateProgress(e);
        });
        
        document.addEventListener('mousemove', (e) => {
            if (isDragging) updateProgress(e);
        });
        
        document.addEventListener('mouseup', () => {
            isDragging = false;
        });
        
        // Touch events
        progressContainer.addEventListener('touchstart', (e) => {
            isDragging = true;
            updateProgress(e);
        });
        
        document.addEventListener('touchmove', (e) => {
            if (isDragging) updateProgress(e);
        });
        
        document.addEventListener('touchend', () => {
            isDragging = false;
        });
    }
    
    setupTouchGestures() {
        const overlay = this.touchOverlay;
        let touchStartX = 0;
        let touchStartTime = 0;
        let lastTap = 0;
        
        overlay.addEventListener('touchstart', (e) => {
            touchStartX = e.touches[0].clientX;
            touchStartTime = Date.now();
        });
        
        overlay.addEventListener('touchend', (e) => {
            const touchEndX = e.changedTouches[0].clientX;
            const touchDuration = Date.now() - touchStartTime;
            const deltaX = touchEndX - touchStartX;
            
            if (Math.abs(deltaX) < 50 && touchDuration < 300) {
                // Tap detected
                const now = Date.now();
                if (now - lastTap < 300) {
                    // Double tap - toggle fullscreen
                    this.toggleFullscreen();
                } else {
                    // Single tap - play/pause
                    this.togglePlayPause();
                }
                lastTap = now;
            } else if (Math.abs(deltaX) > 100) {
                // Swipe detected
                if (deltaX > 0) {
                    this.jump(10); // Swipe right - forward
                } else {
                    this.jump(-10); // Swipe left - backward
                }
            }
        });
        
        // Click for desktop
        overlay.addEventListener('click', (e) => {
            if (!e.touches) {
                this.togglePlayPause();
            }
        });
    }
    
    // Video control methods
    
    loadVideo(videoBlob, metadata = {}) {
        console.log('MedicalVideoPlayer: Loading video...');
        this.videoBlob = videoBlob;
        this.metadata = metadata;
        
        // Create object URL
        const videoUrl = URL.createObjectURL(videoBlob);
        this.video.src = videoUrl;
        
        // Load annotations if provided
        if (metadata.annotations) {
            this.annotations = metadata.annotations;
            this.renderAnnotations();
        }
        
        // Load critical moments
        if (metadata.criticalMoments) {
            this.addFrameMarkers(metadata.criticalMoments);
        }
    }
    
    togglePlayPause() {
        if (this.isPlaying) {
            this.pause();
        } else {
            this.play();
        }
    }
    
    play() {
        this.video.play();
        this.isPlaying = true;
        this.updatePlayPauseButton();
        this.emit('play');
    }
    
    pause() {
        this.video.pause();
        this.isPlaying = false;
        this.updatePlayPauseButton();
        this.emit('pause');
    }
    
    seek(time) {
        this.video.currentTime = Math.max(0, Math.min(time, this.duration));
        this.emit('seek', { time: this.video.currentTime });
    }
    
    stepFrame(direction) {
        const frameTime = 1 / this.options.frameRate;
        const newTime = this.video.currentTime + (frameTime * direction);
        this.seek(newTime);
        
        // Ensure we're paused for frame stepping
        if (this.isPlaying) {
            this.pause();
        }
    }
    
    jump(seconds) {
        const newTime = this.video.currentTime + seconds;
        this.seek(newTime);
    }
    
    setPlaybackSpeed(speed) {
        this.currentSpeed = speed;
        this.video.playbackRate = speed;
        this.container.querySelector('#speedLabel').textContent = speed + 'x';
        this.emit('speedChanged', { speed });
    }
    
    toggleFullscreen() {
        if (!document.fullscreenElement) {
            this.container.requestFullscreen();
        } else {
            document.exitFullscreen();
        }
    }
    
    toggleAnnotations() {
        this.annotationCanvas.classList.toggle('hidden');
        const btn = this.container.querySelector('#annotationToggle');
        btn.classList.toggle('active');
    }
    
    // Event handlers
    
    onLoadStart() {
        this.isLoading = true;
        this.container.querySelector('#videoLoading').classList.remove('hidden');
    }
    
    onLoadedMetadata() {
        this.duration = this.video.duration;
        this.updateTimeline();
        this.updateFrameInfo();
    }
    
    onCanPlay() {
        this.isLoading = false;
        this.container.querySelector('#videoLoading').classList.add('hidden');
        this.container.querySelector('#touchHint').classList.remove('hidden');
        
        // Set canvas size to match video
        this.annotationCanvas.width = this.video.videoWidth;
        this.annotationCanvas.height = this.video.videoHeight;
    }
    
    onTimeUpdate() {
        this.currentTime = this.video.currentTime;
        
        // Check if we have an active segment and need to stop at outPoint
        if (this.activeSegment && this.activeSegment.outPoint !== undefined) {
            if (this.currentTime >= this.activeSegment.outPoint) {
                this.video.currentTime = this.activeSegment.outPoint;
                this.pause();
                console.log('MedicalVideoPlayer: Reached segment end at', this.activeSegment.outPoint);
                this.emit('segmentEnded', { segment: this.activeSegment });
            }
        }
        
        this.updateProgress();
        this.updateTimeDisplay();
        this.updateFrameDisplay();
        this.checkAnnotations();
    }
    
    onEnded() {
        this.isPlaying = false;
        this.updatePlayPauseButton();
        this.emit('ended');
    }
    
    onError(e) {
        console.error('MedicalVideoPlayer: Video error', e);
        this.emit('error', { error: e });
    }
    
    // UI update methods
    
    updatePlayPauseButton() {
        const playIcon = this.container.querySelector('#playIcon');
        const pauseIcon = this.container.querySelector('#pauseIcon');
        
        if (this.isPlaying) {
            playIcon.classList.add('hidden');
            pauseIcon.classList.remove('hidden');
            this.container.querySelector('#touchHint').classList.add('hidden');
        } else {
            playIcon.classList.remove('hidden');
            pauseIcon.classList.add('hidden');
        }
    }
    
    updateProgress() {
        const percentage = (this.currentTime / this.duration) * 100;
        this.progressPlayed.style.width = percentage + '%';
        this.progressHandle.style.left = percentage + '%';
    }
    
    updateTimeDisplay() {
        const formatTime = (seconds) => {
            const mins = Math.floor(seconds / 60);
            const secs = Math.floor(seconds % 60);
            return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        };
        
        this.container.querySelector('#currentTimeDisplay').textContent = formatTime(this.currentTime);
        this.container.querySelector('#totalTimeDisplay').textContent = formatTime(this.duration);
    }
    
    updateFrameDisplay() {
        const currentFrame = Math.floor(this.currentTime * this.options.frameRate);
        
        if (this.isLiveRecording) {
            // For live recording, show current frame and elapsed time
            const totalFrames = currentFrame; // Total frames recorded so far
            this.container.querySelector('#currentFrame').textContent = currentFrame;
            this.container.querySelector('#totalFrames').textContent = totalFrames;
        } else {
            // For playback, show current frame and total frames
            const totalFrames = isFinite(this.duration) ? Math.floor(this.duration * this.options.frameRate) : 0;
            this.container.querySelector('#currentFrame').textContent = currentFrame;
            this.container.querySelector('#totalFrames').textContent = totalFrames;
        }
    }
    
    updateFrameInfo() {
        const totalFrames = Math.floor(this.duration * this.options.frameRate);
        this.container.querySelector('#totalFrames').textContent = totalFrames;
    }
    
    updateTooltip(time, x) {
        const tooltip = this.container.querySelector('#handleTooltip');
        const formatTime = (seconds) => {
            const mins = Math.floor(seconds / 60);
            const secs = Math.floor(seconds % 60);
            return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        };
        
        tooltip.textContent = formatTime(time);
        tooltip.style.left = x + 'px';
        tooltip.classList.add('visible');
        
        clearTimeout(this.tooltipTimeout);
        this.tooltipTimeout = setTimeout(() => {
            tooltip.classList.remove('visible');
        }, 1000);
    }
    
    // Frame markers for critical moments
    
    addFrameMarkers(criticalMoments) {
        const markersContainer = this.container.querySelector('#frameMarkers');
        markersContainer.innerHTML = '';
        
        criticalMoments.forEach(moment => {
            const position = (moment.time / this.duration) * 100;
            const marker = document.createElement('div');
            marker.className = 'frame-marker critical';
            marker.style.left = position + '%';
            marker.title = moment.description || 'Kritischer Moment';
            marker.dataset.time = moment.time;
            
            marker.addEventListener('click', () => {
                this.seek(moment.time);
            });
            
            markersContainer.appendChild(marker);
        });
    }
    
    // Annotation support
    
    renderAnnotations() {
        const ctx = this.annotationCanvas.getContext('2d');
        ctx.clearRect(0, 0, this.annotationCanvas.width, this.annotationCanvas.height);
        
        // Filter annotations for current time
        const currentAnnotations = this.annotations.filter(ann => {
            return this.currentTime >= ann.startTime && this.currentTime <= ann.endTime;
        });
        
        currentAnnotations.forEach(ann => {
            this.drawAnnotation(ctx, ann);
        });
    }
    
    drawAnnotation(ctx, annotation) {
        ctx.save();
        
        switch (annotation.type) {
            case 'text':
                ctx.font = '16px Open Sans';
                ctx.fillStyle = annotation.color || '#ffffff';
                ctx.strokeStyle = '#000000';
                ctx.lineWidth = 3;
                ctx.strokeText(annotation.text, annotation.x, annotation.y);
                ctx.fillText(annotation.text, annotation.x, annotation.y);
                break;
                
            case 'arrow':
                this.drawArrow(ctx, annotation);
                break;
                
            case 'circle':
                ctx.strokeStyle = annotation.color || '#ff0000';
                ctx.lineWidth = annotation.width || 2;
                ctx.beginPath();
                ctx.arc(annotation.x, annotation.y, annotation.radius, 0, Math.PI * 2);
                ctx.stroke();
                break;
                
            case 'rectangle':
                ctx.strokeStyle = annotation.color || '#ff0000';
                ctx.lineWidth = annotation.width || 2;
                ctx.strokeRect(annotation.x, annotation.y, annotation.width, annotation.height);
                break;
        }
        
        ctx.restore();
    }
    
    drawArrow(ctx, annotation) {
        const headlen = 10;
        const dx = annotation.endX - annotation.startX;
        const dy = annotation.endY - annotation.startY;
        const angle = Math.atan2(dy, dx);
        
        ctx.strokeStyle = annotation.color || '#ff0000';
        ctx.lineWidth = annotation.width || 2;
        
        // Draw line
        ctx.beginPath();
        ctx.moveTo(annotation.startX, annotation.startY);
        ctx.lineTo(annotation.endX, annotation.endY);
        ctx.stroke();
        
        // Draw arrowhead
        ctx.beginPath();
        ctx.moveTo(annotation.endX, annotation.endY);
        ctx.lineTo(
            annotation.endX - headlen * Math.cos(angle - Math.PI / 6),
            annotation.endY - headlen * Math.sin(angle - Math.PI / 6)
        );
        ctx.moveTo(annotation.endX, annotation.endY);
        ctx.lineTo(
            annotation.endX - headlen * Math.cos(angle + Math.PI / 6),
            annotation.endY - headlen * Math.sin(angle + Math.PI / 6)
        );
        ctx.stroke();
    }
    
    checkAnnotations() {
        if (this.annotations.length > 0) {
            this.renderAnnotations();
        }
    }
    
    // Public API
    
    setActiveSegment(segment) {
        this.activeSegment = segment;
        if (segment && segment.inPoint !== undefined) {
            this.seek(segment.inPoint);
        }
        console.log('MedicalVideoPlayer: Active segment set', segment);
    }
    
    clearActiveSegment() {
        this.activeSegment = null;
    }
    
    setLiveRecording(isLive) {
        this.isLiveRecording = isLive;
        if (isLive) {
            // Reset duration for live recording
            this.duration = 0;
        }
    }
    
    getCurrentTime() {
        return this.currentTime;
    }
    
    getCurrentFrame() {
        return Math.floor(this.currentTime * this.options.frameRate);
    }
    
    getDuration() {
        return this.duration;
    }
    
    getTotalFrames() {
        return Math.floor(this.duration * this.options.frameRate);
    }
    
    addAnnotation(annotation) {
        annotation.id = annotation.id || Date.now();
        annotation.startTime = annotation.startTime || this.currentTime;
        annotation.endTime = annotation.endTime || this.currentTime + 5;
        
        this.annotations.push(annotation);
        this.renderAnnotations();
        this.emit('annotationAdded', { annotation });
    }
    
    removeAnnotation(id) {
        const index = this.annotations.findIndex(ann => ann.id === id);
        if (index > -1) {
            const removed = this.annotations.splice(index, 1)[0];
            this.renderAnnotations();
            this.emit('annotationRemoved', { annotation: removed });
        }
    }
    
    getAnnotations() {
        return [...this.annotations];
    }
    
    exportFrame() {
        const canvas = document.createElement('canvas');
        canvas.width = this.video.videoWidth;
        canvas.height = this.video.videoHeight;
        
        const ctx = canvas.getContext('2d');
        ctx.drawImage(this.video, 0, 0);
        
        // Draw annotations if enabled
        if (!this.annotationCanvas.classList.contains('hidden')) {
            ctx.drawImage(this.annotationCanvas, 0, 0);
        }
        
        return canvas.toDataURL('image/jpeg', 0.95);
    }
    
    destroy() {
        // Clean up
        if (this.video.src) {
            URL.revokeObjectURL(this.video.src);
        }
        
        clearTimeout(this.tooltipTimeout);
        
        // Remove all event listeners
        this.video.removeEventListener('loadstart', this.onLoadStart);
        this.video.removeEventListener('loadedmetadata', this.onLoadedMetadata);
        this.video.removeEventListener('canplay', this.onCanPlay);
        this.video.removeEventListener('timeupdate', this.onTimeUpdate);
        this.video.removeEventListener('ended', this.onEnded);
        this.video.removeEventListener('error', this.onError);
    }
    
    // Event system
    
    emit(eventType, data = {}) {
        const event = new CustomEvent(`videoPlayer:${eventType}`, {
            detail: data
        });
        this.container.dispatchEvent(event);
    }
    
    on(eventType, callback) {
        this.container.addEventListener(`videoPlayer:${eventType}`, callback);
    }
    
    off(eventType, callback) {
        this.container.removeEventListener(`videoPlayer:${eventType}`, callback);
    }
}

// Export for use
window.MedicalVideoPlayer = MedicalVideoPlayer;