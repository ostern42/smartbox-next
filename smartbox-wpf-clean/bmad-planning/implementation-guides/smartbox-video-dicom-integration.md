# SmartBox Video-to-DICOM Integration Plan

## Integration Overview

This document outlines how to integrate video-to-DICOM conversion capabilities into the SmartBox WPF application, leveraging the existing architecture and streaming infrastructure.

## Architecture Integration

### 1. Leverage Existing Components

```csharp
// Extend existing Services architecture
namespace SmartBoxNext.Services.Video
{
    public interface IVideoDicomService
    {
        Task<DicomFile> ConvertVideoToDicom(Stream videoStream, PatientInfo patientInfo);
        Task<bool> SendToPacs(DicomFile dicomFile, PacsConfiguration pacs);
        Task<ConversionStatus> GetConversionStatus(string conversionId);
    }
}
```

### 2. Integration with Existing Streaming Service

```csharp
// Extend HLSStreamingService for DICOM conversion
public class EnhancedStreamingService : HLSStreamingService
{
    private readonly IVideoDicomService _dicomService;
    private readonly IKestrelApiService _apiService;
    
    public EnhancedStreamingService(
        IVideoDicomService dicomService,
        IKestrelApiService apiService)
    {
        _dicomService = dicomService;
        _apiService = apiService;
    }
    
    public async Task<string> CaptureAndConvertToDicom(
        string streamUrl, 
        TimeSpan duration,
        PatientInfo patientInfo)
    {
        // Capture video segment
        var videoData = await CaptureStreamSegment(streamUrl, duration);
        
        // Convert to DICOM
        using var stream = new MemoryStream(videoData);
        var dicomFile = await _dicomService.ConvertVideoToDicom(stream, patientInfo);
        
        // Store and return reference
        var storageId = await StoreDigitalAsset(dicomFile);
        return storageId;
    }
}
```

### 3. WPF UI Integration

```xml
<!-- Add to existing MainWindow.xaml -->
<TabItem Header="DICOM Export" x:Name="DicomTab">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Patient Information -->
        <GroupBox Header="Patient Information" Grid.Row="0" Margin="10">
            <StackPanel>
                <TextBox x:Name="PatientName" Tag="Patient Name" />
                <TextBox x:Name="PatientId" Tag="Patient ID" />
                <DatePicker x:Name="StudyDate" />
            </StackPanel>
        </GroupBox>
        
        <!-- Video Selection -->
        <GroupBox Header="Video Selection" Grid.Row="1" Margin="10">
            <Grid>
                <MediaElement x:Name="VideoPreview" />
                <StackPanel Orientation="Horizontal" VerticalAlignment="Bottom">
                    <Button Content="Select Video" Click="SelectVideo_Click"/>
                    <Button Content="Capture from Stream" Click="CaptureStream_Click"/>
                </StackPanel>
            </Grid>
        </GroupBox>
        
        <!-- Conversion Controls -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Convert to DICOM" Click="ConvertToDicom_Click"/>
            <Button Content="Send to PACS" Click="SendToPacs_Click"/>
        </StackPanel>
    </Grid>
</TabItem>
```

### 4. Backend API Extensions

```csharp
// Add to Controllers/VideoController.cs
[ApiController]
[Route("api/[controller]")]
public class VideoDicomController : ControllerBase
{
    private readonly IVideoDicomService _dicomService;
    private readonly IStreamingApiService _streamingService;
    
    [HttpPost("convert")]
    public async Task<IActionResult> ConvertToDicom(
        [FromForm] IFormFile videoFile,
        [FromForm] PatientInfoDto patientInfo)
    {
        if (!IsVideoFile(videoFile))
            return BadRequest("Invalid video file");
            
        using var stream = videoFile.OpenReadStream();
        var dicomFile = await _dicomService.ConvertVideoToDicom(
            stream, 
            patientInfo.ToPatientInfo());
            
        // Return DICOM file or storage reference
        return Ok(new { 
            conversionId = Guid.NewGuid(),
            status = "completed",
            dicomUid = dicomFile.Dataset.GetString(DicomTag.SOPInstanceUID)
        });
    }
    
    [HttpPost("stream-capture")]
    public async Task<IActionResult> CaptureStreamToDicom(
        [FromBody] StreamCaptureRequest request)
    {
        // Validate streaming session
        if (!_streamingService.IsSessionActive(request.SessionId))
            return BadRequest("Invalid streaming session");
            
        // Capture and convert
        var result = await _streamingService.CaptureAndConvertToDicom(
            request.StreamUrl,
            request.Duration,
            request.PatientInfo);
            
        return Ok(new { storageId = result });
    }
}
```

