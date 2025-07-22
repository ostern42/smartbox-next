// Debug wrapper to find the exact error
window.addEventListener('DOMContentLoaded', () => {
    console.log('DOM Content Loaded');
    
    // Check if all required elements exist
    const elements = {
        webcamPreview: document.getElementById('webcamPreview'),
        captureCanvas: document.getElementById('captureCanvas'),
        webcamPlaceholder: document.getElementById('webcamPlaceholder'),
        captureButton: document.getElementById('captureButton'),
        recordButton: document.getElementById('recordButton'),
        exportDicomButton: document.getElementById('exportDicomButton'),
        settingsButton: document.getElementById('settingsButton'),
        debugButton: document.getElementById('debugButton'),
        analyzeButton: document.getElementById('analyzeButton')
    };
    
    console.log('Elements found:', elements);
    
    // Find missing elements
    for (const [name, element] of Object.entries(elements)) {
        if (!element) {
            console.error(`Missing element: ${name}`);
        }
    }
    
    // Try to initialize the app
    try {
        window.app = new SmartBoxApp();
        console.log('SmartBoxApp initialized successfully');
        
        // Test WebView2 communication
        if (window.chrome && window.chrome.webview) {
            console.log('WebView2 is available, sending test message...');
            window.chrome.webview.postMessage(JSON.stringify({
                action: 'test',
                data: { message: 'Hello from JavaScript!' }
            }));
        } else {
            console.error('WebView2 is NOT available!');
        }
    } catch (error) {
        console.error('Failed to initialize SmartBoxApp:', error);
        console.error('Stack trace:', error.stack);
    }
    
    // Add click listeners to all buttons for debugging
    document.querySelectorAll('button').forEach(button => {
        button.addEventListener('click', (e) => {
            console.log(`Button clicked: ${button.id || button.textContent}`);
        });
    });
    
    // Specifically test the settings button
    const settingsBtn = document.getElementById('settingsButton');
    if (settingsBtn) {
        console.log('Adding special handler to settings button...');
        settingsBtn.addEventListener('click', () => {
            console.log('Settings button clicked - sending openSettings message');
            if (window.chrome && window.chrome.webview) {
                const msg = JSON.stringify({
                    action: 'openSettings',
                    data: {}
                });
                console.log('Sending message:', msg);
                window.chrome.webview.postMessage(msg);
            }
        });
    }
});