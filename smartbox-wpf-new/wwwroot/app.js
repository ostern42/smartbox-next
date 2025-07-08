// SmartBox Next - Simplified Web UI Application
class SmartBoxApp {
    constructor() {
        console.log('SmartBoxApp constructor started');
        this.stream = null;
        this.mediaRecorder = null;
        this.recordedChunks = [];
        this.isRecording = false;
        this.debugLog = [];
        this.config = null;
        this.lastCapturedPhoto = null;
        
        // Delay initialization to ensure DOM is ready
        setTimeout(() => {
            this.initializeElements();
            this.attachEventListeners();
            this.log('SmartBox Next Web UI initialized');
            
            // Auto-initialize webcam after a short delay
            setTimeout(() => {
                this.log('Auto-initializing webcam...');
                this.initWebcam();
            }, 2000);
        }, 100);
    }

    initializeElements() {
        console.log('Initializing elements...');
        
        // Video elements
        this.video = document.getElementById('webcamPreview');
        this.canvas = document.getElementById('captureCanvas');
        this.placeholder = document.getElementById('webcamPlaceholder');
        
        // Form elements
        this.patientName = document.getElementById('patientName');
        this.patientID = document.getElementById('patientID');
        this.birthDate = document.getElementById('birthDate');
        this.gender = document.getElementById('gender');
        this.studyDescription = document.getElementById('studyDescription');
        this.accessionNumber = document.getElementById('accessionNumber');
        this.debugInfo = document.getElementById('debugInfo');
        
        // Buttons - with null checks
        this.captureButton = document.getElementById('captureButton');
        this.recordButton = document.getElementById('recordButton');
        this.exportDicomButton = document.getElementById('exportDicomButton');
        this.settingsButton = document.getElementById('settingsButton');
        this.debugButton = document.getElementById('debugButton');
        this.analyzeButton = document.getElementById('analyzeButton');
        this.openLogsButton = document.getElementById('openLogsButton');
        this.testButton = document.getElementById('testButton');
        
        // Recording elements - with null checks
        if (this.recordButton) {
            this.recordIcon = this.recordButton.querySelector('.record-icon');
        }
        this.recordText = document.getElementById('recordText');
        
        // Emergency section
        this.emergencySection = document.getElementById('emergencySection');
        
        console.log('Elements initialized');
    }

    attachEventListeners() {
        console.log('Attaching event listeners...');
        
        // Only attach if elements exist
        if (this.captureButton) {
            this.captureButton.addEventListener('click', () => this.capturePhoto());
        }
        if (this.recordButton) {
            this.recordButton.addEventListener('click', () => this.toggleRecording());
        }
        if (this.exportDicomButton) {
            this.exportDicomButton.addEventListener('click', () => this.exportDicom());
        }
        if (this.settingsButton) {
            this.settingsButton.addEventListener('click', () => this.openSettings());
        }
        if (this.debugButton) {
            this.debugButton.addEventListener('click', () => this.toggleDebug());
        }
        if (this.analyzeButton) {
            this.analyzeButton.addEventListener('click', () => this.analyzeCamera());
        }
        if (this.openLogsButton) {
            this.openLogsButton.addEventListener('click', () => this.openLogs());
        }
        if (this.testButton) {
            this.testButton.addEventListener('click', () => this.testWebView2());
        }
        
        // Listen for messages from C# host
        window.addEventListener('message', (e) => this.handleHostMessage(e));
        
        // Emergency template buttons
        const emergencyButtons = document.querySelectorAll('.emergency-button');
        emergencyButtons.forEach(btn => {
            btn.addEventListener('click', (e) => this.applyEmergencyTemplate(e.currentTarget.dataset.template));
        });
        
        // Request config to check if emergency templates are enabled
        setTimeout(() => {
            this.sendToHost('requestConfig', {});
        }, 1000);
        
        console.log('Event listeners attached');
    }

    log(message, level = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = `[${timestamp}] ${level.toUpperCase()}: ${message}`;
        
        console.log(logEntry);
        this.debugLog.push(logEntry);
        
        // Keep only last 100 entries
        if (this.debugLog.length > 100) {
            this.debugLog.shift();
        }
        
        // Update debug display if visible
        if (this.debugInfo && this.debugInfo.style.display !== 'none') {
            this.debugInfo.textContent = this.debugLog.join('\n');
            this.debugInfo.scrollTop = this.debugInfo.scrollHeight;
        }
    }

