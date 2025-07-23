/**
 * UnifiedTimeline - Phase 2 Timeline Consolidation
 * 
 * Combines the best features from existing timeline implementations:
 * - AdaptiveTimeline: Intelligent scaling and thumbnail management
 * - VideoTimelineComponent: Professional UI and segment support
 * - TimelineIntegrationManager: Medical workflow integration
 * 
 * Features:
 * - FFmpeg segment awareness with visual boundaries
 * - Real-time thumbnail updates with intelligent caching
 * - Touch gesture support for mobile/tablet interfaces
 * - Medical-grade precision and compliance features
 * - Event-driven architecture for extensibility
 */

class UnifiedTimeline extends EventTarget {
    constructor(container, options = {}) {
        super();
        
        this.container = typeof container === 'string' ? 
            document.getElementById(container) : container;
        
        if (!this.container) {
            throw new Error('UnifiedTimeline: Container not found');
        }
        
        // Enhanced options combining all timeline features
        this.options = {
            // Display settings
            height: options.height || 120,
            thumbnailWidth: options.thumbnailWidth || 160,
            thumbnailHeight: options.thumbnailHeight || 90,
            
            // Scaling settings (from AdaptiveTimeline)
            minScale: options.minScale || 30,        // 30 seconds minimum view
            maxScale: options.maxScale || 3600,      // 1 hour maximum view
            defaultScale: options.defaultScale || 300, // 5 minutes default
            timeScales: options.timeScales || [30, 60, 120, 300, 600, 900, 1800, 3600],
            
            // FFmpeg integration
            segmentDuration: options.segmentDuration || 10,  // FFmpeg segment duration
            showSegmentBoundaries: options.showSegmentBoundaries !== false,
            enableLiveUpdates: options.enableLiveUpdates !== false,
            
            // Interaction settings
            enableTouch: options.enableTouch !== false,
            enableWheel: options.enableWheel !== false,
            enableKeyboard: options.enableKeyboard !== false,
            enableDragToSeek: options.enableDragToSeek !== false,
            
            // Medical features
            medicalMode: options.medicalMode || false,
            frameAccuracy: options.frameAccuracy !== false,
            criticalMomentHighlight: options.criticalMomentHighlight !== false,
            
            // Performance settings
            thumbnailInterval: options.thumbnailInterval || 1.0,  // seconds
            cacheLimit: options.cacheLimit || 500,
            renderingThrottle: options.renderingThrottle || 16,  // 60fps
            
            // Integration options
            videoEngine: options.videoEngine || null,
            streamingPlayer: options.streamingPlayer || null,
            
            ...options
        };
        
        // State management
        this.state = {
            duration: 0,
            currentTime: 0,
            position: 0,           // viewport position in seconds
            scale: this.options.defaultScale,
            pixelsPerSecond: 0,
            viewportWidth: 0,
            isRecording: false,
            isLive: false,
            isDragging: false,
            lastUpdateTime: 0,
            prerecordingBuffer: 60 // seconds
        };
        
        // Data collections
        this.segments = [];
        this.markers = [];
        this.thumbnails = new Map();
        this.criticalMoments = [];
        
        // UI elements
        this.elements = {};
        
        // Event handling
        this.eventListeners = new Map();
        this.touchState = {
            touches: [],
            lastDistance: 0,
            initialScale: 0
        };
        
        // Performance optimization
        this.renderingFrame = null;
        this.resizeObserver = null;
        
        // Initialize
        this.init();
    }
    
    init() {
        console.log('🎬 Initializing UnifiedTimeline');
        
        this.createDOMStructure();
        this.setupVideoEngineIntegration();
        this.attachEventListeners();
        this.setupResizeObserver();
        this.calculateDimensions();
        this.render();
        
        // Initial state
        this.emit('initialized', { timeline: this });
    }
    
