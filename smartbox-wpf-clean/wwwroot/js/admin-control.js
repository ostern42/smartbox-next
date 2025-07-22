/**
 * SmartBox Admin Control Interface
 * Wireless tablet control system for SmartBox-Next
 */

class AdminControlInterface {
    constructor() {
        this.websocket = null;
        this.connectionStatus = 'disconnected';
        this.heartbeatInterval = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 2000;
        
        // System state
        this.systemStatus = {
            cpu: 0,
            memory: 0,
            storage: 0,
            recording: false,
            currentPatient: null
        };
        
        this.patients = [];
        this.queueItems = [];
        
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.connectWebSocket();
        this.updateConnectionUI();
        
        // Initialize quality sliders
        this.initializeQualityControls();
        
        // Start periodic updates
        this.startStatusUpdates();
    }

    setupEventListeners() {
        // Connection and basic controls
        document.getElementById('refreshPatientsBtn').addEventListener('click', () => this.refreshPatients());
        document.getElementById('capturePhotoBtn').addEventListener('click', () => this.capturePhoto());
        document.getElementById('markCriticalBtn').addEventListener('click', () => this.markCritical());
        document.getElementById('emergencyStopBtn').addEventListener('click', () => this.emergencyStop());
        
        // Recording controls
        document.getElementById('startRecordingBtn').addEventListener('click', () => this.startRecording());
        document.getElementById('stopRecordingBtn').addEventListener('click', () => this.stopRecording());
        
        // Queue controls
        document.getElementById('processQueueBtn').addEventListener('click', () => this.processQueue());
        
        // System controls
        document.getElementById('diagnosticsBtn').addEventListener('click', () => this.runDiagnostics());
        document.getElementById('exportCurrentBtn').addEventListener('click', () => this.exportCurrent());
        document.getElementById('clearCacheBtn').addEventListener('click', () => this.clearCache());
        document.getElementById('restartServiceBtn').addEventListener('click', () => this.restartService());
        document.getElementById('shutdownBtn').addEventListener('click', () => this.shutdownSystem());
        
        // Quality control sliders
        document.getElementById('qualitySlider').addEventListener('input', (e) => this.updateQuality('video', e.target.value));
        document.getElementById('audioSlider').addEventListener('input', (e) => this.updateQuality('audio', e.target.value));
        
        // Window events
        window.addEventListener('beforeunload', () => this.disconnect());
        window.addEventListener('online', () => this.connectWebSocket());
        window.addEventListener('offline', () => this.updateConnectionUI());
    }

    initializeQualityControls() {
        const qualitySlider = document.getElementById('qualitySlider');
        const audioSlider = document.getElementById('audioSlider');
        
        qualitySlider.addEventListener('input', () => {
            document.getElementById('qualityValue').textContent = qualitySlider.value + '%';
        });
        
        audioSlider.addEventListener('input', () => {
            document.getElementById('audioValue').textContent = audioSlider.value + '%';
        });
    }

    connectWebSocket() {
        if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
            return;
        }

