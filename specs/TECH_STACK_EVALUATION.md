# Tech Stack Evaluation für SmartBox Next

## Evaluierte Optionen

### 1. Rust + Tauri
```toml
[dependencies]
tauri = "1.5"
tokio = { version = "1", features = ["full"] }
v4l = "0.14"  # Linux Video4Linux
windows-capture = "1.0"  # Windows capture
dicom = "0.5"  # DICOM handling
```

**Bewertung**: 8/10
- ✅ Maximale Performance
- ✅ Kleine Binary Size (~10MB)
- ✅ Sichere Memory-Verwaltung
- ❌ Längere Entwicklungszeit
- ❌ Weniger DICOM Libraries

### 2. Go + Wails ⭐ EMPFOHLEN
```go
// Hauptabhängigkeiten
github.com/wailsapp/wails/v2
github.com/suyashkumar/dicom
github.com/blackjack/webcam
github.com/kbinani/screenshot
```

**Bewertung**: 9/10
- ✅ Schnelle Entwicklung
- ✅ Gute DICOM Library (suyashkumar/dicom)
- ✅ Cross-Platform ohne Probleme
- ✅ Einfache Concurrency für Streaming
- ✅ ~25MB Binary Size (akzeptabel)
- ❌ GC Overhead (minimal bei Video-Streaming)

### 3. C++ + Qt
```cmake
find_package(Qt6 REQUIRED COMPONENTS Core Widgets Quick Multimedia)
find_package(DCMTK REQUIRED)  # DICOM Toolkit
```

**Bewertung**: 7/10
- ✅ Bewährt im Medical Bereich
- ✅ Beste Hardware-Integration
- ✅ DCMTK ist DICOM-Referenz
- ❌ Komplexe Entwicklung
- ❌ Große Dependencies
- ❌ Plattform-spezifischer Code

### 4. Electron + Node.js
```json
{
  "dependencies": {
    "electron": "^27.0.0",
    "node-webcam": "^0.8.0",
    "dicom-parser": "^1.8.0",
    "cornerstone-core": "^2.6.0"
  }
}
```

**Bewertung**: 5/10
- ✅ Schnellste UI-Entwicklung
- ✅ Große Community
- ❌ Huge Binary Size (>100MB)
- ❌ RAM-Verbrauch
- ❌ Nicht embedded-tauglich

## Detaillierte Analyse: Go + Wails

### Architektur
```
Frontend (Vue 3 + TypeScript)
    ↕️ Wails Runtime Bridge
Backend (Go)
    ├── Capture Service
    │   ├── Webcam Handler
    │   ├── USB Grabber Handler
    │   └── PCIe Card Handler
    ├── DICOM Service
    │   ├── Dataset Builder
    │   ├── C-STORE Client
    │   └── Worklist Client
    └── Management Service
        ├── Config Manager
        ├── Remote API
        └── Update Service
```

### Key Libraries

#### Video Capture
```go
// Windows
github.com/kbinani/screenshot  // Screen capture
github.com/go-ole/go-ole       // DirectShow access

// Linux
github.com/blackjack/webcam    // V4L2 wrapper
github.com/korandiz/v4l        // Alternative V4L2

// Cross-Platform
gocv.io/x/gocv                 // OpenCV bindings
```

#### DICOM
```go
github.com/suyashkumar/dicom   // Main DICOM library
// Features:
// - Read/Write DICOM
// - C-STORE client
// - Custom tag handling
// - Streaming support
```

#### UI Framework
```javascript
// Wails + Vue 3 + Vite
// Modern, fast, type-safe
```

### Performance Projections
- **Startup Time**: ~2-3 seconds
- **Memory Usage**: 50-150MB (depending on video)
- **CPU Usage**: 5-15% during capture
- **Binary Size**: 25-30MB
- **Capture Latency**: <100ms
- **DICOM Export**: <2s for 1080p image

### Development Timeline mit Go+Wails
- **Week 1**: Setup + Basic Capture ✅
- **Week 2**: DICOM Integration ✅
- **Week 3**: UI + Worklist ✅
- **Week 4**: Hardware Support
- **Week 5**: Video + Streaming
- **Week 6**: Remote Management
- **Week 7**: Testing + Optimization

## Finale Empfehlung

**Go + Wails** bietet die beste Balance zwischen:
- Entwicklungsgeschwindigkeit
- Performance
- Wartbarkeit
- Feature-Completeness

Der einzige Nachteil (GC) ist bei Video-Streaming vernachlässigbar, da die meisten Daten als Byte-Arrays gehandhabt werden und nicht dem GC unterliegen.