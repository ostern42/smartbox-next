// SmartBox Settings Manager - Refactored with Automatic Mapping
// 
// NAMING CONVENTION: 
// - HTML IDs: kebab-case (e.g., pacs-server-host)
// - JavaScript/JSON communication with C#: PascalCase (e.g., ServerHost)
// - This matches the C# model properties and config.json format
//
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
            this.testPacsButton.addEventListener('click', (e) => {
                e.preventDefault();
                this.testPacsConnection();
            });
        }

        // Test MWL button
        if (this.testMwlButton) {
            this.testMwlButton.addEventListener('click', (e) => {
                e.preventDefault();
                this.testMwlConnection();
            });
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

    // Convert HTML ID to C# property path
    // Example: storage-photos-path → Storage.PhotosPath
    htmlIdToPropertyPath(htmlId) {
        const parts = htmlId.split('-');
        if (parts.length < 2) return null;
        
        const section = this.capitalizeSection(parts[0]);
        const property = parts.slice(1)
            .map((p, i) => i === 0 ? this.capitalize(p) : this.capitalize(p))
            .join('');
        
        return { section, property };
    }

    // Special handling for section names
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
            const config = this.gatherFormData();
            console.log('Saving config:', config);
            
            // Send to C# host
            this.sendToHost('savesettings', config);
            
            // Show success notification
            this.showNotification('Settings saved successfully!', 'success');
        } catch (error) {
            console.error('Failed to save settings:', error);
            this.showNotification('Failed to save settings: ' + error.message, 'error');
        }
    }

    gatherFormData() {
        const config = {
            Storage: {},
            Pacs: {},
            MwlSettings: {},
            Video: {},
            Application: {}
        };

        // Get all input elements
        const inputs = this.form.querySelectorAll('input, select');
        
        inputs.forEach(input => {
            const mapping = this.htmlIdToPropertyPath(input.id);
            if (!mapping) {
                console.warn('No mapping for input:', input.id);
                return;
            }

            const { section, property } = mapping;
            
            // Get value based on input type
            let value;
            if (input.type === 'checkbox') {
                value = input.checked;
            } else if (input.type === 'number') {
                value = parseInt(input.value) || 0;
            } else if (input.classList.contains('numeric-input')) {
                // Handle numeric inputs that are type="text" (for keyboard)
                value = parseInt(input.value) || 0;
            } else {
                value = input.value;
            }

            // Special handling for certain fields
            if (property === 'EnablePacs') {
                // PACS doesn't have EnablePacs, it's determined by ServerHost
                return;
            }

            // Set value in config
            if (config[section]) {
                config[section][property] = value;
            }
        });

        console.log('Gathered config:', config);
        return config;
    }

    populateForm(config) {
        if (!config) return;
        
        console.log('Populating form with config:', config);
        this.config = config;

        // Populate all inputs
        const inputs = this.form.querySelectorAll('input, select');
        
        inputs.forEach(input => {
            const mapping = this.htmlIdToPropertyPath(input.id);
            if (!mapping) return;

            const { section, property } = mapping;
            const sectionConfig = config[section];
            
            if (!sectionConfig) return;

            // Special handling for EnablePacs
            if (input.id === 'pacs-enable-pacs') {
                input.checked = !!(sectionConfig.ServerHost);
                return;
            }

            const value = sectionConfig[property];
            if (value === undefined) return;

            // Set value based on input type
            if (input.type === 'checkbox') {
                input.checked = value;
            } else {
                input.value = value;
            }
        });
    }

    async testPacsConnection() {
        try {
            this.showNotification('Testing PACS connection...', 'info');
            
            // Gather current PACS settings - using PascalCase to match C# models
            const serverHostInput = document.getElementById('pacs-server-host');
            const serverHost = serverHostInput ? serverHostInput.value : '';
            
            if (!serverHost) {
                this.showNotification('Server host is empty. Please enter a server address.', 'error');
                return;
            }
            
            const pacsConfig = {
                ServerHost: serverHost,
                ServerPort: parseInt(document.getElementById('pacs-server-port').value) || 104,
                CalledAeTitle: document.getElementById('pacs-called-ae-title').value,
                CallingAeTitle: document.getElementById('pacs-calling-ae-title').value,
                Timeout: parseInt(document.getElementById('pacs-timeout').value) || 30
            };

            console.log('Testing PACS with config:', pacsConfig);
            
            // Show detailed test info
            this.showNotification(
                `Testing PACS connection to ${pacsConfig.ServerHost}:${pacsConfig.ServerPort} ` +
                `(AET: ${pacsConfig.CallingAeTitle} → ${pacsConfig.CalledAeTitle})`, 
                'info'
            );
            
            this.sendToHost('testpacsconnection', pacsConfig);
        } catch (error) {
            console.error('Failed to test PACS connection:', error);
            this.showNotification('Failed to test connection: ' + error.message, 'error');
        }
    }

    async testMwlConnection() {
        try {
            // Gather current MWL settings - using PascalCase to match C# models
            const serverHostInput = document.getElementById('mwlsettings-mwl-server-host');
            const serverHost = serverHostInput ? serverHostInput.value : '';
            
            if (!serverHost) {
                this.showNotification('MWL server host is empty. Please enter a server address.', 'error');
                return;
            }
            
            const mwlConfig = {
                EnableWorklist: document.getElementById('mwlsettings-enable-worklist').checked,
                MwlServerHost: serverHost,
                MwlServerPort: parseInt(document.getElementById('mwlsettings-mwl-server-port').value) || 105,
                MwlServerAET: document.getElementById('mwlsettings-mwl-server-aet').value,
                LocalAET: 'SMARTBOX' // This matches C# property name
            };

            console.log('Testing MWL with config:', mwlConfig);
            
            // Show detailed test info
            let testMsg = `Testing MWL connection to ${mwlConfig.MwlServerHost}:${mwlConfig.MwlServerPort} ` +
                `(AET: ${mwlConfig.LocalAET} → ${mwlConfig.MwlServerAET})`;
            
            // Warn if using non-standard port
            if (mwlConfig.MwlServerPort !== 105 && mwlConfig.MwlServerPort !== 104) {
                testMsg += ` ⚠️ Non-standard port`;
            }
            
            this.showNotification(testMsg, 'info');
            
            this.sendToHost('testmwlconnection', mwlConfig);
        } catch (error) {
            console.error('Failed to test MWL connection:', error);
            this.showNotification('Failed to test connection: ' + error.message, 'error');
        }
    }

    browseFolder(button) {
        const fieldId = button.dataset.for;
        console.log('Browse folder for field:', fieldId);
        
        this.sendToHost('browsefolder', { fieldId });
    }

    sendToHost(action, data) {
        const message = {
            action: action,
            data: data
        };
        
        console.log('Sending to host:', message);
        
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(JSON.stringify(message));
        } else {
            console.warn('WebView2 not available, using mock response');
            // Mock response for testing
            setTimeout(() => {
                this.handleHostMessage({
                    data: {
                        action: action + 'Response',
                        success: true,
                        data: {}
                    }
                });
            }, 100);
        }
    }

    handleHostMessage(event) {
        const message = event.data;
        console.log('Received from host:', message);

        switch (message.action) {
            case 'settingsLoaded':
                this.populateForm(message.data);
                break;
                
            case 'settingsSaved':
                if (message.success) {
                    this.showNotification('Settings saved successfully!', 'success');
                } else {
                    const errorMsg = message.error || message.message || 'Unknown error';
                    this.showNotification('Failed to save settings: ' + errorMsg, 'error');
                    console.error('Save settings error:', message);
                }
                break;
                
            case 'folderSelected':
                console.log('Folder selected message:', message);
                if (message.fieldId && message.path) {
                    const input = document.getElementById(message.fieldId);
                    console.log('Looking for input with ID:', message.fieldId, 'Found:', !!input);
                    if (input) {
                        input.value = message.path;
                        console.log('Set input value to:', message.path);
                        // Trigger change event so form knows value changed
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                } else {
                    console.warn('Missing fieldId or path in folderSelected message:', message);
                }
                break;
                
            case 'pacsTestResult':
                if (message.success) {
                    this.showNotification('PACS connection successful!', 'success');
                } else {
                    this.showNotification('PACS connection failed: ' + (message.error || 'Unknown error'), 'error');
                }
                break;
                
            case 'mwlTestResult':
                if (message.success) {
                    const count = message.data?.itemCount || 0;
                    this.showNotification(`MWL connection successful! Found ${count} worklist items.`, 'success');
                } else {
                    this.showNotification('MWL connection failed: ' + (message.error || 'Unknown error'), 'error');
                }
                break;
                
            case 'testConnectionResult':
                const testData = message.data || {};
                if (testData.success) {
                    const details = testData.details ? ` (${testData.details.host}:${testData.details.port})` : '';
                    this.showNotification(testData.message + details, 'success');
                } else {
                    this.showNotification(testData.message || 'Connection test failed', 'error');
                }
                break;
        }
    }

    showNotification(message, type = 'info') {
        console.log(`[${type.toUpperCase()}] ${message}`);
        
        // Remove any existing notification
        const existing = document.querySelector('.notification');
        if (existing) {
            existing.remove();
        }

        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            notification.remove();
        }, 5000);
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.settingsManager = new SettingsManager();
});