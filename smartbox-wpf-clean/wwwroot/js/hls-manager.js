/**
 * SmartBox HLS Streaming Manager
 * Handles HLS playback with medical-grade features
 */

class HLSManager {
    constructor(videoElement, config) {
        this.video = videoElement;
        this.config = config || window.StreamingConfig;
        this.hls = null;
        this.isLive = false;
        this.dvrEnabled = false;
        this.currentQuality = -1;
        this.frameRate = 30;
        this.seekPrecision = 0.001; // 1ms precision for medical use
        
        // Event handlers
        this.onError = null;
        this.onManifestLoaded = null;
        this.onLevelLoaded = null;
        this.onFragLoaded = null;
        
        this.initializeHLS();
    }

    /**
     * Initialize HLS.js
     */
    initializeHLS() {
        if (!Hls.isSupported()) {
            console.error('HLS.js is not supported in this browser');
            return false;
        }

        const hlsConfig = {
            ...this.config.hlsOptions,
            // Additional medical-grade configurations
            enableWorker: true,
            lowLatencyMode: true,
            backBufferLength: 90, // Keep more buffer for review
            maxBufferLength: 30,
            maxMaxBufferLength: 600,
            manifestLoadingTimeOut: 10000,
            manifestLoadingMaxRetry: 3,
            manifestLoadingRetryDelay: 1000,
            levelLoadingTimeOut: 10000,
            levelLoadingMaxRetry: 4,
            levelLoadingRetryDelay: 1000,
            fragLoadingTimeOut: 20000,
            fragLoadingMaxRetry: 6,
            fragLoadingRetryDelay: 1000,
            startFragPrefetch: true,
            testBandwidth: true,
            progressive: true,
            // Frame-accurate seeking
            nudgeOffset: 0.1,
            nudgeMaxRetry: 5,
            maxFragLookUpTolerance: 0.001
        };

        this.hls = new Hls(hlsConfig);
        this.attachEventListeners();
        
        return true;
    }

    /**
     * Attach HLS event listeners
     */
    attachEventListeners() {
        // Error handling
        this.hls.on(Hls.Events.ERROR, (event, data) => {
            this.handleError(event, data);
        });

        // Manifest loaded
        this.hls.on(Hls.Events.MANIFEST_LOADED, (event, data) => {
            console.log('Manifest loaded:', data);
            this.isLive = data.levels[0].details.live;
            
            // Extract frame rate from manifest if available
            if (data.levels[0].details.targetduration) {
                const segments = data.levels[0].details.fragments;
                if (segments && segments.length > 1) {
                    // Estimate frame rate from segment duration
                    const segmentDuration = segments[1].start - segments[0].start;
                    if (segmentDuration > 0) {
                        this.frameRate = Math.round(1 / segmentDuration);
                    }
                }
            }
            
            if (this.onManifestLoaded) {
                this.onManifestLoaded(data);
            }
        });

        // Level loaded
        this.hls.on(Hls.Events.LEVEL_LOADED, (event, data) => {
            if (this.onLevelLoaded) {
                this.onLevelLoaded(data);
            }
        });

        // Fragment loaded
        this.hls.on(Hls.Events.FRAG_LOADED, (event, data) => {
            if (this.onFragLoaded) {
                this.onFragLoaded(data);
            }
        });

        // Quality changes
        this.hls.on(Hls.Events.LEVEL_SWITCHED, (event, data) => {
            console.log('Quality level switched to:', data.level);
            this.currentQuality = data.level;
        });

        // DVR info
        this.hls.on(Hls.Events.LEVEL_UPDATED, (event, data) => {
            if (data.details.live && data.details.fragments.length > 0) {
                const details = data.details;
                const dvrWindowLength = details.totalduration;
                const liveEdge = details.edge;
                
                this.dvrEnabled = dvrWindowLength > 0;
                
                // Update DVR window info
                if (this.dvrEnabled) {
                    this.video.setAttribute('data-dvr-window', dvrWindowLength);
                    this.video.setAttribute('data-live-edge', liveEdge);
                }
            }
        });
    }

    /**
     * Load HLS stream
     */
    loadStream(url, startTime = null) {
        if (!this.hls) {
            throw new Error('HLS not initialized');
        }

        // Attach to video element
        this.hls.attachMedia(this.video);

        this.hls.on(Hls.Events.MEDIA_ATTACHED, () => {
            console.log('HLS attached to video element');
            
            // Load the stream
            this.hls.loadSource(url);
            
            this.hls.on(Hls.Events.MANIFEST_PARSED, (event, data) => {
                console.log('Manifest parsed, available quality levels:', data.levels);
                
                // Start playback at specific time if requested
                if (startTime !== null && !this.isLive) {
                    this.video.currentTime = startTime;
                }
            });
        });
    }

