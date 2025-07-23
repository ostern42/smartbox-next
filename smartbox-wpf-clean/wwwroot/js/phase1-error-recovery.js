/**
 * Phase 1 Error Recovery & Fallback Strategies
 * Comprehensive error handling for Foundation Integration components
 */

class Phase1ErrorRecovery {
    constructor(streamingPlayer) {
        this.player = streamingPlayer;
        this.errorHistory = [];
        this.maxErrorHistory = 50;
        this.recoveryStrategies = new Map();
        this.fallbackModes = new Map();
        
        this.setupRecoveryStrategies();
        this.setupFallbackModes();
    }
    
    setupRecoveryStrategies() {
        // WebSocket connection errors
        this.recoveryStrategies.set('WEBSOCKET_CONNECTION_FAILED', [
            () => this.retryWebSocketConnection(),
            () => this.switchToPollingMode(),
            () => this.disableRealTimeUpdates()
        ]);
        
        // FFmpeg API errors
        this.recoveryStrategies.set('FFMPEG_API_ERROR', [
            () => this.retryFFmpegConnection(),
            () => this.fallbackToLegacyMode(),
            () => this.disableVideoEngineIntegration()
        ]);
        
        // Thumbnail generation errors
        this.recoveryStrategies.set('THUMBNAIL_GENERATION_FAILED', [
            () => this.retryThumbnailGeneration(),
            () => this.fallbackToVideoFrameExtraction(),
            () => this.usePlaceholderThumbnails()
        ]);
        
        // Network connectivity errors
        this.recoveryStrategies.set('NETWORK_ERROR', [
            () => this.waitAndRetry(2000),
            () => this.enableOfflineMode(),
            () => this.disableNetworkFeatures()
        ]);
        
        // Media playback errors
        this.recoveryStrategies.set('MEDIA_ERROR', [
            () => this.recoverMediaError(),
            () => this.reloadMediaSource(),
            () => this.fallbackToBasicPlayer()
        ]);
    }
    
    setupFallbackModes() {
        // WebSocket → Polling fallback
        this.fallbackModes.set('websocket', {
            fallback: 'polling',
            checkInterval: 5000,
            implementation: () => this.enablePollingMode()
        });
        
        // FFmpeg API → Legacy mode fallback
        this.fallbackModes.set('ffmpeg_api', {
            fallback: 'legacy',
            checkInterval: null,
            implementation: () => this.enableLegacyMode()
        });
        
        // API thumbnails → Video extraction fallback
        this.fallbackModes.set('api_thumbnails', {
            fallback: 'video_extraction',
            checkInterval: null,
            implementation: () => this.enableVideoExtractionThumbnails()
        });
    }
    
    async handleError(error, context = {}) {
        console.error('Phase 1 Error:', error, context);
        
        // Log error to history
        this.logError(error, context);
        
        // Determine error category
        const errorType = this.categorizeError(error);
        
        // Get recovery strategies for this error type
        const strategies = this.recoveryStrategies.get(errorType) || [];
        
        // Try recovery strategies in order
        const recovered = await this.tryRecoveryStrategies(strategies, error, context);
        
        if (!recovered) {
            console.error('All recovery strategies failed for:', errorType);
            this.enableFailsafeMode(errorType);
        }
        
        return recovered;
    }
    
    categorizeError(error) {
        if (!error) return 'UNKNOWN_ERROR';
        
        const message = error.message || '';
        const code = error.code || '';
        
        // WebSocket errors
        if (message.includes('WebSocket') || code.includes('WS_')) {
            return 'WEBSOCKET_CONNECTION_FAILED';
        }
        
        // FFmpeg API errors
        if (message.includes('FFmpeg') || message.includes('video engine') || code.includes('API_')) {
            return 'FFMPEG_API_ERROR';
        }
        
        // Thumbnail errors
        if (message.includes('thumbnail') || message.includes('Thumbnail')) {
            return 'THUMBNAIL_GENERATION_FAILED';
        }
        
        // Network errors
        if (message.includes('fetch') || message.includes('network') || code.includes('NETWORK_')) {
            return 'NETWORK_ERROR';
        }
        
        // Media errors
        if (message.includes('media') || message.includes('video') || code.includes('MEDIA_')) {
            return 'MEDIA_ERROR';
        }
        
        return 'UNKNOWN_ERROR';
    }
    
