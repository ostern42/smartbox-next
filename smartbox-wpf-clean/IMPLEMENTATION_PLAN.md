# SmartBoxNext Complete Implementation Plan

**Status: UPDATED 2025-07-10 02:05 CEST**

**Phases 0-5: ✅ COMPLETED**
- Phase 0: File Lock Fix - ProcessHelper.cs, Window_Closing handler, PowerShell scripts
- Phase 1: Service Architecture - Windows Service, SharedMemory IPC, Named Pipes
- Phase 2: DirectShow Yuan Integration - YuanCaptureGraph, FrameProcessor, SampleGrabber
- Phase 3: DICOM Pipeline - OptimizedDicomConverter, YUY2Converter, fo-dicom integration
- Phase 4: PACS Queue - IntegratedQueueManager, QueueProcessor integration
- Phase 5: WebRTC + Yuan Integration - UnifiedCaptureManager, MainWindow handlers

**Ready for Phase 6: PIP Enhancement**

---

## Overview
Vollständiger Implementierungsplan für SmartBoxNext mit Yuan SC550N1 SDI/HDMI Capture, DICOM Export und PACS Integration.

**Ziel**: Medizinische Bildgebungsanwendung mit professioneller Video-Capture, DICOM-Export und PACS-Integration.

**Tech Stack**: 
- .NET 8, WPF, WebView2 (UI)
- .NET Framework 4.8 Windows Service (Capture)
- DirectShow.NET für Yuan Capture
- SharedMemory.CircularBuffer für IPC (60 FPS!)
- fo-dicom für DICOM
- WebRTC für Webcam (bereits funktioniert)

**KRITISCHE ERKENNTNISSE aus Research Report 2:**
- Yuan SDK hat KEIN natives DICOM (Third-Party nötig)
- Memory Mapped Files ermöglichen 60 FPS IPC (Named Pipes nur 1-2 FPS!)
- Windows Service mit Session 0 braucht SampleGrabber (nicht VMR9)
- YUY2 Format beibehalten für Performance

---

## Phase 0: Prerequisites & File Lock Fix [KRITISCH!]

### Ziel
WebView2 File Lock Problem lösen, damit Development ohne Neustarts möglich ist.

### Erfolgskriterien ✅ COMPLETED
- [x] Build funktioniert nach App-Schließen ohne Restart
- [x] Alle WebView2 Prozesse werden sauber beendet
- [x] Keine locked DLLs nach Exit

### Technische Schritte

#### 0.1 ProcessHelper.cs erstellen
```
Datei: Helpers/ProcessHelper.cs
- GetChildProcesses(int parentPid)
- KillProcessTree(int rootPid)
- FindWebView2Processes()
- ForceKillWebView2()
```

#### 0.2 Window_Closing Handler verbessern
```
Datei: MainWindow.xaml.cs
- Cancel immediate close (e.Cancel = true)
- Stop WebView2 navigation
- Navigate to about:blank
- Stop all services (QueueProcessor, WebServer)
- Dispose WebView2 mit Delay
- Kill child processes
- Environment.Exit(0)
```

#### 0.3 PowerShell Cleanup Script
```
Datei: fix-locks.ps1
- Find WebView2 processes by parent
- Use handle.exe for locked files
- Force kill with timeout
- Clean bin/obj folders
```

#### 0.4 Build Helper Scripts
```
Dateien:
- build-clean.ps1 (kill processes + build)
- restart-and-build.ps1 (full cleanup)
```

### Test-Verfahren
1. App starten
2. App schließen
3. Sofort `dotnet build` → muss funktionieren
4. 10x wiederholen für Stabilität

### Geschätzte Zeit: 4-6 Stunden
### Risiko: Hoch - blockiert alles andere

---

## Phase 1: Windows Service Architecture Setup

### Ziel
.NET Framework 4.8 Windows Service für Yuan Capture mit Memory Mapped Files IPC zu .NET 8 UI.

### Erfolgskriterien ✅ COMPLETED
- [x] Windows Service läuft mit Session 0 Isolation
- [x] Memory Mapped Files IPC funktioniert
- [x] 60 FPS zwischen Service und UI erreichbar
- [x] Control Commands über Named Pipes

### Technische Schritte

#### 1.1 SmartBoxNext.CaptureService Projekt
```
Neues Projekt: SmartBoxNext.CaptureService (.NET Framework 4.8)
- Windows Service Template
- DirectShowLib NuGet
- SharedMemory NuGet für CircularBuffer
```

#### 1.2 Service Base Implementation
```
Datei: CaptureService.cs
- Windows Service mit MTA Threading
- CoInitializeEx für COM
- Proper Session 0 handling
- Event logging
```

#### 1.3 SharedMemory Setup
```
Datei: SharedMemoryManager.cs
- CircularBuffer (10 nodes × 4MB)
- FrameHeader struct
- Producer (Service) implementation
- Synchronization handling
```

