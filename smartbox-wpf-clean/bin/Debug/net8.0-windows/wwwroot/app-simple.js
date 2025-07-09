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

    sendToHost(type, data) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({ type, data });
        }
    }

    // Stub methods for other functionality
    async capturePhoto() {
        this.log('Capture photo clicked');
    }

    toggleRecording() {
        this.log('Toggle recording clicked');
    }

    exportDicom() {
        this.log('Export DICOM clicked');
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

    analyzeCamera() {
        this.log('Analyze camera clicked');
    }

    openLogs() {
        this.log('Open logs clicked');
    }

    testWebView2() {
        this.log('Test WebView2 clicked');
    }

    handleHostMessage(event) {
        this.log(`Received message from host: ${JSON.stringify(event.data)}`);
    }
}

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