    async tryRecoveryStrategies(strategies, error, context) {
        for (let i = 0; i < strategies.length; i++) {
            const strategy = strategies[i];
            
            try {
                console.log(`Trying recovery strategy ${i + 1}/${strategies.length}`);
                const result = await strategy(error, context);
                
                if (result) {
                    console.log(`Recovery strategy ${i + 1} succeeded`);
                    return true;
                }
            } catch (strategyError) {
                console.error(`Recovery strategy ${i + 1} failed:`, strategyError);
            }
        }
        
        return false;
    }
    
    logError(error, context) {
        const errorEntry = {
            timestamp: Date.now(),
            error: {
                message: error.message || 'Unknown error',
                code: error.code || 'NO_CODE',
                stack: error.stack || 'No stack trace'
            },
            context: context || {},
            userAgent: navigator.userAgent,
            url: window.location.href
        };
        
        this.errorHistory.unshift(errorEntry);
        
        // Keep history within limits
        if (this.errorHistory.length > this.maxErrorHistory) {
            this.errorHistory = this.errorHistory.slice(0, this.maxErrorHistory);
        }
        
        // Send to monitoring service if available
        this.sendErrorToMonitoring(errorEntry);
    }
    
    sendErrorToMonitoring(errorEntry) {
        // Only send critical errors to avoid spam
        if (this.isCriticalError(errorEntry.error)) {
            try {
                // Implement your monitoring service integration here
                console.log('Critical error logged:', errorEntry);
            } catch (e) {
                console.error('Failed to send error to monitoring:', e);
            }
        }
    }
    
    isCriticalError(error) {
        const criticalKeywords = ['fatal', 'critical', 'crash', 'memory', 'security'];
        const message = (error.message || '').toLowerCase();
        
        return criticalKeywords.some(keyword => message.includes(keyword));
    }
    
    // Recovery Strategy Implementations
    
    async retryWebSocketConnection() {
        if (!this.player.wsHandler) return false;
        
        try {
            this.player.wsHandler.resetReconnection();
            await this.waitAndRetry(1000);
            this.player.wsHandler.establishConnection();
            return true;
        } catch (error) {
            console.error('WebSocket retry failed:', error);
            return false;
        }
    }
    
    async switchToPollingMode() {
        try {
            console.log('Switching to polling mode for real-time updates');
            this.enablePollingMode();
            return true;
        } catch (error) {
            console.error('Failed to switch to polling mode:', error);
            return false;
        }
    }
    
    async disableRealTimeUpdates() {
        try {
            console.log('Disabling real-time updates');
            if (this.player.wsHandler) {
                this.player.wsHandler.disconnect();
            }
            this.player.options.enableWebSocketUpdates = false;
            return true;
        } catch (error) {
            console.error('Failed to disable real-time updates:', error);
            return false;
        }
    }
    
    async retryFFmpegConnection() {
        if (!this.player.videoEngine) return false;
        
        try {
            await this.player.videoEngine.initialize();
            return true;
        } catch (error) {
            console.error('FFmpeg retry failed:', error);
            return false;
        }
    }
    
    async fallbackToLegacyMode() {
        try {
            console.log('Falling back to legacy mode');
            this.player.options.enableFFmpegIntegration = false;
            this.enableLegacyMode();
            return true;
        } catch (error) {
            console.error('Failed to enable legacy mode:', error);
            return false;
        }
    }
    
    async disableVideoEngineIntegration() {
        try {
            console.log('Disabling video engine integration');
            if (this.player.videoEngine) {
                this.player.videoEngine.cleanup();
                this.player.videoEngine = null;
            }
            this.player.options.enableFFmpegIntegration = false;
            return true;
        } catch (error) {
            console.error('Failed to disable video engine:', error);
            return false;
        }
    }
    
    async retryThumbnailGeneration() {
        try {
            // Clear thumbnail cache and retry
            this.player.clearThumbnailCache();
            return true;
        } catch (error) {
            console.error('Thumbnail retry failed:', error);
            return false;
        }
    }
    
    async fallbackToVideoFrameExtraction() {
        try {
            console.log('Falling back to video frame extraction for thumbnails');
            this.player.options.enableUnifiedThumbnails = false;
            return true;
        } catch (error) {
            console.error('Failed to enable video frame extraction:', error);
            return false;
        }
    }
    
    async usePlaceholderThumbnails() {
        try {
            console.log('Using placeholder thumbnails');
            // Override thumbnail loading to always use placeholders
            this.player.loadThumbnail = (timestamp) => 
                Promise.resolve(this.player.getFallbackThumbnail(timestamp));
            return true;
        } catch (error) {
            console.error('Failed to enable placeholder thumbnails:', error);
            return false;
        }
    }
    
