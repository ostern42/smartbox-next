# DICOM-Anforderungen für SmartBox Next

## Basierend auf CamBridge-Erfahrung

### Core DICOM Features
1. **C-STORE Client**
   - Multiple Presentation Contexts Support
   - Transfer Syntax: JPEG Baseline (1.2.840.10008.1.2.4.50)
   - Retry-Logic mit exponential backoff
   - User-friendly Fehlermeldungen

2. **Modality Worklist (MWL)**
   - C-FIND SCU für Worklist Query
   - Scheduled Procedure Step (SPS) Support
   - Patient/Study/Series Hierarchie

3. **DICOM Dataset Creation**
   - Secondary Capture IOD für Video/Bilder
   - Multiframe Support für Videos
   - Proper UID Generation (Study/Series/Instance)
   - Required Tags gemäß NEMA Standard

### Presentation Contexts (Minimum)
```
- Secondary Capture Image Storage (1.2.840.10008.5.1.4.1.1.7)
- Multiframe True Color Secondary Capture (1.2.840.10008.5.1.4.1.1.7.4)
- Video Endoscopic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.1.1)
- Video Microscopic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.2.1)
- Video Photographic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.4.1)
```

### Transfer Syntaxes
```
- Implicit VR Little Endian (1.2.840.10008.1.2)
- Explicit VR Little Endian (1.2.840.10008.1.2.1)
- JPEG Baseline Process 1 (1.2.840.10008.1.2.4.50)
- JPEG Extended Process 2 & 4 (1.2.840.10008.1.2.4.51)
- MPEG2 Main Profile @ Main Level (1.2.840.10008.1.2.4.100)
- MPEG2 Main Profile @ High Level (1.2.840.10008.1.2.4.101)
- MPEG-4 AVC/H.264 High Profile (1.2.840.10008.1.2.4.102)
```

### Required DICOM Tags (Minimum Set)
```
Patient Level:
- (0010,0010) PatientName
- (0010,0020) PatientID
- (0010,0030) PatientBirthDate
- (0010,0040) PatientSex

Study Level:
- (0020,000D) StudyInstanceUID
- (0008,0020) StudyDate
- (0008,0030) StudyTime
- (0008,0050) AccessionNumber
- (0008,0090) ReferringPhysicianName

Series Level:
- (0020,000E) SeriesInstanceUID
- (0008,0060) Modality
- (0020,0011) SeriesNumber
- (0008,103E) SeriesDescription

Instance Level:
- (0008,0018) SOPInstanceUID
- (0008,0016) SOPClassUID
- (0020,0013) InstanceNumber
- (0028,0010) Rows
- (0028,0011) Columns
- (0028,0100) BitsAllocated
- (0028,0101) BitsStored
- (0028,0102) HighBit
- (0028,0103) PixelRepresentation
- (0028,0004) PhotometricInterpretation
- (0028,0002) SamplesPerPixel
- (7FE0,0010) PixelData
```

### Error Handling (aus CamBridge)
- Network Timeouts (configurable)
- Association Rejection Handling
- Transfer Syntax Negotiation Failures
- User-friendly Fehlermeldungen in Deutsch/Englisch

### Performance Considerations
- Streaming für große Video-Dateien
- Parallel C-STORE für multiple Destinations
- Memory-efficient Pixel Data Handling
- Progress Reporting für UI

### Compliance
- DICOM Part 3 (Information Object Definitions)
- DICOM Part 4 (Service Class Specifications)
- DICOM Part 5 (Data Structures and Encoding)
- DICOM Part 6 (Data Dictionary)
- DICOM Part 7 (Message Exchange)
- DICOM Part 8 (Network Communication)

### Testing Requirements
- DICOM Validation Tools Integration
- Test gegen verschiedene PACS (dcm4chee, Orthanc, etc.)
- Conformance Statement Generation