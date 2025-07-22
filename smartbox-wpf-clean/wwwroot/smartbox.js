// Check WebView2 on load
window.addEventListener('DOMContentLoaded', function() {
    console.log('SmartBox UI loaded');
    console.log('WebView2 available:', !!(window.chrome && window.chrome.webview));
    if (window.chrome && window.chrome.webview) {
        console.log('WebView2 postMessage type:', typeof window.chrome.webview.postMessage);
    }
});

// Tab Management
function showTab(tabName) {
    console.log('Switching to tab:', tabName);
    
    // Hide all tabs
    document.getElementById('mainTab').classList.add('hidden');
    document.getElementById('settingsTab').classList.add('hidden');
    document.getElementById('diagnosticsTab').classList.add('hidden');
    
    // Remove active class from all nav buttons
    var buttons = document.querySelectorAll('.nav-btn');
    for (var i = 0; i < buttons.length; i++) {
        buttons[i].classList.remove('active');
    }
    
    // Show selected tab
    var selectedTab = document.getElementById(tabName + 'Tab');
    if (selectedTab) {
        selectedTab.classList.remove('hidden');
        console.log('Tab shown:', tabName);
    } else {
        console.error('Tab not found:', tabName);
    }
    
    // Activate corresponding nav button using data attribute
    var activeButton = document.querySelector('[data-tab="' + tabName + '"]');
    if (activeButton) {
        activeButton.classList.add('active');
    }
}

// Main Tab Functions
function startCapture() {
    document.getElementById('captureStatus').innerHTML = '📸 Capture session started - Ready for imaging';
}

function takeSnapshot() {
    document.getElementById('captureStatus').innerHTML = '✅ Snapshot captured successfully';
}

function stopCapture() {
    document.getElementById('captureStatus').innerHTML = '⏹️ Capture session stopped';
}

function viewDicom() {
    document.getElementById('dicomStatus').innerHTML = '📁 DICOM files loaded - 15 studies found';
}

function exportDicom() {
    document.getElementById('dicomStatus').innerHTML = '📤 Sending to PACS server...';
    sendToBackend('sendToPacs');
}

function validateDicom() {
    document.getElementById('dicomStatus').innerHTML = '🔍 Validating DICOM files...';
    sendToBackend('validateDicom');
}

function loadWorklist() {
    document.getElementById('workflowStatus').innerHTML = '📋 Loading modality worklist...';
    sendToBackend('loadWorklist');
}

function selectPatient() {
    document.getElementById('workflowStatus').innerHTML = '👤 Patient: John Doe (ID: 12345) - Ready for imaging';
}

function clearPatient() {
    document.getElementById('workflowStatus').innerHTML = '🔄 Patient selection cleared';
}

// Backend Communication
function sendToBackend(action, data) {
    console.log('Sending to backend:', action, data);
    
    try {
        if (window.chrome && window.chrome.webview) {
            var message = JSON.stringify(Object.assign({ action: action }, data || {}));
            console.log('About to send message:', message);
            window.chrome.webview.postMessage(message);
            console.log('Message sent successfully');
        } else {
            console.error('WebView2 bridge not available');
            console.error('window.chrome:', window.chrome);
            console.error('window.chrome.webview:', window.chrome ? window.chrome.webview : 'chrome not defined');
            alert('Communication with backend not available. Please restart the application.');
        }
    } catch (error) {
        console.error('Error sending message to backend:', error);
        console.error('Error details:', error.message, error.stack);
        alert('Error communicating with backend: ' + error.message);
    }
}

// Listen for backend responses
window.addEventListener('message', function(event) {
    console.log('Message event received:', event);
    try {
        var response = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
        console.log('Parsed response:', response);
        handleBackendResponse(response);
    } catch (error) {
        console.error('Error parsing backend response:', error, 'Data:', event.data);
    }
});

