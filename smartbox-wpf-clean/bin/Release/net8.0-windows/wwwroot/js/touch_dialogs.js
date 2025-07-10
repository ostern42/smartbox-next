/**
 * Touch Dialog Manager for SmartBox Next
 * Handles confirmation dialogs with proper touch UX patterns
 * LEFT = Cancel/Safe, RIGHT = Confirm/Action
 */

class TouchDialogManager {
    constructor() {
        this.currentDialog = null;
        this.dialogQueue = [];
        
        // Get dialog elements
        this.backdrop = document.getElementById('dialogBackdrop');
        this.dialogBox = document.getElementById('dialogBox');
        this.dialogTitle = document.getElementById('dialogTitle');
        this.dialogMessage = document.getElementById('dialogMessage');
        this.cancelButton = document.getElementById('dialogCancel');
        this.confirmButton = document.getElementById('dialogConfirm');
        
        this.initializeDialogs();
    }

    initializeDialogs() {
        if (!this.backdrop) {
            console.error('TouchDialogManager: Dialog elements not found');
            return;
        }

        // Backdrop click to cancel (optional behavior)
        this.backdrop.addEventListener('click', (e) => {
            if (e.target === this.backdrop) {
                this.dismiss();
            }
        });

        // Button event listeners
        this.cancelButton?.addEventListener('click', () => this.onCancel());
        this.confirmButton?.addEventListener('click', () => this.onConfirm());
        
        // Keyboard navigation (Accessibility)
        document.addEventListener('keydown', (e) => this.onKeyDown(e));

        console.log('TouchDialogManager: Initialized');
    }

