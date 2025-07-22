# Settings Persistence Debug Guide

## 🔍 Complete F12 Console Test Suite

### 1. Check Current Config Path
```javascript
// See which config file is being used
console.log('=== CONFIG PATH TEST ===');
window.chrome.webview.postMessage(JSON.stringify({
    action: 'getsettings',
    data: {}
}));
```

### 2. Test Save/Load Cycle
```javascript
// Step 1: Save a unique test value
console.log('=== SAVE TEST ===');
const testId = 'TEST_' + Date.now();
document.getElementById('storage-photos-path').value = testId;

// Trigger save
document.querySelector('[data-action="savesettings"]').click();

// Step 2: Wait and reload settings
setTimeout(() => {
    console.log('=== RELOAD TEST ===');
    window.settingsManager.loadSettings();
}, 2000);

// Step 3: Check if value persisted
setTimeout(() => {
    const newValue = document.getElementById('storage-photos-path').value;
    console.log('Test ID saved:', testId);
    console.log('Value after reload:', newValue);
    console.log('PERSISTENCE TEST:', newValue === testId ? '✅ PASSED' : '❌ FAILED');
}, 3000);
```

### 3. Monitor Save Process
```javascript
// Intercept and log all host messages
const originalPost = window.chrome.webview.postMessage;
window.chrome.webview.postMessage = function(msg) {
    console.log('📤 TO HOST:', JSON.parse(msg));
    originalPost.call(this, msg);
};

// Log all received messages
window.addEventListener('message', (e) => {
    console.log('📥 FROM HOST:', e.data);
});
```

### 4. Check Form Data Collection
```javascript
// Test the data collection
const form = document.getElementById('settingsForm');
const config = window.actionHandler.collectSettingsFormData(form);
console.log('Collected config:', JSON.stringify(config, null, 2));
```

### 5. Simulate Complete Workflow
```javascript
async function testFullWorkflow() {
    console.log('🧪 STARTING FULL WORKFLOW TEST');
    
    // 1. Get current settings
    console.log('1️⃣ Getting current settings...');
    window.settingsManager.loadSettings();
    
    await new Promise(resolve => setTimeout(resolve, 1000));
    
    // 2. Modify multiple values
    console.log('2️⃣ Modifying values...');
    const testData = {
        'storage-photos-path': './TestPhotos_' + Date.now(),
        'pacs-server-host': '192.168.99.99',
        'pacs-enabled': true,
        'video-default-frame-rate': '25'
    };
    
    for (const [id, value] of Object.entries(testData)) {
        const input = document.getElementById(id);
        if (input) {
            if (input.type === 'checkbox') {
                input.checked = value;
            } else {
                input.value = value;
            }
            console.log(`  Set ${id} = ${value}`);
        }
    }
    
    // 3. Save
    console.log('3️⃣ Saving...');
    document.querySelector('[data-action="savesettings"]').click();
    
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    // 4. Reload page (simulate app restart)
    console.log('4️⃣ Reloading page...');
    console.log('❗ After reload, run: checkPersistedValues()');
    
    // Store test data for after reload
    localStorage.setItem('testData', JSON.stringify(testData));
    
    setTimeout(() => location.reload(), 1000);
}

// Run after page reload
function checkPersistedValues() {
    const testData = JSON.parse(localStorage.getItem('testData'));
    console.log('🔍 CHECKING PERSISTED VALUES');
    
    let passed = 0;
    let failed = 0;
    
    for (const [id, expectedValue] of Object.entries(testData)) {
        const input = document.getElementById(id);
        if (input) {
            const actualValue = input.type === 'checkbox' ? input.checked : input.value;
            const matches = actualValue == expectedValue;
            
            console.log(`${matches ? '✅' : '❌'} ${id}: expected="${expectedValue}" actual="${actualValue}"`);
            
            if (matches) passed++;
            else failed++;
        }
    }
    
    console.log(`\n📊 RESULTS: ${passed} passed, ${failed} failed`);
    localStorage.removeItem('testData');
}

// Start test
testFullWorkflow();
```

## 🐛 Common Issues & Solutions

### Issue 1: Multiple config.json files
```powershell
# Find all config.json files
Get-ChildItem -Path . -Filter config.json -Recurse | Select FullName, LastWriteTime
```

### Issue 2: File permissions
```powershell
# Check if config.json is read-only
Get-ItemProperty .\bin\Debug\net8.0-windows\config.json | Select IsReadOnly, LastWriteTime
```

### Issue 3: Working directory mismatch
```javascript
// In F12, check where the app thinks it's running from
console.log('Expected config location: AppDomain.CurrentDomain.BaseDirectory + config.json');
```

## 🔧 Quick Fixes to Try

1. **Delete all config.json files except one**
2. **Make config.json NOT read-only**
3. **Run app directly from exe (not VS)**
4. **Check Windows Event Viewer for file access errors**

## 📝 Manual Verification Steps

1. Open `bin\Debug\net8.0-windows\config.json` in Notepad
2. Change a value manually (e.g., `"PhotosPath": "./MANUAL_TEST"`)
3. Save the file
4. Start the app
5. Open Settings
6. Check if your manual change appears

If manual changes appear → Save process is broken
If manual changes don't appear → Load process is broken