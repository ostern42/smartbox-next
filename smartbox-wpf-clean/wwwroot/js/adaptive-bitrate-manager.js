/**
 * Adaptive Bitrate Manager for SmartBox-Next
 * Phase 3: Playback Enhancements - Intelligent Quality Switching
 * 
 * Medical-grade adaptive bitrate management with HLS integration
 * Provides smooth quality transitions based on network conditions and buffer health
 */

class AdaptiveBitrateManager {
    constructor(player, hlsInstance, options = {}) {
        this.player = player;
        this.hls = hlsInstance;
        this.options = {
            // Evaluation intervals
            evaluationInterval: 2000,        // 2 seconds between quality evaluations
            measurementWindow: 30000,        // 30 seconds of bandwidth measurements
            
            // Bandwidth safety margins
            safetyMargin: 0.7,              // Use 70% of available bandwidth
            upgradeMargin: 0.8,             // Upgrade threshold (80% utilization)
            downgradeMargin: 0.9,           // Downgrade threshold (90% utilization)
            
            // Buffer thresholds
            minBufferLength: 5,             // Minimum buffer length (seconds)
            healthyBufferLength: 10,        // Healthy buffer length (seconds)
            emergencyBufferLength: 2,       // Emergency downgrade threshold
            
            // Quality switching constraints
            maxUpgradeSteps: 1,             // Maximum quality levels to upgrade at once
            maxDowngradeSteps: 2,           // Maximum quality levels to downgrade at once
            stabilityWindow: 10000,         // 10 seconds before allowing upgrades after downgrade
            
            // Medical mode settings
            medicalMode: false,             // Enhanced precision for medical use
            frameAccurateSeking: true,      // Frame-accurate quality switches
            qualityPresets: {
                diagnostic: { priority: 'quality', minLevel: 2 },    // High quality for diagnosis
                surgical: { priority: 'stability', maxSwitches: 2 }, // Stable quality for surgery
                review: { priority: 'adaptive', responsiveness: 'high' } // Responsive for review
            },
            
            ...options
        };
        
        // State management
        this.measurements = [];
        this.qualityLevels = [];
        this.autoMode = true;
        this.currentPreset = 'review';
        this.lastQualityChange = 0;
        this.switchCount = 0;
        this.emergencyMode = false;
        
        // Performance tracking
        this.metrics = {
            totalSwitches: 0,
            upgrades: 0,
            downgrades: 0,
            emergencyDowngrades: 0,
            averageBandwidth: 0,
            bufferUnderrunCount: 0,
            qualityStabilityScore: 100
        };
        
        // Initialize the manager
        this.initialize();
    }
    
    /**
     * Initialize the adaptive bitrate manager
     */
    initialize() {
        console.log('🎯 Initializing AdaptiveBitrateManager');
        console.log(`📊 Medical mode: ${this.options.medicalMode ? 'Enabled' : 'Disabled'}`);
        console.log(`⚙️ Quality preset: ${this.currentPreset}`);
        
        this.setupHLSEventListeners();
        this.startQualityEvaluation();
        this.setupPlayerIntegration();
        
        // Initialize quality levels
        if (this.hls.levels && this.hls.levels.length > 0) {
            this.updateQualityLevels();
        }
    }
    
    /**
     * Setup HLS event listeners for monitoring
     */
    setupHLSEventListeners() {
        // Fragment loading events for bandwidth measurement
        this.hls.on(Hls.Events.FRAG_LOADED, this.onFragmentLoaded.bind(this));
        this.hls.on(Hls.Events.FRAG_LOADING_TIMEOUT, this.onFragmentTimeout.bind(this));
        this.hls.on(Hls.Events.FRAG_LOAD_EMERGENCY_ABORTED, this.onFragmentEmergencyAbort.bind(this));
        
        // Level and manifest events
        this.hls.on(Hls.Events.LEVEL_LOADED, this.onLevelLoaded.bind(this));
        this.hls.on(Hls.Events.MANIFEST_PARSED, this.onManifestParsed.bind(this));
        this.hls.on(Hls.Events.LEVEL_SWITCHING, this.onLevelSwitching.bind(this));
        this.hls.on(Hls.Events.LEVEL_SWITCHED, this.onLevelSwitched.bind(this));
        
        // Buffer events
        this.hls.on(Hls.Events.BUFFER_APPENDED, this.onBufferAppended.bind(this));
        this.hls.on(Hls.Events.BUFFER_EOS, this.onBufferEOS.bind(this));
        this.hls.on(Hls.Events.BUFFER_STALLED, this.onBufferStalled.bind(this));
        
        // Error events
        this.hls.on(Hls.Events.ERROR, this.onHLSError.bind(this));
    }
    
