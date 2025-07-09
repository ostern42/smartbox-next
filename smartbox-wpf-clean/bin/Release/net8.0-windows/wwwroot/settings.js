// SmartBox Settings Manager - Fixed Version
class SettingsManager {
    constructor() {
        this.config = null;
        this.initializeElements();
        this.attachEventListeners();
        this.loadSettings();
    }

    initializeElements() {
        // Navigation
        this.navItems = document.querySelectorAll('.nav-item');
        this.sections = document.querySelectorAll('.settings-section');
        
        // Buttons
        this.backButton = document.getElementById('backButton');
        this.homeButton = document.getElementById('homeButton');
        this.saveButton = document.getElementById('saveButton');
        this.testPacsButton = document.getElementById('test-pacs');
        this.testMwlButton = document.getElementById('test-mwl');
        
        // Form elements
        this.form = document.getElementById('settingsForm');
        
        console.log('Elements initialized:', {
            backButton: !!this.backButton,
            homeButton: !!this.homeButton,
            saveButton: !!this.saveButton
        });
    }

    attachEventListeners() {
        // Navigation
        this.navItems.forEach(item => {
            item.addEventListener('click', (e) => {
                const section = e.currentTarget.dataset.section;
                this.showSection(section);
            });
        });

        // Back button
        if (this.backButton) {
            this.backButton.addEventListener('click', () => {
                console.log('Back button clicked');
                window.location.href = 'index.html';
            });
        }

        // Home button
        if (this.homeButton) {
            this.homeButton.addEventListener('click', () => {
                console.log('Home button clicked');
                window.location.href = 'index.html';
            });
        }

        // Save button
        if (this.saveButton) {
            this.saveButton.addEventListener('click', () => this.saveSettings());
        }

        // Test PACS button
        if (this.testPacsButton) {
            this.testPacsButton.addEventListener('click', () => this.testPacsConnection());
        }

        // Test MWL button
        if (this.testMwlButton) {
            this.testMwlButton.addEventListener('click', () => this.testMwlConnection());
        }

        // Browse folder buttons
        const browseButtons = document.querySelectorAll('.browse-button');
        browseButtons.forEach(btn => {
            btn.addEventListener('click', (e) => this.browseFolder(e.currentTarget));
        });

        // Keyboard shortcut for back (ESC key)
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                console.log('ESC key pressed');
                window.location.href = 'index.html';
            }
        });

        // Listen for messages from C# host
        window.addEventListener('message', (e) => this.handleHostMessage(e));
    }

    showSection(sectionName) {
        // Update navigation
        this.navItems.forEach(item => {
            if (item.dataset.section === sectionName) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });

        // Update sections
        this.sections.forEach(section => {
            if (section.id === `${sectionName}-section`) {
                section.classList.add('active');
            } else {
                section.classList.remove('active');
            }
        });
    }

    async loadSettings() {
        try {
            // Send request to C# host
            this.sendToHost('getSettings', {});
        } catch (error) {
            console.error('Failed to load settings:', error);
        }
    }

    async saveSettings() {
        try {
            const formData = new FormData(this.form);
            const settings = {};

            // Convert form data to nested object
            for (const [key, value] of formData.entries()) {
                const parts = key.split('-');
                if (parts.length === 2) {
                    const [section, field] = parts;
                    if (!settings[section]) {
                        settings[section] = {};
                    }
                    
                    // Convert numeric values
                    if (value && !isNaN(value)) {
                        settings[section][field] = Number(value);
                    } else if (value === 'true' || value === 'false') {
                        settings[section][field] = value === 'true';
                    } else {
                        settings[section][field] = value;
                    }
                }
            }

            // Send to C# host
            this.sendToHost('saveSettings', settings);
            
            this.showNotification('Settings saved successfully!', 'success');
        } catch (error) {
            console.error('Failed to save settings:', error);
            this.showNotification('Failed to save settings', 'error');
        }
    }

    testPacsConnection() {
        if (this.testPacsButton) {
            this.testPacsButton.disabled = true;
            this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--Sync"></i><span>Testing...</span>';
        }

        // Get PACS settings from form
        const pacsSettings = {
            serverHost: document.getElementById('pacs-serverHost').value,
            serverPort: parseInt(document.getElementById('pacs-serverPort').value),
            calledAeTitle: document.getElementById('pacs-calledAeTitle').value,
            callingAeTitle: document.getElementById('pacs-callingAeTitle').value
        };

        // Send test request to C# host
        this.sendToHost('testPacsConnection', pacsSettings);
    }

    testMwlConnection() {
        if (this.testMwlButton) {
            this.testMwlButton.disabled = true;
            this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--Sync"></i><span>Testing...</span>';
        }

        // Get MWL settings from form
        const mwlSettings = {
            serverHost: document.getElementById('mwl-server-ip').value,
            serverPort: parseInt(document.getElementById('mwl-server-port').value),
            serverAeTitle: document.getElementById('mwl-server-ae').value,
            localAeTitle: document.getElementById('mwl-local-ae').value
        };

        // Send test request to C# host
        this.sendToHost('testMwlConnection', mwlSettings);
    }

    browseFolder(button) {
        const inputId = button.dataset.for;
        this.sendToHost('browseFolder', { 
            inputId: inputId,
            currentPath: document.getElementById(inputId).value 
        });
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification ${type}`;
        notification.textContent = message;
        
        document.body.appendChild(notification);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    sendToHost(action, data) {
        if (window.chrome && window.chrome.webview) {
            const message = JSON.stringify({ action, data });
            window.chrome.webview.postMessage(message);
            console.log(`Sent to host: ${action}`, data);
        } else {
            console.warn('WebView2 bridge not available');
        }
    }

    handleHostMessage(event) {
        const message = event.data;
        
        switch (message.action) {
            case 'settingsLoaded':
                this.populateForm(message.data);
                break;
                
            case 'settingsSaved':
                console.log('Settings saved successfully');
                break;
                
            case 'pacsTestResult':
                if (this.testPacsButton) {
                    this.testPacsButton.disabled = false;
                    if (message.data.success) {
                        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                        this.testPacsButton.style.background = '#107c10';
                    } else {
                        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                        this.testPacsButton.style.background = '#d13438';
                        alert(`Connection failed: ${message.data.error}`);
                    }
                    
                    setTimeout(() => {
                        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                        this.testPacsButton.style.background = '';
                    }, 3000);
                }
                break;

            case 'mwlTestResult':
                if (this.testMwlButton) {
                    this.testMwlButton.disabled = false;
                    if (message.data.success) {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                        this.testMwlButton.style.background = '#107c10';
                        this.showNotification(`MWL Connected! Found ${message.data.count || 0} worklist items.`, 'success');
                    } else {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                        this.testMwlButton.style.background = '#d13438';
                        this.showNotification(`MWL Connection failed: ${message.data.error}`, 'error');
                    }
                    
                    setTimeout(() => {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                        this.testMwlButton.style.background = '';
                    }, 3000);
                }
                break;
                
            case 'folderSelected':
                if (message.data.inputId && message.data.path) {
                    const input = document.getElementById(message.data.inputId);
                    if (input) {
                        input.value = message.data.path;
                    }
                }
                break;
                
            default:
                console.log(`Unknown message from host: ${message.action}`);
        }
    }

    populateForm(config) {
        this.config = config;
        
        // Populate all form fields
        Object.keys(config).forEach(section => {
            Object.keys(config[section]).forEach(field => {
                const inputId = `${section.toLowerCase()}-${field.charAt(0).toLowerCase() + field.slice(1)}`;
                const input = document.getElementById(inputId);
                
                if (input) {
                    if (input.type === 'checkbox') {
                        input.checked = config[section][field];
                    } else {
                        input.value = config[section][field];
                    }
                }
            });
        });
    }
}

// Global function for receiving messages from C#
window.receiveMessage = function(message) {
    console.log('Received message from C#:', message);
    
    if (window.settingsManager) {
        window.settingsManager.handleHostMessage({ data: message });
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Settings page loaded');
    window.settingsManager = new SettingsManager();
});