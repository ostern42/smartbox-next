/**
 * SmartBox Button Action System
 * Session 87 Safe - Never guess property names!
 * 
 * This system provides property-safe button action mapping
 * with explicit verification of DOM properties before use.
 */

console.log('[ButtonActions] Loading property-safe button system...');

// VERIFIED PROPERTY MAPPINGS (Session 87 Prevention)
const VERIFIED_PROPERTIES = {
    // Video element properties (verified from MDN)
    VIDEO: {
        WIDTH: 'videoWidth',      // NOT 'width' - that was the Session 87 trauma!
        HEIGHT: 'videoHeight',    // NOT 'height'
        CURRENT_TIME: 'currentTime',
        DURATION: 'duration',
        PAUSED: 'paused',
        MUTED: 'muted',
        SRC_OBJECT: 'srcObject'
    },
    // Canvas properties
    CANVAS: {
        WIDTH: 'width',          // Canvas uses 'width', not 'videoWidth'
        HEIGHT: 'height'         // Canvas uses 'height', not 'videoHeight'
    },
    // Element properties
    ELEMENT: {
        CLASS_LIST: 'classList',
        DATASET: 'dataset',
        ID: 'id',
        INNER_TEXT: 'innerText',
        TEXT_CONTENT: 'textContent',
        STYLE: 'style'
    }
};

// Button action configuration with explicit mapping
const BUTTON_CONFIG = {
    // MWL Actions
    'refreshMWL': {
        action: 'refreshmwl',
        handler: 'mwlRefresh',
        requiresConfirm: false,
        icon: 'ms-Icon--Refresh'
    },
    
    // Settings and App Control
    'settingsButton': {
        action: 'opensettings',
        handler: 'openSettings',
        requiresConfirm: false,
        icon: '⚙'
    },
    
    'exitButton': {
        action: 'exitapp',
        handler: 'exitApp',
        requiresConfirm: false  // Dialog is handled by mode_manager
    },
    
    // Navigation
    'backToPatientSelection': {
        action: 'goback',
        handler: 'backToPatientSelection',
        requiresConfirm: false,
        icon: 'ms-Icon--Back'
    },
    
    // Capture Actions
    'capturePhotoButton': {
        action: 'capturephoto',
        handler: 'capturePhoto',
        requiresConfirm: false,
        icon: 'ms-Icon--Camera'
    },
    
    'toggleVideoButton': {
        action: 'togglevideo',
        handler: 'toggleVideoRecording',
        requiresConfirm: false,
        icon: 'ms-Icon--Video',
        toggleStates: {
            start: { icon: 'ms-Icon--Video', text: 'Video starten' },
            stop: { icon: 'ms-Icon--StopSolid', text: 'Video stoppen' }
        }
    },
    
    'markCriticalMomentButton': {
        action: 'markcritical',
        handler: 'markCriticalMoment',
        requiresConfirm: false,
        icon: 'ms-Icon--Flag',
        requiresActiveRecording: true
    },
    
    // Export
    'exportButton': {
        action: 'exportcaptures',
        handler: 'exportCaptures',
        requiresConfirm: false,
        icon: 'ms-Icon--Export'
    },
    
    // Emergency Patients
    'emergencyMale': {
        action: 'createemergencypatient',
        handler: 'createEmergencyPatient',
        data: { type: 'male' },
        requiresConfirm: false
    },
    
    'emergencyFemale': {
        action: 'createemergencypatient',
        handler: 'createEmergencyPatient',
        data: { type: 'female' },
        requiresConfirm: false
    },
    
    'emergencyChild': {
        action: 'createemergencypatient',
        handler: 'createEmergencyPatient',
        data: { type: 'child' },
        requiresConfirm: false
    }
};

/**
 * Button Action Manager - Property Safe Implementation
 */