    /**
     * Setup player integration for enhanced monitoring
     */
    setupPlayerIntegration() {
        // Monitor video events
        if (this.player.video) {
            this.player.video.addEventListener('waiting', this.onVideoWaiting.bind(this));
            this.player.video.addEventListener('canplay', this.onVideoCanPlay.bind(this));
            this.player.video.addEventListener('stalled', this.onVideoStalled.bind(this));
        }
        
        // Integrate with player events
        this.player.on('seek', this.onPlayerSeek.bind(this));
        this.player.on('play', this.onPlayerPlay.bind(this));
        this.player.on('pause', this.onPlayerPause.bind(this));
    }
    
    /**
     * Start periodic quality evaluation
     */
    startQualityEvaluation() {
        this.evaluationInterval = setInterval(() => {
            if (this.autoMode && this.hls.levels.length > 1) {
                this.evaluateQuality();
            }
        }, this.options.evaluationInterval);
        
        console.log(`⏱️ Quality evaluation started (${this.options.evaluationInterval}ms interval)`);
    }
    
    /**
     * Handle fragment loaded event for bandwidth measurement
     */
    onFragmentLoaded(event, data) {
        const stats = data.stats;
        if (!stats) return;
        
        // Calculate bandwidth and latency
        const loadTime = stats.tload - stats.trequest;
        const bandwidth = loadTime > 0 ? (stats.total * 8) / (loadTime / 1000) : 0; // bits per second
        const latency = stats.tfirst - stats.trequest;
        
        // Store measurement
        const measurement = {
            timestamp: Date.now(),
            bandwidth: bandwidth,
            latency: latency,
            fragmentSize: stats.total,
            loadTime: loadTime,
            level: data.frag.level,
            duration: data.frag.duration
        };
        
        this.measurements.push(measurement);
        
        // Clean old measurements
        this.cleanOldMeasurements();
        
        // Update metrics
        this.updateBandwidthMetrics();
        
        // Log detailed measurement for medical mode
        if (this.options.medicalMode) {
            console.log(`📊 Fragment loaded - Level: ${data.frag.level}, Bandwidth: ${(bandwidth / 1000000).toFixed(2)} Mbps, Latency: ${latency}ms`);
        }
    }
    
    /**
     * Handle manifest parsed event
     */
    onManifestParsed(event, data) {
        this.updateQualityLevels();
        console.log(`📋 Manifest parsed: ${this.qualityLevels.length} quality levels available`);
        
        // Apply medical mode optimizations
        if (this.options.medicalMode) {
            this.applyMedicalOptimizations();
        }
    }
    
    /**
     * Handle level switching event
     */
    onLevelSwitching(event, data) {
        console.log(`🔄 Quality switching to level ${data.level} (${this.getLevelDescription(data.level)})`);
    }
    
    /**
     * Handle level switched event
     */
    onLevelSwitched(event, data) {
        this.lastQualityChange = Date.now();
        this.switchCount++;
        this.metrics.totalSwitches++;
        
        const levelInfo = this.getLevelInfo(data.level);
        console.log(`✅ Quality switched to level ${data.level}: ${levelInfo.resolution} @ ${(levelInfo.bitrate / 1000000).toFixed(2)} Mbps`);
        
        // Emit quality change event
        this.player.emit('qualityChanged', {
            level: data.level,
            levelInfo: levelInfo,
            auto: this.autoMode,
            metrics: this.getPerformanceMetrics()
        });
    }
    
