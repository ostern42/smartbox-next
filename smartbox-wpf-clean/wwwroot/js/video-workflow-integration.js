/**
 * Video Workflow Integration for SmartBox Next
 * Orchestrates video capture, editing, and export workflow
 */
class VideoWorkflowManager {
    constructor(app) {
        this.app = app;
        this.currentMode = 'capture'; // capture, review, edit
        this.videoEditor = null;
        this.annotationTools = null;
        this.currentVideoBlob = null;
        this.currentPatient = null;
        this.capturedVideos = [];
        this.isEditorActive = false;
        
        this.init();
    }
    
    init() {
        this.setupUI();
        this.setupEventListeners();
        this.loadComponents();
        console.log('VideoWorkflowManager: Initialized');
    }
    
    setupUI() {
        // Add video editing mode button to recording interface
        const recordingControls = document.querySelector('.recording-controls');
        if (recordingControls) {
            const editButton = document.createElement('button');
            editButton.className = 'control-button';
            editButton.id = 'openVideoEditorBtn';
            editButton.innerHTML = `
                <i class="ms-Icon ms-Icon--Edit"></i>
                <span>Video bearbeiten</span>
            `;
            editButton.style.display = 'none'; // Hidden until video is captured
            recordingControls.appendChild(editButton);
        }
        
        // Create video editor container
        const editorContainer = document.createElement('div');
        editorContainer.id = 'videoEditorContainer';
        editorContainer.className = 'video-editor-container hidden';
        editorContainer.innerHTML = `
            <div class="editor-header">
                <button class="back-button" id="exitVideoEditor">
                    <i class="ms-Icon ms-Icon--Back"></i>
                    <span>Zurück zur Aufnahme</span>
                </button>
                <div class="editor-title">
                    <h2>Video-Editor</h2>
                    <span class="patient-info" id="editorPatientInfo">-</span>
                </div>
            </div>
            <div id="videoEditorWorkspace"></div>
        `;
        document.querySelector('.main-content').appendChild(editorContainer);
        
        // Create annotation overlay for live capture
        const captureArea = document.getElementById('captureArea');
        if (captureArea) {
            const annotationOverlay = document.createElement('div');
            annotationOverlay.id = 'liveAnnotationOverlay';
            annotationOverlay.className = 'live-annotation-overlay hidden';
            annotationOverlay.innerHTML = `<div id="liveAnnotationTools"></div>`;
            captureArea.appendChild(annotationOverlay);
        }
        
        // Add annotation toggle to capture controls
        const captureControls = document.querySelector('.recording-controls');
        if (captureControls) {
            const annotateButton = document.createElement('button');
            annotateButton.className = 'control-button';
            annotateButton.id = 'toggleLiveAnnotations';
            annotateButton.innerHTML = `
                <i class="ms-Icon ms-Icon--EditNote"></i>
                <span>Annotieren</span>
            `;
            captureControls.insertBefore(annotateButton, captureControls.lastElementChild);
        }
    }
    
    loadComponents() {
        // Components are loaded on demand to save resources
        console.log('VideoWorkflowManager: Components will be loaded on demand');
    }
    
    setupEventListeners() {
        // Video capture events
        document.addEventListener('videoCaptured', (e) => this.onVideoCaptured(e));
        document.addEventListener('startVideoRecording', (e) => this.onRecordingStarted(e));
        document.addEventListener('stopVideoRecording', (e) => this.onRecordingStopped(e));
        
        // Editor button
        const editButton = document.getElementById('openVideoEditorBtn');
        if (editButton) {
            editButton.addEventListener('click', () => this.openVideoEditor());
        }
        
        // Exit editor button
        const exitButton = document.getElementById('exitVideoEditor');
        if (exitButton) {
            exitButton.addEventListener('click', () => this.closeVideoEditor());
        }
        
        // Live annotation toggle
        const annotateButton = document.getElementById('toggleLiveAnnotations');
        if (annotateButton) {
            annotateButton.addEventListener('click', () => this.toggleLiveAnnotations());
        }
        
        // Handle export events
        document.addEventListener('exportCaptures', (e) => this.onExportRequested(e));
        
        // Critical moment marking during recording
        const markCriticalBtn = document.getElementById('markCriticalMomentButton');
        if (markCriticalBtn) {
            markCriticalBtn.addEventListener('click', () => this.markCriticalMoment());
        }
    }
    
