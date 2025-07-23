# Phase 4: Error Recovery & Resilience (Priority: Medium)

This phase implements robust error handling and recovery mechanisms to ensure reliable video streaming even under adverse network conditions or system failures.

## 4.1 Robust Error Handling

**File**: `stream-error-recovery.js` (New)

### Comprehensive Error Recovery System

```javascript
class StreamErrorRecovery {
    constructor(player) {
        this.player = player;
        this.retryCount = 0;
        this.maxRetries = 5;
        this.backoffMultiplier = 2;
        this.baseDelay = 1000;
        
        this.setupErrorHandlers();
    }
    
    setupErrorHandlers() {
        // HLS.js error handling
        if (this.player.hls) {
            this.player.hls.on(Hls.Events.ERROR, (event, data) => {
                this.handleHlsError(data);
            });
        }
        
        // Video element error handling
        this.player.video.addEventListener('error', (e) => {
            this.handleVideoError(e);
        });
        
        // Network monitoring
        window.addEventListener('online', () => this.handleNetworkRestore());
        window.addEventListener('offline', () => this.handleNetworkLoss());
    }
    
    async handleHlsError(data) {
        console.error('HLS Error:', data);
        
        if (data.fatal) {
            switch (data.type) {
                case Hls.ErrorTypes.NETWORK_ERROR:
                    await this.handleNetworkError(data);
                    break;
                    
                case Hls.ErrorTypes.MEDIA_ERROR:
                    await this.handleMediaError(data);
                    break;
                    
                default:
                    await this.handleFatalError(data);
                    break;
            }
        } else {
            // Non-fatal errors - log and continue
            console.warn('Non-fatal HLS error:', data);
            this.player.emit('warning', {
                type: 'hls_error',
                details: data.details,
                fatal: false
            });
        }
    }
}
```

### Network Error Recovery

```javascript
    async handleNetworkError(data) {
        console.log(`Network error, retry ${this.retryCount + 1}/${this.maxRetries}`);
        
        if (this.retryCount < this.maxRetries) {
            this.retryCount++;
            
            // Calculate delay with exponential backoff and jitter
            const delay = this.baseDelay * Math.pow(this.backoffMultiplier, this.retryCount - 1);
            const jitter = Math.random() * 1000;
            const totalDelay = delay + jitter;
            
            this.player.emit('retrying', {
                attempt: this.retryCount,
                delay: totalDelay,
                error: data
            });
            
            await this.wait(totalDelay);
            
            // Try recovery strategies in order
            const recovered = await this.tryRecoveryStrategies([
                () => this.reloadManifest(),
                () => this.switchToAlternativeStream(),
                () => this.downgradeQuality(),
                () => this.fallbackToLowerProtocol()
            ]);
            
            if (recovered) {
                this.retryCount = 0;
                this.player.emit('recovered', { strategy: recovered });
            } else {
                await this.handleNetworkError(data); // Recursive retry
            }
        } else {
            await this.handleFatalError(data);
        }
    }
```

### Media Error Recovery

```javascript
    async handleMediaError(data) {
        console.log('Media error, attempting recovery');
        
        try {
            // First try: Recover media error
            this.player.hls.recoverMediaError();
            
            // Monitor if recovery worked
            await this.wait(2000);
            
            if (this.player.video.error) {
                // Second try: Swap codecs
                console.log('Media recovery failed, swapping codecs');
                this.player.hls.swapAudioCodec();
                this.player.hls.recoverMediaError();
                
                await this.wait(2000);
                
                if (this.player.video.error) {
                    // Final attempt: Full reload
                    await this.reloadPlayer();
                }
            }
        } catch (error) {
            console.error('Media recovery failed:', error);
            await this.handleFatalError(data);
        }
    }
```

### Recovery Strategies

```javascript
    async tryRecoveryStrategies(strategies) {
        for (const strategy of strategies) {
            try {
                const result = await strategy();
                if (result) {
                    return result;
                }
            } catch (error) {
                console.warn('Recovery strategy failed:', error);
            }
        }
        return null;
    }
    
    async reloadManifest() {
        console.log('Reloading manifest...');
        this.player.hls.startLoad();
        await this.wait(2000);
        return !this.player.video.error ? 'manifest_reload' : null;
    }
    
    async switchToAlternativeStream() {
        if (this.player.alternativeUrls && this.player.alternativeUrls.length > 0) {
            console.log('Switching to alternative stream...');
            const altUrl = this.player.alternativeUrls.shift();
            this.player.loadStream(altUrl);
            await this.wait(3000);
            return !this.player.video.error ? 'alternative_stream' : null;
        }
        return null;
    }
    
    async downgradeQuality() {
        if (this.player.hls.levels.length > 1) {
            console.log('Downgrading quality...');
            const currentLevel = this.player.hls.currentLevel;
            if (currentLevel > 0) {
                this.player.hls.currentLevel = 0; // Lowest quality
                await this.wait(1000);
                return !this.player.video.error ? 'quality_downgrade' : null;
            }
        }
        return null;
    }
    
    async fallbackToLowerProtocol() {
        // Implement protocol fallback if available
        // e.g., HTTPS -> HTTP, HLS -> Progressive download
        return null;
    }
```

