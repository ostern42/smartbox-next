// SmartBox Action System - Finale Version
// Automatisches Binding für alle Buttons mit data-action

console.log('[ActionSystem] Loading final version...');

// Zentrale Action-Definitionen
const ACTIONS = {
    // App Control
    OPEN_SETTINGS: 'opensettings',
    EXIT_APP: 'exitapp',
    
    // Capture Actions
    CAPTURE_PHOTO: 'capturephoto',
    CAPTURE_VIDEO: 'capturevideo',
    STOP_VIDEO: 'stopvideo',
    
    // Export/Send
    EXPORT_CAPTURES: 'exportcaptures',
    
    // MWL/Worklist
    LOAD_MWL: 'loadmwl',
    REFRESH_MWL: 'refreshmwl',
    SELECT_PATIENT: 'selectpatient',
    
    // Settings
    SAVE_SETTINGS: 'savesettings',
    TEST_PACS: 'testpacsconnection',
    TEST_MWL: 'testmwlconnection',
    BROWSE_FOLDER: 'browsefolder',
    
    // Patient Management
    CREATE_EMERGENCY_PATIENT: 'createemergencypatient',
    CLEAR_PATIENT: 'clearpatient',
    
    // Navigation
    SWITCH_MODE: 'switchmode',
    GO_BACK: 'goback'
};

// Globale sendToHost Funktion
function sendToHost(action, data = {}) {
    if (!window.chrome?.webview) {
        console.warn('[ActionSystem] WebView2 not available');
        return;
    }
    
    console.log(`[ActionSystem] Sending: ${action}`, data);
    
    window.chrome.webview.postMessage(JSON.stringify({
        type: action,
        data: data
    }));
}

// Action Handler mit automatischem Binding
class ActionHandler {
    constructor() {
        this.specialHandlers = {};
        this.setupGlobalHandlers();
        console.log('[ActionHandler] Initialized');
    }
    
    setupGlobalHandlers() {
        // Globaler Click-Handler für alle Elemente mit data-action
        document.addEventListener('click', (e) => {
            const element = e.target.closest('[data-action]');
            if (!element) return;
            
            e.preventDefault();
            e.stopPropagation();
            
            const action = element.dataset.action;
            const actionData = this.collectActionData(element);
            
            console.log(`[ActionHandler] ${element.id || element.className || 'element'} → ${action}`);
            
            // Check for special handler
            if (this.specialHandlers[action]) {
                this.specialHandlers[action](element, actionData);
            } else if (this.needsConfirmation(action)) {
                this.confirmAndExecute(action, actionData, element);
            } else {
                sendToHost(action, actionData);
            }
        });
        
        // Support für Enter-Taste
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                const element = e.target;
                if (element.hasAttribute('data-action')) {
                    element.click();
                }
            }
        });
        
        console.log('[ActionHandler] Global handlers installed');
    }
    
    collectActionData(element) {
        const data = {};
        
        // Sammle alle data-* Attribute
        for (const key in element.dataset) {
            if (key !== 'action') {
                data[key] = element.dataset[key];
            }
        }
        
        // Spezialbehandlung für Formulare
        if (element.dataset.collectForm) {
            const formData = this.collectFormData(element.dataset.collectForm);
            Object.assign(data, formData);
        }
        
        return data;
    }
    
    collectFormData(formId) {
        const form = document.getElementById(formId) || document.querySelector(formId);
        if (!form) {
            console.warn(`[ActionHandler] Form ${formId} not found`);
            return {};
        }
        
        const data = {};
        const inputs = form.querySelectorAll('input, select, textarea');
        
        inputs.forEach(input => {
            if (input.name || input.id) {
                const key = input.name || input.id;
                if (input.type === 'checkbox') {
                    data[key] = input.checked;
                } else if (input.type === 'radio') {
                    if (input.checked) {
                        data[key] = input.value;
                    }
                } else {
                    data[key] = input.value;
                }
            }
        });
        
        return data;
    }
    
    needsConfirmation(action) {
        return ['exitapp', 'deletecapture', 'clearpatient'].includes(action);
    }
    
    confirmAndExecute(action, data, element) {
        // Nutze Touch Dialogs wenn verfügbar
        if (window.TouchDialogs) {
            const messages = {
                'exitapp': 'Möchten Sie SmartBox wirklich beenden?',
                'deletecapture': 'Diese Aufnahme wirklich löschen?',
                'clearpatient': 'Patientendaten wirklich löschen?'
            };
            
            window.TouchDialogs.showExitConfirmation(
                messages[action] || 'Sind Sie sicher?',
                () => sendToHost(action, data)
            );
        } else {
            // Fallback
            if (confirm('Sind Sie sicher?')) {
                sendToHost(action, data);
            }
        }
    }
    
    registerSpecialHandler(action, handler) {
        this.specialHandlers[action] = handler;
        console.log(`[ActionHandler] Special handler registered: ${action}`);
    }
    
    // Debug-Funktion
    listAllActions() {
        const elements = document.querySelectorAll('[data-action]');
        console.log(`[ActionHandler] Found ${elements.length} actionable elements:`);
        elements.forEach(el => {
            console.log(`  - ${el.id || el.className || el.tagName}: ${el.dataset.action}`);
        });
    }
}

// Globale Instanz erstellen
let actionHandler = null;

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        actionHandler = new ActionHandler();
        window.actionHandler = actionHandler;
        
        // Debug: Liste alle Actions
        setTimeout(() => actionHandler.listAllActions(), 100);
    });
} else {
    actionHandler = new ActionHandler();
    window.actionHandler = actionHandler;
    
    // Debug: Liste alle Actions
    setTimeout(() => actionHandler.listAllActions(), 100);
}

console.log('[ActionSystem] Final version loaded successfully');

// Export für andere Module
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ACTIONS, sendToHost, ActionHandler };
}