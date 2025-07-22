/**
 * Real-time Performance Monitoring Dashboard for SmartBox-Next
 * Medical-grade performance monitoring with 99.9% reliability requirements
 */

class MedicalPerformanceMonitor {
    constructor() {
        this.isInitialized = false;
        this.isConnected = false;
        this.monitoringInterval = null;
        this.charts = {};
        this.metrics = {
            cpu: [],
            memory: [],
            storage: [],
            network: [],
            health: []
        };
        this.alerts = [];
        this.config = {
            updateInterval: 2000, // 2 seconds for medical-grade monitoring
            maxDataPoints: 300, // 10 minutes of history
            thresholds: {
                cpu: { warning: 50, critical: 80 },
                memory: { warning: 70, critical: 90 },
                storage: { warning: 85, critical: 95 },
                network: { warning: 80, critical: 95 }
            }
        };
        this.websocket = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 10;
    }

    async initialize() {
        if (this.isInitialized) return;
        
        console.log('Initializing Medical Performance Monitor...');
        
        try {
            await this.setupWebSocket();
            await this.initializeCharts();
            await this.initializeUI();
            this.startMonitoring();
            
            this.isInitialized = true;
            console.log('Medical Performance Monitor initialized successfully');
            
            // Show initialization success
            this.showNotification('Performance monitoring started', 'success');
            
        } catch (error) {
            console.error('Failed to initialize performance monitor:', error);
            this.showNotification('Failed to start performance monitoring', 'error');
            throw error;
        }
    }

