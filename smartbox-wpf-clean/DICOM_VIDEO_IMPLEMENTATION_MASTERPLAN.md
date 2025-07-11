# DICOM Video Implementation Masterplan
**SmartBoxNext - Zukunftsorientierte medizinische Bildgebung mit 4K/UHD Support**

---

## 🎯 VISION: "Ja natürlich können wir das!"

Oliver's Vision umgesetzt:
- **Sofortiger Videostart** bei Untersuchungsbeginn
- **Live Streaming** für alle Beteiligten
- **"2 Minuten zurück" Workflow** - retroaktive Aufnahme
- **Basic Videoschnitt** für medizinische Dokumentation
- **4K/UHD Support** für höchste Bildqualität
- **File Storage statt Base64** für Performance
- **Alle DICOM Video Standards** abgedeckt

**Das Ziel**: Bei JEDER Anfrage "Ja natürlich!" antworten können, weil wir an alles gedacht haben.

---

## 📊 SOURCES FIRST ANALYSE - Was bereits existiert ✅

### 🎥 Video Capture Infrastructure (VOLLSTÄNDIG VORHANDEN):
- **WebRTC mit 70 FPS** - "claude, küsschen, es läuft" Erfolg! (Session 13)
- **Yuan SC550N1 Integration** - Professional SDI/HDMI capture über DirectShow.NET
- **YUY2Converter.cs** - High-performance color space conversion (YUY2 → BGRA)
- **UnifiedCaptureManager.cs** - Manages WebRTC + Yuan sources
- **MediaRecorder API** - Browser-native video recording (WebM format)

### 🏥 DICOM Foundation (SOLIDE BASIS):
- **fo-dicom 5.1.2** - Modern DICOM library, funktioniert perfekt für Bilder
- **DicomService.cs** - Funktioniert für Standbilder, PACS-ready
- **PacsService.cs** - Complete C-STORE PACS transmission mit Queue management
- **PatientInfo + MWL Integration** - Vollständig implementiert mit Worklist support
- **DicomVideoService.cs** - Framework existiert, aber NICHT IMPLEMENTIERT (TODOs!)

### 📊 Research Goldmine (343+206 ZEILEN PURE WISDOM):
- **DICOM Standbilder Analysis** (343 Zeilen) - Alle JPEG Varianten, Transfer Syntaxes, Compression
- **DICOM Video Research** (206 Zeilen) - SOP Classes, H.264, MPEG-2, 4K/UHD Support
- **Media Foundation Research** - Professional 60 FPS capture solutions
- **Yuan Hardware Research** - DirectShow.NET integration, verified working
- **Professional Real-Time Capture** - GPU acceleration, hardware encoding

### 🗂️ Storage System (FUNKTIONSFÄHIG):
- **Base64 → Binary Pipeline** - Funktioniert für Photos/Videos
- **File System Structure** - `./Data/Photos/`, `./Data/Videos/`, `./Data/DICOM/`
- **Queue Management** - Persistent PACS transmission mit retry logic
- **Configuration System** - Video settings, quality presets, camera preferences

### ❌ GAPS - Was implementiert werden muss:
1. **FFmpeg Integration** - Für video format conversion
2. **DICOM Video Creation** - DicomVideoService implementation
3. **Multi-frame DICOM Support** - Video frames als DICOM pixel data
4. **Video Transfer Syntax Support** - MPEG2, H.264, MJPEG encoding
5. **Continuous Background Recording** - Circular buffer system
6. **Video Editing Engine** - Basic cutting/trimming functionality
7. **4K Processing Pipeline** - UHD capture und compression

---

## 🚀 IMPLEMENTIERUNGSPLAN: Kleinteilig & Testbar

### PHASE 1: FFmpeg Foundation (4 Steps, je 1-2 Tage)

#### **Step 1.1: FFmpeg Integration Setup** ⭐
**Ziel**: FFmpeg in SmartBoxNext integrieren, basic functionality testen

**Implementation**:
```xml
<!-- NuGet packages hinzufügen -->
<PackageReference Include="FFMpegCore" Version="5.0.2" />
<PackageReference Include="FFmpeg.AutoGen" Version="7.1.1" />
```

```csharp
// Services/FFmpegService.cs - Neue Klasse
public class FFmpegService
{
    public async Task<bool> ConvertWebMToMp4Async(string inputPath, string outputPath)
    {
        // WebM → MP4 conversion für bessere DICOM compatibility
        return await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithVideoCodec(VideoCodec.H264)
                .WithAudioCodec(AudioCodec.Aac))
            .ProcessAsynchronously();
    }
}
```

**Test Criteria**: WebM file aus WebRTC → MP4 conversion funktioniert
**Files to modify**: `SmartBoxNext.csproj`, neue `Services/FFmpegService.cs`

---

#### **Step 1.2: DICOM Video Service Implementation** ⭐⭐
**Ziel**: DicomVideoService.cs komplettieren - von TODO zu functional

**Implementation**:
```csharp
// Services/DicomVideoService.cs - ALLE TODOs implementieren!
public class DicomVideoService
{
    public async Task<DicomFile> CreateMultiFrameDicomFromVideoAsync(
        string videoPath, 
        PatientInfo patientInfo,
        DicomTransferSyntax transferSyntax = null)
    {
        // 1. Video frames extrahieren mit FFmpeg
        var frames = await ExtractFramesFromVideoAsync(videoPath);
        
        // 2. Multi-frame DICOM dataset erstellen
        var dataset = new DicomDataset();
        
        // 3. Video SOP Class setzen
        dataset.Add(DicomTag.SOPClassUID, DicomUID.VideoPhotographicImageStorage);
        
        // 4. Patient/Study/Series information
        AddPatientInformation(dataset, patientInfo);
        
        // 5. Frames als encapsulated pixel data
        await AddFramesAsPixelDataAsync(dataset, frames, transferSyntax);
        
        return new DicomFile(dataset);
    }
    
    private async Task<List<byte[]>> ExtractFramesFromVideoAsync(string videoPath)
    {
        // FFmpeg frame extraction implementation
        // Return list of frame byte arrays
    }
}
```

