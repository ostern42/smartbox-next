# SmartBox Next - C#/JavaScript API Interface Documentation
**Version**: 2.0.0  
**Generated**: 2025-07-22  
**Purpose**: Comprehensive mapping of WebView2 communication interface for refactoring and optimization

## 🔌 WebView2 Communication Architecture

### Message Flow
```
JavaScript (Frontend) ←→ WebView2 Bridge ←→ C# (Backend)
```

### JavaScript → C# Protocol
```javascript
window.chrome.webview.postMessage(JSON.stringify({
    action: "actionName",    // Required: C# handler identifier
    data: { ... },          // Optional: Action-specific payload
    timestamp: "ISO-8601"   // Optional: Client timestamp
}));
```

### C# → JavaScript Protocol
```csharp
webView.CoreWebView2.PostWebMessageAsString(JsonSerializer.Serialize(new {
    type = "responseType",   // Required: Response identifier
    success = true/false,    // Required: Operation status
    data = new { ... },     // Optional: Response payload
    message = "...",        // Optional: Success message
    error = "..."           // Optional: Error details
}));
```

## 📊 Message Type Mapping Tables

### JavaScript → C# Actions

| JS Action | C# Handler | Data Structure | Response Type | Status |
|-----------|------------|----------------|---------------|---------|
| `saveSettings` | `HandleSaveSettings()` | `{ data: AppConfig }` | `settingsSaved` | ✅ Implemented |
| `getSettings` | `HandleGetSettings()` | None | `settingsLoaded` | ✅ Implemented |
| `sendToPacs` | `HandleSendToPacs()` | None | `pacsSent` | ✅ Implemented |
| `runDiagnostics` | `HandleRunDiagnostics()` | None | `diagnosticsComplete` | ✅ Implemented |
| `validateDicom` | `HandleValidateDicom()` | None | `dicomValidated` | ✅ Implemented |
| `loadWorklist` / `loadMWL` | `HandleLoadWorklist()` | None | `worklistLoaded` | ✅ Implemented |
| `exitApplication` / `exitApp` | `HandleExitApplication()` | None | None (app closes) | ✅ Implemented |
| `openSettings` | `HandleOpenSettings()` | None | None | ⚠️ Stub |
| `capturePhoto` | `HandleCapturePhoto()` | `{ captureId, imageData, patient }` | `photoCaptured` | ✅ Implemented |
| `captureVideo` | `HandleCaptureVideo()` | `{ captureId, videoBlob, duration, patient }` | `videoCaptured` | ✅ Implemented |
| `exportCaptures` | `HandleExportCaptures()` | `{ captures[] }` | `capturesExported` | ⚠️ Partial |
| `testpacsconnection` | `HandleTestPacsConnection()` | None | `pacsTestResult` | ✅ Implemented |
| `testmwlconnection` | `HandleTestMwlConnection()` | None | `mwlTestResult` | ✅ Implemented |

### C# → JavaScript Responses

| Response Type | Trigger Action | Data Structure | UI Update Target |
|---------------|----------------|----------------|------------------|
| `settingsLoaded` | `getSettings` | `{ data: AppConfig }` | Settings form fields |
| `settingsSaved` | `saveSettings` | `{ success, message/error }` | Status notification |
| `pacsSent` | `sendToPacs` | `{ success, message/error }` | DICOM status panel |
| `diagnosticsComplete` | `runDiagnostics` | `{ data: { systemHealth, memory, disk, network } }` | Diagnostics panel |
| `dicomValidated` | `validateDicom` | `{ data: { totalFiles, validFiles, compliance } }` | DICOM status panel |
| `worklistLoaded` | `loadWorklist` | `{ data: WorklistItem[] }` | Worklist display |
| `photoCaptured` | `capturePhoto` | `{ captureId, fileName, timestamp }` | Capture status |
| `videoCaptured` | `captureVideo` | `{ captureId, fileName, duration, timestamp }` | Capture status |
| `pacsTestResult` | `testpacsconnection` | `{ success, message, error }` | Test result modal |
| `mwlTestResult` | `testmwlconnection` | `{ success, message, data: { worklistCount } }` | Test result modal |
| `testResult` | Various tests | `{ service, success, message }` | Generic test modal |

## 🔧 Property Mapping Tables

### AppConfig Settings Mapping

