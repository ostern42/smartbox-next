# DICOM Go Implementation Research

## Problem
Wir erstellen DICOM-Dateien in Go, die sich nicht in MicroDicom öffnen lassen. Die Dateien sind ~134KB groß (JPEG-komprimiert).

## Aktuelle Implementierung
- Transfer Syntax: 1.2.840.10008.1.2.4.50 (JPEG Baseline)
- File Meta: Explicit VR Little Endian
- Main Dataset: Implicit VR 
- Encapsulated JPEG data mit Basic Offset Table
- Photometric Interpretation: YBR_FULL_422

## Symptome
- Datei wird erstellt (134KB)
- MicroDicom zeigt keine Fehlermeldung
- Datei geht einfach nicht auf

## Research-Fragen

### 1. Go DICOM Libraries
- Welche Go Libraries gibt es für DICOM? (suyashkumar/dicom, gradienthealth/dicom?)
- Haben diese JPEG compression support?
- Gibt es funktionierende Beispiele für JPEG DICOM in Go?

### 2. DICOM JPEG Spezifika
- Muss bei JPEG Transfer Syntax das Main Dataset auch Explicit VR sein?
- Sind spezielle JPEG APP Marker nötig?
- Braucht MicroDicom spezielle DICOM Tags?

### 3. Debugging-Ansätze
- Wie kann man DICOM-Dateien validieren? (dciodvfy, dcmtk?)
- Gibt es Online DICOM Validators?
- Welche DICOM Viewer sind toleranter als MicroDicom?

### 4. Forum-Suche
- Stack Overflow: "golang dicom jpeg compression"
- DICOM Forums: "microdicom won't open jpeg compressed"
- GitHub Issues in Go DICOM libraries

### 5. Alternative Ansätze
- Sollten wir doch unkomprimiertes RGB verwenden?
- Gibt es einen "MicroDicom-kompatiblen" Weg?
- Können wir die DICOM-Datei von einem funktionierenden Tool analysieren?

## Kontext
Cambridge verwendet fo-dicom (C#) und deren DICOM-Dateien funktionieren in MicroDicom. Wir brauchen eine Go-Lösung die ähnlich kompatibel ist.

## Gewünschte Ergebnisse
1. Funktionierende Go Library oder Code-Beispiel
2. Erklärung warum unsere Dateien nicht funktionieren
3. Minimales Beispiel das in MicroDicom aufgeht