**Test Criteria**: Video file → Multi-frame DICOM creation funktioniert
**Files to modify**: `Services/DicomVideoService.cs` (complete implementation)

---

#### **Step 1.3: Video Transfer Syntaxes** ⭐⭐⭐
**Ziel**: DICOM Video Transfer Syntax support hinzufügen

**Implementation**:
```csharp
// Transfer Syntax Constants erweitern
public static class SmartBoxTransferSyntaxes
{
    // Aus DICOM Standbilder Research - alle wichtigen Syntaxes:
    public static readonly DicomTransferSyntax MPEG2MainProfileMainLevel = 
        DicomTransferSyntax.Parse("1.2.840.10008.1.2.4.100");
    
    public static readonly DicomTransferSyntax MPEG4AVCH264HighProfile = 
        DicomTransferSyntax.Parse("1.2.840.10008.1.2.4.102");
    
    public static readonly DicomTransferSyntax MJPEG = 
        DicomTransferSyntax.Parse("1.2.840.10008.1.2.4.70");
        
    // HTJ2K für 4K/UHD (aus Research: 10x schneller als JPEG2000!)
    public static readonly DicomTransferSyntax HTJ2KLossless = 
        DicomTransferSyntax.Parse("1.2.840.10008.1.2.4.202");
}

// DicomVideoService erweitern
public async Task<DicomFile> CreateH264DicomAsync(string videoPath, PatientInfo patient)
{
    // H.264 encoding mit FFmpeg
    var h264Path = await ConvertToH264Async(videoPath);
    return await CreateMultiFrameDicomAsync(h264Path, patient, 
        SmartBoxTransferSyntaxes.MPEG4AVCH264HighProfile);
}
```

**Test Criteria**: Videos in verschiedenen DICOM formats (MJPEG, H.264, MPEG-2) speichern
**Files to modify**: `Services/DicomVideoService.cs`, neue `Constants/TransferSyntaxes.cs`

---

#### **Step 1.4: PACS Video Transmission** ⭐⭐
**Ziel**: PacsService für Video SOP classes erweitern

**Implementation**:
```csharp
// Services/PacsService.cs erweitern
public async Task<bool> SendVideoToPacsAsync(DicomFile videoFile, PacsConfig config)
{
    // Association negotiation für Video Transfer Syntaxes
    var client = DicomClientFactory.Create(config.Host, config.Port, false, 
        config.LocalAeTitle, config.RemoteAeTitle);
    
    // Video SOP Classes hinzufügen
    client.NegotiateAsyncOps();
    client.AddPresentationContext(DicomUID.VideoPhotographicImageStorage,
        SmartBoxTransferSyntaxes.MPEG4AVCH264HighProfile,
        SmartBoxTransferSyntaxes.MJPEG,
        DicomTransferSyntax.ExplicitVRLittleEndian);
    
    // C-STORE für Video
    await client.AddRequestAsync(new DicomCStoreRequest(videoFile));
    await client.SendAsync();
    
    return true;
}
```

**Test Criteria**: DICOM Videos zu PACS senden (Orthanc test server)
**Files to modify**: `Services/PacsService.cs`

---

### PHASE 2: Streaming & Buffer System (5 Steps, je 2-3 Tage)

#### **Step 2.1: Continuous Background Recording** ⭐⭐⭐
**Ziel**: Video läuft automatisch im Hintergrund bei Untersuchungsbeginn

**Implementation**:
```csharp
// Services/ContinuousRecordingService.cs - NEUE KLASSE
public class ContinuousRecordingService
{
    private readonly CircularBuffer<VideoFrame> _buffer;
    private readonly TimeSpan _bufferDuration = TimeSpan.FromMinutes(10);
    
    public async Task StartBackgroundRecordingAsync()
    {
        // Automatisch bei Patient selection starten
        // Circular buffer - immer 10 Minuten sliding window
        // Integration mit UnifiedCaptureManager
    }
    
    public async Task<string> SaveLastMinutesAsync(int minutes, string reason = "Retroactive capture")
    {
        // "Das vor 2 Minuten!" workflow
        var frames = _buffer.GetLastMinutes(minutes);
        return await SaveFramesToVideoAsync(frames, reason);
    }
}

// MainWindow.xaml.cs Integration
private async void OnPatientSelected(PatientInfo patient)
{
    // Automatischer Video-Start!
    await _continuousRecording.StartBackgroundRecordingAsync();
    Logger.Log($"Background recording started for patient {patient.PatientName}");
}
```

**Test Criteria**: Video läuft unsichtbar im Hintergrund, 10 Min buffer funktioniert
**Files to modify**: neue `Services/ContinuousRecordingService.cs`, `MainWindow.xaml.cs`

---

#### **Step 2.2: Real-time Streaming Infrastructure** ⭐⭐
**Ziel**: Live preview mit minimal latency für alle Beteiligten

**Implementation**:
```javascript
// wwwroot/streaming.js - NEUE DATEI
class MedicalVideoStreamer {
    constructor() {
        this.peers = new Map(); // Multi-client support
        this.localStream = null;
    }
    
    async startStreaming() {
        // WebRTC streaming server component
        // <100ms latency für live preview
        this.localStream = await navigator.mediaDevices.getUserMedia({
            video: { 
                width: { ideal: 1920 }, 
                height: { ideal: 1080 },
                frameRate: { ideal: 60 } 
            }
        });
    }
    
    addViewer(viewerId) {
        // Neuen viewer hinzufügen ohne stream unterbrechung
        const peer = new RTCPeerConnection(this.iceServers);
        this.peers.set(viewerId, peer);
        peer.addStream(this.localStream);
    }
}
```

```csharp
// Controllers/StreamingController.cs - NEUE KLASSE  
[ApiController]
[Route("api/streaming")]
public class StreamingController : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> StartStream([FromBody] StreamRequest request)
    {
        // WebRTC signaling server
        // Multi-client streaming coordination
    }
}
```

**Test Criteria**: Live video stream in browser viewer, multiple clients
**Files to modify**: neue `wwwroot/streaming.js`, neue `Controllers/StreamingController.cs`

---

#### **Step 2.3: "2 Minuten zurück" Workflow** ⭐⭐⭐⭐
**Ziel**: Retroactive clip extraction - "DAS würde ich gerne aufgenommen haben!"