class ButtonActionManager {
    constructor() {
        this.isRecording = false;
        this.recordingStartTime = null;
        this.criticalMoments = [];
        this.verifiedProperties = new Map();
        
        // Initialize after DOM ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.initialize());
        } else {
            this.initialize();
        }
    }
    
    initialize() {
        console.log('[ButtonActions] Initializing property-safe button system...');
        
        // Verify properties first!
        this.verifyDOMProperties();
        
        // Register all configured buttons
        this.registerAllButtons();
        
        // Set up global click handler for data-action buttons
        this.setupGlobalHandler();
        
        console.log('[ButtonActions] Button action system initialized');
    }
    
    /**
     * Verify DOM properties exist before using them
     * Session 87 Prevention Protocol!
     */
    verifyDOMProperties() {
        console.log('[ButtonActions] Verifying DOM properties...');
        
        // Test video element properties
        const testVideo = document.createElement('video');
        for (const [key, prop] of Object.entries(VERIFIED_PROPERTIES.VIDEO)) {
            if (prop in testVideo) {
                this.verifiedProperties.set(`video.${prop}`, true);
                console.log(`[ButtonActions] ✓ Video property verified: ${prop}`);
            } else {
                console.error(`[ButtonActions] ✗ Video property NOT found: ${prop}`);
            }
        }
        
        // Test canvas properties
        const testCanvas = document.createElement('canvas');
        for (const [key, prop] of Object.entries(VERIFIED_PROPERTIES.CANVAS)) {
            if (prop in testCanvas) {
                this.verifiedProperties.set(`canvas.${prop}`, true);
                console.log(`[ButtonActions] ✓ Canvas property verified: ${prop}`);
            }
        }
        
        // Clean up test elements
        testVideo.remove();
        testCanvas.remove();
    }
    
    /**
     * Safely get property value with verification
     */
    getVerifiedProperty(element, propertyPath) {
        const elementType = element.tagName.toLowerCase();
        const fullPath = `${elementType}.${propertyPath}`;
        
        if (!this.verifiedProperties.has(fullPath)) {
            console.error(`[ButtonActions] Property not verified: ${fullPath}`);
            return null;
        }
        
        return element[propertyPath];
    }
    
    /**
     * Register all buttons from configuration
     */
    registerAllButtons() {
        for (const [buttonId, config] of Object.entries(BUTTON_CONFIG)) {
            this.registerButton(buttonId, config);
        }
    }
    
    /**
     * Register a single button with its action
     */
    registerButton(buttonId, config) {
        const button = document.getElementById(buttonId);
        if (!button) {
            console.warn(`[ButtonActions] Button not found: ${buttonId}`);
            return;
        }
        
        // Store config in button dataset for retrieval
        button.dataset.buttonConfig = JSON.stringify(config);
        
        // Add click handler
        button.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.executeButtonAction(buttonId, config, button);
        });
        
        console.log(`[ButtonActions] Registered: ${buttonId} → ${config.action}`);
    }
    
    /**
     * Set up global handler for dynamic buttons
     */
    setupGlobalHandler() {
        document.addEventListener('click', (e) => {
            const button = e.target.closest('[data-action]');
            if (!button) return;
            
            // Skip if already handled by specific registration
            if (button.dataset.buttonConfig) return;
            
            const action = button.dataset.action;
            const config = this.findConfigByAction(action);
            
            if (config) {
                e.preventDefault();
                e.stopPropagation();
                this.executeButtonAction(button.id || 'dynamic', config, button);
            }
        });
    }
    
    /**
     * Find config by action name
     */
    findConfigByAction(action) {
        for (const config of Object.values(BUTTON_CONFIG)) {
            if (config.action === action) {
                return config;
            }
        }
        return null;
    }
    
    /**
     * Execute button action with proper handling
     */
    executeButtonAction(buttonId, config, buttonElement) {
        console.log(`[ButtonActions] Executing: ${buttonId} → ${config.action}`);
        
        // Check prerequisites
        if (config.requiresActiveRecording && !this.isRecording) {
            this.showNotification('Bitte starten Sie zuerst die Videoaufnahme', 'warning');
            return;
        }
        
        // Handle confirmation if required
        if (config.requiresConfirm) {
            this.confirmAndExecute(config, buttonElement);
        } else {
            this.executeAction(config, buttonElement);
        }
    }
    
    /**
     * Show confirmation dialog before executing
     */
    confirmAndExecute(config, buttonElement) {
        const message = config.confirmMessage || 'Sind Sie sicher?';
        
        if (window.touchDialogManager) {
            window.touchDialogManager.showConfirmation({
                title: 'Bestätigung',
                message: message,
                confirmText: 'Ja',
                cancelText: 'Abbrechen',
                onConfirm: () => this.executeAction(config, buttonElement)
            });
        } else if (confirm(message)) {
            this.executeAction(config, buttonElement);
        }
    }
    
    /**
     * Execute the actual action
     */
    executeAction(config, buttonElement) {
        // Collect any additional data
        const actionData = {
            ...config.data,
            timestamp: new Date().toISOString()
        };
        
        // Handle special actions internally
        switch (config.handler) {
            case 'toggleVideoRecording':
                this.handleVideoToggle(buttonElement);
                break;
                
            case 'markCriticalMoment':
                this.handleMarkCriticalMoment();
                break;
                
            case 'capturePhoto':
                this.handleCapturePhoto();
                break;
                
            case 'openSettings':
                this.handleOpenSettings();
                break;
                
            case 'exitApp':
                this.handleExitApp();
                break;
                
            default:
                // Send to host or trigger app event
                this.sendAction(config.action, actionData);
        }
    }
    
    /**
     * Handle video recording toggle
     */
    handleVideoToggle(buttonElement) {
        if (this.isRecording) {
            // Stop recording
            this.isRecording = false;
            this.recordingStartTime = null;
            
            // Update button state
            if (buttonElement && BUTTON_CONFIG.toggleVideoButton.toggleStates) {
                const startState = BUTTON_CONFIG.toggleVideoButton.toggleStates.start;
                this.updateButtonState(buttonElement, startState);
            }
            
            // Trigger stop event
            document.dispatchEvent(new CustomEvent('stopVideoRecording'));
            console.log('[ButtonActions] Video recording stopped');
            
        } else {
            // Start recording
            this.isRecording = true;
            this.recordingStartTime = Date.now();
            this.criticalMoments = [];
            
            // Update button state
            if (buttonElement && BUTTON_CONFIG.toggleVideoButton.toggleStates) {
                const stopState = BUTTON_CONFIG.toggleVideoButton.toggleStates.stop;
                this.updateButtonState(buttonElement, stopState);
            }
            
            // Trigger start event
            document.dispatchEvent(new CustomEvent('startVideoRecording'));
            console.log('[ButtonActions] Video recording started');
        }
    }
    
    /**
     * Update button visual state
     */
    updateButtonState(button, state) {
        // Update icon
        const icon = button.querySelector('i');
        if (icon && state.icon) {
            icon.className = `ms-Icon ${state.icon}`;
        }
        
        // Update text
        const text = button.querySelector('span');
        if (text && state.text) {
            text.textContent = state.text;
        }
    }
    
    /**
     * Handle marking critical moment during recording
     */
    handleMarkCriticalMoment() {
        if (!this.isRecording || !this.recordingStartTime) {
            console.warn('[ButtonActions] Cannot mark critical moment - not recording');
            return;
        }
        
        const timestamp = Date.now() - this.recordingStartTime;
        const moment = {
            timestamp: timestamp,
            timeInSeconds: Math.floor(timestamp / 1000),
            note: `Critical moment at ${this.formatTime(timestamp)}`
        };
        
        this.criticalMoments.push(moment);
        console.log('[ButtonActions] Critical moment marked:', moment);
        
        // Visual feedback
        this.showNotification(`Kritischer Moment markiert: ${moment.note}`, 'success');
        
        // Send to app
        document.dispatchEvent(new CustomEvent('criticalMomentMarked', { 
            detail: moment 
        }));
    }
    
    /**
     * Handle photo capture with property-safe canvas operations
     */
    handleCapturePhoto() {
        const video = document.getElementById('webcamPreviewLarge');
        const canvas = document.getElementById('captureCanvas');
        
        if (!video || !canvas) {
            console.error('[ButtonActions] Video or canvas element not found');
            return;
        }
        
        // Use VERIFIED properties only!
        const videoWidth = this.getVerifiedProperty(video, VERIFIED_PROPERTIES.VIDEO.WIDTH);
        const videoHeight = this.getVerifiedProperty(video, VERIFIED_PROPERTIES.VIDEO.HEIGHT);
        
        if (!videoWidth || !videoHeight) {
            console.error('[ButtonActions] Could not get video dimensions safely');
            return;
        }
        
        // Set canvas size using verified properties
        canvas[VERIFIED_PROPERTIES.CANVAS.WIDTH] = videoWidth;
        canvas[VERIFIED_PROPERTIES.CANVAS.HEIGHT] = videoHeight;
        
        console.log(`[ButtonActions] Canvas sized to: ${videoWidth}x${videoHeight}`);
        
        // Trigger photo capture event
        document.dispatchEvent(new CustomEvent('capturePhoto'));
    }
    
    /**
     * Handle settings button
     */
    handleOpenSettings() {
        console.log('[ButtonActions] Opening settings...');
        if (window.smartBoxApp && window.smartBoxApp.openSettings) {
            window.smartBoxApp.openSettings();
        } else {
            console.warn('[ButtonActions] smartBoxApp.openSettings not available');
        }
    }
    
    /**
     * Handle exit button
     */
    handleExitApp() {
        console.log('[ButtonActions] Exit app requested...');
        if (window.smartBoxApp && window.smartBoxApp.onExitRequested) {
            window.smartBoxApp.onExitRequested();
        } else {
            console.warn('[ButtonActions] smartBoxApp.onExitRequested not available');
        }
    }
    
    /**
     * Send action to host or app
     */
    sendAction(action, data) {
        // Try WebView2 first
        if (window.chrome?.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: action,
                data: data
            }));
        } else {
            // Fallback to app handler
            if (window.smartBoxApp && window.smartBoxApp[action]) {
                window.smartBoxApp[action](data);
            } else {
                console.warn(`[ButtonActions] No handler for action: ${action}`);
            }
        }
    }
    
    /**
     * Show notification to user
     */
    showNotification(message, type = 'info') {
        if (window.touchDialogManager) {
            window.touchDialogManager.showToast(message, type);
        } else {
            console.log(`[ButtonActions] ${type}: ${message}`);
        }
    }
    
    /**
     * Format time for display
     */
    formatTime(milliseconds) {
        const totalSeconds = Math.floor(milliseconds / 1000);
        const minutes = Math.floor(totalSeconds / 60);
        const seconds = totalSeconds % 60;
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }
    
    /**
     * Get current recording state
     */
    getRecordingState() {
        return {
            isRecording: this.isRecording,
            duration: this.isRecording ? Date.now() - this.recordingStartTime : 0,
            criticalMoments: this.criticalMoments
        };
    }
    
    /**
     * Debug: List all registered buttons
     */
    listRegisteredButtons() {
        console.log('[ButtonActions] Registered buttons:');
        for (const [buttonId, config] of Object.entries(BUTTON_CONFIG)) {
            const element = document.getElementById(buttonId);
            const status = element ? '✓' : '✗';
            console.log(`  ${status} ${buttonId} → ${config.action}`);
        }
    }
}

// Create global instance
window.buttonActionManager = new ButtonActionManager();

// Export for testing
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ButtonActionManager, BUTTON_CONFIG, VERIFIED_PROPERTIES };
}

console.log('[ButtonActions] Property-safe button system loaded - Session 87 trauma prevented!');