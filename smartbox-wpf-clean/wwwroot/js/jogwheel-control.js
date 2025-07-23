/**
 * SmartBox Jogwheel Control
 * Ein innovatives Touch-Interface für präzises Video-Scrubbing
 * 
 * Inspiriert von professionellen Video-Editing-Konsolen,
 * optimiert für Touch-Bedienung
 */

class JogwheelControl {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            size: 200,
            mass: 2.0,
            friction: 0.95,
            springConstant: 0.1,
            sensitivity: 1.0,
            hapticFeedback: true,
            ...options
        };
        
        this.state = {
            angle: 0,
            velocity: 0,
            isActive: false,
            lastTouchAngle: 0,
            touchHistory: [],
            momentum: 0
        };
        
        this.callbacks = {
            onScrub: null,
            onStart: null,
            onStop: null
        };
        
        this.detents = [
            { angle: 0, strength: 0.8, label: 'Start' },
            { angle: Math.PI/2, strength: 0.3 },
            { angle: Math.PI, strength: 0.3 },
            { angle: -Math.PI/2, strength: 0.3 }
        ];
        
        this.init();
    }
    
    init() {
        this.createDOM();
        this.setupEventListeners();
        this.startPhysicsLoop();
    }
    
    createDOM() {
        const wheel = document.createElement('div');
        wheel.className = 'jogwheel';
        wheel.innerHTML = `
            <div class="jogwheel-ring">
                <div class="jogwheel-indicator"></div>
                <div class="jogwheel-center">
                    <div class="jogwheel-grip"></div>
                </div>
                <div class="jogwheel-markers">
                    ${this.createMarkers()}
                </div>
            </div>
            <div class="jogwheel-info">
                <span class="jogwheel-speed">0x</span>
                <span class="jogwheel-mode">PRÄZISION</span>
            </div>
        `;
        
        wheel.style.width = this.options.size + 'px';
        wheel.style.height = this.options.size + 'px';
        
        this.container.appendChild(wheel);
        this.elements = {
            wheel: wheel,
            indicator: wheel.querySelector('.jogwheel-indicator'),
            speedDisplay: wheel.querySelector('.jogwheel-speed'),
            modeDisplay: wheel.querySelector('.jogwheel-mode'),
            ring: wheel.querySelector('.jogwheel-ring')
        };
    }
    
    createMarkers() {
        let markers = '';
        const count = 12;
        for (let i = 0; i < count; i++) {
            const angle = (i / count) * 360;
            markers += `<div class="jogwheel-marker" style="transform: rotate(${angle}deg)"></div>`;
        }
        return markers;
    }
    
    setupEventListeners() {
        // Touch events
        this.elements.wheel.addEventListener('touchstart', this.handleTouchStart.bind(this));
        this.elements.wheel.addEventListener('touchmove', this.handleTouchMove.bind(this));
        this.elements.wheel.addEventListener('touchend', this.handleTouchEnd.bind(this));
        
        // Mouse events als Fallback
        this.elements.wheel.addEventListener('mousedown', this.handleMouseDown.bind(this));
        
        // Prevent default touch behavior
        this.elements.wheel.addEventListener('touchstart', (e) => e.preventDefault(), { passive: false });
    }
    
    handleTouchStart(e) {
        const touch = e.touches[0];
        const angle = this.getTouchAngle(touch);
        
        this.state.isActive = true;
        this.state.lastTouchAngle = angle;
        this.state.touchHistory = [{
            angle: angle,
            time: Date.now()
        }];
        
        this.elements.wheel.classList.add('active');
        
        if (this.callbacks.onStart) {
            this.callbacks.onStart();
        }
        
        // Visuelles Feedback
        this.pulseEffect();
    }
    
    handleTouchMove(e) {
        if (!this.state.isActive) return;
        
        const touch = e.touches[0];
        const currentAngle = this.getTouchAngle(touch);
        let deltaAngle = currentAngle - this.state.lastTouchAngle;
        
        // Handle angle wraparound
        if (deltaAngle > Math.PI) deltaAngle -= 2 * Math.PI;
        if (deltaAngle < -Math.PI) deltaAngle += 2 * Math.PI;
        
        // Update state
        this.state.angle += deltaAngle;
        this.state.lastTouchAngle = currentAngle;
        
        // Track touch history for momentum
        this.state.touchHistory.push({
            angle: currentAngle,
            time: Date.now()
        });
        
        // Keep only recent history
        const now = Date.now();
        this.state.touchHistory = this.state.touchHistory.filter(h => now - h.time < 100);
        
        // Calculate velocity from touch history
        if (this.state.touchHistory.length > 1) {
            const first = this.state.touchHistory[0];
            const last = this.state.touchHistory[this.state.touchHistory.length - 1];
            const timeDelta = (last.time - first.time) / 1000;
            if (timeDelta > 0) {
                this.state.velocity = (last.angle - first.angle) / timeDelta;
            }
        }
        
        // Update visual
        this.updateVisual();
        
        // Callback with scrub amount
        if (this.callbacks.onScrub) {
            const scrubSpeed = this.calculateScrubSpeed(deltaAngle, this.state.velocity);
            this.callbacks.onScrub(scrubSpeed);
        }
        
        // Haptic feedback bei Detents
        this.checkDetents(this.state.angle);
    }
    
    handleTouchEnd(e) {
        this.state.isActive = false;
        this.elements.wheel.classList.remove('active');
        
        // Calculate momentum from final velocity
        this.state.momentum = this.state.velocity * this.options.mass;
        
        if (this.callbacks.onStop) {
            this.callbacks.onStop();
        }
    }
    
    handleMouseDown(e) {
        // Mouse-Simulation für Desktop-Testing
        const mouseMove = (e) => {
            const rect = this.elements.wheel.getBoundingClientRect();
            const touch = {
                clientX: e.clientX,
                clientY: e.clientY
            };
            this.handleTouchMove({ touches: [touch], preventDefault: () => {} });
        };
        
        const mouseUp = () => {
            document.removeEventListener('mousemove', mouseMove);
            document.removeEventListener('mouseup', mouseUp);
            this.handleTouchEnd({});
        };
        
        document.addEventListener('mousemove', mouseMove);
        document.addEventListener('mouseup', mouseUp);
        
        const rect = this.elements.wheel.getBoundingClientRect();
        this.handleTouchStart({ 
            touches: [{
                clientX: e.clientX,
                clientY: e.clientY
            }],
            preventDefault: () => {}
        });
    }
    
    getTouchAngle(touch) {
        const rect = this.elements.wheel.getBoundingClientRect();
        const centerX = rect.left + rect.width / 2;
        const centerY = rect.top + rect.height / 2;
        
        const x = touch.clientX - centerX;
        const y = touch.clientY - centerY;
        
        return Math.atan2(y, x);
    }
    
    calculateScrubSpeed(deltaAngle, velocity) {
        const baseSpeed = deltaAngle * this.options.sensitivity;
        const velocityFactor = Math.abs(velocity) / 10; // Geschwindigkeitsbonus
        
        // Modi basierend auf Geschwindigkeit
        let mode = 'PRÄZISION';
        let multiplier = 1;
        
        if (Math.abs(velocity) > 5) {
            mode = 'SCHNELL';
            multiplier = 5;
        } else if (Math.abs(velocity) > 2) {
            mode = 'NORMAL';
            multiplier = 2;
        }
        
        this.updateMode(mode, velocityFactor);
        
        return baseSpeed * multiplier;
    }
    
    updateMode(mode, speed) {
        this.elements.modeDisplay.textContent = mode;
        this.elements.speedDisplay.textContent = speed.toFixed(1) + 'x';
        
        // Visuelle Anpassung basierend auf Modus
        this.elements.wheel.className = 'jogwheel ' + mode.toLowerCase();
    }
    
    checkDetents(angle) {
        const normalizedAngle = angle % (2 * Math.PI);
        
        for (let detent of this.detents) {
            const distance = Math.abs(normalizedAngle - detent.angle);
            if (distance < 0.1) { // Innerhalb von ~6 Grad
                const force = detent.strength * (1 - distance / 0.1);
                
                // Magnetische Anziehung
                this.state.velocity *= (1 - force * 0.5);
                
                // Haptic feedback
                if (this.options.hapticFeedback && window.navigator.vibrate) {
                    window.navigator.vibrate(force * 10);
                }
                
                // Visuelles Feedback
                this.elements.ring.style.transform = `scale(${1 + force * 0.02})`;
                
                break;
            }
        }
    }
    
    startPhysicsLoop() {
        const update = () => {
            if (!this.state.isActive && Math.abs(this.state.momentum) > 0.01) {
                // Apply momentum
                this.state.angle += this.state.momentum * 0.016; // 60fps
                
                // Apply friction
                this.state.momentum *= this.options.friction;
                
                // Apply spring force (Rückstellung zur Nullposition)
                const springForce = -this.state.angle * this.options.springConstant;
                this.state.momentum += springForce * 0.016;
                
                // Update visual
                this.updateVisual();
                
                // Callback
                if (this.callbacks.onScrub) {
                    this.callbacks.onScrub(this.state.momentum * this.options.sensitivity);
                }
            }
            
            requestAnimationFrame(update);
        };
        
        update();
    }
    
    updateVisual() {
        const degrees = (this.state.angle * 180 / Math.PI) % 360;
        this.elements.indicator.style.transform = `rotate(${degrees}deg)`;
        
        // Subtle scaling based on velocity
        const scale = 1 + Math.min(Math.abs(this.state.velocity) * 0.01, 0.1);
        this.elements.ring.style.transform = `scale(${scale})`;
    }
    
    pulseEffect() {
        this.elements.wheel.classList.add('pulse');
        setTimeout(() => {
            this.elements.wheel.classList.remove('pulse');
        }, 300);
    }
    
    // Public API
    on(event, callback) {
        if (event === 'scrub') this.callbacks.onScrub = callback;
        if (event === 'start') this.callbacks.onStart = callback;
        if (event === 'stop') this.callbacks.onStop = callback;
    }
    
    reset() {
        this.state.angle = 0;
        this.state.velocity = 0;
        this.state.momentum = 0;
        this.updateVisual();
    }
    
    show() {
        this.elements.wheel.classList.add('visible');
    }
    
    hide() {
        this.elements.wheel.classList.remove('visible');
    }
}

// Export für Module
if (typeof module !== 'undefined' && module.exports) {
    module.exports = JogwheelControl;
}