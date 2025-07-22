/**
 * SmartBox Medical Message Safety Layer
 * Prevents silent failures in C# <-> JavaScript communication
 * 
 * MEDICAL SAFETY: All messages are validated before sending
 */

class MedicalMessageSafety {
    static VALID_ACTIONS = [
        // System controls (normalized to lowercase for comparison)
        'opensettings', 'exitapp', 'openlogs',
        
        // Patient workflow  
        'queryworklist', 'refreshworklist', 'selectworklistitem', 'getworklistcachestatus',
        
        // Medical capture
        'photocaptured', 'savephoto', 'videorecorded', 'savevideo', 'capturehighres',
        
        // Yuan capture
        'connectyuan', 'disconnectyuan', 'getyuaninputs', 'selectyuaninput', 'setactivesource',
        'getunifiedstatus', 'getcapturestats',
        
        // DICOM & PACS
        'exportdicom', 'exportcaptures', 'sendtopacs', 'testpacsconnection', 'testmwlconnection',
        
        // Settings
        'requestconfig', 'updateconfig', 'getsettings', 'savesettings', 'browsefolder',
        
        // Capture management
        'deletecapture', 'webcaminitialized', 'cameraanalysis',
        
        // Diagnostics
        'log', 'testwebview', 'ping', 'exit', 'close'
    ];
    
    /**
     * Validates and normalizes message action
     * @param {string} action - The action to validate
     * @returns {string|null} - Normalized action or null if invalid
     */
    static validateAction(action) {
        if (!action || typeof action !== 'string') {
            console.error('[MEDICAL SAFETY] Invalid action type:', typeof action);
            return null;
        }
        
        const normalized = action.toLowerCase();
        
        if (!this.VALID_ACTIONS.includes(normalized)) {
            console.error('[MEDICAL SAFETY] Unknown action:', action);
            console.warn('[MEDICAL SAFETY] Valid actions are:', this.VALID_ACTIONS);
            return null;
        }
        
        return normalized;
    }
    
    /**
     * Validates patient information for medical safety
     * @param {Object} patient - Patient data to validate
     * @returns {boolean} - True if valid
     */
    static validatePatientInfo(patient) {
        if (!patient) return true; // Optional parameter
        
        if (typeof patient !== 'object') {
            console.error('[MEDICAL SAFETY] Patient info must be object, got:', typeof patient);
            return false;
        }
        
        // Critical patient data validation
        if (patient.id && typeof patient.id !== 'string') {
            console.error('[MEDICAL SAFETY] Patient ID must be string');
            return false;
        }
        
        if (patient.name && typeof patient.name !== 'string') {
            console.error('[MEDICAL SAFETY] Patient name must be string');
            return false;
        }
        
        if (patient.gender && !['M', 'F', 'O'].includes(patient.gender)) {
            console.error('[MEDICAL SAFETY] Invalid gender value:', patient.gender);
            return false;
        }
        
        return true;
    }
    
    /**
     * Validates capture data for medical integrity
     * @param {Object} capture - Capture data to validate
     * @returns {boolean} - True if valid
     */
    static validateCaptureData(capture) {
        if (!capture || typeof capture !== 'object') {
            console.error('[MEDICAL SAFETY] Capture data must be object');
            return false;
        }
        
        if (!capture.id || typeof capture.id !== 'string') {
            console.error('[MEDICAL SAFETY] Capture must have string ID');
            return false;
        }
        
        if (!['photo', 'video'].includes(capture.type)) {
            console.error('[MEDICAL SAFETY] Invalid capture type:', capture.type);
            return false;
        }
        
        // Must have either data or filePath
        if (!capture.data && !capture.filePath) {
            console.error('[MEDICAL SAFETY] Capture must have either data or filePath');
            return false;
        }
        
        return true;
    }
    
    /**
     * Safe message sender with validation
     * @param {Object} message - Message to send
     * @returns {boolean} - True if sent successfully
     */
    static sendMessage(message) {
        try {
            // Validate message structure
            if (!message || typeof message !== 'object') {
                console.error('[MEDICAL SAFETY] Message must be object');
                return false;
            }
            
            // Validate and normalize action
            const validatedAction = this.validateAction(message.action);
            if (!validatedAction) {
                return false;
            }
            
            // Create safe message copy
            const safeMessage = {
                action: validatedAction,
                data: message.data || {},
                timestamp: new Date().toISOString()
            };
            
            // Validate patient data if present
            if (safeMessage.data.patient && !this.validatePatientInfo(safeMessage.data.patient)) {
                return false;
            }
            
            // Validate captures if present
            if (safeMessage.data.captures) {
                if (!Array.isArray(safeMessage.data.captures)) {
                    console.error('[MEDICAL SAFETY] Captures must be array');
                    return false;
                }
                
                for (const capture of safeMessage.data.captures) {
                    if (!this.validateCaptureData(capture)) {
                        return false;
                    }
                }
            }
            
            // Send the validated message
            const messageJson = JSON.stringify(safeMessage);
            
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(messageJson);
                console.log('[MEDICAL SAFETY] Message sent safely:', validatedAction);
                return true;
            } else {
                console.error('[MEDICAL SAFETY] WebView2 not available');
                return false;
            }
            
        } catch (error) {
            console.error('[MEDICAL SAFETY] Message send failed:', error);
            return false;
        }
    }
    
    /**
     * Legacy sendMessage function for backward compatibility
     * @param {string} action - Action to send
     * @param {Object} data - Data to send
     * @returns {boolean} - True if sent successfully
     */
    static sendSimpleMessage(action, data = {}) {
        return this.sendMessage({
            action: action,
            data: data
        });
    }
}

// Export for global use
window.MedicalMessageSafety = MedicalMessageSafety;

// Override global sendMessage for safety
window.sendMessage = function(message) {
    return MedicalMessageSafety.sendMessage(message);
};

// Legacy support for simple messages
window.sendSimpleMessage = function(action, data) {
    return MedicalMessageSafety.sendSimpleMessage(action, data);
};

console.log('[MEDICAL SAFETY] Message safety layer initialized');