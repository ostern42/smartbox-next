# SmartBox Next - Portable App Design & Configuration System

## Portable App Structure

```
SmartBoxNext/
├── SmartBoxNext.exe          # Main executable
├── config.json               # User configuration (created on first run)
├── config.json.example       # Example configuration
├── webrtc.html              # WebRTC capture page
├── settings.html             # Settings UI (identical to native)
├── remote.html               # Remote management dashboard
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
- **Touch-First UI**: Complete touch operation
- **Remote Management**: Web-based configuration

## Configuration UI Design (Modern Windows Terminal Style)

### Design Inspiration: Windows Terminal Settings
- **Modern, Clean UI**: Like Windows Terminal settings page
- **Sans-serif Typography**: Segoe UI Variable with font weights
- **Subtle Gray Dividers**: Clean section separation
- **Smooth Animations**: Transitions and hover effects
- **Acrylic Background**: Optional blur effect

### Main Settings Window Layout

```
SmartBox Settings                                          ×
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

│ Storage                │  Storage Settings
│ PACS                   │  
│ Video                  │  Photos Path
│ Application            │  [./Data/Photos                ] [Browse]
│ Advanced               │  
│                        │  Videos Path
│                        │  [./Data/Videos                ] [Browse]
│                        │  
│                        │  DICOM Path
│                        │  [./Data/DICOM                 ] [Browse]
│                        │  
│                        │  ☑ Use Relative Paths
│                        │  Makes the application portable
│                        │  
│                        │  Maximum Storage Size
│                        │  [10240] MB
│                        │  
│                        │  Retention Period
│                        │  [30] days
│                        │  
│                        │  [Apply]  [Reset]

Help Panel (Right Side):
┌─────────────────────────┐
│ Photos Path             │
│                         │
│ Directory where         │
│ captured photos are     │
│ saved.                  │
│                         │
│ • Use relative paths    │
│   for portability       │
│ • Absolute paths for    │
│   fixed installations   │
│                         │
│ Current: ./Data/Photos  │
│ Full: C:\SmartBox\...   │
└─────────────────────────┘
```

### Design Elements:

1. **Typography**:
   - Headers: Segoe UI Variable Display, Semi-bold, 16pt
   - Labels: Segoe UI Variable Text, Regular, 12pt
   - Values: Segoe UI Variable Text, Light, 12pt
   - Help: Segoe UI Variable Text, Regular, 11pt

2. **Colors**:
   - Background: System adaptive (Light/Dark mode)
   - Dividers: Subtle gray (#E0E0E0 light, #404040 dark)
   - Active field: Accent color border
   - Valid field: Green border (#107C10)
   - Invalid field: Red border (#E81123)

3. **Spacing** (Touch-Optimized):
   - Touch targets: Minimum 44x44px (Microsoft guidelines)
   - Section padding: 32px (increased for touch)
   - Field spacing: 24px (more space between elements)
   - Button padding: 16px vertical, 32px horizontal

4. **Interactions**:
   - Smooth focus transitions (200ms)
   - Touch ripple effects
   - Swipe to navigate sections
   - Long-press for help
   - Pinch to zoom (for accessibility)

## Touch-First Design Requirements

### Touch UI Principles:
1. **Large Touch Targets**: All interactive elements minimum 44x44px
2. **Clear Visual Feedback**: Touch ripples, color changes
3. **No Hover States**: Everything works with tap/touch
4. **Virtual Keyboard**: Optimized layouts for different input types
5. **Gesture Support**: Swipe between sections, pinch to zoom

### Touch-Optimized Controls:

```
Text Input (Touch-Optimized):
┌─────────────────────────────────────┐
│                                     │ 60px height
│  Photos Path                        │
│                                     │
└─────────────────────────────────────┘

Toggle Switch (Easy Touch):
┌──────────────┐
│ ●━━━━━━━━━━ │  44px height
└──────────────┘

Number Input (With Steppers):
┌─────────────────────────────────────┐
│  [-]    10240 MB    [+]             │ Large +/- buttons
└─────────────────────────────────────┘

Dropdown (Touch-Friendly):
┌─────────────────────────────────────┐
│  Select Theme              ▼        │ 60px height
└─────────────────────────────────────┘
```

### On-Screen Keyboard Integration:
- Number fields: Numeric keyboard
- Email fields: Email keyboard layout
- IP Address: Custom numeric with dots
- Path fields: Full keyboard with / and \

## HTML/Web Version Requirements

### Dual Implementation Strategy:
1. **Native WinUI3**: Primary application
2. **HTML/CSS/JS**: Identical settings interface

### Shared Features:
- Same layout and design
- Same validation logic
- Same help system
- Same assistant mode
- Synchronized via REST API

### Web Settings Architecture:
```
settings.html
├── Modern responsive design
├── Touch-first interactions
├── WebSocket for real-time sync
├── REST API for config CRUD
└── Progressive Web App capable

remote.html (Management Dashboard)
├── Device status overview
├── Remote configuration
├── Multi-device management
├── Queue monitoring
├── Log viewer
└── Remote assistance
```

### Implementation Approach:
1. **Design Once**: Create design system
2. **Implement Twice**: WinUI3 + HTML/CSS/JS
3. **Share Logic**: Common validation rules
4. **API Bridge**: REST/WebSocket communication

### Touch Gestures:
- **Swipe Left/Right**: Navigate sections
- **Swipe Down**: Show help for current field
- **Long Press**: Context menu
- **Pinch**: Zoom for accessibility
- **Double Tap**: Quick actions

## First-Run Assistant (Integrated with Settings UI)

### Concept: Smart Configuration Assistant
When config is empty or invalid, the Settings window opens in **Assistant Mode**:

1. **Progressive Disclosure**: Only shows current field/section
2. **Auto-Focus**: Automatically focuses the current field
3. **Live Validation**: Green border when valid, red when invalid
4. **Contextual Help**: Help panel shows detailed info for current field
5. **Smart Navigation**: Can't proceed until current field is valid

### Visual Flow:

```
SmartBox Initial Setup                                      ×
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

