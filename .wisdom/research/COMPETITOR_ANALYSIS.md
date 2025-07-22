# Konkurrenzanalyse - Medical Video/Image Capture Systeme

## 1. Nexus E&L SmartBox (Unsere aktuelle Lösung)
- Java-basiert
- Unterstützt verschiedene Grabberkarten
- DICOM-Export
- Worklist-Integration
- **Schwächen**: Performance, Ressourcenverbrauch, Workarounds für Hardware

## 2. Meso Box
- Website: mesobox.com
- Scheint im Aufbau/Umbau zu sein
- Details müssen weiter recherchiert werden

## 3. Diana (zu recherchieren)
- Medical Imaging Solution
- DICOM-konform
- Details folgen

## 4. Weitere relevante Lösungen

### Kommerzielle Produkte
- **Epiphan Video Grabber** mit DICOM-Software
- **Canfield Scientific** - Medical Imaging Systeme
- **KARL STORZ** AIDA System (High-End)
- **Stryker** SDC3 (Integrated OR)

### Open Source Alternativen
- **Orthanc** (DICOM Server, aber kein Capture)
- **dcm4che** (DICOM Toolkit)
- **OpenCV** + Custom DICOM Implementation

## Feature-Vergleich (vorläufig)

| Feature | SmartBox | Meso Box | Diana | Unser Ziel |
|---------|----------|----------|-------|------------|
| Video Capture | ✓ | ? | ? | ✓✓ |
| DICOM Export | ✓ | ? | ? | ✓✓ |
| Worklist | ✓ | ? | ? | ✓ |
| Streaming | ? | ? | ? | ✓ |
| Remote Mgmt | ? | ? | ? | ✓ |
| Embedded | ✓ | ? | ? | ✓✓ |
| Performance | ⚠️ | ? | ? | ✓✓ |

## Marktlücken & Chancen
1. **Performance**: Viele Lösungen sind Java/Electron-basiert → Native Performance
2. **Moderne UI**: Medical Software oft mit veralteten Interfaces
3. **Cloud-Ready**: Remote Management oft nachträglich aufgesetzt
4. **Flexibilität**: Viele Systeme sehr starr in Hardware-Unterstützung
5. **Preis**: High-End Lösungen sehr teuer, Low-End oft unzuverlässig

## USPs für SmartBox Next
- Native Performance (Rust/C++)
- Moderne, intuitive UI
- Cloud-native Remote Management
- Flexible Hardware-Unterstützung
- Open Standards (DICOM, HL7 FHIR ready)
- Entwickelt mit modernen DevOps-Praktiken