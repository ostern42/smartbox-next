# SmartBoxNext Configuration Test Checklist

## Test Environment
- [ ] Visual Studio Debug Mode
- [ ] Standalone Execution (after build)

## Settings Dialog Tests

### 1. Opening Settings
- [ ] Click Settings button in main UI
- [ ] Settings modal opens with iframe
- [ ] All sections are visible (Storage, PACS, Video, Application)
- [ ] Navigation between sections works

### 2. Browse Folder Buttons
- [ ] **Photos Path**: Click browse → Folder picker opens → Select folder → Path updates
- [ ] **Videos Path**: Click browse → Folder picker opens → Select folder → Path updates  
- [ ] **DICOM Path**: Click browse → Folder picker opens → Select folder → Path updates
- [ ] **Temp Path**: Click browse → Folder picker opens → Select folder → Path updates

### 3. Configuration Save/Load
- [ ] Modify any field value
- [ ] Click Save button
- [ ] Save button shows "Saved!" confirmation
- [ ] Close and reopen settings
- [ ] Verify saved values persist

### 4. PACS Connection Test
- [ ] Enter PACS server details
- [ ] Click "Test Connection" button
- [ ] Button shows "Testing..." state
- [ ] Success: Green checkmark appears
- [ ] Failure: Red X and error message

### 5. Touch Keyboard Integration
- [ ] Click on any text input field
- [ ] Touch keyboard appears
- [ ] Numeric keyboard for IP/Port fields
- [ ] QWERTZ keyboard for text fields
- [ ] AltGr+ß produces backslash

### 6. Data Validation
- [ ] Port numbers: Only accept valid range (1-65535)
- [ ] IP Address: Valid format check
- [ ] Required fields: Cannot save with empty required fields
- [ ] Path validation: Warns if path doesn't exist

### 7. WebView2 Communication
- [ ] All buttons trigger C# actions
- [ ] C# responses update UI correctly
- [ ] No timeout errors
- [ ] Console shows proper message flow

## Main App Integration

### 8. Photo Capture
- [ ] Capture photo
- [ ] Photo saves to configured Photos path
- [ ] Log shows correct save location

### 9. Video Recording  
- [ ] Start/stop recording
- [ ] Video saves to configured Videos path
- [ ] Log shows correct save location

### 10. Open Logs Button
- [ ] Click "Open Logs" button
- [ ] Windows Explorer opens logs folder
- [ ] Daily log files are present

## Standalone Execution Tests

### 11. First Run Experience
- [ ] Delete config.json
- [ ] Run SmartBoxNext.exe
- [ ] Default config is created
- [ ] App starts normally

### 12. Portable Deployment
- [ ] Copy SmartBoxNext-Portable folder
- [ ] Run from different location
- [ ] All relative paths work correctly
- [ ] No hardcoded paths fail

## Known Issues to Verify
- [ ] WebView2 message timeout (Session 13)
- [ ] Standalone execution fails (Session 15)
- [ ] Settings style needs update to modern Windows Terminal look

## Test Results
Date: ___________
Tester: ___________
Build Version: ___________

### Summary
- Tests Passed: ___/___
- Issues Found:
  1. 
  2. 
  3. 

### Notes: