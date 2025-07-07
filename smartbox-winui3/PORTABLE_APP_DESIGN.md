# SmartBox Next - Portable App Design & Configuration System

## Portable App Structure

```
SmartBoxNext/
├── SmartBoxNext.exe          # Main executable
├── config.json               # User configuration (created on first run)
├── config.json.example       # Example configuration
├── webrtc.html              # WebRTC capture page
├── Data/                    # All user data (configurable)
│   ├── Photos/              # Captured photos
│   ├── Videos/              # Recorded videos
│   ├── DICOM/               # DICOM exports
│   ├── Queue/               # Upload queue (SQLite DB)
│   └── Temp/                # Temporary files
├── Logs/                    # Application logs
│   └── smartbox-{date}.log
└── README.txt               # Quick start guide
```

### Key Features:
- **Zero Installation**: Extract ZIP and run
- **Self-Contained**: All dependencies included
- **Portable Data**: Everything in one folder
- **Relative Paths**: Works from any location
- **No Registry**: No Windows registry entries
- **USB-Ready**: Can run from USB drive

## Configuration UI Design (Terminal-Style)

### Main Settings Window Layout

```
┌─ SmartBox Settings ─────────────────────────────────┬─ Help ──────────────┐
│                                                      │                      │
│ ▼ Storage Settings                                  │ Storage Settings     │
│   Photos Path:        [./Data/Photos    ] [Browse]  │                      │
│   Videos Path:        [./Data/Videos    ] [Browse]  │ Configure where      │
│   DICOM Path:         [./Data/DICOM     ] [Browse]  │ captured media is    │
│   □ Use Relative Paths                              │ stored. Relative     │
│   Max Storage (MB):   [10240           ]            │ paths are portable.  │
│   Retention Days:     [30              ]            │                      │
│                                                      │ Max Storage: When    │
│ ▼ PACS Configuration                                 │ limit is reached,    │
│   AE Title:          [SMARTBOX         ]            │ oldest files are     │
│   Remote AE Title:   [PACS             ]            │ deleted.             │
│   Remote Host:       [192.168.1.100    ]            │                      │
│   Remote Port:       [104              ]            │ Retention: Files     │
│   Timeout (sec):     [30               ]            │ older than this are  │
│   □ Use TLS                                          │ auto-deleted.        │
│                                                      │                      │
│ ▶ Video Settings                                     │                      │
│ ▶ Application Settings                               │                      │
│ ▶ Advanced Settings                                  │                      │
│                                                      │                      │
│ [Test Connection] [Validate] [Save] [Cancel]         │                      │
└──────────────────────────────────────────────────────┴──────────────────────┘
```

### Design Principles:

1. **Section Headers**: Bold, larger font, expandable/collapsible
2. **Indented Fields**: Clear hierarchy with proper spacing
3. **Dynamic Help Panel**: Context-sensitive help for selected field
4. **Validation Feedback**: Real-time validation with error messages
5. **Terminal Aesthetic**: Monospace fonts, box-drawing characters

## First-Run Assistant

### Step 1: Welcome
```
Welcome to SmartBox Next!

This assistant will help you configure the essential settings.
You can change these settings later at any time.

[Next] [Skip Assistant]
```

### Step 2: Storage Setup
```
Where should SmartBox store captured media?

○ Use default folders (recommended)
   Photos: ./Data/Photos
   Videos: ./Data/Videos
   
○ Choose custom locations
   [Browse for each folder]

[Back] [Next]
```

### Step 3: PACS Configuration
```
Configure PACS Connection:

Your AE Title: [SMARTBOX        ] (?)
                                   ↑
                        "Application Entity Title
                         identifies this device
                         to the PACS server"

PACS Server:   [________________] (?)
PACS Port:     [104             ] (?)
PACS AE Title: [________________] (?)

[Test Connection]

[Back] [Next] [Skip PACS]
```

### Step 4: Video Quality
```
Select default video quality:

○ High Quality (1920x1080 @ 60 FPS) - Recommended
○ Standard (1280x720 @ 30 FPS) - Lower bandwidth
○ Custom settings

[Back] [Finish]
```

## Implementation Plan

### Phase 1: Core Config System
- [x] AppConfig class with JSON serialization
- [x] Default values and validation
- [x] Portable path handling
- [ ] Config migration for updates

### Phase 2: Settings UI
- [ ] Create SettingsWindow.xaml
- [ ] Implement collapsible sections
- [ ] Add field validation
- [ ] Dynamic help panel
- [ ] Terminal-style theming

### Phase 3: First-Run Assistant
- [ ] Create AssistantWindow.xaml
- [ ] Step-by-step wizard flow
- [ ] Inline help tooltips
- [ ] Connection testing
- [ ] Save initial config

### Phase 4: Integration
- [ ] Load config on startup
- [ ] Apply settings throughout app
- [ ] Hot-reload config changes
- [ ] Settings menu item
- [ ] Keyboard shortcuts (F10 for settings)

## Configuration Help Content

### Storage Settings
- **Photos Path**: Where captured images are saved
- **Videos Path**: Where recorded videos are saved
- **DICOM Path**: Where DICOM exports are stored
- **Use Relative Paths**: Makes the app portable
- **Max Storage**: Automatic cleanup when limit reached
- **Retention Days**: Auto-delete old files

### PACS Configuration
- **AE Title**: Your device's identifier (1-16 chars)
- **Remote AE Title**: PACS server identifier
- **Remote Host**: IP address or hostname
- **Remote Port**: Usually 104 for DICOM
- **Timeout**: Connection timeout in seconds
- **Use TLS**: Secure connection (if supported)

### Video Settings
- **Resolution**: Higher = better quality, larger files
- **FPS**: 60 for smooth motion, 30 for smaller files
- **Bitrate**: Higher = better quality, larger files
- **Format**: WebM (recommended) or MP4
- **JPEG Quality**: For photo captures (95 recommended)

## Benefits of This Design

1. **User-Friendly**: Clear sections, helpful descriptions
2. **Professional**: Terminal aesthetic fits medical software
3. **Accessible**: Keyboard navigation, clear focus indicators
4. **Portable**: Everything in one folder, no installation
5. **Flexible**: Easy to add new settings sections
6. **Helpful**: Context-sensitive help reduces support needs

## Next Steps

1. Implement SettingsWindow with the terminal-style UI
2. Create the dynamic help system
3. Build the first-run assistant
4. Add config hot-reload capability
5. Create settings import/export feature