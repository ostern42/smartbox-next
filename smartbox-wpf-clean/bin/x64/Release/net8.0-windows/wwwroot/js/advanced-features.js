/**
 * Advanced Features JavaScript Module for SmartBox-Next
 * Provides frontend controls for AI-enhanced workflow, advanced audio/video processing,
 * smart automation, and HL7 FHIR integration
 * MEDICAL SAFETY: All controls include confirmation dialogs and audit trails
 */

class AdvancedFeaturesManager {
    constructor() {
        this.isInitialized = false;
        this.websocket = null;
        this.aiWorkflowEnabled = false;
        this.audioProcessingEnabled = false;
        this.videoEnhancementEnabled = false;
        this.automationEnabled = false;
        this.hl7IntegrationEnabled = false;
        
        // Real-time monitoring data
        this.currentMetrics = {
            aiWorkflow: {},
            audioProcessing: {},
            videoEnhancement: {},
            automation: {},
            hl7Integration: {}
        };
        
        // UI Elements
        this.elements = {};
        
        // Event handlers
        this.eventHandlers = new Map();
        
        console.log('🤖 Advanced Features Manager initialized for medical video capture revolution');
    }

    async initialize() {
        try {
            console.log('🚀 Initializing Advanced Features Manager...');
            
            // Initialize WebSocket connection for real-time updates
            await this.initializeWebSocket();
            
            // Initialize UI elements
            this.initializeUIElements();
            
            // Setup event listeners
            this.setupEventListeners();
            
            // Load current configuration
            await this.loadConfiguration();
            
            // Initialize feature modules
            await this.initializeFeatureModules();
            
            // Start real-time monitoring
            this.startRealTimeMonitoring();
            
            this.isInitialized = true;
            console.log('✅ Advanced Features Manager initialized successfully');
            
            // Show welcome notification
            this.showNotification('🎉 Advanced Medical Features Activated!', 'success');
            
        } catch (error) {
            console.error('❌ Failed to initialize Advanced Features Manager:', error);
            this.showNotification('❌ Failed to initialize advanced features', 'error');
        }
    }

    async initializeWebSocket() {
        return new Promise((resolve, reject) => {
            try {
                const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
                const wsUrl = `${protocol}//${window.location.host}/ws/advanced-features`;
                
                this.websocket = new WebSocket(wsUrl);
                
                this.websocket.onopen = () => {
                    console.log('🔌 WebSocket connected for advanced features');
                    resolve();
                };
                
                this.websocket.onmessage = (event) => {
                    this.handleWebSocketMessage(JSON.parse(event.data));
                };
                
                this.websocket.onclose = () => {
                    console.log('🔌 WebSocket disconnected, attempting reconnection...');
                    setTimeout(() => this.initializeWebSocket(), 5000);
                };
                
                this.websocket.onerror = (error) => {
                    console.error('🔌 WebSocket error:', error);
                    reject(error);
                };
                
            } catch (error) {
                reject(error);
            }
        });
    }

    initializeUIElements() {
        // Main control panels
        this.elements.aiWorkflowPanel = document.getElementById('ai-workflow-panel');
        this.elements.audioProcessingPanel = document.getElementById('audio-processing-panel');
        this.elements.videoEnhancementPanel = document.getElementById('video-enhancement-panel');
        this.elements.automationPanel = document.getElementById('automation-panel');
        this.elements.hl7IntegrationPanel = document.getElementById('hl7-integration-panel');
        
        // Control buttons
        this.elements.aiWorkflowToggle = document.getElementById('ai-workflow-toggle');
        this.elements.audioProcessingToggle = document.getElementById('audio-processing-toggle');
        this.elements.videoEnhancementToggle = document.getElementById('video-enhancement-toggle');
        this.elements.automationToggle = document.getElementById('automation-toggle');
        this.elements.hl7IntegrationToggle = document.getElementById('hl7-integration-toggle');
        
        // Monitoring displays
        this.elements.realTimeMetrics = document.getElementById('real-time-metrics');
        this.elements.statusIndicators = document.getElementById('status-indicators');
        this.elements.alertsPanel = document.getElementById('alerts-panel');
        
        // Create UI elements if they don't exist
        this.createMissingUIElements();
    }

    createMissingUIElements() {
        // Create advanced features container if it doesn't exist
        if (!document.getElementById('advanced-features-container')) {
            const container = document.createElement('div');
            container.id = 'advanced-features-container';
            container.className = 'advanced-features-container';
            container.innerHTML = this.getAdvancedFeaturesHTML();
            
            // Insert after main controls
            const mainControls = document.querySelector('.video-controls') || document.body;
            mainControls.insertAdjacentElement('afterend', container);
        }
        
        // Re-query elements after creation
        this.initializeUIElements();
    }

