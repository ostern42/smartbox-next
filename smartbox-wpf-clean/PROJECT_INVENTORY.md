# SmartBoxNext - Complete Project Inventory
**Created**: 2025-07-14
**Last Updated**: 2025-07-14
**Status**: ACTIVE PROJECT ✅

> **CRITICAL**: This document MUST be updated with every change!

## 🌐 Network Configuration

### Port Assignments (Avoid Conflicts!)
| Service | Port | Protocol | Status | Conflicts |
|---------|------|----------|--------|-----------|
| SmartBox WebServer | 8080 | HTTP | ✅ Active | Check other projects! |
| PACS Server (Test) | 11112 | DICOM | 🔧 External | - |
| MWL Server (Test) | 105 | DICOM | 🔧 External | - |

### IP Addresses & Hostnames
| Component | Default IP | Environment | Purpose |
|-----------|------------|-------------|---------|
| PACS Server | 192.168.1.100 | Test/Demo | DICOM storage |
| MWL Server | 192.168.1.100 | Test/Demo | Worklist queries |
| Local Host | 127.0.0.1 | Development | WebView2 content |

## 📁 File System Structure

### Core Directories
```
/smartbox-next/smartbox-wpf-clean/
├── wwwroot/                    # Web UI files
│   ├── js/                     # JavaScript modules
│   ├── styles/                 # CSS themes
│   ├── index.html             # Main UI
│   └── settings.html          # Settings UI
├── Services/                   # Backend services
├── bin/                       # Build output
└── obj/                       # Build cache
```

### Storage Paths (Configurable)
| Purpose | Default Path | Property | Notes |
|---------|--------------|----------|-------|
| Photos | `./Photos` | `Storage.PhotosPath` | Captured images |
| Videos | `./Videos` | `Storage.VideosPath` | Video recordings |
| DICOM | `./DicomOutput` | `Storage.DicomPath` | Generated DICOM |
| Temp | `./Temp` | `Storage.TempPath` | Cache & temporary |

## 🔧 Configuration Properties

### AppConfig Structure
```csharp
AppConfig
├── Application
│   ├── AutoStartCapture: bool
│   ├── EnableDebugLogging: bool
│   └── EnableEmergencyTemplates: bool
├── Storage
│   ├── PhotosPath: string
│   ├── VideosPath: string
│   ├── DicomPath: string
│   ├── TempPath: string
│   ├── EnableAutoCleanup: bool
│   ├── RetentionDays: int
│   └── CompressOldFiles: bool
├── Pacs
│   ├── Enabled: bool (computed)
│   ├── ServerHost: string
│   ├── ServerPort: int
│   ├── CalledAeTitle: string
│   ├── CallingAeTitle: string
│   ├── Timeout: int
│   ├── UseSecureConnection: bool
│   ├── MaxRetries: int
│   ├── AutoSendOnCapture: bool
│   └── SendInBackground: bool
├── MwlSettings
│   ├── EnableWorklist: bool
│   ├── MwlServerHost: string
│   ├── MwlServerPort: int
│   ├── MwlServerAET: string
│   ├── LocalAET: string
│   ├── CacheExpiryHours: int
│   ├── AutoRefreshSeconds: int
│   ├── ShowEmergencyFirst: bool
│   ├── DefaultQueryPeriod: string
│   ├── QueryDaysBefore: int
│   └── QueryDaysAfter: int
├── Video
│   ├── MaxRecordingMinutes: int
│   ├── DefaultResolution: string
│   ├── DefaultFrameRate: int
│   ├── DefaultQuality: int
│   ├── EnableHardwareAcceleration: bool
│   └── PreferredCamera: string
├── Dicom
│   ├── OutputDirectory: string
│   ├── StationName: string
│   ├── AeTitle: string
│   ├── Modality: string
│   └── PatientIdPrefix: string
└── LocalAET: string
```

## 🎭 Core Classes & Entities

### Main Classes
| Class | Purpose | Key Properties |
|-------|---------|----------------|
| `MainWindowMinimal` | WPF Main Window | `_config`, `_logger`, `webView` |
| `AppConfig` | Configuration | All settings sections |
| `WorklistItem` | MWL Patient Data | `PatientId`, `PatientName`, `StudyInstanceUID` |
| `MwlCache` | MWL Caching | `Items`, `LastUpdate`, `IsStale` |
| `DiagnosticWindow` | Test Dialogs | `TestSuccessful`, `TestMessage` |

### Service Classes
| Service | File | Purpose |
|---------|------|---------|
| `MwlService` | MwlService.cs | DICOM Worklist queries |
| `PacsService` | Services/PacsService.cs | PACS communication |
| `DicomServiceMinimal` | DicomServiceMinimal.cs | DICOM file creation |

## 🔌 JavaScript/C# Communication

