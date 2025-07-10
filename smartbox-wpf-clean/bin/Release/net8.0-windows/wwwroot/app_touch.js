/**
 * SmartBox Next Touch Application
 * Main application controller for touch-optimized medical imaging
 */

class SmartBoxTouchApp {
    constructor() {
        this.isInitialized = false;
        this.webcamStream = null;
        this.isRecording = false;
        this.mediaRecorder = null;
        this.recordingStart = null;
        this.recordingTimer = null;
        
        // Initialize managers
        this.gestureManager = null;
        this.dialogManager = null;
        this.modeManager = null;
        
        // MWL data
        this.mwlData = [];
        this.filteredMwlData = [];
        
        console.log('SmartBoxTouchApp: Starting initialization...');
        this.initialize();
    }

    async initialize() {
        try {
            // Initialize managers
            this.gestureManager = new TouchGestureManager();
            this.dialogManager = new TouchDialogManager();
            this.modeManager = new ModeManager();
            
            // Make dialog manager globally available
            window.touchDialogManager = this.dialogManager;
            
            // Set up event listeners
            this.setupEventListeners();
            
            // Initialize webcam
            await this.initializeWebcam();
            
            // Load initial MWL data
            await this.loadMWLData();
            
            // Set up periodic refresh
            this.setupPeriodicRefresh();
            
            this.isInitialized = true;
            console.log('SmartBoxTouchApp: Initialization complete');
            
            // Hide loading overlay if it exists
            const loadingOverlay = document.getElementById('loadingOverlay');
            if (loadingOverlay) {
                loadingOverlay.classList.add('hidden');
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Initialization failed:', error);
            this.showError('Fehler beim Starten der Anwendung: ' + error.message);
        }
    }

    setupEventListeners() {
        // Gesture events
        document.addEventListener('mwlRefresh', () => this.onMWLRefresh());
        document.addEventListener('emergencyPatientSelected', (e) => this.onEmergencyPatientSelected(e));
        document.addEventListener('capturePhoto', () => this.onCapturePhoto());
        document.addEventListener('startVideoRecording', () => this.onStartVideoRecording());
        document.addEventListener('stopVideoRecording', () => this.onStopVideoRecording());
        document.addEventListener('confirmDeleteThumbnail', (e) => this.onConfirmDeleteThumbnail(e));
        
        // Mode change events
        document.addEventListener('modeChanged', (e) => this.onModeChanged(e));
        document.addEventListener('initializeRecordingWebcam', (e) => this.onInitializeRecordingWebcam(e));
        
        // UI events
        this.setupUIEventListeners();
        
        console.log('SmartBoxTouchApp: Event listeners set up');
    }

    setupUIEventListeners() {
        // MWL filter
        const mwlFilter = document.getElementById('mwlFilter');
        if (mwlFilter) {
            mwlFilter.addEventListener('input', (e) => this.onMWLFilterChange(e));
        }
        
        // Export button
        const exportButton = document.getElementById('exportButton');
        if (exportButton) {
            exportButton.addEventListener('click', () => this.onExportRequested());
        }
        
        // Patient card clicks
        document.addEventListener('click', (e) => {
            const patientCard = e.target.closest('.patient-card');
            if (patientCard) {
                this.onPatientCardClick(patientCard);
            }
        });
    }

    async initializeWebcam() {
        try {
            console.log('SmartBoxTouchApp: Initializing webcam...');
            
            const constraints = {
                video: {
                    width: { ideal: 1280 },
                    height: { ideal: 960 },
                    facingMode: 'user'
                },
                audio: true
            };
            
            this.webcamStream = await navigator.mediaDevices.getUserMedia(constraints);
            
            // Initialize small preview
            const smallPreview = document.getElementById('webcamPreviewSmall');
            if (smallPreview) {
                smallPreview.srcObject = this.webcamStream;
            }
            
            console.log('SmartBoxTouchApp: Webcam initialized');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Webcam initialization failed:', error);
            this.showError('Kamera konnte nicht initialisiert werden: ' + error.message);
        }
    }

    async onInitializeRecordingWebcam(event) {
        const videoElement = event.detail.videoElement;
        if (videoElement && this.webcamStream) {
            videoElement.srcObject = this.webcamStream;
            console.log('SmartBoxTouchApp: Recording webcam initialized');
        }
    }

    async loadMWLData() {
        try {
            console.log('SmartBoxTouchApp: Loading MWL data...');
            
            // Send request to WebView2 host
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({
                    type: 'loadMWL',
                    data: {}
                });
            } else {
                // Fallback demo data for testing
                this.onMWLDataReceived(this.getDemoMWLData());
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: MWL loading failed:', error);
            this.showError('Worklist konnte nicht geladen werden: ' + error.message);
        }
    }

    onMWLDataReceived(mwlData) {
        console.log('SmartBoxTouchApp: MWL data received:', mwlData.length, 'entries');
        
        this.mwlData = mwlData;
        this.filteredMwlData = [...mwlData];
        this.renderMWLCards();
    }

    renderMWLCards() {
        const mwlCards = document.getElementById('mwlCards');
        if (!mwlCards) return;
        
        // Clear existing cards
        mwlCards.innerHTML = '';
        
        // Render filtered cards
        this.filteredMwlData.forEach(patient => {
            const card = this.createPatientCard(patient);
            mwlCards.appendChild(card);
        });
        
        console.log('SmartBoxTouchApp: Rendered', this.filteredMwlData.length, 'patient cards');
    }

    createPatientCard(patient) {
        const card = document.createElement('div');
        card.className = 'patient-card';
        card.dataset.patientId = patient.id;
        
        card.innerHTML = `
            <div class="patient-info">
                <div class="patient-name">
                    <i class="ms-Icon ms-Icon--Contact"></i>
                    <span>${patient.name}</span>
                </div>
                <div class="patient-details">
                    <span class="patient-birth">
                        <i class="ms-Icon ms-Icon--Cake"></i>
                        ${patient.birthDate}
                    </span>
                    <span class="patient-study">
                        <i class="ms-Icon ms-Icon--Medical"></i>
                        ${patient.studyType}
                    </span>
                    <span class="patient-time">
                        <i class="ms-Icon ms-Icon--Clock"></i>
                        ${patient.scheduledTime}
                    </span>
                </div>
            </div>
            <div class="card-action">
                <span>Antippen zum Auswählen</span>
            </div>
        `;
        
        return card;
    }

    onPatientCardClick(card) {
        const patientId = card.dataset.patientId;
        const patient = this.mwlData.find(p => p.id === patientId);
        
        if (patient) {
            console.log('SmartBoxTouchApp: Patient selected:', patient.name);
            
            // Add visual feedback
            card.classList.add('selected');
            
            // Emit patient selection event
            document.dispatchEvent(new CustomEvent('patientSelected', {
                detail: patient
            }));
        }
    }

    onEmergencyPatientSelected(event) {
        console.log('SmartBoxTouchApp: Emergency patient selected:', event.detail.type);
        // The mode manager handles this
    }

    onMWLFilterChange(event) {
        const filterText = event.target.value.toLowerCase().trim();
        
        if (filterText === '') {
            this.filteredMwlData = [...this.mwlData];
        } else {
            this.filteredMwlData = this.mwlData.filter(patient => 
                patient.name.toLowerCase().includes(filterText) ||
                patient.studyType.toLowerCase().includes(filterText) ||
                patient.id.toLowerCase().includes(filterText)
            );
        }
        
        this.renderMWLCards();
    }

    onMWLRefresh() {
        console.log('SmartBoxTouchApp: MWL refresh requested');
        this.loadMWLData();
    }

    onModeChanged(event) {
        console.log('SmartBoxTouchApp: Mode changed to:', event.detail.currentMode);
        
        if (event.detail.currentMode === 'recording') {
            // Reset recording state
            this.isRecording = false;
            this.updateRecordingUI();
        }
    }

    async onCapturePhoto() {
        try {
            console.log('SmartBoxTouchApp: Capturing photo...');
            
            const video = document.getElementById('webcamPreviewLarge');
            const canvas = document.getElementById('captureCanvas');
            
            if (!video || !canvas) {
                throw new Error('Capture elements not found');
            }
            
            // Set canvas size to video size
            canvas.width = video.videoWidth;
            canvas.height = video.videoHeight;
            
            // Draw video frame to canvas
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0);
            
            // Get image data
            const imageData = canvas.toDataURL('image/jpeg', 0.8);
            
            // Create thumbnail
            const thumbnailCanvas = document.createElement('canvas');
            thumbnailCanvas.width = 160;
            thumbnailCanvas.height = 120;
            const thumbCtx = thumbnailCanvas.getContext('2d');
            thumbCtx.drawImage(video, 0, 0, 160, 120);
            const thumbnail = thumbnailCanvas.toDataURL('image/jpeg', 0.6);
            
            // Add capture to mode manager
            const captureId = this.modeManager.addCapture({
                type: 'photo',
                data: imageData,
                thumbnail: thumbnail
            });
            
            // Send to WebView2 host for processing
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({
                    type: 'capturePhoto',
                    data: {
                        captureId: captureId,
                        imageData: imageData,
                        patient: this.modeManager.getCurrentPatient()
                    }
                });
            }
            
            console.log('SmartBoxTouchApp: Photo captured successfully');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Photo capture failed:', error);
            this.showError('Foto konnte nicht aufgenommen werden: ' + error.message);
        }
    }

    async onStartVideoRecording() {
        try {
            if (this.isRecording) return;
            
            console.log('SmartBoxTouchApp: Starting video recording...');
            
            if (!this.webcamStream) {
                throw new Error('Webcam not available');
            }
            
            // Create media recorder
            this.mediaRecorder = new MediaRecorder(this.webcamStream, {
                mimeType: 'video/webm;codecs=vp8'
            });
            
            this.recordedChunks = [];
            
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.recordedChunks.push(event.data);
                }
            };
            
            this.mediaRecorder.onstop = () => {
                this.onVideoRecordingComplete();
            };
            
            // Start recording
            this.mediaRecorder.start();
            this.isRecording = true;
            this.recordingStart = Date.now();
            
            // Update UI
            this.updateRecordingUI();
            this.startRecordingTimer();
            
            console.log('SmartBoxTouchApp: Video recording started');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Video recording start failed:', error);
            this.showError('Video-Aufnahme konnte nicht gestartet werden: ' + error.message);
        }
    }

    onStopVideoRecording() {
        if (!this.isRecording || !this.mediaRecorder) return;
        
        console.log('SmartBoxTouchApp: Stopping video recording...');
        
        this.mediaRecorder.stop();
        this.isRecording = false;
        this.stopRecordingTimer();
        this.updateRecordingUI();
    }

    onVideoRecordingComplete() {
        try {
            const blob = new Blob(this.recordedChunks, { type: 'video/webm' });
            const videoUrl = URL.createObjectURL(blob);
            
            // Create thumbnail from video
            this.createVideoThumbnail(videoUrl).then(thumbnail => {
                const duration = Math.round((Date.now() - this.recordingStart) / 1000);
                
                // Add capture to mode manager
                const captureId = this.modeManager.addCapture({
                    type: 'video',
                    data: videoUrl,
                    thumbnail: thumbnail,
                    duration: duration
                });
                
                // Send to WebView2 host for processing
                if (window.chrome && window.chrome.webview) {
                    // Convert blob to base64 for transmission
                    const reader = new FileReader();
                    reader.onload = () => {
                        window.chrome.webview.postMessage({
                            type: 'captureVideo',
                            data: {
                                captureId: captureId,
                                videoData: reader.result,
                                duration: duration,
                                patient: this.modeManager.getCurrentPatient()
                            }
                        });
                    };
                    reader.readAsDataURL(blob);
                }
                
                console.log('SmartBoxTouchApp: Video recording completed, duration:', duration, 'seconds');
            });
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Video processing failed:', error);
            this.showError('Video konnte nicht verarbeitet werden: ' + error.message);
        }
    }

    async createVideoThumbnail(videoUrl) {
        return new Promise((resolve) => {
            const video = document.createElement('video');
            video.src = videoUrl;
            video.currentTime = 1; // Get frame at 1 second
            
            video.onloadeddata = () => {
                const canvas = document.createElement('canvas');
                canvas.width = 160;
                canvas.height = 120;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(video, 0, 0, 160, 120);
                resolve(canvas.toDataURL('image/jpeg', 0.6));
            };
        });
    }

    updateRecordingUI() {
        const recordingIndicator = document.getElementById('recordingIndicator');
        const captureHint = document.getElementById('captureHint');
        
        if (this.isRecording) {
            recordingIndicator?.classList.remove('hidden');
            captureHint?.classList.add('hidden');
        } else {
            recordingIndicator?.classList.add('hidden');
            captureHint?.classList.remove('hidden');
        }
    }

    startRecordingTimer() {
        this.recordingTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - this.recordingStart) / 1000);
            const minutes = Math.floor(elapsed / 60);
            const seconds = elapsed % 60;
            
            const timeText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            const recordingTime = document.getElementById('recordingTime');
            if (recordingTime) {
                recordingTime.textContent = timeText;
            }
        }, 1000);
    }

    stopRecordingTimer() {
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }

    onConfirmDeleteThumbnail(event) {
        const { index, element } = event.detail;
        
        this.dialogManager.confirmDelete('Aufnahme').then((confirmed) => {
            if (confirmed) {
                const captureId = element.dataset.captureId;
                this.modeManager.removeCapture(parseInt(captureId));
                console.log('SmartBoxTouchApp: Thumbnail deleted:', index);
            }
        });
    }

    onExportRequested() {
        const captures = this.modeManager.getCurrentCaptures();
        const unexportedCaptures = captures.filter(c => !c.exported);
        
        if (unexportedCaptures.length === 0) {
            this.showError('Keine Aufnahmen zum Exportieren vorhanden.');
            return;
        }
        
        this.dialogManager.confirmExport(unexportedCaptures.length).then((confirmed) => {
            if (confirmed) {
                this.exportCaptures(unexportedCaptures);
            }
        });
    }

    async exportCaptures(captures) {
        try {
            console.log('SmartBoxTouchApp: Exporting', captures.length, 'captures...');
            
            // Show loading dialog
            this.dialogManager.showLoading({
                message: `${captures.length} Aufnahme(n) werden exportiert...`
            });
            
            // Send export request to WebView2 host
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage({
                    type: 'exportCaptures',
                    data: {
                        captures: captures.map(c => ({ id: c.id, type: c.type })),
                        patient: this.modeManager.getCurrentPatient()
                    }
                });
            } else {
                // Simulate export for testing
                setTimeout(() => {
                    this.onExportComplete(captures.map(c => c.id));
                }, 3000);
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Export failed:', error);
            this.dialogManager.dismiss();
            this.showError('Export fehlgeschlagen: ' + error.message);
        }
    }

    onExportComplete(captureIds) {
        this.dialogManager.dismiss();
        this.modeManager.markCapturesExported(captureIds);
        
        this.dialogManager.showSuccess({
            message: `${captureIds.length} Aufnahme(n) erfolgreich exportiert.`
        });
        
        console.log('SmartBoxTouchApp: Export completed for', captureIds.length, 'captures');
    }

    setupPeriodicRefresh() {
        // Refresh MWL every 5 minutes
        setInterval(() => {
            if (this.modeManager.getCurrentMode() === 'patient') {
                this.loadMWLData();
            }
        }, 5 * 60 * 1000);
    }

    showError(message) {
        if (this.dialogManager) {
            this.dialogManager.error(message);
        } else {
            alert(message);
        }
    }

    getDemoMWLData() {
        return [
            {
                id: '12345678',
                name: 'Müller, Hans',
                birthDate: '12.05.1965',
                studyType: 'Endoskopie',
                scheduledTime: '14:00 Uhr',
                accessionNumber: 'ACC-001',
                studyDescription: 'Gastroskopie'
            },
            {
                id: '23456789',
                name: 'Schmidt, Maria',
                birthDate: '23.08.1978',
                studyType: 'Radiographie',
                scheduledTime: '14:30 Uhr',
                accessionNumber: 'ACC-002',
                studyDescription: 'Thorax-Röntgen'
            },
            {
                id: '34567890',
                name: 'Weber, Klaus',
                birthDate: '01.01.1990',
                studyType: 'Sonographie',
                scheduledTime: '15:00 Uhr',
                accessionNumber: 'ACC-003',
                studyDescription: 'Abdomen-Ultraschall'
            }
        ];
    }
}

// WebView2 message handler
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
        const { type, data } = event.data;
        
        switch (type) {
            case 'mwlData':
                if (window.smartBoxApp) {
                    window.smartBoxApp.onMWLDataReceived(data);
                }
                break;
                
            case 'exportComplete':
                if (window.smartBoxApp) {
                    window.smartBoxApp.onExportComplete(data.captureIds);
                }
                break;
                
            default:
                console.log('SmartBoxTouchApp: Unknown message type:', type);
        }
    });
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.smartBoxApp = new SmartBoxTouchApp();
});

console.log('SmartBoxTouchApp: Script loaded');