    // Video capture workflow
    
    onRecordingStarted(event) {
        console.log('VideoWorkflowManager: Recording started');
        
        // Enable critical moment marking
        const markBtn = document.getElementById('markCriticalMomentButton');
        if (markBtn) {
            markBtn.classList.remove('hidden');
        }
        
        // Store recording metadata
        this.currentRecording = {
            startTime: Date.now(),
            criticalMoments: [],
            annotations: []
        };
    }
    
    onRecordingStopped(event) {
        console.log('VideoWorkflowManager: Recording stopped');
        
        // Hide critical moment button
        const markBtn = document.getElementById('markCriticalMomentButton');
        if (markBtn) {
            markBtn.classList.add('hidden');
        }
    }
    
    onVideoCaptured(event) {
        const { captureId, videoBlob, duration, patient } = event.detail;
        
        console.log('VideoWorkflowManager: Video captured', { captureId, duration });
        
        // Store captured video
        const videoData = {
            id: captureId,
            blob: videoBlob,
            duration: duration,
            patient: patient,
            timestamp: Date.now(),
            criticalMoments: this.currentRecording?.criticalMoments || [],
            annotations: this.currentRecording?.annotations || []
        };
        
        this.capturedVideos.push(videoData);
        this.currentVideoBlob = videoBlob;
        this.currentPatient = patient;
        
        // Show edit button
        const editButton = document.getElementById('openVideoEditorBtn');
        if (editButton) {
            editButton.style.display = 'flex';
        }
        
        // Update thumbnail with video icon
        this.addVideoThumbnail(videoData);
    }
    
    markCriticalMoment() {
        if (!this.app.isRecording || !this.currentRecording) return;
        
        const currentTime = (Date.now() - this.currentRecording.startTime) / 1000;
        
        console.log('VideoWorkflowManager: Critical moment marked at', currentTime);
        
        this.currentRecording.criticalMoments.push({
            time: currentTime,
            timestamp: Date.now(),
            description: 'Kritischer Moment'
        });
        
        // Visual feedback
        this.showCriticalMomentFeedback();
        
        // Update timeline if active
        if (this.app.timelineManager) {
            this.app.timelineManager.markCritical();
        }
    }
    
    showCriticalMomentFeedback() {
        const feedback = document.createElement('div');
        feedback.className = 'critical-moment-feedback';
        feedback.innerHTML = `
            <i class="ms-Icon ms-Icon--Flag"></i>
            <span>Kritischer Moment markiert</span>
        `;
        
        document.body.appendChild(feedback);
        
        setTimeout(() => {
            feedback.classList.add('visible');
        }, 10);
        
        setTimeout(() => {
            feedback.classList.remove('visible');
            setTimeout(() => feedback.remove(), 300);
        }, 2000);
    }
    
    // Video editor workflow
    
    async openVideoEditor() {
        if (!this.currentVideoBlob) {
            console.warn('VideoWorkflowManager: No video to edit');
            return;
        }
        
        console.log('VideoWorkflowManager: Opening video editor');
        
        // Load editor components if not already loaded
        if (!this.videoEditor) {
            await this.loadVideoEditor();
        }
        
        // Update patient info
        const patientInfo = document.getElementById('editorPatientInfo');
        if (patientInfo && this.currentPatient) {
            patientInfo.textContent = `${this.currentPatient.name} | ${this.currentPatient.id}`;
        }
        
        // Hide recording mode, show editor
        document.getElementById('recordingMode').classList.add('hidden');
        document.getElementById('videoEditorContainer').classList.remove('hidden');
        
        // Load video into editor
        const metadata = {
            duration: this.capturedVideos[this.capturedVideos.length - 1].duration,
            criticalMoments: this.capturedVideos[this.capturedVideos.length - 1].criticalMoments,
            annotations: this.capturedVideos[this.capturedVideos.length - 1].annotations
        };
        
        this.videoEditor.loadVideo(this.currentVideoBlob, this.currentPatient, metadata);
        
        this.isEditorActive = true;
        this.currentMode = 'edit';
    }
    