    async waitAndRetry(delay) {
        return new Promise(resolve => {
            setTimeout(() => resolve(true), delay);
        });
    }
    
    async enableOfflineMode() {
        try {
            console.log('Enabling offline mode');
            // Implement offline capabilities
            this.player.isOffline = true;
            return true;
        } catch (error) {
            console.error('Failed to enable offline mode:', error);
            return false;
        }
    }
    
    async disableNetworkFeatures() {
        try {
            console.log('Disabling network-dependent features');
            this.player.options.enableWebSocketUpdates = false;
            this.player.options.enableFFmpegIntegration = false;
            return true;
        } catch (error) {
            console.error('Failed to disable network features:', error);
            return false;
        }
    }
    
    async recoverMediaError() {
        if (!this.player.primaryHLS) return false;
        
        try {
            this.player.primaryHLS.recoverMediaError();
            return true;
        } catch (error) {
            console.error('Media error recovery failed:', error);
            return false;
        }
    }
    
    async reloadMediaSource() {
        if (!this.player.primaryHLS) return false;
        
        try {
            const currentSource = this.player.primaryHLS.url;
            if (currentSource) {
                this.player.primaryHLS.loadSource(currentSource);
                return true;
            }
            return false;
        } catch (error) {
            console.error('Media source reload failed:', error);
            return false;
        }
    }
    
    async fallbackToBasicPlayer() {
        try {
            console.log('Falling back to basic player');
            // Disable advanced features and use basic HTML5 video
            this.player.useBasicPlayer = true;
            return true;
        } catch (error) {
            console.error('Failed to enable basic player:', error);
            return false;
        }
    }
    
    // Fallback Mode Implementations
    
    enablePollingMode() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
        }
        
        this.pollingInterval = setInterval(async () => {
            try {
                if (this.player.videoEngine && this.player.currentSessionId) {
                    const status = await this.player.videoEngine.getRecordingStatus();
                    if (status) {
                        this.player.updateRecordingStatus(status);
                    }
                }
            } catch (error) {
                console.warn('Polling update failed:', error);
            }
        }, 5000);
        
        console.log('Polling mode enabled');
    }
    
    enableLegacyMode() {
        console.log('Legacy mode enabled - using traditional streaming without FFmpeg integration');
        // Implement legacy streaming behavior
        this.player.isLegacyMode = true;
    }
    
    enableVideoExtractionThumbnails() {
        console.log('Video extraction thumbnails enabled');
        this.player.thumbnailExtractionOnly = true;
    }
    
    enableFailsafeMode(errorType) {
        console.warn(`Enabling failsafe mode due to: ${errorType}`);
        
        // Disable all advanced features
        this.player.options.enableFFmpegIntegration = false;
        this.player.options.enableWebSocketUpdates = false;
        this.player.options.enableUnifiedThumbnails = false;
        
        // Show user notification
        this.showFailsafeNotification(errorType);
        
        // Log failsafe activation
        this.logError(new Error(`Failsafe mode activated: ${errorType}`), { 
            failsafe: true,
            reason: errorType 
        });
    }
    
    showFailsafeNotification(errorType) {
        const message = `System running in safe mode due to ${errorType}. Some features may be limited.`;
        
        if (this.player.showErrorIndicator) {
            this.player.showErrorIndicator(message);
        } else {
            console.warn(message);
        }
    }
    
    // Utility methods
    
    getErrorStats() {
        const stats = {
            totalErrors: this.errorHistory.length,
            errorsByType: {},
            recentErrors: this.errorHistory.slice(0, 10),
            criticalErrors: this.errorHistory.filter(e => this.isCriticalError(e.error))
        };
        
        this.errorHistory.forEach(entry => {
            const type = this.categorizeError(entry.error);
            stats.errorsByType[type] = (stats.errorsByType[type] || 0) + 1;
        });
        
        return stats;
    }
    
    clearErrorHistory() {
        this.errorHistory = [];
        console.log('Error history cleared');
    }
    
    cleanup() {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
            this.pollingInterval = null;
        }
        
        this.errorHistory = [];
        this.recoveryStrategies.clear();
        this.fallbackModes.clear();
        
        console.log('Phase1ErrorRecovery cleanup completed');
    }
}

// Export for use in other modules
window.Phase1ErrorRecovery = Phase1ErrorRecovery;