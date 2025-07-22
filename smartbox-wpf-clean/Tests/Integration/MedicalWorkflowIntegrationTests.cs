using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;
using Xunit;
using System.Drawing;
using FellowOakDicom;

namespace SmartBoxNext.Tests.Integration;

/// <summary>
/// Integration tests for complete medical workflows
/// Tests end-to-end scenarios including capture → DICOM → PACS → audit
/// </summary>
[Collection("MedicalWorkflowCollection")]
public class MedicalWorkflowIntegrationTests : IClassFixture<MedicalWorkflowTestFixture>
{
    private readonly MedicalWorkflowTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalWorkflowIntegrationTests> _logger;

    public MedicalWorkflowIntegrationTests(MedicalWorkflowTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<MedicalWorkflowIntegrationTests>>();
    }

    #region Complete Medical Image Capture Workflow

    [Fact]
    public async Task CompleteImageCaptureWorkflow_ShouldProcessFromCaptureToAudit()
    {
        // Arrange
        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();
        var queueManager = _serviceProvider.GetRequiredService<IntegratedQueueManager>();
        var auditService = _serviceProvider.GetRequiredService<AuditLoggingService>();

        var testPatient = new MedicalPatient
        {
            PatientID = "INT_TEST_001",
            PatientName = "Integration^Test^Patient",
            DateOfBirth = new DateTime(1985, 6, 15),
            Gender = "F",
            MedicalRecordNumber = "MRN123456"
        };

        var testProcedure = new MedicalProcedure
        {
            ProcedureID = "PROC_001",
            ProcedureDescription = "Endoscopic Examination",
            StudyInstanceUID = DicomUID.Generate().UID,
            Modality = "ES",
            BodyPartExamined = "STOMACH",
            PerformingPhysician = "Dr. Smith"
        };

        // Act - Complete workflow
        _logger.LogInformation("Starting complete medical image capture workflow test");

        // Step 1: Simulate image capture
        var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
        captureResult.Success.Should().BeTrue();

        // Step 2: Convert to DICOM
        var dicomResult = await dicomConverter.ConvertToDicomAsync(
            captureResult.ImageData, testPatient, testProcedure);
        dicomResult.Success.Should().BeTrue();

        // Step 3: Queue for PACS transmission
        var queueResult = await queueManager.QueueForTransmissionAsync(
            dicomResult.DicomFile, testPatient, testProcedure);
        queueResult.Success.Should().BeTrue();

        // Step 4: Process queue (send to PACS)
        var transmissionResult = await queueManager.ProcessQueueAsync();
        transmissionResult.ProcessedCount.Should().BeGreaterThan(0);
        transmissionResult.SuccessfulTransmissions.Should().BeGreaterThan(0);

        // Step 5: Verify audit trail
        var auditRecords = await auditService.GetAuditRecordsAsync(
            DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, testPatient.PatientID);
        
        // Assert - Verify complete workflow
        auditRecords.Should().NotBeEmpty();
        auditRecords.Should().Contain(r => r.EventType == "IMAGE_CAPTURED");
        auditRecords.Should().Contain(r => r.EventType == "DICOM_CREATED");
        auditRecords.Should().Contain(r => r.EventType == "QUEUED_FOR_TRANSMISSION");
        auditRecords.Should().Contain(r => r.EventType == "TRANSMITTED_TO_PACS");

        _logger.LogInformation("Complete medical workflow test completed successfully");
    }

    [Fact]
    public async Task MultiModalityWorkflow_ShouldHandleDifferentImageSources()
    {
        // Arrange
        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();

        var testPatient = new MedicalPatient
        {
            PatientID = "MULTIMODAL_001",
            PatientName = "Multimodal^Test^Patient"
        };

        var procedures = new[]
        {
            new MedicalProcedure { Modality = "ES", ProcedureDescription = "Endoscopy" },
            new MedicalProcedure { Modality = "US", ProcedureDescription = "Ultrasound" },
            new MedicalProcedure { Modality = "CR", ProcedureDescription = "Computed Radiography" }
        };

        var captureSources = new[] { CaptureSource.Yuan, CaptureSource.WebRTC, CaptureSource.File };

        // Act - Test multiple modalities
        var results = new List<WorkflowResult>();

        for (int i = 0; i < procedures.Length; i++)
        {
            var captureResult = await unifiedCaptureManager.CaptureImageAsync(captureSources[i]);
            var dicomResult = await dicomConverter.ConvertToDicomAsync(
                captureResult.ImageData, testPatient, procedures[i]);
            
            results.Add(new WorkflowResult 
            { 
                CaptureSuccess = captureResult.Success,
                DicomSuccess = dicomResult.Success,
                Modality = procedures[i].Modality
            });
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.CaptureSuccess && r.DicomSuccess);
        results.Select(r => r.Modality).Should().BeEquivalentTo(new[] { "ES", "US", "CR" });
    }