    closeVideoEditor() {
        console.log('VideoWorkflowManager: Closing video editor');
        
        // Save any unsaved changes
        if (this.videoEditor && this.videoEditor.isModified) {
            if (confirm('Ungespeicherte Änderungen vorhanden. Trotzdem schließen?')) {
                this.videoEditor.saveSession();
            } else {
                return;
            }
        }
        
        // Show recording mode, hide editor
        document.getElementById('videoEditorContainer').classList.add('hidden');
        document.getElementById('recordingMode').classList.remove('hidden');
        
        this.isEditorActive = false;
        this.currentMode = 'capture';
    }
    
    async loadVideoEditor() {
        console.log('VideoWorkflowManager: Loading video editor components');
        
        // Create video editor instance
        this.videoEditor = new MedicalVideoEditor('videoEditorWorkspace');
        
        // Set up editor event listeners
        this.videoEditor.on('exportDicom', (e) => this.onExportDicom(e));
        this.videoEditor.on('exportSegments', (e) => this.onExportSegments(e));
        this.videoEditor.on('save', (e) => this.onEditorSave(e));
        
        // Create annotation tools for editor
        const annotationContainer = document.createElement('div');
        annotationContainer.id = 'editorAnnotationTools';
        annotationContainer.className = 'editor-annotation-tools';
        document.getElementById('videoEditorWorkspace').appendChild(annotationContainer);
        
        this.annotationTools = new MedicalAnnotationTools('editorAnnotationTools', {
            enableVoiceNotes: true,
            measurementScale: this.calculateMeasurementScale()
        });
        
        // Connect annotation tools to video player
        this.annotationTools.on('save', (e) => {
            const annotations = e.detail.annotations;
            this.videoEditor.videoPlayer.annotations = annotations;
        });
    }
    
    // Live annotation during capture
    
    toggleLiveAnnotations() {
        const overlay = document.getElementById('liveAnnotationOverlay');
        const button = document.getElementById('toggleLiveAnnotations');
        
        if (!overlay || !button) return;
        
        overlay.classList.toggle('hidden');
        button.classList.toggle('active');
        
        if (!overlay.classList.contains('hidden') && !this.liveAnnotationTools) {
            // Initialize live annotation tools
            this.liveAnnotationTools = new MedicalAnnotationTools('liveAnnotationTools', {
                enableVoiceNotes: false, // Disabled during recording
                colors: ['#FF0000', '#00FF00', '#0000FF', '#FFFF00'],
                defaultLineWidth: 5 // Thicker for live annotations
            });
            
            // Make canvas transparent for overlay
            const canvas = this.liveAnnotationTools.canvas;
            if (canvas) {
                canvas.style.pointerEvents = 'auto';
                canvas.style.background = 'transparent';
            }
        }
    }
    
    // Export workflow
    
    onExportRequested(event) {
        console.log('VideoWorkflowManager: Export requested');
        
        // Check if we're in editor mode
        if (this.isEditorActive && this.videoEditor) {
            // Use editor's export functionality
            this.videoEditor.exportToDicom();
        } else {
            // Export captured videos directly
            this.exportCapturedVideos();
        }
    }
    
    async exportCapturedVideos() {
        const videos = this.capturedVideos.filter(v => v.patient);
        
        if (videos.length === 0) {
            alert('Keine Videos zum Exportieren vorhanden');
            return;
        }
        
        console.log(`VideoWorkflowManager: Exporting ${videos.length} videos`);
        
        // Show export progress
        this.showExportProgress();
        
        try {
            for (const video of videos) {
                await this.exportVideoToDicom(video);
            }
            
            this.hideExportProgress();
            this.showExportSuccess(videos.length);
            
            // Clear exported videos
            this.capturedVideos = [];
            this.updateThumbnails();
            
        } catch (error) {
            console.error('VideoWorkflowManager: Export failed', error);
            this.hideExportProgress();
            alert('Fehler beim Export: ' + error.message);
        }
    }
    
