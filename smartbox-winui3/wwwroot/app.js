// SmartBox Next - Web UI Application
class SmartBoxApp {
    constructor() {
        this.stream = null;
        this.mediaRecorder = null;
        this.recordedChunks = [];
        this.isRecording = false;
        this.debugLog = [];
        
        this.initializeElements();
        this.attachEventListeners();
        this.log('SmartBox Next Web UI initialized');
    }

    initializeElements() {
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
        
        // Buttons
        this.initWebcamButton = document.getElementById('initWebcamButton');
        this.captureButton = document.getElementById('captureButton');
        this.recordButton = document.getElementById('recordButton');
        this.exportDicomButton = document.getElementById('exportDicomButton');
        this.settingsButton = document.getElementById('settingsButton');
        this.debugButton = document.getElementById('debugButton');
        this.analyzeButton = document.getElementById('analyzeButton');
        
        // Recording elements
        this.recordIcon = this.recordButton.querySelector('.record-icon');
        this.recordText = document.getElementById('recordText');
    }

    attachEventListeners() {
        this.initWebcamButton.addEventListener('click', () => this.initWebcam());
        this.captureButton.addEventListener('click', () => this.capturePhoto());
        this.recordButton.addEventListener('click', () => this.toggleRecording());
        this.exportDicomButton.addEventListener('click', () => this.exportDicom());
        this.settingsButton.addEventListener('click', () => this.openSettings());
        this.debugButton.addEventListener('click', () => this.toggleDebug());
        this.analyzeButton.addEventListener('click', () => this.analyzeCamera());
        
        // Listen for messages from C# host
        window.addEventListener('message', (e) => this.handleHostMessage(e));
        
        // Mark numeric inputs for keyboard
        this.markNumericInputs();
    }
    
    markNumericInputs() {
        // Mark IP and port fields for numeric keyboard
        const numericFields = ['server-ip', 'server-port', 'local-port'];
        numericFields.forEach(id => {
            const field = document.getElementById(id);
            if (field) {
                field.classList.add('numeric-input');
                field.classList.add('use-keyboard');
            }
        });
        
        // Mark all inputs for touch keyboard on touch devices
        if ('ontouchstart' in window) {
            document.querySelectorAll('input, textarea').forEach(input => {
                input.classList.add('use-keyboard');
            });
        }
    }

    log(message, level = 'info') {
        const timestamp = new Date().toLocaleTimeString();
        const logEntry = `[${timestamp}] ${level.toUpperCase()}: ${message}`;
        this.debugLog.push(logEntry);
        
        // Keep only last 100 entries
        if (this.debugLog.length > 100) {
            this.debugLog.shift();
        }
        
        // Update debug display
        this.debugInfo.value = this.debugLog.join('\n');
        this.debugInfo.scrollTop = this.debugInfo.scrollHeight;
        
        // Also log to console
        console.log(logEntry);
    }

    async initWebcam() {
        try {
            this.log('Initializing webcam...');
            this.initWebcamButton.disabled = true;
            
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
            this.placeholder.classList.add('hidden');
            
            // Enable capture buttons
            this.captureButton.disabled = false;
            this.recordButton.disabled = false;
            this.analyzeButton.disabled = false;
            
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
            this.initWebcamButton.disabled = false;
            
            // Notify C# host of error
            this.sendToHost('webcamError', { error: error.message });
        }
    }