    /**
     * Handle HLS errors
     */
    onHLSError(event, data) {
        if (data.fatal) {
            console.error('🚨 Fatal HLS error:', data);
            this.emergencyMode = true;
            this.switchToLowestQuality();
        } else if (data.type === Hls.ErrorTypes.NETWORK_ERROR) {
            console.warn('⚠️ Network error, considering quality downgrade:', data);
            this.evaluateEmergencyDowngrade();
        }
    }
    
    /**
     * Handle buffer stalled event
     */
    onBufferStalled(event, data) {
        console.warn('⏸️ Buffer stalled, emergency quality adjustment');
        this.metrics.bufferUnderrunCount++;
        this.evaluateEmergencyDowngrade();
    }
    
    /**
     * Handle video waiting event
     */
    onVideoWaiting() {
        if (this.autoMode) {
            console.warn('⏸️ Video waiting for data, evaluating quality downgrade');
            this.evaluateEmergencyDowngrade();
        }
    }
    
    /**
     * Main quality evaluation logic
     */
    evaluateQuality() {
        if (this.measurements.length < 3) {
            return; // Need more data
        }
        
        const avgBandwidth = this.getAverageBandwidth();
        const bufferHealth = this.getBufferHealth();
        const currentLevel = this.hls.currentLevel;
        
        // Skip evaluation if in emergency mode and insufficient time has passed
        if (this.emergencyMode && Date.now() - this.lastQualityChange < 5000) {
            return;
        }
        
        // Calculate optimal level
        const optimalLevel = this.calculateOptimalLevel(avgBandwidth, bufferHealth, currentLevel);
        
        // Apply quality switching logic
        if (this.shouldSwitchQuality(currentLevel, optimalLevel, bufferHealth)) {
            this.switchToLevel(optimalLevel, 'adaptive');
        }
        
        // Update stability metrics
        this.updateStabilityMetrics();
    }
    
    /**
     * Calculate weighted average bandwidth
     */
    getAverageBandwidth() {
        if (this.measurements.length === 0) return 0;
        
        // Use weighted average giving more weight to recent measurements
        let weightedSum = 0;
        let weightSum = 0;
        const now = Date.now();
        
        this.measurements.forEach((measurement, index) => {
            // Time-based weight (more recent = higher weight)
            const age = now - measurement.timestamp;
            const timeWeight = Math.exp(-age / 10000); // Exponential decay over 10 seconds
            
            // Position-based weight (later measurements = higher weight)
            const positionWeight = (index + 1) / this.measurements.length;
            
            // Combined weight
            const weight = timeWeight * positionWeight;
            
            weightedSum += measurement.bandwidth * weight;
            weightSum += weight;
        });
        
        return weightSum > 0 ? weightedSum / weightSum : 0;
    }
    
    /**
     * Get comprehensive buffer health analysis
     */
    getBufferHealth() {
        const video = this.player.video;
        if (!video || !video.buffered) {
            return { length: 0, holes: 0, isHealthy: false, isEmpty: true };
        }
        
        const buffered = video.buffered;
        const currentTime = video.currentTime;
        
        let bufferLength = 0;
        let bufferHoles = 0;
        let totalBuffered = 0;
        
        // Analyze buffer ranges
        for (let i = 0; i < buffered.length; i++) {
            const start = buffered.start(i);
            const end = buffered.end(i);
            
            totalBuffered += end - start;
            
            // Check if current time is within this range
            if (start <= currentTime && currentTime <= end) {
                bufferLength = end - currentTime;
            }
            
            // Count gaps in buffer
            if (i > 0 && start > buffered.end(i - 1)) {
                bufferHoles++;
            }
        }
        
        const bufferHealth = {
            length: bufferLength,
            holes: bufferHoles,
            totalBuffered: totalBuffered,
            isEmpty: bufferLength === 0,
            isHealthy: bufferLength > this.options.healthyBufferLength && bufferHoles === 0,
            isEmergency: bufferLength < this.options.emergencyBufferLength,
            utilization: video.duration > 0 ? totalBuffered / video.duration : 0
        };
        
        return bufferHealth;
    }
    
