// SmartBox Action System v2 - Schrittweise Implementation
// Sicherer Start mit mehr Debugging

console.log('[Actions-v2] Loading...');

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
    
    // Settings
    SAVE_SETTINGS: 'savesettings',
    TEST_PACS: 'testpacsconnection',
    TEST_MWL: 'testmwlconnection',
    BROWSE_FOLDER: 'browsefolder'
};

// Globale sendToHost Funktion
function sendToHost(action, data = {}) {
    console.log(`[Actions-v2] sendToHost called: ${action}`, data);
    
    if (!window.chrome || !window.chrome.webview) {
        console.warn('[Actions-v2] WebView2 not available');
        return;
    }
    
    try {
        const message = JSON.stringify({
            type: action,
            data: data
        });
        console.log('[Actions-v2] Sending message:', message);
        window.chrome.webview.postMessage(message);
    } catch (error) {
        console.error('[Actions-v2] Error sending message:', error);
    }
}

// Einfacher Action Handler - erstmal ohne automatisches Binding
class SimpleActionHandler {
    constructor() {
        console.log('[SimpleActionHandler] Constructor called');
        this.specialHandlers = {};
        
        // Warte bis DOM wirklich bereit ist
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => {
                console.log('[SimpleActionHandler] DOM loaded, initializing...');
                this.init();
            });
        } else {
            console.log('[SimpleActionHandler] DOM already loaded, initializing...');
            this.init();
        }
    }
    
    init() {
        console.log('[SimpleActionHandler] Init called');
        this.logExistingButtons();
        
        // Automatisches Binding für alle Buttons mit data-action
        // this.bindAllButtons(); // NOCH NICHT - wird von app.js gesteuert
    }
    
    logExistingButtons() {
        const buttonsWithAction = document.querySelectorAll('[data-action]');
        console.log(`[SimpleActionHandler] Found ${buttonsWithAction.length} buttons with data-action:`);
        buttonsWithAction.forEach(button => {
            console.log(`  - ${button.id || button.className}: data-action="${button.dataset.action}"`);
        });
    }
    
    // Manuelles Binding für einzelne Buttons (zum Testen)
    bindButton(buttonId) {
        const button = document.getElementById(buttonId);
        if (!button) {
            console.error(`[SimpleActionHandler] Button ${buttonId} not found`);
            return;
        }
        
        const action = button.dataset.action;
        if (!action) {
            console.error(`[SimpleActionHandler] Button ${buttonId} has no data-action`);
            return;
        }
        
        console.log(`[SimpleActionHandler] Binding button ${buttonId} to action ${action}`);
        
        button.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log(`[SimpleActionHandler] Button ${buttonId} clicked, action: ${action}`);
            
            // Check for special handler
            if (this.specialHandlers[action]) {
                console.log(`[SimpleActionHandler] Using special handler for ${action}`);
                this.specialHandlers[action](button);
            } else {
                console.log(`[SimpleActionHandler] Using default handler for ${action}`);
                sendToHost(action, {});
            }
        });
    }
    
    // Registriere speziellen Handler
    registerSpecialHandler(action, handler) {
        console.log(`[SimpleActionHandler] Registering special handler for ${action}`);
        this.specialHandlers[action] = handler;
    }
}

// Globale Instanz erstellen
console.log('[Actions-v2] Creating global handler instance...');
window.simpleActionHandler = new SimpleActionHandler();

console.log('[Actions-v2] Script loaded successfully');

// Export für andere Module
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { ACTIONS, sendToHost, SimpleActionHandler };
}