    #endregion

    #region DICOM-PACS Integration Tests

    [Fact]
    public async Task DicomToPacsIntegration_ShouldTransmitSecurely()
    {
        // Arrange
        var dicomService = _serviceProvider.GetRequiredService<DicomService>();
        var pacsService = _serviceProvider.GetRequiredService<PacsService>();
        var securityService = _serviceProvider.GetRequiredService<DICOMSecurityService>();

        var testImage = GenerateTestMedicalImage(1024, 768);
        var testPatient = CreateTestPatient("PACS_INT_001");
        var testStudy = CreateTestStudy("Secure PACS Integration Test");

        // Act
        // Create DICOM file
        var dicomResult = await dicomService.CreateDicomFileAsync(testImage, testPatient, testStudy);
        dicomResult.Success.Should().BeTrue();

        // Apply security (encryption, digital signature)
        var securityResult = await securityService.ApplySecurityAttributesAsync(dicomResult.DicomFile);
        securityResult.Success.Should().BeTrue();

        // Transmit to PACS
        var transmissionResult = await pacsService.SendToPACSAsync(dicomResult.DicomFile);

        // Assert
        transmissionResult.Success.Should().BeTrue();
        transmissionResult.TransmissionSecure.Should().BeTrue();
        transmissionResult.DigitalSignatureVerified.Should().BeTrue();
        transmissionResult.ResponseStatus.Should().Be(DicomStatus.Success);
    }

    [Fact]
    public async Task DicomQueryRetrieve_ShouldWorkWithPACS()
    {
        // Arrange
        var pacsService = _serviceProvider.GetRequiredService<PacsService>();
        var testPatientId = "QR_TEST_001";

        // First, store a test study
        await StoreTestStudyInPACS(testPatientId);

        // Act - Query for patient studies
        var queryResult = await pacsService.QueryPatientStudiesAsync(testPatientId);

        // Assert
        queryResult.Success.Should().BeTrue();
        queryResult.Studies.Should().NotBeEmpty();
        queryResult.Studies.Should().Contain(s => s.PatientID == testPatientId);

        // Act - Retrieve specific study
        var studyUID = queryResult.Studies.First().StudyInstanceUID;
        var retrieveResult = await pacsService.RetrieveStudyAsync(studyUID);

        // Assert
        retrieveResult.Success.Should().BeTrue();
        retrieveResult.DicomFiles.Should().NotBeEmpty();
    }

    #endregion

    #region HL7 Integration Tests

    [Fact]
    public async Task HL7WorklistIntegration_ShouldSynchronizePatientData()
    {
        // Arrange
        var hl7Service = _serviceProvider.GetRequiredService<HL7IntegrationService>();
        var worklistService = _serviceProvider.GetRequiredService<MwlService>();

        var hl7Message = CreateTestHL7ADTMessage("HL7_INT_001", "Admission");

        // Act
        // Process HL7 message
        var hl7Result = await hl7Service.ProcessHL7MessageAsync(hl7Message);
        hl7Result.Success.Should().BeTrue();

        // Check worklist synchronization
        var worklistItems = await worklistService.GetWorklistItemsAsync();
        
        // Assert
        worklistItems.Should().Contain(item => item.PatientID == "HL7_INT_001");
        
        var syncedItem = worklistItems.First(item => item.PatientID == "HL7_INT_001");
        syncedItem.PatientName.Should().NotBeEmpty();
        syncedItem.ScheduledProcedureStepStartDate.Should().BeCloseTo(DateTime.Now, TimeSpan.FromDays(1));
    }

    [Fact]
    public async Task HL7ToAuditIntegration_ShouldTrackPatientEvents()
    {
        // Arrange
        var hl7Service = _serviceProvider.GetRequiredService<HL7IntegrationService>();
        var auditService = _serviceProvider.GetRequiredService<AuditLoggingService>();

        var admissionMessage = CreateTestHL7ADTMessage("AUDIT_HL7_001", "Admission");
        var dischargeMessage = CreateTestHL7ADTMessage("AUDIT_HL7_001", "Discharge");

        // Act
        await hl7Service.ProcessHL7MessageAsync(admissionMessage);
        await hl7Service.ProcessHL7MessageAsync(dischargeMessage);

        // Allow time for async audit processing
        await Task.Delay(1000);

        // Retrieve audit records
        var auditRecords = await auditService.GetAuditRecordsAsync(
            DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow, "AUDIT_HL7_001");

        // Assert
        auditRecords.Should().NotBeEmpty();
        auditRecords.Should().Contain(r => r.EventType == "PATIENT_ADMISSION");
        auditRecords.Should().Contain(r => r.EventType == "PATIENT_DISCHARGE");
    }

