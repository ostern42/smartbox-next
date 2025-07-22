using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBoxNext.Services;
using Xunit;
using AutoFixture;
using FellowOakDicom;
using FellowOakDicom.Network;
using System.Drawing;

namespace SmartBoxNext.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for DICOM Service
/// Validates DICOM PS3.15 security profiles and medical imaging compliance
/// </summary>
public class DicomServiceTests
{
    private readonly Mock<ILogger<DicomService>> _mockLogger;
    private readonly Mock<IAuditLoggingService> _mockAuditService;
    private readonly Mock<IDICOMSecurityService> _mockSecurityService;
    private readonly DicomService _service;
    private readonly Fixture _autoFixture;

    public DicomServiceTests()
    {
        _mockLogger = new Mock<ILogger<DicomService>>();
        _mockAuditService = new Mock<IAuditLoggingService>();
        _mockSecurityService = new Mock<IDICOMSecurityService>();
        _autoFixture = new Fixture();
        
        _service = new DicomService(
            _mockLogger.Object, 
            _mockAuditService.Object,
            _mockSecurityService.Object);
    }

    #region DICOM File Creation Tests

    [Fact]
    public async Task CreateDicomFileAsync_ShouldGenerateValidSecondaryCapture()
    {
        // Arrange
        var imageData = GenerateTestImageData(1920, 1080);
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();

        // Act
        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DicomFile.Should().NotBeNull();
        
        var dataset = result.DicomFile.Dataset;
        dataset.GetString(DicomTag.SOPClassUID).Should().Be(DicomUID.SecondaryCaptureImageStorage.UID);
        dataset.GetString(DicomTag.PatientName).Should().Be(patientInfo.Name);
        dataset.GetString(DicomTag.PatientID).Should().Be(patientInfo.ID);
        dataset.GetString(DicomTag.StudyInstanceUID).Should().NotBeEmpty();
        dataset.GetString(DicomTag.SeriesInstanceUID).Should().NotBeEmpty();
        dataset.GetString(DicomTag.SOPInstanceUID).Should().NotBeEmpty();

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogDICOMEventAsync(
            "DICOM_FILE_CREATED", 
            It.IsAny<string>(),
            patientInfo.ID,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateDicomFileAsync_ShouldIncludeRequiredMedicalMetadata()
    {
        // Arrange
        var imageData = GenerateTestImageData(1024, 768);
        var patientInfo = new PatientInfo
        {
            ID = "PATIENT_001",
            Name = "Test^Patient",
            DateOfBirth = new DateTime(1980, 1, 1),
            Sex = "M"
        };
        var studyInfo = new StudyInfo
        {
            StudyDescription = "Endoscopic Procedure",
            StudyDate = DateTime.Now,
            Modality = "ES", // Endoscopy
            BodyPartExamined = "ABDOMEN"
        };

        // Act
        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);

        // Assert
        var dataset = result.DicomFile.Dataset;
        
        // Patient-level metadata
        dataset.GetString(DicomTag.PatientID).Should().Be("PATIENT_001");
        dataset.GetString(DicomTag.PatientName).Should().Be("Test^Patient");
        dataset.GetString(DicomTag.PatientBirthDate).Should().Be("19800101");
        dataset.GetString(DicomTag.PatientSex).Should().Be("M");
        
        // Study-level metadata
        dataset.GetString(DicomTag.StudyDescription).Should().Be("Endoscopic Procedure");
        dataset.GetString(DicomTag.Modality).Should().Be("ES");
        dataset.GetString(DicomTag.BodyPartExamined).Should().Be("ABDOMEN");
        
        // Image-level metadata
        dataset.GetUInt16(DicomTag.Rows).Should().Be(768);
        dataset.GetUInt16(DicomTag.Columns).Should().Be(1024);
        dataset.GetUInt16(DicomTag.BitsAllocated).Should().Be(8);
        dataset.GetUInt16(DicomTag.BitsStored).Should().Be(8);
        dataset.GetUInt16(DicomTag.HighBit).Should().Be(7);
        dataset.GetUInt16(DicomTag.PixelRepresentation).Should().Be(0);
    }

    [Fact]
    public async Task CreateDicomFileAsync_ShouldHandleHighResolutionImages()
    {
        // Arrange - 4K image
        var imageData = GenerateTestImageData(3840, 2160);
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();

        // Act
        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        var dataset = result.DicomFile.Dataset;
        dataset.GetUInt16(DicomTag.Rows).Should().Be(2160);
        dataset.GetUInt16(DicomTag.Columns).Should().Be(3840);
        
        // Verify file size is reasonable (compressed)
        var fileSize = result.FileSizeBytes;
        fileSize.Should().BeLessThan(50 * 1024 * 1024); // Less than 50MB for 4K image
    }

    #endregion

    #region DICOM Network Operations Tests

    [Fact]
    public async Task SendToPACSAsync_ShouldEstablishSecureConnection()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();
        var pacsConfig = new PACSConfiguration
        {
            ServerHost = "localhost",
            ServerPort = 11112,
            CallingAET = "SMARTBOX",
            CalledAET = "ORTHANC",
            UseSecureConnection = true
        };

        _mockSecurityService.Setup(x => x.EstablishSecureDICOMConnectionAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<DICOMSecurityProfileType>()))
            .ReturnsAsync(new SecureDICOMConnection { IsSecure = true, SecurityProfile = "TLS_AES" });

