/**
 * Mode Manager for SmartBox Next Touch Interface
 * Handles transitions between Patient Selection and Recording modes
 */

class ModeManager {
    constructor() {
        this.currentMode = 'patient'; // 'patient' | 'recording'
        this.currentPatient = null;
        this.captures = [];
        this.isTransitioning = false;
        
        // Get mode containers
        this.patientMode = document.getElementById('patientMode');
        this.recordingMode = document.getElementById('recordingMode');
        this.patientStatus = document.getElementById('patientStatus');
        this.patientStatusText = document.getElementById('patientStatusText');
        
        // Get recording mode elements
        this.recordingPatientName = document.getElementById('recordingPatientName');
        this.recordingPatientId = document.getElementById('recordingPatientId');
        this.recordingStudyType = document.getElementById('recordingStudyType');
        
        this.initializeModeManager();
    }

    initializeModeManager() {
        console.log('ModeManager: Initializing...');
        
        // Ensure we start in patient mode
        this.switchToPatientMode();
        
        // Listen for patient selection events
        document.addEventListener('patientSelected', (e) => {
            this.onPatientSelected(e.detail);
        });
        
        // Listen for emergency patient events
        document.addEventListener('emergencyPatientSelected', (e) => {
            this.onEmergencyPatientSelected(e.detail);
        });
        
        // Listen for session end events
        document.addEventListener('endSession', () => {
            this.onEndSession();
        });
        
        // Listen for exit button
        const exitButton = document.getElementById('exitButton');
        if (exitButton) {
            exitButton.addEventListener('click', () => {
                this.onExitRequested();
            });
        }
        
        console.log('ModeManager: Initialized in', this.currentMode, 'mode');
    }

    /**
     * Switch to patient selection mode
     */
    switchToPatientMode() {
        if (this.isTransitioning) return;
        
        console.log('ModeManager: Switching to patient mode');
        this.isTransitioning = true;
        
        // Update current mode
        this.currentMode = 'patient';
        
        // Show patient mode, hide recording mode
        this.patientMode.classList.remove('hidden');
        this.recordingMode.classList.add('hidden');
        
        // Update patient status
        this.updatePatientStatus('Kein Patient');
        
        // Clear current patient
        this.currentPatient = null;
        
        // Reset captures
        this.captures = [];
        
        // Stop any ongoing recording
        this.stopVideoRecording();
        
        // Focus on patient selection
        setTimeout(() => {
            const filterInput = document.getElementById('mwlFilter');
            if (filterInput) {
                filterInput.focus();
            }
            this.isTransitioning = false;
        }, 250);
        
        // Emit mode change event
        this.emitModeChange('patient');
    }

    /**
     * Switch to recording mode
     */
    switchToRecordingMode() {
        if (this.isTransitioning || !this.currentPatient) return;
        
        console.log('ModeManager: Switching to recording mode');
        this.isTransitioning = true;
        
        // Update current mode
        this.currentMode = 'recording';
        
        // Hide patient mode, show recording mode
        this.patientMode.classList.add('hidden');
        this.recordingMode.classList.remove('hidden');
        
        // Update recording mode patient info
        this.updateRecordingPatientInfo();
        
        // Initialize webcam for recording
        this.initializeRecordingWebcam();
        
        // Update patient status
        this.updatePatientStatus(this.currentPatient.name);
        
        setTimeout(() => {
            this.isTransitioning = false;
        }, 250);
        
        // Emit mode change event
        this.emitModeChange('recording');
    }

    /**
     * Handle patient selection from MWL
     */
    onPatientSelected(patientData) {
        console.log('ModeManager: Patient selected:', patientData);
        
        this.currentPatient = {
            id: patientData.id,
            name: patientData.name,
            birthDate: patientData.birthDate,
            studyType: patientData.studyType,
            accessionNumber: patientData.accessionNumber,
            studyDescription: patientData.studyDescription,
            isEmergency: false
        };
        
        // Switch to recording mode
        this.switchToRecordingMode();
    }

    /**
     * Handle emergency patient selection
     */
    onEmergencyPatientSelected(emergencyData) {
        console.log('ModeManager: Emergency patient selected:', emergencyData.type);
        
        // Create emergency patient data
        const emergencyPatients = {
            male: {
                id: 'EMRG-M-' + Date.now(),
                name: 'Notfall, Mann',
                birthDate: '01.01.1970',
                studyType: 'Notfall',
                accessionNumber: 'EMRG-' + Date.now(),
                studyDescription: 'Notfall-Untersuchung',
                isEmergency: true,
                gender: 'M'
            },
            female: {
                id: 'EMRG-F-' + Date.now(),
                name: 'Notfall, Frau',
                birthDate: '01.01.1970',
                studyType: 'Notfall',
                accessionNumber: 'EMRG-' + Date.now(),
                studyDescription: 'Notfall-Untersuchung',
                isEmergency: true,
                gender: 'F'
            },
            child: {
                id: 'EMRG-C-' + Date.now(),
                name: 'Notfall, Kind',
                birthDate: '01.01.2010',
                studyType: 'Notfall',
                accessionNumber: 'EMRG-' + Date.now(),
                studyDescription: 'Notfall-Untersuchung',
                isEmergency: true,
                gender: 'O'
            }
        };
        
        this.currentPatient = emergencyPatients[emergencyData.type];
        
        // Switch to recording mode immediately
        this.switchToRecordingMode();
    }

