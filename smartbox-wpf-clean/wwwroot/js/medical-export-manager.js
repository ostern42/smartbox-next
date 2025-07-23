/**
 * SmartBox Medical Export Manager
 * Handles video export with medical metadata and compliance
 */

class MedicalExportManager {
    constructor(config) {
        this.config = config || window.StreamingConfig;
        this.authManager = window.AuthManager;
        this.exportQueue = [];
        this.currentExport = null;
        this.exportWorker = null;
        
        // Initialize worker if available
        this.initializeWorker();
    }

    /**
     * Initialize Web Worker for export processing
     */
    initializeWorker() {
        if (typeof Worker !== 'undefined') {
            try {
                // Create inline worker for export processing
                const workerCode = `
                    self.onmessage = function(e) {
                        const { type, data } = e.data;
                        
                        switch(type) {
                            case 'progress':
                                // Report progress back
                                self.postMessage({
                                    type: 'progress',
                                    progress: data.progress
                                });
                                break;
                        }
                    };
                `;
                
                const blob = new Blob([workerCode], { type: 'application/javascript' });
                this.exportWorker = new Worker(URL.createObjectURL(blob));
                
                this.exportWorker.onmessage = (e) => {
                    this.handleWorkerMessage(e.data);
                };
            } catch (e) {
                console.warn('Web Worker not available:', e);
            }
        }
    }

    /**
     * Handle worker messages
     */
    handleWorkerMessage(data) {
        if (data.type === 'progress' && this.currentExport) {
            this.currentExport.onProgress(data.progress);
        }
    }

    /**
     * Export video range with medical metadata
     */
    async exportRange(options) {
        const exportRequest = {
            id: this.generateExportId(),
            sessionId: options.sessionId,
            ranges: options.ranges,
            format: options.format || 'mp4',
            quality: options.quality || 'high',
            includeMetadata: options.includeMetadata !== false,
            metadata: this.buildMedicalMetadata(options),
            status: 'pending',
            progress: 0,
            onProgress: options.onProgress || (() => {}),
            onComplete: options.onComplete || (() => {}),
            onError: options.onError || (() => {})
        };

        // Add to queue
        this.exportQueue.push(exportRequest);
        
        // Process queue if not already processing
        if (!this.currentExport) {
            this.processExportQueue();
        }
        
        return exportRequest.id;
    }

    /**
     * Build medical metadata for export
     */
    buildMedicalMetadata(options) {
        const user = this.authManager.getCurrentUser();
        const timestamp = new Date().toISOString();
        
        return {
            // DICOM-compatible metadata
            patientId: options.patientId || 'ANONYMOUS',
            patientName: options.patientName || 'Anonymous^Patient',
            studyDate: options.studyDate || timestamp.split('T')[0].replace(/-/g, ''),
            studyTime: timestamp.split('T')[1].replace(/[:.]/g, '').substr(0, 6),
            studyDescription: options.studyDescription || 'Medical Recording',
            seriesDescription: options.seriesDescription || 'Video Export',
            modality: options.modality || 'XC', // External Camera
            manufacturer: 'SmartBox Medical',
            institutionName: options.institutionName || user?.institution || 'Medical Center',
            operatorName: user?.displayName || 'Unknown',
            
            // Additional medical metadata
            procedureType: options.procedureType,
            bodyPart: options.bodyPart,
            laterality: options.laterality,
            viewPosition: options.viewPosition,
            clinicalNotes: options.clinicalNotes,
            
            // Export metadata
            exportDate: timestamp,
            exportedBy: user?.username,
            exportVersion: '1.0',
            compressionQuality: options.quality,
            frameRate: options.frameRate || 30,
            
            // Audit trail
            auditTrail: {
                created: timestamp,
                createdBy: user?.username,
                reason: options.exportReason || 'Medical Record',
                authorized: true
            }
        };
    }

    /**
     * Process export queue
     */
    async processExportQueue() {
        if (this.exportQueue.length === 0) {
            this.currentExport = null;
            return;
        }

        this.currentExport = this.exportQueue.shift();
        this.currentExport.status = 'processing';

        try {
            // Server-side export for better quality and compliance
            if (this.config.get('medical.serverSideExport', true)) {
                await this.serverSideExport(this.currentExport);
            } else {
                // Client-side export as fallback
                await this.clientSideExport(this.currentExport);
            }
            
            this.currentExport.status = 'completed';
            this.currentExport.onComplete(this.currentExport);
            
        } catch (error) {
            console.error('Export error:', error);
            this.currentExport.status = 'failed';
            this.currentExport.error = error.message;
            this.currentExport.onError(error);
        }

        // Process next in queue
        this.processExportQueue();
    }

    /**
     * Server-side export (recommended for medical compliance)
     */
    async serverSideExport(exportRequest) {
        const { sessionId, ranges, format, quality, metadata } = exportRequest;

        // Prepare export request
        const requestData = {
            sessionId: sessionId,
            ranges: ranges.map(range => ({
                startTime: range.in,
                endTime: range.out,
                startFrame: Math.floor(range.in * 30), // Assuming 30fps
                endFrame: Math.floor(range.out * 30)
            })),
            format: format,
            quality: quality,
            metadata: metadata,
            options: {
                preserveFrameAccuracy: true,
                includeTimecode: true,
                generateChecksum: true
            }
        };

        // Start export on server
        const response = await this.authManager.authenticatedFetch(
            `${this.config.apiUrl}/export/create`,
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(requestData)
            }
        );

        if (!response.ok) {
            throw new Error('Failed to start export');
        }

        const { exportId } = await response.json();
        exportRequest.exportId = exportId;

