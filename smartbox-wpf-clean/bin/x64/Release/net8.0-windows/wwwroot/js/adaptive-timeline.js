/**
 * Adaptive Timeline für SmartBox
 * 
 * Eine intelligente Timeline die sich automatisch an die Videolänge anpasst
 * und Echtzeit-Thumbnails für präzise Navigation bietet
 */

class AdaptiveTimeline {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            height: 100,
            thumbnailWidth: 160,
            fps: 25,
            timeScales: [30, 60, 120, 180, 300, 600, 900, 1800, 3600],
            enableWaveform: true,
            enableThumbnails: true,
            enableMotionTracking: true,
            ...options
        };
        
        this.state = {
            duration: 0,
            currentTime: 0,
            timeScale: 30,
            viewportWidth: 0,
            thumbnails: new Map(),
            isGenerating: false,
            isLiveRecording: false,
            playheadPosition: 0,
            selectedRegion: null,
            markers: [],
            lastThumbnailTime: -1,
            thumbnailInterval: 1 // Start with 1 second intervals
        };
        
        this.thumbnailCache = new ThumbnailCache();
        this.motionAnalyzer = new MotionAnalyzer();
        
        this.init();
    }
    
    init() {
        this.createDOM();
        this.setupEventListeners();
        this.setupResizeObserver();
        this.setupWorker();
    }
    
    createDOM() {
        this.container.innerHTML = `
            <div class="adaptive-timeline">
                <div class="timeline-ruler">
                    <div class="timeline-timestamps"></div>
                </div>
                <div class="timeline-tracks">
                    <div class="timeline-thumbnail-track">
                        <canvas class="thumbnail-canvas"></canvas>
                        <div class="thumbnail-loading">
                            <div class="loading-progress"></div>
                        </div>
                    </div>
                    <div class="timeline-waveform-track">
                        <canvas class="waveform-canvas"></canvas>
                    </div>
                    <div class="timeline-motion-track">
                        <canvas class="motion-canvas"></canvas>
                    </div>
                </div>
                <div class="timeline-playhead">
                    <div class="playhead-line"></div>
                    <div class="playhead-handle"></div>
                    <div class="playhead-time">00:00.00</div>
                </div>
                <div class="timeline-selection" style="display: none;">
                    <div class="selection-handle start"></div>
                    <div class="selection-handle end"></div>
                </div>
                <div class="timeline-markers"></div>
            </div>
        `;
        
        this.elements = {
            timeline: this.container.querySelector('.adaptive-timeline'),
            ruler: this.container.querySelector('.timeline-ruler'),
            timestamps: this.container.querySelector('.timeline-timestamps'),
            thumbnailCanvas: this.container.querySelector('.thumbnail-canvas'),
            waveformCanvas: this.container.querySelector('.waveform-canvas'),
            motionCanvas: this.container.querySelector('.motion-canvas'),
            playhead: this.container.querySelector('.timeline-playhead'),
            playheadTime: this.container.querySelector('.playhead-time'),
            selection: this.container.querySelector('.timeline-selection'),
            markers: this.container.querySelector('.timeline-markers'),
            loadingProgress: this.container.querySelector('.loading-progress')
        };
        
        // Canvas contexts
        this.contexts = {
            thumbnail: this.elements.thumbnailCanvas.getContext('2d'),
            waveform: this.elements.waveformCanvas.getContext('2d'),
            motion: this.elements.motionCanvas.getContext('2d')
        };
    }
    
    setupEventListeners() {
        let isDragging = false;
        let dragMode = null; // 'playhead', 'selection-start', 'selection-end'
        
        // Playhead dragging
        this.elements.playhead.addEventListener('mousedown', (e) => {
            isDragging = true;
            dragMode = 'playhead';
            this.elements.timeline.classList.add('dragging');
        });
        
        // Timeline click for seek
        this.elements.timeline.addEventListener('click', (e) => {
            if (!isDragging) {
                const rect = this.elements.timeline.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const time = (x / rect.width) * this.state.timeScale;
                this.seek(time);
            }
        });
        
        // Mouse move for dragging
        document.addEventListener('mousemove', (e) => {
            if (isDragging) {
                const rect = this.elements.timeline.getBoundingClientRect();
                const x = Math.max(0, Math.min(e.clientX - rect.left, rect.width));
                const time = (x / rect.width) * this.state.timeScale;
                
                if (dragMode === 'playhead') {
                    this.updatePlayhead(time);
                }
                
                // Show preview thumbnail while dragging
                this.showPreviewThumbnail(x, time);
            }
        });
        
        // Mouse up
        document.addEventListener('mouseup', () => {
            isDragging = false;
            dragMode = null;
            this.elements.timeline.classList.remove('dragging');
            this.hidePreviewThumbnail();
        });
        
        // Touch support
        this.setupTouchEvents();
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (!this.container.contains(document.activeElement)) return;
            
            switch(e.key) {
                case 'ArrowLeft':
                    this.seek(this.state.currentTime - (e.shiftKey ? 10 : 1));
                    break;
                case 'ArrowRight':
                    this.seek(this.state.currentTime + (e.shiftKey ? 10 : 1));
                    break;
                case '+':
                case '=':
                    this.zoomIn();
                    break;
                case '-':
                    this.zoomOut();
                    break;
            }
        });
    }
    
    setupTouchEvents() {
        let touchStartX = 0;
        let touchStartTime = 0;
        let lastTouchX = 0;
        let velocity = 0;
        
        this.elements.timeline.addEventListener('touchstart', (e) => {
            const touch = e.touches[0];
            touchStartX = touch.clientX;
            lastTouchX = touch.clientX;
            touchStartTime = this.state.currentTime;
            velocity = 0;
        });
        
        this.elements.timeline.addEventListener('touchmove', (e) => {
            e.preventDefault();
            const touch = e.touches[0];
            const deltaX = touch.clientX - lastTouchX;
            velocity = deltaX;
            lastTouchX = touch.clientX;
            
            // Scrub based on movement
            const rect = this.elements.timeline.getBoundingClientRect();
            const scrubAmount = (deltaX / rect.width) * this.state.timeScale;
            this.seek(this.state.currentTime - scrubAmount);
        });
        
        this.elements.timeline.addEventListener('touchend', (e) => {
            // Momentum scrolling
            if (Math.abs(velocity) > 5) {
                this.applyMomentum(velocity);
            }
        });
    }
    
    setupResizeObserver() {
        const resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                const width = entry.contentRect.width;
                if (width !== this.state.viewportWidth) {
                    this.state.viewportWidth = width;
                    this.updateCanvasSizes();
                    this.recalculateTimeScale();
                    this.render();
                }
            }
        });
        
        resizeObserver.observe(this.container);
    }
    
    setupWorker() {
        try {
            // Web Worker für Thumbnail-Generierung
            this.worker = new Worker('js/thumbnail-worker.js');
            
            this.worker.onmessage = (e) => {
                const { frameNumber, thumbnail, progress } = e.data;
                
                if (thumbnail) {
                    this.thumbnailCache.set(frameNumber, thumbnail);
                    this.renderThumbnail(frameNumber);
                }
                
                if (progress !== undefined) {
                    this.updateLoadingProgress(progress);
                }
            };
        } catch (error) {
            console.warn('AdaptiveTimeline: Could not initialize Web Worker:', error);
            console.warn('Thumbnail generation will be disabled');
            this.worker = null;
            // Disable thumbnail features
            this.options.enableThumbnails = false;
        }
    }
    
    // Public API
    
    setVideo(videoElement, duration) {
        this.videoElement = videoElement;
        this.state.duration = duration;
        
        // Set viewport width if not already set
        if (this.state.viewportWidth === 0) {
            this.state.viewportWidth = this.container.clientWidth;
        }
        
        // Calculate optimal time scale
        this.recalculateTimeScale();
        
        // Update canvas sizes immediately
        this.updateCanvasSizes();
        
        // Start thumbnail generation
        if (this.options.enableThumbnails && videoElement) {
            this.generateThumbnails();
        }
        
        // Analyze waveform if audio present
        if (this.options.enableWaveform && videoElement) {
            this.generateWaveform();
        }
        
        // Motion analysis
        if (this.options.enableMotionTracking && videoElement) {
            this.analyzeMotion();
        }
        
        // Force immediate render
        this.render();
    }
    
    seek(time) {
        time = Math.max(0, Math.min(time, this.state.duration));
        this.state.currentTime = time;
        this.updatePlayhead(time);
        
        if (this.onSeek) {
            this.onSeek(time);
        }
    }
    
    updateTime(currentTime) {
        this.state.currentTime = currentTime;
        this.updatePlayhead(currentTime);
        
        // For live recording, generate thumbnails as we go
        if (this.state.isLiveRecording && this.videoElement) {
            this.generateLiveThumbnail(currentTime);
        }
    }
    
    // Time scale management
    
    recalculateTimeScale() {
        const optimalScale = this.calculateOptimalScale(
            this.state.duration, 
            this.state.viewportWidth
        );
        
        if (optimalScale !== this.state.timeScale) {
            const previousScale = this.state.timeScale;
            this.state.timeScale = optimalScale;
            
            // Intelligent thumbnail recycling
            this.recycleThumbnails(previousScale, optimalScale);
            
            // Update ruler
            this.updateRuler();
        }
    }
    
    calculateOptimalScale(duration, viewportWidth) {
        // For live recording or no duration set, always start with 30 seconds
        if (!duration || duration === 0) {
            return 30;
        }
        
        // For recorded videos, find the smallest scale that fits the duration
        for (let scale of this.options.timeScales) {
            if (duration <= scale) {
                return scale;
            }
        }
        
        // For very long videos, use minute-based scaling
        return Math.ceil(duration / 60) * 60;
    }
    
    zoomIn() {
        const currentIndex = this.options.timeScales.indexOf(this.state.timeScale);
        if (currentIndex > 0) {
            this.state.timeScale = this.options.timeScales[currentIndex - 1];
            this.render();
        }
    }
    
    zoomOut() {
        const currentIndex = this.options.timeScales.indexOf(this.state.timeScale);
        if (currentIndex < this.options.timeScales.length - 1 && 
            this.options.timeScales[currentIndex + 1] >= this.state.duration) {
            this.state.timeScale = this.options.timeScales[currentIndex + 1];
            this.render();
        }
    }
    
    // Thumbnail management
    
    setLiveRecording(isLive) {
        this.state.isLiveRecording = isLive;
        if (isLive) {
            this.state.lastThumbnailTime = -1;
            // Calculate initial thumbnail interval for 30s view
            this.state.thumbnailInterval = this.calculateThumbnailInterval();
        }
    }
    
    generateLiveThumbnail(currentTime) {
        if (!this.options.enableThumbnails || !this.videoElement) return;
        
        // Calculate the interval based on current time scale
        const interval = this.calculateThumbnailInterval();
        const frameInterval = interval / this.options.fps;
        
        // Check if we need to generate a thumbnail at this time
        const thumbnailIndex = Math.floor(currentTime / frameInterval) * frameInterval;
        
        // Only generate if we haven't already generated this thumbnail
        if (thumbnailIndex > this.state.lastThumbnailTime && 
            currentTime >= thumbnailIndex) {
            
            this.captureThumbnail(thumbnailIndex);
            this.state.lastThumbnailTime = thumbnailIndex;
            
            // Re-render thumbnails to show the new one
            this.renderThumbnails();
        }
    }
    
    captureThumbnail(time) {
        try {
            const canvas = document.createElement('canvas');
            const height = Math.floor(this.options.thumbnailWidth * 9 / 16);
            canvas.width = this.options.thumbnailWidth;
            canvas.height = height;
            
            const ctx = canvas.getContext('2d');
            ctx.drawImage(this.videoElement, 0, 0, canvas.width, canvas.height);
            
            // Create image from canvas
            const img = new Image();
            img.src = canvas.toDataURL('image/jpeg', 0.7);
            
            // Store in cache with frame number as key
            const frameNumber = Math.floor(time * this.options.fps);
            this.thumbnailCache.set(frameNumber, img);
            
            console.log('Live thumbnail captured at', time, 'seconds, frame', frameNumber);
        } catch (error) {
            console.warn('Failed to capture live thumbnail:', error);
        }
    }
    
    generateThumbnails() {
        if (this.state.isGenerating || !this.worker) return;
        
        this.state.isGenerating = true;
        const interval = this.calculateThumbnailInterval();
        
        // Send video to worker for processing
        this.worker.postMessage({
            command: 'generateThumbnails',
            videoBlob: this.videoElement.src,
            duration: this.state.duration,
            interval: interval,
            thumbnailWidth: this.options.thumbnailWidth,
            fps: this.options.fps
        });
    }
    
    calculateThumbnailInterval() {
        const maxThumbnails = Math.floor(this.state.viewportWidth / this.options.thumbnailWidth);
        const framesInView = this.state.timeScale * this.options.fps;
        return Math.ceil(framesInView / maxThumbnails);
    }
    
    recycleThumbnails(previousScale, newScale) {
        // Knuth-inspired elegant algorithm for thumbnail recycling
        const previousInterval = this.calculateIntervalForScale(previousScale);
        const newInterval = this.calculateIntervalForScale(newScale);
        
        console.log(`Timeline scale change: ${previousScale}s → ${newScale}s`);
        console.log(`Thumbnail interval: ${previousInterval}f → ${newInterval}f`);
        
        if (newScale > previousScale) {
            // Zooming out - we can reuse thumbnails intelligently
            // For example: 30s→60s means we use every 2nd thumbnail
            const reuseFactor = newInterval / previousInterval;
            
            // Mark which thumbnails to keep
            const newCache = new Map();
            for (let [frameNumber, thumbnail] of this.thumbnailCache.cache) {
                // Keep thumbnails that align with new interval
                if (frameNumber % newInterval === 0) {
                    newCache.set(frameNumber, thumbnail);
                }
            }
            
            // Update cache with only the needed thumbnails
            this.thumbnailCache.cache = newCache;
            console.log(`Kept ${newCache.size} thumbnails after zoom out`);
            
        } else {
            // Zooming in - we need more thumbnails between existing ones
            // But we keep all existing thumbnails and generate new ones as needed
            console.log('Zoom in - keeping all existing thumbnails');
            
            // If in live recording, new thumbnails will be generated automatically
            // For playback, we would need to generate intermediate thumbnails
            if (!this.state.isLiveRecording && this.videoElement) {
                this.generateIntermediateThumbnails(previousInterval, newInterval);
            }
        }
    }
    
    calculateIntervalForScale(scale) {
        const viewportWidth = this.state.viewportWidth || this.container.clientWidth;
        const maxThumbnails = Math.floor(viewportWidth / this.options.thumbnailWidth);
        const framesInView = scale * this.options.fps;
        return Math.ceil(framesInView / maxThumbnails);
    }
    
    generateIntermediateThumbnails(oldInterval, newInterval) {
        // Generate thumbnails at the new interval that don't exist yet
        const duration = this.state.duration;
        const fps = this.options.fps;
        
        for (let time = 0; time < duration; time += newInterval / fps) {
            const frameNumber = Math.floor(time * fps);
            
            // Only generate if we don't have this thumbnail
            if (!this.thumbnailCache.get(frameNumber)) {
                // In a real implementation, we would queue this for generation
                console.log(`Need to generate thumbnail at frame ${frameNumber}`);
            }
        }
    }
    
    // Rendering
    
    render() {
        this.updateCanvasSizes();
        this.renderThumbnails();
        this.renderWaveform();
        this.renderMotion();
        this.updateRuler();
        this.updateMarkers();
    }
    
    updateCanvasSizes() {
        const width = this.state.viewportWidth;
        const height = this.options.height;
        
        // Update all canvas sizes
        [this.elements.thumbnailCanvas, this.elements.waveformCanvas, this.elements.motionCanvas].forEach(canvas => {
            canvas.width = width;
            canvas.height = height / 3;
        });
    }
    
    renderThumbnails() {
        const ctx = this.contexts.thumbnail;
        const width = this.state.viewportWidth;
        const height = this.elements.thumbnailCanvas.height;
        
        ctx.clearRect(0, 0, width, height);
        
        // Calculate visible range
        const startTime = 0;
        const endTime = this.state.timeScale;
        const interval = this.calculateThumbnailInterval();
        
        // Render each thumbnail
        let x = 0;
        for (let time = startTime; time < endTime; time += interval / this.options.fps) {
            const frameNumber = Math.floor(time * this.options.fps);
            const thumbnail = this.thumbnailCache.get(frameNumber);
            
            if (thumbnail) {
                ctx.drawImage(thumbnail, x, 0, this.options.thumbnailWidth, height);
            } else {
                // Placeholder
                ctx.fillStyle = 'rgba(255, 255, 255, 0.1)';
                ctx.fillRect(x, 0, this.options.thumbnailWidth, height);
            }
            
            x += this.options.thumbnailWidth;
        }
        
        // Overlay gradient for better visibility
        const gradient = ctx.createLinearGradient(0, 0, 0, height);
        gradient.addColorStop(0, 'rgba(0, 0, 0, 0.2)');
        gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');
        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, width, height);
    }
    
    renderWaveform() {
        // Placeholder for waveform rendering
        const ctx = this.contexts.waveform;
        const width = this.state.viewportWidth;
        const height = this.elements.waveformCanvas.height;
        
        ctx.clearRect(0, 0, width, height);
        ctx.strokeStyle = 'rgba(25, 118, 210, 0.5)';
        ctx.beginPath();
        ctx.moveTo(0, height / 2);
        ctx.lineTo(width, height / 2);
        ctx.stroke();
    }
    
    renderMotion() {
        // Placeholder for motion tracking visualization
        const ctx = this.contexts.motion;
        const width = this.state.viewportWidth;
        const height = this.elements.motionCanvas.height;
        
        ctx.clearRect(0, 0, width, height);
    }
    
    updateRuler() {
        const timestamps = [];
        let interval;
        
        // Determine appropriate interval based on time scale
        if (this.state.timeScale <= 30) {
            interval = 5; // 5 second intervals for 30s view
        } else if (this.state.timeScale <= 60) {
            interval = 10; // 10 second intervals for 60s view
        } else if (this.state.timeScale <= 180) {
            interval = 30; // 30 second intervals for up to 3 minutes
        } else {
            interval = 60; // 1 minute intervals for longer views
        }
        
        for (let time = 0; time <= this.state.timeScale; time += interval) {
            const x = (time / this.state.timeScale) * 100;
            timestamps.push(`<div class="timestamp" style="left: ${x}%">${this.formatTimeShort(time)}</div>`);
        }
        
        this.elements.timestamps.innerHTML = timestamps.join('');
    }
    
    updatePlayhead(time) {
        const position = (time / this.state.timeScale) * 100;
        this.elements.playhead.style.left = position + '%';
        this.elements.playheadTime.textContent = this.formatTime(time);
    }
    
    formatTime(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        const ms = Math.floor((seconds % 1) * 100);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
    }
    
    formatTimeShort(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        if (mins === 0) {
            return `0:${secs.toString().padStart(2, '0')}`;
        }
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    }
    
    // Preview thumbnail while scrubbing
    
    showPreviewThumbnail(x, time) {
        // Implementation for showing preview thumbnail at cursor position
    }
    
    hidePreviewThumbnail() {
        // Hide preview thumbnail
    }
    
    // Momentum scrolling
    
    applyMomentum(velocity) {
        let currentVelocity = velocity;
        const friction = 0.95;
        
        const animate = () => {
            if (Math.abs(currentVelocity) > 0.5) {
                const rect = this.elements.timeline.getBoundingClientRect();
                const scrubAmount = (currentVelocity / rect.width) * this.state.timeScale;
                this.seek(this.state.currentTime - scrubAmount);
                
                currentVelocity *= friction;
                requestAnimationFrame(animate);
            }
        };
        
        animate();
    }
    
    // Loading progress
    
    updateLoadingProgress(progress) {
        this.elements.loadingProgress.style.width = progress + '%';
        
        if (progress >= 100) {
            this.state.isGenerating = false;
            this.elements.loadingProgress.parentElement.style.display = 'none';
        }
    }
}

