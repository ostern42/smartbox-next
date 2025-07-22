# Web Research Prompt für Konkurrenzanalyse

## Ziel
Recherchiere medizinische Video/Bild-Erfassungssysteme für Endoskopie und OP-Dokumentation, um die besten Features für unser Open-Source-Projekt zu identifizieren.

## Zu recherchierende Systeme

### Enterprise/High-End
1. **Sony Medical Solutions**
   - NUCLeUS System
   - Medical Grade Monitors mit Capture
   - Surgical Imaging Platform

2. **Olympus Medical**
   - EndoALPHA System
   - ENDORECORD
   - CV-190/290 Prozessoren mit Aufzeichnung

3. **KARL STORZ**
   - AIDA System
   - OR1 Integration
   - IMAGE1 S Connect

4. **Stryker**
   - SDC3 HD Information Management
   - Connected OR Platform
   - 1688 AIM 4K Platform

### Mid-Range/Spezialisten
5. **MediCapture**
   - USB300/USB170 Recorder
   - MediCap200
   - MVR Pro/Lite

6. **Hauppauge**
   - HD PVR Pro 60
   - Colossus 2
   - Medical Editions

7. **TEAC Medical**
   - UR-4MD
   - MV-10XBS
   - HD Recorder Systems

8. **FSN Medical**
   - FusionHD System
   - IP-basierte Lösungen

### Software-Lösungen
9. **Canfield Scientific**
   - Mirror Software
   - VISIA Systeme

10. **FotoFinder**
    - Medicam 1000
    - Bodystudio ATBM

## Recherche-Fokus

### 1. User Interface
- Wie sieht die Bedienoberfläche aus?
- Touch vs. Tastatur/Maus?
- Workflow-Optimierungen?
- Screenshots der UIs

### 2. Capture Features
- Unterstützte Eingänge (SDI, HDMI, DVI, Composite)
- Auflösungen und Framerates
- Simultane Quellen?
- Live-Preview während Aufnahme

### 3. DICOM/PACS Integration
- Worklist Support?
- Welche DICOM-Features?
- Auto-Routing?
- HL7/FHIR Support?

### 4. Besondere Features
- KI-Features (Bildverbesserung, Auto-Tagging)
- Streaming-Fähigkeiten
- Remote-Zugriff
- Mobile Apps
- Sprachsteuerung
- Fußschalter-Integration

### 5. Workflow-Optimierungen
- Quick-Capture Modi
- Preset-Management
- Batch-Processing
- Report-Integration

### 6. Technische Details
- Betriebssysteme
- Hardware-Requirements
- Formfaktoren
- Zertifizierungen (FDA, CE Medical)

### 7. Preise (wenn verfügbar)
- Listenpreise
- Lizenzmodelle
- Service-Kosten

### 8. User Reviews/Feedback
- Was loben Nutzer?
- Was kritisieren sie?
- Feature-Wünsche

## Output Format

Bitte strukturiere die Findings so:

```markdown
# Medical Capture Systems Analysis

## System: [Name]
**Hersteller**: 
**Preis**: 
**Website**: 

### Killer Features
- Feature 1: [Beschreibung]
- Feature 2: [Beschreibung]

### UI/UX Highlights
- [Screenshots oder Beschreibung]

### Technische Specs
- Input: 
- Output: 
- DICOM: 

### Was wir "inspiriert" übernehmen sollten
- [Feature mit Verbesserungsidee]

### Schwächen (die wir besser machen)
- [Kritikpunkt]
```

## Spezielle Suchen

1. **GitHub/GitLab**: Gibt es Open-Source-Alternativen?
2. **Patents**: Welche Features sind patentiert (müssen wir umgehen)?
3. **Medical Forums**: Was wünschen sich Endoskopie-Techniker?
4. **YouTube**: Demo-Videos der Systeme

## Lizenz-Check
Prüfe bei Open-Source-Komponenten:
- MIT/BSD/Apache 2.0 = ✅ (kommerziell nutzbar)
- GPL = ⚠️ (Copyleft, problematisch)
- AGPL = ❌ (nicht für kommerzielle Closed-Source)

## Das Beste aus allen Welten
Ziel ist es, die besten Features zu identifizieren und sie BESSER zu implementieren:
- Einfacher
- Schneller  
- Intuitiver
- Offener (Standards)
- Günstiger

*"Good artists copy, great artists steal" - aber wir machen es besser!*