        // Act
        var result = await _service.SendToPACSAsync(dicomFile, pacsConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TransmissionSecure.Should().BeTrue();
        result.ResponseStatus.Should().Be(DicomStatus.Success);

        // Verify secure connection was established
        _mockSecurityService.Verify(x => x.EstablishSecureDICOMConnectionAsync(
            pacsConfig.ServerHost, 
            pacsConfig.ServerPort, 
            DICOMSecurityProfileType.TLS_AES), Times.Once);
    }

    [Fact]
    public async Task SendToPACSAsync_ShouldRetryOnTransientFailures()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();
        var pacsConfig = _autoFixture.Create<PACSConfiguration>();
        
        // Simulate transient network failure then success
        var callCount = 0;
        _service.SetNetworkSimulation(() =>
        {
            callCount++;
            if (callCount <= 2)
                throw new DicomNetworkException("Transient network error");
            return new DicomCStoreResponse(DicomStatus.Success);
        });

        // Act
        var result = await _service.SendToPACSAsync(dicomFile, pacsConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.RetryAttempts.Should().Be(2);
        result.FinalAttemptSuccessful.Should().BeTrue();
    }

    [Fact]
    public async Task SendToPACSAsync_ShouldFailAfterMaxRetries()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();
        var pacsConfig = _autoFixture.Create<PACSConfiguration>();
        
        // Simulate persistent network failure
        _service.SetNetworkSimulation(() => 
            throw new DicomNetworkException("Persistent network error"));

        // Act
        var result = await _service.SendToPACSAsync(dicomFile, pacsConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.RetryAttempts.Should().Be(3); // Max retries
        result.LastError.Should().Contain("Persistent network error");
    }

    #endregion

    #region DICOM Validation Tests

    [Fact]
    public async Task ValidateDicomConformanceAsync_ShouldVerifyRequiredTags()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();

        // Act
        var result = await _service.ValidateDicomConformanceAsync(dicomFile);

