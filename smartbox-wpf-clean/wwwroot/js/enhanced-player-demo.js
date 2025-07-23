/**
 * Enhanced Streaming Player Demo
 * Demonstration of Phase 1 Foundation Integration features
 */

class EnhancedPlayerDemo {
    constructor() {
        this.player = null;
        this.demoSessionId = 'demo-session-' + Date.now();
        this.isInitialized = false;
    }
    
    async initialize() {
        if (this.isInitialized) {
            console.log('Demo already initialized');
            return;
        }
        
        try {
            console.log('🚀 Initializing Enhanced Streaming Player Demo');
            
            // Create enhanced player instance
            const container = document.getElementById('captureArea');
            if (!container) {
                throw new Error('Capture area container not found');
            }
            
            // Initialize with demo options
            this.player = new EnhancedStreamingPlayer(container, {
                sessionId: this.demoSessionId,
                enableFFmpegIntegration: true,
                enableWebSocketUpdates: true,
                enableUnifiedThumbnails: true
            });
            
            // Setup demo event listeners
            this.setupDemoEventListeners();
            
            // Add demo controls
            this.addDemoControls();
            
            this.isInitialized = true;
            console.log('✅ Enhanced player demo initialized successfully');
            
        } catch (error) {
            console.error('❌ Failed to initialize demo:', error);
        }
    }
    
    setupDemoEventListeners() {
        if (!this.player) return;
        
        // VideoEngine events
        this.player.on('recordingStarted', (session) => {
            console.log('📹 Demo: Recording started', session);
            this.updateDemoStatus('Recording started', 'success');
        });
        
        this.player.on('recordingStopped', (result) => {
            console.log('⏹️ Demo: Recording stopped', result);
            this.updateDemoStatus('Recording stopped', 'info');
        });
        
        this.player.on('segmentCompleted', (segment) => {
            console.log('📦 Demo: New segment', segment);
            this.updateDemoStatus(`Segment ${segment.number} completed`, 'info');
        });
        
        this.player.on('thumbnailReady', (data) => {
            console.log('🖼️ Demo: Thumbnail ready', data);
            this.updateDemoStatus(`Thumbnail ready: ${data.timestamp}s`, 'info');
        });
        
        this.player.on('websocketConnected', () => {
            console.log('🔌 Demo: WebSocket connected');
            this.updateDemoStatus('Real-time updates connected', 'success');
        });
        
        this.player.on('websocketDisconnected', () => {
            console.log('🔌 Demo: WebSocket disconnected');
            this.updateDemoStatus('Connection lost, reconnecting...', 'warning');
        });
        
        this.player.on('markerAdded', (marker) => {
            console.log('📍 Demo: Marker added', marker);
            this.updateDemoStatus(`Marker added: ${marker.type}`, 'info');
        });
        
        this.player.on('snapshotTaken', (snapshot) => {
            console.log('📸 Demo: Snapshot taken', snapshot);
            this.updateDemoStatus('Snapshot captured', 'success');
        });
        
        this.player.on('streamError', (error) => {
            console.error('❌ Demo: Stream error', error);
            this.updateDemoStatus(`Error: ${error.message}`, 'error');
        });
    }
    