#### 1.4 Named Pipe Control Channel
```
Datei: ControlPipeServer.cs
- Commands: Start, Stop, SelectInput, etc.
- Async message handling
- Error recovery
```

#### 1.5 UI SharedMemory Client
```
In SmartBoxNext (UI):
Datei: Services/SharedMemoryClient.cs
- Consumer implementation
- Frame callback system
- Control pipe client
```

### Test-Verfahren
1. Service installation test
2. IPC bandwidth benchmark
3. Latency measurement (<10ms target)
4. Memory stability test
5. Service restart recovery

### Geschätzte Zeit: 10-12 Stunden
### Abhängigkeiten: Phase 0 complete

---

## Phase 2: DirectShow.NET Yuan Integration (im Service)

### Ziel
Yuan SC550N1 Capture über DirectShow.NET im Windows Service mit 60 FPS YUY2.

### Erfolgskriterien ✅ COMPLETED
- [x] Yuan Karte im Service erkannt (Session 0!)
- [x] SampleGrabber funktioniert (nicht VMR9)
- [x] 60 FPS YUY2 Frames zu SharedMemory
- [x] Smart Tee für multiple outputs

### Technische Schritte

#### 2.1 DirectShow im Service
```
In CaptureService:
- MTA COM initialization
- Hardware permissions check
- Session 0 compatible graph
```

#### 2.2 YuanCaptureGraph
```
Datei: YuanCaptureGraph.cs
- Device enumeration
- SampleGrabber (NICHT VMR9!)
- YUY2 format configuration
- Smart Tee filter für multi-output
```

#### 2.3 Frame Processing Pipeline
```
Datei: FrameProcessor.cs
- BufferCB implementation
- Async frame handling
- Buffer pool (no GC pressure)
- SharedMemory write
```

#### 2.4 Multi-Branch Support
```
- Live Preview branch → SharedMemory
- Snapshot branch → High-res buffer
- Recording branch → Optional encoder
```

### Test-Verfahren
1. Service mit Yuan device test
2. Frame rate stability
3. CPU usage (<20% target)
4. YUY2 format verification
5. Multi-branch performance

### Geschätzte Zeit: 8-10 Stunden
### Abhängigkeiten: Phase 1

---

## Phase 3: DICOM Pipeline Implementation

### Ziel
JPEG/Video Frames in DICOM wrappen mit fo-dicom (Yuan hat kein natives DICOM!).

### Erfolgskriterien ✅ COMPLETED
- [x] YUY2 → RGB → JPEG Conversion funktioniert
- [x] DICOM files von MicroDicom lesbar
- [x] Metadata korrekt gesetzt
- [x] Performance <100ms pro Frame
- [x] CPU-optimierte YUY2 Conversion

### Technische Schritte

#### 3.1 DicomConverter mit fo-dicom
```
Datei: Services/DicomConverter.cs
- Secondary Capture workflow
- fo-dicom für DICOM generation
- JPEGProcess1 transfer syntax
- Fixed tags für medical compliance
```

#### 3.2 Optimized YUY2 Converter
```
Datei: Converters/Yuy2Converter.cs
- CPU-optimierte Conversion (unsafe code)
- Lookup tables für YUV→RGB
- Target: <8ms für 1080p
- Optional: Keep YUY2 bis final stage
```

#### 3.3 JPEG Encoder
```
Datei: Converters/JpegEncoder.cs
- ImageSharp für quality control
- Medical-grade compression settings
- Lossless option für kritische Bilder
```

#### 3.4 Frame Pipeline Integration
```
In UI App:
- Receive YUY2 from SharedMemory
- Convert on-demand für Display/DICOM
- Batch processing für efficiency
```

### Test-Verfahren
1. YUY2 → RGB performance test
2. DICOM Secondary Capture validation
3. Viewer compatibility (MicroDicom, OsiriX)
4. CPU usage profiling
5. Memory leak detection

### Geschätzte Zeit: 8-10 Stunden
### Abhängigkeiten: Phase 2

---

## Phase 4: PACS Queue Activation

### Ziel
Fertigstellung der PACS Upload Queue mit UI Integration.

### Erfolgskriterien ✅ COMPLETED
- [x] DICOM files werden zur Queue hinzugefügt
- [x] Upload zu PACS funktioniert
- [x] Retry logic bei Fehlern
- [x] UI zeigt Queue Status

### Technische Schritte

#### 3.1 DicomExporter → QueueManager Integration
```
Änderung: DicomExporter.cs
- Nach DICOM save → _queueManager.Enqueue()
- PatientInfo passing
```

#### 3.2 QueueProcessor Start
```
Änderung: MainWindow.xaml.cs
- Verify QueueProcessor.Start() läuft
- Background service monitoring
```

