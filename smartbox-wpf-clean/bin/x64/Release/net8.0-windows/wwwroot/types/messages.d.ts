/**
 * SmartBox-Next Medical Device Communication Interface
 * TypeScript definitions for C# <-> JavaScript WebView2 communication
 * 
 * MEDICAL SAFETY: These interfaces ensure type-safe communication
 * preventing silent failures that could impact patient care.
 */

// Base message structure
interface BaseMessage {
    action: string;
    data?: any;
    timestamp?: string;
}

// Outbound messages (JavaScript -> C#)
interface OutboundMessage extends BaseMessage {
    action: 
        // System controls (case-insensitive for safety)
        | "openSettings" | "opensettings"
        | "exitApp" | "exitapp" 
        | "openLogs"
        
        // Patient workflow
        | "queryWorklist" | "queryworklist"
        | "refreshworklist"
        | "selectworklistitem"
        | "getworklistcachestatus"
        
        // Medical capture actions
        | "photoCaptured"
        | "savePhoto"
        | "videoRecorded" 
        | "saveVideo"
        | "capturehighres"
        
        // Yuan capture system
        | "connectyuan"
        | "disconnectyuan"
        | "getyuaninputs" 
        | "selectyuaninput"
        | "setactivesource"
        | "getunifiedstatus"
        | "getcapturestats"
        
        // DICOM & PACS
        | "exportDicom"
        | "exportCaptures" | "exportcaptures"
        | "sendToPacs"
        | "testpacsconnection"
        | "testmwlconnection"
        
        // Settings management
        | "requestConfig"
        | "updateConfig"
        | "getsettings"
        | "savesettings"
        | "browsefolder"
        
        // Capture management
        | "deleteCapture" | "deletecapture"
        | "webcamInitialized"
        | "cameraAnalysis"
        
        // Diagnostics
        | "log"
        | "testWebView"
        
        // Simple commands
        | "ping"
        | "exit" 
        | "close";
}

// Inbound messages (C# -> JavaScript)
interface InboundMessage extends BaseMessage {
    type: 
        | "success"
        | "error" 
        | "test"
        | "showSettings"
        | "queueStatus"
        | "worklistResult"
        | "worklistRefreshResult"
        | "worklistCacheStatus"
        | "worklistItemSelected"
        | "yuanConnectionResult"
        | "yuanDisconnected"
        | "yuanInputsResult"
        | "yuanInputSelected" 
        | "activeSourceChanged"
        | "highResCaptured"
        | "captureStatsResult"
        | "unifiedStatusResult"
        | "exportProgress"
        | "exportComplete"
        | "captureDeleted"
        | "photoSaved"
        | "settingsLoaded"
        | "settingsSaved"
        | "testConnectionResult"
        | "pacsTestResult"
        | "folderSelected"
        | "showExitConfirmation"
        | "updateConfig";
}

// Patient information structure
interface PatientInfo {
    id?: string;
    name?: string;      // Format: "LastName, FirstName"
    birthDate?: string; // ISO date string
    gender?: "M" | "F" | "O";
    studyDescription?: string;
    studyInstanceUID?: string;
    accessionNumber?: string;
}

// Worklist item structure
interface WorklistItem {
    studyInstanceUID: string;
    accessionNumber?: string;
    patientId: string;
    patientName: string;
    birthDate?: string;
    sex?: "M" | "F" | "O";
    age?: string;
    scheduledDate: string;
    scheduledTime: string;
    studyDescription: string;
    isEmergency?: boolean;
}

// Capture data structures
interface CaptureData {
    id: string;
    type: "photo" | "video";
    data?: string;      // Base64 encoded data
    fileName?: string;  // File-based approach
    filePath?: string;  // Full file path
    timestamp: string;
    patient?: PatientInfo;
    source?: "webrtc" | "yuan";
}

// Export request structure
interface ExportRequest {
    captures: CaptureData[];
    patient: PatientInfo;
}

// Configuration structures
interface PacsConfig {
    ServerHost: string;
    ServerPort: number;
    CalledAeTitle: string;
    CallingAeTitle: string;
    Timeout: number;
    EnableTls: boolean;
    MaxRetries: number;
    RetryDelay: number;
    Enabled?: boolean;
}

interface MwlConfig {
    EnableWorklist: boolean;
    MwlServerHost: string;
    MwlServerPort: number;
    MwlServerAET: string;
}

interface VideoConfig {
    DefaultResolution: string;
    DefaultFrameRate: number;
    DefaultQuality: number;
    EnableHardwareAcceleration: boolean;
    PreferredCamera: string;
}

interface StorageConfig {
    PhotosPath: string;
    VideosPath: string;
    DicomPath: string;
    QueuePath: string;
    TempPath: string;
    MaxStorageDays: number;
    EnableAutoCleanup: boolean;
}

interface ApplicationConfig {
    Language: string;
    Theme: string;
    EnableTouchKeyboard: boolean;
    EnableDebugMode: boolean;
    AutoStartCapture: boolean;
    WebServerPort: number;
    EnableRemoteAccess: boolean;
    HideExitButton: boolean;
    EnableEmergencyTemplates: boolean;
    AutoExportDicom: boolean;
}

interface AppConfig {
    Storage: StorageConfig;
    Pacs: PacsConfig;
    Video: VideoConfig;
    Application: ApplicationConfig;
    MwlSettings?: MwlConfig;
}

// Global message handling function
declare function sendMessage(message: OutboundMessage): void;
declare function receiveMessage(message: InboundMessage): void;

// Medical safety validation functions
declare function validatePatientInfo(patient: PatientInfo): boolean;
declare function validateCaptureData(capture: CaptureData): boolean;
declare function validateExportRequest(request: ExportRequest): boolean;

// Browser API extensions for medical device
interface Window {
    sendMessage: (message: OutboundMessage) => void;
    receiveMessage: (message: InboundMessage) => void;
    chrome: {
        webview: {
            postMessage: (message: string) => void;
        }
    };
}