| UI Element ID | Config Path | Type | Default | Validation |
|---------------|-------------|------|---------|------------|
| **Storage Section** |
| `storage-photos-path` | `Storage.PhotosPath` | string | "./Photos" | Directory path |
| `storage-videos-path` | `Storage.VideosPath` | string | "./Videos" | Directory path |
| `storage-dicom-path` | `Storage.DicomPath` | string | "./DicomOutput" | Directory path |
| `storage-temp-path` | `Storage.TempPath` | string | "./Temp" | Directory path |
| `storage-enable-auto-cleanup` | `Storage.EnableAutoCleanup` | bool | false | - |
| `storage-retention-days` | `Storage.RetentionDays` | int | 30 | 1-365 |
| `storage-compress-old-files` | `Storage.CompressOldFiles` | bool | false | - |
| **PACS Section** |
| `pacs-server-host` | `Pacs.ServerHost` | string | "192.168.1.100" | IP/hostname |
| `pacs-server-port` | `Pacs.ServerPort` | int | 11112 | 1-65535 |
| `pacs-called-ae-title` | `Pacs.CalledAeTitle` | string | "PACS" | 1-16 chars |
| `pacs-calling-ae-title` | `Pacs.CallingAeTitle` | string | "SMARTBOX" | 1-16 chars |
| `pacs-timeout` | `Pacs.Timeout` | int | 30 | 5-300 seconds |
| `pacs-use-secure-connection` | `Pacs.UseSecureConnection` | bool | false | - |
| `pacs-max-retries` | `Pacs.MaxRetries` | int | 3 | 0-10 |
| `pacs-auto-send-on-capture` | `Pacs.AutoSendOnCapture` | bool | false | - |
| `pacs-send-in-background` | `Pacs.SendInBackground` | bool | true | - |
| **MWL Section** |
| `mwl-enable-worklist` | `MwlSettings.EnableWorklist` | bool | true | - |
| `mwl-server-host` | `MwlSettings.MwlServerHost` | string | "192.168.1.100" | IP/hostname |
| `mwl-server-port` | `MwlSettings.MwlServerPort` | int | 105 | 1-65535 |
| `mwl-server-aet` | `MwlSettings.MwlServerAET` | string | "ORTHANC" | 1-16 chars |
| `mwl-local-aet` | `MwlSettings.LocalAET` | string | "SMARTBOX" | 1-16 chars |
| `mwl-cache-expiry-hours` | `MwlSettings.CacheExpiryHours` | int | 24 | 1-168 |
| `mwl-auto-refresh-seconds` | `MwlSettings.AutoRefreshSeconds` | int | 300 | 60-3600 |
| `mwl-show-emergency-first` | `MwlSettings.ShowEmergencyFirst` | bool | true | - |
| **Video Section** |
| `video-max-recording-minutes` | `Video.MaxRecordingMinutes` | int | 30 | 1-60 |
| `video-codec` | `Video.VideoCodec` | string | "h264" | h264/vp8/vp9 |
| `video-bitrate` | `Video.VideoBitrate` | int | 5000000 | 1M-50M |
| `video-framerate` | `Video.VideoFramerate` | int | 30 | 15-60 |
| `video-resolution` | `Video.VideoResolution` | string | "1920x1080" | WxH format |
| `video-enable-audio-capture` | `Video.EnableAudioCapture` | bool | true | - |
| `video-audio-bitrate` | `Video.AudioBitrate` | int | 128000 | 64k-320k |
| **DICOM Section** |
| `dicom-station-name` | `Dicom.StationName` | string | "SMARTBOX-ED" | 1-64 chars |
| `dicom-ae-title` | `Dicom.AeTitle` | string | "SMARTBOX" | 1-16 chars |
| `dicom-modality` | `Dicom.Modality` | string | "XC" | XC/ES/OT |
| `dicom-output-directory` | `Dicom.OutputDirectory` | string | "./DicomOutput" | Directory path |
| `dicom-patient-id-prefix` | `Dicom.PatientIdPrefix` | string | "SB" | 1-8 chars |

## 🔄 Data Structure Definitions

### WorklistItem Structure
```typescript
interface WorklistItem {
    patientId: string;
    patientName: string;
    patientBirthDate?: string;
    patientSex?: string;
    studyInstanceUID: string;
    scheduledProcedureStepStartDate?: string;
    scheduledProcedureStepStartTime?: string;
    modality?: string;
    scheduledStationAETitle?: string;
    scheduledProcedureStepDescription?: string;
    scheduledStationName?: string;
    scheduledProcedureStepLocation?: string;
    accessionNumber?: string;
    requestingPhysician?: string;
    referringPhysicianName?: string;
    studyDescription?: string;
    procedureCodeSequence?: any;
    admissionID?: string;
    currentPatientLocation?: string;
}
```