        try {
            // Try to connect to WebSocket server (assuming it runs on port 5001)
            this.websocket = new WebSocket('ws://localhost:5001/');
            
            this.websocket.onopen = () => {
                console.log('WebSocket connected');
                this.connectionStatus = 'connected';
                this.reconnectAttempts = 0;
                this.updateConnectionUI();
                this.startHeartbeat();
                this.requestInitialData();
            };
            
            this.websocket.onmessage = (event) => {
                this.handleMessage(JSON.parse(event.data));
            };
            
            this.websocket.onclose = () => {
                console.log('WebSocket disconnected');
                this.connectionStatus = 'disconnected';
                this.updateConnectionUI();
                this.stopHeartbeat();
                this.scheduleReconnect();
            };
            
            this.websocket.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.connectionStatus = 'error';
                this.updateConnectionUI();
            };
            
        } catch (error) {
            console.error('Failed to create WebSocket connection:', error);
            this.connectionStatus = 'error';
            this.updateConnectionUI();
            this.scheduleReconnect();
        }
    }

    scheduleReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            setTimeout(() => {
                console.log(`Reconnect attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
                this.connectWebSocket();
            }, this.reconnectDelay * this.reconnectAttempts);
        }
    }

    startHeartbeat() {
        this.heartbeatInterval = setInterval(() => {
            if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
                this.sendMessage('heartbeat', { timestamp: Date.now() });
            }
        }, 30000); // Send heartbeat every 30 seconds
    }

    stopHeartbeat() {
        if (this.heartbeatInterval) {
            clearInterval(this.heartbeatInterval);
            this.heartbeatInterval = null;
        }
    }

    updateConnectionUI() {
        const indicator = document.getElementById('connectionIndicator');
        const text = document.getElementById('connectionText');
        
        switch (this.connectionStatus) {
            case 'connected':
                indicator.classList.add('connected');
                text.textContent = 'Connected';
                break;
            case 'disconnected':
                indicator.classList.remove('connected');
                text.textContent = 'Disconnected';
                break;
            case 'error':
                indicator.classList.remove('connected');
                text.textContent = 'Connection Error';
                break;
            default:
                indicator.classList.remove('connected');
                text.textContent = 'Connecting...';
        }
    }

    sendMessage(type, data = {}) {
        if (this.websocket && this.websocket.readyState === WebSocket.OPEN) {
            const message = {
                type: type,
                data: data,
                timestamp: new Date().toISOString()
            };
            this.websocket.send(JSON.stringify(message));
        } else {
            console.warn('Cannot send message: WebSocket not connected');
        }
    }

    handleMessage(message) {
        console.log('Received message:', message);
        
        switch (message.type) {
            case 'system_status':
                this.updateSystemStatus(message.data);
                break;
            case 'recording_state':
                this.updateRecordingState(message.data);
                break;
            case 'patient_list':
                this.updatePatientList(message.data);
                break;
            case 'queue_update':
                this.updateQueue(message.data);
                break;
            case 'status_update':
                this.handleStatusUpdate(message.data);
                break;
            case 'ack':
                this.handleAcknowledgment(message.data);
                break;
            case 'error':
                this.handleError(message.data);
                break;
        }
    }

    requestInitialData() {
        this.sendMessage('get_system_status');
        this.sendMessage('get_patient_list');
        this.sendMessage('get_queue_status');
        this.sendMessage('get_recording_state');
    }

    updateSystemStatus(status) {
        this.systemStatus = { ...this.systemStatus, ...status };
        
        // Update CPU
        if (status.cpu_usage !== undefined) {
            document.getElementById('cpuUsage').textContent = status.cpu_usage + '%';
            document.getElementById('cpuBar').style.width = status.cpu_usage + '%';
        }
        
        // Update Memory
        if (status.memory_usage !== undefined) {
            document.getElementById('memoryUsage').textContent = status.memory_usage + ' MB';
            const memoryPercent = Math.min((status.memory_usage / 8192) * 100, 100); // Assume 8GB max
            document.getElementById('memoryBar').style.width = memoryPercent + '%';
        }
        
        // Update Storage
        if (status.storage_info) {
            const storagePercent = status.storage_info.used_percentage || 0;
            document.getElementById('storageUsage').textContent = storagePercent + '% used';
            document.getElementById('storageBar').style.width = storagePercent + '%';
        }
        
        // Update Recording Status
        if (status.recording_status) {
            this.updateRecordingStatus(status.recording_status);
        }
    }

    updateRecordingStatus(recordingStatus) {
        const isRecording = recordingStatus.is_recording;
        const duration = recordingStatus.duration_seconds || 0;
        const fileSize = recordingStatus.file_size_mb || 0;
        const frameRate = recordingStatus.frame_rate || 0;
        
        // Update recording time
        document.getElementById('recordingTime').textContent = this.formatDuration(duration);
        document.getElementById('fileSize').textContent = fileSize.toFixed(1) + ' MB';
        document.getElementById('frameRate').textContent = frameRate + ' fps';
        
        // Update recording buttons
        document.getElementById('startRecordingBtn').disabled = isRecording;
        document.getElementById('stopRecordingBtn').disabled = !isRecording;
        
        // Update stream overlay
        const recordingIndicator = document.getElementById('recordingIndicator');
        const streamStatus = document.getElementById('streamStatus');
        
        if (isRecording) {
            recordingIndicator.style.display = 'block';
            streamStatus.textContent = 'Recording - ' + this.formatDuration(duration);
        } else {
            recordingIndicator.style.display = 'none';
            streamStatus.textContent = 'Live Stream';
        }
    }

    updatePatientList(patients) {
        this.patients = patients || [];
        const patientList = document.getElementById('patientList');
        
        if (this.patients.length === 0) {
            patientList.innerHTML = '<div class="patient-item">No patients available</div>';
            return;
        }
        
        patientList.innerHTML = this.patients.map(patient => 
            `<div class="patient-item ${patient.isSelected ? 'selected' : ''}" data-patient-id="${patient.id}">
                <div><strong>${patient.name}</strong></div>
                <div style="font-size: 11px; opacity: 0.8;">${patient.id} - ${patient.modality}</div>
            </div>`
        ).join('');
        
        // Add click handlers
        patientList.querySelectorAll('.patient-item').forEach(item => {
            item.addEventListener('click', () => {
                const patientId = item.dataset.patientId;
                this.selectPatient(patientId);
            });
        });
    }

    updateQueue(queueData) {
        this.queueItems = queueData.items || [];
        const queueList = document.getElementById('queueList');
        
        if (this.queueItems.length === 0) {
            queueList.innerHTML = '<div class="queue-item">Queue empty</div>';
            return;
        }
        
        queueList.innerHTML = this.queueItems.map(item => 
            `<div class="queue-item">
                <div>${item.filename}</div>
                <span class="queue-status ${item.status}">${item.status}</span>
            </div>`
        ).join('');
    }

    handleStatusUpdate(data) {
        // Handle real-time status updates
        if (data.type === 'patient_changed') {
            this.systemStatus.currentPatient = data.patient;
        }
    }

    handleAcknowledgment(data) {
        console.log('Command acknowledged:', data);
    }

    handleError(error) {
        console.error('Server error:', error);
        this.showNotification('Error: ' + error.message, 'error');
    }

    // Control Methods
    refreshPatients() {
        this.sendMessage('refresh_patients');
        this.showNotification('Refreshing patient list...', 'info');
    }

    selectPatient(patientId) {
        this.sendMessage('select_patient', { patientId });
        this.showNotification('Switching to patient: ' + patientId, 'info');
    }

    capturePhoto() {
        this.sendMessage('capture_photo');
        this.showNotification('Capturing photo...', 'success');
    }

    markCritical() {
        this.sendMessage('mark_critical', { timestamp: Date.now() });
        this.showNotification('Critical moment marked', 'warning');
    }

    emergencyStop() {
        if (confirm('Are you sure you want to perform an emergency stop?')) {
            this.sendMessage('emergency_stop');
            this.showNotification('Emergency stop initiated', 'error');
        }
    }

    startRecording() {
        const quality = document.getElementById('qualitySlider').value;
        const audio = document.getElementById('audioSlider').value;
        
        this.sendMessage('start_recording', {
            quality: parseInt(quality),
            audioLevel: parseInt(audio)
        });
        this.showNotification('Starting recording...', 'success');
    }

    stopRecording() {
        this.sendMessage('stop_recording');
        this.showNotification('Stopping recording...', 'info');
    }

    updateQuality(type, value) {
        this.sendMessage('update_quality', {
            type: type,
            value: parseInt(value)
        });
    }

    processQueue() {
        this.sendMessage('process_queue');
        this.showNotification('Processing export queue...', 'info');
    }

    runDiagnostics() {
        this.sendMessage('run_diagnostics');
        this.showNotification('Running system diagnostics...', 'info');
    }

    exportCurrent() {
        this.sendMessage('export_current');
        this.showNotification('Exporting current session...', 'info');
    }

    clearCache() {
        if (confirm('Clear all cached data?')) {
            this.sendMessage('clear_cache');
            this.showNotification('Clearing cache...', 'warning');
        }
    }

    restartService() {
        if (confirm('Restart the capture service?')) {
            this.sendMessage('restart_service');
            this.showNotification('Restarting service...', 'warning');
        }
    }

    shutdownSystem() {
        if (confirm('Are you sure you want to shutdown the system?')) {
            this.sendMessage('shutdown_system');
            this.showNotification('Shutting down system...', 'error');
        }
    }

    // Utility Methods
    formatDuration(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        
        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    showNotification(message, type = 'info') {
        // Simple notification system - could be enhanced with a proper notification library
        console.log(`[${type.toUpperCase()}] ${message}`);
        
        // Create temporary notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 100px;
            right: 20px;
            background: ${type === 'error' ? '#ef4444' : type === 'warning' ? '#f59e0b' : type === 'success' ? '#10b981' : '#3b82f6'};
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            z-index: 1000;
            font-size: 14px;
            font-weight: 600;
            box-shadow: 0 8px 25px rgba(0,0,0,0.2);
            transform: translateX(100%);
            transition: transform 0.3s ease;
        `;
        
        document.body.appendChild(notification);
        
        // Animate in
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    }

    startStatusUpdates() {
        // Update status every 5 seconds
        setInterval(() => {
            if (this.connectionStatus === 'connected') {
                this.sendMessage('get_system_status');
            }
        }, 5000);
    }

    disconnect() {
        if (this.websocket) {
            this.websocket.close();
        }
        this.stopHeartbeat();
    }
}

// Initialize the admin interface when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.adminControl = new AdminControlInterface();
});

// Export for potential external use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminControlInterface;
}