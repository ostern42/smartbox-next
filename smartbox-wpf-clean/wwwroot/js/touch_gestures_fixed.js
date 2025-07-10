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
            this.log('TouchGestureManagerFixed: mwlScrollContainer not found, trying mwlContainer...');
            const altContainer = document.getElementById('mwlContainer');
            if (altContainer) {
                this.setupPullToRefresh(altContainer, pullIndicator);
            }
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
                
                if (deltaX < -this.swipeThreshold) {
                    selectedType = 'female';
                } else if (deltaX < -this.swipeThreshold * 2) {
                    selectedType = 'child';
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
     * Enable/disable gestures
     */
    setEnabled(enabled) {
        this.isEnabled = enabled;
        this.log('TouchGestureManagerFixed: Gestures', enabled ? 'enabled' : 'disabled');
    }
}

// Replace the old manager
window.TouchGestureManager = TouchGestureManagerFixed;