        // Assert
        result.Should().NotBeNull();
        result.IsConformant.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        
        // Check specific validation results
        result.HasRequiredPatientTags.Should().BeTrue();
        result.HasRequiredStudyTags.Should().BeTrue();
        result.HasRequiredSeriesTags.Should().BeTrue();
        result.HasRequiredImageTags.Should().BeTrue();
        result.PixelDataValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDicomConformanceAsync_ShouldDetectMissingRequiredTags()
    {
        // Arrange - Create DICOM file with missing required tags
        var dataset = new DicomDataset();
        dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
        // Missing Patient ID, Study Instance UID, etc.
        
        var dicomFile = new DicomFile(dataset);

        // Act
        var result = await _service.ValidateDicomConformanceAsync(dicomFile);

        // Assert
        result.Should().NotBeNull();
        result.IsConformant.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
        result.ValidationErrors.Should().Contain(e => e.Contains("PatientID"));
        result.ValidationErrors.Should().Contain(e => e.Contains("StudyInstanceUID"));
    }

    [Fact]
    public async Task ValidateDicomConformanceAsync_ShouldVerifyPixelDataIntegrity()
    {
        // Arrange
        var imageData = GenerateTestImageData(512, 512);
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();
        
        var dicomResult = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);
        
        // Corrupt pixel data
        var dataset = dicomResult.DicomFile.Dataset;
        var corruptedPixelData = new byte[100]; // Too small
        dataset.AddOrUpdate(DicomTag.PixelData, corruptedPixelData);

        // Act
        var result = await _service.ValidateDicomConformanceAsync(dicomResult.DicomFile);