    #endregion

    #region Performance and Reliability Tests

    [Fact]
    public async Task EndToEndWorkflow_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var testPatient = CreateTestPatient("PERF_INT_001");
        var testStudy = CreateTestStudy("Performance Integration Test");

        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();
        var queueManager = _serviceProvider.GetRequiredService<IntegratedQueueManager>();

        // Act - Complete workflow with timing
        var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
        var dicomResult = await dicomConverter.ConvertToDicomAsync(
            captureResult.ImageData, testPatient, testStudy);
        var queueResult = await queueManager.QueueForTransmissionAsync(
            dicomResult.DicomFile, testPatient, testStudy);

        var totalTime = DateTime.UtcNow - startTime;

        // Assert
        totalTime.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(10)); // Complete workflow under 10 seconds
        captureResult.Success.Should().BeTrue();
        dicomResult.Success.Should().BeTrue();
        queueResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentWorkflows_ShouldHandleMultiplePatients()
    {
        // Arrange
        var numberOfConcurrentWorkflows = 5;
        var tasks = new List<Task<bool>>();

        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();

        // Act - Start multiple concurrent workflows
        for (int i = 0; i < numberOfConcurrentWorkflows; i++)
        {
            var patientId = $"CONCURRENT_{i:D3}";
            var task = ProcessSingleWorkflowAsync(unifiedCaptureManager, dicomConverter, patientId);
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(numberOfConcurrentWorkflows);
        results.Should().OnlyContain(success => success);
    }

    #endregion

    #region Error Recovery and Resilience Tests

    [Fact]
    public async Task WorkflowWithPACSFailure_ShouldRetryAndRecover()
    {
        // Arrange
        var queueManager = _serviceProvider.GetRequiredService<IntegratedQueueManager>();
        var testPatient = CreateTestPatient("RETRY_TEST_001");
        var testStudy = CreateTestStudy("Retry Integration Test");

        // Create test DICOM file
        var testImage = GenerateTestMedicalImage(512, 512);
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();
        var dicomResult = await dicomConverter.ConvertToDicomAsync(testImage, testPatient, testStudy);

        // Simulate PACS failure then recovery
        _fixture.SimulatePACSFailure = true;

        // Act - Queue for transmission (will fail initially)
        var queueResult = await queueManager.QueueForTransmissionAsync(
            dicomResult.DicomFile, testPatient, testStudy);
        queueResult.Success.Should().BeTrue(); // Queuing should succeed

        var initialProcessResult = await queueManager.ProcessQueueAsync();
        initialProcessResult.FailedTransmissions.Should().BeGreaterThan(0);

        // Restore PACS and retry
        _fixture.SimulatePACSFailure = false;
        await Task.Delay(2000); // Wait for retry interval

        var retryProcessResult = await queueManager.ProcessQueueAsync();

        // Assert
        retryProcessResult.SuccessfulTransmissions.Should().BeGreaterThan(0);
        retryProcessResult.FailedTransmissions.Should().Be(0);
    }

    [Fact]
    public async Task WorkflowWithNetworkInterruption_ShouldResume()
    {
        // Arrange
        var pacsService = _serviceProvider.GetRequiredService<PacsService>();
        var testImage = GenerateTestMedicalImage(1024, 768);
        var testPatient = CreateTestPatient("NETWORK_INT_001");
        var testStudy = CreateTestStudy("Network Interruption Test");

        var dicomService = _serviceProvider.GetRequiredService<DicomService>();
        var dicomResult = await dicomService.CreateDicomFileAsync(testImage, testPatient, testStudy);

        // Simulate network interruption
        _fixture.SimulateNetworkInterruption = true;

        // Act - Attempt transmission (should fail)
        var failedResult = await pacsService.SendToPACSAsync(dicomResult.DicomFile);
        failedResult.Success.Should().BeFalse();

        // Restore network
        _fixture.SimulateNetworkInterruption = false;

        // Retry transmission
        var successResult = await pacsService.SendToPACSAsync(dicomResult.DicomFile);

        // Assert
        successResult.Success.Should().BeTrue();
        successResult.RetryAttempts.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private async Task<bool> ProcessSingleWorkflowAsync(
        UnifiedCaptureManager captureManager, 
        OptimizedDicomConverter dicomConverter, 
        string patientId)
    {
        try
        {
            var testPatient = CreateTestPatient(patientId);
            var testStudy = CreateTestStudy($"Concurrent Test {patientId}");

            var captureResult = await captureManager.CaptureImageAsync(CaptureSource.Yuan);
            if (!captureResult.Success) return false;

            var dicomResult = await dicomConverter.ConvertToDicomAsync(
                captureResult.ImageData, testPatient, testStudy);
            
            return dicomResult.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task StoreTestStudyInPACS(string patientId)
    {
        var dicomService = _serviceProvider.GetRequiredService<DicomService>();
        var pacsService = _serviceProvider.GetRequiredService<PacsService>();

        var testImage = GenerateTestMedicalImage(512, 512);
        var testPatient = CreateTestPatient(patientId);
        var testStudy = CreateTestStudy("Test Study for Query/Retrieve");

        var dicomResult = await dicomService.CreateDicomFileAsync(testImage, testPatient, testStudy);
        await pacsService.SendToPACSAsync(dicomResult.DicomFile);
    }

    private byte[] GenerateTestMedicalImage(int width, int height)
    {
        var imageData = new byte[width * height * 3];
        var random = new Random();
        random.NextBytes(imageData);
        return imageData;
    }

    private MedicalPatient CreateTestPatient(string patientId)
    {
        return new MedicalPatient
        {
            PatientID = patientId,
            PatientName = $"Test^Patient^{patientId}",
            DateOfBirth = new DateTime(1980, 1, 1),
            Gender = "M",
            MedicalRecordNumber = $"MRN_{patientId}"
        };
    }

    private MedicalProcedure CreateTestStudy(string description)
    {
        return new MedicalProcedure
        {
            StudyInstanceUID = DicomUID.Generate().UID,
            ProcedureDescription = description,
            Modality = "ES",
            BodyPartExamined = "ABDOMEN",
            PerformingPhysician = "Dr. Test"
        };
    }

    private string CreateTestHL7ADTMessage(string patientId, string eventType)
    {
        var now = DateTime.Now;
        return $@"MSH|^~\&|SmartBox|CIRSS|HIS|HOSPITAL|{now:yyyyMMddHHmmss}||ADT^A01|MSG{patientId}|P|2.5
EVN||{now:yyyyMMddHHmmss}|||Dr.Test
PID|1||{patientId}||Test^Patient^{patientId}||{now.AddYears(-40):yyyyMMdd}|M|||123 Test St^^TestCity^ST^12345
PV1|1|I|ICU^101^1|||||||SUR||||V";
    }

    #endregion
}

/// <summary>
/// Test fixture for medical workflow integration tests
/// Provides shared services and test infrastructure
/// </summary>
public class MedicalWorkflowTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    public bool SimulatePACSFailure { get; set; }
    public bool SimulateNetworkInterruption { get; set; }

    public MedicalWorkflowTestFixture()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add test configuration
        services.AddSingleton(TestConfiguration.Configuration);

        // Add medical services
        services.AddScoped<UnifiedCaptureManager>();
        services.AddScoped<OptimizedDicomConverter>();
        services.AddScoped<IntegratedQueueManager>();
        services.AddScoped<DicomService>();
        services.AddScoped<PacsService>();
        services.AddScoped<HL7IntegrationService>();
        services.AddScoped<MwlService>();
        services.AddScoped<AuditLoggingService>();
        services.AddScoped<DICOMSecurityService>();

        // Add test-specific mock services
        services.AddScoped<ITestDICOMServer, MockDICOMServer>();
        services.AddScoped<ITestHL7Server, MockHL7Server>();
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Collection definition for medical workflow tests
/// </summary>
[CollectionDefinition("MedicalWorkflowCollection")]
public class MedicalWorkflowCollection : ICollectionFixture<MedicalWorkflowTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Supporting data structures for integration tests
/// </summary>
public class MedicalPatient
{
    public string PatientID { get; set; } = "";
    public string PatientName { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string MedicalRecordNumber { get; set; } = "";
}

public class MedicalProcedure
{
    public string ProcedureID { get; set; } = "";
    public string ProcedureDescription { get; set; } = "";
    public string StudyInstanceUID { get; set; } = "";
    public string Modality { get; set; } = "";
    public string BodyPartExamined { get; set; } = "";
    public string PerformingPhysician { get; set; } = "";
}

public class WorkflowResult
{
    public bool CaptureSuccess { get; set; }
    public bool DicomSuccess { get; set; }
    public string Modality { get; set; } = "";
}

public enum CaptureSource
{
    Yuan,
    WebRTC,
    File
}