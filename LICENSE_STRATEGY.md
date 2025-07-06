# SmartBox Next - Lizenzstrategie

## Dual-Licensing Ansatz

### 1. Open Source Version (MIT License)
```
MIT License

Copyright (c) 2025 Claude's Improbably Reliable Software Solutions

Permission is hereby granted, free of charge...
```

**Umfang**:
- Core Capture Functionality
- Basic DICOM Export
- Web Interface
- Standard Hardware Support

**Vorteile**:
- Community Contributions
- Vertrauen durch Transparenz
- Marketing durch Adoption
- Keine Lizenz-Konflikte

### 2. Commercial Version (Proprietary)
**Zusätzliche Features**:
- Enterprise Management Console
- Advanced DICOM Features (Structured Reports)
- Premium Hardware Support (spezielle Grabberkarten)
- Priority Support & SLA
- Custom Branding
- Regulatory Documentation (FDA/CE)

## Technologie-Stack (License-Safe)

### ✅ Erlaubte Dependencies
```go
// Core (all MIT/BSD/Apache)
github.com/wailsapp/wails/v2      // MIT
github.com/suyashkumar/dicom       // MIT
github.com/gin-gonic/gin           // MIT
github.com/gorilla/websocket       // BSD

// UI (all MIT)
vue@3                              // MIT
tailwindcss                        // MIT
vite                               // MIT
```

### ❌ Zu vermeiden
- Qt (LGPL - kompliziert)
- FFmpeg (LGPL/GPL - je nach Build)
- DCMTK (BSD aber mit Attributions)
- Electron (MIT aber huge)

### ⚠️ Vorsichtig nutzen
```go
// Nur als optional/plugin
gocv.io/x/gocv  // Apache 2.0 (OpenCV bindings)
// OpenCV selbst ist Apache 2.0, aber aufpassen bei Codecs
```

## Kommerzielle Strategie

### Preismodell
1. **SmartBox Next CE** (Open Source)
   - Kostenlos
   - Community Support
   - Basic Features

2. **SmartBox Next Pro** 
   - 2.999€ einmalig pro Box
   - 1 Jahr Support inklusive
   - Alle Features

3. **SmartBox Next Enterprise**
   - 4.999€ pro Box + 999€/Jahr Support
   - Central Management
   - Custom Development
   - SLA

### Verkaufsargumente
- **TCO**: 70% günstiger als Sony/Olympus
- **Open Core**: Kein Vendor Lock-in
- **Modern**: Aktuelle Technologie
- **Support**: Deutsche Firma (CIRSS GmbH?)

## Rechtliche Absicherung

### Contributor License Agreement (CLA)
Alle Contributors müssen zustimmen:
```
Ich übertrage CIRSS das Recht, meinen Code unter
beliebiger Lizenz zu veröffentlichen, behalte aber
meine Urheberrechte.
```

### Trademark
- "SmartBox Next™" als Marke schützen
- Logo registrieren
- Domain sichern

### Patent-Strategie
- Defensive Publications für Kern-Features
- Keine Software-Patente (zu teuer/komplex)
- Prior Art dokumentieren

## Feature-Aufteilung

### Open Source (MIT)
- [x] Webcam Capture
- [x] Basic DICOM Export
- [x] Worklist Query
- [x] Web UI
- [x] Single Box Mode
- [x] Standard Grabber Support

### Commercial Only
- [ ] Central Management API
- [ ] Advanced Analytics
- [ ] AI-Features
- [ ] Custom Hardware Drivers
- [ ] Regulatory Compliance Pack
- [ ] Enterprise Authentication (AD/LDAP)
- [ ] Audit Trail with Signatures
- [ ] HL7 FHIR Integration

## Build System
```makefile
# Open Source Build
make build-oss

# Commercial Build (includes proprietary/)
make build-commercial KEY=$(LICENSE_KEY)
```

## Marketing-Strategie
1. **Phase 1**: Open Source Release
   - GitHub mit guter Doku
   - Medical IT Communities
   - Konferenz-Talks

2. **Phase 2**: Commercial Launch
   - Direktvertrieb an Kliniken
   - Partner mit Medical Distributoren
   - Messe-Präsenz (MEDICA, etc.)

3. **Phase 3**: Enterprise
   - Große Klinik-Ketten
   - Internationale Expansion
   - OEM Partnerships

## Warum das funktioniert
- **RedHat Model**: Support & Features verkaufen
- **Medical Market**: Zahlt für Zertifizierung & Support
- **Open Core**: Vertrauen durch Transparenz
- **Deutsche Qualität**: "Made by CIRSS"

*"Free as in Freedom, Expensive as in Medical Device"*