    /**
     * Calculate optimal quality level based on conditions
     */
    calculateOptimalLevel(bandwidth, bufferHealth, currentLevel) {
        const levels = this.hls.levels;
        if (!levels || levels.length === 0) return -1;
        
        // Apply safety margin to bandwidth
        const safeBandwidth = bandwidth * this.options.safetyMargin;
        
        // Find highest quality that fits within bandwidth
        let optimalLevel = -1;
        
        for (let i = levels.length - 1; i >= 0; i--) {
            if (levels[i].bitrate <= safeBandwidth) {
                optimalLevel = i;
                break;
            }
        }
        
        // If no level found, use lowest
        if (optimalLevel === -1) {
            optimalLevel = 0;
        }
        
        // Apply buffer-based adjustments
        optimalLevel = this.applyBufferAdjustments(optimalLevel, bufferHealth, currentLevel);
        
        // Apply medical mode constraints
        if (this.options.medicalMode) {
            optimalLevel = this.applyMedicalConstraints(optimalLevel, currentLevel);
        }
        
        // Apply preset constraints
        optimalLevel = this.applyPresetConstraints(optimalLevel);
        
        return optimalLevel;
    }
    
    /**
     * Apply buffer health adjustments to quality level
     */
    applyBufferAdjustments(optimalLevel, bufferHealth, currentLevel) {
        // Emergency downgrade for critical buffer situation
        if (bufferHealth.isEmergency) {
            return Math.max(0, currentLevel - this.options.maxDowngradeSteps);
        }
        
        // Conservative approach for unhealthy buffer
        if (!bufferHealth.isHealthy) {
            return Math.max(0, Math.min(optimalLevel, currentLevel - 1));
        }
        
        // Aggressive upgrade for very healthy buffer
        if (bufferHealth.length > 30 && bufferHealth.holes === 0) {
            return Math.min(this.hls.levels.length - 1, optimalLevel + 1);
        }
        
        return optimalLevel;
    }
    
    /**
     * Apply medical mode constraints
     */
    applyMedicalConstraints(optimalLevel, currentLevel) {
        const preset = this.options.qualityPresets[this.currentPreset];
        if (!preset) return optimalLevel;
        
        // Apply minimum quality for diagnostic preset
        if (preset.minLevel !== undefined) {
            optimalLevel = Math.max(optimalLevel, preset.minLevel);
        }
        
        // Limit quality switches for surgical preset
        if (preset.maxSwitches !== undefined && this.switchCount >= preset.maxSwitches) {
            return currentLevel;
        }
        
        // Apply conservative switching for medical mode
        const timeSinceLastSwitch = Date.now() - this.lastQualityChange;
        if (timeSinceLastSwitch < this.options.stabilityWindow && optimalLevel > currentLevel) {
            return currentLevel; // Don't upgrade too quickly
        }
        
        return optimalLevel;
    }
    
    /**
     * Apply quality preset constraints
     */
    applyPresetConstraints(optimalLevel) {
        const preset = this.options.qualityPresets[this.currentPreset];
        if (!preset) return optimalLevel;
        
        if (preset.minLevel !== undefined) {
            optimalLevel = Math.max(optimalLevel, preset.minLevel);
        }
        
        if (preset.maxLevel !== undefined) {
            optimalLevel = Math.min(optimalLevel, preset.maxLevel);
        }
        
        return optimalLevel;
    }
    
    /**
     * Determine if quality should be switched
     */
    shouldSwitchQuality(currentLevel, optimalLevel, bufferHealth) {
        if (currentLevel === optimalLevel) return false;
        
        // Always allow emergency downgrades
        if (bufferHealth.isEmergency && optimalLevel < currentLevel) {
            return true;
        }
        
        // Check upgrade constraints
        if (optimalLevel > currentLevel) {
            const levelDiff = optimalLevel - currentLevel;
            if (levelDiff > this.options.maxUpgradeSteps) {
                return false;
            }
            
            // Ensure stable period before upgrading
            const timeSinceLastSwitch = Date.now() - this.lastQualityChange;
            if (timeSinceLastSwitch < this.options.stabilityWindow) {
                return false;
            }
        }
        
        // Check downgrade constraints
        if (optimalLevel < currentLevel) {
            const levelDiff = currentLevel - optimalLevel;
            if (levelDiff > this.options.maxDowngradeSteps && !bufferHealth.isEmergency) {
                return false;
            }
        }
        
        return true;
    }
    
