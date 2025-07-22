# SmartBox Next - Regulatory Research Clarifications

## 1. Geplante Funktionalität

### Phase 1 (MVP) - Reine Capture-Software
- **Primär**: Bild/Video-Erfassung und Speicherung
- **DICOM Export**: Ohne Bildanalyse oder -verarbeitung
- **Overlays**: Nur informativer Natur (Patientendaten, Zeitstempel)
- **Keine** diagnostischen Features
- **Keine** Messungen oder Bildverbesserung
- → Ziel: Niedrigste Klassifizierung

### Phase 2 (Professional) - Erweiterte Features
- **Messtools**: Ruler, Winkel, Flächen (wie Konkurrenz)
- **Quality Check**: Blur Detection, Exposure Warning
- **Keine** Diagnoseunterstützung oder klinische Entscheidungshilfe

### Phase 3 (Future) - AI Features
- **Auto-Tagging**: Anatomie-Erkennung
- **Scene Detection**: Prozedur-Phasen
- **Immer noch keine** Diagnose-Features
- → Bewusst unter Klasse IIa bleiben

## 2. Zielmarkt-Priorität

### Primär: EU-Markt (CE)
- Deutschland als Hauptmarkt
- DACH-Region Expansion
- CE-Kennzeichnung prioritär
- **Zeitplan**: Q3 2025

### Sekundär: US-Markt (FDA)
- Nach erfolgreicher EU-Einführung
- 510(k) Pathway angestrebt
- **Zeitplan**: 2026

### Begründung:
- Nähe zum Markt (Deutschland)
- Einfachere Sprache/Support
- Bekannte Regulatory Consultants
- Niedrigere Einstiegshürden

## 3. Hardware-Komponente

### Dual-Strategie:

#### A. Software-Only (Priorität)
- **Download/Installation** auf Windows PCs
- Kunde nutzt eigene Hardware
- **Vorteil**: Einfachere Zulassung
- **Klassifizierung**: Vermutlich Klasse I oder IIa

#### B. Hardware-Appliance (Optional)
- Fertig konfigurierte Box
- Embedded Windows/Linux
- Touchscreen integriert
- **Später**: Nach Software-Zulassung
- **Klassifizierung**: Könnte IIa/IIb werden

## 4. Spezifische Szenarien für Recherche

### Szenario 1: "Basic Capture"
- Nur Video/Bild-Erfassung
- DICOM Export
- Informative Overlays
- **Kein** Clinical Decision Support
- **Keine** Bildverarbeitung
→ Angestrebte Klassifizierung: Klasse I

### Szenario 2: "Professional"  
- Basic + Messtools
- Quality Warnings (nicht-diagnostisch)
- Annotations
→ Wahrscheinlich Klasse IIa

### Szenario 3: "Mit AI" (Zukunft)
- Auto-Kategorisierung
- Workflow-Optimierung
- **Explizit keine** Diagnose-AI
→ Klasse IIa halten

## 5. Wichtige Abgrenzungen

### Was SmartBox Next NICHT ist:
- ❌ PACS (Picture Archiving System)
- ❌ Diagnostische Workstation
- ❌ Bildverarbeitungssoftware
- ❌ Clinical Decision Support
- ❌ Therapieplanungssystem

### Was SmartBox Next IST:
- ✅ Erfassungssystem (Image Acquisition)
- ✅ Dokumentationswerkzeug
- ✅ DICOM Konverter/Exporter
- ✅ Workflow-Unterstützung
- ✅ Sekundäre Anzeige

## 6. Regulatory Strategy

### "Start Simple, Grow Smart"
1. **Launch**: Als Klasse I Software
2. **Upgrade**: Features schrittweise
3. **Vermeiden**: Klasse IIb/III Features
4. **Fokus**: Usability statt Diagnostik

### Konkurrenz-Benchmark
- MediCapture: FDA 510(k) cleared
- Hauppauge Medical: Unklar
- Software-Only Lösungen: Meist Klasse I/IIa

## 7. Besondere Überlegungen

### Open Source Aspekt
- Core als MIT-lizenziert
- Medical Features als Closed Source?
- Dokumentation trotzdem MDR-konform

### Continuous Deployment
- Wie Updates handhaben?
- Security Patches vs. Feature Updates
- Change Control Process

Diese Klarstellungen sollten helfen, die Recherche auf die relevantesten regulatorischen Pfade zu fokussieren.