#### 3.3 Settings UI - Queue Status
```
Änderungen: wwwroot/settings.html
- Queue statistics display
- Retry failed button
- Clear completed button
```

#### 3.4 JavaScript Handlers
```
Änderung: wwwroot/settings.js
- getQueueStatus handler
- retryFailedItems handler
- clearCompleted handler
```

#### 3.5 C# Message Handlers
```
Änderung: MainWindow.xaml.cs
- HandleGetQueueStatus()
- HandleRetryFailed()
- HandleClearCompleted()
```

### Test-Verfahren
1. Orthanc Docker setup
2. Single file upload test
3. Batch upload test
4. Network failure simulation
5. Retry mechanism test

### Geschätzte Zeit: 4-6 Stunden
### Abhängigkeiten: Phase 3

---

## Phase 5: Yuan Multi-Input Control (im Service)

### Ziel
Umschaltung zwischen SDI/HDMI/Component/etc. Inputs der Yuan Karte.

### Erfolgskriterien
- [ ] Alle Inputs werden erkannt
- [ ] Switching <100ms
- [ ] UI zeigt verfügbare Inputs
- [ ] Keyboard shortcuts funktionieren

### Technische Schritte

#### 5.1 IAMCrossbar im Service
```
In CaptureService:
Datei: YuanInputController.cs
- FindCrossbarInterface()
- EnumerateInputs() 
- Route(outputPin, inputPin)
- Input type detection
- Handle via Named Pipe commands
```

#### 5.2 Service Command Extension
```
Erweiterung: ControlPipeServer.cs
- GetInputs command
- SelectInput command
- GetCurrentInput command
- Input change notifications
```

#### 5.3 UI - Source Selection
```
Änderungen: wwwroot/index.html
- Source selection buttons
- Active source indicator
- Input type icons (SDI, HDMI, etc.)
```

#### 5.4 Client-Service Communication
```
In UI App:
- SendCommand("GetInputs")
- SendCommand("SelectInput", index)
- Handle input change events
```

#### 5.5 Keyboard Shortcuts
```
Änderung: MainWindow.xaml.cs
- F1-F6 für Input switching
- Commands to Service
- Visual feedback
```

### Test-Verfahren
1. Connect multiple sources
2. Switch between all inputs
3. Verify video continuity
4. Stress test rapid switching
5. UI responsiveness test

### Geschätzte Zeit: 6-8 Stunden
### Abhängigkeiten: Phase 1

---

## Phase 5: WebRTC Integration & Multi-Source Capture

### Ziel
Yuan + WebRTC gleichzeitig, Snapshot während Video, WebRTC Streaming von Yuan Frames.

### Erfolgskriterien ✅ COMPLETED
- [x] Yuan und WebRTC parallel aktiv
- [x] Snapshot während laufendem Video
- [x] Yuan Frames via WebRTC streambar
- [x] PIP mit Yuan + WebRTC möglich
- [x] Unified capture interface

### Technische Schritte

#### 5.1 Smart Tee Multi-Branch Setup
```
In CaptureService:
Datei: MultiOutputManager.cs
- Smart Tee Filter implementation
- Branch 1: Live Preview (SharedMemory)
- Branch 2: Snapshot (High-Res Buffer)
- Branch 3: Recording (Optional)
- Snapshot ohne Video-Unterbrechung
```

#### 5.2 Unified Capture Manager
```
In UI App:
Datei: Services/UnifiedCaptureManager.cs
- Manages Yuan + WebRTC sources
- CapturePhoto() from any source
- StartRecording() for active sources
- Source switching logic
```

#### 5.3 WebRTC Yuan Bridge
```
Datei: wwwroot/webrtc-bridge.js
- Canvas from Yuan frames
- captureStream() API
- WebRTC peer connection
- Remote streaming capability
```

#### 5.4 Dual Display UI
```
Änderungen: wwwroot/index.html
- Main display (Yuan/WebRTC)
- PIP display (other source)
- Source selector
- Capture controls
```

#### 5.5 Snapshot Integration
```
- Service snapshot command
- High-res frame extraction
- Async DICOM creation
- No video interruption
```

### Test-Verfahren
1. Yuan + WebRTC simultaneous test
2. Snapshot during video recording
3. WebRTC streaming of Yuan frames
4. PIP combinations (Yuan+WebRTC)
5. Performance with both sources

### Geschätzte Zeit: 10-12 Stunden
### Abhängigkeiten: Phase 4

---

## Phase 6: PIP (Picture-in-Picture) Enhancement

### Ziel
Flexible PIP für Yuan + WebRTC Kombinationen.

### Erfolgskriterien
- [ ] Yuan als Main, WebRTC als PIP
- [ ] WebRTC als Main, Yuan als PIP
- [ ] 4 Positionen + custom size
- [ ] Smooth 60 FPS mit PIP