    /**
     * Switch to specific quality level
     */
    switchToLevel(level, reason = 'manual') {
        if (level < 0 || level >= this.hls.levels.length) {
            console.warn(`⚠️ Invalid quality level: ${level}`);
            return false;
        }
        
        const currentLevel = this.hls.currentLevel;
        const levelInfo = this.getLevelInfo(level);
        
        console.log(`🎯 Switching to level ${level} (${reason}): ${levelInfo.resolution} @ ${(levelInfo.bitrate / 1000000).toFixed(2)} Mbps`);
        
        // Perform the switch
        this.hls.currentLevel = level;
        
        // Update metrics
        if (level > currentLevel) {
            this.metrics.upgrades++;
        } else if (level < currentLevel) {
            this.metrics.downgrades++;
            if (reason === 'emergency') {
                this.metrics.emergencyDowngrades++;
            }
        }
        
        // Reset emergency mode if switching to higher quality
        if (level > currentLevel) {
            this.emergencyMode = false;
        }
        
        return true;
    }
    
    /**
     * Evaluate emergency quality downgrade
     */
    evaluateEmergencyDowngrade() {
        const currentLevel = this.hls.currentLevel;
        if (currentLevel <= 0) return;
        
        const bufferHealth = this.getBufferHealth();
        
        // Immediate downgrade for critical buffer situation
        if (bufferHealth.isEmergency) {
            const targetLevel = Math.max(0, currentLevel - 2);
            this.switchToLevel(targetLevel, 'emergency');
            this.emergencyMode = true;
        }
    }
    
    /**
     * Switch to lowest quality (emergency)
     */
    switchToLowestQuality() {
        console.warn('🚨 Switching to lowest quality due to emergency');
        this.switchToLevel(0, 'emergency');
        this.emergencyMode = true;
    }
    
    /**
     * Update quality levels information
     */
    updateQualityLevels() {
        this.qualityLevels = this.hls.levels.map((level, index) => ({
            index: index,
            bitrate: level.bitrate,
            width: level.width,
            height: level.height,
            resolution: `${level.width}x${level.height}`,
            codecs: level.codecs,
            frameRate: level.frameRate
        }));
        
        console.log('📊 Quality levels updated:', this.qualityLevels.map(l => 
            `${l.index}: ${l.resolution} @ ${(l.bitrate / 1000000).toFixed(2)} Mbps`
        ).join(', '));
    }
    
    /**
     * Get level information
     */
    getLevelInfo(level) {
        return this.qualityLevels[level] || { 
            resolution: 'Unknown', 
            bitrate: 0, 
            width: 0, 
            height: 0 
        };
    }
    
    /**
     * Get level description string
     */
    getLevelDescription(level) {
        const info = this.getLevelInfo(level);
        return `${info.resolution} @ ${(info.bitrate / 1000000).toFixed(2)} Mbps`;
    }
    
    /**
     * Clean old bandwidth measurements
     */
    cleanOldMeasurements() {
        const cutoff = Date.now() - this.options.measurementWindow;
        this.measurements = this.measurements.filter(m => m.timestamp > cutoff);
    }
    
    /**
     * Update bandwidth metrics
     */
    updateBandwidthMetrics() {
        if (this.measurements.length > 0) {
            this.metrics.averageBandwidth = this.getAverageBandwidth();
        }
    }
    
    /**
     * Update stability metrics
     */
    updateStabilityMetrics() {
        // Calculate quality stability score based on switch frequency
        const now = Date.now();
        const recentSwitches = this.measurements.filter(m => 
            now - m.timestamp < 60000 // Last minute
        ).length;
        
        // Penalize frequent switches
        this.metrics.qualityStabilityScore = Math.max(0, 100 - (recentSwitches * 10));
    }
    