    async exportVideoToDicom(videoData) {
        // Convert video blob to DICOM format
        const dicomData = {
            action: 'convertVideoToDicom',
            videoBlob: await this.blobToBase64(videoData.blob),
            patient: videoData.patient,
            metadata: {
                captureTime: new Date(videoData.timestamp),
                duration: videoData.duration,
                criticalMoments: videoData.criticalMoments,
                annotations: videoData.annotations,
                modality: 'XC', // External Camera
                studyDescription: 'Medical Video Capture'
            }
        };
        
        // Send to C# backend
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(dicomData));
        }
    }
    
    onExportDicom(event) {
        const exportData = event.detail;
        console.log('VideoWorkflowManager: Exporting edited video to DICOM', exportData);
        
        // Process segments and create DICOM
        this.exportEditedVideoToDicom(exportData);
    }
    
    onExportSegments(event) {
        const { segments } = event.detail;
        console.log(`VideoWorkflowManager: Exporting ${segments.length} segments`);
        
        // Export individual segments
        segments.forEach((segment, index) => {
            this.exportSegmentToFile(segment, index);
        });
    }
    
    async exportEditedVideoToDicom(exportData) {
        // Show progress
        this.showExportProgress('Exportiere bearbeitetes Video zu DICOM...');
        
        try {
            // Prepare DICOM metadata
            const dicomData = {
                action: 'exportEditedVideoToDicom',
                segments: exportData.segments,
                videoBlob: await this.blobToBase64(this.currentVideoBlob),
                patient: exportData.patientInfo,
                metadata: {
                    editInfo: exportData.editInfo,
                    criticalMoments: exportData.criticalMoments,
                    annotations: exportData.annotations,
                    modality: 'XC',
                    studyDescription: 'Edited Medical Video'
                }
            };
            
            // Send to backend
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(JSON.stringify(dicomData));
            }
            
            setTimeout(() => {
                this.hideExportProgress();
                this.showExportSuccess(1, 'Bearbeitetes Video');
            }, 2000);
            
        } catch (error) {
            console.error('VideoWorkflowManager: DICOM export failed', error);
            this.hideExportProgress();
            alert('Fehler beim DICOM Export: ' + error.message);
        }
    }
    
    async exportSegmentToFile(segment, index) {
        // Create a downloadable file for the segment
        const segmentBlob = await this.extractSegmentBlob(segment);
        const fileName = `segment_${index + 1}_${segment.startTime.toFixed(1)}-${segment.endTime.toFixed(1)}s.webm`;
        
        // Create download link
        const url = URL.createObjectURL(segmentBlob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        
        // Clean up
        URL.revokeObjectURL(url);
    }
    
    async extractSegmentBlob(segment) {
        // This would use FFmpeg or similar to extract the segment
        // For now, return the full video (in production, implement proper extraction)
        console.warn('VideoWorkflowManager: Segment extraction not fully implemented');
        return this.currentVideoBlob;
    }
    
    // Thumbnail management
    
    addVideoThumbnail(videoData) {
        // Add thumbnail to the strip
        const thumbnailScroll = document.getElementById('thumbnailScroll');
        if (!thumbnailScroll) return;
        
        // Find the add button
        const addButton = thumbnailScroll.querySelector('.add-new');
        
        // Create video thumbnail
        const thumbnail = document.createElement('div');
        thumbnail.className = 'thumbnail video-thumbnail';
        thumbnail.dataset.index = this.capturedVideos.length - 1;
        thumbnail.dataset.videoId = videoData.id;
        
        // Create video element for thumbnail
        const video = document.createElement('video');
        video.src = URL.createObjectURL(videoData.blob);
        video.currentTime = 1; // Seek to 1 second for thumbnail
        video.muted = true;
        
        video.onloadeddata = () => {
            // Create canvas for thumbnail
            const canvas = document.createElement('canvas');
            canvas.width = 120;
            canvas.height = 90;
            const ctx = canvas.getContext('2d');
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            
            // Use canvas as thumbnail
            thumbnail.innerHTML = `
                <img src="${canvas.toDataURL()}" alt="Video ${this.capturedVideos.length}">
                <div class="thumbnail-type">
                    <i class="ms-Icon ms-Icon--Video"></i>
                </div>
                <div class="thumbnail-number">#${this.capturedVideos.length}</div>
                <div class="thumbnail-duration">${this.formatDuration(videoData.duration)}</div>
            `;
            
            // Clean up
            URL.revokeObjectURL(video.src);
        };
        
        // Insert before add button
        thumbnailScroll.insertBefore(thumbnail, addButton);
        
        // Update export count
        this.updateExportCount();
        
        // Add click handler
        thumbnail.addEventListener('click', () => {
            this.previewVideo(videoData);
        });
    }
    
    previewVideo(videoData) {
        console.log('VideoWorkflowManager: Previewing video', videoData.id);
        
        // Set current video and open editor in preview mode
        this.currentVideoBlob = videoData.blob;
        this.currentPatient = videoData.patient;
        this.openVideoEditor();
    }
    
    updateThumbnails() {
        // Refresh thumbnail strip after export
        const thumbnails = document.querySelectorAll('.thumbnail.video-thumbnail');
        thumbnails.forEach(thumb => thumb.remove());
        
        // Re-add remaining videos
        this.capturedVideos.forEach(video => {
            this.addVideoThumbnail(video);
        });
    }
    
    updateExportCount() {
        const exportCount = document.getElementById('exportCount');
        if (exportCount) {
            const videoCount = this.capturedVideos.filter(v => v.patient).length;
            exportCount.textContent = `(${videoCount} Videos)`;
        }
    }
    
    // Progress indicators
    
    showExportProgress(message = 'Exportiere Videos...') {
        const overlay = document.createElement('div');
        overlay.id = 'exportProgressOverlay';
        overlay.className = 'export-progress-overlay';
        overlay.innerHTML = `
            <div class="export-progress-content">
                <div class="loading-spinner"></div>
                <h3>${message}</h3>
                <div class="progress-bar">
                    <div class="progress-fill" id="exportProgressBar"></div>
                </div>
                <p id="exportProgressText">Vorbereitung...</p>
            </div>
        `;
        
        document.body.appendChild(overlay);
    }
    
    updateExportProgress(current, total) {
        const progressBar = document.getElementById('exportProgressBar');
        const progressText = document.getElementById('exportProgressText');
        
        if (progressBar && progressText) {
            const percentage = (current / total) * 100;
            progressBar.style.width = percentage + '%';
            progressText.textContent = `Video ${current} von ${total}`;
        }
    }
    
    hideExportProgress() {
        const overlay = document.getElementById('exportProgressOverlay');
        if (overlay) {
            overlay.remove();
        }
    }
    
    showExportSuccess(count, type = 'Videos') {
        const success = document.createElement('div');
        success.className = 'export-success';
        success.innerHTML = `
            <i class="ms-Icon ms-Icon--CheckMark"></i>
            <h3>Export erfolgreich</h3>
            <p>${count} ${type} erfolgreich an PACS gesendet</p>
        `;
        
        document.body.appendChild(success);
        
        setTimeout(() => {
            success.classList.add('visible');
        }, 10);
        
        setTimeout(() => {
            success.classList.remove('visible');
            setTimeout(() => success.remove(), 300);
        }, 3000);
    }
    
    // Utility functions
    
    calculateMeasurementScale() {
        // Calculate pixels per mm based on video resolution and known sensor size
        // This would need calibration for accurate measurements
        const defaultPPM = 3.77; // Example: ~3.77 pixels per mm at standard distance
        return defaultPPM;
    }
    
    formatDuration(seconds) {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${mins}:${secs.toString().padStart(2, '0')}`;
    }
    
    async blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onloadend = () => {
                const base64 = reader.result.split(',')[1];
                resolve(base64);
            };
            reader.onerror = reject;
            reader.readAsDataURL(blob);
        });
    }
    
    // Cleanup
    
    dispose() {
        if (this.videoEditor) {
            this.videoEditor.destroy();
        }
        
        if (this.annotationTools) {
            // Annotation tools cleanup
        }
        
        if (this.liveAnnotationTools) {
            // Live annotation cleanup
        }
        
        // Clear video URLs
        this.capturedVideos.forEach(video => {
            if (video.blob) {
                URL.revokeObjectURL(URL.createObjectURL(video.blob));
            }
        });
        
        console.log('VideoWorkflowManager: Disposed');
    }
}

// Additional CSS for workflow integration
const workflowStyles = document.createElement('style');
workflowStyles.textContent = `
/* Video editor container */
.video-editor-container {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: #f3f2f1;
    z-index: 50;
}

.video-editor-container.hidden {
    display: none;
}

.editor-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 15px 20px;
    background: white;
    border-bottom: 1px solid #e1dfdd;
}

.editor-title {
    display: flex;
    align-items: center;
    gap: 15px;
}

.editor-title h2 {
    margin: 0;
    font-size: 20px;
}

.patient-info {
    color: #605e5c;
    font-size: 14px;
}

#videoEditorWorkspace {
    height: calc(100% - 70px);
}

