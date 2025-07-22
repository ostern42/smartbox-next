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
        
        // Special handling for settings form - create hierarchical structure
        if (formId === 'settingsForm') {
            return this.collectSettingsFormData(form);
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
    
    collectSettingsFormData(form) {
        console.log('[ActionHandler] Collecting settings with hierarchical structure');
        
        const config = {
            Storage: {},
            Pacs: {},
            MwlSettings: {},
            Video: {},
            Application: {}
        };
        
        const inputs = form.querySelectorAll('input, select, textarea');
        
        inputs.forEach(input => {
            if (!input.id) return;
            
            const mapping = this.htmlIdToPropertyPath(input.id);
            if (!mapping) return;
            
            const { section, property } = mapping;
            
            // Get value based on input type
            let value;
            if (input.type === 'checkbox') {
                value = input.checked;
            } else if (input.type === 'number' || input.classList.contains('numeric-input')) {
                value = parseInt(input.value) || 0;
            } else {
                value = input.value;
            }
            
            // Set value in hierarchical config
            if (config[section]) {
                config[section][property] = value;
            }
        });
        
        console.log('[ActionHandler] Settings config collected:', config);
        return config;
    }
    
    // Convert HTML ID to C# property path (same as settings.js)
    htmlIdToPropertyPath(htmlId) {
        const parts = htmlId.split('-');
        if (parts.length < 2) return null;
        
        const section = this.capitalizeSection(parts[0]);
        const property = parts.slice(1)
            .map((p, i) => i === 0 ? this.capitalize(p) : this.capitalize(p))
            .join('');
        
        return { section, property };
    }
    
    // Special handling for section names (same as settings.js)
    capitalizeSection(section) {
        const sectionMap = {
            'storage': 'Storage',
            'pacs': 'Pacs',
            'mwlsettings': 'MwlSettings',
            'video': 'Video',
            'application': 'Application'
        };
        return sectionMap[section.toLowerCase()] || section;
    }
    
    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
    }
    
    needsConfirmation(action) {
        return ['deletecapture', 'clearpatient'].includes(action);
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