    /**
     * Handle HLS errors
     */
    handleError(event, data) {
        console.error('HLS error:', data);

        if (data.fatal) {
            switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    console.error('Fatal network error encountered, trying to recover...');
                    // Try to recover network error
                    this.hls.startLoad();
                    break;
                    
                case Hls.ErrorTypes.MEDIA_ERROR:
                    console.error('Fatal media error encountered, trying to recover...');
                    // Try to recover media error
                    this.hls.recoverMediaError();
                    break;
                    
                default:
                    console.error('Fatal error, cannot recover');
                    // Cannot recover, notify application
                    if (this.onError) {
                        this.onError(data);
                    }
                    this.destroy();
                    break;
            }
        }
    }

    /**
     * Seek to specific time with frame accuracy
     */
    seekToTime(targetTime) {
        if (!this.video || this.video.readyState < 2) {
            console.warn('Video not ready for seeking');
            return;
        }

        // For frame-accurate seeking
        const frameDuration = 1 / this.frameRate;
        const nearestFrame = Math.round(targetTime / frameDuration) * frameDuration;
        
        // Use precise seeking
        this.video.currentTime = nearestFrame;
        
        // For even more precision, we can use fragment loading
        if (this.hls && this.hls.media) {
            this.hls.currentTime = nearestFrame;
        }
    }

    /**
     * Step frame forward or backward
     */
    stepFrame(direction) {
        const frameDuration = 1 / this.frameRate;
        const currentTime = this.video.currentTime;
        const targetTime = currentTime + (frameDuration * direction);
        
        this.seekToTime(Math.max(0, targetTime));
    }

    /**
     * Jump to specific number of seconds
     */
    jump(seconds) {
        const targetTime = this.video.currentTime + seconds;
        this.seekToTime(Math.max(0, targetTime));
    }

    /**
     * Go to live edge (for live streams)
     */
    goToLive() {
        if (!this.isLive || !this.hls) {
            return;
        }

        const liveSyncPosition = this.hls.liveSyncPosition;
        if (liveSyncPosition !== null) {
            this.video.currentTime = liveSyncPosition;
        }
    }

    /**
     * Get DVR window information
     */
    getDVRInfo() {
        if (!this.isLive || !this.hls || !this.hls.levels[this.hls.currentLevel]) {
            return null;
        }

        const details = this.hls.levels[this.hls.currentLevel].details;
        if (!details) {
            return null;
        }

        return {
            enabled: this.dvrEnabled,
            windowLength: details.totalduration,
            windowStart: details.fragments[0]?.start || 0,
            windowEnd: details.edge,
            liveEdge: details.edge,
            currentTime: this.video.currentTime,
            behindLive: details.edge - this.video.currentTime
        };
    }

    /**
     * Get available quality levels
     */
    getQualityLevels() {
        if (!this.hls || !this.hls.levels) {
            return [];
        }

        return this.hls.levels.map((level, index) => ({
            index: index,
            bitrate: level.bitrate,
            width: level.width,
            height: level.height,
            codec: level.codec,
            name: `${level.height}p`
        }));
    }

    /**
     * Set quality level
     */
    setQualityLevel(levelIndex) {
        if (!this.hls) return;

        if (levelIndex === -1) {
            // Auto quality
            this.hls.currentLevel = -1;
        } else if (levelIndex >= 0 && levelIndex < this.hls.levels.length) {
            this.hls.currentLevel = levelIndex;
        }
    }

    /**
     * Get current quality level
     */
    getCurrentQuality() {
        if (!this.hls) return -1;
        return this.hls.currentLevel;
    }

    /**
     * Get buffered ranges
     */
    getBufferedRanges() {
        const buffered = this.video.buffered;
        const ranges = [];
        
        for (let i = 0; i < buffered.length; i++) {
            ranges.push({
                start: buffered.start(i),
                end: buffered.end(i)
            });
        }
        
        return ranges;
    }

    /**
     * Get stream statistics
     */
    getStats() {
        if (!this.hls) return null;

        return {
            bandwidth: this.hls.bandwidthEstimate,
            currentLevel: this.hls.currentLevel,
            loadLevel: this.hls.loadLevel,
            droppedFrames: this.video.getVideoPlaybackQuality ? 
                this.video.getVideoPlaybackQuality().droppedVideoFrames : 0,
            decodedFrames: this.video.getVideoPlaybackQuality ?
                this.video.getVideoPlaybackQuality().totalVideoFrames : 0,
            bufferedDuration: this.getBufferedDuration()
        };
    }

    /**
     * Get total buffered duration
     */
    getBufferedDuration() {
        const buffered = this.video.buffered;
        let total = 0;
        
        for (let i = 0; i < buffered.length; i++) {
            total += buffered.end(i) - buffered.start(i);
        }
        
        return total;
    }

    /**
     * Enable/disable DVR mode
     */
    setDVRMode(enabled) {
        this.dvrEnabled = enabled;
        
        if (this.hls && this.isLive) {
            // Reload with DVR parameters if needed
            const currentSource = this.hls.url;
            if (currentSource) {
                const url = new URL(currentSource);
                if (enabled) {
                    url.searchParams.set('dvr', 'true');
                } else {
                    url.searchParams.delete('dvr');
                }
                this.loadStream(url.toString());
            }
        }
    }

    /**
     * Destroy HLS instance
     */
    destroy() {
        if (this.hls) {
            this.hls.destroy();
            this.hls = null;
        }
    }
}

// Export
window.HLSManager = HLSManager;