    createDOMStructure() {
        this.container.className = 'unified-timeline-container';
        this.container.innerHTML = `
            <div class="timeline-header">
                <div class="timeline-info">
                    <span class="timeline-status">Ready</span>
                    <span class="timeline-duration">00:00</span>
                    <span class="timeline-scale">${this.formatTimeScale(this.state.scale)}</span>
                </div>
                <div class="timeline-controls">
                    <button class="timeline-btn zoom-out" title="Zoom Out">−</button>
                    <button class="timeline-btn zoom-in" title="Zoom In">+</button>
                    <button class="timeline-btn scale-cycle" title="Cycle Scale">⧉</button>
                </div>
            </div>
            <div class="timeline-viewport">
                <div class="timeline-track">
                    <div class="timeline-segments"></div>
                    <div class="timeline-thumbnails"></div>
                    <div class="timeline-markers"></div>
                    <div class="timeline-playhead"></div>
                    <div class="timeline-ruler"></div>
                </div>
            </div>
            <div class="timeline-scrubber">
                <div class="scrubber-track">
                    <div class="scrubber-thumb"></div>
                </div>
            </div>
        `;
        
        // Cache DOM elements
        this.elements = {
            header: this.container.querySelector('.timeline-header'),
            status: this.container.querySelector('.timeline-status'),
            duration: this.container.querySelector('.timeline-duration'),
            scaleInfo: this.container.querySelector('.timeline-scale'),
            viewport: this.container.querySelector('.timeline-viewport'),
            track: this.container.querySelector('.timeline-track'),
            segments: this.container.querySelector('.timeline-segments'),
            thumbnails: this.container.querySelector('.timeline-thumbnails'),
            markers: this.container.querySelector('.timeline-markers'),
            playhead: this.container.querySelector('.timeline-playhead'),
            ruler: this.container.querySelector('.timeline-ruler'),
            scrubber: this.container.querySelector('.timeline-scrubber'),
            scrubberTrack: this.container.querySelector('.scrubber-track'),
            scrubberThumb: this.container.querySelector('.scrubber-thumb'),
            zoomOut: this.container.querySelector('.zoom-out'),
            zoomIn: this.container.querySelector('.zoom-in'),
            scaleCycle: this.container.querySelector('.scale-cycle')
        };
    }
    
    setupVideoEngineIntegration() {
        if (this.options.videoEngine) {
            this.videoEngine = this.options.videoEngine;
            
            // Bind to video engine events
            this.videoEngine.on('segmentCompleted', this.onSegmentCompleted.bind(this));
            this.videoEngine.on('thumbnailReady', this.onThumbnailReady.bind(this));
            this.videoEngine.on('recordingStarted', this.onRecordingStarted.bind(this));
            this.videoEngine.on('recordingStopped', this.onRecordingStopped.bind(this));
            
            console.log('🔌 VideoEngine integration enabled');
        }
        
        if (this.options.streamingPlayer) {
            this.streamingPlayer = this.options.streamingPlayer;
            
            // Bind to streaming player events
            this.streamingPlayer.on('timeupdate', this.onTimeUpdate.bind(this));
            this.streamingPlayer.on('durationchange', this.onDurationChange.bind(this));
            this.streamingPlayer.on('segmentCompleted', this.onSegmentCompleted.bind(this));
            
            console.log('📺 StreamingPlayer integration enabled');
        }
    }
    
    attachEventListeners() {
        // Zoom controls
        this.elements.zoomOut.addEventListener('click', () => this.zoomOut());
        this.elements.zoomIn.addEventListener('click', () => this.zoomIn());
        this.elements.scaleCycle.addEventListener('click', () => this.cycleScale());
        
        // Touch gestures
        if (this.options.enableTouch) {
            this.setupTouchGestures();
        }
        
        // Mouse wheel zoom
        if (this.options.enableWheel) {
            this.elements.viewport.addEventListener('wheel', this.onWheel.bind(this), {
                passive: false
            });
        }
        
        // Keyboard navigation
        if (this.options.enableKeyboard) {
            this.container.addEventListener('keydown', this.onKeyDown.bind(this));
            this.container.tabIndex = 0; // Make focusable
        }
        
        // Drag to seek
        if (this.options.enableDragToSeek) {
            this.setupDragToSeek();
        }
        
        // Scrubber interaction
        this.setupScrubberEvents();
    }
    