    /**
     * Handle session end request
     */
    onEndSession() {
        if (this.currentMode === 'patient') {
            // No session to end
            return;
        }
        
        console.log('ModeManager: Ending session for patient:', this.currentPatient?.name);
        
        // Check for unsaved captures
        const unsavedCaptures = this.captures.filter(c => !c.exported).length;
        
        if (unsavedCaptures > 0) {
            // Show warning dialog
            const dialogManager = window.touchDialogManager;
            if (dialogManager) {
                dialogManager.confirmEndSession(unsavedCaptures).then((confirmed) => {
                    if (confirmed) {
                        this.switchToPatientMode();
                    }
                });
            }
        } else {
            // Safe to end session
            this.switchToPatientMode();
        }
    }

    /**
     * Handle app exit request
     */
    onExitRequested() {
        console.log('ModeManager: Exit requested');
        
        const dialogManager = window.touchDialogManager;
        if (!dialogManager) {
            this.exitApp();
            return;
        }
        
        // Check current state
        if (this.currentMode === 'recording') {
            const unsavedCaptures = this.captures.filter(c => !c.exported).length;
            
            if (unsavedCaptures > 0) {
                dialogManager.showConfirmation({
                    title: 'Anwendung beenden?',
                    message: unsavedCaptures === 1
                        ? 'Eine Aufnahme wurde noch nicht exportiert!'
                        : `${unsavedCaptures} Aufnahmen wurden noch nicht exportiert!`,
                    cancelText: 'Abbrechen',
                    confirmText: 'Trotzdem beenden',
                    confirmStyle: 'danger',
                    onConfirm: () => this.exitApp(),
                    onCancel: () => {} // Do nothing
                });
                return;
            }
        }
        
        // Standard exit confirmation
        dialogManager.showConfirmation({
            title: 'Beenden',
            message: 'SmartBox Next wirklich beenden?',
            cancelText: 'Abbrechen',
            confirmText: 'Beenden',
            onConfirm: () => this.exitApp(),
            onCancel: () => {} // Do nothing
        });
    }

    /**
     * Update patient status display
     */
    updatePatientStatus(statusText) {
        if (this.patientStatusText) {
            this.patientStatusText.textContent = statusText;
        }
        
        // Update icon based on status
        const icon = this.patientStatus?.querySelector('i');
        if (icon) {
            if (statusText === 'Kein Patient') {
                icon.className = 'ms-Icon ms-Icon--Contact';
            } else {
                icon.className = 'ms-Icon ms-Icon--ContactCard';
            }
        }
    }

    /**
     * Update recording mode patient information
     */
    updateRecordingPatientInfo() {
        if (!this.currentPatient) return;
        
        if (this.recordingPatientName) {
            this.recordingPatientName.textContent = this.currentPatient.name;
        }
        
        if (this.recordingPatientId) {
            this.recordingPatientId.textContent = this.currentPatient.id;
        }
        
        if (this.recordingStudyType) {
            this.recordingStudyType.textContent = this.currentPatient.studyType;
        }
    }

    /**
     * Initialize webcam for recording mode
     */
    initializeRecordingWebcam() {
        const video = document.getElementById('webcamPreviewLarge');
        if (!video) return;
        
        // This would be handled by the main app's webcam manager
        console.log('ModeManager: Requesting webcam initialization for recording mode');
        
        // Emit event for main app
        this.emitEvent('initializeRecordingWebcam', { videoElement: video });
    }

    /**
     * Stop video recording
     */
    stopVideoRecording() {
        console.log('ModeManager: Stopping any active video recording');
        this.emitEvent('stopVideoRecording');
    }

    /**
     * Add capture to the session
     */
    addCapture(captureData) {
        const capture = {
            id: Date.now(),
            type: captureData.type, // 'photo' | 'video'
            timestamp: new Date(),
            thumbnail: captureData.thumbnail,
            data: captureData.data,
            exported: false,
            duration: captureData.duration || null
        };
        
        this.captures.push(capture);
        
        console.log('ModeManager: Added capture:', capture.type, 'Total captures:', this.captures.length);
        
        // Update export button
        this.updateExportButton();
        
        // Update thumbnail strip
        this.updateThumbnailStrip();
        
        return capture.id;
    }