    async initWebcam() {
        try {
            this.log('Initializing webcam...');
            
            // Request camera access with optimal settings
            const constraints = {
                video: {
                    width: { ideal: 1920 },
                    height: { ideal: 1080 },
                    frameRate: { ideal: 60, min: 30 },
                    facingMode: 'user'
                },
                audio: false
            };
            
            this.stream = await navigator.mediaDevices.getUserMedia(constraints);
            this.video.srcObject = this.stream;
            
            // Get actual capabilities
            const track = this.stream.getVideoTracks()[0];
            const settings = track.getSettings();
            const capabilities = track.getCapabilities();
            
            this.log(`Camera initialized: ${settings.width}x${settings.height} @ ${settings.frameRate}fps`);
            this.log(`Device: ${track.label}`);
            
            // Hide placeholder
            if (this.placeholder) {
                this.placeholder.classList.add('hidden');
            }
            
            // Enable capture buttons
            if (this.captureButton) this.captureButton.disabled = false;
            if (this.recordButton) this.recordButton.disabled = false;
            if (this.analyzeButton) this.analyzeButton.disabled = false;
            
            // Notify C# host
            this.sendToHost('webcamInitialized', {
                width: settings.width,
                height: settings.height,
                frameRate: settings.frameRate,
                deviceId: settings.deviceId,
                capabilities: capabilities
            });
            
        } catch (error) {
            this.log(`Failed to initialize webcam: ${error.message}`, 'error');
            
            // Notify C# host of error
            this.sendToHost('webcamError', { error: error.message });
        }
    }

    sendToHost(action, data) {
        if (window.chrome && window.chrome.webview) {
            const message = JSON.stringify({ action, data });
            window.chrome.webview.postMessage(message);
            this.log(`Sent to host: ${action}`);
        } else {
            this.log('WebView2 bridge not available', 'warn');
        }
    }

    // Photo capture functionality
    async capturePhoto() {
        this.log('Capture photo clicked');
        
        if (!this.stream || !this.video) {
            this.log('No active video stream', 'error');
            return;
        }
        
        try {
            // Create a canvas to capture the current video frame
            const canvas = document.createElement('canvas');
            
            canvas.width = this.video.videoWidth;
            canvas.height = this.video.videoHeight;
            
            const ctx = canvas.getContext('2d');
            ctx.drawImage(this.video, 0, 0, canvas.width, canvas.height);
            
            // Convert to blob
            canvas.toBlob(async (blob) => {
                if (!blob) {
                    this.log('Failed to capture photo', 'error');
                    return;
                }
                
                // Convert blob to base64
                const reader = new FileReader();
                reader.onloadend = () => {
                    const base64data = reader.result;
                    
                    // Remove data URL prefix for C#
                    const base64Image = base64data.split(',')[1];
                    
                    // Store for DICOM export
                    this.lastCapturedPhoto = base64Image;
                    
                    // Send to C# host with correct message format
                    this.sendToHost('photoCaptured', {
                        imageData: base64Image,
                        width: canvas.width,
                        height: canvas.height,
                        timestamp: new Date().toISOString(),
                        patient: this.getPatientInfo()
                    });
                    
                    this.log(`Photo captured: ${canvas.width}x${canvas.height}`);
                    
                    // Show preview
                    this.showPhotoPreview(base64data);
                };
                reader.readAsDataURL(blob);
                
            }, 'image/jpeg', 0.95);
            
        } catch (error) {
            this.log(`Failed to capture photo: ${error.message}`, 'error');
        }
    }
    