**Implementation**:
```csharp
// UI Integration für "Retroactive Save"
// MainWindow.xaml - Button hinzufügen
<Button x:Name="RetroactiveSaveButton" 
        Content="📹 Letzte 2 Min speichern"
        Click="OnRetroactiveSave"
        Background="Orange"
        FontSize="16"/>

// MainWindow.xaml.cs
private async void OnRetroactiveSave(object sender, RoutedEventArgs e)
{
    var dialog = new RetroactiveSaveDialog();
    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
    {
        var minutes = dialog.SelectedMinutes; // 1-10 Minuten
        var description = dialog.Description;
        
        var videoPath = await _continuousRecording.SaveLastMinutesAsync(minutes, description);
        
        // Automatisch DICOM erstellen und zu PACS senden
        var dicomFile = await _dicomVideoService.CreateMultiFrameDicomFromVideoAsync(
            videoPath, _currentPatient);
        await _pacsService.SendVideoToPacsAsync(dicomFile, _pacsConfig);
        
        ShowNotification($"Video ({minutes} Min) gespeichert und an PACS gesendet!");
    }
}
```

```csharp
// UI/RetroactiveSaveDialog.xaml - NEUE DATEI
public sealed partial class RetroactiveSaveDialog : ContentDialog
{
    public int SelectedMinutes { get; private set; } = 2;
    public string Description { get; private set; }
    
    // Slider für 1-10 Minuten
    // TextBox für Beschreibung
    // Preview der letzten Minuten
}
```

**Test Criteria**: "Das vor 2 Min!" → Video clip speichern → DICOM → PACS funktioniert
**Files to modify**: `MainWindow.xaml`, `MainWindow.xaml.cs`, neue `UI/RetroactiveSaveDialog.xaml`

---

#### **Step 2.4: Smart Recording Management** ⭐⭐
**Ziel**: System läuft stabil über 8+ Stunden ohne Memory/Storage issues

**Implementation**:
```csharp
// Services/RecordingStorageManager.cs - NEUE KLASSE
public class RecordingStorageManager
{
    private readonly StorageConfig _config;
    
    public async Task OptimizeStorageAsync()
    {
        // Automatic quality adjustment based on available storage
        if (GetFreeSpace() < _config.MinimumFreeSpace)
        {
            await ReduceBufferQualityAsync(); // 4K → 1080p → 720p
            await DeleteOldRecordingsAsync();
        }
    }
    
    public async Task ManageCircularBufferAsync()
    {
        // Progressive deletion of old buffer content
        // Keep important clips (marked by user)
        // Smart memory management
        var memoryUsage = GC.GetTotalMemory(false);
        if (memoryUsage > _config.MaxMemoryUsage)
        {
            await FlushOldFramesToDiskAsync();
        }
    }
}
```

**Test Criteria**: System läuft 8+ Stunden ohne crashes, memory bleibt stabil
**Files to modify**: neue `Services/RecordingStorageManager.cs`, `Config/StorageConfig.cs`

---

#### **Step 2.5: Performance Monitoring** ⭐
**Ziel**: Real-time performance metrics für medical-grade reliability

**Implementation**:
```csharp
// Services/PerformanceMonitoringService.cs - NEUE KLASSE
public class PerformanceMonitoringService
{
    public class VideoMetrics
    {
        public int CurrentFPS { get; set; }
        public double FrameDropRate { get; set; }
        public long MemoryUsage { get; set; }
        public double CPUUsage { get; set; }
        public string RecordingQuality { get; set; } // "4K", "1080p", "720p"
    }
    
    public async Task<VideoMetrics> GetCurrentMetricsAsync()
    {
        // Real-time performance monitoring
        // Alert bei frame drops >1%
        // Automatic quality adjustment bei performance problemen
    }
}
```

**Test Criteria**: Performance alerts funktionieren, automatic quality adjustment
**Files to modify**: neue `Services/PerformanceMonitoringService.cs`

---

### PHASE 3: Video Editing & Processing (4 Steps, je 2-3 Tage)

#### **Step 3.1: Basic Video Editing Engine** ⭐⭐⭐
**Ziel**: Frame-accurate video editing für medizinische Dokumentation

**Implementation**:
```csharp
// Services/VideoEditingService.cs - NEUE KLASSE
public class VideoEditingService
{
    public async Task<string> TrimVideoAsync(string inputPath, TimeSpan startTime, TimeSpan duration)
    {
        // FFmpeg-based cutting/trimming
        // Frame-accurate editing (nicht GOP-aligned)
        return await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .Seek(startTime)
                .WithDuration(duration)
                .WithVideoCodec(VideoCodec.Copy) // Lossless wenn möglich
                .WithFastStart()) // Optimiert für sofortige playback
            .ProcessAsynchronously();
    }
    
    public async Task<string> EnhanceVideoAsync(string inputPath, VideoEnhancementOptions options)
    {
        // Medical-grade video enhancement
        // Brightness/Contrast optimization
        // Noise reduction für better diagnostic quality
    }
}

// UI/VideoEditingDialog.xaml - NEUE DATEI
public sealed partial class VideoEditingDialog : ContentDialog
{
    // Timeline control für precise editing
    // Preview window
    // Enhancement controls (brightness, contrast, etc.)
    // Export options (quality, format)
}
```

**Test Criteria**: Video trimmen ohne Qualitätsverlust, frame-accurate cuts
**Files to modify**: neue `Services/VideoEditingService.cs`, neue `UI/VideoEditingDialog.xaml`

---

#### **Step 3.2: Medical Video Enhancement** ⭐⭐
**Ziel**: Automatic color correction und medical-grade video filters