### Message Types (JS → C#)
| Type | Handler | Purpose |
|------|---------|---------|
| `saveSettings` | `HandleSaveSettings()` | Save configuration |
| `getSettings` | `HandleGetSettings()` | Load configuration |
| `testpacsconnection` | `HandleTestPacsConnection()` | Test PACS |
| `testmwlconnection` | `HandleTestMwlConnection()` | Test MWL |
| `loadMWL` | `HandleLoadWorklist()` | Query worklist |
| `exitApp` | `HandleExitApplication()` | Close app |
| `capturePhoto` | `HandleCapturePhoto()` | Take photo |
| `captureVideo` | `HandleCaptureVideo()` | Record video |

### Message Types (C# → JS)
| Type | Purpose | Data Structure |
|------|---------|----------------|
| `settingsLoaded` | Configuration data | `{ data: AppConfig }` |
| `testResult` | Test results | `{ service, success, message }` |
| `mwlData` | Worklist items | `{ items: WorklistItem[] }` |

## 🎨 UI Components & IDs

### Critical HTML Elements
| Element ID | Purpose | Data Action |
|------------|---------|-------------|
| `mwlDateRange` | Date selector | - |
| `mwlFilter` | Search input | - |
| `test-pacs` | PACS test | `testpacsconnection` |
| `test-mwl` | MWL test | `testmwlconnection` |
| Settings form inputs | Config fields | Various (see PROPERTY_MAPPING_2025.md) |

## 📋 Enums & Constants

### Date Range Options
```javascript
const DATE_RANGES = {
    TODAY: 'today',
    THREE_DAYS: '3days',
    WEEK: 'week', 
    CUSTOM: 'custom'
};
```

### DICOM Constants
```csharp
// Core DICOM Settings
public const string DEFAULT_MODALITY = "XC";        // External-camera Photography
public const string MODALITY_OTHER = "OT";          // Other
public const string MODALITY_ENDOSCOPY = "ES";      // Endoscopy  
public const string DEFAULT_AE_TITLE = "SMARTBOX";
public const int DEFAULT_PACS_PORT = 11112;
public const int DEFAULT_MWL_PORT = 105;
public const string SOFTWARE_VERSION = "2.0.0";
```

### Critical Magic Numbers
```csharp
// Network Timeouts
public const int DICOM_TIMEOUT_MS = 5000;           // 5 second DICOM timeout
public const int CONNECTION_TIMEOUT_S = 30;         // 30 second connection timeout
public const int MWL_REFRESH_INTERVAL_S = 300;      // 5 minute auto-refresh

// Video Processing  
public const int MAX_4K_WIDTH = 3840;
public const int MAX_4K_HEIGHT = 2160;
public const int TARGET_FPS = 60;
public const int DEFAULT_QUALITY = 85;              // JPEG quality %

// Memory Management
public const int BUFFER_SIZE = 4096;                // Standard buffer size
public const string SHARED_MEMORY_NAME = "SmartBoxNextVideo";
```

### UI Color Constants (RGB)
```csharp
// Status Colors
public static readonly Color SUCCESS_BG = Color.FromRgb(242, 250, 242);    // Light green
public static readonly Color ERROR_BG = Color.FromRgb(253, 242, 242);      // Light red  
public static readonly Color SUCCESS_BORDER = Color.FromRgb(16, 124, 16);  // Green
public static readonly Color ERROR_BORDER = Color.FromRgb(216, 59, 1);     // Red
public static readonly Color SECONDARY_TEXT = Color.FromRgb(96, 94, 92);   // Gray
```

### Theme Names
```html
<!-- Available Themes -->
data-theme="medical-blue"     <!-- Default medical theme -->
data-theme="medical-teal"     <!-- Alternative medical theme -->
data-theme="dark"             <!-- Dark mode -->
data-theme="night"            <!-- Night shift theme -->
data-theme="highcontrast"     <!-- Accessibility theme -->
```

## 🚦 Project Status Tracking

### Build Status
- ✅ Builds successfully in Visual Studio
- ✅ All dependencies resolved
- ✅ No critical warnings

### Feature Status
| Feature | Status | Notes |
|---------|--------|-------|
| Settings Save/Load | ✅ Working | Complete implementation |
| PACS Test Connection | ✅ Working | Diagnostic window |
| MWL Test Connection | ✅ Working | Diagnostic window |
| Date Range Selector | ✅ Working | 4 options available |
| Camera Permissions | ✅ Working | Auto-grant implemented |
| Exit Confirmation | ✅ Working | Single dialog only |

## 🔄 Update Protocol

### When to Update This Document
1. **Every new property/setting added**
2. **Every port number change**
3. **Every new IP address/hostname**
4. **Every new class or service**
5. **Every message type change**
6. **Every enum or constant addition**

### Update Checklist
- [ ] Network ports checked for conflicts
- [ ] Property mappings documented
- [ ] File paths verified
- [ ] Git committed with change description
- [ ] Team notified of changes

---
**⚠️ REMEMBER: Other projects on this development machine:**
- Check port conflicts before changing ports
- Coordinate IP addresses with other medical projects  
- Update shared network documentation