    setupTouchGestures() {
        let touches = [];
        let lastDistance = 0;
        let initialScale = 0;
        
        this.elements.viewport.addEventListener('touchstart', (e) => {
            touches = Array.from(e.touches);
            
            if (touches.length === 2) {
                e.preventDefault();
                lastDistance = this.getTouchDistance(touches);
                initialScale = this.state.scale;
            }
        }, { passive: false });
        
        this.elements.viewport.addEventListener('touchmove', (e) => {
            if (e.touches.length === 2) {
                e.preventDefault();
                
                const newDistance = this.getTouchDistance(Array.from(e.touches));
                const scaleRatio = newDistance / lastDistance;
                const newScale = Math.max(this.options.minScale, 
                    Math.min(this.options.maxScale, initialScale / scaleRatio));
                
                this.setScale(newScale);
            } else if (e.touches.length === 1 && touches.length === 1) {
                // Single finger pan
                const deltaX = e.touches[0].clientX - touches[0].clientX;
                const deltaTime = deltaX / this.state.pixelsPerSecond;
                this.setPosition(this.state.position - deltaTime);
            }
            
            touches = Array.from(e.touches);
        }, { passive: false });
        
        this.elements.viewport.addEventListener('touchend', () => {
            touches = [];
            lastDistance = 0;
            initialScale = 0;
        });
    }
    
    setupDragToSeek() {
        let isDragging = false;
        let startX = 0;
        let startTime = 0;
        
        this.elements.track.addEventListener('mousedown', (e) => {
            isDragging = true;
            startX = e.clientX;
            startTime = this.pixelsToTime(e.offsetX);
            this.state.isDragging = true;
            
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
            
            this.emit('seekStart', { time: startTime });
        });
        
        const onMouseMove = (e) => {
            if (!isDragging) return;
            
            const deltaX = e.clientX - startX;
            const deltaTime = deltaX / this.state.pixelsPerSecond;
            const newTime = Math.max(0, Math.min(this.state.duration, startTime + deltaTime));
            
            this.setCurrentTime(newTime);
            this.emit('seeking', { time: newTime });
        };
        
        const onMouseUp = () => {
            isDragging = false;
            this.state.isDragging = false;
            
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            
            this.emit('seekEnd', { time: this.state.currentTime });
        };
    }
    
    setupScrubberEvents() {
        let isDragging = false;
        
        this.elements.scrubberThumb.addEventListener('mousedown', (e) => {
            isDragging = true;
            e.preventDefault();
            
            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        });
        
        const onMouseMove = (e) => {
            if (!isDragging) return;
            
            const rect = this.elements.scrubberTrack.getBoundingClientRect();
            const ratio = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width));
            const newTime = ratio * this.state.duration;
            
