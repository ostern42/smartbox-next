/**
 * SmartBox Streaming Configuration
 * Centralized configuration for API endpoints and settings
 */

class StreamingConfig {
    constructor() {
        // Default configuration
        this.defaults = {
            apiUrl: 'http://localhost:5002/api',
            wsUrl: 'ws://localhost:5001',
            authRefreshInterval: 900000, // 15 minutes
            tokenExpiryBuffer: 60000, // 1 minute before expiry
            reconnectDelay: 5000,
            maxReconnectAttempts: 10,
            hlsOptions: {
                enableWorker: true,
                lowLatencyMode: true,
                backBufferLength: 90,
                maxBufferLength: 30,
                maxMaxBufferLength: 600,
                maxBufferSize: 60 * 1000 * 1000, // 60 MB
                maxBufferHole: 0.5,
                highBufferWatchdogPeriod: 2,
                nudgeOffset: 0.1,
                nudgeMaxRetry: 3,
                maxFragLookUpTolerance: 0.25,
                liveSyncDurationCount: 3,
                liveMaxLatencyDurationCount: Infinity,
                liveDurationInfinity: true,
                enableWebVTT: true,
                enableCEA708Captions: true,
                stretchShortVideoTrack: false,
                maxAudioFramesDrift: 1,
                forceKeyFrameOnDiscontinuity: true,
                abrEwmaFastLive: 3,
                abrEwmaSlowLive: 9,
                abrEwmaFastVoD: 3,
                abrEwmaSlowVoD: 9,
                abrEwmaDefaultEstimate: 5e5,
                abrBandWidthFactor: 0.95,
                abrBandWidthUpFactor: 0.7,
                abrMaxWithRealBitrate: false,
                maxStarvationDelay: 4,
                maxLoadingDelay: 4,
                minAutoBitrate: 0,
                emeEnabled: false,
                startLevel: -1,
                defaultAudioCodec: undefined,
                fragLoadingTimeOut: 20000,
                fragLoadingMaxRetry: 6,
                fragLoadingRetryDelay: 1000,
                fragLoadingMaxRetryTimeout: 64000,
                levelLoadingTimeOut: 10000,
                levelLoadingMaxRetry: 4,
                levelLoadingRetryDelay: 1000,
                levelLoadingMaxRetryTimeout: 64000,
                manifestLoadingTimeOut: 10000,
                manifestLoadingMaxRetry: 1,
                manifestLoadingRetryDelay: 1000,
                manifestLoadingMaxRetryTimeout: 64000
            },
            videojs: {
                controls: true,
                autoplay: false,
                preload: 'auto',
                fluid: true,
                liveui: true,
                liveTracker: {
                    trackingThreshold: 10,
                    liveTolerance: 15
                },
                html5: {
                    vhs: {
                        overrideNative: true,
                        enableLowInitialPlaylist: true,
                        smoothQualityChange: true,
                        fastQualityChange: true,
                        bandwidth: 4194304,
                        useBandwidthFromLocalStorage: true
                    }
                }
            },
            medical: {
                minTouchTargetSize: 44, // WCAG 2.1 AAA compliance
                frameRate: 30,
                enableHighContrast: false,
                enableAccessibilityMode: false,
                exportFormats: ['mp4', 'webm', 'dicom'],
                defaultExportFormat: 'mp4',
                enableMedicalMetadata: true,
                preserveFrameAccuracy: true
            }
        };

        // Load configuration from various sources
        this.config = this.loadConfiguration();
    }

    loadConfiguration() {
        let config = { ...this.defaults };

        // Check for global configuration
        if (window.SMARTBOX_CONFIG) {
            config = this.mergeDeep(config, window.SMARTBOX_CONFIG);
        }

        // Check for environment-specific overrides
        const env = this.detectEnvironment();
        if (env.overrides) {
            config = this.mergeDeep(config, env.overrides);
        }

        // Check localStorage for runtime overrides
        const stored = localStorage.getItem('smartbox_streaming_config');
        if (stored) {
            try {
                const storedConfig = JSON.parse(stored);
                config = this.mergeDeep(config, storedConfig);
            } catch (e) {
                console.warn('Failed to parse stored configuration:', e);
            }
        }

        // Apply URL parameters for debugging
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.has('apiUrl')) {
            config.apiUrl = urlParams.get('apiUrl');
        }
        if (urlParams.has('wsUrl')) {
            config.wsUrl = urlParams.get('wsUrl');
        }

        return config;
    }

    detectEnvironment() {
        const hostname = window.location.hostname;
        const protocol = window.location.protocol;

        // Production environment
        if (hostname !== 'localhost' && hostname !== '127.0.0.1' && !hostname.startsWith('192.168.')) {
            return {
                name: 'production',
                overrides: {
                    apiUrl: `${protocol}//${hostname}:5002/api`,
                    wsUrl: `${protocol === 'https:' ? 'wss:' : 'ws:'}//${hostname}:5001`,
                    authRefreshInterval: 300000, // 5 minutes in production
                    hlsOptions: {
                        maxBufferLength: 60,
                        maxMaxBufferLength: 1200
                    }
                }
            };
        }

        // Development environment
        return {
            name: 'development',
            overrides: {}
        };
    }

    mergeDeep(target, source) {
        const output = Object.assign({}, target);
        if (this.isObject(target) && this.isObject(source)) {
            Object.keys(source).forEach(key => {
                if (this.isObject(source[key])) {
                    if (!(key in target)) {
                        Object.assign(output, { [key]: source[key] });
                    } else {
                        output[key] = this.mergeDeep(target[key], source[key]);
                    }
                } else {
                    Object.assign(output, { [key]: source[key] });
                }
            });
        }
        return output;
    }

    isObject(item) {
        return item && typeof item === 'object' && !Array.isArray(item);
    }

    get(path, defaultValue = undefined) {
        return path.split('.').reduce((obj, key) => 
            obj && obj[key] !== undefined ? obj[key] : defaultValue, this.config);
    }

    set(path, value) {
        const keys = path.split('.');
        const lastKey = keys.pop();
        const target = keys.reduce((obj, key) => {
            if (!obj[key]) obj[key] = {};
            return obj[key];
        }, this.config);
        target[lastKey] = value;

        // Save to localStorage
        this.save();
    }

    save() {
        try {
            localStorage.setItem('smartbox_streaming_config', JSON.stringify(this.config));
        } catch (e) {
            console.error('Failed to save configuration:', e);
        }
    }

    reset() {
        localStorage.removeItem('smartbox_streaming_config');
        this.config = this.loadConfiguration();
    }

    // Convenience getters
    get apiUrl() { return this.config.apiUrl; }
    get wsUrl() { return this.config.wsUrl; }
    get hlsOptions() { return this.config.hlsOptions; }
    get videojsOptions() { return this.config.videojs; }
    get medicalSettings() { return this.config.medical; }
}

// Export as singleton
window.StreamingConfig = new StreamingConfig();