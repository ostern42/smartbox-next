# Phase 2: Timeline Consolidation (Priority: High)

This phase creates a unified timeline component that combines the best features from existing implementations while adding new capabilities for the FFmpeg video engine integration.

## 2.1 Create Unified Timeline Component

**File**: `unified-timeline.js` (New)

### Unified Timeline Implementation

```javascript
// Unified timeline component combining best features
class UnifiedTimeline extends EventTarget {
    constructor(container, options = {}) {
        super();
        this.container = container;
        this.options = {
            minScale: 30,        // 30 seconds minimum view
            maxScale: 3600,      // 1 hour maximum view
            defaultScale: 30,   // 5 minutes default
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
}
```

### Intelligent Scaling Features

```javascript
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
```

### Touch Gesture Support

```javascript
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
```

### Key Features
- Combines best features from existing timeline implementations
- FFmpeg segment awareness with visual boundaries
- Real-time thumbnail updates with caching
- separate timelines for still photos (placed at capture time) and video (start/stop in/out markers and preview)
- Touch gesture support for mobile devices
- Intelligent scaling with center-point stability
- Event-driven architecture for extensibility

## 2.2 Migration Strategy

**File**: `timeline-migration.js` (Temporary)

### Helper for Migrating from Old Implementations

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

### Migration Steps

1. **Identify Existing Timeline Implementations**
   - `adaptive-timeline.js`
   - `timeline-component.js`
   - `timeline-integration.js`

2. **Create Feature Matrix**
   - Map features from each implementation
   - Identify overlapping functionality
   - Note unique capabilities

3. **Gradual Migration**
   - Replace one timeline at a time
   - Maintain backward compatibility
   - Test thoroughly after each replacement

4. **Deprecation Plan**
   - Mark old implementations as deprecated
   - Provide migration guide
   - Remove after validation period

## Implementation Checklist

- [ ] Create UnifiedTimeline class structure
- [ ] Implement segment rendering with boundaries
- [ ] Add thumbnail caching system
- [ ] Implement intelligent scaling algorithm
- [ ] Add touch gesture support
- [ ] Create migration helpers
- [ ] Test with existing player integration
- [ ] Document API differences

## CSS Requirements

```css
.timeline-segment {
    position: absolute;
    height: 100%;
    border-right: 1px solid rgba(255, 255, 255, 0.2);
    background: rgba(0, 123, 255, 0.1);
    transition: background 0.2s;
}

.timeline-segment.complete {
    background: rgba(40, 167, 69, 0.1);
}

.timeline-segment.editable {
    cursor: pointer;
}

.timeline-segment.editable:hover {
    background: rgba(0, 123, 255, 0.2);
}

.timeline-thumbnail.loaded {
    opacity: 1;
    transition: opacity 0.3s;
}
```

## Navigation

- [← Phase 1: Foundation Integration](01-phase1-foundation-integration.md)
- [Phase 3: Playback Enhancements →](03-phase3-playback-enhancements.md)