    /**
     * Show confirmation dialog
     * @param {Object} options - Dialog configuration
     */
    showConfirmation(options = {}) {
        const config = {
            title: 'Bestätigung',
            message: 'Möchten Sie fortfahren?',
            cancelText: 'Abbrechen',
            confirmText: 'OK',
            cancelIcon: 'ms-Icon ms-Icon--Cancel',
            confirmIcon: 'ms-Icon ms-Icon--CheckMark',
            confirmStyle: 'confirm', // 'confirm', 'danger', 'success'
            onConfirm: null,
            onCancel: null,
            allowBackdropDismiss: true,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show delete confirmation dialog
     */
    showDeleteConfirmation(options = {}) {
        const config = {
            title: 'Wirklich löschen?',
            message: options.message || 'Diese Aktion kann nicht rückgängig gemacht werden.',
            cancelText: 'Abbrechen',
            confirmText: 'Löschen',
            cancelIcon: 'ms-Icon ms-Icon--Cancel',
            confirmIcon: 'ms-Icon ms-Icon--Delete',
            confirmStyle: 'danger',
            allowBackdropDismiss: false,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show export confirmation dialog
     */
    showExportConfirmation(options = {}) {
        const config = {
            title: 'An PACS senden?',
            message: options.message || 'Alle Aufnahmen werden an das PACS-System gesendet.',
            cancelText: 'Abbrechen',
            confirmText: 'Senden',
            cancelIcon: 'ms-Icon ms-Icon--Cancel',
            confirmIcon: 'ms-Icon ms-Icon--Upload',
            confirmStyle: 'success',
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show session end confirmation
     */
    showEndSessionConfirmation(options = {}) {
        const captureCount = options.captureCount || 0;
        const hasUnsaved = captureCount > 0;
        
        const unsavedMessage = captureCount === 1
            ? 'Eine Aufnahme wurde noch nicht exportiert!'
            : `${captureCount} Aufnahmen wurden noch nicht exportiert!`;
        
        const config = {
            title: 'Sitzung beenden?',
            message: hasUnsaved 
                ? unsavedMessage
                : 'Möchten Sie die Sitzung wirklich beenden?',
            cancelText: 'Zurück',
            confirmText: 'Beenden',
            cancelIcon: 'ms-Icon ms-Icon--Back',
            confirmIcon: hasUnsaved ? 'ms-Icon ms-Icon--Warning' : 'ms-Icon ms-Icon--SignOut',
            confirmStyle: hasUnsaved ? 'danger' : 'confirm',
            allowBackdropDismiss: false,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show error dialog
     */
    showError(options = {}) {
        const config = {
            title: 'Fehler',
            message: options.message || 'Ein unbekannter Fehler ist aufgetreten.',
            cancelText: null, // Hide cancel button
            confirmText: 'OK',
            confirmIcon: 'ms-Icon ms-Icon--ErrorBadge',
            confirmStyle: 'danger',
            allowBackdropDismiss: true,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show success dialog
     */
    showSuccess(options = {}) {
        const config = {
            title: 'Erfolgreich',
            message: options.message || 'Vorgang erfolgreich abgeschlossen.',
            cancelText: null, // Hide cancel button
            confirmText: 'OK',
            confirmIcon: 'ms-Icon ms-Icon--CheckMark',
            confirmStyle: 'success',
            allowBackdropDismiss: true,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Show loading dialog
     */
    showLoading(options = {}) {
        const config = {
            title: 'Bitte warten',
            message: options.message || 'Vorgang wird ausgeführt...',
            cancelText: null,
            confirmText: null,
            showSpinner: true,
            allowBackdropDismiss: false,
            ...options
        };

        this.showDialog(config);
    }

    /**
     * Generic dialog display method
     */
    showDialog(config) {
        if (this.currentDialog) {
            // Queue if dialog already showing
            this.dialogQueue.push(config);
            return;
        }

        this.currentDialog = config;

        // Set content
        this.dialogTitle.textContent = config.title;
        this.dialogMessage.textContent = config.message;

        // Configure cancel button (LEFT)
        if (config.cancelText) {
            this.cancelButton.style.display = 'flex';
            this.cancelButton.querySelector('span').textContent = config.cancelText;
            
            // Update icon
            const cancelIcon = this.cancelButton.querySelector('i');
            if (cancelIcon && config.cancelIcon) {
                cancelIcon.className = config.cancelIcon;
            }
        } else {
            this.cancelButton.style.display = 'none';
        }

        // Configure confirm button (RIGHT)
        if (config.confirmText) {
            this.confirmButton.style.display = 'flex';
            this.confirmButton.querySelector('span').textContent = config.confirmText;
            
            // Update icon
            const confirmIcon = this.confirmButton.querySelector('i');
            if (confirmIcon && config.confirmIcon) {
                confirmIcon.className = config.confirmIcon;
            }

            // Apply style
            this.confirmButton.className = `dialog-button confirm ${config.confirmStyle}`;
        } else {
            this.confirmButton.style.display = 'none';
        }

        // Add loading spinner if needed
        if (config.showSpinner) {
            this.addLoadingSpinner();
        } else {
            this.removeLoadingSpinner();
        }

        // Configure backdrop behavior
        if (!config.allowBackdropDismiss) {
            this.backdrop.style.pointerEvents = 'none';
            this.dialogBox.style.pointerEvents = 'auto';
        } else {
            this.backdrop.style.pointerEvents = 'auto';
        }

        // Show dialog
        this.backdrop.classList.remove('hidden');
        
        // Focus management for accessibility
        setTimeout(() => {
            if (config.confirmText) {
                this.confirmButton.focus();
            } else if (config.cancelText) {
                this.cancelButton.focus();
            }
        }, 100);

        // Haptic feedback
        this.hapticFeedback('light');
    }

    /**
     * Add loading spinner to dialog
     */
    addLoadingSpinner() {
        if (this.dialogMessage.querySelector('.dialog-spinner')) return;

        const spinner = document.createElement('div');
        spinner.className = 'dialog-spinner';
        spinner.style.cssText = `
            width: 32px;
            height: 32px;
            border: 3px solid #e1dfdd;
            border-top: 3px solid #0078d4;
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 16px auto 0;
        `;
        
        this.dialogMessage.appendChild(spinner);
    }

    /**
     * Remove loading spinner
     */
    removeLoadingSpinner() {
        const spinner = this.dialogMessage.querySelector('.dialog-spinner');
        if (spinner) {
            spinner.remove();
        }
    }

    /**
     * Handle cancel button (LEFT button)
     */
    onCancel() {
        if (!this.currentDialog) return;

        const callback = this.currentDialog.onCancel;
        this.dismiss();
        
        if (callback && typeof callback === 'function') {
            callback();
        }

        this.hapticFeedback('light');
    }

    /**
     * Handle confirm button (RIGHT button)
     */
    onConfirm() {
        if (!this.currentDialog) return;

        const callback = this.currentDialog.onConfirm;
        this.dismiss();
        
        if (callback && typeof callback === 'function') {
            callback();
        }

        this.hapticFeedback('medium');
    }

    /**
     * Dismiss current dialog
     */
    dismiss() {
        if (!this.currentDialog) return;

        this.backdrop.classList.add('hidden');
        this.currentDialog = null;
        this.removeLoadingSpinner();

        // Process next dialog in queue
        setTimeout(() => {
            if (this.dialogQueue.length > 0) {
                const nextDialog = this.dialogQueue.shift();
                this.showDialog(nextDialog);
            }
        }, 150);
    }

    /**
     * Dismiss all dialogs and clear queue
     */
    dismissAll() {
        this.dialogQueue = [];
        this.dismiss();
    }

    /**
     * Handle keyboard shortcuts (Accessibility)
     */
    onKeyDown(event) {
        if (!this.currentDialog || this.backdrop.classList.contains('hidden')) {
            return;
        }
        
        switch (event.key) {
            case 'Escape':
                if (this.currentDialog.allowBackdropDismiss !== false) {
                    event.preventDefault();
                    this.onCancel();
                }
                break;
            case 'Enter':
                event.preventDefault();
                this.onConfirm();
                break;
            case 'Tab':
                // Focus management for dialog
                this.handleTabNavigation(event);
                break;
        }
    }
    
    handleTabNavigation(event) {
        const focusableElements = this.dialogBox.querySelectorAll(
            'button:not([disabled]), [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;
        
        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];
        
        if (event.shiftKey) {
            if (document.activeElement === firstElement) {
                event.preventDefault();
                lastElement.focus();
            }
        } else {
            if (document.activeElement === lastElement) {
                event.preventDefault();
                firstElement.focus();
            }
        }
    }

    /**
     * Quick methods for common dialogs
     */
    
    // Quick delete confirmation with item count
    confirmDelete(itemName, count = 1) {
        const message = count > 1 
            ? `${count} ${itemName} wirklich löschen?`
            : `${itemName} wirklich löschen?`;
            
        return new Promise((resolve) => {
            this.showDeleteConfirmation({
                message: message,
                onConfirm: () => resolve(true),
                onCancel: () => resolve(false)
            });
        });
    }

    // Quick export confirmation
    confirmExport(itemCount = 1, targetSystem = 'PACS') {
        const message = itemCount > 1
            ? `${itemCount} Aufnahmen an ${targetSystem} senden?`
            : `Aufnahme an ${targetSystem} senden?`;
            
        return new Promise((resolve) => {
            this.showExportConfirmation({
                message: message,
                onConfirm: () => resolve(true),
                onCancel: () => resolve(false)
            });
        });
    }

    // Quick session end confirmation
    confirmEndSession(unsavedCount = 0) {
        return new Promise((resolve) => {
            this.showEndSessionConfirmation({
                captureCount: unsavedCount,
                onConfirm: () => resolve(true),
                onCancel: () => resolve(false)
            });
        });
    }

    // Show simple alert
    alert(message, title = 'Information') {
        return new Promise((resolve) => {
            this.showSuccess({
                title: title,
                message: message,
                onConfirm: () => resolve()
            });
        });
    }

    // Show simple error
    error(message, title = 'Fehler') {
        return new Promise((resolve) => {
            this.showError({
                title: title,
                message: message,
                onConfirm: () => resolve()
            });
        });
    }

    /**
     * Haptic feedback
     */
    hapticFeedback(intensity = 'light') {
        if ('vibrate' in navigator) {
            const patterns = {
                light: 10,
                medium: 50,
                heavy: 100
            };
            navigator.vibrate(patterns[intensity] || 10);
        }
    }

    /**
     * Check if any dialog is currently showing
     */
    isShowing() {
        return this.currentDialog !== null;
    }

    /**
     * Get current dialog config
     */
    getCurrentDialog() {
        return this.currentDialog;
    }
}

// Export for use in main app
window.TouchDialogManager = TouchDialogManager;