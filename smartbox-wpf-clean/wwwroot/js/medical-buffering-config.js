/**
 * Medical-Grade Buffering Configuration for SmartBox-Next
 * Phase 3: Playback Enhancements - Enhanced Buffering Strategy
 * 
 * Provides specialized HLS.js configurations optimized for medical video streaming
 * with enhanced precision, reliability, and performance requirements
 */

const MedicalBufferingConfig = {
    // Development/Testing configuration
    development: {
        // Buffer management
        backBufferLength: 30,           // 30 seconds back buffer for scrubbing
        maxBufferLength: 30,            // 30 seconds forward buffer
        maxMaxBufferLength: 120,        // 2 minutes total buffer
        
        // Frame-accurate seeking
        nudgeOffset: 0.001,             // 1ms precision for seeking
        maxFragLookUpTolerance: 0.001,  // 1ms fragment lookup tolerance
        
        // Network settings
        manifestLoadingTimeOut: 10000,     // 10 seconds manifest timeout
        manifestLoadingMaxRetry: 3,        // 3 retry attempts
        manifestLoadingRetryDelay: 500,    // 500ms retry delay
        
        // Fragment loading
        fragLoadingTimeOut: 20000,         // 20 seconds fragment timeout
        fragLoadingMaxRetry: 4,            // 4 retry attempts
        fragLoadingRetryDelay: 1000,       // 1 second retry delay
        
        // Performance optimizations
        startFragPrefetch: true,           // Enable fragment prefetching
        testBandwidth: true,               // Enable bandwidth testing
        progressive: true,                 // Enable progressive enhancement
        
        // Error recovery
        maxBufferHole: 0.5,               // 500ms maximum buffer hole
        enableSoftwareAES: false,         // Disable software AES for performance
        
        // Live streaming settings
        liveBackBufferLength: 3,          // 3 seconds live back buffer
        liveSyncDuration: 2,              // 2 seconds sync duration
        liveMaxLatencyDuration: 8,        // 8 seconds max latency
        
        // Memory management
        maxBufferSize: 200 * 1000 * 1000, // 200 MB buffer size limit
        
        // Advanced settings
        stretchShortVideoTrack: false,
        forceKeyFrameOnDiscontinuity: true,
        appendErrorMaxRetry: 3,
        
        // Medical mode specific
        medicalPrecisionMode: false
    },
    
    // Production configuration for medical use
    production: {
        // Enhanced buffer management for medical precision
        backBufferLength: 120,          // 2 minutes back buffer for detailed scrubbing
        maxBufferLength: 60,            // 1 minute forward buffer
        maxMaxBufferLength: 600,        // 10 minutes total buffer for extended review
        
        // Ultra-precise seeking for medical review
        nudgeOffset: 0.0001,            // 0.1ms precision for frame-accurate seeking
        maxFragLookUpTolerance: 0.0001, // 0.1ms fragment lookup tolerance
        
        // Aggressive loading for reliability
        manifestLoadingTimeOut: 20000,     // 20 seconds manifest timeout
        manifestLoadingMaxRetry: 6,        // 6 retry attempts for reliability
        manifestLoadingRetryDelay: 1000,   // 1 second retry delay
        
        // Enhanced fragment loading
        fragLoadingTimeOut: 30000,         // 30 seconds fragment timeout
        fragLoadingMaxRetry: 8,            // 8 retry attempts for medical reliability
        fragLoadingRetryDelay: 1500,       // 1.5 second retry delay
        
        // Maximum performance settings
        startFragPrefetch: true,           // Aggressive prefetching
        testBandwidth: true,               // Continuous bandwidth monitoring
        progressive: true,                 // Progressive enhancement enabled
        
        // Low latency for live medical streaming
        liveBackBufferLength: 5,          // 5 seconds live back buffer
        liveSyncDuration: 3,              // 3 seconds sync duration
        liveMaxLatencyDuration: 10,       // 10 seconds max latency
        
        // Enhanced memory management
        maxBufferSize: 600 * 1000 * 1000, // 600 MB for medical content
        maxBufferHole: 0.3,               // 300ms maximum buffer hole
        
        // Error recovery and reliability
        enableSoftwareAES: false,         // Hardware AES preferred
        stretchShortVideoTrack: false,    // Maintain original timing
        forceKeyFrameOnDiscontinuity: true, // Ensure clean transitions
        appendErrorMaxRetry: 6,           // Enhanced retry for medical reliability
        
        // Medical mode specific settings
        medicalPrecisionMode: true,       // Enable medical-grade precision
        
        // Advanced streaming controls
        capLevelToPlayerSize: false,      // Don't limit quality to player size
        startLevel: -1,                   // Auto-select initial quality
        autoStartLoad: true,              // Automatically start loading
        
        // Enhanced seek accuracy
        accurateSeek: true,               // Enable accurate seeking
        seekOnSeekableEnd: true,          // Seek to exact end positions
        
        // Buffer management optimizations
        bufferFlushingThreshold: 0.5,     // Flush threshold for memory management
        maxSeekHole: 2,                   // Maximum seek hole tolerance
        
        // Network optimization
        manifestLoadingMaxRetryTimeout: 64000,  // Max timeout for manifest retries
        levelLoadingMaxRetryTimeout: 64000,     // Max timeout for level retries
        fragLoadingMaxRetryTimeout: 64000,      // Max timeout for fragment retries
        
        // Performance monitoring
        enableStreamingWorker: true,      // Use web workers for processing
        enableWebAssembly: true,          // Enable WASM optimizations
        
        // Medical workflow integration
        keySystemsMapping: {},            // DRM support if needed
        requestMediaKeySystemAccessFunc: null, // Custom DRM handling
        
        // Debug and monitoring (production-safe)
        debug: false,                     // Disable debug logging
        enableWorker: true,               // Enable worker threads
        workerPath: null,                 // Use default worker path
        
        // Content steering and ABR
        cmcd: {},                         // Common Media Client Data
        enableDateRangeMetadataCues: true, // Enable metadata cues
        enableEmsgMetadataCues: true,     // Enable EMSG metadata
        enableID3MetadataCues: true       // Enable ID3 metadata
    },
    
    // Surgical configuration for critical procedures
    surgical: {
        // Maximum stability and reliability
        backBufferLength: 300,          // 5 minutes for surgical review
        maxBufferLength: 120,           // 2 minutes forward buffer
        maxMaxBufferLength: 1800,       // 30 minutes total for long procedures
        
        // Absolute precision
        nudgeOffset: 0.00001,           // 0.01ms precision
        maxFragLookUpTolerance: 0.00001, // 0.01ms tolerance
        
        // Maximum reliability settings
        manifestLoadingTimeOut: 30000,     // 30 seconds
        manifestLoadingMaxRetry: 10,       // 10 retries
        manifestLoadingRetryDelay: 2000,   // 2 second delays
        
        fragLoadingTimeOut: 45000,         // 45 seconds
        fragLoadingMaxRetry: 12,           // 12 retries
        fragLoadingRetryDelay: 2500,       // 2.5 second delays
        
        // Conservative performance for stability
        startFragPrefetch: true,
        testBandwidth: false,           // Disable to reduce variables
        progressive: false,             // Disable for maximum stability
        
        // Minimal live latency
        liveBackBufferLength: 1,        // 1 second for immediate response
        liveSyncDuration: 1,            // 1 second sync
        liveMaxLatencyDuration: 3,      // 3 seconds max latency
        
        // Maximum memory allocation
        maxBufferSize: 1000 * 1000 * 1000, // 1 GB for surgical precision
        maxBufferHole: 0.1,             // 100ms maximum hole
        
        // Maximum reliability
        appendErrorMaxRetry: 10,
        medicalPrecisionMode: true,
        accurateSeek: true,
        
        // Quality constraints for surgical stability
        capLevelToPlayerSize: false,
        startLevel: 2,                  // Start with high quality
        autoStartLoad: true
    },
    
    // Emergency configuration for rapid response
    emergency: {
        // Minimal buffering for immediate response
        backBufferLength: 5,            // 5 seconds
        maxBufferLength: 10,            // 10 seconds
        maxMaxBufferLength: 30,         // 30 seconds total
        
        // Fast seeking
        nudgeOffset: 0.01,              // 10ms for speed
        maxFragLookUpTolerance: 0.01,
        
        // Fast loading with fewer retries
        manifestLoadingTimeOut: 5000,   // 5 seconds
        manifestLoadingMaxRetry: 2,     // 2 retries only
        manifestLoadingRetryDelay: 200, // 200ms quick retry
        
        fragLoadingTimeOut: 10000,      // 10 seconds
        fragLoadingMaxRetry: 3,         // 3 retries
        fragLoadingRetryDelay: 500,     // 500ms retry
        
        // Speed optimizations
        startFragPrefetch: false,       // Disable prefetch for immediate start
        testBandwidth: false,           // Skip bandwidth test
        progressive: true,              // Enable for speed
        
        // Minimal live latency
        liveBackBufferLength: 1,
        liveSyncDuration: 0.5,          // 500ms sync
        liveMaxLatencyDuration: 2,      // 2 seconds max
        
        // Minimal memory usage
        maxBufferSize: 50 * 1000 * 1000, // 50 MB
        maxBufferHole: 1,               // 1 second hole tolerance
        
        // Reduced reliability for speed
        appendErrorMaxRetry: 2,
        medicalPrecisionMode: false,
        
        // Lower quality for speed
        startLevel: 0,                  // Start with lowest quality
        autoStartLoad: true
    },
    
    /**
     * Get configuration based on environment and medical context
     * @param {string} environment - 'development', 'production', 'surgical', 'emergency'
     * @param {Object} customOptions - Custom overrides
     * @returns {Object} HLS.js configuration object
     */
    getConfig(environment = 'production', customOptions = {}) {
        const baseConfig = this[environment] || this.production;
        
        // Apply custom options
        const config = {
            ...baseConfig,
            ...customOptions
        };
        
        // Add debug information for development
        if (environment === 'development') {
            config.debug = true;
            console.log('🏥 Medical Buffering Config loaded:', environment);
        }
        
        return config;
    },
    
    /**
     * Get medical-optimized configuration with specific presets
     * @param {string} medicalMode - 'diagnostic', 'surgical', 'review', 'emergency'
     * @param {Object} options - Additional options
     * @returns {Object} Optimized configuration
     */
    getMedicalConfig(medicalMode = 'review', options = {}) {
        let baseEnvironment = 'production';
        
        // Map medical modes to environments
        switch (medicalMode) {
            case 'surgical':
                baseEnvironment = 'surgical';
                break;
            case 'emergency':
                baseEnvironment = 'emergency';
                break;
            case 'diagnostic':
            case 'review':
            default:
                baseEnvironment = 'production';
                break;
        }
        
        const config = this.getConfig(baseEnvironment, options);
        
        // Add medical mode specific enhancements
        config.medicalMode = medicalMode;
        config.enableMedicalLogging = options.enableLogging || false;
        
        // Add medical event handlers if provided
        if (options.medicalEventHandlers) {
            config.medicalEventHandlers = options.medicalEventHandlers;
        }
        
        console.log(`🏥 Medical configuration loaded for ${medicalMode} mode`);
        
        return config;
    },
    
    /**
     * Validate configuration for medical compliance
     * @param {Object} config - Configuration to validate
     * @returns {Object} Validation results
     */
    validateMedicalCompliance(config) {
        const compliance = {
            valid: true,
            warnings: [],
            errors: [],
            recommendations: []
        };
        
        // Check buffer sizes for medical requirements
        if (config.backBufferLength < 30) {
            compliance.warnings.push('Back buffer < 30s may impact medical review capabilities');
        }
        
        if (config.nudgeOffset > 0.001) {
            compliance.warnings.push('Seek precision > 1ms may not meet medical standards');
        }
        
        // Check retry settings for reliability
        if (config.fragLoadingMaxRetry < 4) {
            compliance.recommendations.push('Consider increasing fragment retry count for medical reliability');
        }
        
        // Check memory allocation
        if (config.maxBufferSize < 200 * 1000 * 1000) {
            compliance.recommendations.push('Consider increasing buffer size for medical content');
        }
        
        return compliance;
    },
    
    /**
     * Create optimized configuration for specific medical workflows
     * @param {string} workflow - 'endoscopy', 'surgery', 'radiology', 'cardiology', 'general'
     * @param {Object} deviceConstraints - Device-specific constraints
     * @returns {Object} Workflow-optimized configuration
     */
    createWorkflowConfig(workflow = 'general', deviceConstraints = {}) {
        let baseConfig;
        
        switch (workflow) {
            case 'endoscopy':
                baseConfig = this.getMedicalConfig('diagnostic', {
                    backBufferLength: 60,      // 1 minute for procedure review
                    maxBufferLength: 30,       // Quick forward buffering
                    startLevel: 1              // Good quality for detail
                });
                break;
                
            case 'surgery':
                baseConfig = this.getMedicalConfig('surgical', {
                    enableLogging: true        // Enhanced logging for surgical procedures
                });
                break;
                
            case 'radiology':
                baseConfig = this.getMedicalConfig('diagnostic', {
                    backBufferLength: 180,     // 3 minutes for detailed review
                    nudgeOffset: 0.0001,       // Maximum precision
                    startLevel: -1             // Highest available quality
                });
                break;
                
            case 'cardiology':
                baseConfig = this.getMedicalConfig('review', {
                    liveMaxLatencyDuration: 5, // Low latency for real-time monitoring
                    testBandwidth: true        // Monitor bandwidth for critical streams
                });
                break;
                
            case 'emergency':
                baseConfig = this.getMedicalConfig('emergency');
                break;
                
            default:
                baseConfig = this.getMedicalConfig('review');
                break;
        }
        
        // Apply device constraints
        if (deviceConstraints.limitedMemory) {
            baseConfig.maxBufferSize = Math.min(baseConfig.maxBufferSize, 100 * 1000 * 1000); // 100 MB
            baseConfig.maxMaxBufferLength = Math.min(baseConfig.maxMaxBufferLength, 120); // 2 minutes
        }
        
        if (deviceConstraints.slowNetwork) {
            baseConfig.manifestLoadingTimeOut *= 2;
            baseConfig.fragLoadingTimeOut *= 2;
            baseConfig.startFragPrefetch = false;
        }
        
        if (deviceConstraints.limitedCPU) {
            baseConfig.enableStreamingWorker = false;
            baseConfig.enableWebAssembly = false;
        }
        
        console.log(`🏥 Workflow configuration created for ${workflow}`);
        
        return baseConfig;
    }
};

// Export for use in HLS initialization
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MedicalBufferingConfig;
} else if (typeof window !== 'undefined') {
    window.MedicalBufferingConfig = MedicalBufferingConfig;
}