/* Live annotation overlay */
.live-annotation-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 10;
}

.live-annotation-overlay.hidden {
    display: none;
}

#liveAnnotationTools {
    width: 100%;
    height: 100%;
}

#liveAnnotationTools .annotation-canvas {
    pointer-events: auto;
}

#liveAnnotationTools .tool-palette {
    top: auto;
    bottom: 20px;
    right: 20px;
    max-height: 400px;
}

/* Video thumbnail enhancements */
.video-thumbnail {
    position: relative;
}

.thumbnail-duration {
    position: absolute;
    bottom: 5px;
    right: 5px;
    background: rgba(0, 0, 0, 0.8);
    color: white;
    padding: 2px 6px;
    border-radius: 2px;
    font-size: 11px;
}

/* Critical moment feedback */
.critical-moment-feedback {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: #d83b01;
    color: white;
    padding: 20px 40px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    gap: 15px;
    font-size: 18px;
    font-weight: 500;
    opacity: 0;
    transition: opacity 0.3s;
    z-index: 100;
}

.critical-moment-feedback.visible {
    opacity: 1;
}

.critical-moment-feedback i {
    font-size: 24px;
}

/* Export progress overlay */
.export-progress-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.8);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 200;
}

.export-progress-content {
    background: white;
    padding: 40px;
    border-radius: 8px;
    text-align: center;
    min-width: 400px;
}

