/**
 * Debug Overlay for SmartBox Touch Interface
 * Shows status and helps identify issues
 */

class DebugOverlay {
    constructor() {
        this.createOverlay();
        this.startMonitoring();
    }

    createOverlay() {
        const overlay = document.createElement('div');
        overlay.id = 'debugOverlay';
        overlay.style.cssText = `
            position: fixed;
            top: 90px;
            left: 10px;
            background: rgba(0,0,0,0.8);
            color: white;
            padding: 10px;
            border-radius: 8px;
            font-family: monospace;
            font-size: 11px;
            z-index: 10000;
            max-width: 250px;
            pointer-events: none;
            opacity: 0.7;
        `;
        
        document.body.appendChild(overlay);
        this.overlay = overlay;
    }

    updateStatus() {
        const mwlCards = document.querySelectorAll('.patient-card');
        const webcamVideo = document.getElementById('webcamPreviewSmall');
        const emergencyArea = document.getElementById('emergencySwipe');
        const pullIndicator = document.getElementById('pullToRefresh');
        
        const status = {
            'MWL Cards': mwlCards.length,
            'WebCam Video': webcamVideo ? (webcamVideo.srcObject ? 'Connected' : 'No Source') : 'Not Found',
            'Emergency Area': emergencyArea ? 'Found' : 'Missing',
            'Pull Indicator': pullIndicator ? 'Found' : 'Missing',
            'Touch Support': 'ontouchstart' in window ? 'Yes' : 'No',
            'Screen Size': `${window.innerWidth}x${window.innerHeight}`,
            'User Agent': navigator.userAgent.includes('WebView') ? 'WebView2' : 'Browser'
        };
        
        const html = Object.entries(status)
            .map(([key, value]) => `<div><strong>${key}:</strong> ${value}</div>`)
            .join('');
            
        this.overlay.innerHTML = `<h4 style="margin:0 0 8px 0;">SmartBox Debug</h4>${html}`;
    }

    startMonitoring() {
        // Update every 2 seconds
        setInterval(() => {
            this.updateStatus();
        }, 2000);
        
        // Initial update
        setTimeout(() => this.updateStatus(), 1000);
    }
}

// Auto-start debug overlay
document.addEventListener('DOMContentLoaded', () => {
    new DebugOverlay();
});