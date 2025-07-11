# SmartBoxNext PACS Implementation Plan
Date: 2025-01-10

## Conversation Summary

### What We Did:
1. **Analyzed SmartBoxNext Current State**:
   - Reviewed CURRENT.md and discovered PACS export is only a stub (simulation)
   - Found the critical issue: `HandleExportCaptures` doesn't actually create DICOM files or send to PACS
   - Identified that photos are saved but not converted to DICOM format

2. **Created Comprehensive Implementation Plan**:
   - Developed 6-phase roadmap with atomic, testable steps
   - Prioritized features by importance (Quick Win → Critical → Nice to Have)
   - Written detailed code examples for each phase

### Current Status:
- **Working**: WebRTC 70 FPS video, photo capture, settings (after Session 26 refactoring)
- **NOT Working**: PACS export (only simulation), DICOM creation
- **File Locations**: 
  - Main project: `/mnt/c/Users/oliver.stern/source/repos/smartbox-next/smartbox-wpf-clean/`
  - Key file needing fix: `MainWindow.xaml.cs` (lines 2178-2242)

## Implementation Roadmap

### Phase 1: DICOM Creation (Quick Win)
**Goal**: Convert captured images to DICOM format
**Priority**: Critical
**Estimated Time**: 2-3 hours

#### Steps:
1. Install fo-dicom NuGet package
2. Create `DicomService.cs` class
3. Implement basic DICOM conversion
4. Test with sample image

#### Code Example:
```csharp
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;

public class DicomService
{
    public DicomFile CreateDicomFromImage(string imagePath, PatientInfo patient)
    {
        var file = new DicomFile();
        var dataset = file.Dataset;
        
        // Patient Module
        dataset.AddOrUpdate(DicomTag.PatientName, patient.Name);
        dataset.AddOrUpdate(DicomTag.PatientID, patient.Id);
        dataset.AddOrUpdate(DicomTag.PatientBirthDate, patient.BirthDate);
        dataset.AddOrUpdate(DicomTag.PatientSex, patient.Sex);
        
        // Study Module
        dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUID.Generate());
        dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now);
        dataset.AddOrUpdate(DicomTag.StudyTime, DateTime.Now);
        dataset.AddOrUpdate(DicomTag.StudyDescription, "SmartBox Capture");
        
        // Series Module
        dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate());
        dataset.AddOrUpdate(DicomTag.SeriesNumber, "1");
        dataset.AddOrUpdate(DicomTag.Modality, "XC"); // External Camera
        
        // Image Module
        dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
        dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate());
        
        // Add the image
        var image = new DicomImage(imagePath);
        dataset.AddOrUpdate(DicomTag.PixelData, image.PixelData);
        
        return file;
    }
}
```

### Phase 2: PACS Connection (Critical)
**Goal**: Establish connection to PACS server
**Priority**: Critical
**Estimated Time**: 3-4 hours

#### Steps:
1. Create `PacsService.cs` class
2. Implement DICOM C-STORE
3. Add connection testing
4. Handle network errors

#### Code Example:
```csharp
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

public class PacsService
{
    private readonly string _pacsHost;
    private readonly int _pacsPort;
    private readonly string _callingAe;
    private readonly string _calledAe;
    
    public async Task<bool> SendToPacs(DicomFile dicomFile)
    {
        var client = DicomClientFactory.Create(_pacsHost, _pacsPort, false, _callingAe, _calledAe);
        
        client.NegotiateAsyncOps();
        
        var request = new DicomCStoreRequest(dicomFile);
        
        request.OnResponseReceived += (req, response) =>
        {
            if (response.Status == DicomStatus.Success)
            {
                Console.WriteLine("DICOM file sent successfully");
            }
            else
            {
                Console.WriteLine($"Failed to send DICOM: {response.Status}");
            }
        };
        
        await client.AddRequestAsync(request);
        await client.SendAsync();
        
        return true;
    }
}
```

### Phase 3: Integration with MainWindow (Critical)
**Goal**: Replace stub implementation with real PACS export
**Priority**: Critical
**Estimated Time**: 2 hours

