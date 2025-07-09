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
            // Create field mapping from HTML IDs to C# property names
            const fieldMapping = {
                // Storage fields
                'photos-path': { section: 'Storage', field: 'PhotosPath' },
                'videos-path': { section: 'Storage', field: 'VideosPath' },
                'dicom-path': { section: 'Storage', field: 'DicomPath' },
                'queue-path': { section: 'Storage', field: 'QueuePath' },
                'temp-path': { section: 'Storage', field: 'TempPath' },
                'max-storage-days': { section: 'Storage', field: 'MaxStorageDays' },
                'enable-auto-cleanup': { section: 'Storage', field: 'EnableAutoCleanup' },
                
                // PACS fields
                'pacs-serverHost': { section: 'Pacs', field: 'ServerHost' },
                'pacs-serverPort': { section: 'Pacs', field: 'ServerPort' },
                'pacs-calledAeTitle': { section: 'Pacs', field: 'CalledAeTitle' },
                'pacs-callingAeTitle': { section: 'Pacs', field: 'CallingAeTitle' },
                'pacs-timeout': { section: 'Pacs', field: 'Timeout' },
                'pacs-enableTls': { section: 'Pacs', field: 'EnableTls' },
                'pacs-maxRetries': { section: 'Pacs', field: 'MaxRetries' },
                'pacs-retryDelay': { section: 'Pacs', field: 'RetryDelay' },
                
                // MWL fields
                'mwl-enable': { section: 'MwlSettings', field: 'EnableWorklist' },
                'mwl-server-ip': { section: 'MwlSettings', field: 'MwlServerHost' },
                'mwl-server-port': { section: 'MwlSettings', field: 'MwlServerPort' },
                'mwl-server-ae': { section: 'MwlSettings', field: 'MwlServerAET' },
                'mwl-cache-hours': { section: 'MwlSettings', field: 'CacheExpiryHours' },
                // These fields don't exist in MwlConfig, skip them
                // 'mwl-local-ae': { section: 'MwlSettings', field: 'LocalAET' },
                // 'mwl-modality': { section: 'MwlSettings', field: 'Modality' },
                // 'mwl-station-name': { section: 'MwlSettings', field: 'StationName' },
                // 'mwl-auto-refresh': { section: 'MwlSettings', field: 'AutoRefresh' },
                
                // Video fields
                'preferred-width': { section: 'Video', field: 'DefaultWidth' },
                'preferred-height': { section: 'Video', field: 'DefaultHeight' },
                'preferred-fps': { section: 'Video', field: 'DefaultFrameRate' },
                'video-quality': { section: 'Video', field: 'DefaultQuality' },
                'enable-hardware-acceleration': { section: 'Video', field: 'EnableHardwareAcceleration' },
                'preferred-camera': { section: 'Video', field: 'PreferredCamera' },
                
                // Application fields
                'language': { section: 'Application', field: 'Language' },
                'theme': { section: 'Application', field: 'Theme' },
                'enable-touch-keyboard': { section: 'Application', field: 'EnableTouchKeyboard' },
                'enable-debug-mode': { section: 'Application', field: 'EnableDebugMode' },
                'auto-start-capture': { section: 'Application', field: 'AutoStartCapture' },
                'web-server-port': { section: 'Application', field: 'WebServerPort' },
                'enable-remote-access': { section: 'Application', field: 'EnableRemoteAccess' },
                'hide-exit-button': { section: 'Application', field: 'HideExitButton' },
                'enable-emergency-templates': { section: 'Application', field: 'EnableEmergencyTemplates' }
            };

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
                const mapping = fieldMapping[input.id];
                if (!mapping) {
                    console.warn('No mapping found for field:', input.id);
                    return;
                }
                
                // Get value based on input type
                let value;
                if (input.type === 'checkbox') {
                    value = input.checked;
                } else if (input.type === 'number') {
                    value = parseInt(input.value) || 0;
                } else {
                    value = input.value;
                }
                
                settings[mapping.section][mapping.field] = value;
            });
            
            // Special handling for video resolution
            const width = document.getElementById('preferred-width')?.value || '1920';
            const height = document.getElementById('preferred-height')?.value || '1080';
            settings.Video.DefaultResolution = `${width}x${height}`;
            // Remove the individual width/height fields that don't exist in C#
            delete settings.Video.DefaultWidth;
            delete settings.Video.DefaultHeight;

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
        
        // Create reverse field mapping from C# property names to HTML IDs
        const reverseFieldMapping = {
            // Storage fields
            'Storage.PhotosPath': 'photos-path',
            'Storage.VideosPath': 'videos-path',
            'Storage.DicomPath': 'dicom-path',
            'Storage.QueuePath': 'queue-path',
            'Storage.TempPath': 'temp-path',
            'Storage.MaxStorageDays': 'max-storage-days',
            'Storage.EnableAutoCleanup': 'enable-auto-cleanup',
            
            // PACS fields
            'Pacs.ServerHost': 'pacs-serverHost',
            'Pacs.ServerPort': 'pacs-serverPort',
            'Pacs.CalledAeTitle': 'pacs-calledAeTitle',
            'Pacs.CallingAeTitle': 'pacs-callingAeTitle',
            'Pacs.Timeout': 'pacs-timeout',
            'Pacs.EnableTls': 'pacs-enableTls',
            'Pacs.MaxRetries': 'pacs-maxRetries',
            'Pacs.RetryDelay': 'pacs-retryDelay',
            
            // MWL fields
            'MwlSettings.EnableWorklist': 'mwl-enable',
            'MwlSettings.MwlServerHost': 'mwl-server-ip',
            'MwlSettings.MwlServerPort': 'mwl-server-port',
            'MwlSettings.MwlServerAET': 'mwl-server-ae',
            'MwlSettings.CacheExpiryHours': 'mwl-cache-hours',
            'MwlSettings.AutoRefreshSeconds': 'mwl-auto-refresh-seconds',
            'MwlSettings.ShowEmergencyFirst': 'mwl-show-emergency-first',
            
            // Video fields
            'Video.DefaultFrameRate': 'preferred-fps',
            'Video.DefaultQuality': 'video-quality',
            'Video.EnableHardwareAcceleration': 'enable-hardware-acceleration',
            'Video.PreferredCamera': 'preferred-camera',
            
            // Application fields
            'Application.Language': 'language',
            'Application.Theme': 'theme',
            'Application.EnableTouchKeyboard': 'enable-touch-keyboard',
            'Application.EnableDebugMode': 'enable-debug-mode',
            'Application.AutoStartCapture': 'auto-start-capture',
            'Application.WebServerPort': 'web-server-port',
            'Application.EnableRemoteAccess': 'enable-remote-access',
            'Application.HideExitButton': 'hide-exit-button',
            'Application.EnableEmergencyTemplates': 'enable-emergency-templates'
        };
        
        // Populate all form fields
        Object.keys(config).forEach(section => {
            if (!config[section]) return;
            
            Object.keys(config[section]).forEach(field => {
                const key = `${section}.${field}`;
                const inputId = reverseFieldMapping[key];
                
                if (inputId) {
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
                } else {
                    console.log(`No mapping for ${key}`);
                }
            });
        });
        
        // Special handling for video resolution
        if (config.Video && config.Video.DefaultResolution) {
            const [width, height] = config.Video.DefaultResolution.split('x');
            const widthInput = document.getElementById('preferred-width');
            const heightInput = document.getElementById('preferred-height');
            if (widthInput) widthInput.value = width || '1920';
            if (heightInput) heightInput.value = height || '1080';
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    console.log('Settings page loaded');
    window.settingsManager = new SettingsManager();
});