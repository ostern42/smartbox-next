// Settings JavaScript
class SettingsManager {
    constructor() {
        this.config = {};
        this.originalConfig = {};
        this.initializeElements();
        this.attachEventListeners();
        this.loadConfiguration();
    }

    initializeElements() {
        // Navigation
        this.navItems = document.querySelectorAll('.nav-item');
        this.sections = document.querySelectorAll('.settings-section');
        
        // Buttons
        this.backButton = document.getElementById('backButton');
        this.saveButton = document.getElementById('saveButton');
        this.testPacsButton = document.getElementById('test-pacs');
        
        // Storage fields
        this.photosPath = document.getElementById('photos-path');
        this.videosPath = document.getElementById('videos-path');
        this.dicomPath = document.getElementById('dicom-path');
        this.tempPath = document.getElementById('temp-path');
        
        // PACS fields
        this.pacsEnabled = document.getElementById('pacs-enabled');
        this.serverAe = document.getElementById('server-ae');
        this.serverIp = document.getElementById('server-ip');
        this.serverPort = document.getElementById('server-port');
        this.localAe = document.getElementById('local-ae');
        this.localPort = document.getElementById('local-port');
        
        // Video fields
        this.preferredWidth = document.getElementById('preferred-width');
        this.preferredHeight = document.getElementById('preferred-height');
        this.preferredFps = document.getElementById('preferred-fps');
        this.videoFormat = document.getElementById('video-format');
        this.videoBitrate = document.getElementById('video-bitrate');
        
        // Application fields
        this.autoStart = document.getElementById('auto-start');
        this.fullscreen = document.getElementById('fullscreen');
        this.touchMode = document.getElementById('touch-mode');
        this.debugMode = document.getElementById('debug-mode');
        this.language = document.getElementById('language');
    }

    attachEventListeners() {
        // Navigation
        this.navItems.forEach(item => {
            item.addEventListener('click', (e) => this.switchSection(e.currentTarget));
        });
        
        // Buttons
        this.backButton.addEventListener('click', () => this.close());
        this.saveButton.addEventListener('click', () => this.saveConfiguration());
        this.testPacsButton.addEventListener('click', () => this.testPacsConnection());
        
        // Browse buttons
        document.querySelectorAll('.browse-button').forEach(btn => {
            btn.addEventListener('click', (e) => this.browseFolder(e.currentTarget));
        });
        
        // Listen for messages from C# host
        window.addEventListener('message', (e) => this.handleHostMessage(e));
        
        // Mark inputs for keyboard
        this.markInputsForKeyboard();
    }

    markInputsForKeyboard() {
        // Mark numeric inputs
        const numericInputs = [
            'server-ip', 'server-port', 'local-port',
            'preferred-width', 'preferred-height', 'preferred-fps', 'video-bitrate'
        ];
        
        numericInputs.forEach(id => {
            const input = document.getElementById(id);
            if (input) {
                input.classList.add('numeric-input');
                input.classList.add('use-keyboard');
            }
        });
        
        // Mark all text inputs for keyboard
        document.querySelectorAll('input[type="text"], input[type="number"]').forEach(input => {
            input.classList.add('use-keyboard');
        });
    }

    switchSection(navItem) {
        // Update navigation
        this.navItems.forEach(item => item.classList.remove('active'));
        navItem.classList.add('active');
        
        // Update sections
        const targetSection = navItem.dataset.section;
        this.sections.forEach(section => {
            section.classList.remove('active');
            if (section.id === `${targetSection}-section`) {
                section.classList.add('active');
            }
        });
    }

    loadConfiguration() {
        // Check if we're in an iframe
        if (window.parent !== window) {
            // Request config from parent window first
            window.parent.postMessage({ action: 'requestConfig' }, '*');
        } else {
            // Request configuration from C# host
            this.sendToHost('requestConfig', {});
        }
    }

    displayConfiguration(config) {
        this.config = config;
        this.originalConfig = JSON.parse(JSON.stringify(config));
        
        // Storage
        this.photosPath.value = config.storage?.photosPath || './Data/Photos';
        this.videosPath.value = config.storage?.videosPath || './Data/Videos';
        this.dicomPath.value = config.storage?.dicomPath || './Data/DICOM';
        this.tempPath.value = config.storage?.tempPath || './Data/Temp';
        
        // PACS
        this.pacsEnabled.checked = config.pacs?.enabled || false;
        this.serverAe.value = config.pacs?.remoteAeTitle || 'PACS_SERVER';
        this.serverIp.value = config.pacs?.remoteHost || '192.168.1.100';
        this.serverPort.value = config.pacs?.remotePort || 104;
        this.localAe.value = config.pacs?.aeTitle || 'SMARTBOX';
        this.localPort.value = config.pacs?.localPort || 0;
        
        // Video
        this.preferredWidth.value = config.video?.preferredWidth || 1920;
        this.preferredHeight.value = config.video?.preferredHeight || 1080;
        this.preferredFps.value = config.video?.preferredFps || 30;
        this.videoFormat.value = config.video?.videoFormat || 'webm';
        this.videoBitrate.value = config.video?.videoBitrateMbps || 5;
        
        // Application
        this.autoStart.checked = config.application?.autoStartCapture || false;
        this.fullscreen.checked = config.application?.startFullscreen !== false;
        this.touchMode.checked = config.application?.touchMode !== false;
        this.debugMode.checked = config.application?.showDebugInfo || false;
        this.language.value = config.application?.language || 'en-US';
    }