function handleBackendResponse(response) {
    switch(response.type) {
        case 'settingsSaved':
            if (response.success) {
                document.getElementById('settingsStatus').innerHTML = '💾 Settings saved successfully to config.json!';
            } else {
                document.getElementById('settingsStatus').innerHTML = '❌ Failed to save settings: ' + response.error;
            }
            break;
        case 'pacsSent':
            if (response.success) {
                document.getElementById('dicomStatus').innerHTML = '✅ ' + response.message;
            } else {
                document.getElementById('dicomStatus').innerHTML = '❌ PACS send failed: ' + response.error;
            }
            break;
        case 'diagnosticsComplete':
            if (response.success) {
                var data = response.data;
                document.getElementById('diagnosticsStatus').innerHTML = 
                    '🔧 Diagnostics Complete (' + data.timestamp + '):<br>' +
                    '✅ System Health: ' + data.systemHealth + '<br>' +
                    '✅ Memory Usage: ' + data.memoryUsage + '<br>' +
                    '✅ Disk Space: ' + data.diskSpace + '<br>' +
                    '✅ Network: ' + data.network + '<br>' +
                    '✅ DICOM Service: ' + data.dicomService;
            } else {
                document.getElementById('diagnosticsStatus').innerHTML = '❌ Diagnostics failed: ' + response.error;
            }
            break;
        case 'dicomValidated':
            if (response.success) {
                var data = response.data;
                document.getElementById('dicomStatus').innerHTML = 
                    '✅ DICOM Validation Complete:<br>' +
                    '📁 Total Files: ' + data.totalFiles + '<br>' +
                    '✅ Valid Files: ' + data.validFiles + '<br>' +
                    '🏥 Compliance: ' + data.compliance;
            } else {
                document.getElementById('dicomStatus').innerHTML = '❌ DICOM validation failed: ' + response.error;
            }
            break;
        case 'worklistLoaded':
            if (response.success) {
                var patients = response.data;
                var worklistHtml = '📋 Modality Worklist Loaded (' + patients.length + ' studies):<br>';
                for (var i = 0; i < patients.length; i++) {
                    var p = patients[i];
                    worklistHtml += '👤 ' + p.patientName + ' (ID: ' + p.patientId + ') - ' + p.studyDescription + ' @ ' + p.scheduledTime + '<br>';
                }
                document.getElementById('workflowStatus').innerHTML = worklistHtml;
            } else {
                document.getElementById('workflowStatus').innerHTML = '❌ Worklist loading failed: ' + response.error;
            }
            break;
    }
}

// Settings Functions
function saveSettings() {
    document.getElementById('settingsStatus').innerHTML = '💾 Saving settings...';
    
    var settings = {
        stationName: document.getElementById('stationName').value,
        aeTitle: document.getElementById('aeTitle').value,
        outputDir: document.getElementById('outputDir').value,
        pacsServer: document.getElementById('pacsServer').value,
        pacsPort: document.getElementById('pacsPort').value,
        pacsAeTitle: document.getElementById('pacsAeTitle').value,
        imageQuality: document.getElementById('imageQuality').value,
        autoSave: document.getElementById('autoSave').checked
    };
    
    sendToBackend('saveSettings', { settings: settings });
}

function resetSettings() {
    document.getElementById('stationName').value = 'SMARTBOX-ED';
    document.getElementById('aeTitle').value = 'SMARTBOX';
    document.getElementById('outputDir').value = 'C:\\DicomOutput';
    document.getElementById('pacsServer').value = '192.168.1.100';
    document.getElementById('pacsPort').value = '11112';
    document.getElementById('pacsAeTitle').value = 'PACS';
    document.getElementById('imageQuality').value = 'medium';
    document.getElementById('autoSave').checked = true;
    document.getElementById('settingsStatus').innerHTML = '🔄 Settings reset to default values';
}

// Diagnostics Functions
function runDiagnostics() {
    document.getElementById('diagnosticsStatus').innerHTML = '🔧 Running system diagnostics...';
    sendToBackend('runDiagnostics');
}

function testPacsConnection() {
    document.getElementById('diagnosticsStatus').innerHTML = '📡 Testing PACS connection...';
    sendToBackend('runDiagnostics');
}

function checkDicomCompliance() {
    document.getElementById('diagnosticsStatus').innerHTML = '🏥 Checking DICOM compliance...';
    sendToBackend('validateDicom');
}

// Exit Application
function exitApplication() {
    console.log('Exit button clicked');
    if (confirm('Are you sure you want to exit SmartBox?')) {
        console.log('User confirmed exit');
        sendToBackend('exitApplication');
    } else {
        console.log('User cancelled exit');
    }
}