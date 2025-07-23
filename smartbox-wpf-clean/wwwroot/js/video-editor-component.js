/**
 * Video Editor Component for SmartBox Next
 * Medical-grade video editing with touch-optimized interface
 */
class MedicalVideoEditor {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.options = {
            maxSegments: 20,
            minSegmentDuration: 0.5, // seconds
            frameRate: 30,
            enableAutoSave: true,
            autoSaveInterval: 30000, // 30 seconds
            touchTargetSize: 44,
            ...options
        };
        
        this.videoPlayer = null;
        this.timeline = null;
        this.videoBlob = null;
        this.segments = [];
        this.selectedSegment = null;
        this.editHistory = [];
        this.historyIndex = -1;
        this.isModified = false;
        this.patientInfo = null;
        
        this.init();
    }
    
    init() {
        this.createStructure();
        this.setupEventListeners();
        this.setupAutoSave();
        console.log('MedicalVideoEditor: Initialized');
    }
    
    createStructure() {
        this.container.className = 'medical-video-editor';
        this.container.innerHTML = `
            <div class="editor-layout">
                <!-- Video preview area -->
                <div class="editor-preview">
                    <div id="videoPlayerContainer"></div>
                    
                    <!-- Quick action buttons overlay -->
                    <div class="quick-actions">
                        <button class="quick-action-btn" id="markInBtn" title="Markiere Start">
                            <i class="ms-Icon ms-Icon--ChevronLeftEnd6"></i>
                            <span>Start</span>
                        </button>
                        <button class="quick-action-btn" id="markOutBtn" title="Markiere Ende">
                            <i class="ms-Icon ms-Icon--ChevronRightEnd6"></i>
                            <span>Ende</span>
                        </button>
                        <button class="quick-action-btn critical" id="markCriticalBtn" title="Kritischer Moment">
                            <i class="ms-Icon ms-Icon--Flag"></i>
                            <span>Kritisch</span>
                        </button>
                    </div>
                </div>
                
                <!-- Enhanced timeline with segments -->
                <div class="editor-timeline">
                    <div id="enhancedTimeline"></div>
                    
                    <!-- Segment list -->
                    <div class="segment-list" id="segmentList">
                        <div class="segment-header">
                            <h3>Video Segmente</h3>
                            <span class="segment-count" id="segmentCount">0 Segmente</span>
                        </div>
                        <div class="segment-items" id="segmentItems">
                            <!-- Segments will be added here dynamically -->
                        </div>
                    </div>
                </div>
                
                <!-- Editing tools panel -->
                <div class="editor-tools">
                    <div class="tool-section">
                        <h4>Schnitt-Werkzeuge</h4>
                        <div class="tool-buttons">
                            <button class="tool-btn" id="trimBtn" title="Trimmen">
                                <i class="ms-Icon ms-Icon--Trim"></i>
                                <span>Trimmen</span>
                            </button>
                            <button class="tool-btn" id="splitBtn" title="Teilen">
                                <i class="ms-Icon ms-Icon--Cut"></i>
                                <span>Teilen</span>
                            </button>
                            <button class="tool-btn" id="deleteSegmentBtn" title="Segment löschen">
                                <i class="ms-Icon ms-Icon--Delete"></i>
                                <span>Löschen</span>
                            </button>
                            <button class="tool-btn" id="joinSegmentsBtn" title="Segmente verbinden">
                                <i class="ms-Icon ms-Icon--Merge"></i>
                                <span>Verbinden</span>
                            </button>
                        </div>
                    </div>
                    
                    <div class="tool-section">
                        <h4>Bearbeitung</h4>
                        <div class="tool-buttons">
                            <button class="tool-btn" id="undoBtn" title="Rückgängig">
                                <i class="ms-Icon ms-Icon--Undo"></i>
                                <span>Rückgängig</span>
                            </button>
                            <button class="tool-btn" id="redoBtn" title="Wiederholen">
                                <i class="ms-Icon ms-Icon--Redo"></i>
                                <span>Wiederholen</span>
                            </button>
                            <button class="tool-btn" id="resetBtn" title="Zurücksetzen">
                                <i class="ms-Icon ms-Icon--Refresh"></i>
                                <span>Reset</span>
                            </button>
                        </div>
                    </div>
                    
                    <div class="tool-section">
                        <h4>Export</h4>
                        <div class="export-options">
                            <button class="export-btn primary" id="exportDicomBtn">
                                <i class="ms-Icon ms-Icon--Health"></i>
                                <span>DICOM Export</span>
                            </button>
                            <button class="export-btn" id="exportSegmentsBtn">
                                <i class="ms-Icon ms-Icon--Export"></i>
                                <span>Segmente exportieren</span>
                            </button>
                            <button class="export-btn" id="saveSessionBtn">
                                <i class="ms-Icon ms-Icon--Save"></i>
                                <span>Sitzung speichern</span>
                            </button>
                        </div>
                    </div>
                </div>
                
                <!-- Trim mode overlay -->
                <div class="trim-overlay hidden" id="trimOverlay">
                    <div class="trim-controls">
                        <div class="trim-handle start" id="trimStart">
                            <div class="handle-grip"></div>
                            <span class="handle-time">00:00</span>
                        </div>
                        <div class="trim-selection" id="trimSelection"></div>
                        <div class="trim-handle end" id="trimEnd">
                            <div class="handle-grip"></div>
                            <span class="handle-time">00:00</span>
                        </div>
                    </div>
                    <div class="trim-actions">
                        <button class="trim-btn cancel" id="cancelTrimBtn">
                            <i class="ms-Icon ms-Icon--Cancel"></i>
                            <span>Abbrechen</span>
                        </button>
                        <button class="trim-btn confirm" id="confirmTrimBtn">
                            <i class="ms-Icon ms-Icon--CheckMark"></i>
                            <span>Trimmen</span>
                        </button>
                    </div>
                </div>
                
                <!-- Auto-save indicator -->
                <div class="autosave-indicator" id="autosaveIndicator">
                    <i class="ms-Icon ms-Icon--CloudUpload"></i>
                    <span>Auto-gespeichert</span>
                </div>
            </div>
        `;
        
        // Initialize video player
        this.videoPlayer = new MedicalVideoPlayer('videoPlayerContainer', {
            enableAnnotations: true,
            enableFrameStep: true
        });
        
        // Initialize enhanced timeline
        this.timeline = new VideoTimelineComponent('enhancedTimeline', {
            height: 150,
            enableSegmentMode: true
        });
    }
    
    setupEventListeners() {
        // Tool buttons
        this.container.querySelector('#trimBtn').addEventListener('click', () => this.enterTrimMode());
        this.container.querySelector('#splitBtn').addEventListener('click', () => this.splitAtPlayhead());
        this.container.querySelector('#deleteSegmentBtn').addEventListener('click', () => this.deleteSelectedSegment());
        this.container.querySelector('#joinSegmentsBtn').addEventListener('click', () => this.joinSelectedSegments());
        
        // Edit actions
        this.container.querySelector('#undoBtn').addEventListener('click', () => this.undo());
        this.container.querySelector('#redoBtn').addEventListener('click', () => this.redo());
        this.container.querySelector('#resetBtn').addEventListener('click', () => this.reset());
        
        // Quick actions
        this.container.querySelector('#markInBtn').addEventListener('click', () => this.markIn());
        this.container.querySelector('#markOutBtn').addEventListener('click', () => this.markOut());
        this.container.querySelector('#markCriticalBtn').addEventListener('click', () => this.markCritical());
        
        // Export actions
        this.container.querySelector('#exportDicomBtn').addEventListener('click', () => this.exportToDicom());
        this.container.querySelector('#exportSegmentsBtn').addEventListener('click', () => this.exportSegments());
        this.container.querySelector('#saveSessionBtn').addEventListener('click', () => this.saveSession());
        
        // Trim mode controls
        this.setupTrimControls();
        
        // Video player events
        this.videoPlayer.on('timeupdate', (e) => this.onVideoTimeUpdate(e));
        this.videoPlayer.on('seek', (e) => this.onVideoSeek(e));
        
        // Timeline events
        this.timeline.on('seek', (e) => this.onTimelineSeek(e));
        this.timeline.on('segmentSelected', (e) => this.onSegmentSelected(e));
        
        // Initialize Jogwheel
        this.setupJogwheel();
        
        // Initialize Adaptive Timeline
        this.setupAdaptiveTimeline();
        
        // Keyboard shortcuts
        this.setupKeyboardShortcuts();
    }
    
    setupTrimControls() {
        const trimOverlay = this.container.querySelector('#trimOverlay');
        const trimStart = this.container.querySelector('#trimStart');
        const trimEnd = this.container.querySelector('#trimEnd');
        const trimSelection = this.container.querySelector('#trimSelection');
        
        let activeTrimHandle = null;
        let trimStartTime = 0;
        let trimEndTime = 0;
        
        const updateTrimUI = () => {
            const duration = this.videoPlayer.getDuration();
            const startPercent = (trimStartTime / duration) * 100;
            const endPercent = (trimEndTime / duration) * 100;
            
            trimStart.style.left = startPercent + '%';
            trimEnd.style.left = endPercent + '%';
            trimSelection.style.left = startPercent + '%';
            trimSelection.style.width = (endPercent - startPercent) + '%';
            
            // Update time labels
            trimStart.querySelector('.handle-time').textContent = this.formatTime(trimStartTime);
            trimEnd.querySelector('.handle-time').textContent = this.formatTime(trimEndTime);
        };
        
        const handleTrimDrag = (e) => {
            if (!activeTrimHandle) return;
            
            const rect = trimOverlay.getBoundingClientRect();
            const x = (e.clientX || e.touches[0].clientX) - rect.left;
            const percent = Math.max(0, Math.min(1, x / rect.width));
            const time = percent * this.videoPlayer.getDuration();
            
            if (activeTrimHandle === trimStart) {
                trimStartTime = Math.min(time, trimEndTime - this.options.minSegmentDuration);
            } else if (activeTrimHandle === trimEnd) {
                trimEndTime = Math.max(time, trimStartTime + this.options.minSegmentDuration);
            }
            
            updateTrimUI();
            this.videoPlayer.seek(time);
        };
        
        // Mouse events
        [trimStart, trimEnd].forEach(handle => {
            handle.addEventListener('mousedown', (e) => {
                activeTrimHandle = handle;
                document.addEventListener('mousemove', handleTrimDrag);
                document.addEventListener('mouseup', () => {
                    activeTrimHandle = null;
                    document.removeEventListener('mousemove', handleTrimDrag);
                });
            });
            
            // Touch events
            handle.addEventListener('touchstart', (e) => {
                activeTrimHandle = handle;
                document.addEventListener('touchmove', handleTrimDrag);
                document.addEventListener('touchend', () => {
                    activeTrimHandle = null;
                    document.removeEventListener('touchmove', handleTrimDrag);
                });
            });
        });
        
        // Trim actions
        this.container.querySelector('#cancelTrimBtn').addEventListener('click', () => {
            this.exitTrimMode();
        });
        
        this.container.querySelector('#confirmTrimBtn').addEventListener('click', () => {
            this.confirmTrim(trimStartTime, trimEndTime);
        });
        
        // Store references
        this.trimControls = {
            overlay: trimOverlay,
            updateUI: updateTrimUI,
            setTimes: (start, end) => {
                trimStartTime = start;
                trimEndTime = end;
                updateTrimUI();
            }
        };
    }
    
    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            if (!this.container.contains(document.activeElement)) return;
            
            switch(e.key) {
                case ' ':
                    e.preventDefault();
                    this.videoPlayer.togglePlayPause();
                    break;
                case 'i':
                case 'I':
                    this.markIn();
                    break;
                case 'o':
                case 'O':
                    this.markOut();
                    break;
                case 's':
                case 'S':
                    if (!e.ctrlKey) this.splitAtPlayhead();
                    break;
                case 'Delete':
                    this.deleteSelectedSegment();
                    break;
                case 'z':
                case 'Z':
                    if (e.ctrlKey) {
                        e.shiftKey ? this.redo() : this.undo();
                    }
                    break;
            }
        });
    }
    
    setupAutoSave() {
        if (!this.options.enableAutoSave) return;
        
        this.autoSaveInterval = setInterval(() => {
            if (this.isModified) {
                this.autoSave();
            }
        }, this.options.autoSaveInterval);
    }
    
    setupJogwheel() {
        // Create jogwheel container
        const jogwheelContainer = document.createElement('div');
        jogwheelContainer.className = 'jogwheel-container';
        this.container.appendChild(jogwheelContainer);
        
        // Initialize jogwheel
        this.jogwheel = new JogwheelControl(jogwheelContainer, {
            size: 180,
            sensitivity: 0.5,
            hapticFeedback: true
        });
        
        // Jogwheel events
        this.jogwheel.on('scrub', (amount) => {
            const currentTime = this.videoPlayer.getCurrentTime();
            const newTime = currentTime + amount;
            this.videoPlayer.seek(newTime);
        });
        
        this.jogwheel.on('start', () => {
            this.videoPlayer.pause();
        });
        
        // Show jogwheel on timeline hover or touch
        let jogwheelTimeout;
        this.timeline.container.addEventListener('mouseenter', () => {
            clearTimeout(jogwheelTimeout);
            this.jogwheel.show();
        });
        
        this.timeline.container.addEventListener('mouseleave', () => {
            jogwheelTimeout = setTimeout(() => {
                this.jogwheel.hide();
            }, 1000);
        });
        
        // Touch activation
        this.timeline.container.addEventListener('touchstart', (e) => {
            if (e.touches.length === 2) {
                // Two-finger touch shows jogwheel
                this.jogwheel.show();
                e.preventDefault();
            }
        });
    }
    
    setupAdaptiveTimeline() {
        // Replace standard timeline with adaptive timeline
        const timelineContainer = this.container.querySelector('.timeline-container');
        if (!timelineContainer) return;
        
        // Create adaptive timeline
        this.adaptiveTimeline = new AdaptiveTimeline(timelineContainer, {
            height: 120,
            thumbnailWidth: 160,
            fps: this.options.framerate,
            enableWaveform: true,
            enableThumbnails: true,
            enableMotionTracking: true
        });
        
        // Connect to video player
        this.videoPlayer.on('loadedmetadata', () => {
            const duration = this.videoPlayer.getDuration();
            this.adaptiveTimeline.setVideo(this.videoPlayer.videoElement, duration);
        });
        
        // Sync playback
        this.videoPlayer.on('timeupdate', (time) => {
            this.adaptiveTimeline.updateTime(time);
        });
        
        // Timeline seek
        this.adaptiveTimeline.onSeek = (time) => {
            this.videoPlayer.seek(time);
        };
    }
    
    // Video loading and initialization
    
    loadVideo(videoBlob, patientInfo, metadata = {}) {
        console.log('MedicalVideoEditor: Loading video for editing');
        
        this.videoBlob = videoBlob;
        this.patientInfo = patientInfo;
        this.metadata = metadata;
        
        // Load video in player
        this.videoPlayer.loadVideo(videoBlob, metadata);
        
        // Initialize segments with full video
        this.segments = [{
            id: this.generateId(),
            startTime: 0,
            endTime: metadata.duration || 0,
            type: 'original',
            selected: false
        }];
        
        this.updateSegmentList();
        this.updateTimeline();
        
        // Load any saved session
        this.loadSession();
    }
    
    // Trim functionality
    
    enterTrimMode() {
        console.log('MedicalVideoEditor: Entering trim mode');
        
        const currentTime = this.videoPlayer.getCurrentTime();
        const duration = this.videoPlayer.getDuration();
        
        // Set initial trim points
        const trimStart = Math.max(0, currentTime - 5);
        const trimEnd = Math.min(duration, currentTime + 5);
        
        this.trimControls.setTimes(trimStart, trimEnd);
        this.trimControls.overlay.classList.remove('hidden');
        
        // Pause video
        this.videoPlayer.pause();
        
        this.emit('trimModeEntered');
    }
    
    exitTrimMode() {
        console.log('MedicalVideoEditor: Exiting trim mode');
        this.trimControls.overlay.classList.add('hidden');
        this.emit('trimModeExited');
    }
    
    confirmTrim(startTime, endTime) {
        console.log(`MedicalVideoEditor: Trimming from ${startTime} to ${endTime}`);
        
        // Add to history
        this.addToHistory({
            action: 'trim',
            before: [...this.segments],
            trimStart: startTime,
            trimEnd: endTime
        });
        
        // Create new segment
        const newSegment = {
            id: this.generateId(),
            startTime: startTime,
            endTime: endTime,
            type: 'trimmed',
            selected: false
        };
        
        // Replace segments with trimmed version
        this.segments = [newSegment];
        
        this.updateSegmentList();
        this.updateTimeline();
        this.exitTrimMode();
        this.setModified(true);
        
        this.emit('trimmed', { segment: newSegment });
    }
    
    // Split functionality
    
    splitAtPlayhead() {
        const currentTime = this.videoPlayer.getCurrentTime();
        console.log(`MedicalVideoEditor: Splitting at ${currentTime}`);
        
        // Find segment containing current time
        const segmentIndex = this.segments.findIndex(seg => 
            currentTime >= seg.startTime && currentTime <= seg.endTime
        );
        
        if (segmentIndex === -1) return;
        
        const segment = this.segments[segmentIndex];
        
        // Don't split if too close to edges
        if (currentTime - segment.startTime < this.options.minSegmentDuration ||
            segment.endTime - currentTime < this.options.minSegmentDuration) {
            console.warn('Cannot split: Too close to segment edge');
            return;
        }
        
        // Add to history
        this.addToHistory({
            action: 'split',
            before: [...this.segments],
            splitTime: currentTime,
            segmentIndex: segmentIndex
        });
        
        // Create two new segments
        const firstSegment = {
            id: this.generateId(),
            startTime: segment.startTime,
            endTime: currentTime,
            type: 'split',
            selected: false
        };
        
        const secondSegment = {
            id: this.generateId(),
            startTime: currentTime,
            endTime: segment.endTime,
            type: 'split',
            selected: false
        };
        
        // Replace original segment with two new ones
        this.segments.splice(segmentIndex, 1, firstSegment, secondSegment);
        
        this.updateSegmentList();
        this.updateTimeline();
        this.setModified(true);
        
        this.emit('split', { segments: [firstSegment, secondSegment] });
    }
    
    // Delete functionality
    
    deleteSelectedSegment() {
        if (!this.selectedSegment) {
            console.warn('No segment selected for deletion');
            return;
        }
        
        console.log(`MedicalVideoEditor: Deleting segment ${this.selectedSegment.id}`);
        
        // Add to history
        this.addToHistory({
            action: 'delete',
            before: [...this.segments],
            deletedSegment: this.selectedSegment
        });
        
        // Remove segment
        const index = this.segments.findIndex(seg => seg.id === this.selectedSegment.id);
        if (index > -1) {
            this.segments.splice(index, 1);
        }
        
        this.selectedSegment = null;
        this.updateSegmentList();
        this.updateTimeline();
        this.setModified(true);
        
        this.emit('segmentDeleted');
    }
    
    // Join functionality
    
    joinSelectedSegments() {
        // Get selected segments
        const selected = this.segments.filter(seg => seg.selected);
        
        if (selected.length < 2) {
            console.warn('Select at least 2 segments to join');
            return;
        }
        
        // Sort by start time
        selected.sort((a, b) => a.startTime - b.startTime);
        
        // Check if segments are adjacent
        let canJoin = true;
        for (let i = 0; i < selected.length - 1; i++) {
            if (Math.abs(selected[i].endTime - selected[i + 1].startTime) > 0.1) {
                canJoin = false;
                break;
            }
        }
        
        if (!canJoin) {
            console.warn('Can only join adjacent segments');
            return;
        }
        
        console.log('MedicalVideoEditor: Joining segments');
        
        // Add to history
        this.addToHistory({
            action: 'join',
            before: [...this.segments],
            joinedSegments: selected
        });
        
        // Create joined segment
        const joinedSegment = {
            id: this.generateId(),
            startTime: selected[0].startTime,
            endTime: selected[selected.length - 1].endTime,
            type: 'joined',
            selected: false
        };
        
        // Remove selected segments and add joined one
        this.segments = this.segments.filter(seg => !seg.selected);
        this.segments.push(joinedSegment);
        this.segments.sort((a, b) => a.startTime - b.startTime);
        
        this.updateSegmentList();
        this.updateTimeline();
        this.setModified(true);
        
        this.emit('segmentsJoined', { segment: joinedSegment });
    }
    
    // Quick marking functions
    
    markIn() {
        const currentTime = this.videoPlayer.getCurrentTime();
        console.log(`MedicalVideoEditor: Mark in at ${currentTime}`);
        
        this.markInTime = currentTime;
        this.emit('markIn', { time: currentTime });
        
        // Show visual feedback
        this.showMarker('in', currentTime);
    }
    
    markOut() {
        const currentTime = this.videoPlayer.getCurrentTime();
        console.log(`MedicalVideoEditor: Mark out at ${currentTime}`);
        
        this.markOutTime = currentTime;
        this.emit('markOut', { time: currentTime });
        
        // Show visual feedback
        this.showMarker('out', currentTime);
        
        // If both marks set, create segment
        if (this.markInTime !== undefined && this.markOutTime !== undefined) {
            this.createSegmentFromMarks();
        }
    }
    
    markCritical() {
        const currentTime = this.videoPlayer.getCurrentTime();
        console.log(`MedicalVideoEditor: Critical moment at ${currentTime}`);
        
        // Add critical marker to timeline
        this.timeline.addCriticalMarker(currentTime);
        
        // Add to metadata
        if (!this.metadata.criticalMoments) {
            this.metadata.criticalMoments = [];
        }
        
        this.metadata.criticalMoments.push({
            time: currentTime,
            timestamp: new Date(),
            type: 'critical'
        });
        
        this.emit('criticalMarked', { time: currentTime });
    }
    
    createSegmentFromMarks() {
        if (this.markInTime === undefined || this.markOutTime === undefined) return;
        
        const startTime = Math.min(this.markInTime, this.markOutTime);
        const endTime = Math.max(this.markInTime, this.markOutTime);
        
        if (endTime - startTime < this.options.minSegmentDuration) {
            console.warn('Marked segment too short');
            return;
        }
        
        // Add to history
        this.addToHistory({
            action: 'createFromMarks',
            before: [...this.segments],
            markIn: startTime,
            markOut: endTime
        });
        
        // Create new segment
        const newSegment = {
            id: this.generateId(),
            startTime: startTime,
            endTime: endTime,
            type: 'marked',
            selected: false
        };
        
        this.segments.push(newSegment);
        this.segments.sort((a, b) => a.startTime - b.startTime);
        
        // Clear marks
        this.markInTime = undefined;
        this.markOutTime = undefined;
        
        this.updateSegmentList();
        this.updateTimeline();
        this.setModified(true);
        
        this.emit('segmentCreated', { segment: newSegment });
    }
    
    // History management
    
    addToHistory(action) {
        // Remove any actions after current index
        this.editHistory = this.editHistory.slice(0, this.historyIndex + 1);
        
        // Add new action
        this.editHistory.push(action);
        this.historyIndex++;
        
        // Limit history size
        if (this.editHistory.length > 50) {
            this.editHistory.shift();
            this.historyIndex--;
        }
        
        this.updateHistoryButtons();
    }
    
    undo() {
        if (this.historyIndex < 0) return;
        
        const action = this.editHistory[this.historyIndex];
        console.log(`MedicalVideoEditor: Undo ${action.action}`);
        
        // Restore previous state
        this.segments = [...action.before];
        this.historyIndex--;
        
        this.updateSegmentList();
        this.updateTimeline();
        this.updateHistoryButtons();
        this.setModified(true);
        
        this.emit('undo', { action: action.action });
    }
    
    redo() {
        if (this.historyIndex >= this.editHistory.length - 1) return;
        
        this.historyIndex++;
        const action = this.editHistory[this.historyIndex];
        console.log(`MedicalVideoEditor: Redo ${action.action}`);
        
        // Re-apply action
        switch (action.action) {
            case 'trim':
                this.segments = [{
                    id: this.generateId(),
                    startTime: action.trimStart,
                    endTime: action.trimEnd,
                    type: 'trimmed',
                    selected: false
                }];
                break;
            case 'split':
                // Re-apply split
                const segment = action.before[action.segmentIndex];
                const firstSegment = {
                    id: this.generateId(),
                    startTime: segment.startTime,
                    endTime: action.splitTime,
                    type: 'split',
                    selected: false
                };
                const secondSegment = {
                    id: this.generateId(),
                    startTime: action.splitTime,
                    endTime: segment.endTime,
                    type: 'split',
                    selected: false
                };
                this.segments = [...action.before];
                this.segments.splice(action.segmentIndex, 1, firstSegment, secondSegment);
                break;
            // Add other action types as needed
        }
        
        this.updateSegmentList();
        this.updateTimeline();
        this.updateHistoryButtons();
        this.setModified(true);
        
        this.emit('redo', { action: action.action });
    }
    
    reset() {
        console.log('MedicalVideoEditor: Resetting to original');
        
        if (!confirm('Alle Änderungen verwerfen und zum Original zurückkehren?')) {
            return;
        }
        
        // Reset to original single segment
        this.segments = [{
            id: this.generateId(),
            startTime: 0,
            endTime: this.videoPlayer.getDuration(),
            type: 'original',
            selected: false
        }];
        
        this.editHistory = [];
        this.historyIndex = -1;
        this.selectedSegment = null;
        this.markInTime = undefined;
        this.markOutTime = undefined;
        
        this.updateSegmentList();
        this.updateTimeline();
        this.updateHistoryButtons();
        this.setModified(false);
        
        this.emit('reset');
    }
    
    updateHistoryButtons() {
        const undoBtn = this.container.querySelector('#undoBtn');
        const redoBtn = this.container.querySelector('#redoBtn');
        
        undoBtn.disabled = this.historyIndex < 0;
        redoBtn.disabled = this.historyIndex >= this.editHistory.length - 1;
    }
    
    // UI Updates
    
    updateSegmentList() {
        const segmentItems = this.container.querySelector('#segmentItems');
        const segmentCount = this.container.querySelector('#segmentCount');
        
        segmentItems.innerHTML = '';
        segmentCount.textContent = `${this.segments.length} Segmente`;
        
        this.segments.forEach((segment, index) => {
            const duration = segment.endTime - segment.startTime;
            const item = document.createElement('div');
            item.className = `segment-item ${segment.selected ? 'selected' : ''}`;
            item.dataset.segmentId = segment.id;
            
            item.innerHTML = `
                <div class="segment-info">
                    <span class="segment-index">#${index + 1}</span>
                    <span class="segment-time">${this.formatTime(segment.startTime)} - ${this.formatTime(segment.endTime)}</span>
                    <span class="segment-duration">${this.formatTime(duration)}</span>
                </div>
                <div class="segment-actions">
                    <button class="segment-btn play" title="Abspielen">
                        <i class="ms-Icon ms-Icon--Play"></i>
                    </button>
                    <button class="segment-btn select" title="Auswählen">
                        <i class="ms-Icon ms-Icon--CheckboxComposite"></i>
                    </button>
                </div>
            `;
            
            // Event listeners
            item.addEventListener('click', () => this.selectSegment(segment));
            
            const playBtn = item.querySelector('.play');
            playBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.playSegment(segment);
            });
            
            const selectBtn = item.querySelector('.select');
            selectBtn.addEventListener('click', (e) => {
                e.stopPropagation();
                this.toggleSegmentSelection(segment);
            });
            
            segmentItems.appendChild(item);
        });
    }
    
    updateTimeline() {
        // Update timeline with segments
        this.timeline.segments = this.segments.map(seg => ({
            id: seg.id,
            startTime: seg.startTime,
            endTime: seg.endTime,
            isActive: seg.selected
        }));
        
        this.timeline.updateSegments();
    }
    
    selectSegment(segment) {
        // Deselect all
        this.segments.forEach(seg => seg.selected = false);
        
        // Select clicked segment
        segment.selected = true;
        this.selectedSegment = segment;
        
        this.updateSegmentList();
        this.updateTimeline();
        
        // Seek to segment start
        this.videoPlayer.seek(segment.startTime);
        
        this.emit('segmentSelected', { segment });
    }
    
    toggleSegmentSelection(segment) {
        segment.selected = !segment.selected;
        
        if (segment.selected) {
            this.selectedSegment = segment;
        } else if (this.selectedSegment === segment) {
            this.selectedSegment = null;
        }
        
        this.updateSegmentList();
        this.updateTimeline();
    }
    
    playSegment(segment) {
        console.log(`MedicalVideoEditor: Playing segment ${segment.id}`);
        
        // Seek to start and play
        this.videoPlayer.seek(segment.startTime);
        this.videoPlayer.play();
        
        // Set up end time monitoring
        const checkEnd = () => {
            if (this.videoPlayer.getCurrentTime() >= segment.endTime) {
                this.videoPlayer.pause();
                this.videoPlayer.off('timeupdate', checkEnd);
            }
        };
        
        this.videoPlayer.on('timeupdate', checkEnd);
    }
    
    // Export functions
    
    async exportToDicom() {
        console.log('MedicalVideoEditor: Exporting to DICOM');
        
        const exportData = {
            segments: this.segments,
            criticalMoments: this.metadata.criticalMoments || [],
            annotations: this.videoPlayer.getAnnotations(),
            patientInfo: this.patientInfo,
            editInfo: {
                editedAt: new Date(),
                segmentCount: this.segments.length,
                totalDuration: this.calculateTotalDuration()
            }
        };
        
        this.emit('exportDicom', exportData);
        
        // Show export progress
        this.showExportProgress();
    }
    
    async exportSegments() {
        console.log('MedicalVideoEditor: Exporting segments');
        
        const selectedSegments = this.segments.filter(seg => seg.selected);
        if (selectedSegments.length === 0) {
            alert('Bitte wählen Sie Segmente zum Exportieren aus');
            return;
        }
        
        const exportData = {
            segments: selectedSegments,
            format: 'webm', // or let user choose
            includeAnnotations: true
        };
        
        this.emit('exportSegments', exportData);
    }
    
    // Session management
    
    saveSession() {
        console.log('MedicalVideoEditor: Saving editing session');
        
        const sessionData = {
            version: '1.0',
            timestamp: new Date(),
            patientInfo: this.patientInfo,
            segments: this.segments,
            editHistory: this.editHistory,
            historyIndex: this.historyIndex,
            metadata: this.metadata,
            annotations: this.videoPlayer.getAnnotations()
        };
        
        // Save to localStorage or send to backend
        const sessionKey = `edit_session_${this.patientInfo.id}_${Date.now()}`;
        localStorage.setItem(sessionKey, JSON.stringify(sessionData));
        
        this.showAutoSaveIndicator();
        this.setModified(false);
        
        this.emit('sessionSaved', { key: sessionKey });
    }
    
    loadSession() {
        // Check for existing sessions for this patient
        const sessions = this.findSessions(this.patientInfo.id);
        
        if (sessions.length > 0) {
            // Load most recent session
            const latestSession = sessions[0];
            const sessionData = JSON.parse(localStorage.getItem(latestSession.key));
            
            if (sessionData) {
                this.segments = sessionData.segments;
                this.editHistory = sessionData.editHistory || [];
                this.historyIndex = sessionData.historyIndex || -1;
                this.metadata = { ...this.metadata, ...sessionData.metadata };
                
                if (sessionData.annotations) {
                    sessionData.annotations.forEach(ann => {
                        this.videoPlayer.addAnnotation(ann);
                    });
                }
                
                this.updateSegmentList();
                this.updateTimeline();
                
                console.log('MedicalVideoEditor: Session loaded');
            }
        }
    }
    
    findSessions(patientId) {
        const sessions = [];
        const prefix = `edit_session_${patientId}_`;
        
        for (let i = 0; i < localStorage.length; i++) {
            const key = localStorage.key(i);
            if (key.startsWith(prefix)) {
                sessions.push({
                    key: key,
                    timestamp: parseInt(key.split('_').pop())
                });
            }
        }
        
        // Sort by timestamp descending
        sessions.sort((a, b) => b.timestamp - a.timestamp);
        
        return sessions;
    }
    
    autoSave() {
        this.saveSession();
        console.log('MedicalVideoEditor: Auto-saved');
    }
    
    // Helper functions
    
    formatTime(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        const ms = Math.floor((seconds % 1) * 100);
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}.${ms.toString().padStart(2, '0')}`;
    }
    
    generateId() {
        return `seg_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }
    
    calculateTotalDuration() {
        return this.segments.reduce((total, seg) => total + (seg.endTime - seg.startTime), 0);
    }
    
    setModified(modified) {
        this.isModified = modified;
        
        // Update UI to show unsaved changes
        if (modified) {
            this.container.classList.add('modified');
        } else {
            this.container.classList.remove('modified');
        }
    }
    
    showMarker(type, time) {
        // Visual feedback for mark in/out
        const marker = document.createElement('div');
        marker.className = `mark-indicator ${type}`;
        marker.textContent = type === 'in' ? 'IN' : 'OUT';
        
        this.container.appendChild(marker);
        
        setTimeout(() => {
            marker.remove();
        }, 2000);
    }
    
    showAutoSaveIndicator() {
        const indicator = this.container.querySelector('#autosaveIndicator');
        indicator.classList.add('visible');
        
        setTimeout(() => {
            indicator.classList.remove('visible');
        }, 3000);
    }
    
    showExportProgress() {
        // Show export progress overlay
        const progress = document.createElement('div');
        progress.className = 'export-progress';
        progress.innerHTML = `
            <div class="progress-content">
                <div class="progress-spinner"></div>
                <h3>Exportiere zu DICOM...</h3>
                <p>Bitte warten</p>
            </div>
        `;
        
        this.container.appendChild(progress);
        
        // Remove after export completes
        setTimeout(() => {
            progress.remove();
        }, 3000);
    }
    
    // Event handlers
    
    onVideoTimeUpdate(e) {
        // Update timeline position
        this.timeline.updateTime(e.detail.time);
    }
    
    onVideoSeek(e) {
        // Update timeline position
        this.timeline.currentTime = e.detail.time;
        this.timeline.updateTimeIndicator();
    }
    
    onTimelineSeek(e) {
        // Update video position
        this.videoPlayer.seek(e.detail.time);
    }
    
    onSegmentSelected(e) {
        // Find and select segment
        const segment = this.segments.find(seg => seg.id === e.detail.segmentId);
        if (segment) {
            this.selectSegment(segment);
        }
    }
    
    // Cleanup
    
    destroy() {
        // Clear auto-save
        if (this.autoSaveInterval) {
            clearInterval(this.autoSaveInterval);
        }
        
        // Save current session
        if (this.isModified) {
            this.saveSession();
        }
        
        // Cleanup components
        this.videoPlayer.destroy();
        
        console.log('MedicalVideoEditor: Destroyed');
    }
    
    // Event system
    
    emit(eventType, data = {}) {
        const event = new CustomEvent(`videoEditor:${eventType}`, {
            detail: data
        });
        this.container.dispatchEvent(event);
    }
    
    on(eventType, callback) {
        this.container.addEventListener(`videoEditor:${eventType}`, callback);
    }
    
    off(eventType, callback) {
        this.container.removeEventListener(`videoEditor:${eventType}`, callback);
    }
}

// Export for use
window.MedicalVideoEditor = MedicalVideoEditor;