### Patient Data Structure
```typescript
interface PatientInfo {
    id: string;
    name: string;
    birthDate?: string;
    gender?: string;
    accessionNumber?: string;
    studyDescription?: string;
    referringPhysician?: string;
}
```

### Capture Data Structures
```typescript
interface PhotoCaptureData {
    captureId: string;
    imageData: string;  // Base64 encoded JPEG
    patient?: PatientInfo;
    timestamp: string;
    metadata?: {
        width: number;
        height: number;
        quality: number;
    };
}

interface VideoCaptureData {
    captureId: string;
    videoBlob: string;  // Base64 encoded WebM
    duration: number;   // Seconds
    patient?: PatientInfo;
    timestamp: string;
    metadata?: {
        width: number;
        height: number;
        framerate: number;
        codec: string;
    };
}
```

## 🚧 Refactoring Opportunities

### 1. Naming Consistency Issues
| Current | Recommended | Location |
|---------|-------------|----------|
| `opensettings` | `openSettings` | Message handler |
| `savesettings` | `saveSettings` | Message handler |
| `testpacsconnection` | `testPacsConnection` | Action name |
| Mixed `action`/`type` | Standardize on `action` | Request format |

### 2. Response Format Standardization
```javascript
// Recommended unified response format
{
    action: "originalAction",     // Echo original action
    type: "responseType",        // Response type identifier
    success: boolean,            // Operation status
    data: {},                   // Optional payload
    message: string,            // User-friendly message
    error: string,              // Error details if failed
    timestamp: "ISO-8601"       // Server timestamp
}
```

### 3. Error Handling Improvements
```javascript
// Current: Inline error strings
webView.CoreWebView2.PostWebMessageAsString(
    "{\"type\":\"error\",\"error\":\"" + ex.Message + "\"}"
);

// Recommended: Structured error object
SendResponse(new ErrorResponse {
    Action = originalAction,
    Type = "error",
    Success = false,
    Error = new {
        Code = "PACS_CONNECTION_FAILED",
        Message = ex.Message,
        Details = ex.InnerException?.Message,
        Timestamp = DateTime.UtcNow
    }
});
```

### 4. Message Validation Layer
```javascript
// Add medical safety validation before sending
class MessageSafetyValidator {
    static validateAction(action) {
        const validActions = [
            'saveSettings', 'getSettings', 'sendToPacs',
            'runDiagnostics', 'validateDicom', 'loadWorklist',
            'capturePhoto', 'captureVideo', 'exportCaptures'
        ];
        return validActions.includes(action);
    }
    
    static validatePatientData(patient) {
        // HIPAA compliance validation
        return patient && patient.id && patient.id.length >= 3;
    }
}
```

## 🔐 Security Considerations

### Message Security
1. **Input Validation**: All incoming messages must be validated
2. **Data Sanitization**: HTML escape all user inputs
3. **Size Limits**: Enforce maximum message size (1MB)
4. **Rate Limiting**: Prevent message flooding

### Patient Data Protection
1. **Encryption**: Use AES-256 for sensitive data in messages
2. **Audit Logging**: Log all patient data access
3. **Session Management**: Enforce 30-minute timeout
4. **Access Control**: Implement role-based permissions

## 📈 Performance Optimization

### Message Batching
```javascript
// Batch multiple operations
{
    action: "batchOperation",
    operations: [
        { action: "saveSettings", data: {...} },
        { action: "validateDicom", data: {...} },
        { action: "sendToPacs", data: {...} }
    ]
}
```

### Caching Strategy
- Cache settings for 5 minutes
- Cache worklist for configured expiry
- Cache DICOM validation results
- Invalidate on data changes

### Async Operations
- Use async/await for all I/O operations
- Implement progress reporting for long operations
- Support operation cancellation

## 🔄 Migration Strategy

### Phase 1: Standardization
1. Unify action/type naming
2. Standardize response formats
3. Add validation layer

### Phase 2: Enhancement
1. Implement batching
2. Add caching layer
3. Improve error handling

### Phase 3: Optimization
1. Reduce message size
2. Implement compression
3. Add performance monitoring

---
*This documentation provides a complete reference for the C#/JavaScript interface in SmartBox Next, enabling efficient refactoring and feature development.*