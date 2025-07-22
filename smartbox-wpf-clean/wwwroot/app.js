/**
 * SmartBox Next Touch Application
 * Main application controller for touch-optimized medical imaging
 */

class SmartBoxTouchApp {
    constructor() {
        this.isInitialized = false;
        this.webcamStream = null;
        this.isRecording = false;
        this.mediaRecorder = null;
        this.recordingStart = null;
        this.recordingTimer = null;
        
        // Initialize managers
        this.gestureManager = null;
        this.dialogManager = null;
        this.modeManager = null;
        this.timelineManager = null;
        
        // MWL data
        this.mwlData = [];
        this.filteredMwlData = [];
        
        // Sort state
        this.sortState = { column: 'name', ascending: true };
        
        console.log('SmartBoxTouchApp: Starting initialization...');
        this.initialize();
    }

    async initialize() {
        try {
            // Initialize managers
            this.gestureManager = new TouchGestureManager();
            this.dialogManager = new TouchDialogManager();
            this.modeManager = new ModeManager();
            this.timelineManager = new TimelineIntegrationManager(this);
            
            // Make dialog manager globally available
            window.touchDialogManager = this.dialogManager;
            
            // Set up event listeners
            this.setupEventListeners();
            
            // Initialize webcam
            await this.initializeWebcam();
            
            // Load initial MWL data with saved default period
            await this.loadDefaultMWLData();
            
            // Set up periodic refresh
            this.setupPeriodicRefresh();
            
            this.isInitialized = true;
            console.log('SmartBoxTouchApp: Initialization complete');
            
            // Hide loading overlay if it exists
            const loadingOverlay = document.getElementById('loadingOverlay');
            if (loadingOverlay) {
                loadingOverlay.classList.add('hidden');
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Initialization failed:', error);
            this.showError('Fehler beim Starten der Anwendung: ' + error.message);
        }
    }

    setupEventListeners() {
        // Gesture events
        document.addEventListener('mwlRefresh', () => this.onMWLRefresh());
        document.addEventListener('emergencyPatientSelected', (e) => this.onEmergencyPatientSelected(e));
        document.addEventListener('capturePhoto', () => this.onCapturePhoto());
        document.addEventListener('startVideoRecording', () => this.onStartVideoRecording());
        document.addEventListener('stopVideoRecording', () => this.onStopVideoRecording());
        document.addEventListener('confirmDeleteThumbnail', (e) => this.onConfirmDeleteThumbnail(e));
        
        // Critical moment marking
        document.addEventListener('criticalMomentMarked', (e) => this.onCriticalMomentMarked(e));
        
        // Mode change events
        document.addEventListener('modeChanged', (e) => this.onModeChanged(e));
        document.addEventListener('initializeRecordingWebcam', (e) => this.onInitializeRecordingWebcam(e));
        
        // UI events
        this.setupUIEventListeners();
        
        console.log('SmartBoxTouchApp: Event listeners set up');
    }

    setupUIEventListeners() {
        // MWL filter
        const mwlFilter = document.getElementById('mwlFilter');
        if (mwlFilter) {
            mwlFilter.addEventListener('input', (e) => this.onMWLFilterChange(e));
        }
        
        // Date range selector
        const dateRangeSelect = document.getElementById('mwlDateRange');
        if (dateRangeSelect) {
            dateRangeSelect.addEventListener('change', (e) => this.onDateRangeChange(e));
        }
        
        // Custom date inputs
        const dateFrom = document.getElementById('mwlDateFrom');
        const dateTo = document.getElementById('mwlDateTo');
        if (dateFrom) {
            dateFrom.addEventListener('change', () => this.onCustomDateChange());
        }
        if (dateTo) {
            dateTo.addEventListener('change', () => this.onCustomDateChange());
        }
        
        // MIGRATED TO ACTION SYSTEM - Old event listeners disabled
        // Export, Settings, and Exit buttons now use data-action attributes
        
        // Video recording toggle handler
        document.addEventListener('toggleVideoRecording', () => {
            if (window.buttonActionManager) {
                const state = window.buttonActionManager.getRecordingState();
                if (state.isRecording) {
                    this.onStopVideoRecording();
                } else {
                    this.onStartVideoRecording();
                }
            }
        });
        
        // // Export button
        // const exportButton = document.getElementById('exportButton');
        // if (exportButton) {
        //     exportButton.addEventListener('click', () => this.onExportRequested());
        // }
        
        // // Settings button
        // const settingsButton = document.getElementById('settingsButton');
        // if (settingsButton) {
        //     console.log('Settings button found, adding event listener');
        //     settingsButton.addEventListener('click', (e) => {
        //         e.preventDefault();
        //         e.stopPropagation();
        //         console.log('Settings button clicked');
        //         this.openSettings();
        //     });
        // } else {
        //     console.error('Settings button not found!');
        // }
        
        // // Exit button
        // const exitButton = document.getElementById('exitButton');
        // if (exitButton) {
        //     console.log('Exit button found, adding event listener');
        //     exitButton.addEventListener('click', (e) => {
        //         e.preventDefault();
        //         e.stopPropagation();
        //         console.log('Exit button clicked');
        //         this.onExitRequested();
        //     });
        // } else {
        //     console.error('Exit button not found!');
        // }
        
        // Back to patient selection button
        const backButton = document.getElementById('backToPatientSelection');
        if (backButton) {
            backButton.addEventListener('click', () => this.onBackToPatientSelection());
        }
        
        // Patient row clicks
        document.addEventListener('click', (e) => {
            const patientRow = e.target.closest('.mwl-table tbody tr');
            if (patientRow) {
                this.onPatientRowClick(patientRow);
            }
            
            // Theme button clicks
            const themeButton = e.target.closest('.theme-button');
            if (themeButton) {
                this.onThemeChange(themeButton.dataset.theme);
            }
            
            // Sort header clicks
            const sortHeader = e.target.closest('.mwl-table th.sortable');
            if (sortHeader) {
                this.onSortColumn(sortHeader.dataset.sort);
            }
        });
    }

    async initializeWebcam() {
        try {
            console.log('SmartBoxTouchApp: Initializing webcam...');
            
            const constraints = {
                video: {
                    width: { ideal: 1280 },
                    height: { ideal: 960 },
                    facingMode: 'user'
                },
                audio: false // Disable audio for now to avoid permission issues
            };
            
            this.webcamStream = await navigator.mediaDevices.getUserMedia(constraints);
            
            // Initialize small preview
            const smallPreview = document.getElementById('webcamPreviewSmall');
            if (smallPreview) {
                smallPreview.srcObject = this.webcamStream;
                
                // Hide overlay when webcam starts
                smallPreview.addEventListener('loadedmetadata', () => {
                    const overlay = smallPreview.parentElement.querySelector('.preview-overlay');
                    if (overlay) {
                        overlay.style.display = 'none';
                    }
                    console.log('SmartBoxTouchApp: Webcam preview started');
                });
                
                // Show overlay if webcam fails
                smallPreview.addEventListener('error', () => {
                    const overlay = smallPreview.parentElement.querySelector('.preview-overlay');
                    if (overlay) {
                        overlay.style.display = 'flex';
                        overlay.innerHTML = '<i class="ms-Icon ms-Icon--Warning"></i><span>Kamera-Fehler</span>';
                    }
                });
            }
            
            console.log('SmartBoxTouchApp: Webcam initialized');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Webcam initialization failed:', error);
            
            // Show error in overlay instead of alert
            const smallPreview = document.getElementById('webcamPreviewSmall');
            if (smallPreview) {
                const overlay = smallPreview.parentElement.querySelector('.preview-overlay');
                if (overlay) {
                    overlay.innerHTML = '<i class="ms-Icon ms-Icon--Warning"></i><span>Kamera nicht verfügbar</span>';
                }
            }
        }
    }

    async onInitializeRecordingWebcam(event) {
        const videoElement = event.detail.videoElement;
        if (videoElement && this.webcamStream) {
            videoElement.srcObject = this.webcamStream;
            console.log('SmartBoxTouchApp: Recording webcam initialized');
        }
    }

    async loadDefaultMWLData() {
        // Request settings from backend to get default query period
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: 'getSettings',
                data: {}
            }));
            
            // Wait a bit for settings, then load with default
            setTimeout(() => {
                this.loadMWLData();
            }, 500);
        } else {
            // No backend, just load with default
            this.loadMWLData();
        }
    }

    async loadMWLData(dateRange = null) {
        try {
            console.log('SmartBoxTouchApp: Loading MWL data...');
            
            // Always show demo data first for immediate UI feedback
            this.onMWLDataReceived(this.getDemoMWLData());
            
            // Determine date range
            if (!dateRange) {
                const dateRangeSelect = document.getElementById('mwlDateRange');
                dateRange = dateRangeSelect ? dateRangeSelect.value : '3days';
            }
            
            // Calculate dates based on selection
            let fromDate, toDate;
            const today = new Date();
            
            switch (dateRange) {
                case 'today':
                    fromDate = toDate = today;
                    break;
                case '3days':
                    fromDate = new Date(today);
                    fromDate.setDate(today.getDate() - 1); // Yesterday
                    toDate = new Date(today);
                    toDate.setDate(today.getDate() + 1); // Tomorrow
                    break;
                case 'week':
                    fromDate = new Date(today);
                    fromDate.setDate(today.getDate() - today.getDay()); // Start of week
                    toDate = new Date(fromDate);
                    toDate.setDate(fromDate.getDate() + 6); // End of week
                    break;
                case 'custom':
                    const dateFromInput = document.getElementById('mwlDateFrom');
                    const dateToInput = document.getElementById('mwlDateTo');
                    if (dateFromInput.value && dateToInput.value) {
                        fromDate = new Date(dateFromInput.value);
                        toDate = new Date(dateToInput.value);
                    } else {
                        // Default to 3 days if custom dates not set
                        fromDate = new Date(today);
                        fromDate.setDate(today.getDate() - 1);
                        toDate = new Date(today);
                        toDate.setDate(today.getDate() + 1);
                    }
                    break;
            }
            
            // Send request to WebView2 host for real data
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'loadMWL',
                    data: {
                        fromDate: fromDate.toISOString().split('T')[0],
                        toDate: toDate.toISOString().split('T')[0]
                    }
                }));
                
                // Set timeout to ensure demo data shows if no response
                setTimeout(() => {
                    if (this.mwlData.length <= 3) {
                        console.log('SmartBoxTouchApp: No MWL response, keeping demo data');
                    }
                }, 2000);
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: MWL loading failed:', error);
            // Still show demo data on error
            this.onMWLDataReceived(this.getDemoMWLData());
        }
    }
    
    onCriticalMomentMarked(event) {
        console.log('SmartBoxTouchApp: Critical moment marked:', event.detail);
        
        // Store critical moment with current capture session
        if (this.isRecording && this.mediaRecorder) {
            if (!this.currentRecordingMetadata) {
                this.currentRecordingMetadata = { criticalMoments: [] };
            }
            this.currentRecordingMetadata.criticalMoments.push(event.detail);
            
            // Visual feedback
            if (this.dialogManager) {
                this.dialogManager.showToast('Kritischer Moment markiert', 'success');
            }
        }
    }

    onMWLDataReceived(mwlData) {
        console.log('SmartBoxTouchApp: MWL data received:', mwlData.length, 'entries');
        
        this.mwlData = mwlData;
        this.filteredMwlData = [...mwlData];
        
        // Apply default sorting by name
        this.onSortColumn('name');
    }

    renderMWLCards() {
        const mwlTableBody = document.getElementById('mwlTableBody');
        if (!mwlTableBody) return;
        
        // Clear existing rows
        mwlTableBody.innerHTML = '';
        
        // Render filtered rows
        this.filteredMwlData.forEach(patient => {
            const row = this.createPatientRow(patient);
            mwlTableBody.appendChild(row);
        });
        
        console.log('SmartBoxTouchApp: Rendered', this.filteredMwlData.length, 'patient rows');
    }

    createPatientRow(patient) {
        const row = document.createElement('tr');
        row.dataset.patientId = patient.id;
        
        // Parse name to separate last and first name
        const nameParts = patient.name.split(', ');
        const lastName = nameParts[0] || '';
        const firstName = nameParts[1] || '';
        
        // Determine gender (would normally come from data)
        const gender = patient.gender || 'M'; // Default to M if not provided
        
        // Format study date (using scheduled time for now)
        const today = new Date().toLocaleDateString('de-DE');
        const studyDate = today + ' ' + patient.scheduledTime;
        
        row.innerHTML = `
            <td>${lastName}</td>
            <td>${firstName}</td>
            <td>${patient.birthDate}</td>
            <td>${gender}</td>
            <td>${studyDate}</td>
            <td>${patient.studyType}</td>
            <td>${patient.location || 'OP 1'}</td>
            <td>${patient.comment || ''}</td>
        `;
        
        return row;
    }

    onPatientRowClick(row) {
        const patientId = row.dataset.patientId;
        const patient = this.mwlData.find(p => p.id === patientId);
        
        if (patient) {
            console.log('SmartBoxTouchApp: Patient selected:', patient.name);
            
            // Remove previous selection
            document.querySelectorAll('.mwl-table tbody tr.selected').forEach(r => {
                r.classList.remove('selected');
            });
            
            // Add visual feedback
            row.classList.add('selected');
            
            // Emit patient selection event
            document.dispatchEvent(new CustomEvent('patientSelected', {
                detail: patient
            }));
        }
    }
    
    onThemeChange(theme) {
        // Update root element theme
        document.documentElement.setAttribute('data-theme', theme);
        
        // Update active button
        document.querySelectorAll('.theme-button').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.theme === theme);
        });
        
        console.log('SmartBoxTouchApp: Theme changed to:', theme);
    }
    
    onSortColumn(columnKey) {
        // Track sort direction
        if (!this.sortState) {
            this.sortState = { column: null, ascending: true };
        }
        
        // Toggle direction if same column
        if (this.sortState.column === columnKey) {
            this.sortState.ascending = !this.sortState.ascending;
        } else {
            this.sortState.column = columnKey;
            this.sortState.ascending = true;
        }
        
        // Sort the data
        this.filteredMwlData.sort((a, b) => {
            let aVal, bVal;
            
            switch (columnKey) {
                case 'name':
                    const aName = a.name.split(', ')[0] || '';
                    const bName = b.name.split(', ')[0] || '';
                    aVal = aName.toLowerCase();
                    bVal = bName.toLowerCase();
                    break;
                case 'firstName':
                    const aFirst = a.name.split(', ')[1] || '';
                    const bFirst = b.name.split(', ')[1] || '';
                    aVal = aFirst.toLowerCase();
                    bVal = bFirst.toLowerCase();
                    break;
                case 'birthDate':
                    // Parse German date format DD.MM.YYYY
                    const parseDate = (dateStr) => {
                        const parts = dateStr.split('.');
                        return new Date(parts[2], parts[1] - 1, parts[0]);
                    };
                    aVal = parseDate(a.birthDate).getTime();
                    bVal = parseDate(b.birthDate).getTime();
                    break;
                case 'studyDate':
                    aVal = a.scheduledTime;
                    bVal = b.scheduledTime;
                    break;
                default:
                    aVal = a[columnKey] || '';
                    bVal = b[columnKey] || '';
            }
            
            if (aVal < bVal) return this.sortState.ascending ? -1 : 1;
            if (aVal > bVal) return this.sortState.ascending ? 1 : -1;
            return 0;
        });
        
        // Update sort indicators
        document.querySelectorAll('.mwl-table th').forEach(th => {
            const indicator = th.querySelector('.sort-indicator');
            if (indicator) {
                th.classList.remove('sorted');
                indicator.textContent = '';
            }
        });
        
        const currentHeader = document.querySelector(`[data-sort="${columnKey}"]`);
        if (currentHeader) {
            currentHeader.classList.add('sorted');
            const indicator = currentHeader.querySelector('.sort-indicator');
            if (indicator) {
                indicator.textContent = this.sortState.ascending ? '▲' : '▼';
            }
        }
        
        // Re-render the table
        this.renderMWLCards();
    }

    onEmergencyPatientSelected(event) {
        console.log('SmartBoxTouchApp: Emergency patient selected:', event.detail.type);
        // The mode manager handles this
    }

    onMWLFilterChange(event) {
        const filterText = event.target.value.toLowerCase().trim();
        
        if (filterText === '') {
            this.filteredMwlData = [...this.mwlData];
        } else {
            this.filteredMwlData = this.mwlData.filter(patient => 
                patient.name.toLowerCase().includes(filterText) ||
                patient.studyType.toLowerCase().includes(filterText) ||
                patient.id.toLowerCase().includes(filterText)
            );
        }
        
        this.renderMWLCards();
    }

    onMWLRefresh() {
        console.log('SmartBoxTouchApp: MWL refresh requested');
        this.loadMWLData();
    }

    onModeChanged(event) {
        console.log('SmartBoxTouchApp: Mode changed to:', event.detail.currentMode);
        
        if (event.detail.currentMode === 'recording') {
            // Reset recording state
            this.isRecording = false;
            this.updateRecordingUI();
        }
    }

    async onCapturePhoto() {
        try {
            console.log('SmartBoxTouchApp: Capturing photo...');
            
            const video = document.getElementById('webcamPreviewLarge');
            const canvas = document.getElementById('captureCanvas');
            
            if (!video || !canvas) {
                throw new Error('Capture elements not found');
            }
            
            // Set canvas size to video size - VERIFIED PROPERTIES ONLY!
            // Session 87 Prevention: using videoWidth/videoHeight, NOT width/height
            const videoWidth = video.videoWidth;
            const videoHeight = video.videoHeight;
            
            if (!videoWidth || !videoHeight) {
                console.error('SmartBoxTouchApp: Video dimensions not available');
                throw new Error('Video not ready for capture');
            }
            
            canvas.width = videoWidth;
            canvas.height = videoHeight;
            
            console.log(`SmartBoxTouchApp: Canvas sized to ${videoWidth}x${videoHeight}`);
            
            // Draw video frame to canvas
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0);
            
            // Get image data
            const imageData = canvas.toDataURL('image/jpeg', 0.8);
            
            // Create thumbnail
            const thumbnailCanvas = document.createElement('canvas');
            thumbnailCanvas.width = 160;
            thumbnailCanvas.height = 120;
            const thumbCtx = thumbnailCanvas.getContext('2d');
            thumbCtx.drawImage(video, 0, 0, 160, 120);
            const thumbnail = thumbnailCanvas.toDataURL('image/jpeg', 0.6);
            
            // Add capture to mode manager
            const captureId = this.modeManager.addCapture({
                type: 'photo',
                data: imageData,
                thumbnail: thumbnail
            });
            
            // Add photo thumbnail to timeline
            if (this.timelineManager) {
                this.timelineManager.addPhotoThumbnail(thumbnail);
            }
            
            // Send to WebView2 host for processing
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'capturePhoto',
                    data: {
                        captureId: captureId,
                        imageData: imageData,
                        patient: this.modeManager.getCurrentPatient()
                    }
                }));
            }
            
            console.log('SmartBoxTouchApp: Photo captured successfully');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Photo capture failed:', error);
            this.showError('Foto konnte nicht aufgenommen werden: ' + error.message);
        }
    }

    async onStartVideoRecording() {
        try {
            if (this.isRecording) return;
            
            console.log('SmartBoxTouchApp: Starting video recording...');
            
            if (!this.webcamStream) {
                throw new Error('Webcam not available');
            }
            
            // Create media recorder
            this.mediaRecorder = new MediaRecorder(this.webcamStream, {
                mimeType: 'video/webm;codecs=vp8'
            });
            
            this.recordedChunks = [];
            
            this.mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    this.recordedChunks.push(event.data);
                }
            };
            
            this.mediaRecorder.onstop = () => {
                this.onVideoRecordingComplete();
            };
            
            // Start recording
            this.mediaRecorder.start();
            this.isRecording = true;
            this.recordingStart = Date.now();
            
            // Update UI
            this.updateRecordingUI();
            this.startRecordingTimer();
            
            // Show critical moment button
            const criticalButton = document.getElementById('markCriticalMomentButton');
            if (criticalButton) {
                criticalButton.classList.remove('hidden');
            }
            
            // Add recording class to container
            const captureArea = document.getElementById('captureArea');
            if (captureArea) {
                captureArea.classList.add('recording-active');
            }
            
            // Update video button state
            const videoButton = document.getElementById('toggleVideoButton');
            if (videoButton) {
                videoButton.classList.add('recording');
            }
            
            console.log('SmartBoxTouchApp: Video recording started');
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Video recording start failed:', error);
            this.showError('Video-Aufnahme konnte nicht gestartet werden: ' + error.message);
        }
    }

    onStopVideoRecording() {
        if (!this.isRecording || !this.mediaRecorder) return;
        
        console.log('SmartBoxTouchApp: Stopping video recording...');
        
        this.mediaRecorder.stop();
        this.isRecording = false;
        this.stopRecordingTimer();
        this.updateRecordingUI();
        
        // Hide critical moment button
        const criticalButton = document.getElementById('markCriticalMomentButton');
        if (criticalButton) {
            criticalButton.classList.add('hidden');
        }
        
        // Remove recording class from container
        const captureArea = document.getElementById('captureArea');
        if (captureArea) {
            captureArea.classList.remove('recording-active');
        }
        
        // Update video button state
        const videoButton = document.getElementById('toggleVideoButton');
        if (videoButton) {
            videoButton.classList.remove('recording');
        }
    }

    onVideoRecordingComplete() {
        try {
            const blob = new Blob(this.recordedChunks, { type: 'video/webm' });
            const videoUrl = URL.createObjectURL(blob);
            
            // Create thumbnail from video
            this.createVideoThumbnail(videoUrl).then(thumbnail => {
                const duration = Math.round((Date.now() - this.recordingStart) / 1000);
                
                // Add capture to mode manager
                const captureId = this.modeManager.addCapture({
                    type: 'video',
                    data: videoUrl,
                    thumbnail: thumbnail,
                    duration: duration,
                    criticalMoments: this.currentRecordingMetadata?.criticalMoments || []
                });
                
                // Reset metadata
                this.currentRecordingMetadata = null;
                
                // Send to WebView2 host for processing
                if (window.chrome && window.chrome.webview) {
                    // Convert blob to base64 for transmission
                    const reader = new FileReader();
                    reader.onload = () => {
                        window.chrome.webview.postMessage(JSON.stringify({
                            type: 'captureVideo',
                            data: {
                                captureId: captureId,
                                videoData: reader.result,
                                duration: duration,
                                patient: this.modeManager.getCurrentPatient()
                            }
                        }));
                    };
                    reader.readAsDataURL(blob);
                }
                
                console.log('SmartBoxTouchApp: Video recording completed, duration:', duration, 'seconds');
            });
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Video processing failed:', error);
            this.showError('Video konnte nicht verarbeitet werden: ' + error.message);
        }
    }

    async createVideoThumbnail(videoUrl) {
        return new Promise((resolve) => {
            const video = document.createElement('video');
            video.src = videoUrl;
            video.currentTime = 1; // Get frame at 1 second
            
            video.onloadeddata = () => {
                const canvas = document.createElement('canvas');
                canvas.width = 160;
                canvas.height = 120;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(video, 0, 0, 160, 120);
                resolve(canvas.toDataURL('image/jpeg', 0.6));
            };
        });
    }

    updateRecordingUI() {
        const recordingIndicator = document.getElementById('recordingIndicator');
        const captureHint = document.getElementById('captureHint');
        
        if (this.isRecording) {
            recordingIndicator?.classList.remove('hidden');
            captureHint?.classList.add('hidden');
        } else {
            recordingIndicator?.classList.add('hidden');
            captureHint?.classList.remove('hidden');
        }
    }

    startRecordingTimer() {
        this.recordingTimer = setInterval(() => {
            const elapsed = Math.floor((Date.now() - this.recordingStart) / 1000);
            const minutes = Math.floor(elapsed / 60);
            const seconds = elapsed % 60;
            
            const timeText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            
            const recordingTime = document.getElementById('recordingTime');
            if (recordingTime) {
                recordingTime.textContent = timeText;
            }
        }, 1000);
    }

    stopRecordingTimer() {
        if (this.recordingTimer) {
            clearInterval(this.recordingTimer);
            this.recordingTimer = null;
        }
    }

    onConfirmDeleteThumbnail(event) {
        const { index, element } = event.detail;
        
        this.dialogManager.confirmDelete('Aufnahme').then((confirmed) => {
            if (confirmed) {
                const captureId = element.dataset.captureId;
                this.modeManager.removeCapture(parseInt(captureId));
                console.log('SmartBoxTouchApp: Thumbnail deleted:', index);
            }
        });
    }

    onExportRequested() {
        console.log('SmartBoxTouchApp: onExportRequested called');
        console.log('SmartBoxTouchApp: modeManager exists:', !!this.modeManager);
        
        // Get captures for export (selected or all)
        const capturesToExport = this.modeManager.getCapturesForExport();
        console.log('SmartBoxTouchApp: Captures to export:', capturesToExport);
        
        const unexportedCaptures = capturesToExport.filter(c => !c.exported);
        console.log('SmartBoxTouchApp: Unexported captures:', unexportedCaptures);
        
        if (unexportedCaptures.length === 0) {
            this.showError('Keine Aufnahmen zum Exportieren vorhanden.');
            return;
        }
        
        this.dialogManager.confirmExport(unexportedCaptures.length).then((confirmed) => {
            if (confirmed) {
                this.exportCaptures(unexportedCaptures);
            }
        });
    }

    async exportCaptures(captures) {
        try {
            console.log('SmartBoxTouchApp: Exporting', captures.length, 'captures...');
            
            // Show loading dialog
            const message = captures.length === 1 
                ? 'Aufnahme wird exportiert...' 
                : `${captures.length} Aufnahmen werden exportiert...`;
            
            try {
                this.dialogManager.showLoading({ message });
                console.log('SmartBoxTouchApp: Loading dialog shown successfully');
            } catch (dialogError) {
                console.error('SmartBoxTouchApp: Dialog error (continuing anyway):', dialogError);
            }
            
            // Send export request to WebView2 host
            if (window.chrome && window.chrome.webview) {
                console.log('SmartBoxTouchApp: Sending exportCaptures message to C#...');
                const messageData = {
                    type: 'exportCaptures',
                    data: {
                        captures: captures.map(c => ({ 
                            id: c.id, 
                            type: c.type,
                            data: c.data, // Include the actual image data
                            timestamp: c.timestamp
                        })),
                        patient: this.modeManager.getCurrentPatient()
                    }
                };
                console.log('SmartBoxTouchApp: Message data:', messageData);
                console.log('SmartBoxTouchApp: Captures being sent:', messageData.data.captures.length);
                
                try {
                    // WebView2 expects a string, not an object!
                    const messageString = JSON.stringify(messageData);
                    window.chrome.webview.postMessage(messageString);
                    console.log('SmartBoxTouchApp: Message sent successfully');
                } catch (postError) {
                    console.error('SmartBoxTouchApp: Failed to post message:', postError);
                    console.error('SmartBoxTouchApp: Message type:', typeof messageData);
                    console.error('SmartBoxTouchApp: Message keys:', Object.keys(messageData));
                }
            } else {
                // Simulate export for testing
                setTimeout(() => {
                    this.onExportComplete(captures.map(c => c.id));
                }, 3000);
            }
            
        } catch (error) {
            console.error('SmartBoxTouchApp: Export failed:', error);
            this.dialogManager.dismiss();
            this.showError('Export fehlgeschlagen: ' + error.message);
        }
    }

    onExportComplete(captureIds) {
        this.dialogManager.dismiss();
        this.modeManager.markCapturesExported(captureIds);
        
        const message = captureIds.length === 1 
            ? 'Aufnahme erfolgreich exportiert.'
            : `${captureIds.length} Aufnahmen erfolgreich exportiert.`;
        
        this.dialogManager.showSuccess({ message });
        
        console.log('SmartBoxTouchApp: Export completed for', captureIds.length, 'captures');
    }

    setupPeriodicRefresh() {
        // Refresh MWL every 5 minutes
        setInterval(() => {
            if (this.modeManager.getCurrentMode() === 'patient') {
                this.loadMWLData();
            }
        }, 5 * 60 * 1000);
    }

    showError(message) {
        if (this.dialogManager) {
            this.dialogManager.error(message);
        } else {
            alert(message);
        }
    }
    
    openSettings() {
        console.log('Settings button clicked - openSettings called');
        
        // Navigate to settings page
        window.location.href = './settings.html';
    }
    
    onExitRequested() {
        console.log('Exit requested - app.js onExitRequested called');
        
        // Always delegate to mode manager which handles the exit logic including
        // checking for unsaved captures and showing confirmation dialogs
        if (this.modeManager && this.modeManager.onExitRequested) {
            console.log('Delegating to mode manager exit handler');
            this.modeManager.onExitRequested();
        } else {
            // If mode manager not available, exit directly without dialog
            console.log('Mode manager not available, exiting directly');
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify({
                    type: 'exitApp',
                    data: {}
                }));
            }
        }
    }
    
    onBackToPatientSelection() {
        console.log('SmartBoxTouchApp: Back to patient selection requested');
        
        // Check if recording is in progress
        if (this.isRecording) {
            this.dialogManager.showConfirmation({
                title: 'Aufnahme abbrechen?',
                message: 'Die laufende Video-Aufnahme wird gestoppt. Fortfahren?',
                confirmText: 'Ja, zurück',
                cancelText: 'Weiter aufnehmen',
                confirmStyle: 'danger',
                onConfirm: () => {
                    this.stopVideoRecording();
                    this.switchToPatientMode();
                }
            });
        } else {
            // Check for unsaved captures
            const captures = this.modeManager.getCurrentCaptures();
            const unexportedCaptures = captures.filter(c => !c.exported);
            
            if (unexportedCaptures.length > 0) {
                const message = unexportedCaptures.length === 1
                    ? 'Eine Aufnahme wurde noch nicht exportiert. Trotzdem zurück?'
                    : `${unexportedCaptures.length} Aufnahmen wurden noch nicht exportiert. Trotzdem zurück?`;
                
                this.dialogManager.showConfirmation({
                    title: 'Ungespeicherte Aufnahmen',
                    message: message,
                    confirmText: 'Ja, zurück',
                    cancelText: 'Bleiben',
                    confirmStyle: 'danger',
                    onConfirm: () => this.switchToPatientMode()
                });
            } else {
                this.switchToPatientMode();
            }
        }
    }
    
    switchToPatientMode() {
        console.log('SmartBoxTouchApp: Switching to patient selection mode');
        
        // Reset current patient
        if (this.modeManager) {
            this.modeManager.setCurrentPatient(null);
        }
        
        // Switch modes via mode manager
        if (this.modeManager && this.modeManager.switchToMode) {
            this.modeManager.switchToMode('patient');
        } else {
            // Fallback: manual mode switching
            document.getElementById('recordingMode').classList.add('hidden');
            document.getElementById('patientMode').classList.remove('hidden');
            
            // Update header
            const patientStatus = document.getElementById('patientStatusText');
            if (patientStatus) {
                patientStatus.textContent = 'Kein Patient';
            }
        }
    }

    getDemoMWLData() {
        return [
            {
                id: '12345678',
                name: 'Müller, Hans',
                birthDate: '12.05.1965',
                gender: 'M',
                studyType: 'Endoskopie',
                scheduledTime: '14:00',
                location: 'OP 1',
                comment: 'Nüchtern',
                accessionNumber: 'ACC-001',
                studyDescription: 'Gastroskopie'
            },
            {
                id: '23456789',
                name: 'Schmidt, Maria',
                birthDate: '23.08.1978',
                gender: 'W',
                studyType: 'Radiographie',
                scheduledTime: '14:30',
                location: 'Röntgen 2',
                comment: '',
                accessionNumber: 'ACC-002',
                studyDescription: 'Thorax-Röntgen'
            },
            {
                id: '34567890',
                name: 'Weber, Klaus',
                birthDate: '01.01.1990',
                gender: 'M',
                studyType: 'Sonographie',
                scheduledTime: '15:00',
                location: 'Sono 1',
                comment: 'Kontrastmittel vorbereitet',
                accessionNumber: 'ACC-003',
                studyDescription: 'Abdomen-Ultraschall'
            },
            {
                id: '45678901',
                name: 'Meyer, Anna',
                birthDate: '15.03.1955',
                gender: 'W',
                studyType: 'MRT',
                scheduledTime: '15:30',
                location: 'MRT 1',
                comment: 'Platzangst',
                accessionNumber: 'ACC-004',
                studyDescription: 'Kopf-MRT'
            },
            {
                id: '56789012',
                name: 'Fischer, Thomas',
                birthDate: '07.11.1972',
                gender: 'M',
                studyType: 'CT',
                scheduledTime: '16:00',
                location: 'CT 2',
                comment: '',
                accessionNumber: 'ACC-005',
                studyDescription: 'Thorax-CT'
            },
            {
                id: '67890123',
                name: 'Wagner, Lisa',
                birthDate: '20.06.1985',
                gender: 'W',
                studyType: 'Endoskopie',
                scheduledTime: '16:30',
                location: 'OP 2',
                comment: 'Sedierung erwünscht',
                accessionNumber: 'ACC-006',
                studyDescription: 'Koloskopie'
            },
            {
                id: '78901234',
                name: 'Becker, Frank',
                birthDate: '18.09.1960',
                gender: 'M',
                studyType: 'Radiographie',
                scheduledTime: '17:00',
                location: 'Röntgen 1',
                comment: '',
                accessionNumber: 'ACC-007',
                studyDescription: 'Abdomen-Röntgen'
            },
            {
                id: '89012345',
                name: 'Schulz, Emma',
                birthDate: '25.12.1995',
                gender: 'W',
                studyType: 'Sonographie',
                scheduledTime: '17:30',
                location: 'Sono 2',
                comment: 'Schwanger',
                accessionNumber: 'ACC-008',
                studyDescription: 'Gynäkologische Sonographie'
            },
            {
                id: '90123456',
                name: 'Koch, Michael',
                birthDate: '03.04.1948',
                gender: 'M',
                studyType: 'CT',
                scheduledTime: '18:00',
                location: 'CT 1',
                comment: 'Niereninsuffizienz',
                accessionNumber: 'ACC-009',
                studyDescription: 'Abdomen-CT'
            },
            {
                id: '01234567',
                name: 'Hofmann, Sarah',
                birthDate: '14.07.1982',
                gender: 'W',
                studyType: 'MRT',
                scheduledTime: '18:30',
                location: 'MRT 2',
                comment: '',
                accessionNumber: 'ACC-010',
                studyDescription: 'Knie-MRT'
            },
            {
                id: '11234567',
                name: 'Richter, Peter',
                birthDate: '29.02.1956',
                gender: 'M',
                studyType: 'Endoskopie',
                scheduledTime: '19:00',
                location: 'OP 3',
                comment: 'Blutverdünner',
                accessionNumber: 'ACC-011',
                studyDescription: 'ERCP'
            },
            {
                id: '21234567',
                name: 'Klein, Julia',
                birthDate: '11.10.1988',
                gender: 'W',
                studyType: 'Radiographie',
                scheduledTime: '19:30',
                location: 'Röntgen 3',
                comment: '',
                accessionNumber: 'ACC-012',
                studyDescription: 'Hand-Röntgen'
            },
            {
                id: '31234567',
                name: 'Wolf, Andreas',
                birthDate: '22.01.1970',
                gender: 'M',
                studyType: 'Sonographie',
                scheduledTime: '20:00',
                location: 'Sono 1',
                comment: 'Notfall',
                accessionNumber: 'ACC-013',
                studyDescription: 'Notfall-Sonographie'
            },
            {
                id: '41234567',
                name: 'Neumann, Claudia',
                birthDate: '05.05.1975',
                gender: 'W',
                studyType: 'CT',
                scheduledTime: '20:30',
                location: 'CT 2',
                comment: 'Allergie gegen Kontrastmittel',
                accessionNumber: 'ACC-014',
                studyDescription: 'Schädel-CT'
            },
            {
                id: '51234567',
                name: 'Zimmermann, Robert',
                birthDate: '17.08.1992',
                gender: 'M',
                studyType: 'MRT',
                scheduledTime: '21:00',
                location: 'MRT 1',
                comment: '',
                accessionNumber: 'ACC-015',
                studyDescription: 'Wirbelsäulen-MRT'
            }
        ];
    }

    onDateRangeChange(event) {
        const value = event.target.value;
        console.log('SmartBoxTouchApp: Date range changed to:', value);
        
        const dateFrom = document.getElementById('mwlDateFrom');
        const dateTo = document.getElementById('mwlDateTo');
        
        if (value === 'custom') {
            // Show custom date inputs
            dateFrom.classList.remove('hidden');
            dateTo.classList.remove('hidden');
            
            // Set default values if empty
            if (!dateFrom.value || !dateTo.value) {
                const today = new Date();
                const yesterday = new Date(today);
                yesterday.setDate(today.getDate() - 1);
                const tomorrow = new Date(today);
                tomorrow.setDate(today.getDate() + 1);
                
                dateFrom.value = yesterday.toISOString().split('T')[0];
                dateTo.value = tomorrow.toISOString().split('T')[0];
            }
        } else {
            // Hide custom date inputs
            dateFrom.classList.add('hidden');
            dateTo.classList.add('hidden');
            
            // Reload MWL data with new date range
            this.loadMWLData(value);
        }
    }

    onCustomDateChange() {
        const dateFrom = document.getElementById('mwlDateFrom');
        const dateTo = document.getElementById('mwlDateTo');
        
        if (dateFrom.value && dateTo.value) {
            console.log('SmartBoxTouchApp: Custom date range:', dateFrom.value, 'to', dateTo.value);
            this.loadMWLData('custom');
        }
    }
}