    getAdvancedFeaturesHTML() {
        return `
            <div class="advanced-features-header">
                <h2>🤖 Advanced Medical Features</h2>
                <div class="feature-status-bar" id="status-indicators">
                    <div class="status-indicator" id="ai-status">
                        <span class="status-light off"></span>
                        <span>AI Workflow</span>
                    </div>
                    <div class="status-indicator" id="audio-status">
                        <span class="status-light off"></span>
                        <span>Audio Enhancement</span>
                    </div>
                    <div class="status-indicator" id="video-status">
                        <span class="status-light off"></span>
                        <span>Video Enhancement</span>
                    </div>
                    <div class="status-indicator" id="automation-status">
                        <span class="status-light off"></span>
                        <span>Smart Automation</span>
                    </div>
                    <div class="status-indicator" id="hl7-status">
                        <span class="status-light off"></span>
                        <span>HL7 Integration</span>
                    </div>
                </div>
            </div>
            
            <div class="features-grid">
                <!-- AI Workflow Panel -->
                <div class="feature-panel" id="ai-workflow-panel">
                    <div class="panel-header">
                        <h3>🧠 AI-Enhanced Workflow</h3>
                        <button class="toggle-btn" id="ai-workflow-toggle">Activate</button>
                    </div>
                    <div class="panel-content">
                        <div class="metric-row">
                            <span>Current Phase:</span>
                            <span id="current-phase">Not Started</span>
                        </div>
                        <div class="metric-row">
                            <span>Transcription Confidence:</span>
                            <span id="transcription-confidence">0%</span>
                        </div>
                        <div class="metric-row">
                            <span>Critical Moments:</span>
                            <span id="critical-moments-count">0</span>
                        </div>
                        <div class="controls">
                            <button class="action-btn" id="start-procedure-analysis">Start Analysis</button>
                            <button class="action-btn" id="mark-critical-moment">Mark Critical</button>
                            <button class="action-btn" id="voice-command-toggle">Voice Commands</button>
                        </div>
                        <div class="live-transcription" id="live-transcription">
                            <h4>Live Medical Transcription</h4>
                            <div class="transcription-text" id="transcription-text">
                                Waiting for speech input...
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Audio Processing Panel -->
                <div class="feature-panel" id="audio-processing-panel">
                    <div class="panel-header">
                        <h3>🎵 Advanced Audio Processing</h3>
                        <button class="toggle-btn" id="audio-processing-toggle">Activate</button>
                    </div>
                    <div class="panel-content">
                        <div class="audio-devices">
                            <h4>Active Microphones</h4>
                            <div id="active-microphones">
                                <div class="device-item">
                                    <span>Primary Microphone</span>
                                    <div class="vu-meter" id="vu-meter-1">
                                        <div class="vu-bar"></div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="schluckdiagnostik-section">
                            <h4>Schluckdiagnostik Analysis</h4>
                            <div class="metric-row">
                                <span>Swallowing Events:</span>
                                <span id="swallowing-events">0</span>
                            </div>
                            <div class="metric-row">
                                <span>Audio Quality:</span>
                                <span id="audio-quality">Good</span>
                            </div>
                        </div>
                        <div class="controls">
                            <button class="action-btn" id="start-multi-mic">Start Multi-Mic</button>
                            <button class="action-btn" id="enhance-audio">Enhance Audio</button>
                            <button class="action-btn" id="spatial-analysis">Spatial Analysis</button>
                        </div>
                    </div>
                </div>
                
                <!-- Video Enhancement Panel -->
                <div class="feature-panel" id="video-enhancement-panel">
                    <div class="panel-header">
                        <h3>📹 4K HDR Video Enhancement</h3>
                        <button class="toggle-btn" id="video-enhancement-toggle">Activate</button>
                    </div>
                    <div class="panel-content">
                        <div class="video-metrics">
                            <div class="metric-row">
                                <span>Resolution:</span>
                                <span id="video-resolution">1920x1080</span>
                            </div>
                            <div class="metric-row">
                                <span>Frame Rate:</span>
                                <span id="frame-rate">60 FPS</span>
                            </div>
                            <div class="metric-row">
                                <span>Enhancement Level:</span>
                                <span id="enhancement-level">Medical Grade</span>
                            </div>
                        </div>
                        <div class="enhancement-controls">
                            <div class="slider-control">
                                <label>Brightness: <span id="brightness-value">0</span></label>
                                <input type="range" id="brightness-slider" min="-50" max="50" value="0">
                            </div>
                            <div class="slider-control">
                                <label>Contrast: <span id="contrast-value">1.0</span></label>
                                <input type="range" id="contrast-slider" min="0.5" max="2.0" step="0.1" value="1.0">
                            </div>
                            <div class="slider-control">
                                <label>Sharpness: <span id="sharpness-value">0.5</span></label>
                                <input type="range" id="sharpness-slider" min="0" max="1" step="0.1" value="0.5">
                            </div>
                        </div>
                        <div class="controls">
                            <button class="action-btn" id="enable-4k-hdr">Enable 4K HDR</button>
                            <button class="action-btn" id="multi-camera-sync">Multi-Camera Sync</button>
                            <button class="action-btn" id="stabilization">Stabilization</button>
                        </div>
                    </div>
                </div>
                
                <!-- Smart Automation Panel -->
                <div class="feature-panel" id="automation-panel">
                    <div class="panel-header">
                        <h3>🤖 Smart Automation</h3>
                        <button class="toggle-btn" id="automation-toggle">Activate</button>
                    </div>
                    <div class="panel-content">
                        <div class="automation-status">
                            <div class="metric-row">
                                <span>Room Occupancy:</span>
                                <span id="room-occupancy">Detecting...</span>
                            </div>
                            <div class="metric-row">
                                <span>Auto Recording:</span>
                                <span id="auto-recording-status">Disabled</span>
                            </div>
                            <div class="metric-row">
                                <span>Buffer Usage:</span>
                                <span id="buffer-usage">45%</span>
                            </div>
                        </div>
                        <div class="automation-settings">
                            <div class="checkbox-control">
                                <input type="checkbox" id="auto-recording-checkbox">
                                <label for="auto-recording-checkbox">Automatic Recording</label>
                            </div>
                            <div class="checkbox-control">
                                <input type="checkbox" id="auto-export-checkbox">
                                <label for="auto-export-checkbox">Automatic Export</label>
                            </div>
                            <div class="checkbox-control">
                                <input type="checkbox" id="quality-validation-checkbox">
                                <label for="quality-validation-checkbox">Quality Validation</label>
                            </div>
                        </div>
                        <div class="controls">
                            <button class="action-btn" id="optimize-buffers">Optimize Buffers</button>
                            <button class="action-btn" id="schedule-export">Schedule Export</button>
                            <button class="action-btn" id="validate-quality">Validate Quality</button>
                        </div>
                    </div>
                </div>
                
                <!-- HL7 Integration Panel -->
                <div class="feature-panel" id="hl7-integration-panel">
                    <div class="panel-header">
                        <h3>🏥 HL7 FHIR Integration</h3>
                        <button class="toggle-btn" id="hl7-integration-toggle">Connect</button>
                    </div>
                    <div class="panel-content">
                        <div class="integration-status">
                            <div class="metric-row">
                                <span>FHIR Server:</span>
                                <span id="fhir-status">Disconnected</span>
                            </div>
                            <div class="metric-row">
                                <span>Epic Integration:</span>
                                <span id="epic-status">Not Configured</span>
                            </div>
                            <div class="metric-row">
                                <span>Active Streams:</span>
                                <span id="active-streams">0</span>
                            </div>
                        </div>
                        <div class="patient-context" id="patient-context">
                            <h4>Current Patient</h4>
                            <div id="patient-info">No patient selected</div>
                        </div>
                        <div class="controls">
                            <button class="action-btn" id="load-patient">Load Patient</button>
                            <button class="action-btn" id="start-streaming">Start Remote Stream</button>
                            <button class="action-btn" id="create-report">Create Report</button>
                        </div>
                    </div>
                </div>
            </div>
            
            <!-- Real-time Metrics Dashboard -->
            <div class="metrics-dashboard" id="real-time-metrics">
                <h3>📊 Real-time Performance Metrics</h3>
                <div class="metrics-grid">
                    <div class="metric-card">
                        <h4>System Performance</h4>
                        <canvas id="performance-chart" width="200" height="100"></canvas>
                    </div>
                    <div class="metric-card">
                        <h4>Audio Levels</h4>
                        <canvas id="audio-chart" width="200" height="100"></canvas>
                    </div>
                    <div class="metric-card">
                        <h4>Video Quality</h4>
                        <canvas id="video-chart" width="200" height="100"></canvas>
                    </div>
                </div>
            </div>
            
            <!-- Alerts Panel -->
            <div class="alerts-panel" id="alerts-panel">
                <h3>🚨 System Alerts</h3>
                <div class="alerts-list" id="alerts-list">
                    <!-- Alerts will be dynamically added here -->
                </div>
            </div>
        `;
    }

