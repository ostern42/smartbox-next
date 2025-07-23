/**
 * SmartBox Authentication Manager
 * Handles JWT tokens, refresh tokens, and secure storage
 */

class AuthManager {
    constructor(config) {
        this.config = config || window.StreamingConfig;
        this.authToken = null;
        this.refreshToken = null;
        this.user = null;
        this.tokenRefreshTimer = null;
        this.refreshPromise = null;
        
        // Initialize from secure storage
        this.loadStoredCredentials();
    }

    /**
     * Load credentials from secure storage
     */
    loadStoredCredentials() {
        try {
            // Use sessionStorage for better security in medical environments
            const stored = sessionStorage.getItem('smartbox_auth');
            if (stored) {
                const data = JSON.parse(stored);
                // Check if tokens are still valid
                if (this.isTokenValid(data.authToken)) {
                    this.authToken = data.authToken;
                    this.refreshToken = data.refreshToken;
                    this.user = data.user;
                    this.scheduleTokenRefresh();
                    return true;
                }
            }
        } catch (e) {
            console.error('Failed to load stored credentials:', e);
        }
        return false;
    }

    /**
     * Save credentials to secure storage
     */
    saveCredentials() {
        try {
            const data = {
                authToken: this.authToken,
                refreshToken: this.refreshToken,
                user: this.user,
                timestamp: Date.now()
            };
            sessionStorage.setItem('smartbox_auth', JSON.stringify(data));
        } catch (e) {
            console.error('Failed to save credentials:', e);
        }
    }

    /**
     * Clear all stored credentials
     */
    clearCredentials() {
        this.authToken = null;
        this.refreshToken = null;
        this.user = null;
        sessionStorage.removeItem('smartbox_auth');
        this.cancelTokenRefresh();
    }

    /**
     * Check if a JWT token is valid
     */
    isTokenValid(token) {
        if (!token) return false;
        
        try {
            const payload = this.parseJWT(token);
            if (!payload || !payload.exp) return false;
            
            // Check expiration with buffer
            const expiryTime = payload.exp * 1000;
            const bufferTime = this.config.get('tokenExpiryBuffer', 60000);
            return Date.now() < (expiryTime - bufferTime);
        } catch (e) {
            return false;
        }
    }

    /**
     * Parse JWT token
     */
    parseJWT(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) return null;
            
            const payload = parts[1];
            const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
            return JSON.parse(decoded);
        } catch (e) {
            console.error('Failed to parse JWT:', e);
            return null;
        }
    }

    /**
     * Login with username and password
     */
    async login(username, password) {
        try {
            const response = await fetch(`${this.config.apiUrl}/auth/login`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Login failed');
            }

            const data = await response.json();
            
            // Store tokens and user info
            this.authToken = data.access_token;
            this.refreshToken = data.refresh_token;
            this.user = data.user;
            
            // Save to storage
            this.saveCredentials();
            
            // Schedule token refresh
            this.scheduleTokenRefresh();
            
            return {
                success: true,
                user: this.user
            };
        } catch (error) {
            console.error('Login error:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }

    /**
     * Logout and clear session
     */
    async logout() {
        try {
            // Notify server about logout
            if (this.authToken) {
                await fetch(`${this.config.apiUrl}/auth/logout`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${this.authToken}`
                    }
                });
            }
        } catch (e) {
            console.error('Logout notification failed:', e);
        } finally {
            // Clear local credentials regardless
            this.clearCredentials();
        }
    }

    /**
     * Get authorization headers
     */
    getAuthHeaders() {
        if (!this.authToken) {
            throw new Error('Not authenticated');
        }
        
        return {
            'Authorization': `Bearer ${this.authToken}`
        };
    }

    /**
     * Make authenticated request
     */
    async authenticatedFetch(url, options = {}) {
        // Ensure we have valid token
        if (!this.isTokenValid(this.authToken)) {
            await this.refreshAccessToken();
        }

        // Add auth headers
        options.headers = {
            ...options.headers,
            ...this.getAuthHeaders()
        };

        const response = await fetch(url, options);

        // Handle 401 - try refresh once
        if (response.status === 401 && this.refreshToken) {
            await this.refreshAccessToken();
            
            // Retry with new token
            options.headers = {
                ...options.headers,
                ...this.getAuthHeaders()
            };
            
            return fetch(url, options);
        }

        return response;
    }

    /**
     * Refresh access token
     */
    async refreshAccessToken() {
        // Prevent multiple simultaneous refresh attempts
        if (this.refreshPromise) {
            return this.refreshPromise;
        }

        this.refreshPromise = this._doRefreshToken();
        
        try {
            await this.refreshPromise;
        } finally {
            this.refreshPromise = null;
        }
    }

    async _doRefreshToken() {
        if (!this.refreshToken) {
            throw new Error('No refresh token available');
        }

        try {
            const response = await fetch(`${this.config.apiUrl}/auth/refresh`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    refresh_token: this.refreshToken
                })
            });

            if (!response.ok) {
                throw new Error('Token refresh failed');
            }

            const data = await response.json();
            
            // Update tokens
            this.authToken = data.access_token;
            if (data.refresh_token) {
                this.refreshToken = data.refresh_token;
            }
            
            // Save updated credentials
            this.saveCredentials();
            
            // Reschedule refresh
            this.scheduleTokenRefresh();
            
        } catch (error) {
            console.error('Token refresh error:', error);
            // Clear credentials on refresh failure
            this.clearCredentials();
            throw error;
        }
    }

    /**
     * Schedule automatic token refresh
     */
    scheduleTokenRefresh() {
        this.cancelTokenRefresh();
        
        if (!this.authToken) return;
        
        try {
            const payload = this.parseJWT(this.authToken);
            if (!payload || !payload.exp) return;
            
            // Calculate refresh time (5 minutes before expiry)
            const expiryTime = payload.exp * 1000;
            const refreshBuffer = 5 * 60 * 1000; // 5 minutes
            const refreshTime = expiryTime - refreshBuffer - Date.now();
            
            if (refreshTime > 0) {
                this.tokenRefreshTimer = setTimeout(() => {
                    this.refreshAccessToken().catch(err => {
                        console.error('Scheduled token refresh failed:', err);
                        // Emit event for UI to handle
                        window.dispatchEvent(new CustomEvent('authExpired'));
                    });
                }, refreshTime);
            }
        } catch (e) {
            console.error('Failed to schedule token refresh:', e);
        }
    }

    /**
     * Cancel scheduled token refresh
     */
    cancelTokenRefresh() {
        if (this.tokenRefreshTimer) {
            clearTimeout(this.tokenRefreshTimer);
            this.tokenRefreshTimer = null;
        }
    }

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        return this.authToken && this.isTokenValid(this.authToken);
    }

    /**
     * Get current user
     */
    getCurrentUser() {
        return this.user;
    }

    /**
     * Check if user has specific role
     */
    hasRole(role) {
        return this.user && this.user.roles && this.user.roles.includes(role);
    }

    /**
     * Check if user has specific permission
     */
    hasPermission(permission) {
        return this.user && this.user.permissions && this.user.permissions.includes(permission);
    }
}

// Export as singleton
window.AuthManager = new AuthManager();