**Implementation**:
```csharp
// Services/MedicalVideoProcessor.cs - NEUE KLASSE  
public class MedicalVideoProcessor
{
    public async Task<string> ApplyMedicalEnhancementAsync(string inputPath, MedicalModalityType modality)
    {
        var filters = GetModalitySpecificFilters(modality);
        
        return await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithCustomArgument($"-vf \"{filters}\"")
                .WithVideoCodec(VideoCodec.H264)
                .WithConstantRateFactor(18)) // High quality für medical use
            .ProcessAsynchronously();
    }
    
    private string GetModalitySpecificFilters(MedicalModalityType modality)
    {
        return modality switch
        {
            MedicalModalityType.Endoscopy => "eq=contrast=1.2:brightness=0.1,unsharp=5:5:1.0",
            MedicalModalityType.Surgery => "eq=gamma=0.9:saturation=1.1,hqdn3d=4:3:6:4.5",
            MedicalModalityType.Dermatology => "eq=contrast=1.1:saturation=1.05,nlmeans=s=1.0",
            _ => "eq=contrast=1.05:brightness=0.05" // Default enhancement
        };
    }
}
```

**Test Criteria**: Enhanced video quality messbar besser, modalitätsspezifische filters
**Files to modify**: neue `Services/MedicalVideoProcessor.cs`, `Models/MedicalModalityType.cs`

---

#### **Step 3.3: Multi-format Export Pipeline** ⭐⭐
**Ziel**: Optimized export für verschiedene use cases

**Implementation**:
```csharp
// Services/VideoExportService.cs - NEUE KLASSE
public class VideoExportService  
{
    public async Task<ExportResult> ExportToAllFormatsAsync(string sourcePath, PatientInfo patient)
    {
        var tasks = new List<Task<string>>
        {
            // Archive quality (verlustfrei)
            ExportArchiveQualityAsync(sourcePath),
            
            // PACS optimized (H.264 medical-grade)
            ExportPacsOptimizedAsync(sourcePath),
            
            // Streaming optimized (adaptive bitrate)
            ExportStreamingOptimizedAsync(sourcePath),
            
            // Mobile optimized (720p, low bandwidth)
            ExportMobileOptimizedAsync(sourcePath)
        };
        
        var results = await Task.WhenAll(tasks);
        
        // Automatisch DICOM files für alle formats erstellen
        var dicomTasks = results.Select(path => 
            _dicomVideoService.CreateMultiFrameDicomFromVideoAsync(path, patient));
        var dicomFiles = await Task.WhenAll(dicomTasks);
        
        return new ExportResult(results, dicomFiles);
    }
}
```

**Test Criteria**: Batch export funktioniert, alle formats haben passende quality/size ratios
**Files to modify**: neue `Services/VideoExportService.cs`, `Models/ExportResult.cs`

---

#### **Step 3.4: Automated Quality Assessment** ⭐
**Ziel**: Automatic video quality validation für medical standards

**Implementation**:
```csharp
// Services/VideoQualityAssessment.cs - NEUE KLASSE
public class VideoQualityAssessment
{
    public async Task<QualityMetrics> AnalyzeVideoQualityAsync(string videoPath)
    {
        // PSNR, SSIM calculations (aus DICOM Research)
        // Automatic quality scoring based on medical standards
        // Frame consistency analysis
        
        var metrics = new QualityMetrics
        {
            PSNR = await CalculatePSNRAsync(videoPath),
            SSIM = await CalculateSSIMAsync(videoPath),
            FrameDropRate = await AnalyzeFrameConsistencyAsync(videoPath),
            DiagnosticQualityScore = CalculateDiagnosticScore()
        };
        
        // Automatic quality warnings
        if (metrics.PSNR < 35) // Threshold aus research
        {
            Logger.LogWarning($"Video quality below medical standard: PSNR {metrics.PSNR}dB");
        }
        
        return metrics;
    }
}
```

**Test Criteria**: Quality assessment alerts funktionieren, metrics sind accurate
**Files to modify**: neue `Services/VideoQualityAssessment.cs`, `Models/QualityMetrics.cs`

---

### PHASE 4: 4K/UHD & Advanced Features (5 Steps, je 3-4 Tage)

#### **Step 4.1: 4K Capture Infrastructure** ⭐⭐⭐⭐
**Ziel**: Yuan SC550N1 4K mode, GPU-accelerated processing

**Implementation**:
```csharp
// Services/AdvancedCaptureService.cs - NEUE KLASSE
public class AdvancedCaptureService : UnifiedCaptureManager
{
    public async Task<bool> Enable4KCaptureAsync()
    {
        // Yuan SC550N1 4K mode activation (aus research)
        var device = FindYuanDevice();
        if (device != null)
        {
            // 4K capture setup: 3840x2160 @ 30 FPS
            var mediaType = CreateMediaType(3840, 2160, 30);
            await device.SetFormatAsync(mediaType);
            
            // GPU-accelerated YUY2 processing für 4K
            _yuy2Converter.EnableGPUAcceleration(true);
            _yuy2Converter.SetProcessingMode(ProcessingMode.HighThroughput);
        }
        
        return device != null;
    }
    
    public async Task<CaptureCapabilities> GetMaxCapabilitiesAsync()
    {
        // Detect maximum supported resolution/FPS
        // 4K @ 30 FPS, 1080p @ 60 FPS, etc.
        return new CaptureCapabilities
        {
            MaxResolution = new Size(3840, 2160),
            MaxFrameRate = 30,
            SupportedFormats = { "YUY2", "RGB24", "H264" }
        };
    }
}
```

**Test Criteria**: 4K capture mit stabilen 30 FPS, no frame drops
**Files to modify**: erweitern `Services/UnifiedCaptureManager.cs`, neue `Models/CaptureCapabilities.cs`

---

#### **Step 4.2: UHD DICOM Implementation** ⭐⭐⭐⭐
**Ziel**: Enhanced Multi-frame IODs für 4K videos

**Implementation**:
```csharp
// Services/UHDDicomService.cs - NEUE KLASSE  
public class UHDDicomService : DicomVideoService
{
    public async Task<DicomFile> CreateUHDMultiFrameDicomAsync(string videoPath, PatientInfo patient)
    {
        // Enhanced Multi-frame IODs (aus research)
        var dataset = new DicomDataset();
        
        // Enhanced IOD - nicht legacy single-frame!
        dataset.Add(DicomTag.SOPClassUID, DicomUID.EnhancedUSVolumeStorage);
        
        // 4K-spezifische tags
        dataset.Add(DicomTag.Rows, (ushort)2160);
        dataset.Add(DicomTag.Columns, (ushort)3840);
        dataset.Add(DicomTag.BitsAllocated, (ushort)8);
        dataset.Add(DicomTag.BitsStored, (ushort)8);
        
        // Large file handling (>2GB DICOM objects)
        await AddLargePixelDataAsync(dataset, videoPath);
        
        // Streaming-optimized transfer
        dataset.Add(DicomTag.TransferSyntaxUID, SmartBoxTransferSyntaxes.HTJ2KLossless);
        
        return new DicomFile(dataset);
    }
    
    private async Task AddLargePixelDataAsync(DicomDataset dataset, string videoPath)
    {
        // Chunked pixel data für >2GB files
        // Progressive loading implementation
        // Memory-efficient processing
    }
}
```