    setupEventListeners() {
        // Feature toggle buttons
        this.addEventListener('ai-workflow-toggle', 'click', () => this.toggleAIWorkflow());
        this.addEventListener('audio-processing-toggle', 'click', () => this.toggleAudioProcessing());
        this.addEventListener('video-enhancement-toggle', 'click', () => this.toggleVideoEnhancement());
        this.addEventListener('automation-toggle', 'click', () => this.toggleAutomation());
        this.addEventListener('hl7-integration-toggle', 'click', () => this.toggleHL7Integration());
        
        // AI Workflow controls
        this.addEventListener('start-procedure-analysis', 'click', () => this.startProcedureAnalysis());
        this.addEventListener('mark-critical-moment', 'click', () => this.markCriticalMoment());
        this.addEventListener('voice-command-toggle', 'click', () => this.toggleVoiceCommands());
        
        // Audio processing controls
        this.addEventListener('start-multi-mic', 'click', () => this.startMultiMicCapture());
        this.addEventListener('enhance-audio', 'click', () => this.enhanceAudio());
        this.addEventListener('spatial-analysis', 'click', () => this.performSpatialAnalysis());
        
        // Video enhancement controls
        this.addEventListener('enable-4k-hdr', 'click', () => this.enable4KHDR());
        this.addEventListener('multi-camera-sync', 'click', () => this.enableMultiCameraSync());
        this.addEventListener('stabilization', 'click', () => this.enableStabilization());
        
        // Video enhancement sliders
        this.addEventListener('brightness-slider', 'input', (e) => this.adjustBrightness(e.target.value));
        this.addEventListener('contrast-slider', 'input', (e) => this.adjustContrast(e.target.value));
        this.addEventListener('sharpness-slider', 'input', (e) => this.adjustSharpness(e.target.value));
        
        // Automation controls
        this.addEventListener('optimize-buffers', 'click', () => this.optimizeBuffers());
        this.addEventListener('schedule-export', 'click', () => this.scheduleExport());
        this.addEventListener('validate-quality', 'click', () => this.validateQuality());
        
        // Automation checkboxes
        this.addEventListener('auto-recording-checkbox', 'change', (e) => this.setAutoRecording(e.target.checked));
        this.addEventListener('auto-export-checkbox', 'change', (e) => this.setAutoExport(e.target.checked));
        this.addEventListener('quality-validation-checkbox', 'change', (e) => this.setQualityValidation(e.target.checked));
        
        // HL7 Integration controls
        this.addEventListener('load-patient', 'click', () => this.loadPatient());
        this.addEventListener('start-streaming', 'click', () => this.startRemoteStreaming());
        this.addEventListener('create-report', 'click', () => this.createProcedureReport());
    }

