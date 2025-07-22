# SmartBoxNext Property Mapping Documentation
**Updated**: 2025-07-14
**Status**: Complete Mapping of HTML/JS to C# Properties

## 🔄 Settings Property Mapping

### Storage Settings
| HTML ID | JS Property Path | C# Property | Type |
|---------|------------------|-------------|------|
| `storage-photos-path` | `Storage.PhotosPath` | `_config.Storage.PhotosPath` | string |
| `storage-videos-path` | `Storage.VideosPath` | `_config.Storage.VideosPath` | string |
| `storage-temp-path` | `Storage.TempPath` | `_config.Storage.TempPath` | string |
| `storage-dicom-path` | `Storage.DicomPath` | `_config.Storage.DicomPath` | string |
| `storage-enable-auto-cleanup` | `Storage.EnableAutoCleanup` | `_config.Storage.EnableAutoCleanup` | bool |
| `storage-retention-days` | `Storage.RetentionDays` | `_config.Storage.RetentionDays` | int |
| `storage-compress-old-files` | `Storage.CompressOldFiles` | `_config.Storage.CompressOldFiles` | bool |

### PACS Settings
| HTML ID | JS Property Path | C# Property | Type |
|---------|------------------|-------------|------|
| `pacs-server-host` | `Pacs.ServerHost` | `_config.Pacs.ServerHost` | string |
| `pacs-server-port` | `Pacs.ServerPort` | `_config.Pacs.ServerPort` | int |
| `pacs-called-ae-title` | `Pacs.CalledAeTitle` | `_config.Pacs.CalledAeTitle` | string |
| `pacs-calling-ae-title` | `Pacs.CallingAeTitle` | `_config.Pacs.CallingAeTitle` | string |
| `pacs-timeout` | `Pacs.Timeout` | `_config.Pacs.Timeout` | int |
| `pacs-use-secure-connection` | `Pacs.UseSecureConnection` | `_config.Pacs.UseSecureConnection` | bool |
| `pacs-max-retries` | `Pacs.MaxRetries` | `_config.Pacs.MaxRetries` | int |
| `pacs-auto-send-on-capture` | `Pacs.AutoSendOnCapture` | `_config.Pacs.AutoSendOnCapture` | bool |
| `pacs-send-in-background` | `Pacs.SendInBackground` | `_config.Pacs.SendInBackground` | bool |

### MWL Settings (Updated 2025-07-14)
| HTML ID | JS Property Path | C# Property | Type |
|---------|------------------|-------------|------|
| `mwlsettings-enable-worklist` | `MwlSettings.EnableWorklist` | `_config.MwlSettings.EnableWorklist` | bool |
| `mwlsettings-mwl-server-host` | `MwlSettings.MwlServerHost` | `_config.MwlSettings.MwlServerHost` | string |
| `mwlsettings-mwl-server-port` | `MwlSettings.MwlServerPort` | `_config.MwlSettings.MwlServerPort` | int |
| `mwlsettings-mwl-server-aet` | `MwlSettings.MwlServerAET` | `_config.MwlSettings.MwlServerAET` | string |
| `mwlsettings-cache-expiry-hours` | `MwlSettings.CacheExpiryHours` | `_config.MwlSettings.CacheExpiryHours` | int |
| `mwlsettings-auto-refresh-seconds` | `MwlSettings.AutoRefreshSeconds` | `_config.MwlSettings.AutoRefreshSeconds` | int |
| `mwlsettings-show-emergency-first` | `MwlSettings.ShowEmergencyFirst` | `_config.MwlSettings.ShowEmergencyFirst` | bool |
| `mwlsettings-default-query-period` | `MwlSettings.DefaultQueryPeriod` | `_config.MwlSettings.DefaultQueryPeriod` | string |
| `mwlsettings-query-days-before` | `MwlSettings.QueryDaysBefore` | `_config.MwlSettings.QueryDaysBefore` | int |
| `mwlsettings-query-days-after` | `MwlSettings.QueryDaysAfter` | `_config.MwlSettings.QueryDaysAfter` | int |

### Video Settings
| HTML ID | JS Property Path | C# Property | Type |
|---------|------------------|-------------|------|
| `video-max-recording-minutes` | `Video.MaxRecordingMinutes` | `_config.Video.MaxRecordingMinutes` | int |
| `video-default-resolution` | `Video.DefaultResolution` | `_config.Video.DefaultResolution` | string |
| `video-default-frame-rate` | `Video.DefaultFrameRate` | `_config.Video.DefaultFrameRate` | int |
| `video-default-quality` | `Video.DefaultQuality` | `_config.Video.DefaultQuality` | int |
| `video-enable-hardware-acceleration` | `Video.EnableHardwareAcceleration` | `_config.Video.EnableHardwareAcceleration` | bool |
| `video-preferred-camera` | `Video.PreferredCamera` | `_config.Video.PreferredCamera` | string |