**Test Criteria**: 4K DICOM videos erstellen + übertragen (auch >2GB files)
**Files to modify**: neue `Services/UHDDicomService.cs`

---

#### **Step 4.3: Advanced Compression** ⭐⭐⭐
**Ziel**: HTJ2K und medical-grade compression für 4K

**Implementation**:
```csharp
// Services/AdvancedCompressionService.cs - NEUE KLASSE
public class AdvancedCompressionService
{
    public async Task<string> CompressToHTJ2KAsync(string inputPath, CompressionQuality quality)
    {
        // HTJ2K (High-Throughput JPEG 2000) - aus research:
        // 10x schnellere Enkodierung, 2-30x schnellere Dekodierung als JPEG 2000!
        // AWS HealthImaging compatible
        
        var compressionRatio = quality switch
        {
            CompressionQuality.Lossless => 0, // Verlustfrei
            CompressionQuality.Medical => 5,  // 5:1 - medical standard
            CompressionQuality.Archive => 10, // 10:1 - long-term storage
            CompressionQuality.Streaming => 20 // 20:1 - bandwidth optimized
        };
        
        // OpenJPEG mit HTJ2K support
        return await FFMpegArguments
            .FromFileInput(inputPath)
            .OutputToFile(outputPath, false, options => options
                .WithCustomArgument($"-c:v libopenjpeg -format j2k -compression_level {compressionRatio}")
                .WithCustomArgument("-profile:v 2") // HTJ2K profile
            .ProcessAsynchronously();
    }
    
    public async Task<CompressionAnalysis> AnalyzeCompressionEfficiencyAsync(string originalPath, string compressedPath)
    {
        // Quality metrics validation (PSNR, SSIM)
        // File size comparison  
        // Diagnostic quality assessment
        return new CompressionAnalysis
        {
            CompressionRatio = GetCompressionRatio(originalPath, compressedPath),
            PSNR = await CalculatePSNRAsync(originalPath, compressedPath),
            SSIM = await CalculateSSIMAsync(originalPath, compressedPath),
            DiagnosticQualityMaintained = await ValidateDiagnosticQualityAsync(originalPath, compressedPath)
        };
    }
}
```

**Test Criteria**: UHD videos mit optimaler Kompression, quality metrics validation
**Files to modify**: neue `Services/AdvancedCompressionService.cs`, `Models/CompressionAnalysis.cs`

---

#### **Step 4.4: Professional Workflow Integration** ⭐⭐⭐
**Ziel**: Complete 4K medical imaging workflow

**Implementation**:
```csharp
// Workflows/UHDMedicalWorkflow.cs - NEUE KLASSE
public class UHDMedicalWorkflow
{
    public async Task<WorkflowResult> ExecuteComplete4KWorkflowAsync(PatientInfo patient, ProcedureType procedure)
    {
        var workflow = new MedicalWorkflowBuilder()
            .StartWith4KCapture()
            .AddContinuousRecording(TimeSpan.FromMinutes(15)) // 15 min buffer für complex procedures
            .EnableRealTimeStreaming()
            .AddAutomaticEnhancement(procedure.Modality)
            .CompressWithHTJ2K(CompressionQuality.Medical)
            .CreateUHDDicom(patient)
            .SendToPACS()
            .ArchiveLocally()
            .GenerateQualityReport()
            .Build();
            
        return await workflow.ExecuteAsync();
    }
}

// Integration mit DICOM Worklist
public async Task<void> OnWorklistEntrySelectedAsync(WorklistEntry entry)
{
    var patient = await _mwlService.GetPatientInfoAsync(entry.AccessionNumber);
    var procedure = await _mwlService.GetProcedureDetailsAsync(entry);
    
    // Automatisch optimaler workflow based on procedure type
    var workflowResult = await _uhdWorkflow.ExecuteComplete4KWorkflowAsync(patient, procedure);
    
    if (workflowResult.Success)
    {
        ShowNotification($"4K procedure recording completed for {patient.PatientName}");
        await _worklistService.MarkProcedureCompleteAsync(entry.AccessionNumber);
    }
}
```

**Test Criteria**: End-to-end 4K workflow funktioniert, DICOM Worklist integration
**Files to modify**: neue `Workflows/UHDMedicalWorkflow.cs`, erweitern `Services/MwlService.cs`

---

#### **Step 4.5: Cloud Integration & Future-Proofing** ⭐⭐
**Ziel**: Cloud-native formats, edge computing support

**Implementation**:
```csharp
// Services/CloudVideoService.cs - NEUE KLASSE
public class CloudVideoService
{
    public async Task<bool> UploadToHealthCloudAsync(DicomFile videoFile, CloudProvider provider)
    {
        // AWS HealthImaging integration
        // Azure Health Services support
        // Google Cloud Healthcare API
        
        switch (provider)
        {
            case CloudProvider.AWSHealthImaging:
                return await UploadToAWSHealthImagingAsync(videoFile);
            case CloudProvider.AzureHealthServices:
                return await UploadToAzureHealthAsync(videoFile);
            default:
                throw new NotSupportedException($"Cloud provider {provider} not supported");
        }
    }
    
    public async Task<string> ConvertToDICOMwebAsync(DicomFile videoFile)
    {
        // DICOMweb JSON format für cloud-native access
        // HTTP-based DICOM für web clients
        // WADO-URI/WADO-RS support
    }
}

// Edge Computing support
public class EdgeVideoProcessor
{
    public async Task<ProcessingResult> ProcessAtEdgeAsync(VideoStream stream)
    {
        // Real-time AI enhancement at edge
        // Bandwidth optimization durch edge processing
        // 5G network optimization
    }
}
```