    addDemoControls() {
        // Create demo control panel
        const demoPanel = document.createElement('div');
        demoPanel.id = 'enhancedPlayerDemo';
        demoPanel.className = 'demo-control-panel';
        demoPanel.innerHTML = `
            <div class="demo-header">
                <h3>Enhanced Player Demo</h3>
                <div class="demo-status" id="demoStatus">Ready</div>
            </div>
            
            <div class="demo-controls">
                <div class="demo-section">
                    <h4>Recording Controls</h4>
                    <button id="demoStartRecording" class="demo-btn">Start Recording</button>
                    <button id="demoStopRecording" class="demo-btn">Stop Recording</button>
                    <button id="demoPauseRecording" class="demo-btn">Pause Recording</button>
                    <button id="demoResumeRecording" class="demo-btn">Resume Recording</button>
                </div>
                
                <div class="demo-section">
                    <h4>Enhanced Features</h4>
                    <button id="demoTakeSnapshot" class="demo-btn">Take Snapshot</button>
                    <button id="demoAddMarker" class="demo-btn">Add Marker</button>
                    <button id="demoLoadThumbnail" class="demo-btn">Load Thumbnail</button>
                </div>
                
                <div class="demo-section">
                    <h4>Connection Testing</h4>
                    <button id="demoTestWebSocket" class="demo-btn">Test WebSocket</button>
                    <button id="demoSimulateError" class="demo-btn">Simulate Error</button>
                    <button id="demoTestRecovery" class="demo-btn">Test Recovery</button>
                </div>
                
                <div class="demo-section">
                    <h4>Information</h4>
                    <button id="demoShowStats" class="demo-btn">Show Stats</button>
                    <button id="demoShowCache" class="demo-btn">Show Cache</button>
                    <button id="demoClearCache" class="demo-btn">Clear Cache</button>
                </div>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(demoPanel);
        
        // Add demo styles
        this.addDemoStyles();
        
        // Setup event listeners
        this.setupDemoControlListeners();
    }
    
    setupDemoControlListeners() {
        // Recording controls
        document.getElementById('demoStartRecording')?.addEventListener('click', () => {
            this.demoStartRecording();
        });
        
        document.getElementById('demoStopRecording')?.addEventListener('click', () => {
            this.demoStopRecording();
        });
        
        document.getElementById('demoPauseRecording')?.addEventListener('click', () => {
            this.demoPauseRecording();
        });
        
        document.getElementById('demoResumeRecording')?.addEventListener('click', () => {
            this.demoResumeRecording();
        });
        
        // Enhanced features
        document.getElementById('demoTakeSnapshot')?.addEventListener('click', () => {
            this.demoTakeSnapshot();
        });
        
        document.getElementById('demoAddMarker')?.addEventListener('click', () => {
            this.demoAddMarker();
        });
        
        document.getElementById('demoLoadThumbnail')?.addEventListener('click', () => {
            this.demoLoadThumbnail();
        });
        
        // Connection testing
        document.getElementById('demoTestWebSocket')?.addEventListener('click', () => {
            this.demoTestWebSocket();
        });
        
        document.getElementById('demoSimulateError')?.addEventListener('click', () => {
            this.demoSimulateError();
        });
        
        document.getElementById('demoTestRecovery')?.addEventListener('click', () => {
            this.demoTestRecovery();
        });
        
        // Information
        document.getElementById('demoShowStats')?.addEventListener('click', () => {
            this.demoShowStats();
        });
        
        document.getElementById('demoShowCache')?.addEventListener('click', () => {
            this.demoShowCache();
        });
        
        document.getElementById('demoClearCache')?.addEventListener('click', () => {
            this.demoClearCache();
        });
    }
    
    // Demo action methods
    async demoStartRecording() {
        try {
            this.updateDemoStatus('Starting demo recording...', 'info');
            
            const session = await this.player.startRecording({
                patientId: 'DEMO_PATIENT_001',
                studyId: 'DEMO_STUDY_001',
                lossless: true,
                frameRate: 30,
                preRecordSeconds: 30
            });
            
            console.log('Demo recording started:', session);
            
        } catch (error) {
            this.updateDemoStatus(`Recording failed: ${error.message}`, 'error');
            console.error('Demo recording failed:', error);
        }
    }
    
    async demoStopRecording() {
        try {
            this.updateDemoStatus('Stopping demo recording...', 'info');
            
            const result = await this.player.stopRecording();
            
            console.log('Demo recording stopped:', result);
            
        } catch (error) {
            this.updateDemoStatus(`Stop failed: ${error.message}`, 'error');
            console.error('Demo stop failed:', error);
        }
    }
    
    async demoPauseRecording() {
        const success = await this.player.pauseRecording();
        this.updateDemoStatus(success ? 'Recording paused' : 'Pause failed', success ? 'info' : 'error');
    }
    
    async demoResumeRecording() {
        const success = await this.player.resumeRecording();
        this.updateDemoStatus(success ? 'Recording resumed' : 'Resume failed', success ? 'info' : 'error');
    }
    
    async demoTakeSnapshot() {
        const snapshot = await this.player.takeSnapshot('JPEG');
        if (snapshot) {
            this.updateDemoStatus('Snapshot captured successfully', 'success');
            console.log('Demo snapshot:', snapshot);
        } else {
            this.updateDemoStatus('Snapshot failed', 'error');
        }
    }
    
    async demoAddMarker() {
        const timestamp = Date.now() / 1000; // Current time in seconds
        const marker = await this.player.addMarker(timestamp, 'Important', 'Demo marker added');
        
        if (marker) {
            this.updateDemoStatus(`Marker added at ${timestamp.toFixed(1)}s`, 'success');
            console.log('Demo marker:', marker);
        } else {
            this.updateDemoStatus('Marker add failed', 'error');
        }
    }
    
    async demoLoadThumbnail() {
        try {
            this.updateDemoStatus('Loading demo thumbnail...', 'info');
            
            const timestamp = 10; // 10 seconds
            const thumbnail = await this.player.loadThumbnail(timestamp, 160);
            
            if (thumbnail) {
                this.updateDemoStatus('Thumbnail loaded successfully', 'success');
                console.log('Demo thumbnail:', thumbnail);
                
                // Show thumbnail in a popup for demo
                this.showThumbnailPreview(thumbnail, timestamp);
            } else {
                this.updateDemoStatus('Thumbnail load failed', 'error');
            }
            
        } catch (error) {
            this.updateDemoStatus(`Thumbnail error: ${error.message}`, 'error');
            console.error('Demo thumbnail error:', error);
        }
    }
    
    demoTestWebSocket() {
        if (this.player.wsConnected) {
            this.updateDemoStatus('WebSocket already connected', 'success');
        } else {
            this.updateDemoStatus('Testing WebSocket connection...', 'info');
            // Trigger reconnection
            this.player.connectToSession(this.demoSessionId);
        }
    }
    
    demoSimulateError() {
        this.updateDemoStatus('Simulating error...', 'warning');
        
        // Simulate different types of errors
        const errorTypes = [
            new Error('Demo network error'),
            new Error('Demo media error'),
            new Error('Demo WebSocket connection failed')
        ];
        
        const randomError = errorTypes[Math.floor(Math.random() * errorTypes.length)];
        randomError.code = 'DEMO_ERROR';
        
        this.player.handleStreamError(randomError);
    }
    
    async demoTestRecovery() {
        if (this.player.errorRecovery) {
            this.updateDemoStatus('Testing error recovery...', 'info');
            
            const testError = new Error('Demo recovery test');
            testError.code = 'NETWORK_ERROR';
            
            const recovered = await this.player.errorRecovery.handleError(testError, {
                context: 'demo_test'
            });
            
            this.updateDemoStatus(
                recovered ? 'Recovery test passed' : 'Recovery test failed',
                recovered ? 'success' : 'error'
            );
        } else {
            this.updateDemoStatus('Error recovery not available', 'warning');
        }
    }
    
    demoShowStats() {
        const stats = {
            sessionId: this.player.currentSessionId,
            isRecording: this.player.isRecording,
            wsConnected: this.player.wsConnected,
            thumbnailCacheSize: this.player.getThumbnailCacheSize(),
            realTimeEnabled: this.player.realTimeFeaturesEnabled,
            errorRecoveryActive: !!this.player.errorRecovery
        };
        
        console.log('Enhanced Player Stats:', stats);
        this.updateDemoStatus(`Stats logged to console`, 'info');
        
        // Show in alert for demo
        alert(`Enhanced Player Stats:\n${JSON.stringify(stats, null, 2)}`);
    }
    
    demoShowCache() {
        const cacheSize = this.player.getThumbnailCacheSize();
        console.log('Thumbnail cache size:', cacheSize);
        
        if (this.player.errorRecovery) {
            const errorStats = this.player.errorRecovery.getErrorStats();
            console.log('Error recovery stats:', errorStats);
        }
        
        this.updateDemoStatus(`Cache info logged (${cacheSize} thumbnails)`, 'info');
    }
    
    demoClearCache() {
        this.player.clearThumbnailCache();
        
        if (this.player.errorRecovery) {
            this.player.errorRecovery.clearErrorHistory();
        }
        
        this.updateDemoStatus('Caches cleared', 'info');
    }
    
    // UI helper methods
    updateDemoStatus(message, type = 'info') {
        const statusElement = document.getElementById('demoStatus');
        if (statusElement) {
            statusElement.textContent = message;
            statusElement.className = `demo-status demo-status-${type}`;
        }
        
        console.log(`Demo Status [${type.toUpperCase()}]: ${message}`);
    }
    
    showThumbnailPreview(thumbnailUrl, timestamp) {
        // Create preview popup
        const popup = document.createElement('div');
        popup.className = 'thumbnail-preview-popup';
        popup.innerHTML = `
            <div class="popup-content">
                <h4>Thumbnail Preview</h4>
                <p>Timestamp: ${timestamp}s</p>
                <img src="${thumbnailUrl}" alt="Thumbnail at ${timestamp}s" style="max-width: 200px; max-height: 150px;">
                <button onclick="this.parentElement.parentElement.remove()">Close</button>
            </div>
        `;
        
        document.body.appendChild(popup);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            popup.remove();
        }, 5000);
    }
    
    addDemoStyles() {
        const style = document.createElement('style');
        style.textContent = `
            .demo-control-panel {
                position: fixed;
                top: 20px;
                right: 20px;
                width: 300px;
                background: rgba(0, 0, 0, 0.9);
                border: 1px solid #333;
                border-radius: 8px;
                padding: 15px;
                color: white;
                font-family: monospace;
                font-size: 12px;
                z-index: 10000;
                max-height: 80vh;
                overflow-y: auto;
            }
            
            .demo-header h3 {
                margin: 0 0 10px 0;
                color: #4CAF50;
                font-size: 14px;
            }
            
            .demo-status {
                padding: 5px 8px;
                border-radius: 4px;
                margin-bottom: 10px;
                font-weight: bold;
                font-size: 11px;
            }
            
            .demo-status-info { background: #2196F3; color: white; }
            .demo-status-success { background: #4CAF50; color: white; }
            .demo-status-warning { background: #FF9800; color: black; }
            .demo-status-error { background: #F44336; color: white; }
            
            .demo-section {
                margin-bottom: 15px;
                border-bottom: 1px solid #333;
                padding-bottom: 10px;
            }
            
            .demo-section:last-child {
                border-bottom: none;
            }
            
            .demo-section h4 {
                margin: 0 0 8px 0;
                color: #FFC107;
                font-size: 12px;
            }
            
            .demo-btn {
                display: block;
                width: 100%;
                margin-bottom: 5px;
                padding: 6px 10px;
                background: #333;
                border: 1px solid #555;
                border-radius: 4px;
                color: white;
                cursor: pointer;
                font-size: 11px;
                transition: background 0.2s;
            }
            
            .demo-btn:hover {
                background: #555;
            }
            
            .demo-btn:active {
                background: #666;
            }
            
            .thumbnail-preview-popup {
                position: fixed;
                top: 50%;
                left: 50%;
                transform: translate(-50%, -50%);
                background: rgba(0, 0, 0, 0.9);
                border: 1px solid #333;
                border-radius: 8px;
                padding: 20px;
                color: white;
                z-index: 10001;
                text-align: center;
            }
            
            .popup-content h4 {
                margin: 0 0 10px 0;
                color: #4CAF50;
            }
            
            .popup-content button {
                margin-top: 10px;
                padding: 5px 15px;
                background: #333;
                border: 1px solid #555;
                border-radius: 4px;
                color: white;
                cursor: pointer;
            }
        `;
        
        document.head.appendChild(style);
    }
}

// Auto-initialize demo when enhanced player is available
window.addEventListener('load', () => {
    // Wait a bit for all components to load
    setTimeout(() => {
        if (window.EnhancedStreamingPlayer) {
            const demo = new EnhancedPlayerDemo();
            demo.initialize();
            
            // Make demo available globally for manual testing
            window.enhancedPlayerDemo = demo;
            
            console.log('🎮 Enhanced Player Demo available at window.enhancedPlayerDemo');
        }
    }, 1000);
});

// Export for manual use
window.EnhancedPlayerDemo = EnhancedPlayerDemo;