.export-progress-content h3 {
    margin: 20px 0;
}

.progress-bar {
    width: 100%;
    height: 6px;
    background: #e1dfdd;
    border-radius: 3px;
    margin: 20px 0;
    overflow: hidden;
}

.progress-fill {
    height: 100%;
    background: #0078d4;
    width: 0;
    transition: width 0.3s;
}

#exportProgressText {
    margin: 0;
    color: #605e5c;
}

/* Export success notification */
.export-success {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%) scale(0.9);
    background: white;
    padding: 40px;
    border-radius: 8px;
    text-align: center;
    box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
    opacity: 0;
    transition: all 0.3s;
    z-index: 201;
}

.export-success.visible {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
}

.export-success i {
    font-size: 48px;
    color: #107c10;
}

.export-success h3 {
    margin: 20px 0 10px 0;
}

.export-success p {
    margin: 0;
    color: #605e5c;
}

/* Button states */
#toggleLiveAnnotations.active {
    background: rgba(0, 120, 212, 0.8);
}

#openVideoEditorBtn {
    background: rgba(0, 120, 212, 0.8);
    border-color: #0078d4;
}

#openVideoEditorBtn:hover {
    background: rgba(0, 120, 212, 0.9);
}

/* Editor annotation tools */
.editor-annotation-tools {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    pointer-events: none;
    z-index: 5;
}

.editor-annotation-tools .annotation-workspace {
    pointer-events: none;
}

.editor-annotation-tools .annotation-canvas {
    pointer-events: auto;
}
`;

document.head.appendChild(workflowStyles);

// Export for use in main app
window.VideoWorkflowManager = VideoWorkflowManager;