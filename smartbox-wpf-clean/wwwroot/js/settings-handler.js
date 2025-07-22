// Settings Handler - Saubere Lösung für Settings-Speicherung
// Behält die komplette Logik und Bestätigungen

class SettingsHandler {
    constructor() {
        this.form = document.getElementById('settingsForm');
        this.initializeHandlers();
        this.setupMessageHandlers();
    }
    
    initializeHandlers() {
        // Registriere Handler beim Action-System
        if (window.actionHandler || window.simpleActionHandler) {
            const handler = window.actionHandler || window.simpleActionHandler;
            
            // Save Settings - mit kompletter Logik
            handler.registerSpecialHandler('savesettings', () => {
                this.handleSaveSettings();
            });
            
            // Test PACS
            handler.registerSpecialHandler('testpacsconnection', () => {
                this.handleTestPacs();
            });
            
            // Test MWL
            handler.registerSpecialHandler('testmwlconnection', () => {
                this.handleTestMwl();
            });
            
            // Browse Folder
            handler.registerSpecialHandler('browsefolder', (button) => {
                this.handleBrowseFolder(button);
            });
            
            console.log('[SettingsHandler] All handlers registered');
        }
    }
    
    setupMessageHandlers() {
        // Listen for messages from C#
        window.chrome.webview.addEventListener('message', (event) => {
            const message = event.data;
            console.log('[SettingsHandler] Received message:', message);
            console.log('[SettingsHandler] Message action:', message.action);
            console.log('[SettingsHandler] Message type:', message.type);
            
            switch (message.action || message.type) {
                case 'settingsLoaded':
                    console.log('[SettingsHandler] Settings loaded, delegating to settingsManager');
                    if (window.settingsManager && window.settingsManager.populateForm) {
                        window.settingsManager.populateForm(message.data);
                    }
                    break;
                    
                case 'settingsSaved':
                    console.log('[SettingsHandler] Settings saved successfully!');
                    this.showNotification('Settings saved successfully!', 'success');
                    break;
                    
                case 'settingsSaveError':
                    this.showNotification('Failed to save settings: ' + (message.error || 'Unknown error'), 'error');
                    break;
                    
                case 'testConnectionResult':
                    if (message.data?.success) {
                        const details = message.data.details ? ` (${message.data.details.host}:${message.data.details.port})` : '';
                        this.showNotification(message.data.message + details, 'success');
                    } else {
                        this.showNotification(message.data.message || 'Connection test failed', 'error');
                    }
                    break;
                    
                case 'pacsTestResult':
                    const pacsButton = document.getElementById('test-pacs');
                    if (pacsButton) {
                        // Cancel timeout since we got a response
                        if (pacsButton.timeoutId) {
                            clearTimeout(pacsButton.timeoutId);
                            pacsButton.timeoutId = null;
                        }
                        
                        pacsButton.disabled = false;
                        if (message.success) {
                            // Show success state on button
                            pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                            pacsButton.style.background = '#107c10';
                            pacsButton.style.color = 'white';
                            this.showNotification('PACS connection successful!', 'success');
                        } else {
                            // Show error state on button
                            pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                            pacsButton.style.background = '#d13438';
                            pacsButton.style.color = 'white';
                            this.showNotification('PACS connection failed: ' + (message.error || 'Unknown error'), 'error');
                        }
                        
                        // Reset button after 3 seconds
                        setTimeout(() => {
                            pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                            pacsButton.style.background = '';
                            pacsButton.style.color = '';
                        }, 3000);
                    }
                    break;
                    
                case 'mwlTestResult':
                    const mwlButton = document.getElementById('test-mwl');
                    if (mwlButton) {
                        // Cancel timeout since we got a response
                        if (mwlButton.timeoutId) {
                            clearTimeout(mwlButton.timeoutId);
                            mwlButton.timeoutId = null;
                        }
                        
                        mwlButton.disabled = false;
                        if (message.success) {
                            const count = message.data?.worklistCount || 0;
                            // Show success state on button
                            mwlButton.innerHTML = `<i class="ms-Icon ms-Icon--CheckMark"></i><span>${count} Items Found!</span>`;
                            mwlButton.style.background = '#107c10';
                            mwlButton.style.color = 'white';
                            this.showNotification(`MWL connection successful! Found ${count} worklist items.`, 'success');
                        } else {
                            // Show error state on button
                            mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                            mwlButton.style.background = '#d13438';
                            mwlButton.style.color = 'white';
                            this.showNotification('MWL connection failed: ' + (message.error || 'Unknown error'), 'error');
                        }
                        
                        // Reset button after 3 seconds
                        setTimeout(() => {
                            mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                            mwlButton.style.background = '';
                            mwlButton.style.color = '';
                        }, 3000);
                    }
                    break;
                    
                case 'folderSelected':
                    if (message.data?.targetInputId && message.data?.path) {
                        const input = document.getElementById(message.data.targetInputId);
                        if (input) {
                            input.value = message.data.path;
                            input.dispatchEvent(new Event('change'));
                        }
                    }
                    break;
                    
                case 'testResult':
                    // Handle test results from C# diagnostic window
                    if (message.service === 'PACS') {
                        const pacsButton = document.getElementById('test-pacs');
                        if (pacsButton) {
                            pacsButton.disabled = false;
                            if (message.success) {
                                // Show success state on button
                                pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--CheckMark"></i><span>Connected!</span>';
                                pacsButton.style.background = '#107c10';
                                pacsButton.style.color = 'white';
                                this.showNotification(message.message || 'PACS connection successful!', 'success');
                            } else {
                                // Show error state on button
                                pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                                pacsButton.style.background = '#d13438';
                                pacsButton.style.color = 'white';
                                this.showNotification(message.message || 'PACS connection failed', 'error');
                            }
                            
                            // Reset button after 3 seconds
                            setTimeout(() => {
                                pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                                pacsButton.style.background = '';
                                pacsButton.style.color = '';
                            }, 3000);
                        }
                    } else if (message.service === 'MWL') {
                        const mwlButton = document.getElementById('test-mwl');
                        if (mwlButton) {
                            mwlButton.disabled = false;
                            if (message.success) {
                                const count = message.worklistCount || 0;
                                // Show success state on button
                                mwlButton.innerHTML = `<i class="ms-Icon ms-Icon--CheckMark"></i><span>${count} Items Found!</span>`;
                                mwlButton.style.background = '#107c10';
                                mwlButton.style.color = 'white';
                                this.showNotification(message.message || `MWL connection successful! Found ${count} worklist items.`, 'success');
                            } else {
                                // Show error state on button
                                mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--ErrorBadge"></i><span>Failed</span>';
                                mwlButton.style.background = '#d13438';
                                mwlButton.style.color = 'white';
                                this.showNotification(message.message || 'MWL connection failed', 'error');
                            }
                            
                            // Reset button after 3 seconds
                            setTimeout(() => {
                                mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                                mwlButton.style.background = '';
                                mwlButton.style.color = '';
                            }, 3000);
                        }
                    }
                    break;
            }
        });
    }
    