    /**
     * Remove capture from session
     */
    removeCapture(captureId) {
        const index = this.captures.findIndex(c => c.id === captureId);
        if (index !== -1) {
            this.captures.splice(index, 1);
            console.log('ModeManager: Removed capture:', captureId);
            
            this.updateExportButton();
            this.updateThumbnailStrip();
        }
    }

    /**
     * Mark captures as exported
     */
    markCapturesExported(captureIds = null) {
        const idsToMark = captureIds || this.captures.map(c => c.id);
        
        idsToMark.forEach(id => {
            const capture = this.captures.find(c => c.id === id);
            if (capture) {
                capture.exported = true;
            }
        });
        
        console.log('ModeManager: Marked captures as exported:', idsToMark.length);
        this.updateExportButton();
    }

    /**
     * Update export button state
     */
    updateExportButton() {
        const exportButton = document.getElementById('exportButton');
        const exportCount = document.getElementById('exportCount');
        
        if (exportButton && exportCount) {
            const totalCaptures = this.captures.length;
            const unexportedCaptures = this.captures.filter(c => !c.exported).length;
            
            exportCount.textContent = `(${totalCaptures} Aufnahmen)`;
            
            if (unexportedCaptures === 0) {
                exportButton.disabled = true;
                exportButton.style.opacity = '0.5';
            } else {
                exportButton.disabled = false;
                exportButton.style.opacity = '1';
            }
        }
    }

    /**
     * Update thumbnail strip display
     */
    updateThumbnailStrip() {
        const thumbnailScroll = document.getElementById('thumbnailScroll');
        if (!thumbnailScroll) return;
        
        // Clear existing thumbnails (except add-new button)
        const existingThumbnails = thumbnailScroll.querySelectorAll('.thumbnail:not(.add-new)');
        existingThumbnails.forEach(thumb => thumb.remove());
        
        // Add thumbnails for each capture
        this.captures.forEach((capture, index) => {
            const thumbnail = this.createThumbnailElement(capture, index);
            thumbnailScroll.insertBefore(thumbnail, thumbnailScroll.lastElementChild);
        });
        
        // Scroll to latest capture
        if (this.captures.length > 0) {
            thumbnailScroll.scrollLeft = thumbnailScroll.scrollWidth;
        }
    }

    /**
     * Create thumbnail element
     */
    createThumbnailElement(capture, index) {
        const thumbnail = document.createElement('div');
        thumbnail.className = 'thumbnail';
        thumbnail.dataset.index = index;
        thumbnail.dataset.captureId = capture.id;
        
        // Create image
        const img = document.createElement('img');
        img.src = capture.thumbnail;
        img.alt = `Aufnahme ${index + 1}`;
        thumbnail.appendChild(img);
        
        // Create type indicator
        const typeIndicator = document.createElement('div');
        typeIndicator.className = 'thumbnail-type';
        const icon = document.createElement('i');
        icon.className = capture.type === 'video' ? 'ms-Icon ms-Icon--Video' : 'ms-Icon ms-Icon--Camera';
        typeIndicator.appendChild(icon);
        thumbnail.appendChild(typeIndicator);
        
        // Create number indicator
        const numberIndicator = document.createElement('div');
        numberIndicator.className = 'thumbnail-number';
        numberIndicator.textContent = `#${index + 1}`;
        thumbnail.appendChild(numberIndicator);
        
        // Add exported indicator if needed
        if (capture.exported) {
            thumbnail.classList.add('exported');
        }
        
        return thumbnail;
    }

    /**
     * Exit the application
     */
    exitApp() {
        console.log('ModeManager: Exiting application');
        
        // Send exit message to WebView2 host
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'exit',
                data: {}
            }));
        } else {
            // Fallback for testing
            window.close();
        }
    }

    /**
     * Emit custom events
     */
    emitEvent(eventName, data = {}) {
        const event = new CustomEvent(eventName, { 
            detail: data,
            bubbles: true 
        });
        document.dispatchEvent(event);
    }

    /**
     * Emit mode change event
     */
    emitModeChange(newMode) {
        this.emitEvent('modeChanged', { 
            previousMode: this.currentMode === newMode ? null : this.currentMode,
            currentMode: newMode,
            patient: this.currentPatient
        });
    }

    /**
     * Get current mode
     */
    getCurrentMode() {
        return this.currentMode;
    }

    /**
     * Get current patient
     */
    getCurrentPatient() {
        return this.currentPatient;
    }

    /**
     * Get current captures
     */
    getCurrentCaptures() {
        return [...this.captures]; // Return copy
    }

    /**
     * Check if session has unsaved data
     */
    hasUnsavedData() {
        return this.captures.some(c => !c.exported);
    }
}

// Export for use in main app
window.ModeManager = ModeManager;