/**
 * Touch Gestures Manager for SmartBox Next
 * Handles all touch interactions including pull-to-refresh, swipes, tap/hold
 */

class TouchGestureManager {
    constructor() {
        this.isEnabled = true;
        this.touchStart = null;
        this.touchCurrent = null;
        this.pullThreshold = 80;
        this.swipeThreshold = 50;
        this.tapHoldThreshold = 500;
        this.activeGesture = null;
        this.tapHoldTimer = null;
        
        // Haptic feedback support
        this.hasHaptics = 'vibrate' in navigator;
        
        // Initialize gesture handlers
        this.initializeGestures();
    }

    initializeGestures() {
        console.log('TouchGestureManager: Initializing gestures...');
        
        // Pull-to-refresh on MWL
        this.initPullToRefresh();
        
        // Emergency swipe area
        this.initEmergencySwipe();
        
        // Capture area tap/hold
        this.initCaptureGestures();
        
        // Thumbnail strip gestures
        this.initThumbnailGestures();
        
        console.log('TouchGestureManager: All gestures initialized');
    }

    /**
     * Pull-to-refresh implementation for MWL
     */
    initPullToRefresh() {
        const mwlContainer = document.getElementById('mwlScrollContainer');
        const pullIndicator = document.getElementById('pullToRefresh');
        let isPulling = false;
        let startY = 0;
        let pullDistance = 0;

        if (!mwlContainer || !pullIndicator) {
            console.warn('TouchGestureManager: Pull-to-refresh elements not found');
            return;
        }
        
        console.log('TouchGestureManager: Initializing pull-to-refresh');

        // Support both touch and mouse events
        const addEventListeners = (startEvent, moveEvent, endEvent) => {

        mwlContainer.addEventListener('touchstart', (e) => {
            if (mwlContainer.scrollTop === 0) {
                startY = e.touches[0].clientY;
                isPulling = true;
                pullIndicator.classList.remove('loading');
            }
        });

        mwlContainer.addEventListener('touchmove', (e) => {
            if (!isPulling) return;
            
            const currentY = e.touches[0].clientY;
            pullDistance = Math.max(0, currentY - startY);
            
            if (pullDistance > 0) {
                e.preventDefault(); // Prevent scroll
                
                // Update pull indicator
                const progress = Math.min(pullDistance / this.pullThreshold, 1);
                pullIndicator.style.transform = `translateY(${pullDistance}px)`;
                
                if (pullDistance >= this.pullThreshold) {
                    pullIndicator.classList.add('pulling');
                    this.hapticFeedback('light');
                } else {
                    pullIndicator.classList.remove('pulling');
                }
            }
        });

        mwlContainer.addEventListener('touchend', (e) => {
            if (!isPulling) return;
            
            isPulling = false;
            
            if (pullDistance >= this.pullThreshold) {
                // Trigger refresh
                this.triggerMWLRefresh();
                pullIndicator.classList.add('loading');
                this.hapticFeedback('medium');
            }
            
            // Reset pull indicator
            pullIndicator.style.transform = '';
            pullIndicator.classList.remove('pulling');
            pullDistance = 0;
        });
    }

    /**
     * Emergency patient swipe implementation
     */
    initEmergencySwipe() {
        const emergencyArea = document.getElementById('emergencySwipe');
        const emergencyOptions = emergencyArea?.querySelector('.emergency-options');
        
        if (!emergencyArea || !emergencyOptions) return;

        let startX = 0;
        let currentX = 0;
        let isSwipeActive = false;
        const optionWidth = emergencyArea.offsetWidth / 3;

        emergencyArea.addEventListener('touchstart', (e) => {
            startX = e.touches[0].clientX;
            currentX = startX;
            isSwipeActive = true;
            emergencyOptions.style.transition = 'none';
        });

        emergencyArea.addEventListener('touchmove', (e) => {
            if (!isSwipeActive) return;
            
            currentX = e.touches[0].clientX;
            const deltaX = currentX - startX;
            const maxSwipe = optionWidth * 2; // Can swipe 2 sections
            const constrainedDelta = Math.max(-maxSwipe, Math.min(0, deltaX));
            
            emergencyOptions.style.transform = `translateX(${constrainedDelta}px)`;
            e.preventDefault();
        });

        emergencyArea.addEventListener('touchend', (e) => {
            if (!isSwipeActive) return;
            
            isSwipeActive = false;
            emergencyOptions.style.transition = '';
            
            const deltaX = currentX - startX;
            const absorbedSwipe = Math.abs(deltaX);
            
            if (absorbedSwipe > this.swipeThreshold) {
                // Determine which option to select
                let selectedIndex = 0;
                if (deltaX < -optionWidth * 1.5) {
                    selectedIndex = 2; // Child
                } else if (deltaX < -optionWidth * 0.5) {
                    selectedIndex = 1; // Female
                }
                
                this.selectEmergencyPatient(selectedIndex);
                this.hapticFeedback('medium');
            }
            
            // Snap back to center
            emergencyOptions.style.transform = '';
        });
    }

    /**
     * Capture area tap and hold gestures
     */
    initCaptureGestures() {
        const captureArea = document.getElementById('captureArea');
        const touchFeedback = document.getElementById('touchFeedback');
        
        if (!captureArea) return;

        captureArea.addEventListener('touchstart', (e) => {
            e.preventDefault();
            
            const touch = e.touches[0];
            const rect = captureArea.getBoundingClientRect();
            
            // Show visual feedback at touch point
            if (touchFeedback) {
                touchFeedback.style.left = (touch.clientX - rect.left - 50) + 'px';
                touchFeedback.style.top = (touch.clientY - rect.top - 50) + 'px';
                touchFeedback.classList.add('active');
            }
            
            // Start tap/hold timer
            this.tapHoldTimer = setTimeout(() => {
                this.startVideoRecording();
                this.hapticFeedback('heavy');
            }, this.tapHoldThreshold);
            
            this.hapticFeedback('light');
        });

        captureArea.addEventListener('touchend', (e) => {
            e.preventDefault();
            
            // Remove visual feedback
            if (touchFeedback) {
                touchFeedback.classList.remove('active');
            }
            
            if (this.tapHoldTimer) {
                clearTimeout(this.tapHoldTimer);
                this.tapHoldTimer = null;
                
                // It was a tap - take photo
                this.capturePhoto();
                this.hapticFeedback('medium');
            } else {
                // Hold was completed - stop video
                this.stopVideoRecording();
            }
        });

        captureArea.addEventListener('touchcancel', (e) => {
            if (touchFeedback) {
                touchFeedback.classList.remove('active');
            }
            
            if (this.tapHoldTimer) {
                clearTimeout(this.tapHoldTimer);
                this.tapHoldTimer = null;
            }
        });
    }

    /**
     * Thumbnail strip horizontal scrolling and delete gestures
     */
    initThumbnailGestures() {
        const thumbnailScroll = document.getElementById('thumbnailScroll');
        
        if (!thumbnailScroll) return;

        // Enable smooth horizontal scrolling
        thumbnailScroll.addEventListener('touchstart', (e) => {
            thumbnailScroll.style.scrollBehavior = 'auto';
        });

        thumbnailScroll.addEventListener('touchend', (e) => {
            thumbnailScroll.style.scrollBehavior = 'smooth';
        });

        // Delete gesture on individual thumbnails
        this.initThumbnailDeleteGesture();
    }

    /**
     * Vertical swipe up on thumbnail to delete
     */
    initThumbnailDeleteGesture() {
        document.addEventListener('touchstart', (e) => {
            const thumbnail = e.target.closest('.thumbnail');
            if (!thumbnail || thumbnail.classList.contains('add-new')) return;

            let startY = e.touches[0].clientY;
            let startTime = Date.now();
            let hasMoved = false;

            const handleMove = (moveEvent) => {
                const deltaY = moveEvent.touches[0].clientY - startY;
                hasMoved = true;
                
                if (deltaY < -this.swipeThreshold) {
                    // Swipe up detected
                    thumbnail.style.transform = `translateY(${deltaY}px)`;
                    thumbnail.style.opacity = Math.max(0.3, 1 + deltaY / 100);
                }
            };

            const handleEnd = (endEvent) => {
                document.removeEventListener('touchmove', handleMove);
                document.removeEventListener('touchend', handleEnd);
                
                const deltaY = endEvent.changedTouches[0].clientY - startY;
                const duration = Date.now() - startTime;
                
                if (deltaY < -this.swipeThreshold && duration < 1000) {
                    // Confirm delete
                    const index = thumbnail.dataset.index;
                    this.confirmDeleteThumbnail(index, thumbnail);
                    this.hapticFeedback('heavy');
                } else {
                    // Reset thumbnail position
                    thumbnail.style.transform = '';
                    thumbnail.style.opacity = '';
                }
            };

            document.addEventListener('touchmove', handleMove);
            document.addEventListener('touchend', handleEnd);
        });
    }

    /**
     * Haptic feedback wrapper
     */
    hapticFeedback(intensity = 'light') {
        if (!this.hasHaptics) return;
        
        const patterns = {
            light: 10,
            medium: 50,
            heavy: 100
        };
        
        navigator.vibrate(patterns[intensity] || 10);
    }

    /**
     * Trigger MWL refresh
     */
    triggerMWLRefresh() {
        console.log('TouchGestureManager: Triggering MWL refresh...');
        
        // Show loading state
        const pullIndicator = document.getElementById('pullToRefresh');
        if (pullIndicator) {
            pullIndicator.classList.add('active', 'loading');
        }
        
        // Simulate refresh (replace with actual MWL reload)
        setTimeout(() => {
            if (pullIndicator) {
                pullIndicator.classList.remove('active', 'loading');
            }
            this.onMWLRefreshed();
        }, 2000);
        
        // Emit event for app to handle
        this.emitEvent('mwlRefresh');
    }

    /**
     * Select emergency patient type
     */
    selectEmergencyPatient(index) {
        const types = ['male', 'female', 'child'];
        const selectedType = types[index] || 'male';
        
        console.log('TouchGestureManager: Emergency patient selected:', selectedType);
        
        this.emitEvent('emergencyPatientSelected', { type: selectedType });
    }

    /**
     * Capture photo
     */
    capturePhoto() {
        console.log('TouchGestureManager: Capturing photo...');
        this.emitEvent('capturePhoto');
    }

    /**
     * Start video recording
     */
    startVideoRecording() {
        console.log('TouchGestureManager: Starting video recording...');
        this.emitEvent('startVideoRecording');
    }

    /**
     * Stop video recording
     */
    stopVideoRecording() {
        console.log('TouchGestureManager: Stopping video recording...');
        this.emitEvent('stopVideoRecording');
    }

    /**
     * Confirm thumbnail deletion
     */
    confirmDeleteThumbnail(index, thumbnailElement) {
        console.log('TouchGestureManager: Confirming delete for thumbnail:', index);
        
        // Show confirmation dialog
        this.emitEvent('confirmDeleteThumbnail', { 
            index: index, 
            element: thumbnailElement 
        });
    }

    /**
     * MWL refresh completed
     */
    onMWLRefreshed() {
        console.log('TouchGestureManager: MWL refresh completed');
        this.emitEvent('mwlRefreshed');
    }

    /**
     * Emit custom events for the main app to handle
     */
    emitEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, { 
            detail: data,
            bubbles: true 
        });
        document.dispatchEvent(event);
    }

    /**
     * Enable/disable gesture handling
     */
    setEnabled(enabled) {
        this.isEnabled = enabled;
        console.log('TouchGestureManager: Gestures', enabled ? 'enabled' : 'disabled');
    }

    /**
     * Cleanup method
     */
    destroy() {
        if (this.tapHoldTimer) {
            clearTimeout(this.tapHoldTimer);
        }
        
        console.log('TouchGestureManager: Destroyed');
    }
}

// Export for use in main app
window.TouchGestureManager = TouchGestureManager;