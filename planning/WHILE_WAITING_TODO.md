# SmartBox Next - Vorbereitende Aufgaben

## Während wir auf Research warten:

### 1. Development Environment Setup
```bash
# Go installieren (falls noch nicht vorhanden)
# Wails CLI
go install github.com/wailsapp/wails/v2/cmd/wails@latest

# Node.js für Frontend
# npm/yarn für Vue 3
```

### 2. Projekt-Struktur verfeinern
```
smartbox-next/
├── app/                    # Wails Application
│   ├── backend/           # Go Backend
│   │   ├── capture/       # Video/Image Capture
│   │   ├── dicom/         # DICOM Handling
│   │   ├── overlay/       # Overlay Rendering
│   │   ├── license/       # Lizenzmanagement
│   │   └── api/           # REST/WebSocket API
│   ├── frontend/          # Vue 3 Frontend
│   │   ├── views/         # Hauptansichten
│   │   ├── components/    # UI Komponenten
│   │   └── stores/        # Pinia Stores
│   └── build/             # Build-Konfiguration
├── docs/                  # Dokumentation
├── research/              # Konkurrenzanalyse
├── planning/              # Planung
├── specs/                 # Spezifikationen
└── licenses/              # Open Source Lizenzen
```

### 3. Key Libraries evaluieren

#### Go Libraries Research
- `github.com/blackjack/webcam` - Linux V4L2
- `github.com/kbinani/screenshot` - Windows Screen Capture
- `gocv.io/x/gocv` - OpenCV Bindings (optional)
- `github.com/suyashkumar/dicom` - DICOM Library
- `github.com/fogleman/gg` - 2D Graphics (für Overlays)

#### Frontend Libraries
- Vue 3 + TypeScript
- Tailwind CSS
- Pinia (State Management)
- VueUse (Utilities)

### 4. DICOM Test-Infrastruktur
- Orthanc Docker Container für Tests
- dcm4che Tools
- Test-Datasets vorbereiten

### 5. Regulatorische Recherche
- CE Medical Device Klassifizierung
- FDA 510(k) Requirements
- IEC 62304 (Medical Software)
- DICOM Conformance Statement Template

### 6. Business Model Canvas
```
Key Partners          | Key Activities      | Value Propositions
- Hardware-Hersteller | - Software Dev      | - 70% günstiger
- Medical Distributors| - Support           | - Modern UI
                     | - Zertifizierung    | - Open Core

Key Resources        | Customer Relations   | Channels
- CIRSS Team         | - Direct Support    | - Direct Sales
- DICOM Expertise    | - Community Forum   | - Partner Channel
- Open Source Comm.  | - Training          | - Online

Cost Structure                | Revenue Streams
- Development                 | - Software Licenses
- Zertifizierung             | - Support Contracts
- Marketing                  | - Custom Development
```

### 7. MVP Feature List (Priorisiert)
1. **P0 - Core**
   - [ ] Webcam Capture
   - [ ] DICOM Export
   - [ ] Basic UI

2. **P1 - Essential**
   - [ ] Worklist Query
   - [ ] Patient Overlay
   - [ ] Multi-Source

3. **P2 - Professional**
   - [ ] Video Recording
   - [ ] Remote Access
   - [ ] License Management

### 8. Technische Experimente
```go
// Webcam Test (Windows)
// test/webcam_test.go
package main

import (
    "github.com/go-ole/go-ole"
    "github.com/go-ole/go-ole/oleutil"
)

func testDirectShow() {
    ole.CoInitialize(0)
    defer ole.CoUninitialize()
    
    // DirectShow Graph Builder
    // ...
}
```

### 9. UI Mockups
- Figma/Excalidraw Sketches
- Touch-optimierte Buttons (min 44x44px)
- Dark Mode für OP-Umgebung
- Responsive für verschiedene Screens

### 10. Fragen für Research-Analyse
- Welche Features werden am meisten genutzt?
- Was nervt Anwender am meisten?
- Welche Workflows sind Standard?
- Wo sind die Pain Points?
- Was kosten Wartungsverträge?

## Sobald Research da ist:
1. Feature-Matrix erstellen
2. USPs finalisieren
3. Entwicklung starten
4. Marketing-Story entwickeln

*"Preparation is half the victory" - CIRSS Wisdom*