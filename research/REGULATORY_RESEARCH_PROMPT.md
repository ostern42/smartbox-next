# Regulatory Research Prompt - Medical Device Software

## Ziel
Recherchiere die regulatorischen Anforderungen für Medical Device Software (speziell Video/Bild-Erfassungssysteme) in EU und USA, um Compliance-Anforderungen für SmartBox Next zu verstehen.

## Hauptfragen

### 1. CE-Kennzeichnung (EU MDR 2017/745)

#### Klassifizierung
- In welche Klasse fällt Software zur medizinischen Bild/Video-Erfassung?
- Regel 11 (Software als Medizinprodukt) - wie wird sie angewendet?
- Unterschied zwischen:
  - Software die nur Bilder erfasst/speichert
  - Software die Bilder verarbeitet/analysiert
  - Software mit Diagnose-Unterstützung

#### Konformitätsbewertung
- Welches Konformitätsbewertungsverfahren für unsere Klasse?
- Benötigte Dokumente:
  - Technische Dokumentation
  - Klinische Bewertung
  - Risikomanagement nach ISO 14971
- Rolle der Benannten Stelle?

#### DICOM/PACS Integration
- Gilt Software die nur DICOM exportiert als Medizinprodukt?
- Abgrenzung zu PACS (Picture Archiving and Communication System)
- Interoperabilität mit anderen Medizinprodukten

### 2. FDA 510(k) (USA)

#### Klassifizierung
- FDA Software Classification (SaMD - Software as Medical Device)
- Welche Produktcodes sind relevant?
  - Image Management System
  - Image Capture System
  - Secondary Display Software
- De Novo vs. 510(k) Pathway?

#### Predicate Devices
- Welche ähnlichen Geräte sind bereits FDA-cleared?
- Substantial Equivalence Argumentation
- Typische 510(k) Submissions für Capture-Systeme

#### FDA Guidance Documents
- "Policy for Device Software Functions"
- "Clinical Decision Support Software"
- Cybersecurity Requirements

### 3. IEC 62304 - Medical Device Software Lifecycle

#### Software Safety Classification
- Klasse A, B oder C für Capture-Software?
- Abhängigkeit von Features (AI, Measurements, etc.)
- Auswirkung auf Entwicklungsprozess

#### Dokumentationsanforderungen
- Software Development Plan
- Software Requirements Specification
- Software Architecture Design
- Verification & Validation
- Software Maintenance Plan

#### Agile Entwicklung
- Wie IEC 62304 mit agilen Methoden vereinbar?
- Continuous Deployment möglich?
- Update/Patch Management

### 4. Weitere Standards

#### IEC 60601-1 (Elektrische Sicherheit)
- Relevant für Hardware-Appliance Version?
- Software-Anforderungen aus 60601-1

#### ISO 13485 (Qualitätsmanagement)
- Notwendig für Hersteller?
- Outsourcing von Entwicklung möglich?

#### DICOM Standards
- DICOM Conformance Statement Requirements
- IHE (Integrating the Healthcare Enterprise) Profile

### 5. Praktische Aspekte

#### Kosten & Zeitrahmen
- CE-Kennzeichnung: Kosten, Dauer
- FDA 510(k): Fees, Review-Zeit
- Jährliche Audits/Gebühren

#### Software Updates
- Change Control Prozesse
- Wann ist eine neue Zulassung nötig?
- Cybersecurity Patches

#### Open Source Komponenten
- Verwendung von Open Source in Medizinprodukten
- Haftungsfragen
- Dokumentationsanforderungen

### 6. Vereinfachungen & Ausnahmen

#### Wellness/Lifestyle Software
- Abgrenzung zu nicht-medizinischer Software
- "General Wellness" Exemption

#### Research Use Only
- RUO Kennzeichnung
- Einschränkungen

#### In-House Manufacturing
- Krankenhaus-Eigenentwicklungen
- "Health Institution Exemption"

## Spezifische Szenarien für SmartBox Next

### Szenario 1: Basis Capture-Only
- Nur Bild/Video-Erfassung
- DICOM Export ohne Analyse
- Keine Messungen/AI

### Szenario 2: Mit Overlays & Messungen
- Patient-Info Overlays
- Ruler/Measurement Tools
- Annotations

### Szenario 3: Mit AI-Features
- Auto-Quality Assessment
- Scene Detection
- Anatomie-Erkennung

## Output Format

Bitte strukturiere die Findings:

```markdown
# Regulatory Requirements für SmartBox Next

## Quick Summary
- CE Klasse: [Klasse mit Begründung]
- FDA Pathway: [510(k) oder De Novo]
- Geschätzte Kosten: [Range]
- Zeitrahmen: [Monate]

## Detaillierte Anforderungen

### CE-Kennzeichnung (MDR)
[Spezifische Requirements]

### FDA Clearance
[Spezifische Requirements]

### Software-Entwicklung (IEC 62304)
[Prozess-Anforderungen]

## Empfehlungen
- Minimaler Weg zur Compliance
- Features die Klassifizierung erhöhen
- Cost-Benefit Analyse
```

## Zusätzliche Recherche

### Competitor Regulatory Status
- MediCapture: FDA Status?
- Hauppauge Medical: CE/FDA?
- Software-Only Lösungen: Wie klassifiziert?

### Regulatory Consultants
- Spezialisierte Beratungsfirmen
- Typische Kosten
- DIY vs. Consultant

### Fast-Track Optionen
- FDA Breakthrough Device
- CE Well-Established Technologies
- Mutual Recognition Agreements

*Ziel: Verstehen was nötig ist für legalen Markteintritt, ohne Over-Engineering*