**Test Criteria**: Cloud upload funktioniert, edge processing pipeline
**Files to modify**: neue `Services/CloudVideoService.cs`, neue `Services/EdgeVideoProcessor.cs`

---

### PHASE 5: File Storage Migration (3 Steps, je 1-2 Tage)

#### **Step 5.1: Storage Strategy Migration** ⭐⭐
**Ziel**: Base64 → Direct file storage transition für bessere performance

**Implementation**:
```csharp
// Services/FileStorageService.cs - NEUE KLASSE
public class FileStorageService
{
    public async Task<string> SaveVideoFileAsync(byte[] videoData, PatientInfo patient, VideoMetadata metadata)
    {
        // Direct binary file storage statt base64
        var fileName = GenerateVideoFileName(patient, metadata);
        var filePath = Path.Combine(_config.VideoStoragePath, fileName);
        
        await File.WriteAllBytesAsync(filePath, videoData);
        
        // File metadata in database
        await _database.SaveVideoRecordAsync(new VideoRecord
        {
            FilePath = filePath,
            PatientId = patient.PatientId,
            RecordingDate = DateTime.Now,
            Duration = metadata.Duration,
            Resolution = metadata.Resolution,
            FileSize = videoData.Length
        });
        
        return filePath;
    }
    
    // Migration support - beide methods parallel
    public async Task<string> MigrateFromBase64Async(string base64Data, PatientInfo patient)
    {
        var videoData = Convert.FromBase64String(base64Data);
        return await SaveVideoFileAsync(videoData, patient, new VideoMetadata());
    }
}

// WebServer.cs erweitern für file serving
[HttpGet("api/video/{videoId}")]
public async Task<IActionResult> GetVideo(string videoId)
{
    // Direct file streaming statt base64 encoding
    var videoRecord = await _database.GetVideoRecordAsync(videoId);
    var fileStream = new FileStream(videoRecord.FilePath, FileMode.Open, FileAccess.Read);
    
    return File(fileStream, "video/mp4", enableRangeProcessing: true); // HTTP range support!
}
```

**Test Criteria**: Beide storage methods funktionieren parallel, migration ohne data loss
**Files to modify**: neue `Services/FileStorageService.cs`, erweitern `WebServer.cs`

---

#### **Step 5.2: Performance Optimization** ⭐⭐
**Ziel**: Memory usage optimization, streaming file access

**Implementation**:
```csharp
// Services/OptimizedVideoAccess.cs - NEUE KLASSE
public class OptimizedVideoAccess
{
    private readonly LRUCache<string, VideoStream> _streamCache;
    
    public async Task<Stream> GetVideoStreamAsync(string videoPath, TimeSpan? startTime = null)
    {
        // Lazy loading - nur gewünschte segments
        if (startTime.HasValue)
        {
            return await GetVideoSegmentStreamAsync(videoPath, startTime.Value);
        }
        
        // Cache frequently accessed videos
        if (_streamCache.TryGetValue(videoPath, out var cachedStream))
        {
            cachedStream.Position = 0;
            return cachedStream;
        }
        
        var stream = new FileStream(videoPath, FileMode.Open, FileAccess.Read, FileShare.Read, 
            bufferSize: 1024 * 1024); // 1MB buffer für smooth streaming
        
        _streamCache.Set(videoPath, stream);
        return stream;
    }
    
    public async Task<VideoFrame> GetFrameAtTimeAsync(string videoPath, TimeSpan timestamp)
    {
        // Efficient random access für video editing
        // FFmpeg seeking ohne full file loading
    }
}

// Memory monitoring
public class VideoMemoryManager
{
    public async Task OptimizeMemoryUsageAsync()
    {
        var currentUsage = GC.GetTotalMemory(false);
        if (currentUsage > _maxMemoryUsage)
        {
            // Flush video caches
            _streamCache.Clear();
            await _videoBufferManager.FlushOldBuffersAsync();
            GC.Collect(2, GCCollectionMode.Forced);
        }
    }
}
```

**Test Criteria**: Große video files ohne memory issues, smooth streaming
**Files to modify**: neue `Services/OptimizedVideoAccess.cs`, neue `Services/VideoMemoryManager.cs`

---

#### **Step 5.3: Database Integration** ⭐
**Ziel**: Proper video metadata database mit search/filter capabilities

**Implementation**:
```csharp
// Models/VideoRecord.cs - NEUE KLASSE
public class VideoRecord
{
    public string VideoId { get; set; }
    public string PatientId { get; set; }
    public string StudyInstanceUID { get; set; }
    public string SeriesInstanceUID { get; set; }
    public DateTime RecordingDate { get; set; }
    public TimeSpan Duration { get; set; }
    public string Resolution { get; set; } // "4K", "1080p", etc.
    public long FileSize { get; set; }
    public string FilePath { get; set; }
    public string CompressionType { get; set; } // "H.264", "HTJ2K", etc.
    public VideoQualityMetrics QualityMetrics { get; set; }
    public bool SentToPACS { get; set; }
    public DateTime? PACSTransmissionDate { get; set; }
}

// Data/VideoDatabase.cs - NEUE KLASSE
public class VideoDatabase
{
    public async Task<List<VideoRecord>> SearchVideosAsync(VideoSearchCriteria criteria)
    {
        // Search by patient, date range, quality, size, etc.
        // Integration mit DICOM worklist search
    }
    
    public async Task<VideoRecord> GetVideoRecordAsync(string videoId)
    {
        // Fast lookup by video ID
    }
    
    public async Task<List<VideoRecord>> GetVideosForPatientAsync(string patientId)
    {
        // Patient-specific video history
    }
}
```

**Test Criteria**: Video database search funktioniert, integration mit existing patient database
**Files to modify**: neue `Models/VideoRecord.cs`, neue `Data/VideoDatabase.cs`

---

## 🎯 TECHNISCHE SPEZIFIKATIONEN