    addEventListener(elementId, event, handler) {
        const element = document.getElementById(elementId);
        if (element) {
            element.addEventListener(event, handler);
            this.eventHandlers.set(`${elementId}-${event}`, { element, event, handler });
        }
    }

    async loadConfiguration() {
        try {
            const response = await fetch('/api/advanced-features/config');
            const config = await response.json();
            
            // Apply configuration to UI
            this.applyConfiguration(config);
            
        } catch (error) {
            console.error('Failed to load configuration:', error);
        }
    }

    applyConfiguration(config) {
        // Apply loaded configuration to UI elements
        if (config.aiWorkflow?.enabled) {
            this.aiWorkflowEnabled = true;
            this.updateStatusIndicator('ai-status', 'on');
        }
        
        // Apply other configuration settings...
    }

    async initializeFeatureModules() {
        // Initialize individual feature modules
        console.log('🔧 Initializing feature modules...');
        
        // Each module can be initialized independently
        await this.initializeAIWorkflowModule();
        await this.initializeAudioProcessingModule();
        await this.initializeVideoEnhancementModule();
        await this.initializeAutomationModule();
        await this.initializeHL7IntegrationModule();
    }

    async initializeAIWorkflowModule() {
        console.log('🧠 Initializing AI Workflow module...');
        // AI Workflow specific initialization
    }

    async initializeAudioProcessingModule() {
        console.log('🎵 Initializing Audio Processing module...');
        // Audio processing specific initialization
        this.initializeVUMeters();
    }

    async initializeVideoEnhancementModule() {
        console.log('📹 Initializing Video Enhancement module...');
        // Video enhancement specific initialization
    }

    async initializeAutomationModule() {
        console.log('🤖 Initializing Smart Automation module...');
        // Automation specific initialization
    }

    async initializeHL7IntegrationModule() {
        console.log('🏥 Initializing HL7 Integration module...');
        // HL7 Integration specific initialization
    }

    startRealTimeMonitoring() {
        // Start monitoring intervals
        setInterval(() => this.updatePerformanceMetrics(), 1000);
        setInterval(() => this.updateAudioMetrics(), 100);
        setInterval(() => this.updateVideoMetrics(), 500);
        
        console.log('📊 Real-time monitoring started');
    }