// Thumbnail Cache with intelligent management
class ThumbnailCache {
    constructor() {
        this.cache = new Map();
        this.maxSize = 200; // Maximum thumbnails in memory
    }
    
    set(frameNumber, thumbnail) {
        // LRU eviction if cache is full
        if (this.cache.size >= this.maxSize) {
            const firstKey = this.cache.keys().next().value;
            this.cache.delete(firstKey);
        }
        
        this.cache.set(frameNumber, thumbnail);
    }
    
    get(frameNumber) {
        const thumbnail = this.cache.get(frameNumber);
        if (thumbnail) {
            // Move to end (LRU)
            this.cache.delete(frameNumber);
            this.cache.set(frameNumber, thumbnail);
        }
        return thumbnail;
    }
    
    decimateByFactor(factor) {
        const newCache = new Map();
        let index = 0;
        
        for (let [frame, thumbnail] of this.cache) {
            if (index % factor === 0) {
                newCache.set(frame, thumbnail);
            }
            index++;
        }
        
        this.cache = newCache;
    }
    
    clear() {
        this.cache.clear();
    }
}

// Motion analyzer for detecting scene changes
class MotionAnalyzer {
    constructor() {
        this.sceneChanges = [];
        this.motionData = [];
    }
    
    analyze(videoElement) {
        // Placeholder for motion analysis
        // Would use frame differencing to detect scene changes
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdaptiveTimeline;
}