### Application Settings
| HTML ID | JS Property Path | C# Property | Type |
|---------|------------------|-------------|------|
| `application-auto-start-capture` | `Application.AutoStartCapture` | `_config.Application.AutoStartCapture` | bool |
| `application-enable-debug-logging` | `Application.EnableDebugLogging` | `_config.Application.EnableDebugLogging` | bool |
| `application-enable-emergency-templates` | `Application.EnableEmergencyTemplates` | `_config.Application.EnableEmergencyTemplates` | bool |

## 📍 Additional Properties in AppConfig

### Root Level
| Property | C# Type | Default | Description |
|----------|---------|---------|-------------|
| `LocalAET` | string | "SMARTBOX" | Local Application Entity Title for DICOM |

### DicomSettings (Not exposed in UI)
| Property | C# Type | Default | Description |
|----------|---------|---------|-------------|
| `OutputDirectory` | string | "DicomOutput" | Where DICOM files are saved |
| `StationName` | string | "SMARTBOX" | DICOM station name |
| `AeTitle` | string | "SMARTBOX" | DICOM AE title |
| `Modality` | string | "XC" | DICOM modality |
| `PatientIdPrefix` | string | "SB" | Prefix for patient IDs |

## 🔧 JavaScript Mapping Implementation

The mapping is handled in `/wwwroot/js/settings-handler.js`:

```javascript
htmlIdToPropertyPath(htmlId) {
    // Special cases for MWL fields
    const mwlSpecialCases = {
        'mwlsettings-mwl-server-host': { section: 'MwlSettings', property: 'MwlServerHost' },
        'mwlsettings-mwl-server-port': { section: 'MwlSettings', property: 'MwlServerPort' },
        'mwlsettings-mwl-server-aet': { section: 'MwlSettings', property: 'MwlServerAET' },
        // ... etc
    };
    
    // Standard mapping pattern: htmlId -> Section.Property
    // Example: 'pacs-server-host' -> 'Pacs.ServerHost'
}
```

## 📨 C# Message Handlers

### Settings Messages
| Message Type | Handler Method | Description |
|--------------|----------------|-------------|
| `saveSettings` | `HandleSaveSettings()` | Saves all settings to config.json |
| `getSettings` | `HandleGetSettings()` | Returns current configuration |
| `testpacsconnection` | `HandleTestPacsConnection()` | Opens PACS diagnostic window |
| `testmwlconnection` | `HandleTestMwlConnection()` | Opens MWL diagnostic window |

### Application Messages
| Message Type | Handler Method | Description |
|--------------|----------------|-------------|
| `loadMWL` | `HandleLoadWorklist()` | Loads MWL data with date range |
| `exitApp` | `HandleExitApplication()` | Closes application |
| `openSettings` | `HandleOpenSettings()` | Opens settings window |
| `capturePhoto` | `HandleCapturePhoto()` | Takes photo capture |
| `captureVideo` | `HandleCaptureVideo()` | Handles video recording |
| `exportCaptures` | `HandleExportCaptures()` | Exports captures to DICOM |

## 🎯 UI Elements with Actions

### Main Interface
| Element | data-action | Handler |
|---------|-------------|---------|
| Settings Button | `opensettings` | Opens settings.html |
| Exit Button | `exitapp` | Shows exit confirmation |
| Export Button | `exportcaptures` | Exports to DICOM/PACS |

### Settings Interface
| Element | data-action | Handler |
|---------|-------------|---------|
| Save Settings | `savesettings` | Saves all form data |
| Test PACS | `testpacsconnection` | Opens PACS test window |
| Test MWL | `testmwlconnection` | Opens MWL test window |
| Browse Folder | `browsefolder` | Opens folder browser |

## 🔍 MWL Date Selector (New Feature)

### UI Elements
| Element ID | Purpose | Options |
|------------|---------|---------|
| `mwlDateRange` | Date range selector | today, 3days, week, custom |
| `mwlDateFrom` | Custom start date | date input (hidden by default) |
| `mwlDateTo` | Custom end date | date input (hidden by default) |

### Date Range Values
- `today`: Current day only
- `3days`: Yesterday to Tomorrow (default)
- `week`: Current week (Sunday to Saturday)
- `custom`: User-defined range

---
**Note**: This mapping is automatically loaded by WISDOM Claude in every session. Always refer to this document when working with settings or UI/C# communication.