#### Steps:
1. Modify `HandleExportCaptures` method
2. Add error handling
3. Update UI feedback
4. Test end-to-end

#### Code Example:
```csharp
private async void HandleExportCaptures(object sender, RoutedEventArgs e)
{
    try
    {
        UpdateStatus("Preparing DICOM export...");
        
        var dicomService = new DicomService();
        var pacsService = new PacsService(
            Settings.PacsHost,
            Settings.PacsPort,
            Settings.CallingAE,
            Settings.CalledAE
        );
        
        var exportedCount = 0;
        
        foreach (var capture in _capturedImages)
        {
            // Create DICOM file
            var patientInfo = new PatientInfo
            {
                Name = Settings.PatientName,
                Id = Settings.PatientId,
                BirthDate = Settings.PatientBirthDate,
                Sex = Settings.PatientSex
            };
            
            var dicomFile = dicomService.CreateDicomFromImage(capture.FilePath, patientInfo);
            
            // Save DICOM locally
            var dicomPath = Path.ChangeExtension(capture.FilePath, ".dcm");
            await dicomFile.SaveAsync(dicomPath);
            
            // Send to PACS
            var success = await pacsService.SendToPacs(dicomFile);
            
            if (success)
            {
                exportedCount++;
                UpdateStatus($"Exported {exportedCount}/{_capturedImages.Count} images");
            }
        }
        
        UpdateStatus($"Export complete: {exportedCount} images sent to PACS");
    }
    catch (Exception ex)
    {
        UpdateStatus($"Export failed: {ex.Message}");
        MessageBox.Show($"Failed to export to PACS: {ex.Message}", "Export Error", 
            MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

### Phase 4: Settings UI Enhancement (Important)
**Goal**: Add PACS configuration to settings
**Priority**: Important
**Estimated Time**: 2 hours

#### Steps:
1. Add PACS settings to SettingsWindow.xaml
2. Implement connection test button
3. Save/load PACS configuration
4. Validate inputs

### Phase 5: Advanced Features (Nice to Have)
**Goal**: Add professional features
**Priority**: Nice to Have
**Estimated Time**: 4-6 hours

#### Features:
- DICOM Worklist integration
- Multiple PACS destinations
- Export queue with retry
- Compression options
- DICOM tag editor

### Phase 6: Testing & Documentation (Critical)
**Goal**: Ensure reliability
**Priority**: Critical
**Estimated Time**: 2-3 hours

#### Steps:
1. Unit tests for DicomService
2. Integration tests for PacsService
3. End-to-end testing
4. User documentation
5. Troubleshooting guide

## Priority Order

1. **Phase 1**: DICOM Creation (Quick Win - 2-3 hours)
2. **Phase 2**: PACS Connection (Critical - 3-4 hours)
3. **Phase 3**: Integration (Critical - 2 hours)
4. **Phase 6**: Testing (Critical - 2-3 hours)
5. **Phase 4**: Settings UI (Important - 2 hours)
6. **Phase 5**: Advanced Features (Nice to Have - 4-6 hours)

## Total Estimated Time
- **Minimum (Critical only)**: 9-12 hours
- **Full Implementation**: 15-20 hours

## Next Steps
1. Start with Phase 1 - Install fo-dicom and create DicomService
2. Test DICOM creation with a sample image
3. Move to Phase 2 - Implement PACS connection
4. Replace stub in MainWindow.xaml.cs

## Key Files to Modify
- `/MainWindow.xaml.cs` (lines 2178-2242)
- Create: `/Services/DicomService.cs`
- Create: `/Services/PacsService.cs`
- Create: `/Models/PatientInfo.cs`
- Update: `/SettingsWindow.xaml` and `.xaml.cs`

## Dependencies
- fo-dicom NuGet package (latest stable version)
- .NET Framework 4.8 or .NET 6+ (check current target)

## Testing Checklist
- [ ] Can create valid DICOM files
- [ ] Can connect to PACS server
- [ ] Can send DICOM files to PACS
- [ ] Error handling works correctly
- [ ] UI provides clear feedback
- [ ] Settings are saved/loaded correctly
- [ ] Works with different image formats
- [ ] Handles network interruptions gracefully