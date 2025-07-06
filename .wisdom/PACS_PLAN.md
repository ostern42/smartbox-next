# PACS Integration Plan for SmartBox-Next

## Learning from CamBridge v1

### Key Components
1. **DicomStoreService** - Handles C-STORE operations
2. **PacsConfiguration** - Config model with:
   - Host & Port (default 104)
   - CalledAeTitle (PACS server)
   - CallingAeTitle (our app, default "CAMBRIDGE")
   - Timeout (default 30s)
   - Retry settings

3. **PacsUploadQueue** - Per-pipeline queue (**WICHTIG: Wir brauchen das!**)

### DICOM C-STORE Protocol
- Standard way to send DICOM to PACS
- Requires proper AE Title configuration
- Returns status codes (Success, Warning, Failure)
- Needs error handling for network issues

### CamBridge uses fo-dicom (C#)
We need Go equivalent:
- suyashkumar/dicom (has networking)
- or manual implementation

## SmartBox-Next Implementation Plan

### Phase 1: Basic C-STORE (MVP)
1. Add PACS config to app.go
2. Implement simple C-STORE client
3. Add "Send to PACS" button
4. Basic error handling

### Phase 2: Queue System (**CRITICAL**)
1. **Local queue** for failed uploads
2. **Persistent storage** (survive restarts)
3. **Remote management** capability
4. **Status monitoring** (pending/failed/success)
5. **Retry mechanism** with backoff

### Phase 3: Emergency Patient Features (**WICHTIG**)
1. **Emergency Templates**:
   - "Notfall männlich" (current date/time)
   - "Notfall weiblich" (current date/time)
   - "Notfall Kind" (with age estimate)
   - Quick access button on main screen

2. **On-Screen Keyboard** (der Spaß!):
   - Touch-optimized layout
   - German special characters (ä, ö, ü, ß)
   - Quick input for:
     - Name fields
     - Birth date (date picker?)
     - Patient ID
   - Auto-complete for common names?

### Phase 4: UI Integration
1. PACS settings in UI
2. Connection test (C-ECHO)
3. Upload queue viewer
4. Emergency patient button
5. Error messages

### Phase 5: Advanced Features
1. Worklist integration
2. Multiple PACS servers
3. Queue priority management
4. Offline mode with sync

## Technical Notes
- Port 104 is standard DICOM port
- AE Titles are like usernames (max 16 chars)
- Orthanc is good for testing (Docker available)
- Queue needs SQLite or similar for persistence
- On-screen keyboard needs careful UX design