        // Assert
        result.Should().NotBeNull();
        result.IsConformant.Should().BeFalse();
        result.PixelDataValid.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Contains("Pixel data"));
    }

    #endregion

    #region Security and Encryption Tests

    [Fact]
    public async Task CreateDicomFileAsync_ShouldApplySecurityAttributes()
    {
        // Arrange
        var imageData = GenerateTestImageData(1024, 768);
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();
        var securityConfig = new DICOMSecurityConfiguration
        {
            RequireDigitalSignature = true,
            EncryptionRequired = true,
            AuditTrailRequired = true
        };

        _mockSecurityService.Setup(x => x.ApplySecurityAttributesAsync(It.IsAny<DicomFile>()))
            .ReturnsAsync(new SecurityApplicationResult { Success = true, SignatureApplied = true });

        // Act
        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo, securityConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SecurityApplied.Should().BeTrue();
        
        // Verify security service was called
        _mockSecurityService.Verify(x => x.ApplySecurityAttributesAsync(
            It.IsAny<DicomFile>()), Times.Once);
    }

    [Fact]
    public async Task SendToPACSAsync_ShouldUseDigitalSignatures()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();
        var pacsConfig = new PACSConfiguration
        {
            RequireDigitalSignature = true,
            SigningCertificatePath = "TestData/Certificates/test-signing.p12"
        };

        _mockSecurityService.Setup(x => x.CreateDICOMDigitalSignatureAsync(
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new DICOMDigitalSignature 
            { 
                Signature = "digital_signature_data",
                Algorithm = "RSA-SHA256",
                IsValid = true 
            });

        // Act
        var result = await _service.SendToPACSAsync(dicomFile, pacsConfig);

        // Assert
        result.Should().NotBeNull();
        result.DigitalSignatureApplied.Should().BeTrue();
        
        // Verify digital signature was created
        _mockSecurityService.Verify(x => x.CreateDICOMDigitalSignatureAsync(
            It.IsAny<byte[]>(), 
            pacsConfig.SigningCertificatePath, 
            "Medical Image Transmission"), Times.Once);
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task CreateDicomFileAsync_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var imageData = GenerateTestImageData(1920, 1080);
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);

        // Assert
        var duration = DateTime.UtcNow - startTime;
        duration.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(5));
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentDicomCreation_ShouldHandleMultipleRequests()
    {
        // Arrange
        var tasks = new List<Task<DicomCreationResult>>();
        for (int i = 0; i < 10; i++)
        {
            var imageData = GenerateTestImageData(512, 512);
            var patientInfo = _autoFixture.Create<PatientInfo>();
            var studyInfo = _autoFixture.Create<StudyInfo>();
            
            tasks.Add(_service.CreateDicomFileAsync(imageData, patientInfo, studyInfo));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.Success);
        
        // Verify unique instance UIDs
        var instanceUIDs = results.Select(r => r.DicomFile.Dataset.GetString(DicomTag.SOPInstanceUID));
        instanceUIDs.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateDicomFileAsync_ShouldHandleInvalidImageData()
    {
        // Arrange
        byte[] invalidImageData = null;
        var patientInfo = _autoFixture.Create<PatientInfo>();
        var studyInfo = _autoFixture.Create<StudyInfo>();

        // Act & Assert
        var ex = await Record.ExceptionAsync(() => 
            _service.CreateDicomFileAsync(invalidImageData, patientInfo, studyInfo));
        
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateDicomFileAsync_ShouldHandleInvalidPatientData()
    {
        // Arrange
        var imageData = GenerateTestImageData(512, 512);
        PatientInfo invalidPatientInfo = null;
        var studyInfo = _autoFixture.Create<StudyInfo>();

        // Act & Assert
        var ex = await Record.ExceptionAsync(() => 
            _service.CreateDicomFileAsync(imageData, invalidPatientInfo, studyInfo));
        
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task SendToPACSAsync_ShouldHandleNetworkTimeouts()
    {
        // Arrange
        var dicomFile = await CreateTestDicomFile();
        var pacsConfig = _autoFixture.Create<PACSConfiguration>();
        
        // Simulate network timeout
        _service.SetNetworkSimulation(() => 
            throw new TimeoutException("Network timeout"));

        // Act
        var result = await _service.SendToPACSAsync(dicomFile, pacsConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.LastError.Should().Contain("timeout");
        result.ErrorType.Should().Be(DicomErrorType.NetworkTimeout);
    }

    #endregion

    #region Helper Methods

    private byte[] GenerateTestImageData(int width, int height)
    {
        // Generate test image data (RGB format)
        var imageData = new byte[width * height * 3];
        var random = new Random(42); // Deterministic for testing
        
        for (int i = 0; i < imageData.Length; i += 3)
        {
            imageData[i] = (byte)random.Next(256);     // R
            imageData[i + 1] = (byte)random.Next(256); // G
            imageData[i + 2] = (byte)random.Next(256); // B
        }
        
        return imageData;
    }

    private async Task<DicomFile> CreateTestDicomFile()
    {
        var imageData = GenerateTestImageData(512, 512);
        var patientInfo = new PatientInfo
        {
            ID = "TEST_PATIENT_001",
            Name = "Test^Patient",
            DateOfBirth = new DateTime(1980, 1, 1),
            Sex = "M"
        };
        var studyInfo = new StudyInfo
        {
            StudyDescription = "Test Study",
            StudyDate = DateTime.Now,
            Modality = "ES"
        };

        var result = await _service.CreateDicomFileAsync(imageData, patientInfo, studyInfo);
        return result.DicomFile;
    }

    #endregion
}

/// <summary>
/// Test data structures for DICOM testing
/// </summary>
public class PatientInfo
{
    public string ID { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public string Sex { get; set; } = "";
}

public class StudyInfo
{
    public string StudyDescription { get; set; } = "";
    public DateTime StudyDate { get; set; }
    public string Modality { get; set; } = "";
    public string BodyPartExamined { get; set; } = "";
}

public class PACSConfiguration
{
    public string ServerHost { get; set; } = "";
    public int ServerPort { get; set; }
    public string CallingAET { get; set; } = "";
    public string CalledAET { get; set; } = "";
    public bool UseSecureConnection { get; set; }
    public bool RequireDigitalSignature { get; set; }
    public string SigningCertificatePath { get; set; } = "";
}

public class DICOMSecurityConfiguration
{
    public bool RequireDigitalSignature { get; set; }
    public bool EncryptionRequired { get; set; }
    public bool AuditTrailRequired { get; set; }
}