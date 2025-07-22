using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBoxNext.Services;
using Xunit;
using AutoFixture;
using System.Security.Cryptography;
using System.Text;

namespace SmartBoxNext.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for HIPAA Privacy Service
/// Validates HIPAA Privacy Rule (45 CFR 164.502-534) and Security Rule (45 CFR 164.306-318) compliance
/// </summary>
public class HIPAAPrivacyServiceTests
{
    private readonly Mock<ILogger<HIPAAPrivacyService>> _mockLogger;
    private readonly Mock<IAuditLoggingService> _mockAuditService;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly HIPAAPrivacyService _service;
    private readonly Fixture _autoFixture;

    public HIPAAPrivacyServiceTests()
    {
        _mockLogger = new Mock<ILogger<HIPAAPrivacyService>>();
        _mockAuditService = new Mock<IAuditLoggingService>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        _autoFixture = new Fixture();
        
        _service = new HIPAAPrivacyService(
            _mockLogger.Object, 
            _mockAuditService.Object,
            _mockEncryptionService.Object);
    }

    #region PHI Encryption Tests (Technical Safeguards)

    [Fact]
    public async Task EncryptPHIAsync_ShouldUseAES256Encryption()
    {
        // Arrange
        var plainText = "Patient John Doe, DOB: 1980-01-01, SSN: 123-45-6789";
        var patientId = "PATIENT_001";
        var expectedEncryptedData = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted_data"));

        _mockEncryptionService.Setup(x => x.EncryptAES256Async(It.IsAny<string>(), It.IsAny<byte[]>()))
            .ReturnsAsync(new EncryptionResult 
            { 
                EncryptedData = expectedEncryptedData,
                Algorithm = "AES-256-CBC",
                KeyLength = 256,
                Success = true
            });

        // Act
        var result = await _service.EncryptPHIAsync(plainText, patientId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Algorithm.Should().Be("AES-256-CBC");
        result.KeyLength.Should().Be(256);
        result.EncryptedData.Should().NotBeEmpty();
        result.EncryptedData.Should().NotBe(plainText);

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogPrivacyEventAsync(
            "PHI_ENCRYPTION", 
            It.IsAny<string>(),
            patientId,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task DecryptPHIAsync_ShouldRestoreOriginalData()
    {
        // Arrange
        var originalText = "Patient Jane Smith, DOB: 1990-05-15";
        var patientId = "PATIENT_002";
        var encryptedData = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted_jane_data"));

        _mockEncryptionService.Setup(x => x.DecryptAES256Async(encryptedData, It.IsAny<byte[]>()))
            .ReturnsAsync(new DecryptionResult 
            { 
                DecryptedData = originalText,
                Success = true
            });

        // Act
        var result = await _service.DecryptPHIAsync(encryptedData, patientId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DecryptedData.Should().Be(originalText);

        // Verify audit logging for PHI access
        _mockAuditService.Verify(x => x.LogPrivacyEventAsync(
            "PHI_DECRYPTION", 
            It.IsAny<string>(),
            patientId,
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task EncryptPHIAsync_ShouldUseUniqueKeysPerPatient()
    {
        // Arrange
        var plainText = "Test PHI data";
        var patientId1 = "PATIENT_001";
        var patientId2 = "PATIENT_002";

        // Act
        var result1 = await _service.EncryptPHIAsync(plainText, patientId1);
        var result2 = await _service.EncryptPHIAsync(plainText, patientId2);

        // Assert
        result1.EncryptedData.Should().NotBe(result2.EncryptedData);
        result1.KeyId.Should().NotBe(result2.KeyId);
    }

    #endregion

    #region Access Control Tests (Administrative Safeguards)

    [Theory]
    [InlineData("physician", "PATIENT_001", "Treatment", true)]
    [InlineData("nurse", "PATIENT_001", "Treatment", true)]
    [InlineData("technician", "PATIENT_001", "Treatment", false)]
    [InlineData("physician", "PATIENT_001", "Payment", true)]
    [InlineData("nurse", "PATIENT_001", "Payment", false)]
    [InlineData("admin", "PATIENT_001", "HealthcareOperations", true)]
    public async Task ValidateAccessRightsAsync_ShouldEnforceRoleBasedAccess(
        string userRole, string patientId, string purpose, bool expectedAccess)
    {
        // Arrange
        var userId = $"user_{userRole}";

        // Act
        var hasAccess = await _service.ValidateAccessRightsAsync(userId, patientId, purpose);

        // Assert
        hasAccess.Should().Be(expectedAccess);

        // Verify access attempt is logged
        _mockAuditService.Verify(x => x.LogPrivacyEventAsync(
            "PHI_ACCESS_VALIDATION", 
            It.IsAny<string>(),
            patientId,
            userId), Times.Once);
    }

    [Fact]
    public async Task ValidateAccessRightsAsync_ShouldEnforceMinimumNecessaryPrinciple()
    {
        // Arrange
        var nurseUserId = "nurse_001";
        var patientId = "PATIENT_001";

        // Act - Nurse should only access treatment-related data
        var treatmentAccess = await _service.ValidateAccessRightsAsync(nurseUserId, patientId, "Treatment");
        var billingAccess = await _service.ValidateAccessRightsAsync(nurseUserId, patientId, "Payment");

        // Assert
        treatmentAccess.Should().BeTrue();
        billingAccess.Should().BeFalse(); // Minimum necessary principle
    }

    [Fact]
    public async Task ValidateAccessRightsAsync_ShouldEnforceTimeBasedRestrictions()
    {
        // Arrange
        var nurseUserId = "nurse_002";
        var patientId = "PATIENT_002";
        
        // Simulate after-hours access attempt
        var currentTime = new DateTime(2024, 1, 1, 2, 0, 0); // 2 AM
        _service.SetCurrentTimeForTesting(currentTime);

        // Act
        var hasAccess = await _service.ValidateAccessRightsAsync(nurseUserId, patientId, "Treatment");

        // Assert
        hasAccess.Should().BeFalse(); // No after-hours access for non-emergency
    }

    #endregion

    #region Emergency Access (Break-Glass) Tests

    [Fact]
    public async Task RequestEmergencyAccessAsync_ShouldGrantAccessForLifeThreateningEmergency()
    {
        // Arrange
        var accessorId = "emergency_physician_001";
        var patientId = "PATIENT_EMERGENCY";
        var emergencyReason = "Patient cardiac arrest - immediate access required";
        var emergencyLevel = EmergencyLevel.LifeThreatening;

        // Act
        var result = await _service.RequestEmergencyAccessAsync(
            accessorId, patientId, emergencyReason, emergencyLevel);

        // Assert
        result.Should().NotBeNull();
        result.AccessGranted.Should().BeTrue();
        result.AccessLevel.Should().Be(EmergencyAccessLevel.Full);
        result.AutoApproved.Should().BeTrue(); // Life-threatening = auto-approval
        result.ExpirationTime.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));

        // Verify emergency access is audited
        _mockAuditService.Verify(x => x.LogEmergencyAccessEventAsync(
            "EMERGENCY_ACCESS_GRANTED", 
            It.IsAny<string>(),
            patientId,
            accessorId), Times.Once);
    }

    [Fact]
    public async Task RequestEmergencyAccessAsync_ShouldRequireApprovalForNonLifeThreatening()
    {
        // Arrange
        var accessorId = "physician_002";
        var patientId = "PATIENT_002";
        var emergencyReason = "Urgent care needed during off-hours";
        var emergencyLevel = EmergencyLevel.Urgent;

        // Act
        var result = await _service.RequestEmergencyAccessAsync(
            accessorId, patientId, emergencyReason, emergencyLevel);

        // Assert
        result.Should().NotBeNull();
        result.AccessGranted.Should().BeFalse(); // Requires manual approval
        result.AutoApproved.Should().BeFalse();
        result.RequiresApproval.Should().BeTrue();
        result.ApprovalRequestId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EmergencyAccess_ShouldExpireAutomatically()
    {
        // Arrange
        var accessorId = "emergency_physician_002";
        var patientId = "PATIENT_003";
        var emergencyLevel = EmergencyLevel.LifeThreatening;

        // Grant emergency access
        var accessResult = await _service.RequestEmergencyAccessAsync(
            accessorId, patientId, "Emergency", emergencyLevel);

        // Simulate time passage
        _service.SetCurrentTimeForTesting(DateTime.UtcNow.AddHours(25));

        // Act - Try to access after expiration
        var hasAccess = await _service.ValidateAccessRightsAsync(accessorId, patientId, "Emergency");

        // Assert
        hasAccess.Should().BeFalse(); // Access should have expired
    }

    #endregion

    #region Data De-identification Tests

    [Fact]
    public async Task DeIdentifyPHIAsync_ShouldRemoveDirectIdentifiers()
    {
        // Arrange
        var phi = new PHIData
        {
            PatientName = "John Doe",
            SSN = "123-45-6789",
            DateOfBirth = new DateTime(1980, 1, 1),
            Address = "123 Main St, Anytown, NY 12345",
            PhoneNumber = "555-123-4567",
            MedicalRecordNumber = "MRN12345",
            AccountNumber = "ACC67890"
        };

        // Act
        var result = await _service.DeIdentifyPHIAsync(phi, DeIdentificationMethod.SafeHarbor);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Method.Should().Be(DeIdentificationMethod.SafeHarbor);
        
        // Verify direct identifiers are removed
        result.DeIdentifiedData.Should().NotContain("John Doe");
        result.DeIdentifiedData.Should().NotContain("123-45-6789");
        result.DeIdentifiedData.Should().NotContain("123 Main St");
        result.DeIdentifiedData.Should().NotContain("555-123-4567");
        
        // Verify clinical data is preserved (if non-identifying)
        result.RetainedClinicalData.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DeIdentifyPHIAsync_ShouldApplyStatisticalDisclosureControl()
    {
        // Arrange
        var phi = new PHIData
        {
            Age = 45,
            ZipCode = "12345",
            AdmissionDate = new DateTime(2024, 1, 15)
        };

        // Act
        var result = await _service.DeIdentifyPHIAsync(phi, DeIdentificationMethod.StatisticalDisclosure);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        
        // Age should be generalized to range
        result.GeneralizedAge.Should().Be("40-50");
        
        // Zip code should be truncated
        result.GeneralizedZipCode.Should().Be("123**");
        
        // Date should be shifted
        result.ShiftedDate.Should().NotBe(phi.AdmissionDate);
    }

    #endregion

    #region GDPR Compliance Tests

    [Fact]
    public async Task ValidateGDPRComplianceAsync_ShouldVerifyDataSubjectRights()
    {
        // Act
        var result = await _service.ValidateGDPRComplianceAsync();

        // Assert
        result.Should().NotBeNull();
        result.RightOfAccessImplemented.Should().BeTrue();
        result.RightToRectificationImplemented.Should().BeTrue();
        result.RightToErasureImplemented.Should().BeTrue();
        result.RightToRestrictProcessingImplemented.Should().BeTrue();
        result.RightToDataPortabilityImplemented.Should().BeTrue();
        result.RightToObjectImplemented.Should().BeTrue();
        result.OverallGDPRCompliance.Should().BeGreaterThan(95);
    }

    [Fact]
    public async Task ProcessDataSubjectRequest_ShouldHandleRightOfAccess()
    {
        // Arrange
        var request = new DataSubjectRequest
        {
            RequestType = DataSubjectRequestType.Access,
            PatientId = "PATIENT_001",
            RequestorEmail = "patient@example.com"
        };

        // Act
        var result = await _service.ProcessDataSubjectRequestAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RequestProcessed.Should().BeTrue();
        result.ResponseGenerated.Should().BeTrue();
        result.ComplianceDeadlineMet.Should().BeTrue();
        result.DataExportGenerated.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessDataSubjectRequest_ShouldHandleRightToErasure()
    {
        // Arrange
        var request = new DataSubjectRequest
        {
            RequestType = DataSubjectRequestType.Erasure,
            PatientId = "PATIENT_DELETE",
            RequestorEmail = "patient@example.com",
            ErasureReason = "Withdrawal of consent"
        };

        // Act
        var result = await _service.ProcessDataSubjectRequestAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.RequestProcessed.Should().BeTrue();
        result.DataErased.Should().BeTrue();
        result.ErasureConfirmed.Should().BeTrue();
        result.LegalHoldChecked.Should().BeTrue();
    }

    #endregion

    #region Audit and Monitoring Tests

    [Fact]
    public async Task AllPHIOperations_ShouldGenerateComprehensiveAuditTrail()
    {
        // Arrange
        var patientId = "AUDIT_TEST_PATIENT";
        var userId = "physician_audit_test";

        // Act - Perform various PHI operations
        await _service.ValidateAccessRightsAsync(userId, patientId, "Treatment");
        await _service.EncryptPHIAsync("Test PHI data", patientId);
        await _service.LogPrivacyEventAsync("PHI_ACCESS", "Test access", patientId);

        // Assert - Verify audit logging for each operation
        _mockAuditService.Verify(x => x.LogPrivacyEventAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(),
            patientId,
            It.IsAny<string>()), Times.AtLeast(3));
    }

    [Fact]
    public async Task AuditTrail_ShouldBeImmutableAndTamperEvident()
    {
        // Arrange
        var auditData = "PHI access by physician_001 for patient PATIENT_001";

        // Act
        var auditId = await _service.LogPrivacyEventAsync("PHI_ACCESS", auditData, "PATIENT_001");

        // Assert
        var auditRecord = await _service.GetAuditRecordAsync(auditId);
        auditRecord.Should().NotBeNull();
        auditRecord.IsTamperEvident.Should().BeTrue();
        auditRecord.DigitalSignature.Should().NotBeEmpty();
        auditRecord.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public async Task EncryptPHIAsync_ShouldCompleteWithinPerformanceThreshold()
    {
        // Arrange
        var largePhiData = new string('x', 10000); // 10KB of PHI data
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _service.EncryptPHIAsync(largePhiData, "PERF_TEST_PATIENT");

        // Assert
        var duration = DateTime.UtcNow - startTime;
        duration.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(2));
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrentAccessValidation_ShouldHandleMultipleRequests()
    {
        // Arrange
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 100; i++)
        {
            var patientId = $"CONCURRENT_PATIENT_{i}";
            tasks.Add(_service.ValidateAccessRightsAsync("physician_001", patientId, "Treatment"));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().OnlyContain(r => r == true);
    }

    #endregion

    #region Error Handling and Edge Cases

    [Fact]
    public async Task EncryptPHIAsync_ShouldHandleNullOrEmptyInput()
    {
        // Act & Assert
        var ex1 = await Record.ExceptionAsync(() => _service.EncryptPHIAsync(null, "PATIENT_001"));
        var ex2 = await Record.ExceptionAsync(() => _service.EncryptPHIAsync("", "PATIENT_001"));
        var ex3 = await Record.ExceptionAsync(() => _service.EncryptPHIAsync("data", null));

        ex1.Should().BeOfType<ArgumentNullException>();
        ex2.Should().BeOfType<ArgumentException>();
        ex3.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task AccessValidation_ShouldHandleInvalidUserRoles()
    {
        // Arrange
        var invalidUserId = "invalid_user_role";
        var patientId = "PATIENT_001";

        // Act
        var hasAccess = await _service.ValidateAccessRightsAsync(invalidUserId, patientId, "Treatment");

        // Assert
        hasAccess.Should().BeFalse();
        
        // Should log security event for invalid access attempt
        _mockAuditService.Verify(x => x.LogSecurityEventAsync(
            "INVALID_ACCESS_ATTEMPT", 
            It.IsAny<string>(),
            invalidUserId), Times.Once);
    }

    #endregion
}