### Technische Schritte

#### 6.1 Hybrid PIP Compositor
```
Datei: Services/HybridPipCompositor.cs
- SharedMemory frames (Yuan)
- WebRTC frames (Canvas)
- Flexible source assignment
- GPU-accelerated wenn möglich
```

#### 6.2 JavaScript PIP Controller
```
Datei: wwwroot/pip-controller.js
- Canvas-based composition
- WebGL for performance
- Touch-draggable PIP
- Resize handles
```

#### 6.3 Layout Persistence
```
- Save PIP preferences
- Quick layout presets
- Keyboard shortcuts (Tab = cycle)
```

### Test-Verfahren
1. All PIP combinations
2. Performance profiling
3. User experience test
4. Touch interaction test

### Geschätzte Zeit: 6-8 Stunden
### Abhängigkeiten: Phase 5

---

## Phase 7: Testing & Optimization

### Ziel
Production-ready Performance und Stabilität.

### Erfolgskriterien
- [ ] 60 FPS sustained für 8 Stunden
- [ ] Memory stabil <500MB
- [ ] CPU <60% auf i5
- [ ] Keine crashes/hangs

### Test-Schritte

#### 6.1 Performance Profiling
- dotMemory für Memory leaks
- PerfView für CPU analysis
- Custom FPS counter
- Frame drop detection

#### 6.2 Stress Testing
- 8-hour continuous capture
- Rapid source switching
- PIP on/off cycling
- Queue stress (1000 files)

#### 6.3 DICOM Validation
- Test mit verschiedenen Viewers
- DICOM compliance check
- Metadata verification

#### 6.4 Integration Testing
- Full workflow test
- MWL → Capture → DICOM → PACS
- Error scenarios

### Optimierungen
- Buffer pool tuning
- GC configuration
- Thread priority
- UI responsiveness

### Geschätzte Zeit: 8-10 Stunden
### Abhängigkeiten: Phase 1-6

---

## Phase 8: Deployment & Documentation

### Ziel
Deployable Package mit Dokumentation.

### Erfolgskriterien
- [ ] Single-file installer
- [ ] Auto-update mechanism
- [ ] User documentation
- [ ] Admin guide

### Schritte

#### 7.1 Build Configuration
- Release build settings
- Code signing
- Version management

#### 7.2 Installer
- WiX oder Inno Setup
- Yuan driver check
- Prerequisites
- Shortcuts

#### 7.3 Documentation
- User manual (DE/EN)
- Quick start guide
- Troubleshooting
- Video tutorials

#### 7.4 Deployment Package
- Installer + Docs
- Config templates
- Test DICOM files

### Geschätzte Zeit: 6-8 Stunden
### Abhängigkeiten: Phase 7

---

## Gesamt-Timeline

| Phase | Beschreibung | Zeit | Start-Bedingung |
|-------|--------------|------|-----------------|
| 0 | File Lock Fix | 4-6h | Sofort |
| 1 | Service Architecture | 10-12h | Phase 0 |
| 2 | DirectShow Yuan | 8-10h | Phase 1 |
| 3 | DICOM Pipeline | 8-10h | Phase 2 |
| 4 | PACS Queue | 4-6h | Phase 3 |
| 5 | WebRTC Integration | 10-12h | Phase 4 |
| 6 | PIP Enhancement | 6-8h | Phase 5 |
| 7 | Testing | 8-10h | Phase 1-6 |
| 8 | Deployment | 6-8h | Phase 7 |

**Total: 64-82 Stunden**

## Risiken & Mitigations

### High Risk
1. **File Locks blocken Development**
   - Mitigation: Phase 0 ZUERST
   - Fallback: VM Development

2. **Session 0 Isolation Issues**
   - Mitigation: SampleGrabber statt VMR9
   - Fallback: Interactive Service

3. **IPC Performance bei 60 FPS**
   - Mitigation: SharedMemory.CircularBuffer proven
   - Fallback: Frame dropping strategy

### Medium Risk
1. **Yuan Multi-Input Switching**
   - Mitigation: IAMCrossbar standard interface
   - Fallback: Manual config

2. **WebRTC + Yuan Sync**
   - Mitigation: Independent pipelines
   - Fallback: Source priority mode

## Key Success Factors

1. **Service Architecture** ermöglicht Session 0 Capture
2. **SharedMemory** garantiert 60 FPS IPC
3. **Smart Tee** erlaubt Snapshot während Video
4. **Unified Manager** integriert Yuan + WebRTC
5. **fo-dicom** löst DICOM requirement

## Next Steps

1. Phase 0 sofort starten (File Locks!)
2. DirectShow.NET installieren und Yuan testen
3. `/compact` für einzelne Phasen nutzen

---

*Dieser Plan ist die Basis für die Implementierung. Jede Phase kann mit `/compact` effizient umgesetzt werden.*