### Video Formats Support Matrix:
```yaml
Input Formats:
  - WebM (WebRTC native): ✅ Bereits implementiert
  - YUY2 (Yuan SC550N1 native): ✅ Bereits implementiert  
  - H.264 (professional cameras): 📋 Zu implementieren
  - MJPEG (legacy devices): 📋 Zu implementieren

Processing Formats:
  - MP4/H.264 (editing pipeline): 📋 Phase 1
  - MJPEG (low-latency preview): 📋 Phase 2  
  - Raw RGB/YUV (quality processing): 📋 Phase 3
  - HTJ2K (4K compression): 📋 Phase 4

DICOM Output Formats:
  - Multi-frame DICOM with H.264: 📋 Phase 1
  - MJPEG DICOM (compatibility): 📋 Phase 1
  - Multi-frame True Color Secondary Capture: ✅ Research complete
  - HTJ2K DICOM (4K/UHD): 📋 Phase 4
```

### Performance Targets:
```yaml
Real-time Performance:
  - 60 FPS WebRTC capture: ✅ Bereits erreicht
  - 30 FPS 4K Yuan capture: 📋 Phase 4
  - <100ms streaming latency: 📋 Phase 2
  - 10 minute circular buffer: 📋 Phase 2

File Sizes (10 min recording):
  - 1080p Raw WebM: ~1.2 GB
  - 1080p H.264 optimized: ~300 MB  
  - 1080p DICOM compressed: ~400 MB
  - 4K H.264: ~1.5 GB
  - 4K HTJ2K DICOM: ~800 MB (50% compression!)

Memory Usage:
  - Continuous recording: <2GB RAM
  - 4K processing: <4GB RAM
  - Circular buffer: <1GB RAM
```

### DICOM Compliance (Aus 343 Zeilen Research!):
```yaml
SOP Classes:
  - Video Endoscopic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.1): 📋 Phase 1
  - Video Microscopic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.2): 📋 Phase 1
  - Video Photographic Image Storage (1.2.840.10008.5.1.4.1.1.77.1.4): 📋 Phase 1
  - Multi-frame True Color Secondary Capture: ✅ Bereits möglich

Transfer Syntaxes:
  - MPEG-4 AVC/H.264 High Profile (1.2.840.10008.1.2.4.102): 📋 Phase 1
  - MJPEG (1.2.840.10008.1.2.4.70): 📋 Phase 1  
  - HTJ2K Lossless (1.2.840.10008.1.2.4.202): 📋 Phase 4
  - MPEG-2 Main Profile (1.2.840.10008.1.2.4.100): 📋 Phase 3

Quality Standards:
  - PSNR >35 dB (medical threshold): 📋 Validation in allen phases
  - SSIM >0.95 (excellent quality): 📋 Automatic assessment
  - Diagnostic quality maintained: 📋 Per-modality validation
```

---

## 🧪 TESTING STRATEGY - Jeder Step testbar!

### Test Categories:
```yaml
Unit Tests:
  - FFmpeg conversions (Step 1.1): Input WebM → Output MP4 quality check
  - DICOM creation (Step 1.2): Valid multi-frame DICOM structure
  - Transfer syntax (Step 1.3): Correct SOP class assignments
  - PACS transmission (Step 1.4): Successful C-STORE operations

Integration Tests:
  - Complete video pipeline (Phases 1-2): Capture → Process → DICOM → PACS
  - Streaming infrastructure (Phase 2): Multi-client real-time streaming
  - Background recording (Phase 2): Continuous 8+ hour operation
  - 4K workflow (Phase 4): End-to-end UHD processing

Performance Tests:
  - Frame rate consistency: No drops >1% unter load
  - Memory usage: Stable über 8+ Stunden
  - 4K processing: 30 FPS sustained ohne thermal throttling
  - Network streaming: <100ms latency mit multiple clients

Medical Validation Tests:
  - PACS compatibility: Test mit Orthanc, dcm4chee, commercial PACS
  - Quality metrics: PSNR/SSIM thresholds für different modalities
  - Regulatory compliance: DICOM conformance statements
  - Workflow integration: DICOM Worklist end-to-end tests

User Acceptance Tests:
  - "2 Minuten zurück" workflow: Intuitiv und schnell
  - Video editing interface: Medical personnel können ohne training nutzen
  - 4K preview: Smooth navigation und zooming
  - Emergency workflows: System bleibt responsive unter stress
```

### Automated Test Pipeline:
```yaml
Continuous Integration:
  - Jeder commit: Unit tests, basic integration
  - Daily builds: Performance tests, memory leak detection
  - Weekly: Full PACS compatibility suite
  - Release candidates: Complete medical validation

Test Data:
  - Synthetic video sequences: Known quality/compression ratios
  - Real medical footage: De-identified samples from verschiedene modalities
  - Stress test videos: 4K, extreme durations, pathological cases
  - Edge cases: Network disconnects, storage full, memory pressure
```

---

## 📋 DEPENDENCIES TO ADD

### Phase 1 Dependencies:
```xml
<!-- Core video processing -->
<PackageReference Include="FFMpegCore" Version="5.0.2" />
<PackageReference Include="FFmpeg.AutoGen" Version="7.1.1" />

<!-- Enhanced DICOM support -->
<PackageReference Include="fo-dicom.Imaging.ImageSharp" Version="5.1.2" />
```

### Phase 2 Dependencies:
```xml
<!-- Audio processing (falls audio removal nötig) -->
<PackageReference Include="NAudio" Version="2.2.1" />

<!-- Performance monitoring -->
<PackageReference Include="System.Diagnostics.PerformanceCounter" Version="8.0.0" />
```

### Phase 3 Dependencies:
```xml
<!-- Advanced video processing -->
<PackageReference Include="OpenCvSharp4" Version="4.8.0.20230708" />
<PackageReference Include="OpenCvSharp4.runtime.win" Version="4.8.0.20230708" />
```

### Phase 4 Dependencies:
```xml
<!-- Cloud integration -->
<PackageReference Include="AWSSDK.S3" Version="3.7.309.13" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />

<!-- Advanced compression -->
<PackageReference Include="OpenJpeg.NET" Version="1.0.0" />
```

