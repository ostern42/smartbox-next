// SmartBox Touch Keyboard - QWERTZ Layout with Smart Numeric Input
class TouchKeyboard {
    constructor() {
        this.activeInput = null;
        this.isShiftActive = false;
        this.isCapsLockActive = false;
        this.isAltGrActive = false;
        this.keyboardType = 'qwertz'; // 'qwertz' or 'numeric'
        this.createKeyboard();
        this.attachEventListeners();
    }

    createKeyboard() {
        // Create keyboard container
        const container = document.createElement('div');
        container.id = 'touch-keyboard';
        container.className = 'touch-keyboard hidden';
        container.innerHTML = `
            <div class="keyboard-header">
                <div class="keyboard-title">Bildschirmtastatur</div>
                <button class="keyboard-close" aria-label="Tastatur schließen">✕</button>
            </div>
            <div class="keyboard-body">
                <div id="qwertz-keyboard" class="keyboard-layout"></div>
                <div id="numeric-keyboard" class="keyboard-layout hidden"></div>
            </div>
        `;
        document.body.appendChild(container);

        // Create QWERTZ layout
        this.createQwertzLayout();
        
        // Create numeric layout
        this.createNumericLayout();
    }

    createQwertzLayout() {
        const qwertzContainer = document.getElementById('qwertz-keyboard');
        
        const rows = [
            ['1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'ß', '´', 'Backspace'],
            ['Tab', 'q', 'w', 'e', 'r', 't', 'z', 'u', 'i', 'o', 'p', 'ü', '+'],
            ['Caps', 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'ö', 'ä', '#', 'Enter'],
            ['Shift', '<', 'y', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '-', 'Shift'],
            ['Ctrl', 'Win', 'Alt', 'Space', 'AltGr', '@', 'Ctrl']
        ];

        const shiftMap = {
            '1': '!', '2': '"', '3': '§', '4': '$', '5': '%', '6': '&',
            '7': '/', '8': '(', '9': ')', '0': '=', 'ß': '?', '´': '`',
            '+': '*', '#': "'", '<': '>', ',': ';', '.': ':', '-': '_'
        };

        const altGrMap = {
            'q': '@', 'e': '€', '7': '{', '8': '[', '9': ']', '0': '}',
            'ß': '\\', '+': '~', '<': '|', 'm': 'µ', '2': '²', '3': '³'
        };

        rows.forEach((row, rowIndex) => {
            const rowDiv = document.createElement('div');
            rowDiv.className = 'keyboard-row';
            
            row.forEach(key => {
                const button = document.createElement('button');
                button.className = 'keyboard-key';
                button.setAttribute('data-key', key);
                
                // Special key styling
                if (['Backspace', 'Tab', 'Caps', 'Enter', 'Shift', 'Ctrl', 'Win', 'Alt', 'Space', 'AltGr'].includes(key)) {
                    button.classList.add('special-key');
                    button.classList.add(`key-${key.toLowerCase()}`);
                }
                
                // Key labels
                switch(key) {
                    case 'Backspace':
                        button.innerHTML = '<span class="key-icon">⌫</span><span class="key-text">Löschen</span>';
                        break;
                    case 'Tab':
                        button.innerHTML = '<span class="key-icon">⇥</span><span class="key-text">Tab</span>';
                        break;
                    case 'Caps':
                        button.innerHTML = '<span class="key-icon">⇪</span><span class="key-text">Caps</span>';
                        break;
                    case 'Enter':
                        button.innerHTML = '<span class="key-icon">⏎</span><span class="key-text">Enter</span>';
                        break;
                    case 'Shift':
                        button.innerHTML = '<span class="key-icon">⇧</span><span class="key-text">Shift</span>';
                        break;
                    case 'Space':
                        button.innerHTML = '<span class="key-text">Leertaste</span>';
                        break;
                    case 'Win':
                        button.innerHTML = '<span class="key-icon">⊞</span>';
                        break;
                    default:
                        button.textContent = key;
                }
                
                // Store shift character if available
                if (shiftMap[key]) {
                    button.setAttribute('data-shift', shiftMap[key]);
                }
                
                // Store AltGr character if available
                if (altGrMap[key]) {
                    button.setAttribute('data-altgr', altGrMap[key]);
                }
                
                button.addEventListener('click', (e) => this.handleKeyPress(e));
                rowDiv.appendChild(button);
            });
            
            qwertzContainer.appendChild(rowDiv);
        });
    }

    createNumericLayout() {
        const numericContainer = document.getElementById('numeric-keyboard');
        
        const layout = `
            <div class="numeric-display">
                <input type="text" id="numeric-preview" readonly placeholder="0.0.0.0">
            </div>
            <div class="numeric-grid">
                <button class="num-key" data-num="7">7</button>
                <button class="num-key" data-num="8">8</button>
                <button class="num-key" data-num="9">9</button>
                <button class="num-key" data-num="4">4</button>
                <button class="num-key" data-num="5">5</button>
                <button class="num-key" data-num="6">6</button>
                <button class="num-key" data-num="1">1</button>
                <button class="num-key" data-num="2">2</button>
                <button class="num-key" data-num="3">3</button>
                <button class="num-key special" data-action="dot">.</button>
                <button class="num-key" data-num="0">0</button>
                <button class="num-key special" data-action="backspace">⌫</button>
            </div>
            <div class="numeric-actions">
                <button class="action-key cancel" data-action="cancel">Abbrechen</button>
                <button class="action-key confirm" data-action="confirm">Übernehmen</button>
            </div>
        `;
        
        numericContainer.innerHTML = layout;
        
        // Add event listeners for numeric keys
        numericContainer.querySelectorAll('.num-key, .action-key').forEach(button => {
            button.addEventListener('click', (e) => this.handleNumericKey(e));
        });
    }

    handleKeyPress(e) {
        const button = e.currentTarget;
        const key = button.getAttribute('data-key');
        
        if (!this.activeInput) return;
        
        // Haptic feedback animation
        button.classList.add('pressed');
        setTimeout(() => button.classList.remove('pressed'), 150);
        
        switch(key) {
            case 'Backspace':
                this.deleteCharacter();
                break;
            case 'Tab':
                this.insertText('\t');
                break;
            case 'Caps':
                this.toggleCapsLock();
                break;
            case 'Enter':
                this.insertText('\n');
                break;
            case 'Shift':
                this.toggleShift();
                break;
            case 'Space':
                this.insertText(' ');
                break;
            case 'Ctrl':
            case 'Win':
            case 'Alt':
                // These are for visual completeness
                break;
            case 'AltGr':
                this.toggleAltGr();
                break;
            default:
                let char = key;
                
                // Check for AltGr character first
                if (this.isAltGrActive) {
                    const altGrChar = button.getAttribute('data-altgr');
                    if (altGrChar) {
                        char = altGrChar;
                    }
                } else if (this.isShiftActive || this.isCapsLockActive) {
                    const shiftChar = button.getAttribute('data-shift');
                    if (shiftChar) {
                        char = shiftChar;
                    } else if (char.match(/[a-z]/)) {
                        char = char.toUpperCase();
                    }
                }
                this.insertText(char);
                
                // Auto-release shift and AltGr after character
                if (this.isShiftActive) {
                    this.toggleShift();
                }
                if (this.isAltGrActive) {
                    this.toggleAltGr();
                }
        }
    }

    handleNumericKey(e) {
        const button = e.currentTarget;
        const num = button.getAttribute('data-num');
        const action = button.getAttribute('data-action');
        const preview = document.getElementById('numeric-preview');
        
        // Haptic feedback
        button.classList.add('pressed');
        setTimeout(() => button.classList.remove('pressed'), 150);
        
        if (num) {
            preview.value += num;
        } else if (action) {
            switch(action) {
                case 'dot':
                    if (!preview.value.endsWith('.')) {
                        preview.value += '.';
                    }
                    break;
                case 'backspace':
                    preview.value = preview.value.slice(0, -1);
                    break;
                case 'cancel':
                    this.hideKeyboard();
                    break;
                case 'confirm':
                    if (this.activeInput) {
                        this.activeInput.value = preview.value;
                        this.activeInput.dispatchEvent(new Event('input', { bubbles: true }));
                    }
                    this.hideKeyboard();
                    break;
            }
        }
    }

    toggleShift() {
        this.isShiftActive = !this.isShiftActive;
        document.querySelectorAll('.key-shift').forEach(key => {
            key.classList.toggle('active', this.isShiftActive);
        });
        this.updateKeyLabels();
    }

    toggleCapsLock() {
        this.isCapsLockActive = !this.isCapsLockActive;
        document.querySelector('.key-caps').classList.toggle('active', this.isCapsLockActive);
        this.updateKeyLabels();
    }

    toggleAltGr() {
        this.isAltGrActive = !this.isAltGrActive;
        document.querySelectorAll('.key-altgr').forEach(key => {
            key.classList.toggle('active', this.isAltGrActive);
        });
        // Update key labels to show AltGr characters
        this.updateKeyLabels();
    }

    updateKeyLabels() {
        document.querySelectorAll('.keyboard-key').forEach(button => {
            const key = button.getAttribute('data-key');
            const shiftChar = button.getAttribute('data-shift');
            const altGrChar = button.getAttribute('data-altgr');
            
            // Skip special keys (they have icons/text)
            if (button.classList.contains('special-key')) {
                return;
            }
            
            // Determine which character to show
            let displayChar = key;
            
            // Priority: AltGr > Shift > Caps > Normal
            if (this.isAltGrActive && altGrChar) {
                displayChar = altGrChar;
            } else if (this.isShiftActive && shiftChar) {
                displayChar = shiftChar;
            } else if ((this.isShiftActive || this.isCapsLockActive) && key.match(/^[a-z]$/)) {
                displayChar = key.toUpperCase();
            } else if (key.match(/^[A-Z]$/) && !this.isShiftActive && !this.isCapsLockActive) {
                displayChar = key.toLowerCase();
            }
            
            // Update button text
            button.textContent = displayChar;
        });
    }

    insertText(text) {
        if (!this.activeInput) return;
        
        const start = this.activeInput.selectionStart;
        const end = this.activeInput.selectionEnd;
        const value = this.activeInput.value;
        
        this.activeInput.value = value.substring(0, start) + text + value.substring(end);
        this.activeInput.selectionStart = this.activeInput.selectionEnd = start + text.length;
        this.activeInput.focus();
        this.activeInput.dispatchEvent(new Event('input', { bubbles: true }));
    }

    deleteCharacter() {
        if (!this.activeInput) return;
        
        const start = this.activeInput.selectionStart;
        const end = this.activeInput.selectionEnd;
        const value = this.activeInput.value;
        
        if (start === end && start > 0) {
            this.activeInput.value = value.substring(0, start - 1) + value.substring(end);
            this.activeInput.selectionStart = this.activeInput.selectionEnd = start - 1;
        } else if (start !== end) {
            this.activeInput.value = value.substring(0, start) + value.substring(end);
            this.activeInput.selectionStart = this.activeInput.selectionEnd = start;
        }
        
        this.activeInput.focus();
        this.activeInput.dispatchEvent(new Event('input', { bubbles: true }));
    }

    showKeyboard(input, type = 'qwertz') {
        this.activeInput = input;
        this.keyboardType = type;
        
        const keyboard = document.getElementById('touch-keyboard');
        keyboard.classList.remove('hidden');
        
        // Show appropriate layout
        if (type === 'numeric') {
            document.getElementById('qwertz-keyboard').classList.add('hidden');
            document.getElementById('numeric-keyboard').classList.remove('hidden');
            document.getElementById('numeric-preview').value = input.value || '';
        } else {
            document.getElementById('qwertz-keyboard').classList.remove('hidden');
            document.getElementById('numeric-keyboard').classList.add('hidden');
        }
        
        // Animate in
        setTimeout(() => keyboard.classList.add('visible'), 10);
    }

    hideKeyboard() {
        const keyboard = document.getElementById('touch-keyboard');
        keyboard.classList.remove('visible');
        
        setTimeout(() => {
            keyboard.classList.add('hidden');
            this.activeInput = null;
            this.isShiftActive = false;
            this.updateKeyLabels();
        }, 300);
    }

    attachEventListeners() {
        // Close button
        document.querySelector('.keyboard-close').addEventListener('click', () => {
            this.hideKeyboard();
        });
        
        // Auto-detect input types
        document.addEventListener('focusin', (e) => {
            const input = e.target;
            if (input.tagName === 'INPUT' || input.tagName === 'TEXTAREA') {
                // Check if it's an IP or port field
                const isNumeric = input.type === 'number' || 
                                input.placeholder?.includes('IP') || 
                                input.placeholder?.includes('Port') ||
                                input.name?.includes('port') ||
                                input.name?.includes('ip') ||
                                input.classList.contains('numeric-input');
                
                // Only show keyboard for touch devices or if explicitly requested
                if ('ontouchstart' in window || input.classList.contains('use-keyboard')) {
                    this.showKeyboard(input, isNumeric ? 'numeric' : 'qwertz');
                }
            }
        });
        
        // Hide on outside click
        document.addEventListener('click', (e) => {
            if (!e.target.closest('#touch-keyboard') && 
                !e.target.closest('input') && 
                !e.target.closest('textarea')) {
                this.hideKeyboard();
            }
        });
    }
}

// Initialize keyboard when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.touchKeyboard = new TouchKeyboard();
    });
} else {
    window.touchKeyboard = new TouchKeyboard();
}