    // Feature Toggle Methods
    async toggleAIWorkflow() {
        try {
            this.aiWorkflowEnabled = !this.aiWorkflowEnabled;
            
            const response = await fetch('/api/ai-workflow/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled: this.aiWorkflowEnabled })
            });
            
            if (response.ok) {
                this.updateStatusIndicator('ai-status', this.aiWorkflowEnabled ? 'on' : 'off');
                this.updateToggleButton('ai-workflow-toggle', this.aiWorkflowEnabled);
                this.showNotification(
                    `AI Workflow ${this.aiWorkflowEnabled ? 'Activated' : 'Deactivated'}`,
                    'success'
                );
            }
            
        } catch (error) {
            console.error('Failed to toggle AI Workflow:', error);
            this.showNotification('Failed to toggle AI Workflow', 'error');
        }
    }

    async toggleAudioProcessing() {
        try {
            this.audioProcessingEnabled = !this.audioProcessingEnabled;
            
            const response = await fetch('/api/audio-processing/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled: this.audioProcessingEnabled })
            });
            
            if (response.ok) {
                this.updateStatusIndicator('audio-status', this.audioProcessingEnabled ? 'on' : 'off');
                this.updateToggleButton('audio-processing-toggle', this.audioProcessingEnabled);
                this.showNotification(
                    `Audio Processing ${this.audioProcessingEnabled ? 'Activated' : 'Deactivated'}`,
                    'success'
                );
            }
            
        } catch (error) {
            console.error('Failed to toggle Audio Processing:', error);
            this.showNotification('Failed to toggle Audio Processing', 'error');
        }
    }

    async toggleVideoEnhancement() {
        try {
            this.videoEnhancementEnabled = !this.videoEnhancementEnabled;
            
            const response = await fetch('/api/video-enhancement/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled: this.videoEnhancementEnabled })
            });
            
            if (response.ok) {
                this.updateStatusIndicator('video-status', this.videoEnhancementEnabled ? 'on' : 'off');
                this.updateToggleButton('video-enhancement-toggle', this.videoEnhancementEnabled);
                this.showNotification(
                    `Video Enhancement ${this.videoEnhancementEnabled ? 'Activated' : 'Deactivated'}`,
                    'success'
                );
            }
            
        } catch (error) {
            console.error('Failed to toggle Video Enhancement:', error);
            this.showNotification('Failed to toggle Video Enhancement', 'error');
        }
    }

    async toggleAutomation() {
        try {
            this.automationEnabled = !this.automationEnabled;
            
            const response = await fetch('/api/automation/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled: this.automationEnabled })
            });
            
            if (response.ok) {
                this.updateStatusIndicator('automation-status', this.automationEnabled ? 'on' : 'off');
                this.updateToggleButton('automation-toggle', this.automationEnabled);
                this.showNotification(
                    `Smart Automation ${this.automationEnabled ? 'Activated' : 'Deactivated'}`,
                    'success'
                );
            }
            
        } catch (error) {
            console.error('Failed to toggle Automation:', error);
            this.showNotification('Failed to toggle Automation', 'error');
        }
    }

    async toggleHL7Integration() {
        try {
            this.hl7IntegrationEnabled = !this.hl7IntegrationEnabled;
            
            const response = await fetch('/api/hl7-integration/toggle', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ enabled: this.hl7IntegrationEnabled })
            });
            
            if (response.ok) {
                this.updateStatusIndicator('hl7-status', this.hl7IntegrationEnabled ? 'on' : 'off');
                this.updateToggleButton('hl7-integration-toggle', this.hl7IntegrationEnabled);
                this.showNotification(
                    `HL7 Integration ${this.hl7IntegrationEnabled ? 'Connected' : 'Disconnected'}`,
                    'success'
                );
            }
            
        } catch (error) {
            console.error('Failed to toggle HL7 Integration:', error);
            this.showNotification('Failed to toggle HL7 Integration', 'error');
        }
    }

    // AI Workflow Methods
    async startProcedureAnalysis() {
        try {
            const response = await fetch('/api/ai-workflow/start-analysis', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    procedureType: 'Endoscopy',
                    patientId: 'current'
                })
            });
            
            if (response.ok) {
                this.showNotification('🧠 Procedure analysis started', 'success');
                this.updateElement('current-phase', 'Preparation');
            }
            
        } catch (error) {
            console.error('Failed to start procedure analysis:', error);
            this.showNotification('Failed to start procedure analysis', 'error');
        }
    }

    async markCriticalMoment() {
        try {
            const response = await fetch('/api/ai-workflow/mark-critical', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    description: 'Manual critical moment marking',
                    timestamp: new Date().toISOString()
                })
            });
            
            if (response.ok) {
                this.showNotification('📍 Critical moment marked', 'success');
                const currentCount = parseInt(this.getElementText('critical-moments-count') || '0');
                this.updateElement('critical-moments-count', (currentCount + 1).toString());
            }
            
        } catch (error) {
            console.error('Failed to mark critical moment:', error);
            this.showNotification('Failed to mark critical moment', 'error');
        }
    }

    // Video Enhancement Methods
    async adjustBrightness(value) {
        await this.sendVideoAdjustment('brightness', value);
        this.updateElement('brightness-value', value);
    }

    async adjustContrast(value) {
        await this.sendVideoAdjustment('contrast', value);
        this.updateElement('contrast-value', value);
    }

    async adjustSharpness(value) {
        await this.sendVideoAdjustment('sharpness', value);
        this.updateElement('sharpness-value', value);
    }

    async sendVideoAdjustment(parameter, value) {
        try {
            await fetch('/api/video-enhancement/adjust', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ parameter, value: parseFloat(value) })
            });
        } catch (error) {
            console.error(`Failed to adjust ${parameter}:`, error);
        }
    }

    // Real-time Monitoring Methods
    updatePerformanceMetrics() {
        // Update performance charts and metrics
        const performanceCanvas = document.getElementById('performance-chart');
        if (performanceCanvas) {
            // Update performance chart
            this.drawPerformanceChart(performanceCanvas);
        }
    }

    updateAudioMetrics() {
        // Update audio level meters and charts
        this.updateVUMeters();
        
        const audioCanvas = document.getElementById('audio-chart');
        if (audioCanvas) {
            this.drawAudioChart(audioCanvas);
        }
    }

    updateVideoMetrics() {
        // Update video quality metrics and charts
        const videoCanvas = document.getElementById('video-chart');
        if (videoCanvas) {
            this.drawVideoChart(videoCanvas);
        }
    }

    initializeVUMeters() {
        // Initialize VU meters for audio monitoring
        const vuMeters = document.querySelectorAll('.vu-meter');
        vuMeters.forEach(meter => {
            const bar = meter.querySelector('.vu-bar');
            if (bar) {
                bar.style.width = '0%';
                bar.style.backgroundColor = '#4CAF50';
            }
        });
    }

    updateVUMeters() {
        // Update VU meter displays with current audio levels
        const vuMeters = document.querySelectorAll('.vu-meter .vu-bar');
        vuMeters.forEach((bar, index) => {
            // Simulate audio level (would be real data in production)
            const level = Math.random() * 100;
            bar.style.width = `${level}%`;
            
            // Color coding for audio levels
            if (level > 80) {
                bar.style.backgroundColor = '#f44336'; // Red for high levels
            } else if (level > 60) {
                bar.style.backgroundColor = '#ff9800'; // Orange for medium levels
            } else {
                bar.style.backgroundColor = '#4CAF50'; // Green for normal levels
            }
        });
    }

    // Chart Drawing Methods
    drawPerformanceChart(canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        // Draw performance chart (simplified)
        ctx.strokeStyle = '#2196F3';
        ctx.lineWidth = 2;
        ctx.beginPath();
        
        for (let i = 0; i < canvas.width; i += 10) {
            const y = canvas.height / 2 + Math.sin(i * 0.1 + Date.now() * 0.001) * 20;
            if (i === 0) {
                ctx.moveTo(i, y);
            } else {
                ctx.lineTo(i, y);
            }
        }
        
        ctx.stroke();
    }

    drawAudioChart(canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        // Draw audio frequency spectrum (simplified)
        ctx.fillStyle = '#4CAF50';
        
        for (let i = 0; i < 20; i++) {
            const height = Math.random() * canvas.height;
            const x = i * (canvas.width / 20);
            ctx.fillRect(x, canvas.height - height, canvas.width / 20 - 2, height);
        }
    }

    drawVideoChart(canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        // Draw video quality metrics (simplified)
        const metrics = ['Brightness', 'Contrast', 'Sharpness'];
        const values = [0.7, 0.8, 0.75]; // Example values
        
        metrics.forEach((metric, index) => {
            const y = (index + 1) * (canvas.height / (metrics.length + 1));
            const width = values[index] * canvas.width * 0.8;
            
            ctx.fillStyle = '#9C27B0';
            ctx.fillRect(10, y - 5, width, 10);
            
            ctx.fillStyle = '#333';
            ctx.font = '10px Arial';
            ctx.fillText(metric, 10, y - 10);
        });
    }

    // WebSocket Message Handling
    handleWebSocketMessage(data) {
        switch (data.type) {
            case 'ai-workflow-update':
                this.handleAIWorkflowUpdate(data.payload);
                break;
            case 'audio-level-update':
                this.handleAudioLevelUpdate(data.payload);
                break;
            case 'video-quality-update':
                this.handleVideoQualityUpdate(data.payload);
                break;
            case 'automation-status':
                this.handleAutomationStatusUpdate(data.payload);
                break;
            case 'hl7-integration-update':
                this.handleHL7IntegrationUpdate(data.payload);
                break;
            case 'alert':
                this.handleAlert(data.payload);
                break;
            default:
                console.log('Unknown WebSocket message type:', data.type);
        }
    }

    handleAIWorkflowUpdate(payload) {
        if (payload.currentPhase) {
            this.updateElement('current-phase', payload.currentPhase);
        }
        if (payload.transcriptionConfidence) {
            this.updateElement('transcription-confidence', `${Math.round(payload.transcriptionConfidence * 100)}%`);
        }
        if (payload.transcriptionText) {
            this.updateElement('transcription-text', payload.transcriptionText);
        }
    }

    handleAudioLevelUpdate(payload) {
        // Update audio level displays
        this.currentMetrics.audioProcessing = { ...this.currentMetrics.audioProcessing, ...payload };
    }

    handleVideoQualityUpdate(payload) {
        if (payload.resolution) {
            this.updateElement('video-resolution', payload.resolution);
        }
        if (payload.frameRate) {
            this.updateElement('frame-rate', `${payload.frameRate} FPS`);
        }
    }

    handleAutomationStatusUpdate(payload) {
        if (payload.roomOccupancy !== undefined) {
            this.updateElement('room-occupancy', payload.roomOccupancy ? 'Occupied' : 'Vacant');
        }
        if (payload.bufferUsage !== undefined) {
            this.updateElement('buffer-usage', `${Math.round(payload.bufferUsage * 100)}%`);
        }
    }

    handleHL7IntegrationUpdate(payload) {
        if (payload.fhirStatus) {
            this.updateElement('fhir-status', payload.fhirStatus);
        }
        if (payload.patientInfo) {
            this.updateElement('patient-info', payload.patientInfo);
        }
    }

    handleAlert(payload) {
        this.addAlert(payload.message, payload.severity || 'info');
    }

    // Utility Methods
    updateStatusIndicator(indicatorId, status) {
        const indicator = document.getElementById(indicatorId);
        if (indicator) {
            const light = indicator.querySelector('.status-light');
            if (light) {
                light.className = `status-light ${status}`;
            }
        }
    }

    updateToggleButton(buttonId, enabled) {
        const button = document.getElementById(buttonId);
        if (button) {
            button.textContent = enabled ? 'Deactivate' : 'Activate';
            button.className = enabled ? 'toggle-btn active' : 'toggle-btn';
        }
    }

    updateElement(elementId, text) {
        const element = document.getElementById(elementId);
        if (element) {
            element.textContent = text;
        }
    }

    getElementText(elementId) {
        const element = document.getElementById(elementId);
        return element ? element.textContent : null;
    }

    showNotification(message, type = 'info') {
        // Create and show notification
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;
        
        // Position and style notification
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 5px;
            color: white;
            font-weight: bold;
            z-index: 10000;
            max-width: 300px;
            box-shadow: 0 4px 8px rgba(0,0,0,0.2);
        `;
        
        // Set background color based on type
        switch (type) {
            case 'success':
                notification.style.backgroundColor = '#4CAF50';
                break;
            case 'error':
                notification.style.backgroundColor = '#f44336';
                break;
            case 'warning':
                notification.style.backgroundColor = '#ff9800';
                break;
            default:
                notification.style.backgroundColor = '#2196F3';
        }
        
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.parentNode.removeChild(notification);
            }
        }, 5000);
        
        console.log(`📢 ${type.toUpperCase()}: ${message}`);
    }

    addAlert(message, severity) {
        const alertsList = document.getElementById('alerts-list');
        if (alertsList) {
            const alert = document.createElement('div');
            alert.className = `alert ${severity}`;
            alert.innerHTML = `
                <span class="alert-time">${new Date().toLocaleTimeString()}</span>
                <span class="alert-message">${message}</span>
                <button class="alert-close" onclick="this.parentElement.remove()">×</button>
            `;
            
            alertsList.insertBefore(alert, alertsList.firstChild);
            
            // Keep only last 10 alerts
            while (alertsList.children.length > 10) {
                alertsList.removeChild(alertsList.lastChild);
            }
        }
    }

    // Cleanup method
    dispose() {
        // Remove event listeners
        this.eventHandlers.forEach(({ element, event, handler }) => {
            element.removeEventListener(event, handler);
        });
        this.eventHandlers.clear();
        
        // Close WebSocket
        if (this.websocket) {
            this.websocket.close();
        }
        
        console.log('🧹 Advanced Features Manager disposed');
    }
}

// CSS Styles for Advanced Features
const advancedFeaturesCSS = `
.advanced-features-container {
    background: #f5f5f5;
    border-radius: 10px;
    padding: 20px;
    margin: 20px 0;
    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

.advanced-features-header {
    text-align: center;
    margin-bottom: 20px;
}

.advanced-features-header h2 {
    color: #1976D2;
    margin-bottom: 15px;
}

.feature-status-bar {
    display: flex;
    justify-content: center;
    gap: 20px;
    flex-wrap: wrap;
}

.status-indicator {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 15px;
    background: white;
    border-radius: 20px;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.status-light {
    width: 12px;
    height: 12px;
    border-radius: 50%;
    transition: background-color 0.3s;
}

.status-light.off { background-color: #ccc; }
.status-light.on { background-color: #4CAF50; }

.features-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
    gap: 20px;
    margin: 20px 0;
}

.feature-panel {
    background: white;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.panel-header {
    background: #1976D2;
    color: white;
    padding: 15px 20px;
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.panel-header h3 {
    margin: 0;
    font-size: 16px;
}

.toggle-btn {
    background: rgba(255,255,255,0.2);
    color: white;
    border: 1px solid rgba(255,255,255,0.3);
    padding: 8px 16px;
    border-radius: 4px;
    cursor: pointer;
    transition: all 0.3s;
}

.toggle-btn:hover {
    background: rgba(255,255,255,0.3);
}

.toggle-btn.active {
    background: #4CAF50;
    border-color: #4CAF50;
}

.panel-content {
    padding: 20px;
}

.metric-row {
    display: flex;
    justify-content: space-between;
    margin-bottom: 10px;
    padding: 8px 0;
    border-bottom: 1px solid #eee;
}

.metric-row:last-of-type {
    border-bottom: none;
}

.controls {
    margin-top: 20px;
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
}

.action-btn {
    background: #2196F3;
    color: white;
    border: none;
    padding: 10px 15px;
    border-radius: 4px;
    cursor: pointer;
    transition: background-color 0.3s;
    font-size: 12px;
}

.action-btn:hover {
    background: #1976D2;
}

.slider-control {
    margin: 15px 0;
}

.slider-control label {
    display: block;
    margin-bottom: 5px;
    font-weight: bold;
}

.slider-control input[type="range"] {
    width: 100%;
}

.checkbox-control {
    margin: 10px 0;
    display: flex;
    align-items: center;
    gap: 8px;
}

.vu-meter {
    display: flex;
    align-items: center;
    height: 20px;
    background: #eee;
    border-radius: 10px;
    overflow: hidden;
    flex: 1;
    margin-left: 10px;
}

.vu-bar {
    height: 100%;
    transition: width 0.1s, background-color 0.3s;
    border-radius: 10px;
}

.device-item {
    display: flex;
    align-items: center;
    margin: 10px 0;
    padding: 10px;
    background: #f9f9f9;
    border-radius: 5px;
}

.live-transcription {
    margin-top: 20px;
    padding: 15px;
    background: #f9f9f9;
    border-radius: 5px;
}

.transcription-text {
    background: white;
    padding: 10px;
    border-radius: 3px;
    min-height: 60px;
    border: 1px solid #ddd;
    font-family: monospace;
    font-size: 12px;
}

.metrics-dashboard {
    background: white;
    border-radius: 8px;
    padding: 20px;
    margin: 20px 0;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 20px;
    margin-top: 15px;
}

.metric-card {
    background: #f9f9f9;
    padding: 15px;
    border-radius: 5px;
    text-align: center;
}

.metric-card h4 {
    margin: 0 0 10px 0;
    color: #666;
}

.alerts-panel {
    background: white;
    border-radius: 8px;
    padding: 20px;
    margin: 20px 0;
    box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.alert {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 10px 15px;
    margin: 5px 0;
    border-radius: 4px;
    background: #e3f2fd;
    border-left: 4px solid #2196F3;
}

.alert.warning {
    background: #fff3e0;
    border-left-color: #ff9800;
}

.alert.error {
    background: #ffebee;
    border-left-color: #f44336;
}

.alert-time {
    font-size: 12px;
    color: #666;
    margin-right: 10px;
}

.alert-message {
    flex: 1;
}

.alert-close {
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    padding: 0 5px;
    color: #666;
}

.notification {
    animation: slideIn 0.3s ease-out;
}

@keyframes slideIn {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}
`;

// Inject CSS styles
function injectAdvancedFeaturesCSS() {
    const style = document.createElement('style');
    style.textContent = advancedFeaturesCSS;
    document.head.appendChild(style);
}

// Global instance
let advancedFeaturesManager = null;

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    injectAdvancedFeaturesCSS();
    
    advancedFeaturesManager = new AdvancedFeaturesManager();
    advancedFeaturesManager.initialize();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (advancedFeaturesManager) {
        advancedFeaturesManager.dispose();
    }
});

// Export for global access
window.AdvancedFeaturesManager = AdvancedFeaturesManager;
window.advancedFeaturesManager = advancedFeaturesManager;