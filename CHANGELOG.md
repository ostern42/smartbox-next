# Changelog

All notable changes to SmartBox-Next will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2025-07-06

### Added
- Initial Wails application setup
- Webcam preview with getUserMedia
- Image capture to canvas
- Patient information form (Name, ID, Birth Date, Sex)
- Study information form (Description, Accession Number)
- DICOM export functionality with JPEG compression
- Multiple DICOM writer implementations:
  - simple_dicom.go - Initial MVP
  - jpeg_dicom.go - Working JPEG compression (59KB files)
  - working_dicom.go - RGB conversion approach
  - cambridge_style_dicom.go - Based on CamBridge v1
- Compact UI layout (AppCompact.vue)
- Exit button functionality
- Output directory: ~/SmartBoxNext/DICOM/

### Fixed
- DICOM compatibility with MicroDicom viewer (after 10 attempts!)
- Proper JPEG compression in DICOM format
- Transfer Syntax handling for compressed images

### Technical Details
- Frontend: Vue 3 with Vite
- Backend: Go with Wails v2
- DICOM: Custom implementation (no external DICOM libraries)
- File sizes: ~59KB for compressed JPEG DICOM

### Known Issues
- Camera selection not yet implemented (hardcoded to first camera)
- No overlay functionality yet
- No trigger system integration

### Session Notes
- Session 1 (2025-07-05 evening - 2025-07-06 02:49): Initial development, DICOM struggles, final success
- Session 2 (2025-07-06 afternoon): Documentation, realization that DICOM already works