### Player Recovery

```javascript
    async reloadPlayer() {
        console.log('Performing full player reload...');
        
        const currentTime = this.player.video.currentTime;
        const wasPlaying = !this.player.video.paused;
        
        // Destroy current instance
        this.player.destroy();
        
        // Recreate player
        await this.player.initialize();
        
        // Restore state
        this.player.seek(currentTime);
        if (wasPlaying) {
            this.player.play();
        }
        
        return 'full_reload';
    }
```

### Network Status Handling

```javascript
    handleNetworkLoss() {
        console.log('Network connection lost');
        this.player.emit('offline');
        
        // Pause playback to prevent errors
        this.player.pause();
        
        // Show offline message
        this.player.showMessage('Network connection lost. Playback paused.', 'warning');
    }
    
    handleNetworkRestore() {
        console.log('Network connection restored');
        this.player.emit('online');
        
        // Reload manifest and resume
        this.player.hls.startLoad();
        this.player.hideMessage();
    }
    
    handleFatalError(data) {
        console.error('Fatal error, cannot recover:', data);
        
        this.player.emit('fatalError', data);
        this.player.showMessage(
            'Playback error. Please refresh the page or try again later.',
            'error'
        );
        
        // Disable player
        this.player.disable();
    }
    
    wait(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
    
    reset() {
        this.retryCount = 0;
    }
```

## Error Recovery Features

### Automatic Retry Logic
- Exponential backoff with jitter
- Configurable retry limits
- Progressive recovery strategies

### Recovery Strategies (in order)
1. **Manifest Reload** - Refresh playlist without interruption
2. **Alternative Stream** - Switch to backup stream URL
3. **Quality Downgrade** - Reduce bitrate to maintain playback
4. **Protocol Fallback** - Switch to more reliable protocol

### Media Error Handling
1. **Recover Media Error** - HLS.js built-in recovery
2. **Codec Swap** - Try alternative audio codec
3. **Full Reload** - Complete player restart with state restoration

### Network Monitoring
- Automatic pause on connection loss
- Automatic resume on connection restore
- User-friendly status messages

## Integration with Player

```javascript
// In the main player class
class EnhancedStreamingPlayer {
    constructor(container, options) {
        // ... existing code ...
        
        // Initialize error recovery
        this.errorRecovery = new StreamErrorRecovery(this);
        
        // Alternative stream URLs for failover
        this.alternativeUrls = options.alternativeUrls || [];
    }
    
    showMessage(text, type = 'info') {
        // Implementation for showing user messages
        const messageEl = document.createElement('div');
        messageEl.className = `player-message ${type}`;
        messageEl.textContent = text;
        this.container.appendChild(messageEl);
    }
    
    hideMessage() {
        const messageEl = this.container.querySelector('.player-message');
        if (messageEl) {
            messageEl.remove();
        }
    }
    
    disable() {
        // Disable player controls
        this.video.controls = false;
        this.container.classList.add('disabled');
    }
}
```

## CSS for Error Messages

```css
.player-message {
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    padding: 20px;
    background: rgba(0, 0, 0, 0.8);
    color: white;
    border-radius: 4px;
    z-index: 1000;
    font-size: 14px;
    text-align: center;
}

.player-message.warning {
    background: rgba(255, 193, 7, 0.9);
    color: #333;
}

.player-message.error {
    background: rgba(220, 53, 69, 0.9);
    color: white;
}

.player-container.disabled {
    opacity: 0.5;
    pointer-events: none;
}
```

## Testing Error Recovery

1. **Network Failures**
   - Disconnect network during playback
   - Simulate slow/unstable connections
   - Test with network throttling

2. **Media Errors**
   - Corrupt segment files
   - Invalid codec configurations
   - Missing segments

3. **Recovery Validation**
   - Verify each recovery strategy
   - Test state restoration
   - Monitor retry behavior

## Navigation

- [← Phase 3: Playback Enhancements](03-phase3-playback-enhancements.md)
- [Phase 5: Advanced Features →](05-phase5-advanced-features.md)