    async setupWebSocket() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}/performance-ws`;
        
        this.websocket = new WebSocket(wsUrl);
        
        return new Promise((resolve, reject) => {
            this.websocket.onopen = () => {
                console.log('Performance WebSocket connected');
                this.isConnected = true;
                this.reconnectAttempts = 0;
                this.updateConnectionStatus(true);
                resolve();
            };
            
            this.websocket.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    this.handleMetricsUpdate(data);
                } catch (error) {
                    console.error('Error parsing WebSocket message:', error);
                }
            };
            
            this.websocket.onclose = () => {
                console.log('Performance WebSocket disconnected');
                this.isConnected = false;
                this.updateConnectionStatus(false);
                this.attemptReconnect();
            };
            
            this.websocket.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.updateConnectionStatus(false);
                reject(error);
            };
            
            // Timeout after 10 seconds
            setTimeout(() => {
                if (!this.isConnected) {
                    reject(new Error('WebSocket connection timeout'));
                }
            }, 10000);
        });
    }

    async attemptReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            this.showNotification('Connection lost - please refresh page', 'error');
            return;
        }
        
        this.reconnectAttempts++;
        const delay = Math.min(5000 * this.reconnectAttempts, 30000); // Exponential backoff, max 30s
        
        console.log(`Attempting reconnection ${this.reconnectAttempts}/${this.maxReconnectAttempts} in ${delay}ms`);
        
        setTimeout(async () => {
            try {
                await this.setupWebSocket();
            } catch (error) {
                console.error('Reconnection attempt failed:', error);
            }
        }, delay);
    }

    async initializeCharts() {
        // Initialize Chart.js charts for real-time monitoring
        Chart.defaults.font.family = "'Segoe UI', Tahoma, Geneva, Verdana, sans-serif";
        Chart.defaults.font.size = 12;
        Chart.defaults.color = '#333';
        
        // CPU Usage Chart
        this.charts.cpu = this.createTimeSeriesChart('cpuChart', 'CPU Usage (%)', '#e74c3c', {
            min: 0,
            max: 100,
            warningLine: this.config.thresholds.cpu.warning,
            criticalLine: this.config.thresholds.cpu.critical
        });
        
        // Memory Usage Chart
        this.charts.memory = this.createTimeSeriesChart('memoryChart', 'Memory Usage (%)', '#3498db', {
            min: 0,
            max: 100,
            warningLine: this.config.thresholds.memory.warning,
            criticalLine: this.config.thresholds.memory.critical
        });
        
        // Storage Usage Chart
        this.charts.storage = this.createTimeSeriesChart('storageChart', 'Storage Usage (%)', '#f39c12', {
            min: 0,
            max: 100,
            warningLine: this.config.thresholds.storage.warning,
            criticalLine: this.config.thresholds.storage.critical
        });
        
        // Network Usage Chart
        this.charts.network = this.createTimeSeriesChart('networkChart', 'Network Usage (%)', '#9b59b6', {
            min: 0,
            max: 100,
            warningLine: this.config.thresholds.network.warning,
            criticalLine: this.config.thresholds.network.critical
        });
        
        // Health Score Chart
        this.charts.health = this.createTimeSeriesChart('healthChart', 'System Health Score', '#27ae60', {
            min: 0,
            max: 100
        });
    }

    createTimeSeriesChart(canvasId, label, color, options = {}) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error(`Canvas element ${canvasId} not found`);
            return null;
        }
        
        const datasets = [
            {
                label: label,
                data: [],
                borderColor: color,
                backgroundColor: color + '20',
                borderWidth: 2,
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                pointHoverRadius: 5
            }
        ];
        
        // Add threshold lines if specified
        if (options.warningLine) {
            datasets.push({
                label: 'Warning Threshold',
                data: [],
                borderColor: '#f39c12',
                borderWidth: 1,
                borderDash: [5, 5],
                fill: false,
                pointRadius: 0
            });
        }
        
        if (options.criticalLine) {
            datasets.push({
                label: 'Critical Threshold',
                data: [],
                borderColor: '#e74c3c',
                borderWidth: 1,
                borderDash: [10, 5],
                fill: false,
                pointRadius: 0
            });
        }
        
        return new Chart(ctx, {
            type: 'line',
            data: {
                labels: [],
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 750
                },
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            displayFormats: {
                                minute: 'HH:mm'
                            }
                        },
                        title: {
                            display: true,
                            text: 'Time'
                        }
                    },
                    y: {
                        min: options.min || 0,
                        max: options.max || 100,
                        title: {
                            display: true,
                            text: label
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false
                    }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            }
        });
    }

    async initializeUI() {
        // Initialize performance summary cards
        this.updateSummaryCard('cpu-summary', 'CPU', 0, 'loading');
        this.updateSummaryCard('memory-summary', 'Memory', 0, 'loading');
        this.updateSummaryCard('storage-summary', 'Storage', 0, 'loading');
        this.updateSummaryCard('network-summary', 'Network', 0, 'loading');
        
        // Initialize alerts panel
        this.initializeAlertsPanel();
        
        // Initialize controls
        this.initializeControls();
        
        // Show loading state
        this.showLoadingState(true);
    }

    initializeAlertsPanel() {
        const alertsContainer = document.getElementById('alerts-container');
        if (alertsContainer) {
            alertsContainer.innerHTML = '<div class="alert-placeholder">No alerts</div>';
        }
    }

    initializeControls() {
        // Emergency mode button
        const emergencyBtn = document.getElementById('emergency-mode-btn');
        if (emergencyBtn) {
            emergencyBtn.addEventListener('click', () => this.toggleEmergencyMode());
        }
        
        // Optimization button
        const optimizeBtn = document.getElementById('optimize-btn');
        if (optimizeBtn) {
            optimizeBtn.addEventListener('click', () => this.triggerOptimization());
        }
        
        // Settings button
        const settingsBtn = document.getElementById('settings-btn');
        if (settingsBtn) {
            settingsBtn.addEventListener('click', () => this.showSettings());
        }
        
        // Refresh button
        const refreshBtn = document.getElementById('refresh-btn');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', () => this.forceRefresh());
        }
    }

    startMonitoring() {
        if (this.monitoringInterval) {
            clearInterval(this.monitoringInterval);
        }
        
        this.monitoringInterval = setInterval(() => {
            this.requestMetricsUpdate();
        }, this.config.updateInterval);
        
        console.log(`Monitoring started with ${this.config.updateInterval}ms interval`);
    }

    stopMonitoring() {
        if (this.monitoringInterval) {
            clearInterval(this.monitoringInterval);
            this.monitoringInterval = null;
        }
        
        console.log('Monitoring stopped');
    }

    requestMetricsUpdate() {
        if (this.isConnected && this.websocket.readyState === WebSocket.OPEN) {
            this.websocket.send(JSON.stringify({
                type: 'request-metrics',
                timestamp: Date.now()
            }));
        }
    }

    handleMetricsUpdate(data) {
        try {
            const timestamp = new Date(data.timestamp);
            
            // Update metrics data
            this.updateMetricData('cpu', timestamp, data.cpu?.usagePercent || 0);
            this.updateMetricData('memory', timestamp, data.memory?.usagePercent || 0);
            this.updateMetricData('storage', timestamp, data.storage?.usagePercent || 0);
            this.updateMetricData('network', timestamp, data.network?.usagePercent || 0);
            this.updateMetricData('health', timestamp, data.health?.score || 0);
            
            // Update charts
            this.updateChart('cpu', data.cpu?.usagePercent || 0);
            this.updateChart('memory', data.memory?.usagePercent || 0);
            this.updateChart('storage', data.storage?.usagePercent || 0);
            this.updateChart('network', data.network?.usagePercent || 0);
            this.updateChart('health', data.health?.score || 0);
            
            // Update summary cards
            this.updateSummaryCard('cpu-summary', 'CPU', data.cpu?.usagePercent || 0, this.getStatusLevel(data.cpu?.usagePercent || 0, 'cpu'));
            this.updateSummaryCard('memory-summary', 'Memory', data.memory?.usagePercent || 0, this.getStatusLevel(data.memory?.usagePercent || 0, 'memory'));
            this.updateSummaryCard('storage-summary', 'Storage', data.storage?.usagePercent || 0, this.getStatusLevel(data.storage?.usagePercent || 0, 'storage'));
            this.updateSummaryCard('network-summary', 'Network', data.network?.usagePercent || 0, this.getStatusLevel(data.network?.usagePercent || 0, 'network'));
            
            // Check for alerts
            this.checkAlerts(data);
            
            // Update session info
            this.updateSessionInfo(data);
            
            // Hide loading state
            this.showLoadingState(false);
            
        } catch (error) {
            console.error('Error handling metrics update:', error);
        }
    }

    updateMetricData(type, timestamp, value) {
        if (!this.metrics[type]) {
            this.metrics[type] = [];
        }
        
        this.metrics[type].push({
            timestamp: timestamp,
            value: value
        });
        
        // Limit data points
        if (this.metrics[type].length > this.config.maxDataPoints) {
            this.metrics[type].shift();
        }
    }

    updateChart(chartType, value) {
        const chart = this.charts[chartType];
        if (!chart) return;
        
        const now = new Date();
        const data = chart.data;
        
        // Add new data point
        data.labels.push(now);
        data.datasets[0].data.push(value);
        
        // Add threshold line data if present
        if (data.datasets.length > 1) {
            const threshold = this.config.thresholds[chartType];
            if (threshold) {
                data.datasets[1].data.push(threshold.warning);
                if (data.datasets.length > 2) {
                    data.datasets[2].data.push(threshold.critical);
                }
            }
        }
        
        // Limit data points
        if (data.labels.length > this.config.maxDataPoints) {
            data.labels.shift();
            data.datasets.forEach(dataset => dataset.data.shift());
        }
        
        chart.update('none'); // Disable animation for real-time updates
    }

    updateSummaryCard(cardId, label, value, status) {
        const card = document.getElementById(cardId);
        if (!card) return;
        
        const valueElement = card.querySelector('.metric-value');
        const statusElement = card.querySelector('.metric-status');
        const iconElement = card.querySelector('.metric-icon');
        
        if (valueElement) {
            valueElement.textContent = `${Math.round(value)}%`;
        }
        
        if (statusElement) {
            statusElement.className = `metric-status status-${status}`;
            statusElement.textContent = status.toUpperCase();
        }
        
        if (iconElement) {
            iconElement.className = `metric-icon ${this.getStatusIcon(status)}`;
        }
        
        // Update card background based on status
        card.className = `performance-card status-${status}`;
    }

    getStatusLevel(value, type) {
        const thresholds = this.config.thresholds[type];
        if (!thresholds) return 'normal';
        
        if (value >= thresholds.critical) return 'critical';
        if (value >= thresholds.warning) return 'warning';
        return 'normal';
    }

    getStatusIcon(status) {
        switch (status) {
            case 'critical': return 'fas fa-exclamation-triangle';
            case 'warning': return 'fas fa-exclamation-circle';
            case 'normal': return 'fas fa-check-circle';
            case 'loading': return 'fas fa-spinner fa-spin';
            default: return 'fas fa-question-circle';
        }
    }

    checkAlerts(data) {
        const alerts = [];
        
        // Check CPU
        if (data.cpu?.usagePercent >= this.config.thresholds.cpu.critical) {
            alerts.push({
                type: 'critical',
                message: `Critical CPU usage: ${Math.round(data.cpu.usagePercent)}%`,
                timestamp: new Date()
            });
        } else if (data.cpu?.usagePercent >= this.config.thresholds.cpu.warning) {
            alerts.push({
                type: 'warning',
                message: `High CPU usage: ${Math.round(data.cpu.usagePercent)}%`,
                timestamp: new Date()
            });
        }
        
        // Check Memory
        if (data.memory?.usagePercent >= this.config.thresholds.memory.critical) {
            alerts.push({
                type: 'critical',
                message: `Critical memory usage: ${Math.round(data.memory.usagePercent)}%`,
                timestamp: new Date()
            });
        } else if (data.memory?.usagePercent >= this.config.thresholds.memory.warning) {
            alerts.push({
                type: 'warning',
                message: `High memory usage: ${Math.round(data.memory.usagePercent)}%`,
                timestamp: new Date()
            });
        }
        
        // Check Storage
        if (data.storage?.usagePercent >= this.config.thresholds.storage.critical) {
            alerts.push({
                type: 'critical',
                message: `Critical storage usage: ${Math.round(data.storage.usagePercent)}%`,
                timestamp: new Date()
            });
        } else if (data.storage?.usagePercent >= this.config.thresholds.storage.warning) {
            alerts.push({
                type: 'warning',
                message: `High storage usage: ${Math.round(data.storage.usagePercent)}%`,
                timestamp: new Date()
            });
        }
        
        // Check Network
        if (data.network?.usagePercent >= this.config.thresholds.network.critical) {
            alerts.push({
                type: 'critical',
                message: `Critical network usage: ${Math.round(data.network.usagePercent)}%`,
                timestamp: new Date()
            });
        } else if (data.network?.usagePercent >= this.config.thresholds.network.warning) {
            alerts.push({
                type: 'warning',
                message: `High network usage: ${Math.round(data.network.usagePercent)}%`,
                timestamp: new Date()
            });
        }
        
        // Add new alerts
        alerts.forEach(alert => this.addAlert(alert));
    }

    addAlert(alert) {
        // Avoid duplicate alerts
        const existing = this.alerts.find(a => 
            a.message === alert.message && 
            Date.now() - a.timestamp.getTime() < 60000 // Within 1 minute
        );
        
        if (existing) return;
        
        this.alerts.unshift(alert);
        
        // Limit alerts
        if (this.alerts.length > 50) {
            this.alerts = this.alerts.slice(0, 50);
        }
        
        this.updateAlertsDisplay();
        this.showNotification(alert.message, alert.type);
    }

    updateAlertsDisplay() {
        const container = document.getElementById('alerts-container');
        if (!container) return;
        
        if (this.alerts.length === 0) {
            container.innerHTML = '<div class="alert-placeholder">No alerts</div>';
            return;
        }
        
        const alertsHtml = this.alerts.slice(0, 10).map(alert => `
            <div class="alert alert-${alert.type}">
                <div class="alert-icon">
                    <i class="${this.getAlertIcon(alert.type)}"></i>
                </div>
                <div class="alert-content">
                    <div class="alert-message">${alert.message}</div>
                    <div class="alert-timestamp">${this.formatTime(alert.timestamp)}</div>
                </div>
            </div>
        `).join('');
        
        container.innerHTML = alertsHtml;
    }

    getAlertIcon(type) {
        switch (type) {
            case 'critical': return 'fas fa-exclamation-triangle';
            case 'warning': return 'fas fa-exclamation-circle';
            case 'info': return 'fas fa-info-circle';
            default: return 'fas fa-bell';
        }
    }

    updateSessionInfo(data) {
        const sessionTimeElement = document.getElementById('session-time');
        const optimizationsElement = document.getElementById('optimizations-count');
        const reliabilityElement = document.getElementById('reliability-score');
        
        if (sessionTimeElement && data.session?.duration) {
            sessionTimeElement.textContent = this.formatDuration(data.session.duration);
        }
        
        if (optimizationsElement && data.session?.optimizations) {
            optimizationsElement.textContent = data.session.optimizations;
        }
        
        if (reliabilityElement && data.health?.reliability) {
            reliabilityElement.textContent = `${Math.round(data.health.reliability)}%`;
        }
    }

    updateConnectionStatus(connected) {
        const statusElement = document.getElementById('connection-status');
        if (statusElement) {
            statusElement.className = `connection-status ${connected ? 'connected' : 'disconnected'}`;
            statusElement.innerHTML = `
                <i class="fas fa-${connected ? 'wifi' : 'wifi-slash'}"></i>
                ${connected ? 'Connected' : 'Disconnected'}
            `;
        }
    }

    showLoadingState(show) {
        const loadingElement = document.getElementById('loading-overlay');
        if (loadingElement) {
            loadingElement.style.display = show ? 'flex' : 'none';
        }
    }

    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-icon">
                <i class="${this.getAlertIcon(type)}"></i>
            </div>
            <div class="notification-message">${message}</div>
            <div class="notification-close">
                <i class="fas fa-times"></i>
            </div>
        `;
        
        // Add to container
        const container = document.getElementById('notifications-container') || 
                         this.createNotificationsContainer();
        container.appendChild(notification);
        
        // Add close handler
        notification.querySelector('.notification-close').addEventListener('click', () => {
            notification.remove();
        });
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    createNotificationsContainer() {
        const container = document.createElement('div');
        container.id = 'notifications-container';
        container.className = 'notifications-container';
        document.body.appendChild(container);
        return container;
    }

    async toggleEmergencyMode() {
        try {
            const response = await fetch('/api/performance/emergency-mode', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ enable: true })
            });
            
            if (response.ok) {
                this.showNotification('Emergency mode activated', 'warning');
            } else {
                throw new Error('Failed to activate emergency mode');
            }
        } catch (error) {
            console.error('Error toggling emergency mode:', error);
            this.showNotification('Failed to activate emergency mode', 'error');
        }
    }

    async triggerOptimization() {
        try {
            const response = await fetch('/api/performance/optimize', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ aggressive: true })
            });
            
            if (response.ok) {
                this.showNotification('Performance optimization triggered', 'success');
            } else {
                throw new Error('Failed to trigger optimization');
            }
        } catch (error) {
            console.error('Error triggering optimization:', error);
            this.showNotification('Failed to trigger optimization', 'error');
        }
    }

    showSettings() {
        // Implementation for settings modal
        this.showNotification('Settings panel coming soon', 'info');
    }

    forceRefresh() {
        this.requestMetricsUpdate();
        this.showNotification('Metrics refreshed', 'info');
    }

    formatTime(date) {
        return date.toLocaleTimeString('en-US', {
            hour12: false,
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    }

    formatDuration(milliseconds) {
        const seconds = Math.floor(milliseconds / 1000);
        const minutes = Math.floor(seconds / 60);
        const hours = Math.floor(minutes / 60);
        
        if (hours > 0) {
            return `${hours}h ${minutes % 60}m ${seconds % 60}s`;
        } else if (minutes > 0) {
            return `${minutes}m ${seconds % 60}s`;
        } else {
            return `${seconds}s`;
        }
    }

    destroy() {
        this.stopMonitoring();
        
        if (this.websocket) {
            this.websocket.close();
        }
        
        Object.values(this.charts).forEach(chart => {
            if (chart) chart.destroy();
        });
        
        this.isInitialized = false;
        console.log('Performance monitor destroyed');
    }
}

// Global instance
let performanceMonitor = null;

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', async () => {
    try {
        performanceMonitor = new MedicalPerformanceMonitor();
        await performanceMonitor.initialize();
    } catch (error) {
        console.error('Failed to initialize performance monitor:', error);
    }
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (performanceMonitor) {
        performanceMonitor.destroy();
    }
});

// Export for external use
window.MedicalPerformanceMonitor = MedicalPerformanceMonitor;