// WebView2 message handler
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
        const { type, data } = event.data;
        
        switch (type) {
            case 'mwlData':
                if (window.smartBoxApp) {
                    window.smartBoxApp.onMWLDataReceived(data);
                }
                break;
                
            case 'exportComplete':
                if (window.smartBoxApp) {
                    window.smartBoxApp.onExportComplete(data.captureIds);
                }
                break;
                
            case 'settingsLoaded':
                if (window.smartBoxApp && data && data.MwlSettings) {
                    // Set the default query period in the date selector
                    const dateRangeSelect = document.getElementById('mwlDateRange');
                    if (dateRangeSelect && data.MwlSettings.DefaultQueryPeriod) {
                        dateRangeSelect.value = data.MwlSettings.DefaultQueryPeriod;
                        // Trigger change event to update UI if needed
                        dateRangeSelect.dispatchEvent(new Event('change'));
                    }
                }
                break;
                
            default:
                console.log('SmartBoxTouchApp: Unknown message type:', type);
        }
    });
}

// Global function to receive messages from C#
window.receiveMessage = function(message) {
    console.log('Received message from C#:', message);
    
    if (!window.smartBoxApp) {
        console.warn('App not initialized yet, queuing message');
        return;
    }
    
    // Handle different message types
    switch (message.action || message.type) {
        case 'exportComplete':
            if (window.smartBoxApp.onExportComplete && message.data && message.data.captureIds) {
                window.smartBoxApp.onExportComplete(message.data.captureIds);
            }
            break;
        case 'error':
            console.error('Error from C#:', message.message);
            if (window.smartBoxApp.dialogManager) {
                window.smartBoxApp.dialogManager.dismiss();
                window.smartBoxApp.dialogManager.error(message.message);
            }
            break;
        case 'success':
            console.log('Success from C#:', message.message);
            break;
        case 'showSettings':
            console.log('Settings data received from C#');
            // For now, just log the settings - you can implement a settings UI here
            if (message.config) {
                console.log('Current settings:', message.config);
                alert('Settings-Seite wird noch implementiert.\n\nAktuelle Einstellungen wurden in der Konsole ausgegeben.');
            }
            break;
        default:
            console.log('Unknown message type:', message.action || message.type);
    }
};

