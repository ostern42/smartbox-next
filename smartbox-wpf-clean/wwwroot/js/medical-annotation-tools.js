/**
 * Medical Annotation Tools for SmartBox Next
 * Touch-optimized annotation system for medical video and images
 */
class MedicalAnnotationTools {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.options = {
            touchTargetSize: 44,
            colors: ['#FF0000', '#00FF00', '#0000FF', '#FFFF00', '#FF00FF', '#00FFFF', '#000000', '#FFFFFF'],
            defaultColor: '#FF0000',
            defaultLineWidth: 3,
            fontSizes: [12, 16, 20, 24, 32],
            defaultFontSize: 20,
            enableVoiceNotes: true,
            measurementScale: 1, // pixels per mm
            ...options
        };
        
        this.canvas = null;
        this.ctx = null;
        this.isDrawing = false;
        this.currentTool = 'pen';
        this.currentColor = this.options.defaultColor;
        this.currentLineWidth = this.options.defaultLineWidth;
        this.currentFontSize = this.options.defaultFontSize;
        this.annotations = [];
        this.currentAnnotation = null;
        this.history = [];
        this.historyIndex = -1;
        
        // Voice recording
        this.mediaRecorder = null;
        this.audioChunks = [];
        
        this.init();
    }
    
    init() {
        this.createStructure();
        this.setupEventListeners();
        this.setupCanvas();
        console.log('MedicalAnnotationTools: Initialized');
    }
    
    createStructure() {
        this.container.className = 'medical-annotation-tools';
        this.container.innerHTML = `
            <div class="annotation-workspace">
                <!-- Canvas overlay -->
                <canvas id="annotationCanvas" class="annotation-canvas"></canvas>
                
                <!-- Tool palette -->
                <div class="tool-palette" id="toolPalette">
                    <div class="palette-header">
                        <h4>Annotations-Werkzeuge</h4>
                        <button class="palette-toggle" id="paletteToggle">
                            <i class="ms-Icon ms-Icon--ChevronLeft"></i>
                        </button>
                    </div>
                    
                    <!-- Drawing tools -->
                    <div class="tool-section">
                        <h5>Zeichnen</h5>
                        <div class="tool-grid">
                            <button class="tool-btn active" data-tool="pen" title="Stift">
                                <i class="ms-Icon ms-Icon--Edit"></i>
                            </button>
                            <button class="tool-btn" data-tool="highlighter" title="Textmarker">
                                <i class="ms-Icon ms-Icon--Highlight"></i>
                            </button>
                            <button class="tool-btn" data-tool="eraser" title="Radierer">
                                <i class="ms-Icon ms-Icon--EraseTool"></i>
                            </button>
                            <button class="tool-btn" data-tool="select" title="Auswählen">
                                <i class="ms-Icon ms-Icon--SelectAll"></i>
                            </button>
                        </div>
                    </div>
                    
                    <!-- Shape tools -->
                    <div class="tool-section">
                        <h5>Formen</h5>
                        <div class="tool-grid">
                            <button class="tool-btn" data-tool="arrow" title="Pfeil">
                                <i class="ms-Icon ms-Icon--ArrowUpRight"></i>
                            </button>
                            <button class="tool-btn" data-tool="circle" title="Kreis">
                                <i class="ms-Icon ms-Icon--CircleRing"></i>
                            </button>
                            <button class="tool-btn" data-tool="rectangle" title="Rechteck">
                                <i class="ms-Icon ms-Icon--RectangleShape"></i>
                            </button>
                            <button class="tool-btn" data-tool="line" title="Linie">
                                <i class="ms-Icon ms-Icon--Line"></i>
                            </button>
                        </div>
                    </div>
                    
                    <!-- Medical tools -->
                    <div class="tool-section">
                        <h5>Medizinisch</h5>
                        <div class="tool-grid">
                            <button class="tool-btn" data-tool="measurement" title="Messung">
                                <i class="ms-Icon ms-Icon--Ruler"></i>
                            </button>
                            <button class="tool-btn" data-tool="angle" title="Winkel">
                                <i class="ms-Icon ms-Icon--TriangleSolid"></i>
                            </button>
                            <button class="tool-btn" data-tool="roi" title="ROI">
                                <i class="ms-Icon ms-Icon--CropTool"></i>
                            </button>
                            <button class="tool-btn" data-tool="pointer" title="Zeiger">
                                <i class="ms-Icon ms-Icon--TouchPointer"></i>
                            </button>
                        </div>
                    </div>
                    
                    <!-- Text and voice -->
                    <div class="tool-section">
                        <h5>Text & Sprache</h5>
                        <div class="tool-grid">
                            <button class="tool-btn" data-tool="text" title="Text">
                                <i class="ms-Icon ms-Icon--FontSize"></i>
                            </button>
                            <button class="tool-btn" data-tool="voice" title="Sprachnotiz">
                                <i class="ms-Icon ms-Icon--Microphone"></i>
                            </button>
                            <button class="tool-btn" data-tool="stamp" title="Stempel">
                                <i class="ms-Icon ms-Icon--Stamp"></i>
                            </button>
                        </div>
                    </div>
                    
                    <!-- Color picker -->
                    <div class="tool-section">
                        <h5>Farbe</h5>
                        <div class="color-grid" id="colorGrid">
                            ${this.options.colors.map(color => 
                                `<button class="color-btn ${color === this.currentColor ? 'active' : ''}" 
                                         data-color="${color}" 
                                         style="background-color: ${color}">
                                </button>`
                            ).join('')}
                        </div>
                    </div>
                    
                    <!-- Line width -->
                    <div class="tool-section">
                        <h5>Strichstärke</h5>
                        <div class="line-width-control">
                            <input type="range" id="lineWidth" 
                                   min="1" max="10" 
                                   value="${this.currentLineWidth}" 
                                   class="width-slider">
                            <span id="lineWidthValue">${this.currentLineWidth}px</span>
                        </div>
                    </div>
                    
                    <!-- Actions -->
                    <div class="tool-section">
                        <h5>Aktionen</h5>
                        <div class="action-buttons">
                            <button class="action-btn" id="undoBtn" title="Rückgängig">
                                <i class="ms-Icon ms-Icon--Undo"></i>
                            </button>
                            <button class="action-btn" id="redoBtn" title="Wiederholen">
                                <i class="ms-Icon ms-Icon--Redo"></i>
                            </button>
                            <button class="action-btn" id="clearBtn" title="Löschen">
                                <i class="ms-Icon ms-Icon--Clear"></i>
                            </button>
                            <button class="action-btn" id="saveBtn" title="Speichern">
                                <i class="ms-Icon ms-Icon--Save"></i>
                            </button>
                        </div>
                    </div>
                </div>
                
                <!-- Text input overlay -->
                <div class="text-input-overlay hidden" id="textInputOverlay">
                    <textarea id="textInput" placeholder="Text eingeben..." rows="3"></textarea>
                    <div class="text-input-controls">
                        <select id="fontSize" class="font-size-select">
                            ${this.options.fontSizes.map(size => 
                                `<option value="${size}" ${size === this.currentFontSize ? 'selected' : ''}>${size}px</option>`
                            ).join('')}
                        </select>
                        <button class="text-btn cancel" id="cancelTextBtn">Abbrechen</button>
                        <button class="text-btn confirm" id="confirmTextBtn">OK</button>
                    </div>
                </div>
                
                <!-- Voice recording overlay -->
                <div class="voice-recording-overlay hidden" id="voiceRecordingOverlay">
                    <div class="voice-recording-content">
                        <div class="recording-indicator">
                            <div class="rec-dot"></div>
                            <span>Aufnahme läuft...</span>
                        </div>
                        <div class="recording-time" id="recordingTime">00:00</div>
                        <button class="voice-btn stop" id="stopVoiceBtn">
                            <i class="ms-Icon ms-Icon--Stop"></i>
                            <span>Stopp</span>
                        </button>
                    </div>
                </div>
                
                <!-- Measurement display -->
                <div class="measurement-display hidden" id="measurementDisplay">
                    <span class="measurement-value" id="measurementValue">0.0 mm</span>
                </div>
                
                <!-- Medical stamps -->
                <div class="stamp-menu hidden" id="stampMenu">
                    <button class="stamp-option" data-stamp="normal">Normal</button>
                    <button class="stamp-option" data-stamp="abnormal">Abnormal</button>
                    <button class="stamp-option" data-stamp="critical">Kritisch</button>
                    <button class="stamp-option" data-stamp="reviewed">Geprüft</button>
                    <button class="stamp-option" data-stamp="followup">Nachuntersuchung</button>
                </div>
            </div>
        `;
        
        // Get references
        this.canvas = this.container.querySelector('#annotationCanvas');
        this.ctx = this.canvas.getContext('2d');
    }
    
    setupCanvas() {
        // Set canvas size
        const container = this.canvas.parentElement;
        this.canvas.width = container.offsetWidth;
        this.canvas.height = container.offsetHeight;
        
        // Set default styles
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
        
        // Handle resize
        window.addEventListener('resize', () => {
            const imageData = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
            this.canvas.width = container.offsetWidth;
            this.canvas.height = container.offsetHeight;
            this.ctx.putImageData(imageData, 0, 0);
        });
    }
    
    setupEventListeners() {
        // Tool selection
        this.container.querySelectorAll('[data-tool]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const tool = e.currentTarget.dataset.tool;
                this.selectTool(tool);
            });
        });
        
        // Color selection
        this.container.querySelectorAll('[data-color]').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const color = e.currentTarget.dataset.color;
                this.selectColor(color);
            });
        });
        
        // Line width
        const lineWidthSlider = this.container.querySelector('#lineWidth');
        lineWidthSlider.addEventListener('input', (e) => {
            this.currentLineWidth = parseInt(e.target.value);
            this.container.querySelector('#lineWidthValue').textContent = `${this.currentLineWidth}px`;
        });
        
        // Actions
        this.container.querySelector('#undoBtn').addEventListener('click', () => this.undo());
        this.container.querySelector('#redoBtn').addEventListener('click', () => this.redo());
        this.container.querySelector('#clearBtn').addEventListener('click', () => this.clear());
        this.container.querySelector('#saveBtn').addEventListener('click', () => this.save());
        
        // Canvas drawing events
        this.setupDrawingEvents();
        
        // Text input
        this.setupTextInput();
        
        // Voice recording
        this.setupVoiceRecording();
        
        // Stamps
        this.setupStamps();
        
        // Palette toggle
        this.container.querySelector('#paletteToggle').addEventListener('click', () => {
            this.container.querySelector('#toolPalette').classList.toggle('collapsed');
        });
    }
    
    setupDrawingEvents() {
        let isDrawing = false;
        let startX, startY;
        
        // Helper to get coordinates
        const getCoordinates = (e) => {
            const rect = this.canvas.getBoundingClientRect();
            const x = (e.clientX || e.touches[0].clientX) - rect.left;
            const y = (e.clientY || e.touches[0].clientY) - rect.top;
            return { x, y };
        };
        
        // Start drawing
        const startDrawing = (e) => {
            e.preventDefault();
            const { x, y } = getCoordinates(e);
            isDrawing = true;
            startX = x;
            startY = y;
            
            this.startAnnotation(x, y);
        };
        
        // Continue drawing
        const draw = (e) => {
            if (!isDrawing) return;
            e.preventDefault();
            
            const { x, y } = getCoordinates(e);
            this.continueAnnotation(x, y);
        };
        
        // End drawing
        const endDrawing = (e) => {
            if (!isDrawing) return;
            e.preventDefault();
            
            isDrawing = false;
            const { x, y } = getCoordinates(e);
            this.endAnnotation(x, y);
        };
        
        // Mouse events
        this.canvas.addEventListener('mousedown', startDrawing);
        this.canvas.addEventListener('mousemove', draw);
        this.canvas.addEventListener('mouseup', endDrawing);
        this.canvas.addEventListener('mouseout', endDrawing);
        
        // Touch events
        this.canvas.addEventListener('touchstart', startDrawing);
        this.canvas.addEventListener('touchmove', draw);
        this.canvas.addEventListener('touchend', endDrawing);
        this.canvas.addEventListener('touchcancel', endDrawing);
    }
    
    setupTextInput() {
        const overlay = this.container.querySelector('#textInputOverlay');
        const input = this.container.querySelector('#textInput');
        const fontSelect = this.container.querySelector('#fontSize');
        
        this.container.querySelector('#confirmTextBtn').addEventListener('click', () => {
            const text = input.value.trim();
            if (text) {
                this.addTextAnnotation(text, this.textPosition.x, this.textPosition.y);
                input.value = '';
                overlay.classList.add('hidden');
            }
        });
        
        this.container.querySelector('#cancelTextBtn').addEventListener('click', () => {
            input.value = '';
            overlay.classList.add('hidden');
        });
        
        fontSelect.addEventListener('change', (e) => {
            this.currentFontSize = parseInt(e.target.value);
        });
    }
    
    setupVoiceRecording() {
        if (!this.options.enableVoiceNotes || !navigator.mediaDevices) return;
        
        const overlay = this.container.querySelector('#voiceRecordingOverlay');
        const stopBtn = this.container.querySelector('#stopVoiceBtn');
        const timeDisplay = this.container.querySelector('#recordingTime');
        
        let recordingStartTime;
        let recordingTimer;
        
        stopBtn.addEventListener('click', () => {
            this.stopVoiceRecording();
            overlay.classList.add('hidden');
            
            if (recordingTimer) {
                clearInterval(recordingTimer);
            }
        });
    }
    
    setupStamps() {
        const stampMenu = this.container.querySelector('#stampMenu');
        
        stampMenu.addEventListener('click', (e) => {
            const stampOption = e.target.closest('.stamp-option');
            if (stampOption) {
                const stampType = stampOption.dataset.stamp;
                this.addStamp(stampType, this.stampPosition.x, this.stampPosition.y);
                stampMenu.classList.add('hidden');
            }
        });
        
        // Close stamp menu on outside click
        document.addEventListener('click', (e) => {
            if (!stampMenu.contains(e.target)) {
                stampMenu.classList.add('hidden');
            }
        });
    }
    
    // Tool selection
    
    selectTool(tool) {
        this.currentTool = tool;
        
        // Update UI
        this.container.querySelectorAll('[data-tool]').forEach(btn => {
            btn.classList.remove('active');
        });
        this.container.querySelector(`[data-tool="${tool}"]`).classList.add('active');
        
        // Set cursor
        this.updateCursor();
        
        console.log(`MedicalAnnotationTools: Selected tool ${tool}`);
    }
    
    selectColor(color) {
        this.currentColor = color;
        
        // Update UI
        this.container.querySelectorAll('[data-color]').forEach(btn => {
            btn.classList.remove('active');
        });
        this.container.querySelector(`[data-color="${color}"]`).classList.add('active');
    }
    
    updateCursor() {
        switch (this.currentTool) {
            case 'pen':
            case 'highlighter':
                this.canvas.style.cursor = 'crosshair';
                break;
            case 'eraser':
                this.canvas.style.cursor = 'grab';
                break;
            case 'text':
                this.canvas.style.cursor = 'text';
                break;
            case 'measurement':
            case 'angle':
                this.canvas.style.cursor = 'crosshair';
                break;
            default:
                this.canvas.style.cursor = 'default';
        }
    }
    
    // Annotation handling
    
    startAnnotation(x, y) {
        this.currentAnnotation = {
            id: this.generateId(),
            tool: this.currentTool,
            color: this.currentColor,
            lineWidth: this.currentLineWidth,
            points: [{ x, y }],
            timestamp: Date.now()
        };
        
        switch (this.currentTool) {
            case 'pen':
            case 'highlighter':
            case 'eraser':
                this.ctx.beginPath();
                this.ctx.moveTo(x, y);
                break;
                
            case 'text':
                this.showTextInput(x, y);
                break;
                
            case 'voice':
                this.startVoiceRecording(x, y);
                break;
                
            case 'stamp':
                this.showStampMenu(x, y);
                break;
                
            case 'measurement':
                this.startMeasurement(x, y);
                break;
        }
    }
    
    continueAnnotation(x, y) {
        if (!this.currentAnnotation) return;
        
        this.currentAnnotation.points.push({ x, y });
        
        switch (this.currentTool) {
            case 'pen':
                this.drawPen(x, y);
                break;
                
            case 'highlighter':
                this.drawHighlighter(x, y);
                break;
                
            case 'eraser':
                this.erase(x, y);
                break;
                
            case 'arrow':
            case 'line':
            case 'circle':
            case 'rectangle':
                this.drawShape(x, y);
                break;
                
            case 'measurement':
                this.updateMeasurement(x, y);
                break;
        }
    }
    
    endAnnotation(x, y) {
        if (!this.currentAnnotation) return;
        
        switch (this.currentTool) {
            case 'arrow':
                this.finalizeArrow(x, y);
                break;
                
            case 'circle':
            case 'rectangle':
                this.finalizeShape(x, y);
                break;
                
            case 'measurement':
                this.finalizeMeasurement(x, y);
                break;
        }
        
        // Add to annotations array
        if (this.currentAnnotation.tool !== 'eraser') {
            this.annotations.push(this.currentAnnotation);
            this.addToHistory();
        }
        
        this.currentAnnotation = null;
    }
    
    // Drawing methods
    
    drawPen(x, y) {
        this.ctx.globalCompositeOperation = 'source-over';
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentLineWidth;
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
    }
    
    drawHighlighter(x, y) {
        this.ctx.globalCompositeOperation = 'multiply';
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentLineWidth * 3;
        this.ctx.globalAlpha = 0.3;
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
        this.ctx.globalAlpha = 1.0;
    }
    
    erase(x, y) {
        this.ctx.globalCompositeOperation = 'destination-out';
        this.ctx.lineWidth = this.currentLineWidth * 3;
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
    }
    
    drawShape(x, y) {
        // Clear canvas and redraw all annotations
        this.redrawCanvas();
        
        const startPoint = this.currentAnnotation.points[0];
        
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = this.currentLineWidth;
        
        switch (this.currentTool) {
            case 'arrow':
                this.drawArrow(startPoint.x, startPoint.y, x, y);
                break;
                
            case 'line':
                this.ctx.beginPath();
                this.ctx.moveTo(startPoint.x, startPoint.y);
                this.ctx.lineTo(x, y);
                this.ctx.stroke();
                break;
                
            case 'circle':
                const radius = Math.sqrt(
                    Math.pow(x - startPoint.x, 2) + Math.pow(y - startPoint.y, 2)
                );
                this.ctx.beginPath();
                this.ctx.arc(startPoint.x, startPoint.y, radius, 0, Math.PI * 2);
                this.ctx.stroke();
                break;
                
            case 'rectangle':
                this.ctx.beginPath();
                this.ctx.rect(
                    startPoint.x, 
                    startPoint.y, 
                    x - startPoint.x, 
                    y - startPoint.y
                );
                this.ctx.stroke();
                break;
        }
    }
    
    drawArrow(x1, y1, x2, y2) {
        const headlen = 15;
        const dx = x2 - x1;
        const dy = y2 - y1;
        const angle = Math.atan2(dy, dx);
        
        // Draw line
        this.ctx.beginPath();
        this.ctx.moveTo(x1, y1);
        this.ctx.lineTo(x2, y2);
        this.ctx.stroke();
        
        // Draw arrowhead
        this.ctx.beginPath();
        this.ctx.moveTo(x2, y2);
        this.ctx.lineTo(
            x2 - headlen * Math.cos(angle - Math.PI / 6),
            y2 - headlen * Math.sin(angle - Math.PI / 6)
        );
        this.ctx.moveTo(x2, y2);
        this.ctx.lineTo(
            x2 - headlen * Math.cos(angle + Math.PI / 6),
            y2 - headlen * Math.sin(angle + Math.PI / 6)
        );
        this.ctx.stroke();
    }
    
    finalizeArrow(x, y) {
        const startPoint = this.currentAnnotation.points[0];
        this.currentAnnotation.endPoint = { x, y };
        this.currentAnnotation.startPoint = startPoint;
    }
    
    finalizeShape(x, y) {
        const startPoint = this.currentAnnotation.points[0];
        this.currentAnnotation.startPoint = startPoint;
        this.currentAnnotation.endPoint = { x, y };
        
        if (this.currentTool === 'circle') {
            this.currentAnnotation.radius = Math.sqrt(
                Math.pow(x - startPoint.x, 2) + Math.pow(y - startPoint.y, 2)
            );
        }
    }
    
    // Text annotations
    
    showTextInput(x, y) {
        this.textPosition = { x, y };
        const overlay = this.container.querySelector('#textInputOverlay');
        overlay.classList.remove('hidden');
        
        // Position near click point
        overlay.style.left = x + 'px';
        overlay.style.top = y + 'px';
        
        // Focus input
        this.container.querySelector('#textInput').focus();
    }
    
    addTextAnnotation(text, x, y) {
        this.ctx.font = `${this.currentFontSize}px Open Sans`;
        this.ctx.fillStyle = this.currentColor;
        this.ctx.strokeStyle = '#000000';
        this.ctx.lineWidth = 2;
        
        // Draw text with outline for visibility
        this.ctx.strokeText(text, x, y);
        this.ctx.fillText(text, x, y);
        
        // Store annotation
        const annotation = {
            id: this.generateId(),
            tool: 'text',
            text: text,
            x: x,
            y: y,
            color: this.currentColor,
            fontSize: this.currentFontSize,
            timestamp: Date.now()
        };
        
        this.annotations.push(annotation);
        this.addToHistory();
    }
    
    // Voice recording
    
    async startVoiceRecording(x, y) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            this.mediaRecorder = new MediaRecorder(stream);
            this.audioChunks = [];
            
            this.mediaRecorder.ondataavailable = (e) => {
                this.audioChunks.push(e.data);
            };
            
            this.mediaRecorder.onstop = () => {
                const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
                this.addVoiceAnnotation(audioBlob, x, y);
                
                // Stop all tracks
                stream.getTracks().forEach(track => track.stop());
            };
            
            this.mediaRecorder.start();
            
            // Show recording UI
            const overlay = this.container.querySelector('#voiceRecordingOverlay');
            overlay.classList.remove('hidden');
            
            // Start timer
            const startTime = Date.now();
            const timeDisplay = this.container.querySelector('#recordingTime');
            
            this.recordingTimer = setInterval(() => {
                const elapsed = Math.floor((Date.now() - startTime) / 1000);
                const mins = Math.floor(elapsed / 60);
                const secs = elapsed % 60;
                timeDisplay.textContent = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
            }, 100);
            
        } catch (error) {
            console.error('MedicalAnnotationTools: Voice recording failed', error);
            alert('Mikrofon-Zugriff verweigert');
        }
    }
    
    stopVoiceRecording() {
        if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
            this.mediaRecorder.stop();
        }
        
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }
    
    addVoiceAnnotation(audioBlob, x, y) {
        // Create visual indicator
        this.ctx.fillStyle = '#FF0000';
        this.ctx.beginPath();
        this.ctx.arc(x, y, 15, 0, Math.PI * 2);
        this.ctx.fill();
        
        // Draw microphone icon
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.font = '16px Segoe Fluent Icons';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText('\uE720', x, y); // Microphone icon
        
        // Store annotation
        const annotation = {
            id: this.generateId(),
            tool: 'voice',
            audioBlob: audioBlob,
            x: x,
            y: y,
            timestamp: Date.now()
        };
        
        this.annotations.push(annotation);
        this.addToHistory();
        
        this.emit('voiceAnnotationAdded', { annotation });
    }
    
    // Stamps
    
    showStampMenu(x, y) {
        this.stampPosition = { x, y };
        const menu = this.container.querySelector('#stampMenu');
        menu.classList.remove('hidden');
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';
    }
    
    addStamp(stampType, x, y) {
        const stamps = {
            normal: { text: 'NORMAL', color: '#00FF00' },
            abnormal: { text: 'ABNORMAL', color: '#FF0000' },
            critical: { text: 'KRITISCH', color: '#FF0000' },
            reviewed: { text: 'GEPRÜFT', color: '#0000FF' },
            followup: { text: 'NACHUNTERSUCHUNG', color: '#FF8C00' }
        };
        
        const stamp = stamps[stampType];
        
        // Draw stamp
        this.ctx.fillStyle = stamp.color;
        this.ctx.strokeStyle = stamp.color;
        this.ctx.lineWidth = 2;
        
        // Draw border
        this.ctx.beginPath();
        this.ctx.rect(x - 60, y - 20, 120, 40);
        this.ctx.stroke();
        
        // Draw text
        this.ctx.font = 'bold 14px Open Sans';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText(stamp.text, x, y);
        
        // Add timestamp
        this.ctx.font = '10px Open Sans';
        this.ctx.fillText(new Date().toLocaleDateString('de-DE'), x, y + 12);
        
        // Store annotation
        const annotation = {
            id: this.generateId(),
            tool: 'stamp',
            stampType: stampType,
            x: x,
            y: y,
            timestamp: Date.now()
        };
        
        this.annotations.push(annotation);
        this.addToHistory();
    }
    
    // Measurement tools
    
    startMeasurement(x, y) {
        this.measurementStart = { x, y };
        this.showMeasurementDisplay();
    }
    
    updateMeasurement(x, y) {
        this.redrawCanvas();
        
        // Draw measurement line
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = 2;
        this.ctx.setLineDash([5, 5]);
        
        this.ctx.beginPath();
        this.ctx.moveTo(this.measurementStart.x, this.measurementStart.y);
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
        
        this.ctx.setLineDash([]);
        
        // Calculate distance
        const distance = Math.sqrt(
            Math.pow(x - this.measurementStart.x, 2) + 
            Math.pow(y - this.measurementStart.y, 2)
        );
        
        // Convert to mm using scale
        const distanceMm = distance / this.options.measurementScale;
        
        // Update display
        this.updateMeasurementDisplay(distanceMm);
    }
    
    finalizeMeasurement(x, y) {
        const distance = Math.sqrt(
            Math.pow(x - this.measurementStart.x, 2) + 
            Math.pow(y - this.measurementStart.y, 2)
        );
        
        const distanceMm = distance / this.options.measurementScale;
        
        // Draw final measurement with label
        this.redrawCanvas();
        
        this.ctx.strokeStyle = this.currentColor;
        this.ctx.lineWidth = 2;
        
        // Draw line
        this.ctx.beginPath();
        this.ctx.moveTo(this.measurementStart.x, this.measurementStart.y);
        this.ctx.lineTo(x, y);
        this.ctx.stroke();
        
        // Draw endpoints
        this.ctx.fillStyle = this.currentColor;
        this.ctx.beginPath();
        this.ctx.arc(this.measurementStart.x, this.measurementStart.y, 3, 0, Math.PI * 2);
        this.ctx.fill();
        this.ctx.beginPath();
        this.ctx.arc(x, y, 3, 0, Math.PI * 2);
        this.ctx.fill();
        
        // Draw label
        const midX = (this.measurementStart.x + x) / 2;
        const midY = (this.measurementStart.y + y) / 2;
        
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(midX - 30, midY - 10, 60, 20);
        
        this.ctx.fillStyle = this.currentColor;
        this.ctx.font = '14px Open Sans';
        this.ctx.textAlign = 'center';
        this.ctx.textBaseline = 'middle';
        this.ctx.fillText(`${distanceMm.toFixed(1)} mm`, midX, midY);
        
        // Hide display
        this.hideMeasurementDisplay();
        
        // Store annotation
        this.currentAnnotation.measurement = distanceMm;
    }
    
    showMeasurementDisplay() {
        const display = this.container.querySelector('#measurementDisplay');
        display.classList.remove('hidden');
    }
    
    updateMeasurementDisplay(value) {
        const valueEl = this.container.querySelector('#measurementValue');
        valueEl.textContent = `${value.toFixed(1)} mm`;
    }
    
    hideMeasurementDisplay() {
        const display = this.container.querySelector('#measurementDisplay');
        display.classList.add('hidden');
    }
    
    // History management
    
    addToHistory() {
        // Clone current canvas state
        const imageData = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
        
        // Remove any states after current index
        this.history = this.history.slice(0, this.historyIndex + 1);
        
        // Add new state
        this.history.push({
            imageData: imageData,
            annotations: [...this.annotations]
        });
        
        this.historyIndex++;
        
        // Limit history size
        if (this.history.length > 50) {
            this.history.shift();
            this.historyIndex--;
        }
        
        this.updateHistoryButtons();
    }
    
    undo() {
        if (this.historyIndex > 0) {
            this.historyIndex--;
            const state = this.history[this.historyIndex];
            
            this.ctx.putImageData(state.imageData, 0, 0);
            this.annotations = [...state.annotations];
            
            this.updateHistoryButtons();
        }
    }
    
    redo() {
        if (this.historyIndex < this.history.length - 1) {
            this.historyIndex++;
            const state = this.history[this.historyIndex];
            
            this.ctx.putImageData(state.imageData, 0, 0);
            this.annotations = [...state.annotations];
            
            this.updateHistoryButtons();
        }
    }
    
    updateHistoryButtons() {
        const undoBtn = this.container.querySelector('#undoBtn');
        const redoBtn = this.container.querySelector('#redoBtn');
        
        undoBtn.disabled = this.historyIndex <= 0;
        redoBtn.disabled = this.historyIndex >= this.history.length - 1;
    }
    
    // Canvas operations
    
    clear() {
        if (!confirm('Alle Annotationen löschen?')) return;
        
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        this.annotations = [];
        this.addToHistory();
    }
    
    redrawCanvas() {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Redraw all annotations
        this.annotations.forEach(annotation => {
            this.redrawAnnotation(annotation);
        });
    }
    
    redrawAnnotation(annotation) {
        switch (annotation.tool) {
            case 'pen':
            case 'highlighter':
                this.ctx.strokeStyle = annotation.color;
                this.ctx.lineWidth = annotation.lineWidth;
                
                if (annotation.tool === 'highlighter') {
                    this.ctx.globalCompositeOperation = 'multiply';
                    this.ctx.globalAlpha = 0.3;
                    this.ctx.lineWidth = annotation.lineWidth * 3;
                }
                
                this.ctx.beginPath();
                annotation.points.forEach((point, index) => {
                    if (index === 0) {
                        this.ctx.moveTo(point.x, point.y);
                    } else {
                        this.ctx.lineTo(point.x, point.y);
                    }
                });
                this.ctx.stroke();
                
                this.ctx.globalCompositeOperation = 'source-over';
                this.ctx.globalAlpha = 1.0;
                break;
                
            case 'text':
                this.ctx.font = `${annotation.fontSize}px Open Sans`;
                this.ctx.fillStyle = annotation.color;
                this.ctx.strokeStyle = '#000000';
                this.ctx.lineWidth = 2;
                this.ctx.strokeText(annotation.text, annotation.x, annotation.y);
                this.ctx.fillText(annotation.text, annotation.x, annotation.y);
                break;
                
            // Add other annotation types as needed
        }
    }
    
    // Export and save
    
    save() {
        const data = {
            annotations: this.annotations,
            imageData: this.canvas.toDataURL('image/png'),
            timestamp: Date.now()
        };
        
        this.emit('save', data);
        console.log('MedicalAnnotationTools: Annotations saved');
    }
    
    exportImage(includeBackground = true) {
        if (includeBackground) {
            return this.canvas.toDataURL('image/png');
        } else {
            // Export only annotations
            const tempCanvas = document.createElement('canvas');
            tempCanvas.width = this.canvas.width;
            tempCanvas.height = this.canvas.height;
            const tempCtx = tempCanvas.getContext('2d');
            
            tempCtx.drawImage(this.canvas, 0, 0);
            return tempCanvas.toDataURL('image/png');
        }
    }
    
    loadAnnotations(annotations) {
        this.annotations = annotations;
        this.redrawCanvas();
    }
    
    // Utility methods
    
    generateId() {
        return `ann_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    setBackgroundImage(imageUrl) {
        const img = new Image();
        img.onload = () => {
            this.backgroundImage = img;
            this.redrawCanvas();
        };
        img.src = imageUrl;
    }
    
    resize(width, height) {
        const imageData = this.ctx.getImageData(0, 0, this.canvas.width, this.canvas.height);
        this.canvas.width = width;
        this.canvas.height = height;
        this.ctx.putImageData(imageData, 0, 0);
    }
    
    // Event system
    
    emit(eventType, data = {}) {
        const event = new CustomEvent(`annotation:${eventType}`, {
            detail: data
        });
        this.container.dispatchEvent(event);
    }
    
    on(eventType, callback) {
        this.container.addEventListener(`annotation:${eventType}`, callback);
    }
    
    off(eventType, callback) {
        this.container.removeEventListener(`annotation:${eventType}`, callback);
    }
}

// Export for use
window.MedicalAnnotationTools = MedicalAnnotationTools;