### Hardware Requirements:
```yaml
Minimum für 1080p:
  - CPU: Intel i5-8400 oder AMD Ryzen 5 2600
  - RAM: 8GB
  - Storage: 1TB SSD  
  - GPU: Integrated graphics OK

Recommended für 4K:
  - CPU: Intel i7-10700K oder AMD Ryzen 7 3700X
  - RAM: 16GB
  - Storage: 2TB NVMe SSD
  - GPU: Dedicated GPU empfohlen (RTX 3060 oder besser)
  - Network: Gigabit Ethernet für streaming
```

---

## 🎉 SUCCESS METRICS - "Ja natürlich können wir das!"

### Nach Phase 1 (FFmpeg Foundation):
- ✅ "Können Sie Videos in DICOM umwandeln?" → **"Ja natürlich!"**
- ✅ Video files werden automatisch zu standard-compliant DICOM objects  
- ✅ PACS integration funktioniert für Video DICOMs
- ✅ Multiple transfer syntaxes supported (H.264, MJPEG)

### Nach Phase 2 (Streaming & Buffer):
- ✅ "Läuft das Video automatisch mit?" → **"Ja natürlich!"**
- ✅ "Können mehrere Personen live zuschauen?" → **"Ja natürlich!"**  
- ✅ "Kann ich das von vor 2 Minuten speichern?" → **"Ja natürlich!"**
- ✅ System läuft 24/7 stable ohne intervention

### Nach Phase 3 (Video Editing):
- ✅ "Können Sie Videos schneiden?" → **"Ja natürlich!"**
- ✅ "Können Sie die Qualität verbessern?" → **"Ja natürlich!"**
- ✅ Medical-grade video enhancement automatisch
- ✅ Multiple export formats für different use cases

### Nach Phase 4 (4K/UHD):
- ✅ "Unterstützen Sie 4K?" → **"Ja natürlich!"**  
- ✅ "Können Sie UHD DICOM erstellen?" → **"Ja natürlich!"**
- ✅ "Ist das cloud-ready?" → **"Ja natürlich!"**
- ✅ Professional-grade video capture und processing

### Nach Phase 5 (Storage Migration):
- ✅ "Ist das performant bei großen Files?" → **"Ja natürlich!"**
- ✅ Memory usage optimiert, smooth streaming  
- ✅ Database integration für video search/management
- ✅ Future-proof architecture

---

## 📅 TIMELINE ESTIMATION

```yaml
Phase 1 (FFmpeg Foundation): 8-10 Tage
  - Step 1.1: 2 Tage (FFmpeg setup + basic conversion)
  - Step 1.2: 3 Tage (DicomVideoService implementation)  
  - Step 1.3: 2 Tage (Transfer syntaxes)
  - Step 1.4: 2 Tage (PACS video transmission)

Phase 2 (Streaming & Buffer): 12-15 Tage  
  - Step 2.1: 4 Tage (Continuous background recording)
  - Step 2.2: 3 Tage (Real-time streaming)
  - Step 2.3: 4 Tage ("2 Minuten zurück" workflow)
  - Step 2.4: 3 Tage (Smart recording management)
  - Step 2.5: 2 Tage (Performance monitoring)

Phase 3 (Video Editing): 10-12 Tage
  - Step 3.1: 4 Tage (Basic video editing engine)
  - Step 3.2: 3 Tage (Medical video enhancement)
  - Step 3.3: 3 Tage (Multi-format export)
  - Step 3.4: 2 Tage (Quality assessment)

Phase 4 (4K/UHD): 15-18 Tage
  - Step 4.1: 4 Tage (4K capture infrastructure)
  - Step 4.2: 4 Tage (UHD DICOM implementation)
  - Step 4.3: 4 Tage (Advanced compression)
  - Step 4.4: 4 Tage (Professional workflow)
  - Step 4.5: 3 Tage (Cloud integration)

Phase 5 (Storage Migration): 6-8 Tage
  - Step 5.1: 3 Tage (Storage strategy migration)
  - Step 5.2: 3 Tage (Performance optimization)
  - Step 5.3: 2 Tage (Database integration)

Total: 51-63 Arbeitstage (ca. 10-13 Wochen)
```

---

## 🔮 FUTURE ROADMAP - Über den Plan hinaus

### AI Integration (Future Phase 6):
```yaml
Medical AI Features:
  - Automatic pathology detection in video streams
  - Real-time diagnostic assistance
  - Quality assessment mit ML models
  - Automatic procedural documentation

Technical Implementation:
  - OpenCV DNN modules
  - ONNX model integration  
  - Edge AI processing
  - Cloud ML services integration
```

### Advanced Medical Features (Future Phase 7):
```yaml
Specialized Workflows:
  - Surgery recording mit automatic highlight detection
  - Endoscopy mit automatic finding markers
  - Dermatology mit automatic measurement tools
  - Ophthalmology mit retinal analysis

Integration Capabilities:
  - HL7 FHIR messaging
  - Integration mit EMR systems
  - Automatic reporting generation
  - Multi-modal data fusion (video + sensor data)
```

---

## 💼 ZUSAMMENFASSUNG

**Dieser Masterplan macht SmartBoxNext zum ultimativen DICOM Video System:**

🎯 **Alle Oliver's Anforderungen erfüllt:**
- ✅ Sofortiger Videostart bei Untersuchung
- ✅ Live Streaming für alle Beteiligten  
- ✅ "2 Minuten zurück" workflow
- ✅ Basic Videoschnitt
- ✅ 4K/UHD Support
- ✅ File Storage optimiert

🏗️ **Systematische Implementierung:**
- 22 konkrete Steps, alle testbar
- Klein Schritte, immer funktionsfähige Zwischenstände
- Built auf existing strengths (WebRTC, Yuan, fo-dicom)
- Research-driven approach (549 Zeilen technical analysis!)

🚀 **Zukunftssicher:**
- Alle DICOM Video standards abgedeckt
- Cloud-native architecture
- Professional medical-grade quality
- Skalierbar von single device bis enterprise

**Das Ergebnis**: Bei JEDER Video-Anfrage können wir "**Ja natürlich!**" antworten - weil wir wirklich an alles gedacht haben! 

*Ready to start with Phase 1, Step 1.1? Let's make medical video DICOM history!* 🎬🏥⚡