    async handleSaveSettings() {
        try {
            console.log('[SettingsHandler] Starting save process...');
            
            // Sammle alle Daten mit der bewährten Logik
            const config = this.gatherFormData();
            console.log('[SettingsHandler] Gathered config:', JSON.stringify(config, null, 2));
            
            // Validierung
            const validation = this.validateConfig(config);
            if (!validation.valid) {
                this.showNotification(`Validation failed: ${validation.message}`, 'error');
                return;
            }
            
            // Zeige "Saving..." Notification
            this.showNotification('Saving settings...', 'info');
            
            // Sende an C#
            console.log('[SettingsHandler] Sending to host...');
            this.sendToHost('savesettings', config);
            
            // Warte auf Response von C# (wird über message handler kommen)
            // Zeige was gespeichert wurde
            this.showSavedSummary(config);
            
        } catch (error) {
            console.error('[SettingsHandler] Save failed:', error);
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

        // Check if form exists
        if (!this.form) {
            console.error('[SettingsHandler] Form not found!');
            this.form = document.getElementById('settingsForm');
            if (!this.form) {
                throw new Error('Settings form not found');
            }
        }

        // Get all input elements
        const inputs = this.form.querySelectorAll('input, select');
        console.log(`[SettingsHandler] Found ${inputs.length} inputs`);
        
        inputs.forEach(input => {
            const mapping = this.htmlIdToPropertyPath(input.id);
            if (!mapping) {
                console.warn('[SettingsHandler] No mapping for input:', input.id);
                return;
            }
            
            // Debug log for MWL fields
            if (input.id.includes('mwl')) {
                console.log(`[SettingsHandler] MWL field: ${input.id} → ${mapping.section}.${mapping.property} = ${input.value}`);
            }

            const { section, property } = mapping;
            
            // Get value based on input type
            let value;
            if (input.type === 'checkbox') {
                value = input.checked;
            } else if (input.type === 'number') {
                value = parseInt(input.value) || 0;
            } else {
                // All text inputs remain as strings
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

        console.log('[SettingsHandler] Gathered config:', config);
        return config;
    }
    
    htmlIdToPropertyPath(htmlId) {
        // Special cases for MWL fields (remove redundant "mwl" from property names)
        const mwlSpecialCases = {
            'mwlsettings-mwl-server-host': { section: 'MwlSettings', property: 'MwlServerHost' },
            'mwlsettings-mwl-server-port': { section: 'MwlSettings', property: 'MwlServerPort' },
            'mwlsettings-mwl-server-aet': { section: 'MwlSettings', property: 'MwlServerAET' },
            'mwlsettings-enable-worklist': { section: 'MwlSettings', property: 'EnableWorklist' },
            'mwlsettings-cache-expiry-hours': { section: 'MwlSettings', property: 'CacheExpiryHours' },
            'mwlsettings-auto-refresh-seconds': { section: 'MwlSettings', property: 'AutoRefreshSeconds' },
            'mwlsettings-show-emergency-first': { section: 'MwlSettings', property: 'ShowEmergencyFirst' },
            'mwlsettings-default-query-period': { section: 'MwlSettings', property: 'DefaultQueryPeriod' },
            'mwlsettings-query-days-before': { section: 'MwlSettings', property: 'QueryDaysBefore' },
            'mwlsettings-query-days-after': { section: 'MwlSettings', property: 'QueryDaysAfter' }
        };
        
        if (mwlSpecialCases[htmlId]) {
            return mwlSpecialCases[htmlId];
        }
        
        const parts = htmlId.split('-');
        if (parts.length < 2) return null;
        
        const section = this.capitalizeSection(parts[0]);
        const property = parts.slice(1)
            .map((p, i) => i === 0 ? this.capitalize(p) : this.capitalize(p))
            .join('');
        
        return { section, property };
    }
    
    capitalizeSection(section) {
        const sectionMap = {
            'storage': 'Storage',
            'pacs': 'Pacs',
            'mwl': 'MwlSettings',
            'mwlsettings': 'MwlSettings',
            'video': 'Video',
            'application': 'Application'
        };
        return sectionMap[section.toLowerCase()] || section;
    }
    
    capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
    }
    
    validateConfig(config) {
        // PACS Validierung
        if (config.Pacs.ServerHost) {
            if (!config.Pacs.CalledAeTitle) {
                return { valid: false, message: 'PACS Server AE Title is required' };
            }
            if (!config.Pacs.CallingAeTitle) {
                return { valid: false, message: 'PACS Local AE Title is required' };
            }
            if (!config.Pacs.ServerPort || config.Pacs.ServerPort <= 0) {
                return { valid: false, message: 'Valid PACS port is required' };
            }
        }
        
        // MWL Validierung
        console.log('[Validation] MwlSettings:', config.MwlSettings);
        if (config.MwlSettings.EnableWorklist) {
            console.log('[Validation] MWL is enabled, checking host:', config.MwlSettings.MwlServerHost);
            if (!config.MwlSettings.MwlServerHost) {
                return { valid: false, message: 'MWL Server Host is required when MWL is enabled' };
            }
            if (!config.MwlSettings.MwlServerPort || config.MwlSettings.MwlServerPort <= 0) {
                return { valid: false, message: 'Valid MWL port is required' };
            }
        }
        
        // Storage Paths Validierung
        if (!config.Storage.PhotosPath) {
            return { valid: false, message: 'Photos path is required' };
        }
        
        return { valid: true };
    }
    
    async handleTestPacs() {
        try {
            // Update button to show loading state
            const pacsButton = document.getElementById('test-pacs');
            if (pacsButton) {
                pacsButton.disabled = true;
                pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--Sync"></i><span>Testing...</span>';
                pacsButton.style.background = '#0078d4';
                pacsButton.style.color = 'white';
            }
            
            this.showNotification('Testing PACS connection...', 'info');
            
            // Sammle PACS-Daten
            const pacsConfig = {
                ServerHost: document.getElementById('pacs-server-host')?.value || '',
                ServerPort: parseInt(document.getElementById('pacs-server-port')?.value) || 104,
                CalledAeTitle: document.getElementById('pacs-called-ae-title')?.value || '',
                CallingAeTitle: document.getElementById('pacs-calling-ae-title')?.value || 'SMARTBOX',
                Timeout: parseInt(document.getElementById('pacs-timeout')?.value) || 30
            };
            
            if (!pacsConfig.ServerHost) {
                this.showNotification('Please enter PACS server host', 'error');
                // Reset button
                if (pacsButton) {
                    pacsButton.disabled = false;
                    pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                    pacsButton.style.background = '';
                    pacsButton.style.color = '';
                }
                return;
            }
            
            // Show detailed test info
            this.showNotification(
                `Testing PACS connection to ${pacsConfig.ServerHost}:${pacsConfig.ServerPort} ` +
                `(AET: ${pacsConfig.CallingAeTitle} → ${pacsConfig.CalledAeTitle})`, 
                'info'
            );
            
            console.log('[SettingsHandler] Testing PACS:', pacsConfig);
            this.sendToHost('testpacsconnection', pacsConfig);
            
            // Auto-reset button after 10 seconds if no response
            const pacsTimeoutId = setTimeout(() => {
                if (pacsButton && pacsButton.disabled) {
                    console.warn('[SettingsHandler] PACS test timeout - resetting button');
                    pacsButton.disabled = false;
                    pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                    pacsButton.style.background = '';
                    pacsButton.style.color = '';
                    this.showNotification('PACS test timed out', 'error');
                }
            }, 10000);
            
            // Store timeout ID on button for cancellation
            pacsButton.timeoutId = pacsTimeoutId;
            
        } catch (error) {
            console.error('[SettingsHandler] PACS test failed:', error);
            this.showNotification('PACS test failed: ' + error.message, 'error');
            // Reset button on error
            const pacsButton = document.getElementById('test-pacs');
            if (pacsButton) {
                pacsButton.disabled = false;
                pacsButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test Connection</span>';
                pacsButton.style.background = '';
                pacsButton.style.color = '';
            }
        }
    }
    
    async handleTestMwl() {
        try {
            // Update button to show loading state
            const mwlButton = document.getElementById('test-mwl');
            if (mwlButton) {
                mwlButton.disabled = true;
                mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--Sync"></i><span>Testing...</span>';
                mwlButton.style.background = '#0078d4';
                mwlButton.style.color = 'white';
            }
            
            this.showNotification('Testing MWL connection...', 'info');
            
            // Sammle MWL-Daten
            const mwlConfig = {
                EnableWorklist: document.getElementById('mwlsettings-enable-worklist')?.checked,
                MwlServerHost: document.getElementById('mwlsettings-mwl-server-host')?.value || '',
                MwlServerPort: parseInt(document.getElementById('mwlsettings-mwl-server-port')?.value) || 105,
                MwlServerAET: document.getElementById('mwlsettings-mwl-server-aet')?.value || '',
                LocalAET: 'SMARTBOX' // This is fixed in the C# code
            };
            
            if (!mwlConfig.MwlServerHost) {
                this.showNotification('Please enter MWL server host', 'error');
                // Reset button
                if (mwlButton) {
                    mwlButton.disabled = false;
                    mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                    mwlButton.style.background = '';
                    mwlButton.style.color = '';
                }
                return;
            }
            
            // Show detailed test info
            this.showNotification(
                `Testing MWL connection to ${mwlConfig.MwlServerHost}:${mwlConfig.MwlServerPort} ` +
                `(AET: ${mwlConfig.LocalAET} → ${mwlConfig.MwlServerAET})`, 
                'info'
            );
            
            console.log('[SettingsHandler] Testing MWL:', mwlConfig);
            this.sendToHost('testmwlconnection', mwlConfig);
            
            // Auto-reset button after 10 seconds if no response
            const mwlTimeoutId = setTimeout(() => {
                if (mwlButton && mwlButton.disabled) {
                    console.warn('[SettingsHandler] MWL test timeout - resetting button');
                    mwlButton.disabled = false;
                    mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                    mwlButton.style.background = '';
                    mwlButton.style.color = '';
                    this.showNotification('MWL test timed out', 'error');
                }
            }, 10000);
            
            // Store timeout ID on button for cancellation
            mwlButton.timeoutId = mwlTimeoutId;
            
        } catch (error) {
            console.error('[SettingsHandler] MWL test failed:', error);
            this.showNotification('MWL test failed: ' + error.message, 'error');
            // Reset button on error
            const mwlButton = document.getElementById('test-mwl');
            if (mwlButton) {
                mwlButton.disabled = false;
                mwlButton.innerHTML = '<i class="ms-Icon ms-Icon--TestBeaker"></i><span>Test MWL Connection</span>';
                mwlButton.style.background = '';
                mwlButton.style.color = '';
            }
        }
    }
    
    handleBrowseFolder(button) {
        const targetInputId = button.dataset.for;
        if (!targetInputId) {
            console.error('[SettingsHandler] No target input specified for browse button');
            return;
        }
        
        console.log('[SettingsHandler] Browse folder for:', targetInputId);
        this.sendToHost('browsefolder', { targetInputId });
    }
    
    sendToHost(action, data) {
        if (window.chrome?.webview) {
            window.chrome.webview.postMessage(JSON.stringify({
                type: action,
                data: data
            }));
        } else {
            console.warn('[SettingsHandler] WebView2 not available');
        }
    }
    
    showNotification(message, type = 'info') {
        console.log(`[${type.toUpperCase()}] ${message}`);
        
        // Remove any existing notification
        const existing = document.querySelector('.notification');
        if (existing) {
            existing.remove();
        }

        // Create notification element - slide in from right
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        
        // Add icon based on type
        const icons = {
            'success': '✓',
            'error': '✕',
            'info': 'ℹ',
            'warning': '⚠'
        };
        
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">${icons[type] || ''}</span>
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Trigger animation
        requestAnimationFrame(() => {
            notification.classList.add('show');
        });
        
        // Auto-remove after 5 seconds (except errors)
        if (type !== 'error') {
            setTimeout(() => {
                notification.classList.remove('show');
                setTimeout(() => notification.remove(), 300);
            }, 5000);
        }
    }
    
    showSavedSummary(config) {
        let summary = 'Saved: ';
        const items = [];
        
        if (config.Pacs.ServerHost) {
            items.push(`PACS (${config.Pacs.ServerHost})`);
        }
        if (config.MwlSettings.EnableMwl) {
            items.push('MWL enabled');
        }
        if (config.Application.AutoStartCapture) {
            items.push('Auto-start enabled');
        }
        
        if (items.length > 0) {
            summary += items.join(', ');
            console.log('[SettingsHandler]', summary);
        }
    }
}

// Initialisiere wenn DOM bereit
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.settingsHandler = new SettingsHandler();
    });
} else {
    window.settingsHandler = new SettingsHandler();
}

console.log('[SettingsHandler] Module loaded');