    async capturePhoto() {
        if (!this.stream) {
            this.log('No webcam stream available', 'error');
            return;
        }
        
        try {
            this.log('Capturing photo...');
            
            // Set canvas size to video dimensions
            this.canvas.width = this.video.videoWidth;
            this.canvas.height = this.video.videoHeight;
            
            // Draw current frame to canvas
            const ctx = this.canvas.getContext('2d');
            ctx.drawImage(this.video, 0, 0);
            
            // Convert to blob
            const blob = await new Promise(resolve => {
                this.canvas.toBlob(resolve, 'image/jpeg', 0.95);
            });
            
            // Convert to base64 for C# transfer
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                
                // Collect patient data
                const patientData = {
                    name: this.patientName.value,
                    id: this.patientID.value,
                    birthDate: this.birthDate.value,
                    gender: this.gender.value,
                    studyDescription: this.studyDescription.value,
                    accessionNumber: this.accessionNumber.value
                };
                
                // Send to C# host
                this.sendToHost('photoCaptured', {
                    imageData: base64,
                    width: this.canvas.width,
                    height: this.canvas.height,
                    timestamp: new Date().toISOString(),
                    patient: patientData
                });
                
                this.log(`Photo captured: ${this.canvas.width}x${this.canvas.height}`);
            };
            reader.readAsDataURL(blob);
            
        } catch (error) {
            this.log(`Failed to capture photo: ${error.message}`, 'error');
        }
    }

    async toggleRecording() {
        if (!this.isRecording) {
            await this.startRecording();
        } else {
            await this.stopRecording();
        }
    }

    async startRecording() {
        if (!this.stream) {
            this.log('No webcam stream available', 'error');
            return;
        }
        
        try {
            this.log('Starting video recording...');
            
            // Configure MediaRecorder
            const options = {
                mimeType: 'video/webm;codecs=vp9',
                videoBitsPerSecond: 5000000 // 5 Mbps
            };
            
            // Fallback to VP8 if VP9 not supported
            if (!MediaRecorder.isTypeSupported(options.mimeType)) {
                options.mimeType = 'video/webm;codecs=vp8';
            }
            
            this.recordedChunks = [];
            this.mediaRecorder = new MediaRecorder(this.stream, options);
            
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.recordedChunks.push(event.data);
                }
            };
            
            this.mediaRecorder.onstop = () => {
                this.processRecording();
            };
            
            this.mediaRecorder.start(1000); // Collect data every second
            this.isRecording = true;
            
            // Update UI
            this.recordButton.classList.add('recording');
            this.recordText.textContent = 'Stop Recording';
            
            // Notify C# host
            this.sendToHost('recordingStarted', {
                mimeType: options.mimeType,
                timestamp: new Date().toISOString()
            });
            
        } catch (error) {
            this.log(`Failed to start recording: ${error.message}`, 'error');
        }
    }

    async stopRecording() {
        if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
            return;
        }
        
        this.log('Stopping video recording...');
        this.mediaRecorder.stop();
        this.isRecording = false;
        
        // Update UI
        this.recordButton.classList.remove('recording');
        this.recordText.textContent = 'Start Recording';
    }

    async processRecording() {
        try {
            const blob = new Blob(this.recordedChunks, { type: 'video/webm' });
            this.log(`Recording complete: ${(blob.size / 1024 / 1024).toFixed(2)} MB`);
            
            // Convert to base64 for C# transfer
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                
                // Collect patient data
                const patientData = {
                    name: this.patientName.value,
                    id: this.patientID.value,
                    birthDate: this.birthDate.value,
                    gender: this.gender.value,
                    studyDescription: this.studyDescription.value,
                    accessionNumber: this.accessionNumber.value
                };
                
                // Send to C# host
                this.sendToHost('videoRecorded', {
                    videoData: base64,
                    size: blob.size,
                    duration: this.recordedChunks.length, // Approximate seconds
                    timestamp: new Date().toISOString(),
                    patient: patientData
                });
            };
            reader.readAsDataURL(blob);
            
        } catch (error) {
            this.log(`Failed to process recording: ${error.message}`, 'error');
        }
    }

    async analyzeCamera() {
        if (!this.stream) {
            this.log('No webcam stream available', 'error');
            return;
        }
        
        try {
            const track = this.stream.getVideoTracks()[0];
            const settings = track.getSettings();
            const capabilities = track.getCapabilities();
            
            const analysis = {
                current: settings,
                capabilities: capabilities,
                constraints: track.getConstraints()
            };
            
            this.log('Camera Analysis:', 'info');
            this.log(JSON.stringify(analysis, null, 2), 'info');
            
            // Send to C# host
            this.sendToHost('cameraAnalysis', analysis);
            
        } catch (error) {
            this.log(`Failed to analyze camera: ${error.message}`, 'error');
        }
    }

    exportDicom() {
        const patientData = {
            name: this.patientName.value,
            id: this.patientID.value,
            birthDate: this.birthDate.value,
            gender: this.gender.value,
            studyDescription: this.studyDescription.value,
            accessionNumber: this.accessionNumber.value
        };
        
        this.sendToHost('exportDicom', patientData);
        this.log('DICOM export requested');
    }

    openSettings() {
        this.sendToHost('openSettings', {});
        this.log('Settings requested');
    }

    toggleDebug() {
        const isVisible = this.debugInfo.style.display !== 'none';
        this.debugInfo.style.display = isVisible ? 'none' : 'block';
        this.debugButton.textContent = isVisible ? 'Show Debug Info' : 'Hide Debug Info';
    }

    // Communication with C# host
    sendToHost(action, data) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                action: action,
                data: data,
                timestamp: new Date().toISOString()
            });
        } else {
            this.log('Running in browser mode - no C# host available', 'warn');
        }
    }

    handleHostMessage(event) {
        const message = event.data;
        
        switch (message.action) {
            case 'updatePatient':
                this.updatePatientInfo(message.data);
                break;
                
            case 'capturePhoto':
                this.capturePhoto();
                break;
                
            case 'startRecording':
                this.startRecording();
                break;
                
            case 'stopRecording':
                this.stopRecording();
                break;
                
            case 'log':
                this.log(message.data.message, message.data.level);
                break;
                
            default:
                this.log(`Unknown message from host: ${message.action}`, 'warn');
        }
    }

    updatePatientInfo(data) {
        if (data.name) this.patientName.value = data.name;
        if (data.id) this.patientID.value = data.id;
        if (data.birthDate) this.birthDate.value = data.birthDate;
        if (data.gender) this.gender.value = data.gender;
        if (data.studyDescription) this.studyDescription.value = data.studyDescription;
        if (data.accessionNumber) this.accessionNumber.value = data.accessionNumber;
        
        this.log('Patient info updated from host');
    }
}

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.smartBoxApp = new SmartBoxApp();
});