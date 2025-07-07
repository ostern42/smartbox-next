# SmartBox-Next Requirements & Product Decisions

## Project Overview

### Problem Statement
Die aktuelle SmartBox-Lösung:
- Läuft mit Java (Performance/Ressourcen-Overhead)
- Krude Workarounds für Grabberkarten-Ansteuerung
- Nicht optimal für Embedded-Szenarien
- Verbesserungspotential bei Usability und Features

### Target Platform
- **Primary**: Windows 10/11 IoT Enterprise
- **Secondary**: Linux-basierte Embedded-Systeme (für schwächere Hardware)
- **Hardware**: Flache All-in-One Systeme mit Touch, USB-Kameras

### Competition Analysis
1. **Nexus E&L SmartBox** (unsere aktuelle Lösung)
2. **Meso Box** - Direkte Konkurrenz
3. **Diana** - High-End Alternative

## Critical Non-Functional Requirements

### 1. Absolute Reliability (**CRITICAL**)
- **NO SINGLE POINT OF FAILURE**
- System must remain operational even if:
  - Power cable is pulled during capture
  - Windows crashes
  - Network fails
  - Storage fails
  - Any component fails
- Graceful degradation, never complete failure
- Auto-recovery from any error state

### 2. Resilient System Architecture
- Windows running from RAM (like Windows PE)
- Write filters for OS protection
- Minimal disk writes
- Power-loss tolerant at every moment
- Sub-10 second boot time
- Auto-start into application

### 3. Queue Requirements (**CRITICAL**)
- Local persistent queue for PACS uploads
- Survives power loss and reboots
- Remote management capability
- Never loses a single image
- Automatic retry with backoff
- Status monitoring (local and remote)

### 4. Emergency Operation Features
- **Emergency Patient Templates**:
  - "Notfall männlich" (auto date/time)
  - "Notfall weiblich" (auto date/time)  
  - "Notfall Kind" (with age estimate)
  - One-button access from main screen
  
- **On-Screen Keyboard**:
  - Touch-optimized for medical gloves
  - German special characters (ä, ö, ü, ß)
  - Context-aware input
  - Quick templates/phrases
  - Must work on resistive touchscreens

## Functional Requirements

### Core Features (MVP)
1. Webcam capture ✓
2. DICOM export ✓
3. Patient data entry ✓
4. PACS upload (C-STORE)
5. Local image storage

### Phase 2 Features
1. Upload queue with persistence
2. Emergency templates
3. PACS configuration UI
4. Connection testing (C-ECHO)
5. Basic error recovery

### Phase 3 Features
1. On-screen keyboard
2. Worklist integration
3. Multiple camera support
4. Overlay functionality
5. Remote management

## Technical Constraints

### Hardware
- Must run on low-end hardware
- Touch screen support (resistive & capacitive)
- Work with USB cameras
- Minimal RAM requirements (4GB target)

### Software
- Windows 10/11 IoT Enterprise preferred
- Go + Wails for application
- SQLite for queue persistence
- No external dependencies for core function

### Regulatory
- IEC 62304 considerations
- DICOM compliance
- Data protection (patient data)
- Audit trail requirements

## Performance Requirements
- Boot to operational: <10 seconds
- Capture to save: <1 second
- DICOM creation: <2 seconds
- Touch response: <100ms
- Queue processing: Continuous background

## Security Requirements
- No patient data in logs
- Encrypted storage for queue
- Secure PACS communication
- Access control for settings
- Remote access authentication

## Maintenance Requirements
- Remote diagnostics
- Automatic updates (with rollback)
- Self-monitoring and alerts
- Field-serviceable by technicians
- Clear error codes for support

---
*Note: This document captures product-level requirements. For technical implementation details, see TECHNICAL.md*