            this.setCurrentTime(newTime);
            this.emit('seeking', { time: newTime });
        };
        
        const onMouseUp = () => {
            isDragging = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        };
        
        // Click to seek on scrubber track
        this.elements.scrubberTrack.addEventListener('click', (e) => {
            if (e.target === this.elements.scrubberThumb) return;
            
            const rect = this.elements.scrubberTrack.getBoundingClientRect();
            const ratio = (e.clientX - rect.left) / rect.width;
            const newTime = ratio * this.state.duration;
            
            this.setCurrentTime(newTime);
            this.emit('seek', { time: newTime });
        });
    }
    
    setupResizeObserver() {
        if ('ResizeObserver' in window) {
            this.resizeObserver = new ResizeObserver((entries) => {
                for (const entry of entries) {
                    this.calculateDimensions();
                    this.scheduleRender();
                }
            });
            
            this.resizeObserver.observe(this.container);
        } else {
            // Fallback for older browsers
            window.addEventListener('resize', () => {
                this.calculateDimensions();
                this.scheduleRender();
            });
        }
    }
    
    calculateDimensions() {
        const rect = this.elements.viewport.getBoundingClientRect();
        this.state.viewportWidth = rect.width;
        this.state.pixelsPerSecond = this.state.viewportWidth / this.state.scale;
        
        // Update container height
        this.container.style.height = `${this.options.height}px`;
    }
    
    // Scaling methods
    setScale(newScale) {
        newScale = Math.max(this.options.minScale, 
                   Math.min(this.options.maxScale, newScale));
        
        if (newScale !== this.state.scale) {
            const oldScale = this.state.scale;
            const centerTime = this.state.position + (this.state.viewportWidth / 2) / this.state.pixelsPerSecond;
            
            this.state.scale = newScale;
            this.state.pixelsPerSecond = this.state.viewportWidth / this.state.scale;
            
            // Keep center point stable
            this.state.position = centerTime - (this.state.viewportWidth / 2) / this.state.pixelsPerSecond;
            this.state.position = Math.max(0, this.state.position);
            
            this.updateScaleInfo();
            this.scheduleRender();
            
            this.emit('scaleChanged', { oldScale, newScale, position: this.state.position });
        }
    }
    
    zoomIn() {
        const currentIndex = this.options.timeScales.indexOf(this.state.scale);
        if (currentIndex > 0) {
            this.setScale(this.options.timeScales[currentIndex - 1]);
        }
    }
    
    zoomOut() {
        const currentIndex = this.options.timeScales.indexOf(this.state.scale);
        if (currentIndex < this.options.timeScales.length - 1) {
            this.setScale(this.options.timeScales[currentIndex + 1]);
        }
    }
    
    cycleScale() {
        const currentIndex = this.options.timeScales.indexOf(this.state.scale);
        const nextIndex = (currentIndex + 1) % this.options.timeScales.length;
        this.setScale(this.options.timeScales[nextIndex]);
    }
    
    // Position and time management
    setPosition(newPosition) {
        this.state.position = Math.max(0, newPosition);
        this.scheduleRender();
        this.emit('positionChanged', { position: this.state.position });
    }
    
    setCurrentTime(newTime) {
        newTime = Math.max(0, Math.min(this.state.duration, newTime));
        
        if (newTime !== this.state.currentTime) {
            this.state.currentTime = newTime;
            this.updatePlayheadPosition();
            this.updateScrubberPosition();
            this.emit('timeupdate', { time: newTime });
        }
    }
    
    setDuration(newDuration) {
        if (newDuration !== this.state.duration) {
            this.state.duration = newDuration;
            this.updateDurationDisplay();
            this.scheduleRender();
            this.emit('durationchange', { duration: newDuration });
        }
    }
    
    // Segment management (FFmpeg integration)
    addSegment(segment) {
        const normalizedSegment = {
            number: segment.number || segment.segmentNumber || this.segments.length + 1,
            startTime: segment.startTime || (segment.number * this.options.segmentDuration),
            duration: segment.duration || this.options.segmentDuration,
            endTime: segment.endTime || (segment.startTime + segment.duration),
            isComplete: segment.isComplete !== false,
            canEdit: segment.canEdit !== false,
            quality: segment.quality || 'standard',
            size: segment.size || 0,
            url: segment.url || null,
            thumbnail: segment.thumbnail || null,
            metadata: segment.metadata || {}
        };
        
        // Check for existing segment
        const existingIndex = this.segments.findIndex(s => s.number === normalizedSegment.number);
        if (existingIndex >= 0) {
            this.segments[existingIndex] = normalizedSegment;
        } else {
            this.segments.push(normalizedSegment);
            this.segments.sort((a, b) => a.number - b.number);
        }
        
        this.renderSegment(normalizedSegment);
        this.emit('segmentAdded', normalizedSegment);
        
        console.log(`📹 Segment ${normalizedSegment.number} added: ${normalizedSegment.startTime}s-${normalizedSegment.endTime}s`);
    }
    
    renderSegment(segment) {
        const existingElement = this.elements.segments.querySelector(`[data-segment="${segment.number}"]`);
        if (existingElement) {
            existingElement.remove();
        }
        
        const segmentElement = document.createElement('div');
        segmentElement.className = 'timeline-segment';
        segmentElement.dataset.segment = segment.number;
        
        const startX = this.timeToPixels(segment.startTime);
        const width = this.timeToPixels(segment.duration);
        
        segmentElement.style.left = `${startX}px`;
        segmentElement.style.width = `${width}px`;
        segmentElement.style.height = '100%';
        
        // Apply segment state classes
        if (segment.isComplete) {
            segmentElement.classList.add('complete');
        }
        
        if (segment.canEdit) {
            segmentElement.classList.add('editable');
        }
        
        if (this.options.medicalMode && segment.metadata?.critical) {
            segmentElement.classList.add('critical');
        }
        
        // Add quality indicator
        if (segment.quality) {
            segmentElement.classList.add(`quality-${segment.quality}`);
        }
        
        // Event handlers
        if (segment.canEdit) {
            segmentElement.addEventListener('click', () => {
                this.emit('segmentClick', { segment, element: segmentElement });
            });
            
            segmentElement.addEventListener('dblclick', () => {
                this.setCurrentTime(segment.startTime);
                this.emit('segmentDoubleClick', { segment, element: segmentElement });
            });
        }
        
        // Tooltip with segment information
        segmentElement.title = `Segment ${segment.number}: ${this.formatTime(segment.startTime)} - ${this.formatTime(segment.endTime)}`;
        
        this.elements.segments.appendChild(segmentElement);
    }
    
    removeSegment(segmentNumber) {
        const index = this.segments.findIndex(s => s.number === segmentNumber);
        if (index >= 0) {
            const removed = this.segments.splice(index, 1)[0];
            
            const element = this.elements.segments.querySelector(`[data-segment="${segmentNumber}"]`);
            if (element) {
                element.remove();
            }
            
            this.emit('segmentRemoved', removed);
            console.log(`🗑️ Segment ${segmentNumber} removed`);
        }
    }
    
    // Marker management
    addMarker(marker) {
        const normalizedMarker = {
            id: marker.id || `marker_${Date.now()}`,
            time: marker.time || marker.timestamp || 0,
            type: marker.type || 'default',
            title: marker.title || marker.description || '',
            description: marker.description || '',
            color: marker.color || '#007bff',
            critical: marker.critical || false,
            editable: marker.editable !== false,
            metadata: marker.metadata || {}
        };
        
        // Remove existing marker with same ID
        this.removeMarker(normalizedMarker.id);
        
        this.markers.push(normalizedMarker);
        this.markers.sort((a, b) => a.time - b.time);
        
        this.renderMarker(normalizedMarker);
        this.emit('markerAdded', normalizedMarker);
        
        console.log(`📍 Marker added: ${normalizedMarker.title} at ${normalizedMarker.time}s`);
    }
    
    renderMarker(marker) {
        const markerElement = document.createElement('div');
        markerElement.className = 'timeline-marker';
        markerElement.dataset.markerId = marker.id;
        
        const x = this.timeToPixels(marker.time);
        markerElement.style.left = `${x}px`;
        markerElement.style.borderLeftColor = marker.color;
        
        // Apply marker type classes
        markerElement.classList.add(`marker-${marker.type}`);
        
        if (marker.critical) {
            markerElement.classList.add('critical');
        }
        
        if (marker.editable) {
            markerElement.classList.add('editable');
        }
        
        // Marker content
        const markerContent = document.createElement('div');
        markerContent.className = 'marker-content';
        markerContent.textContent = marker.title;
        markerElement.appendChild(markerContent);
        
        // Event handlers
        if (marker.editable) {
            markerElement.addEventListener('click', () => {
                this.setCurrentTime(marker.time);
                this.emit('markerClick', { marker, element: markerElement });
            });
            
            markerElement.addEventListener('dblclick', () => {
                this.emit('markerEdit', { marker, element: markerElement });
            });
        }
        
        markerElement.title = `${marker.title}\n${this.formatTime(marker.time)}${marker.description ? '\n' + marker.description : ''}`;
        
        this.elements.markers.appendChild(markerElement);
    }
    
    removeMarker(markerId) {
        const index = this.markers.findIndex(m => m.id === markerId);
        if (index >= 0) {
            const removed = this.markers.splice(index, 1)[0];
            
            const element = this.elements.markers.querySelector(`[data-marker-id="${markerId}"]`);
            if (element) {
                element.remove();
            }
            
            this.emit('markerRemoved', removed);
        }
    }
    
    // Thumbnail management with FFmpeg API integration
    async updateThumbnail(timestamp, url) {
        // Cache thumbnail
        this.thumbnails.set(timestamp, {
            url: url,
            timestamp: Date.now(),
            loaded: true
        });
        
        // Update visible thumbnail if it exists
        const thumbnailElement = this.elements.thumbnails.querySelector(
            `[data-timestamp="${timestamp}"]`
        );
        
        if (thumbnailElement) {
            const img = thumbnailElement.querySelector('img');
            if (img) {
                img.src = url;
                img.onload = () => {
                    thumbnailElement.classList.add('loaded');
                };
            }
        }
        
        this.emit('thumbnailUpdated', { timestamp, url });
    }
    
    async loadThumbnailsInView() {
        const startTime = this.state.position;
        const endTime = this.state.position + this.state.scale;
        const interval = Math.max(0.5, this.options.thumbnailInterval);
        
        const promises = [];
        for (let time = startTime; time <= endTime; time += interval) {
            if (!this.thumbnails.has(time)) {
                promises.push(this.loadThumbnail(time));
            }
        }
        
        if (promises.length > 0) {
            await Promise.allSettled(promises);
        }
    }
    
    async loadThumbnail(timestamp) {
        try {
            let url = null;
            
            // Try streaming player first (FFmpeg API)
            if (this.streamingPlayer && this.streamingPlayer.loadThumbnail) {
                url = await this.streamingPlayer.loadThumbnail(timestamp, this.options.thumbnailWidth);
            }
            // Fallback to video engine
            else if (this.videoEngine && this.videoEngine.getThumbnail) {
                url = await this.videoEngine.getThumbnail(timestamp, this.options.thumbnailWidth);
            }
            
            if (url) {
                await this.updateThumbnail(timestamp, url);
            }
            
            return url;
        } catch (error) {
            console.warn(`Failed to load thumbnail for ${timestamp}s:`, error);
            return null;
        }
    }
    
    renderThumbnails() {
        // Clear existing thumbnails
        this.elements.thumbnails.innerHTML = '';
        
        const startTime = this.state.position;
        const endTime = this.state.position + this.state.scale;
        const interval = Math.max(0.5, this.options.thumbnailInterval);
        
        for (let time = startTime; time <= endTime; time += interval) {
            if (time > this.state.duration) break;
            
            this.renderThumbnail(time);
        }
        
        // Load thumbnails asynchronously
        this.loadThumbnailsInView();
    }
    
    renderThumbnail(timestamp) {
        const thumbnailElement = document.createElement('div');
        thumbnailElement.className = 'timeline-thumbnail';
        thumbnailElement.dataset.timestamp = timestamp;
        
        const x = this.timeToPixels(timestamp);
        thumbnailElement.style.left = `${x}px`;
        thumbnailElement.style.width = `${this.options.thumbnailWidth}px`;
        thumbnailElement.style.height = `${this.options.thumbnailHeight}px`;
        
        // Create image element
        const img = document.createElement('img');
        img.className = 'thumbnail-img';
        img.style.width = '100%';
        img.style.height = '100%';
        img.style.objectFit = 'cover';
        
        // Check cache
        const cached = this.thumbnails.get(timestamp);
        if (cached && cached.url) {
            img.src = cached.url;
            img.onload = () => thumbnailElement.classList.add('loaded');
        } else {
            // Placeholder
            img.src = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYwIiBoZWlnaHQ9IjkwIiB2aWV3Qm94PSIwIDAgMTYwIDkwIiBmaWxsPSJub25lIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxyZWN0IHdpZHRoPSIxNjAiIGhlaWdodD0iOTAiIGZpbGw9IiNmOGY5ZmEiLz48cGF0aCBkPSJNODAgNDVMNjAgMzBIMTAwTDgwIDQ1WiIgZmlsbD0iI2RlZTJlNiIvPjx0ZXh0IHg9IjgwIiB5PSI2NSIgZm9udC1mYW1pbHk9InN5c3RlbS11aSIgZm9udC1zaXplPSIxMCIgZmlsbD0iIzZjNzU3ZCIgdGV4dC1hbmNob3I9Im1pZGRsZSI+TG9hZGluZy4uLjwvdGV4dD48L3N2Zz4=';
        }
        
        // Time label
        const timeLabel = document.createElement('div');
        timeLabel.className = 'thumbnail-time';
        timeLabel.textContent = this.formatTime(timestamp);
        
        thumbnailElement.appendChild(img);
        thumbnailElement.appendChild(timeLabel);
        
        this.elements.thumbnails.appendChild(thumbnailElement);
    }
    
    // Rendering and layout
    scheduleRender() {
        if (this.renderingFrame) {
            cancelAnimationFrame(this.renderingFrame);
        }
        
        this.renderingFrame = requestAnimationFrame(() => {
            this.render();
            this.renderingFrame = null;
        });
    }
    
    render() {
        this.updatePlayheadPosition();
        this.updateScrubberPosition();
        this.renderRuler();
        this.renderSegments();
        this.renderMarkers();
        this.renderThumbnails();
        
        this.emit('rendered');
    }
    
    renderRuler() {
        this.elements.ruler.innerHTML = '';
        
        const startTime = this.state.position;
        const endTime = this.state.position + this.state.scale;
        
        // Calculate appropriate tick interval based on scale
        let majorTickInterval, minorTickInterval;
        
        if (this.state.scale <= 60) {
            majorTickInterval = 10;
            minorTickInterval = 1;
        } else if (this.state.scale <= 300) {
            majorTickInterval = 30;
            minorTickInterval = 5;
        } else if (this.state.scale <= 900) {
            majorTickInterval = 60;
            minorTickInterval = 10;
        } else {
            majorTickInterval = 300;
            minorTickInterval = 60;
        }
        
        // Render major ticks
        for (let time = Math.floor(startTime / majorTickInterval) * majorTickInterval; 
             time <= endTime; 
             time += majorTickInterval) {
            
            if (time < 0) continue;
            
            const x = this.timeToPixels(time);
            const tick = document.createElement('div');
            tick.className = 'ruler-tick major';
            tick.style.left = `${x}px`;
            
            const label = document.createElement('span');
            label.className = 'tick-label';
            label.textContent = this.formatTime(time);
            tick.appendChild(label);
            
            this.elements.ruler.appendChild(tick);
        }
        
        // Render minor ticks
        for (let time = Math.floor(startTime / minorTickInterval) * minorTickInterval; 
             time <= endTime; 
             time += minorTickInterval) {
            
            if (time < 0 || time % majorTickInterval === 0) continue;
            
            const x = this.timeToPixels(time);
            const tick = document.createElement('div');
            tick.className = 'ruler-tick minor';
            tick.style.left = `${x}px`;
            
            this.elements.ruler.appendChild(tick);
        }
    }
    
    renderSegments() {
        // Re-render all segments to update positions
        this.elements.segments.innerHTML = '';
        
        this.segments.forEach(segment => {
            // Only render visible segments
            if (segment.endTime >= this.state.position && 
                segment.startTime <= this.state.position + this.state.scale) {
                this.renderSegment(segment);
            }
        });
    }
    
    renderMarkers() {
        // Re-render all markers to update positions
        this.elements.markers.innerHTML = '';
        
        this.markers.forEach(marker => {
            // Only render visible markers
            if (marker.time >= this.state.position && 
                marker.time <= this.state.position + this.state.scale) {
                this.renderMarker(marker);
            }
        });
    }
    
    updatePlayheadPosition() {
        const x = this.timeToPixels(this.state.currentTime);
        this.elements.playhead.style.left = `${x}px`;
        
        // Update playhead visibility
        const isVisible = this.state.currentTime >= this.state.position && 
                         this.state.currentTime <= this.state.position + this.state.scale;
        this.elements.playhead.style.display = isVisible ? 'block' : 'none';
    }
    
    updateScrubberPosition() {
        if (this.state.duration > 0) {
            const ratio = this.state.currentTime / this.state.duration;
            const x = ratio * this.elements.scrubberTrack.clientWidth;
            this.elements.scrubberThumb.style.left = `${x}px`;
        }
    }
    
    updateScaleInfo() {
        this.elements.scaleInfo.textContent = this.formatTimeScale(this.state.scale);
    }
    
    updateDurationDisplay() {
        this.elements.duration.textContent = this.formatTime(this.state.duration);
    }
    
    // Utility methods
    timeToPixels(time) {
        return (time - this.state.position) * this.state.pixelsPerSecond;
    }
    
    pixelsToTime(pixels) {
        return this.state.position + pixels / this.state.pixelsPerSecond;
    }
    
    getTouchDistance(touches) {
        if (touches.length < 2) return 0;
        
        const dx = touches[1].clientX - touches[0].clientX;
        const dy = touches[1].clientY - touches[0].clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }
    
    formatTime(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = Math.floor(seconds % 60);
        const ms = Math.floor((seconds % 1) * 1000);
        
        if (this.options.medicalMode || this.options.frameAccuracy) {
            // Medical mode: show milliseconds for precision
            if (hours > 0) {
                return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
            } else {
                return `${minutes}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(3, '0')}`;
            }
        } else {
            // Standard mode
            if (hours > 0) {
                return `${hours}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
            } else {
                return `${minutes}:${secs.toString().padStart(2, '0')}`;
            }
        }
    }
    
    formatTimeScale(scale) {
        if (scale < 60) {
            return `${scale}s`;
        } else if (scale < 3600) {
            return `${Math.round(scale / 60)}m`;
        } else {
            return `${Math.round(scale / 3600)}h`;
        }
    }
    
    // Event handling utilities
    emit(eventName, data = {}) {
        const event = new CustomEvent(eventName, { detail: data });
        this.dispatchEvent(event);
        
        // Also emit on container for legacy compatibility
        if (this.container) {
            const containerEvent = new CustomEvent(`timeline:${eventName}`, { detail: data });
            this.container.dispatchEvent(containerEvent);
        }
    }
    
    // Event handlers for video engine integration
    onSegmentCompleted(segment) {
        console.log('📹 UnifiedTimeline: Segment completed', segment);
        this.addSegment(segment);
        
        // Update duration if needed
        const segmentEndTime = segment.startTime + segment.duration;
        if (segmentEndTime > this.state.duration) {
            this.setDuration(segmentEndTime);
        }
    }
    
    onThumbnailReady(data) {
        console.log('🖼️ UnifiedTimeline: Thumbnail ready', data);
        this.updateThumbnail(data.timestamp, data.url);
    }
    
    onRecordingStarted(session) {
        console.log('🔴 UnifiedTimeline: Recording started', session);
        this.state.isRecording = true;
        this.state.isLive = true;
        this.elements.status.textContent = 'Recording';
        this.elements.status.classList.add('recording');
        
        this.emit('recordingStarted', session);
    }
    
    onRecordingStopped(session) {
        console.log('⏹️ UnifiedTimeline: Recording stopped', session);
        this.state.isRecording = false;
        this.state.isLive = false;
        this.elements.status.textContent = 'Ready';
        this.elements.status.classList.remove('recording');
        
        this.emit('recordingStopped', session);
    }
    
    onTimeUpdate(data) {
        if (!this.state.isDragging) {
            this.setCurrentTime(data.time || data.currentTime || 0);
        }
    }
    
    onDurationChange(data) {
        this.setDuration(data.duration || 0);
    }
    
    // Input event handlers
    onWheel(e) {
        e.preventDefault();
        
        const delta = e.deltaY > 0 ? 1.2 : 0.8;
        this.setScale(this.state.scale * delta);
    }
    
    onKeyDown(e) {
        switch (e.key) {
            case 'ArrowLeft':
                e.preventDefault();
                this.setCurrentTime(this.state.currentTime - 1);
                break;
            case 'ArrowRight':
                e.preventDefault();
                this.setCurrentTime(this.state.currentTime + 1);
                break;
            case '+':
            case '=':
                e.preventDefault();
                this.zoomIn();
                break;
            case '-':
                e.preventDefault();
                this.zoomOut();
                break;
            case ' ':
                e.preventDefault();
                this.emit('playPause');
                break;
            case 'Home':
                e.preventDefault();
                this.setCurrentTime(0);
                break;
            case 'End':
                e.preventDefault();
                this.setCurrentTime(this.state.duration);
                break;
        }
    }
    
    // Public API methods
    seek(time) {
        this.setCurrentTime(time);
    }
    
    jumpToSegment(segmentNumber) {
        const segment = this.segments.find(s => s.number === segmentNumber);
        if (segment) {
            this.setCurrentTime(segment.startTime);
            this.emit('segmentJump', { segment });
        }
    }
    
    jumpToMarker(markerId) {
        const marker = this.markers.find(m => m.id === markerId);
        if (marker) {
            this.setCurrentTime(marker.time);
            this.emit('markerJump', { marker });
        }
    }
    
    zoomToFit() {
        if (this.state.duration > 0) {
            let targetScale = this.state.duration;
            
            // Find closest time scale
            const closest = this.options.timeScales.reduce((prev, curr) => 
                Math.abs(curr - targetScale) < Math.abs(prev - targetScale) ? curr : prev
            );
            
            this.setScale(closest);
            this.setPosition(0);
        }
    }
    
    zoomToSelection(startTime, endTime) {
        const duration = endTime - startTime;
        
        // Find appropriate scale
        let targetScale = duration * 1.1; // Add 10% padding
        const closest = this.options.timeScales.reduce((prev, curr) => 
            Math.abs(curr - targetScale) < Math.abs(prev - targetScale) ? curr : prev
        );
        
        this.setScale(closest);
        this.setPosition(startTime - (this.state.scale - duration) / 2);
    }
    
    // Clean up
    destroy() {
        if (this.resizeObserver) {
            this.resizeObserver.disconnect();
        }
        
        if (this.renderingFrame) {
            cancelAnimationFrame(this.renderingFrame);
        }
        
        // Remove all event listeners
        this.eventListeners.clear();
        
        // Clean up DOM
        this.container.innerHTML = '';
        
        console.log('🗑️ UnifiedTimeline destroyed');
    }
    
    // Get current state for debugging/serialization
    getState() {
        return {
            ...this.state,
            segments: [...this.segments],
            markers: [...this.markers],
            thumbnailCount: this.thumbnails.size
        };
    }
}

// Export for use in other modules
window.UnifiedTimeline = UnifiedTimeline;