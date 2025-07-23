/**
 * Playhead Controls für SmartBox Timeline
 * Klassische Transport-Steuerung für präzise Navigation
 */

class PlayheadControls {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            onSeek: null,
            onPlay: null,
            onPause: null,
            ...options
        };
        
        this.state = {
            isPlaying: false,
            currentTime: 0,
            duration: 0,
            playbackRate: 1
        };
        
        this.init();
    }
    
    init() {
        this.createDOM();
        this.setupEventListeners();
    }
    
    createDOM() {
        const controls = document.createElement('div');
        controls.className = 'playhead-controls';
        controls.innerHTML = `
            <div class="transport-controls">
                <!-- Frame zurück -->
                <button class="transport-btn frame-back" title="1 Frame zurück">
                    <span class="btn-icon">|◄</span>
                </button>
                
                <!-- 10 Sekunden zurück -->
                <button class="transport-btn jump-back" title="10 Sekunden zurück">
                    <span class="btn-icon">◄◄</span>
                    <span class="btn-label">10s</span>
                </button>
                
                <!-- Play/Pause -->
                <button class="transport-btn play-pause" title="Abspielen/Pause">
                    <span class="btn-icon play">►</span>
                    <span class="btn-icon pause" style="display: none;">||</span>
                </button>
                
                <!-- 10 Sekunden vor -->
                <button class="transport-btn jump-forward" title="10 Sekunden vor">
                    <span class="btn-label">10s</span>
                    <span class="btn-icon">►►</span>
                </button>
                
                <!-- Frame vor -->
                <button class="transport-btn frame-forward" title="1 Frame vor">
                    <span class="btn-icon">►|</span>
                </button>
            </div>
            
            <div class="time-display">
                <span class="current-time">00:00.00</span>
                <span class="time-separator">/</span>
                <span class="total-time">00:00.00</span>
            </div>
            
            <div class="speed-controls">
                <button class="speed-btn" data-speed="0.5">0.5x</button>
                <button class="speed-btn active" data-speed="1">1x</button>
                <button class="speed-btn" data-speed="2">2x</button>
            </div>
        `;
        
        this.container.appendChild(controls);
        
        // Cache elements
        this.elements = {
            controls: controls,
            playPauseBtn: controls.querySelector('.play-pause'),
            playIcon: controls.querySelector('.play-pause .play'),
            pauseIcon: controls.querySelector('.play-pause .pause'),
            frameBackBtn: controls.querySelector('.frame-back'),
            frameForwardBtn: controls.querySelector('.frame-forward'),
            jumpBackBtn: controls.querySelector('.jump-back'),
            jumpForwardBtn: controls.querySelector('.jump-forward'),
            currentTime: controls.querySelector('.current-time'),
            totalTime: controls.querySelector('.total-time'),
            speedBtns: controls.querySelectorAll('.speed-btn')
        };
    }
    
    setupEventListeners() {
        // Play/Pause
        this.elements.playPauseBtn.addEventListener('click', () => {
            this.togglePlayPause();
        });
        
        // Frame navigation
        this.elements.frameBackBtn.addEventListener('click', () => {
            this.seekFrame(-1);
        });
        
        this.elements.frameForwardBtn.addEventListener('click', () => {
            this.seekFrame(1);
        });
        
        // Jump navigation
        this.elements.jumpBackBtn.addEventListener('click', () => {
            this.seek(this.state.currentTime - 10);
        });
        
        this.elements.jumpForwardBtn.addEventListener('click', () => {
            this.seek(this.state.currentTime + 10);
        });
        
        // Speed controls
        this.elements.speedBtns.forEach(btn => {
            btn.addEventListener('click', () => {
                const speed = parseFloat(btn.dataset.speed);
                this.setPlaybackRate(speed);
                
                // Update active state
                this.elements.speedBtns.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
            });
        });
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (!this.container.contains(document.activeElement)) return;
            
            switch(e.key) {
                case ' ':
                    e.preventDefault();
                    this.togglePlayPause();
                    break;
                case 'ArrowLeft':
                    if (e.shiftKey) {
                        this.seek(this.state.currentTime - 10);
                    } else {
                        this.seekFrame(-1);
                    }
                    break;
                case 'ArrowRight':
                    if (e.shiftKey) {
                        this.seek(this.state.currentTime + 10);
                    } else {
                        this.seekFrame(1);
                    }
                    break;
                case 'j':
                    this.seek(this.state.currentTime - 10);
                    break;
                case 'k':
                    this.togglePlayPause();
                    break;
                case 'l':
                    this.seek(this.state.currentTime + 10);
                    break;
            }
        });
    }
    
    // Public API
    
    updateTime(currentTime, duration) {
        this.state.currentTime = currentTime;
        this.state.duration = duration || this.state.duration;
        
        this.elements.currentTime.textContent = this.formatTime(currentTime);
        this.elements.totalTime.textContent = this.formatTime(this.state.duration);
    }
    
    setPlaying(isPlaying) {
        this.state.isPlaying = isPlaying;
        
        if (isPlaying) {
            this.elements.playIcon.style.display = 'none';
            this.elements.pauseIcon.style.display = 'inline';
            this.elements.playPauseBtn.classList.add('playing');
        } else {
            this.elements.playIcon.style.display = 'inline';
            this.elements.pauseIcon.style.display = 'none';
            this.elements.playPauseBtn.classList.remove('playing');
        }
    }
    
    setPlaybackRate(rate) {
        this.state.playbackRate = rate;
        
        if (this.options.onPlaybackRateChange) {
            this.options.onPlaybackRateChange(rate);
        }
    }
    
    // Private methods
    
    togglePlayPause() {
        if (this.state.isPlaying) {
            this.pause();
        } else {
            this.play();
        }
    }
    
    play() {
        this.setPlaying(true);
        
        if (this.options.onPlay) {
            this.options.onPlay();
        }
    }
    
    pause() {
        this.setPlaying(false);
        
        if (this.options.onPause) {
            this.options.onPause();
        }
    }
    
    seek(time) {
        time = Math.max(0, Math.min(time, this.state.duration));
        this.state.currentTime = time;
        
        if (this.options.onSeek) {
            this.options.onSeek(time);
        }
        
        this.updateTime(time);
    }
    
    seekFrame(direction) {
        // Assuming 25 fps
        const frameTime = 1 / 25;
        this.seek(this.state.currentTime + (frameTime * direction));
    }
    
    formatTime(seconds) {
        // Handle invalid values
        if (!isFinite(seconds) || seconds < 0) {
            seconds = 0;
        }
        
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        const ms = Math.floor((seconds % 1) * 100);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
    }
}

// Export
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PlayheadControls;
}