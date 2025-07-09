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
        this.saveButton = document.getElementById('saveButton');
        this.testPacsButton = document.getElementById('test-pacs');
        this.testMwlButton = document.getElementById('test-mwl');
        
        // Form elements
        this.form = document.getElementById('settingsForm');
        
        console.log('Elements initialized:', {
            backButton: !!this.backButton,
            saveButton: !!this.saveButton,
            testPacsButton: !!this.testPacsButton,
            testMwlButton: !!this.testMwlButton
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

        // Also add receiveMessage function for C# to call
        window.receiveMessage = (message) => {
            console.log('Received message from C#:', message);
            this.handleHostMessage({ data: message });
        };
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
            this.sendToHost('getsettings', {});
        } catch (error) {
            console.error('Failed to load settings:', error);
        }
    }

    async saveSettings() {
        try {
            const settings = {
                Storage: {},
                Pacs: {},
                MwlSettings: {},
                Video: {},
                Application: {}
            };

            // Get all form inputs
            const inputs = this.form.querySelectorAll('input, select');
            
            inputs.forEach(input => {
                const parts = input.id.split('-');
                if (parts.length >= 2) {
                    let section = parts[0];
                    let field = parts.slice(1).join('');
                    
                    // Map section names to config structure
                    const sectionMap = {
                        'storage': 'Storage',
                        'pacs': 'Pacs',
                        'mwl': 'MwlSettings',
                        'video': 'Video',
                        'application': 'Application'
                    };
                    
                    const configSection = sectionMap[section];
                    if (!configSection) return;
                    
                    // Convert field names to PascalCase
                    field = field.charAt(0).toUpperCase() + field.slice(1);
                    
                    // Get value based on input type
                    let value;
                    if (input.type === 'checkbox') {
                        value = input.checked;
                    } else if (input.type === 'number') {
                        value = parseInt(input.value) || 0;
                    } else {
                        value = input.value;
                    }
                    
                    settings[configSection][field] = value;
                }
            });

            console.log('Saving settings:', settings);
            
            // Send to C# host
            this.sendToHost('savesettings', settings);
            this.showNotification('Settings saved successfully', 'success');
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

        // Send test request to C# host (lowercase!)
        this.sendToHost('testpacsconnection', pacsSettings);
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

        // Send test request to C# host (lowercase!)
        this.sendToHost('testmwlconnection', mwlSettings);
    }

    browseFolder(button) {
        const inputId = button.dataset.for;
        this.sendToHost('browsefolder', { 
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
        console.log('Handling host message:', message);
        
        switch (message.action || message.type) {
            case 'settingsLoaded':
                this.populateForm(message.data);
                break;
                
            case 'settingsSaved':
                console.log('Settings saved successfully');
                this.showNotification('Settings saved successfully', 'success');
                break;
                
            case 'pacsTestResult':
                if (this.testPacsButton) {
                    this.testPacsButton.disabled = false;
                    if (message.data && message.data.success) {
                        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                        this.testPacsButton.style.background = '#107c10';
                        this.showNotification('PACS connection successful', 'success');
                    } else {
                        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                        this.testPacsButton.style.background = '#d13438';
                        const error = message.data ? message.data.error : 'Unknown error';
                        this.showNotification(`Connection failed: ${error}`, 'error');
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
                    if (message.data && message.data.success) {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                        this.testMwlButton.style.background = '#107c10';
                        this.showNotification(`MWL Connected! Found ${message.data.count || 0} worklist items.`, 'success');
                    } else {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                        this.testMwlButton.style.background = '#d13438';
                        const error = message.data ? message.data.error : 'Unknown error';
                        this.showNotification(`MWL Connection failed: ${error}`, 'error');
                    }
                    
                    setTimeout(() => {
                        this.testMwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                        this.testMwlButton.style.background = '';
                    }, 3000);
                }
                break;
                
            case 'folderSelected':
                if (message.data && message.data.inputId && message.data.path) {
                    const input = document.getElementById(message.data.inputId);
                    if (input) {
                        input.value = message.data.path;
                    }
                }
                break;
                
            case 'success':
                this.showNotification(message.message || 'Operation successful', 'success');
                break;
                
            case 'error':
                this.showNotification(message.message || 'Operation failed', 'error');
                break;
                
            default:
                console.log(`Unknown message from host: ${message.action || message.type}`);
        }
    }

    populateForm(config) {
        this.config = config;
        console.log('Populating form with config:', config);
        
        // Map config sections to form prefixes
        const sectionMap = {
            'Storage': 'storage',
            'Pacs': 'pacs',
            'MwlSettings': 'mwl',
            'Video': 'video',
            'Application': 'application'
        };
        
        // Populate all form fields
        Object.keys(config).forEach(section => {
            const formPrefix = sectionMap[section];
            if (!formPrefix) return;
            
            Object.keys(config[section]).forEach(field => {
                // Convert PascalCase to kebab-case for form IDs
                const fieldId = field.replace(/([A-Z])/g, (match, p1, offset) => 
                    offset > 0 ? '-' + p1.toLowerCase() : p1.toLowerCase()
                );
                
                const inputId = `${formPrefix}-${fieldId}`;
                const input = document.getElementById(inputId);
                
                if (input) {
                    if (input.type === 'checkbox') {
                        input.checked = config[section][field];
                    } else {
                        input.value = config[section][field];
                    }
                    console.log(`Set ${inputId} to ${config[section][field]}`);
                } else {
                    console.warn(`Input not found: ${inputId}`);
                }
            });
        });
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Settings page loaded');
    window.settingsManager = new SettingsManager();
});