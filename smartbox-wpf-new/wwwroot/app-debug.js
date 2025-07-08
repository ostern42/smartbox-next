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
    } catch (error) {
        console.error('Failed to initialize SmartBoxApp:', error);
        console.error('Stack trace:', error.stack);
    }
});