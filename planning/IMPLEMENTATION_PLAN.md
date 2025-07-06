# SmartBox Next - Implementierungsplan

## Phase 1: MVP (2-3 Wochen)
### Ziel: Basis-Funktionalität mit Webcam und DICOM Export

#### Woche 1: Foundation
- [ ] Tech-Stack finalisieren
- [ ] Projekt-Setup (Build-System, Dependencies)
- [ ] Basic UI Framework
- [ ] Webcam-Capture implementieren
- [ ] Basis DICOM Dataset Creation

#### Woche 2: DICOM Integration
- [ ] DICOM C-STORE Client
- [ ] Worklist Query (C-FIND)
- [ ] Patient Selection UI
- [ ] Basic Configuration System

#### Woche 3: Testing & Polish
- [ ] PACS Integration Tests
- [ ] Error Handling
- [ ] Basic Installer/Deployment
- [ ] Performance Optimierung

## Phase 2: Advanced Features (3-4 Wochen)
### Ziel: Professionelle Features

#### Woche 4-5: Hardware Support
- [ ] USB Grabber Integration
- [ ] PCIe Grabberkarten Support (Yuan)
- [ ] Multi-Source Management
- [ ] Hardware Auto-Detection

#### Woche 6: Video Features
- [ ] Video Recording
- [ ] Multiframe DICOM Support
- [ ] Echtzeit-Streaming
- [ ] Compression Options

#### Woche 7: Enterprise Features
- [ ] Remote Management API
- [ ] Web-based Admin Interface
- [ ] Central Configuration
- [ ] Auto-Update System

## Phase 3: Production Ready (2-3 Wochen)
### Ziel: Deployment & Hardening

#### Woche 8: Embedded Optimization
- [ ] Kiosk Mode
- [ ] Auto-Start Configuration
- [ ] Resource Optimization
- [ ] Crash Recovery

#### Woche 9-10: Final Polish
- [ ] Comprehensive Testing
- [ ] Documentation
- [ ] Conformance Statement
- [ ] Deployment Package

## Technische Entscheidungen

### Empfohlener Tech-Stack

#### Option A: Rust + Tauri (Performance-Fokus)
**Frontend**: React/Vue mit Tauri
**Backend**: Rust
**Pros**: 
- Maximale Performance
- Kleine Binaries
- Sichere Memory-Verwaltung
- Gute FFI für Hardware

**Cons**:
- Längere Entwicklungszeit
- Steile Lernkurve

#### Option B: Go + Wails (Rapid Development)
**Frontend**: React/Vue mit Wails
**Backend**: Go
**Pros**:
- Schnelle Entwicklung
- Gute Performance
- Einfache Concurrency
- Cross-Platform

**Cons**:
- GC overhead
- Weniger Hardware-Control

#### Option C: C++ + Qt (Medical Standard)
**Frontend**: Qt Quick/QML
**Backend**: C++
**Pros**:
- Bewährt im Medical Bereich
- Maximale Hardware-Kontrolle
- Native Performance

**Cons**:
- Komplexität
- Längere Entwicklungszeit

### Empfehlung: Option B (Go + Wails)
**Begründung**:
1. Schnellste Time-to-Market
2. Ausreichende Performance für Video
3. Gute DICOM Libraries (go-dicom)
4. Einfache Wartbarkeit
5. Cross-Platform ohne Overhead

## Architektur-Übersicht

```
┌─────────────────────────────────────────┐
│          UI Layer (React/Vue)           │
├─────────────────────────────────────────┤
│         Wails Bridge (IPC)              │
├─────────────────────────────────────────┤
│          Business Logic (Go)            │
├─────────┬───────────┬──────────────────┤
│ Capture │   DICOM   │  Management      │
│ Module  │   Module  │    Module        │
├─────────┴───────────┴──────────────────┤
│      Hardware Abstraction Layer         │
├─────────────────────────────────────────┤
│   OS APIs │ Drivers │ Network          │
└─────────────────────────────────────────┘
```

## Risiken & Mitigationen

1. **Hardware-Kompatibilität**
   - Risiko: Grabberkarten-Treiber
   - Mitigation: Fallback auf Standard-APIs

2. **DICOM Compliance**
   - Risiko: PACS-Kompatibilität
   - Mitigation: Extensive Testing, CamBridge-Erfahrung

3. **Performance**
   - Risiko: Video-Encoding Bottlenecks
   - Mitigation: Hardware-Acceleration, Streaming

4. **Embedded Constraints**
   - Risiko: Resource-Limits
   - Mitigation: Profiling, Optimization

## Success Metrics
- Boot-Zeit < 30 Sekunden
- Capture-to-PACS < 5 Sekunden
- Memory Usage < 500MB
- 99.9% Uptime
- Zero-Config für Enduser