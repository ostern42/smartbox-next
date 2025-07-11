/**
 * Touch Gestures Manager for SmartBox Next - FIXED VERSION
 * Robust touch/mouse support for WebView2 environment
 */

class TouchGestureManagerFixed {
    constructor() {
        this.isEnabled = true;
        this.pullThreshold = 80;
        this.swipeThreshold = 50;
        this.tapHoldThreshold = 500;
        this.activeGesture = null;
        this.tapHoldTimer = null;
        
        // Haptic feedback support
        this.hasHaptics = 'vibrate' in navigator;
        
        // Debug mode
        this.debug = true;
        
        this.log('TouchGestureManagerFixed: Starting...');
        this.initializeGestures();
    }

    log(message) {
        if (this.debug) {
            console.log(message);
        }
    }

    initializeGestures() {
        this.log('TouchGestureManagerFixed: Initializing all gestures...');
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                this.setupGestures();
            });
        } else {
            this.setupGestures();
        }
    }

    setupGestures() {
        this.log('TouchGestureManagerFixed: Setting up gestures...');
        
        // Pull-to-refresh on MWL
        this.initPullToRefreshFixed();
        
        // Emergency swipe area
        this.initEmergencySwipeFixed();
        
        // Capture area tap/hold
        this.initCaptureGestures();
        
        // Patient card selection (fallback)
        this.initPatientCardSelection();
        
        this.log('TouchGestureManagerFixed: All gestures initialized');
    }

    /**
     * Fixed Pull-to-refresh with both touch and mouse support
     */
    initPullToRefreshFixed() {
        const mwlContainer = document.getElementById('mwlScrollContainer');
        const pullIndicator = document.getElementById('pullToRefresh');
        
        if (!mwlContainer) {
            this.log('TouchGestureManagerFixed: mwlScrollContainer not found - cannot setup pull-to-refresh');
            return;
        }
        
        this.setupPullToRefresh(mwlContainer, pullIndicator);
    }

    setupPullToRefresh(container, indicator) {
        this.log('TouchGestureManagerFixed: Setting up pull-to-refresh on container');
        
        let isPulling = false;
        let startY = 0;
        let pullDistance = 0;

        // Touch events
        container.addEventListener('touchstart', (e) => {
            this.log('Touch start detected');
            if (container.scrollTop === 0) {
                startY = e.touches[0].clientY;
                isPulling = true;
                if (indicator) indicator.classList.remove('loading');
            }
        });

        container.addEventListener('touchmove', (e) => {
            if (!isPulling) return;
            
            const currentY = e.touches[0].clientY;
            pullDistance = Math.max(0, currentY - startY);
            
            if (pullDistance > 20) { // Start effect earlier
                e.preventDefault();
                
                if (indicator) {
                    indicator.style.transform = `translateY(${pullDistance}px)`;
                    indicator.classList.add('active');
                    
                    if (pullDistance >= this.pullThreshold) {
                        indicator.classList.add('pulling');
                        this.hapticFeedback('light');
                    } else {
                        indicator.classList.remove('pulling');
                    }
                }
                
                this.log(`Pull distance: ${pullDistance}px`);
            }
        });

        container.addEventListener('touchend', (e) => {
            if (!isPulling) return;
            
            isPulling = false;
            
            if (pullDistance >= this.pullThreshold) {
                this.log('Pull-to-refresh triggered!');
                this.triggerMWLRefresh();
                if (indicator) indicator.classList.add('loading');
                this.hapticFeedback('medium');
            }
            
            // Reset
            if (indicator) {
                indicator.style.transform = '';
                indicator.classList.remove('pulling', 'active');
            }
            pullDistance = 0;
        });

        // Mouse events as fallback
        container.addEventListener('mousedown', (e) => {
            if (container.scrollTop === 0) {
                startY = e.clientY;
                isPulling = true;
                this.log('Mouse pull started');
            }
        });

        document.addEventListener('mousemove', (e) => {
            if (!isPulling) return;
            
            pullDistance = Math.max(0, e.clientY - startY);
            
            if (pullDistance > 20) {
                if (indicator) {
                    indicator.style.transform = `translateY(${pullDistance}px)`;
                    indicator.classList.add('active');
                }
            }
        });

        document.addEventListener('mouseup', (e) => {
            if (!isPulling) return;
            
            isPulling = false;
            
            if (pullDistance >= this.pullThreshold) {
                this.log('Mouse pull-to-refresh triggered!');
                this.triggerMWLRefresh();
            }
            
            if (indicator) {
                indicator.style.transform = '';
                indicator.classList.remove('pulling', 'active');
            }
            pullDistance = 0;
        });
    }

    /**
     * Fixed Emergency swipe with better element detection
     */
    initEmergencySwipeFixed() {
        const emergencyArea = document.getElementById('emergencySwipe');
        
        if (!emergencyArea) {
            this.log('TouchGestureManagerFixed: Emergency swipe area not found');
            return;
        }
        
        this.log('TouchGestureManagerFixed: Setting up emergency swipe');
        
        let startX = 0;
        let currentX = 0;
        let isSwipeActive = false;

        // Add visual debug
        emergencyArea.style.border = '2px dashed orange';
        emergencyArea.style.minHeight = '60px';

        emergencyArea.addEventListener('touchstart', (e) => {
            this.log('Emergency touch start');
            startX = e.touches[0].clientX;
            currentX = startX;
            isSwipeActive = true;
            e.preventDefault();
        });

        emergencyArea.addEventListener('touchmove', (e) => {
            if (!isSwipeActive) return;
            
            currentX = e.touches[0].clientX;
            const deltaX = currentX - startX;
            
            this.log(`Emergency swipe delta: ${deltaX}px`);
            
            // Visual feedback
            emergencyArea.style.transform = `translateX(${deltaX * 0.3}px)`;
            e.preventDefault();
        });

        emergencyArea.addEventListener('touchend', (e) => {
            if (!isSwipeActive) return;
            
            isSwipeActive = false;
            emergencyArea.style.transform = '';
            
            const deltaX = currentX - startX;
            const absDelta = Math.abs(deltaX);
            
            this.log(`Emergency swipe ended, delta: ${deltaX}px`);
            
            if (absDelta > this.swipeThreshold) {
                let selectedType = 'male'; // default
                
                // Fix: Check longer swipes first!
                if (deltaX < -this.swipeThreshold * 2) {
                    selectedType = 'child';        // Long left swipe: child
                } else if (deltaX < -this.swipeThreshold) {
                    selectedType = 'female';       // Medium left swipe: female
                } else if (deltaX > this.swipeThreshold) {
                    selectedType = 'male';         // Right swipe: male (explicit)
                }
                
                this.log(`Emergency patient selected: ${selectedType}`);
                this.selectEmergencyPatient(selectedType);
                this.hapticFeedback('medium');
            }
        });

        // Mouse support
        emergencyArea.addEventListener('click', (e) => {
            this.log('Emergency area clicked - selecting default male patient');
            this.selectEmergencyPatient('male');
        });
    }

    /**
     * Capture area tap and hold gestures with mouse support
     */
    initCaptureGestures() {
        const captureArea = document.getElementById('captureArea');
        const touchFeedback = document.getElementById('touchFeedback');
        
        if (!captureArea) {
            this.log('TouchGestureManagerFixed: Capture area not found');
            return;
        }

        this.log('TouchGestureManagerFixed: Setting up capture gestures');

        // Touch events
        captureArea.addEventListener('touchstart', (e) => {
            e.preventDefault();
            this.log('Touch start on capture area');
            
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
                this.log('Hold completed - starting video recording');
                this.startVideoRecording();
                this.hapticFeedback('heavy');
            }, this.tapHoldThreshold);
            
            this.hapticFeedback('light');
        });

        captureArea.addEventListener('touchend', (e) => {
            e.preventDefault();
            this.log('Touch end on capture area');
            
            // Remove visual feedback
            if (touchFeedback) {
                touchFeedback.classList.remove('active');
            }
            
            if (this.tapHoldTimer) {
                clearTimeout(this.tapHoldTimer);
                this.tapHoldTimer = null;
                
                this.log('Quick tap - taking photo');
                // It was a tap - take photo
                this.capturePhoto();
                this.hapticFeedback('medium');
            } else {
                this.log('Hold was completed - stopping video');
                // Hold was completed - stop video
                this.stopVideoRecording();
            }
        });

        // CRITICAL: Mouse support for desktop testing
        captureArea.addEventListener('mousedown', (e) => {
            e.preventDefault();
            this.log('MOUSE DOWN detected on capture area!');
            
            // Show visual feedback at mouse point
            if (touchFeedback) {
                const rect = captureArea.getBoundingClientRect();
                touchFeedback.style.left = (e.clientX - rect.left - 50) + 'px';
                touchFeedback.style.top = (e.clientY - rect.top - 50) + 'px';
                touchFeedback.classList.add('active');
            }
            
            // Start tap/hold timer
            this.tapHoldTimer = setTimeout(() => {
                this.log('Mouse hold completed - starting video recording');
                this.startVideoRecording();
                this.hapticFeedback('heavy');
            }, this.tapHoldThreshold);
            
            this.hapticFeedback('light');
        });

        captureArea.addEventListener('mouseup', (e) => {
            e.preventDefault();
            this.log('MOUSE UP detected!');
            
            // Remove visual feedback
            if (touchFeedback) {
                touchFeedback.classList.remove('active');
            }
            
            if (this.tapHoldTimer) {
                clearTimeout(this.tapHoldTimer);
                this.tapHoldTimer = null;
                
                this.log('Quick MOUSE CLICK - taking photo');
                // It was a click - take photo
                this.capturePhoto();
                this.hapticFeedback('medium');
            } else {
                this.log('Mouse hold was completed - stopping video');
                // Hold was completed - stop video
                this.stopVideoRecording();
            }
        });
    }

    /**
     * Simple patient card selection as fallback
     */
    initPatientCardSelection() {
        document.addEventListener('click', (e) => {
            const patientCard = e.target.closest('.patient-card');
            if (patientCard) {
                this.log('Patient card clicked:', patientCard.dataset.patientId);
                
                // Visual feedback
                patientCard.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    patientCard.style.transform = '';
                }, 150);
                
                this.hapticFeedback('light');
            }
        });
    }

    /**
     * Trigger MWL refresh
     */
    triggerMWLRefresh() {
        this.log('TouchGestureManagerFixed: Triggering MWL refresh...');
        
        const pullIndicator = document.getElementById('pullToRefresh');
        if (pullIndicator) {
            pullIndicator.classList.add('active', 'loading');
            
            // Show visual feedback
            setTimeout(() => {
                pullIndicator.classList.remove('active', 'loading');
                this.log('MWL refresh animation complete');
            }, 2000);
        }
        
        // Emit event for app to handle
        this.emitEvent('mwlRefresh');
        
        // Also try to reload the demo data directly
        if (window.smartBoxApp && window.smartBoxApp.loadMWLData) {
            setTimeout(() => {
                window.smartBoxApp.loadMWLData();
            }, 500);
        }
    }

    /**
     * Select emergency patient
     */
    selectEmergencyPatient(type) {
        this.log(`TouchGestureManagerFixed: Emergency patient selected: ${type}`);
        
        // Show visual confirmation
        const emergencyArea = document.getElementById('emergencySwipe');
        if (emergencyArea) {
            emergencyArea.style.backgroundColor = '#28a745';
            emergencyArea.innerHTML = `<div style="padding: 20px; color: white; text-align: center; font-weight: bold;">Notfall ${type} ausgewählt!</div>`;
            
            setTimeout(() => {
                emergencyArea.style.backgroundColor = '';
                emergencyArea.innerHTML = `
                    <div class="emergency-options">
                        <div class="emergency-option" data-type="male">Mann</div>
                        <div class="emergency-option" data-type="female">Frau</div>
                        <div class="emergency-option" data-type="child">Kind</div>
                    </div>
                `;
            }, 2000);
        }
        
        this.emitEvent('emergencyPatientSelected', { type: type });
    }

    /**
     * Haptic feedback
     */
    hapticFeedback(intensity = 'light') {
        if (!this.hasHaptics) {
            this.log(`Haptic feedback: ${intensity} (not supported)`);
            return;
        }
        
        const patterns = {
            light: 10,
            medium: 50,
            heavy: 100
        };
        
        navigator.vibrate(patterns[intensity] || 10);
        this.log(`Haptic feedback: ${intensity}`);
    }

    /**
     * Emit custom events
     */
    emitEvent(eventName, data = {}) {
        this.log(`Emitting event: ${eventName}`, data);
        
        const event = new CustomEvent(eventName, { 
            detail: data,
            bubbles: true 
        });
        document.dispatchEvent(event);
    }

    /**
     * Capture photo
     */
    capturePhoto() {
        this.log('TouchGestureManagerFixed: Capturing photo...');
        this.emitEvent('capturePhoto');
    }

    /**
     * Start video recording
     */
    startVideoRecording() {
        this.log('TouchGestureManagerFixed: Starting video recording...');
        this.emitEvent('startVideoRecording');
    }

    /**
     * Stop video recording
     */
    stopVideoRecording() {
        this.log('TouchGestureManagerFixed: Stopping video recording...');
        this.emitEvent('stopVideoRecording');
    }

    /**
     * Enable/disable gestures
     */
    setEnabled(enabled) {
        this.isEnabled = enabled;
        this.log('TouchGestureManagerFixed: Gestures', enabled ? 'enabled' : 'disabled');
    }
}

// Replace the old manager
window.TouchGestureManager = TouchGestureManagerFixed;