    collectConfiguration() {
        return {
            storage: {
                photosPath: this.photosPath.value,
                videosPath: this.videosPath.value,
                dicomPath: this.dicomPath.value,
                tempPath: this.tempPath.value
            },
            pacs: {
                aeTitle: this.localAe.value,
                remoteAeTitle: this.serverAe.value,
                remoteHost: this.serverIp.value,
                remotePort: parseInt(this.serverPort.value),
                localPort: parseInt(this.localPort.value),
                useTls: false
            },
            video: {
                preferredWidth: parseInt(this.preferredWidth.value),
                preferredHeight: parseInt(this.preferredHeight.value),
                preferredFps: parseInt(this.preferredFps.value),
                videoFormat: this.videoFormat.value,
                videoBitrateMbps: parseInt(this.videoBitrate.value)
            },
            application: {
                autoStartCapture: this.autoStart.checked,
                startFullscreen: this.fullscreen.checked,
                showDebugInfo: this.debugMode.checked,
                language: this.language.value
            }
        };
    }

    saveConfiguration() {
        const config = this.collectConfiguration();
        this.sendToHost('saveConfig', config);
        
        // Show save animation
        this.saveButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Saved!</span>';
        setTimeout(() => {
            this.saveButton.innerHTML = '<i class="ms-Icon ms-Icon--Save"></i><span>Save</span>';
        }, 2000);
    }

    testPacsConnection() {
        const pacsConfig = {
            ServerAeTitle: this.serverAe.value,
            ServerIp: this.serverIp.value,
            ServerPort: parseInt(this.serverPort.value),
            LocalAeTitle: this.localAe.value,
            LocalPort: parseInt(this.localPort.value)
        };
        
        this.testPacsButton.disabled = true;
        this.testPacsButton.innerHTML = '<i class="ms-Icon ms-Icon--Sync"></i><span>Testing...</span>';
        
        this.sendToHost('testPacs', pacsConfig);
    }

    browseFolder(button) {
        const targetInputId = button.dataset.for;
        this.sendToHost('browseFolder', { 
            inputId: targetInputId,
            currentPath: document.getElementById(targetInputId).value 
        });
    }

    close() {
        // Check if there are unsaved changes
        const currentConfig = this.collectConfiguration();
        const hasChanges = JSON.stringify(currentConfig) !== JSON.stringify(this.originalConfig);
        
        if (hasChanges) {
            if (confirm('You have unsaved changes. Do you want to save before closing?')) {
                this.saveConfiguration();
            }
        }
        
        this.sendToHost('closeSettings', {});
    }

    // Communication with C# host
    sendToHost(action, data) {
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage({
                action: action,
                data: data,
                timestamp: new Date().toISOString()
            });
        } else {
            console.log('Running in browser mode - no C# host available');
            
            // For testing in browser
            if (action === 'requestConfig') {
                setTimeout(() => {
                    this.displayConfiguration({
                        Storage: {
                            PhotosPath: './Data/Photos',
                            VideosPath: './Data/Videos',
                            DicomPath: './Data/DICOM',
                            TempPath: './Data/Temp'
                        },
                        Pacs: {
                            Enabled: false,
                            ServerAeTitle: 'PACS_SERVER',
                            ServerIp: '192.168.1.100',
                            ServerPort: 104,
                            LocalAeTitle: 'SMARTBOX',
                            LocalPort: 11113
                        },
                        Video: {
                            PreferredWidth: 1920,
                            PreferredHeight: 1080,
                            PreferredFps: 30,
                            VideoFormat: 'webm',
                            VideoBitrate: 5
                        },
                        Application: {
                            AutoStartCapture: false,
                            StartFullscreen: true,
                            TouchMode: true,
                            DebugMode: false,
                            Language: 'en'
                        }
                    });
                }, 100);
            }
        }
    }

    handleHostMessage(event) {
        const message = event.data;
        
        switch (message.action) {
            case 'configLoaded':
                this.displayConfiguration(message.data);
                break;
                
            case 'configSaved':
                this.originalConfig = JSON.parse(JSON.stringify(this.config));
                break;
                
            case 'folderSelected':
                const input = document.getElementById(message.data.inputId);
                if (input) {
                    input.value = message.data.path;
                }
                break;
                
            case 'pacsTestResult':
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
                break;
                
            default:
                console.log(`Unknown message from host: ${message.action}`);
        }
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.settingsManager = new SettingsManager();
});