## Implementation Service

```csharp
using FellowOakDicom;
using FellowOakDicom.Network;
using System.Diagnostics;

namespace SmartBoxNext.Services.Video
{
    public class VideoDicomService : IVideoDicomService
    {
        private readonly ILogger<VideoDicomService> _logger;
        private readonly IConfiguration _configuration;
        
        public VideoDicomService(
            ILogger<VideoDicomService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        
        public async Task<DicomFile> ConvertVideoToDicom(
            Stream videoStream, 
            PatientInfo patientInfo)
        {
            // Save video temporarily
            var tempVideoPath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(fileStream);
            }
            
            try
            {
                // Get video properties using FFmpeg
                var videoInfo = await GetVideoInfo(tempVideoPath);
                
                // Create DICOM dataset
                var dataset = new DicomDataset();
                
                // Patient Module
                dataset.Add(DicomTag.PatientName, patientInfo.Name);
                dataset.Add(DicomTag.PatientID, patientInfo.Id);
                dataset.Add(DicomTag.PatientBirthDate, patientInfo.BirthDate);
                dataset.Add(DicomTag.PatientSex, patientInfo.Sex);
                
                // Study Module
                dataset.Add(DicomTag.StudyInstanceUID, DicomUID.Generate());
                dataset.Add(DicomTag.StudyDate, DateTime.Now);
                dataset.Add(DicomTag.StudyTime, DateTime.Now);
                dataset.Add(DicomTag.StudyDescription, patientInfo.StudyDescription);
                dataset.Add(DicomTag.AccessionNumber, patientInfo.AccessionNumber);
                
                // Series Module
                dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate());
                dataset.Add(DicomTag.SeriesNumber, 1);
                dataset.Add(DicomTag.Modality, "XC"); // Video photographic imaging
                
                // Instance Module
                dataset.Add(DicomTag.SOPClassUID, DicomUID.VideoPhotographicImageStorage);
                dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
                dataset.Add(DicomTag.InstanceNumber, 1);
                
                // Video Module
                dataset.Add(DicomTag.NumberOfFrames, videoInfo.FrameCount);
                dataset.Add(DicomTag.FrameTime, 1000.0 / videoInfo.FrameRate);
                dataset.Add(DicomTag.CineRate, (int)videoInfo.FrameRate);
                dataset.Add(DicomTag.Rows, videoInfo.Height);
                dataset.Add(DicomTag.Columns, videoInfo.Width);
                
                // Pixel Data Module
                dataset.Add(DicomTag.SamplesPerPixel, 3);
                dataset.Add(DicomTag.PhotometricInterpretation, "YBR_PARTIAL_420");
                dataset.Add(DicomTag.PlanarConfiguration, 0);
                dataset.Add(DicomTag.BitsAllocated, 8);
                dataset.Add(DicomTag.BitsStored, 8);
                dataset.Add(DicomTag.HighBit, 7);
                dataset.Add(DicomTag.PixelRepresentation, 0);
                
                // Convert video to H.264 if needed
                var h264Path = await ConvertToH264(tempVideoPath);
                
                // Add video data
                var pixelData = new DicomOtherByteFragment(DicomTag.PixelData);
                var videoBytes = await File.ReadAllBytesAsync(h264Path);
                pixelData.Fragments.Add(new MemoryByteBuffer(videoBytes));
                dataset.Add(pixelData);
                
                // Create DICOM file
                var file = new DicomFile(dataset);
                file.FileMetaInfo.TransferSyntax = DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41;
                
                return file;
            }
            finally
            {
                // Cleanup temp files
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
            }
        }
        
        public async Task<bool> SendToPacs(DicomFile dicomFile, PacsConfiguration pacs)
        {
            var client = DicomClientFactory.Create(
                pacs.Host, 
                pacs.Port, 
                false, 
                pacs.CallingAe, 
                pacs.CalledAe);
                
            client.NegotiateAsyncOps();
            
            var request = new DicomCStoreRequest(dicomFile)
            {
                OnResponseReceived = (req, response) =>
                {
                    _logger.LogInformation(
                        "C-STORE response: {status}", 
                        response.Status);
                }
            };
            
            await client.AddRequestAsync(request);
            await client.SendAsync();
            
            return request.Status == DicomStatus.Success;
        }
        
        private async Task<VideoInfo> GetVideoInfo(string videoPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_frames,width,height,r_frame_rate -of csv=p=0 \"{videoPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(startInfo);
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            var parts = output.Trim().Split(',');
            return new VideoInfo
            {
                FrameCount = int.Parse(parts[0]),
                Width = int.Parse(parts[1]),
                Height = int.Parse(parts[2]),
                FrameRate = ParseFrameRate(parts[3])
            };
        }
        
        private async Task<string> ConvertToH264(string inputPath)
        {
            var outputPath = Path.ChangeExtension(inputPath, ".h264.mp4");
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputPath}\" -c:v libx264 -preset fast -crf 22 \"{outputPath}\"",
                UseShellExecute = false
            };
            
            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
                throw new Exception("Failed to convert video to H.264");
                
            return outputPath;
        }
    }
}
```