    showPhotoPreview(imageData) {
        // Create a simple preview modal
        const modal = document.createElement('div');
        modal.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0,0,0,0.8);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 10000;
        `;
        
        const img = document.createElement('img');
        img.src = imageData;
        img.style.cssText = `
            max-width: 90%;
            max-height: 90%;
            border: 2px solid white;
            box-shadow: 0 4px 20px rgba(0,0,0,0.5);
        `;
        
        modal.appendChild(img);
        
        // Close on click
        modal.addEventListener('click', () => {
            document.body.removeChild(modal);
        });
        
        document.body.appendChild(modal);
        
        // Auto-close after 3 seconds
        setTimeout(() => {
            if (document.body.contains(modal)) {
                document.body.removeChild(modal);
            }
        }, 3000);
    }
    
    getPatientInfo() {
        // Collect patient information from form
        const name = `${this.patientName?.value || 'Unknown'}, ${this.patientName?.value || 'Unknown'}`;
        return {
            id: this.patientID?.value || '',
            name: name,
            birthDate: this.birthDate?.value || '',
            gender: this.gender?.value || '',
            studyDescription: this.studyDescription?.value || '',
            accessionNumber: this.accessionNumber?.value || ''
        };
    }

    toggleRecording() {
        if (!this.isRecording) {
            this.startRecording();
        } else {
            this.stopRecording();
        }
    }
    
    async startRecording() {
        if (!this.stream || !this.video) {
            this.log('No active video stream', 'error');
            return;
        }
        
        try {
            this.log('Starting video recording...');
            
            // Clear previous chunks
            this.recordedChunks = [];
            
            // Create MediaRecorder with WebM format
            const options = {
                mimeType: 'video/webm;codecs=vp9',
                videoBitsPerSecond: 2500000 // 2.5 Mbps
            };
            
            // Fallback to other formats if VP9 not supported
            if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                options.mimeType = 'video/webm;codecs=vp8';
                if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                    options.mimeType = 'video/webm';
                }
            }
            
            this.mediaRecorder = new MediaRecorder(this.stream, options);
            this.recordingStartTime = Date.now();
            
            // Handle data available
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data && event.data.size > 0) {
                    this.recordedChunks.push(event.data);
                }
            };
            
            // Handle recording stop
            this.mediaRecorder.onstop = () => {
                this.handleRecordingComplete();
            };
            
            // Start recording
            this.mediaRecorder.start(1000); // Collect data every second
            this.isRecording = true;
            
            // Update UI
            if (this.recordButton) {
                this.recordButton.classList.add('recording');
            }
            if (this.recordIcon) {
                this.recordIcon.classList.add('recording');
            }
            if (this.recordText) {
                this.recordText.textContent = 'Stop Recording';
            }
            
            this.log('Recording started');
            
        } catch (error) {
            this.log(`Failed to start recording: ${error.message}`, 'error');
        }
    }
    
    stopRecording() {
        if (!this.mediaRecorder || this.mediaRecorder.state !== 'recording') {
            this.log('No active recording', 'warn');
            return;
        }
        
        try {
            this.log('Stopping video recording...');
            this.mediaRecorder.stop();
            this.isRecording = false;
            
            // Update UI
            if (this.recordButton) {
                this.recordButton.classList.remove('recording');
            }
            if (this.recordIcon) {
                this.recordIcon.classList.remove('recording');
            }
            if (this.recordText) {
                this.recordText.textContent = 'Start Recording';
            }
            
        } catch (error) {
            this.log(`Failed to stop recording: ${error.message}`, 'error');
        }
    }
    
    async handleRecordingComplete() {
        try {
            const duration = (Date.now() - this.recordingStartTime) / 1000;
            this.log(`Recording complete. Duration: ${duration.toFixed(1)}s`);
            
            // Create blob from chunks
            const blob = new Blob(this.recordedChunks, { type: 'video/webm' });
            
            // Convert to base64
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64data = reader.result;
                const base64Video = base64data.split(',')[1];
                
                // Send to C# host
                this.sendToHost('videoRecorded', {
                    videoData: base64Video,
                    duration: duration,
                    timestamp: new Date().toISOString(),
                    patient: this.getPatientInfo()
                });
                
                this.log(`Video ready for upload: ${(blob.size / 1024 / 1024).toFixed(2)} MB`);
            };
            reader.readAsDataURL(blob);
            
        } catch (error) {
            this.log(`Failed to process recording: ${error.message}`, 'error');
        }
    }

    async exportDicom() {
        this.log('Export DICOM clicked');
        
        // Check if we have a recent photo
        if (!this.lastCapturedPhoto) {
            this.log('No photo captured yet', 'warn');
            alert('Please capture a photo first');
            return;
        }
        
        // Get patient info
        const patientInfo = this.getPatientInfo();
        
        // Validate required fields
        if (!patientInfo.id || !patientInfo.name) {
            this.log('Missing patient information', 'warn');
            alert('Please enter patient ID and name');
            return;
        }
        
        // Send to C# for DICOM export
        this.sendToHost('exportDicom', {
            imageData: this.lastCapturedPhoto,
            patientInfo: patientInfo,
            timestamp: new Date().toISOString()
        });
        
        this.log('DICOM export requested');
    }

    openSettings() {
        this.log('Open settings clicked');
        window.location.href = 'settings.html';
    }

    toggleDebug() {
        this.log('Toggle debug clicked');
        if (this.debugInfo) {
            this.debugInfo.style.display = this.debugInfo.style.display === 'none' ? 'block' : 'none';
        }
    }

    async analyzeCamera() {
        this.log('Analyzing camera capabilities...');
        
        if (!this.stream) {
            this.log('No active video stream', 'error');
            return;
        }
        
        try {
            const track = this.stream.getVideoTracks()[0];
            const settings = track.getSettings();
            const capabilities = track.getCapabilities();
            
            const analysis = {
                deviceLabel: track.label,
                currentSettings: settings,
                capabilities: capabilities,
                supportedConstraints: navigator.mediaDevices.getSupportedConstraints()
            };
            
            // Log details
            this.log(`Camera: ${track.label}`);
            this.log(`Current: ${settings.width}x${settings.height} @ ${settings.frameRate}fps`);
            this.log(`Capabilities: ${JSON.stringify(capabilities, null, 2)}`);
            
            // Send to C# host
            this.sendToHost('cameraAnalysis', analysis);
            
        } catch (error) {
            this.log(`Failed to analyze camera: ${error.message}`, 'error');
        }
    }

    openLogs() {
        this.log('Opening logs folder...');
        this.sendToHost('openLogs', {});
    }

    testWebView2() {
        this.log('Testing WebView2 communication...');
        this.sendToHost('testWebView', { 
            message: 'Test from JavaScript',
            timestamp: new Date().toISOString()
        });
    }

    handleHostMessage(event) {
        this.log(`Received message from host: ${JSON.stringify(event.data)}`);
        
        const message = event.data;
        if (message && message.action === 'updateConfig') {
            if (message.data && message.data.enableEmergencyTemplates) {
                // Show emergency templates section
                if (this.emergencySection) {
                    this.emergencySection.style.display = 'block';
                }
            }
        }
    }
    
    applyEmergencyTemplate(template) {
        const now = new Date();
        const dateStr = now.toISOString().split('T')[0];
        const timeStr = now.toTimeString().split(' ')[0];
        
        switch(template) {
            case 'male':
                this.patientName.value = 'Emergency, Male';
                this.patientID.value = `EM${now.getTime()}`;
                this.gender.value = 'M';
                this.studyDescription.value = `Emergency admission ${dateStr} ${timeStr}`;
                this.accessionNumber.value = `ACC${now.getTime()}`;
                break;
                
            case 'female':
                this.patientName.value = 'Emergency, Female';
                this.patientID.value = `EF${now.getTime()}`;
                this.gender.value = 'F';
                this.studyDescription.value = `Emergency admission ${dateStr} ${timeStr}`;
                this.accessionNumber.value = `ACC${now.getTime()}`;
                break;
                
            case 'child':
                this.patientName.value = 'Emergency, Child';
                this.patientID.value = `EC${now.getTime()}`;
                this.gender.value = 'O';
                this.studyDescription.value = `Emergency pediatric ${dateStr} ${timeStr}`;
                this.accessionNumber.value = `ACC${now.getTime()}`;
                // Set approximate birth date (5 years old)
                const childBirthDate = new Date();
                childBirthDate.setFullYear(childBirthDate.getFullYear() - 5);
                this.birthDate.value = childBirthDate.toISOString().split('T')[0];
                break;
        }
        
        this.log(`Applied emergency template: ${template}`);
        
        // Flash the form to indicate it was filled
        this.patientName.parentElement.parentElement.style.background = '#e3f2fd';
        setTimeout(() => {
            this.patientName.parentElement.parentElement.style.background = '';
        }, 500);
    }
}

// Global function for receiving messages from C#
window.receiveMessage = function(message) {
    console.log('Received message from C#:', message);
    
    if (window.app) {
        window.app.handleHostMessage({ data: message });
    }
    
    // Handle specific message types
    if (message.type === 'success' || message.type === 'error' || message.type === 'info') {
        if (window.app) {
            window.app.log(message.message, message.type);
        }
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        console.log('DOM loaded, creating SmartBoxApp');
        window.app = new SmartBoxApp();
    });
} else {
    console.log('DOM already loaded, creating SmartBoxApp');
    window.app = new SmartBoxApp();
}