    /**
     * Apply medical mode optimizations
     */
    applyMedicalOptimizations() {
        if (!this.options.medicalMode) return;
        
        console.log('🏥 Applying medical mode optimizations');
        
        // Prioritize stability over aggressive optimization
        this.options.safetyMargin = 0.6;          // More conservative bandwidth usage
        this.options.stabilityWindow = 15000;     // Longer stability window
        this.options.maxUpgradeSteps = 1;         // Single level upgrades only
        this.options.evaluationInterval = 3000;   // Slower evaluation for stability
        
        // Set medical preset if not already set
        if (!this.options.qualityPresets[this.currentPreset]) {
            this.currentPreset = 'review';
        }
    }
    
    /**
     * Get performance metrics
     */
    getPerformanceMetrics() {
        const bufferHealth = this.getBufferHealth();
        
        return {
            ...this.metrics,
            currentBandwidth: this.measurements.length > 0 ? 
                this.measurements[this.measurements.length - 1].bandwidth : 0,
            bufferLength: bufferHealth.length,
            bufferHealth: bufferHealth.isHealthy,
            emergencyMode: this.emergencyMode,
            measurementCount: this.measurements.length,
            autoMode: this.autoMode,
            currentPreset: this.currentPreset
        };
    }
    
    /**
     * Public API methods
     */
    
    /**
     * Enable/disable automatic quality switching
     */
    setAutoMode(enabled) {
        this.autoMode = enabled;
        console.log(`🎛️ Auto mode: ${enabled ? 'Enabled' : 'Disabled'}`);
        
        this.player.emit('autoModeChanged', { autoMode: this.autoMode });
    }
    
    /**
     * Manually set quality level
     */
    setQualityLevel(level) {
        if (level === -1) {
            this.setAutoMode(true);
        } else {
            this.setAutoMode(false);
            this.switchToLevel(level, 'manual');
        }
    }
    
    /**
     * Set medical quality preset
     */
    setQualityPreset(preset) {
        if (this.options.qualityPresets[preset]) {
            this.currentPreset = preset;
            console.log(`🏥 Quality preset changed to: ${preset}`);
            
            // Reset switch count for new preset
            this.switchCount = 0;
            
            this.player.emit('qualityPresetChanged', { preset: this.currentPreset });
        } else {
            console.warn(`⚠️ Unknown quality preset: ${preset}`);
        }
    }
    
    /**
     * Get available quality levels
     */
    getQualityLevels() {
        return [...this.qualityLevels];
    }
    
    /**
     * Get current quality level
     */
    getCurrentLevel() {
        return this.hls.currentLevel;
    }
    
    /**
     * Get detailed status
     */
    getStatus() {
        const bufferHealth = this.getBufferHealth();
        const avgBandwidth = this.getAverageBandwidth();
        
        return {
            currentLevel: this.hls.currentLevel,
            currentLevelInfo: this.getLevelInfo(this.hls.currentLevel),
            autoMode: this.autoMode,
            emergencyMode: this.emergencyMode,
            averageBandwidth: avgBandwidth,
            bufferHealth: bufferHealth,
            measurements: this.measurements.length,
            metrics: this.getPerformanceMetrics(),
            preset: this.currentPreset,
            availableLevels: this.qualityLevels.length
        };
    }
    
    /**
     * Cleanup and destroy
     */
    destroy() {
        console.log('🗑️ Destroying AdaptiveBitrateManager');
        
        // Clear evaluation interval
        if (this.evaluationInterval) {
            clearInterval(this.evaluationInterval);
            this.evaluationInterval = null;
        }
        
        // Remove HLS event listeners
        if (this.hls) {
            this.hls.off(Hls.Events.FRAG_LOADED, this.onFragmentLoaded);
            this.hls.off(Hls.Events.LEVEL_LOADED, this.onLevelLoaded);
            this.hls.off(Hls.Events.MANIFEST_PARSED, this.onManifestParsed);
            this.hls.off(Hls.Events.LEVEL_SWITCHING, this.onLevelSwitching);
            this.hls.off(Hls.Events.LEVEL_SWITCHED, this.onLevelSwitched);
            this.hls.off(Hls.Events.ERROR, this.onHLSError);
        }
        
        // Clear data
        this.measurements = [];
        this.qualityLevels = [];
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdaptiveBitrateManager;
} else if (typeof window !== 'undefined') {
    window.AdaptiveBitrateManager = AdaptiveBitrateManager;
}