        // Poll for progress
        await this.pollExportProgress(exportRequest);

        // Download the exported file
        await this.downloadExport(exportRequest);
    }

    /**
     * Poll export progress
     */
    async pollExportProgress(exportRequest) {
        const pollInterval = 1000; // 1 second
        const maxPolls = 600; // 10 minutes max
        let polls = 0;

        while (polls < maxPolls) {
            const response = await this.authManager.authenticatedFetch(
                `${this.config.apiUrl}/export/status/${exportRequest.exportId}`
            );

            if (!response.ok) {
                throw new Error('Failed to check export status');
            }

            const status = await response.json();

            if (status.status === 'completed') {
                exportRequest.downloadUrl = status.downloadUrl;
                exportRequest.checksum = status.checksum;
                return;
            } else if (status.status === 'failed') {
                throw new Error(status.error || 'Export failed');
            }

            // Update progress
            if (status.progress !== undefined) {
                exportRequest.progress = status.progress;
                exportRequest.onProgress(status.progress);
            }

            // Wait before next poll
            await new Promise(resolve => setTimeout(resolve, pollInterval));
            polls++;
        }

        throw new Error('Export timeout');
    }

    /**
     * Download exported file
     */
    async downloadExport(exportRequest) {
        const response = await this.authManager.authenticatedFetch(
            exportRequest.downloadUrl,
            {
                method: 'GET'
            }
        );

        if (!response.ok) {
            throw new Error('Failed to download export');
        }

        const blob = await response.blob();
        
        // Verify checksum if provided
        if (exportRequest.checksum) {
            const verified = await this.verifyChecksum(blob, exportRequest.checksum);
            if (!verified) {
                throw new Error('Export checksum verification failed');
            }
        }

        // Generate filename with medical convention
        const filename = this.generateMedicalFilename(exportRequest);

        // Trigger download
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);

        // Log export for audit trail
        this.logExport(exportRequest, filename);
    }

    /**
     * Client-side export (fallback)
     */
    async clientSideExport(exportRequest) {
        // This is a simplified implementation
        // In production, you would use MediaRecorder API or similar
        
        const { ranges } = exportRequest;
        
        // Simulate export progress
        for (let i = 0; i <= 100; i += 10) {
            exportRequest.progress = i;
            exportRequest.onProgress(i);
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        // Create dummy export data
        const exportData = {
            metadata: exportRequest.metadata,
            ranges: ranges,
            timestamp: new Date().toISOString()
        };

        const blob = new Blob([JSON.stringify(exportData, null, 2)], {
            type: 'application/json'
        });

        const filename = this.generateMedicalFilename(exportRequest);
        
        // Trigger download
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }

    /**
     * Generate medical-compliant filename
     */
    generateMedicalFilename(exportRequest) {
        const metadata = exportRequest.metadata;
        const date = new Date();
        const dateStr = date.toISOString().split('T')[0].replace(/-/g, '');
        const timeStr = date.toTimeString().split(' ')[0].replace(/:/g, '');
        
        // Format: PatientID_StudyDate_StudyTime_Modality.ext
        const parts = [
            metadata.patientId.replace(/[^a-zA-Z0-9]/g, '_'),
            dateStr,
            timeStr,
            metadata.modality
        ];

        const basename = parts.join('_');
        const extension = exportRequest.format;
        
        return `${basename}.${extension}`;
    }

    /**
     * Verify file checksum
     */
    async verifyChecksum(blob, expectedChecksum) {
        if (!crypto.subtle) {
            console.warn('Crypto API not available, skipping checksum verification');
            return true;
        }

        try {
            const buffer = await blob.arrayBuffer();
            const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
            const hashArray = Array.from(new Uint8Array(hashBuffer));
            const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
            
            return hashHex === expectedChecksum;
        } catch (e) {
            console.error('Checksum verification error:', e);
            return false;
        }
    }

    /**
     * Log export for audit trail
     */
    async logExport(exportRequest, filename) {
        try {
            await this.authManager.authenticatedFetch(
                `${this.config.apiUrl}/audit/export`,
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        exportId: exportRequest.id,
                        filename: filename,
                        metadata: exportRequest.metadata,
                        timestamp: new Date().toISOString()
                    })
                }
            );
        } catch (e) {
            console.error('Failed to log export:', e);
        }
    }

    /**
     * Generate unique export ID
     */
    generateExportId() {
        return `export_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Get export queue status
     */
    getQueueStatus() {
        return {
            current: this.currentExport ? {
                id: this.currentExport.id,
                status: this.currentExport.status,
                progress: this.currentExport.progress
            } : null,
            queued: this.exportQueue.map(e => ({
                id: e.id,
                status: e.status
            }))
        };
    }

    /**
     * Cancel export
     */
    async cancelExport(exportId) {
        // Remove from queue if queued
        this.exportQueue = this.exportQueue.filter(e => e.id !== exportId);
        
        // Cancel current if matching
        if (this.currentExport && this.currentExport.id === exportId) {
            // Notify server to cancel
            if (this.currentExport.exportId) {
                try {
                    await this.authManager.authenticatedFetch(
                        `${this.config.apiUrl}/export/cancel/${this.currentExport.exportId}`,
                        {
                            method: 'POST'
                        }
                    );
                } catch (e) {
                    console.error('Failed to cancel export on server:', e);
                }
            }
            
            this.currentExport.status = 'cancelled';
            this.currentExport.onError(new Error('Export cancelled'));
            this.currentExport = null;
            
            // Process next in queue
            this.processExportQueue();
        }
    }
}

// Export
window.MedicalExportManager = new MedicalExportManager();