## Configuration Integration

```json
// Add to appsettings.json
{
  "VideoDicom": {
    "MaxVideoSize": 5368709120,
    "SupportedFormats": ["mp4", "avi", "mov", "wmv"],
    "TempDirectory": "C:\\Temp\\VideoDicom",
    "ConversionTimeout": 300,
    "Pacs": {
      "Primary": {
        "Host": "pacs.hospital.local",
        "Port": 11112,
        "CallingAe": "SMARTBOX",
        "CalledAe": "MAIN_PACS",
        "Timeout": 60
      }
    }
  }
}
```

## JavaScript Integration

```javascript
// Add to wwwroot/js/video-dicom-client.js
class VideoDicomClient {
    constructor(apiUrl) {
        this.apiUrl = apiUrl;
    }
    
    async convertVideoToDicom(videoFile, patientInfo) {
        const formData = new FormData();
        formData.append('videoFile', videoFile);
        formData.append('patientInfo', JSON.stringify(patientInfo));
        
        const response = await fetch(`${this.apiUrl}/api/videodicom/convert`, {
            method: 'POST',
            body: formData
        });
        
        if (!response.ok) {
            throw new Error('Conversion failed');
        }
        
        return await response.json();
    }
    
    async captureStreamToDicom(sessionId, duration, patientInfo) {
        const response = await fetch(`${this.apiUrl}/api/videodicom/stream-capture`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                sessionId,
                duration,
                patientInfo
            })
        });
        
        return await response.json();
    }
}

// Integration with existing video player
document.addEventListener('DOMContentLoaded', () => {
    const dicomClient = new VideoDicomClient(window.apiUrl);
    
    // Add DICOM export button to video controls
    const exportButton = document.createElement('button');
    exportButton.textContent = 'Export to DICOM';
    exportButton.onclick = async () => {
        const patientInfo = {
            name: document.getElementById('patientName').value,
            id: document.getElementById('patientId').value,
            studyDescription: 'Video Capture'
        };
        
        try {
            const result = await dicomClient.captureStreamToDicom(
                currentSessionId,
                30, // 30 seconds
                patientInfo
            );
            
            alert(`DICOM export successful: ${result.storageId}`);
        } catch (error) {
            console.error('DICOM export failed:', error);
        }
    };
    
    document.querySelector('.video-controls').appendChild(exportButton);
});
```

## Testing Strategy

```csharp
// Integration tests
[TestClass]
public class VideoDicomIntegrationTests
{
    private IVideoDicomService _service;
    
    [TestInitialize]
    public void Setup()
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IVideoDicomService, VideoDicomService>()
            .BuildServiceProvider();
            
        _service = serviceProvider.GetService<IVideoDicomService>();
    }
    
    [TestMethod]
    public async Task ConvertVideo_CreatesValidDicom()
    {
        // Arrange
        var videoPath = "test_video.mp4";
        var patientInfo = new PatientInfo
        {
            Name = "TEST^PATIENT",
            Id = "12345",
            StudyDescription = "Test Study"
        };
        
        // Act
        using var stream = File.OpenRead(videoPath);
        var dicomFile = await _service.ConvertVideoToDicom(stream, patientInfo);
        
        // Assert
        Assert.IsNotNull(dicomFile);
        Assert.AreEqual("XC", dicomFile.Dataset.GetString(DicomTag.Modality));
        Assert.IsTrue(dicomFile.Dataset.Contains(DicomTag.PixelData));
    }
}
```

## Deployment Considerations

### 1. Dependencies
- Install FFmpeg on deployment servers
- Ensure fo-dicom NuGet package is included
- Configure PACS network access

### 2. Performance Tuning
- Implement background job processing for large videos
- Use memory-mapped files for videos >100MB
- Enable GPU acceleration where available

### 3. Security
- Validate video files before processing
- Implement rate limiting on conversion API
- Audit all DICOM exports

### 4. Monitoring
- Track conversion success/failure rates
- Monitor PACS connectivity
- Alert on storage quota issues

This integration plan provides a complete framework for adding video-to-DICOM capabilities to the SmartBox application while leveraging existing infrastructure and maintaining consistency with the current architecture.