│ ● Storage              │  Let's set up storage locations
│ ○ PACS                 │  
│ ○ Video                │  Photos Path
│ ○ Application          │  [./Data/Photos                ] [Browse]
│                        │  ✓ Valid path
│                        │  
│ Progress: 25%          │  [Next field: Videos Path]
│ ████░░░░░░░░░░░░      │  

                         Help Panel:
                         ┌─────────────────────────┐
                         │ Setting up Photos Path  │
                         │                         │
                         │ This is where your      │
                         │ captured photos will    │
                         │ be saved.               │
                         │                         │
                         │ TIP: Use relative paths │
                         │ (starting with ./) to   │
                         │ keep the app portable.  │
                         │                         │
                         │ Press TAB or click Next │
                         │ to continue.            │
                         └─────────────────────────┘
```

### Assistant Mode Features:

1. **Field Highlighting**:
   - Current field: Accent color glow
   - Completed fields: Green checkmark
   - Invalid fields: Red border with error message
   - Future fields: Grayed out

2. **Smart Validation**:
   - Real-time as you type
   - Clear error messages
   - Suggestions for fixes
   - Test buttons where applicable (PACS)

3. **Progress Tracking**:
   - Visual progress bar
   - Section checkmarks
   - Skip options for optional sections
   - Save progress and resume later

4. **Keyboard Navigation**:
   - TAB: Next field (if valid)
   - SHIFT+TAB: Previous field
   - ENTER: Confirm current field
   - ESC: Exit assistant (with confirmation)

## Implementation Plan

### Phase 1: Core Config System
- [x] AppConfig class with JSON serialization
- [x] Default values and validation
- [x] Portable path handling
- [ ] Config migration for updates

### Phase 2: Modern Settings UI (Windows Terminal Style)
- [x] Create SettingsWindow.xaml (needs style update)
- [x] Implement collapsible sections
- [x] Add field validation
- [x] Dynamic help panel
- [ ] **UPDATE: Modern Windows Terminal styling**
  - [ ] Segoe UI Variable font
  - [ ] Subtle animations
  - [ ] Clean section dividers
  - [ ] Smooth transitions

### Phase 3: Integrated Assistant Mode
- [ ] **NEW: Assistant Mode in Settings Window**
  - [ ] Progressive field disclosure
  - [ ] Auto-focus current field
  - [ ] Live validation with visual feedback
  - [ ] Green border for valid fields
  - [ ] Progress tracking
  - [ ] Can't proceed until field valid
- [ ] Detect empty/invalid config
- [ ] Auto-launch in assistant mode
- [ ] Keyboard navigation (TAB through fields)
- [ ] Skip optional sections

### Phase 4: Integration
- [x] Load config on startup
- [x] Apply settings throughout app
- [ ] Hot-reload config changes
- [x] Settings menu item
- [ ] Keyboard shortcuts (F10 for settings)

### Phase 5: Touch & Web Implementation
- [ ] **Touch-First UI Updates**
  - [ ] Increase all touch targets to 44x44px minimum
  - [ ] Add touch ripple effects
  - [ ] Implement swipe navigation
  - [ ] Virtual keyboard optimization
  - [ ] Remove hover-dependent features
- [ ] **HTML/Web Version**
  - [ ] Create settings.html (identical to native)
  - [ ] Implement REST API for config
  - [ ] WebSocket for real-time sync
  - [ ] Touch gestures in web version
  - [ ] Progressive Web App manifest
- [ ] **Remote Management**
  - [ ] Create remote.html dashboard
  - [ ] Multi-device management
  - [ ] Queue monitoring
  - [ ] Remote assistance features

### Phase 6: Polish
- [ ] Smooth animations (200ms transitions)
- [ ] Touch ripple effects
- [ ] Acrylic background (optional)
- [ ] Dark/Light mode support
- [ ] Accessibility (screen reader support)
- [ ] Glove-friendly mode (extra large targets)

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
2. **Professional**: Modern UI fits medical software
3. **Touch-First**: Complete configuration without mouse/keyboard
4. **Accessible**: Large targets, clear focus, screen reader support
5. **Portable**: Everything in one folder, no installation
6. **Flexible**: Easy to add new settings sections
7. **Helpful**: Context-sensitive help reduces support needs
8. **Remote-Ready**: Web-based management from anywhere
9. **Glove-Friendly**: Medical staff can use with gloves

## Use Cases for Touch & Web

### Touch Scenarios:
1. **Initial Setup**: Technician configures via touchscreen
2. **Emergency Room**: Quick settings change with gloves
3. **Tablet Management**: IT staff uses tablet for multiple devices
4. **Kiosk Mode**: Locked-down touch-only interface

### Web/Remote Scenarios:
1. **Central IT**: Manage all devices from office
2. **Remote Support**: Assist users without site visit
3. **Bulk Configuration**: Deploy settings to multiple devices
4. **Monitoring**: Check device status and queues
5. **Audit Trail**: Review configuration changes

## Next Steps

1. Implement SettingsWindow with the terminal-style UI
2. Create the dynamic help system
3. Build the first-run assistant
4. Add config hot-reload capability
5. Create settings import/export feature