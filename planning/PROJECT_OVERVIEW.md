# SmartBox Next - Projektübersicht

## Problemstellung
Die aktuelle SmartBox-Lösung:
- Läuft mit Java (Performance/Ressourcen-Overhead)
- Krude Workarounds für Grabberkarten-Ansteuerung
- Nicht optimal für Embedded-Szenarien
- Verbesserungspotential bei Usability und Features

## Zielplattform
- **Primär**: Windows 10 Embedded (Kompatibilität)
- **Sekundär**: Linux-basierte Embedded-Systeme (für schwächere Hardware)
- **Hardware**: Flache All-in-One Systeme mit miniPCIe-Grabbern

## Konkurrenzprodukte (zu analysieren)
1. **Nexus E&L SmartBox** (unsere aktuelle Lösung)
2. **Meso Box**
3. **Diana**

## Technische Anforderungen

### Must-Have
- Videoquellen-Unterstützung:
  - Webcam
  - USB-Grabber
  - PCIe-Grabberkarten (Yuan für SDI/DVI/S-Video)
- DICOM-Funktionalität:
  - Modality Worklist (MWL) Query
  - C-STORE (verschiedene Presentation Contexts)
  - 100% NEMA-konform
- Embedded-tauglich (Single-Application-Mode)

### Nice-to-Have
- Echtzeit-Videostreaming
- Remote-Zugriff
- Zentrale Konfiguration & Verwaltung
- Remote-Updates
- Web-basiertes Management-Interface

## Technologie-Optionen (zu evaluieren)

### Frontend/UI
- **Electron** + React/Vue (Cross-Platform, aber ressourcenhungrig)
- **Tauri** + React/Vue (Rust-basiert, leichtgewichtiger)
- **Qt** (C++, nativ, bewährt im Medical-Bereich)
- **Flutter** (Dart, gute Embedded-Performance)

### Backend/Core
- **Rust** (Performance, Sicherheit, gute FFI für Grabberkarten)
- **Go** (Einfachheit, gute Concurrency, schnelle Entwicklung)
- **C++** (Maximale Kontrolle, DirectShow/V4L2 Integration)
- **Python** (Rapid Prototyping, aber Performance?)

### Video-Capture
- **Windows**: DirectShow, Media Foundation, WinRT
- **Linux**: V4L2, GStreamer
- **Cross-Platform**: OpenCV, FFmpeg

### DICOM
- Eigene Implementierung basierend auf CamBridge-Erfahrung
- Oder etablierte Libraries evaluieren

## Nächste Schritte
1. Konkurrenzanalyse durchführen
2. Tech-Stack-Entscheidung
3. MVP-Definition
4. Architektur-Design
5. Implementierung