// Initialize app when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('SmartBoxTouchApp: DOM loaded, creating app instance...');
    window.smartBoxApp = new SmartBoxTouchApp();
    
    // Register special handlers immediately
    if (window.actionHandler) {
        window.actionHandler.registerSpecialHandler('exportcaptures', () => {
            window.smartBoxApp.onExportRequested();
        });
        
        window.actionHandler.registerSpecialHandler('exitapp', () => {
            window.smartBoxApp.onExitRequested();
        });
        console.log('[ActionSystem] Special handlers registered immediately');
    }
    
    // Migrate to new action system after a delay
    setTimeout(() => {
        console.log('[ActionSystem] Starting button migration...');
        if (window.simpleActionHandler) {
            // Liste der Buttons die umgestellt werden sollen
            const buttonsToMigrate = ['settingsButton', 'exitButton', 'exportButton'];
            
            buttonsToMigrate.forEach(buttonId => {
                const button = document.getElementById(buttonId);
                if (button) {
                    // Clone node to remove all event listeners
                    const newButton = button.cloneNode(true);
                    button.parentNode.replaceChild(newButton, button);
                    console.log(`[ActionSystem] Removed old listeners from ${buttonId}`);
                    
                    // Bind mit neuem System
                    window.simpleActionHandler.bindButton(buttonId);
                    console.log(`[ActionSystem] ${buttonId} migrated to new system`);
                }
            });
            
            // Special handlers already registered above
            
            console.log('[ActionSystem] Migration complete!');
        } else {
            console.error('[ActionSystem] simpleActionHandler not available!');
        }
    }, 1000);
});

console.log('SmartBoxTouchApp: Script loaded');