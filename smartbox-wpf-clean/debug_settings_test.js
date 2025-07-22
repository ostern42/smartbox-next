// Settings Debug Script - kopiere in F12 Konsole
console.log('🔍 SETTINGS DEBUG TEST STARTING...');

// 1. Check current values
console.log('=== 1. CURRENT VALUES ===');
const testFields = [
    'storage-photos-path',
    'pacs-server-host', 
    'pacs-enabled',
    'video-default-frame-rate'
];

testFields.forEach(id => {
    const input = document.getElementById(id);
    if (input) {
        const value = input.type === 'checkbox' ? input.checked : input.value;
        console.log(`${id}: ${value}`);
    }
});

// 2. Set unique test values
console.log('=== 2. SETTING TEST VALUES ===');
const testId = Date.now();
const testData = {
    'storage-photos-path': `./TestPath_${testId}`,
    'pacs-server-host': `192.168.${testId % 100}.${testId % 255}`,
    'pacs-enabled': true,
    'video-default-frame-rate': (testId % 50) + 15
};

Object.entries(testData).forEach(([id, value]) => {
    const input = document.getElementById(id);
    if (input) {
        if (input.type === 'checkbox') {
            input.checked = value;
        } else {
            input.value = value;
        }
        console.log(`SET ${id} = ${value}`);
    }
});

// 3. Test save button action
console.log('=== 3. TESTING SAVE ACTION ===');
const saveButton = document.querySelector('[data-action="savesettings"]');
if (saveButton) {
    console.log('Save button found, clicking...');
    saveButton.click();
} else {
    console.error('Save button not found!');
}

// 4. Check what was collected
setTimeout(() => {
    console.log('=== 4. CHECK COLLECTED DATA ===');
    const form = document.getElementById('settingsForm');
    if (window.actionHandler && window.actionHandler.collectSettingsFormData) {
        const collected = window.actionHandler.collectSettingsFormData(form);
        console.log('Collected config:', collected);
        
        // Check if our test values are in there
        console.log('=== TEST VALUE VERIFICATION ===');
        const photosPath = collected.Storage?.PhotosPath;
        const pacsHost = collected.Pacs?.ServerHost;
        console.log(`PhotosPath: expected="${testData['storage-photos-path']}" actual="${photosPath}"`);
        console.log(`PACS Host: expected="${testData['pacs-server-host']}" actual="${pacsHost}"`);
        
        if (photosPath === testData['storage-photos-path']) {
            console.log('✅ PhotosPath collected correctly');
        } else {
            console.log('❌ PhotosPath collection failed');
        }
        
        if (pacsHost === testData['pacs-server-host']) {
            console.log('✅ PACS Host collected correctly');
        } else {
            console.log('❌ PACS Host collection failed');
        }
    } else {
        console.error('actionHandler.collectSettingsFormData not available');
    }
}, 1000);

// 5. Store test data for later verification
localStorage.setItem('settingsTestData', JSON.stringify(testData));
console.log('Test data stored in localStorage for verification after reload');
console.log('To verify persistence: run checkSettingsAfterReload() after navigating away and back');

// Function to check after reload
window.checkSettingsAfterReload = function() {
    const stored = JSON.parse(localStorage.getItem('settingsTestData') || '{}');
    console.log('=== PERSISTENCE CHECK ===');
    
    Object.entries(stored).forEach(([id, expectedValue]) => {
        const input = document.getElementById(id);
        if (input) {
            const actualValue = input.type === 'checkbox' ? input.checked : input.value;
            const matches = actualValue == expectedValue;
            console.log(`${matches ? '✅' : '❌'} ${id}: expected="${expectedValue}" actual="${actualValue}"`);
        }
    });
    
    localStorage.removeItem('settingsTestData');
};

console.log('🔍 DEBUG TEST SETUP COMPLETE - Check results above');