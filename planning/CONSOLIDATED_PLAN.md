# SmartBox Next - Konsolidierter Masterplan
*© 2025 Claude's Improbably Reliable Software Solutions*

## 🎯 Executive Summary

**Was**: Moderne Alternative zu 20k€ Medical Capture Systemen  
**Wie**: Go + Wails, Windows-First, Open Core Model  
**Wann**: MVP in 2-3 Wochen, Production in 8-10 Wochen  
**Warum**: 70% günstiger, 10x benutzerfreundlicher  

## 📋 Projekt-Übersicht

### Vision
Eine schlanke, moderne Medical Capture Lösung, die auf jedem Windows-PC läuft oder als dedizierte Appliance verfügbar ist. Primär Touch-bedienbar, aber voll vernetzt.

### Kern-Features (MVP)
1. **Video/Bild-Capture** (Webcam, USB-Grabber, PCIe-Karten)
2. **DICOM Export** (100% NEMA-konform)
3. **Worklist Integration** (MWL Query)
4. **Patient Overlay** (Burn-In oder DICOM Overlay)
5. **Touch-First UI** (10" optimiert)

### Erweiterte Features (Phase 2)
- Echtzeit-Videostreaming
- Remote Management
- Cloud Backup
- AI Auto-Tagging
- Multi-Box Synchronisation

## 🏗️ Technische Architektur

### Tech Stack (Final)
```yaml
Backend:
  Language: Go
  Framework: Wails v2
  DICOM: github.com/suyashkumar/dicom
  Capture: DirectShow (Windows) / V4L2 (Linux)
  
Frontend:
  Framework: Vue 3 + TypeScript
  UI: Tailwind CSS
  State: Pinia
  Build: Vite

Deployment:
  Windows: MSI Installer
  Linux: AppImage
  Config: YAML/JSON
```

### Architektur-Diagramm
```
┌─────────────────────────────────────┐
│      Touch UI / Web Browser         │
├─────────────────────────────────────┤
│         Wails Runtime               │
├─────────────────────────────────────┤
│          Go Backend                 │
│  ┌─────────┐ ┌──────────┐ ┌──────┐ │
│  │ Capture │ │  DICOM   │ │ API  │ │
│  │ Service │ │ Service  │ │Server│ │
│  └─────────┘ └──────────┘ └──────┘ │
├─────────────────────────────────────┤
│      Hardware Abstraction           │
└─────────────────────────────────────┘
```

## 💰 Business Model

### Lizenzierung (Dual License)
1. **Open Source (MIT)**
   - Core Features
   - Community Support
   - Ideal für Entwickler/Forschung

2. **Commercial**
   - Enterprise Features
   - Professional Support
   - Regulatory Docs

### Preismodell
| Edition | Einmalig | Support/Jahr | Features |
|---------|----------|--------------|----------|
| Community | 0€ | - | Basis |
| Professional | 999€ | 199€ | Alle Features |
| Enterprise | 1.999€ | 399€ | + Central Mgmt |
| Appliance | 2.999€ | 399€ | + Hardware |

### ROI für Kunden
- Konkurrenz: 10.000-20.000€
- SmartBox: 999-2.999€
- **Ersparnis: 70-90%**

## 📅 Implementierungs-Roadmap

### Phase 1: MVP (Wochen 1-3)
```
Woche 1: Foundation
├── Wails Setup
├── Basic UI
├── Webcam Capture
└── DICOM Dataset Creation

Woche 2: Integration  
├── DICOM C-STORE
├── Worklist Query
├── Patient Selection UI
└── Overlay System

Woche 3: Polish
├── Error Handling
├── Installer
├── Testing mit Orthanc
└── Performance Tuning
```

### Phase 2: Professional (Wochen 4-7)
```
Woche 4-5: Hardware
├── USB Grabber Support
├── PCIe Karten (Yuan)
├── Multi-Source Switch
└── Auto-Detection

Woche 6: Video
├── Recording
├── Streaming
├── Multiframe DICOM
└── Compression

Woche 7: Enterprise
├── Web API
├── Remote Config
├── License System
└── Auto-Update
```

### Phase 3: Production (Wochen 8-10)
```
Woche 8: Hardening
├── Kiosk Mode
├── Crash Recovery
├── Resource Limits
└── Security

Woche 9-10: Release
├── Documentation
├── Conformance Statement
├── Marketing Material
└── Launch!
```

## 🔧 Development Setup

### Voraussetzungen
```bash
# Windows (empfohlen für Hardware-Zugriff)
- Windows 10/11 Pro
- Go 1.21+
- Node.js 18+
- Git

# Optional
- Docker (für Orthanc Tests)
- Visual Studio (für C++ Bindings)
```

### Quick Start
```bash
# 1. Wails installieren
go install github.com/wailsapp/wails/v2/cmd/wails@latest

# 2. Projekt klonen
git clone https://github.com/CIRSS/smartbox-next
cd smartbox-next

# 3. Dependencies
cd app
go mod download
cd frontend && npm install

# 4. Development Mode
wails dev

# 5. Build
wails build -platform windows/amd64
```

## 🧪 Test-Strategie

### Test-Umgebung
- **DICOM Server**: Orthanc (localhost:4242/8042) ✅
- **Test-Bilder**: DICOM Samples
- **Hardware**: Webcam, USB-Grabber
- **PACS**: dcm4chee, Orthanc, Commercial

### Test-Fälle
1. Capture → DICOM → PACS Pipeline
2. Worklist Query & Patient Selection
3. Overlay Rendering (Burn-In vs. DICOM)
4. Multi-Source Switching
5. Error Recovery
6. Performance (1080p @ 30fps)

## 📊 Erfolgs-Metriken

### Technical KPIs
- Boot Zeit: < 30 Sekunden
- Capture Latency: < 100ms
- DICOM Export: < 5s für 1080p
- Memory Usage: < 200MB
- CPU Usage: < 15%

### Business KPIs
- Trial → Paid: > 30%
- Support Tickets: < 5/Monat
- Kundenzufriedenheit: > 90%
- Payback Period: < 6 Monate

## 🚀 Go-to-Market

### Phase 1: Soft Launch
- Open Source Release
- Medical IT Communities
- Beta Tester Program

### Phase 2: Commercial Launch
- Direct Sales
- Partner Channel
- MEDICA/Messe

### Phase 3: Scale
- International
- OEM Deals
- Cloud Version

## 🎯 Nächste Schritte

1. **Sofort**: Research-Ergebnisse abwarten
2. **Dann**: Feature-Priorisierung finalisieren
3. **Start**: Wails-Projekt initialisieren
4. **Sprint 1**: Webcam → DICOM Pipeline

## 📝 Offene Fragen

- [ ] Exakte Yuan Grabberkarten-Modelle?
- [ ] Welche PACS sind Ziel-Systeme?
- [ ] Regulatorische Anforderungen (CE)?
- [ ] Vertriebspartner?

---

*"From Vision to Production in 10 Weeks" - The CIRSS Way*