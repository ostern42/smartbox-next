using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SmartBoxNext.Services;
using Xunit;
using AutoFixture;
using AutoFixture.Xunit2;

namespace SmartBoxNext.Tests.Unit.Services;

/// <summary>
/// Comprehensive unit tests for Medical Compliance Service
/// Validates FDA 21 CFR Part 820 compliance and medical device regulations
/// </summary>
public class MedicalComplianceServiceTests : IClassFixture<TestFixture>
{
    private readonly TestFixture _fixture;
    private readonly Mock<ILogger<MedicalComplianceService>> _mockLogger;
    private readonly Mock<IAuditLoggingService> _mockAuditService;
    private readonly MedicalComplianceService _service;
    private readonly Fixture _autoFixture;

    public MedicalComplianceServiceTests(TestFixture fixture)
    {
        _fixture = fixture;
        _mockLogger = new Mock<ILogger<MedicalComplianceService>>();
        _mockAuditService = new Mock<IAuditLoggingService>();
        _autoFixture = new Fixture();
        
        _service = new MedicalComplianceService(_mockLogger.Object, _mockAuditService.Object);
    }

    #region FDA 21 CFR Part 820 Compliance Tests

    [Fact]
    public async Task ValidateDesignControlsAsync_ShouldReturnCompliant_WhenAllControlsImplemented()
    {
        // Arrange
        var testData = _autoFixture.Create<MedicalDeviceValidationRequest>();

        // Act
        var result = await _service.ValidateDesignControlsAsync();

        // Assert
        result.Should().NotBeNull();
        result.OverallCompliance.Should().BeGreaterThan(90);
        result.DesignInputsValid.Should().BeTrue();
        result.DesignOutputsValid.Should().BeTrue();
        result.DesignReviewComplete.Should().BeTrue();
        result.VerificationComplete.Should().BeTrue();
        result.ValidationComplete.Should().BeTrue();
        result.ChangeControlActive.Should().BeTrue();
        result.DesignHistoryFileComplete.Should().BeTrue();

        // Verify audit logging
        _mockAuditService.Verify(x => x.LogComplianceEventAsync(
            "FDA_DESIGN_CONTROLS_VALIDATION", 
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ValidateDesignControlsAsync_ShouldReturnNonCompliant_WhenControlsMissing()
    {
        // Arrange - Simulate missing design controls
        _service.SimulateMissingDesignControls = true;

        // Act
        var result = await _service.ValidateDesignControlsAsync();

        // Assert
        result.Should().NotBeNull();
        result.OverallCompliance.Should().BeLessThan(90);
        result.ValidationErrors.Should().NotBeEmpty();
        result.RequiredActions.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("DesignInputs")]
    [InlineData("DesignOutputs")]
    [InlineData("DesignReview")]
    [InlineData("Verification")]
    [InlineData("Validation")]
    public async Task ValidateSpecificDesignControl_ShouldValidateCorrectly(string controlType)
    {
        // Act
        var result = await _service.ValidateSpecificDesignControlAsync(controlType);

        // Assert
        result.Should().NotBeNull();
        result.ControlType.Should().Be(controlType);
        result.IsValid.Should().BeTrue();
        result.ComplianceEvidence.Should().NotBeEmpty();
    }

    #endregion

    #region ISO 14971 Risk Management Tests

    [Fact]
    public async Task PerformRiskAnalysisAsync_ShouldIdentifyAllRiskCategories()
    {
        // Act
        var result = await _service.PerformRiskAnalysisAsync();

        // Assert
        result.Should().NotBeNull();
        result.SoftwareRisks.Should().NotBeEmpty();
        result.HardwareRisks.Should().NotBeEmpty();
        result.NetworkSecurityRisks.Should().NotBeEmpty();
        result.DataIntegrityRisks.Should().NotBeEmpty();
        result.PatientSafetyRisks.Should().NotBeEmpty();
        result.CybersecurityRisks.Should().NotBeEmpty();
        result.RegulatoryComplianceRisks.Should().NotBeEmpty();
        result.OverallRiskLevel.Should().BeLessOrEqualTo(RiskLevel.Medium);
    }

    [Fact]
    public async Task PerformRiskAnalysisAsync_ShouldCalculateCorrectRiskMatrix()
    {
        // Act
        var result = await _service.PerformRiskAnalysisAsync();

        // Assert
        result.RiskMatrix.Should().NotBeNull();
        result.RiskMatrix.CriticalRisks.Should().BeEmpty(); // No critical risks allowed
        result.RiskMatrix.HighRisks.Count.Should().BeLessOrEqualTo(3);
        result.RiskMatrix.MediumRisks.Should().NotBeEmpty();
        result.RiskMatrix.LowRisks.Should().NotBeEmpty();
    }

    #endregion

    #region IEC 62304 Software Lifecycle Tests

    [Fact]
    public async Task ValidateSoftwareLifecycleAsync_ShouldMeetIEC62304Requirements()
    {
        // Act
        var result = await _service.ValidateSoftwareLifecycleAsync();

        // Assert
        result.Should().NotBeNull();
        result.SoftwareClassification.Should().Be(SoftwareClassification.ClassB); // Non-life-threatening
        result.LifecycleProcessesComplete.Should().BeTrue();
        result.PlanningComplete.Should().BeTrue();
        result.RequirementsAnalysisComplete.Should().BeTrue();
        result.ArchitecturalDesignComplete.Should().BeTrue();
        result.DetailedDesignComplete.Should().BeTrue();
        result.ImplementationComplete.Should().BeTrue();
        result.IntegrationTestingComplete.Should().BeTrue();
        result.SystemTestingComplete.Should().BeTrue();
        result.ReleaseComplete.Should().BeTrue();
    }

    #endregion

    #region Patient Safety Validation Tests

    [Fact]
    public async Task ValidatePatientSafetyAsync_ShouldEnsureNoSafetyRisks()
    {
        // Arrange
        var patientData = _autoFixture.Create<PatientSafetyTestData>();

        // Act
        var result = await _service.ValidatePatientSafetyAsync(patientData);

        // Assert
        result.Should().NotBeNull();
        result.SafetyLevel.Should().Be(PatientSafetyLevel.Safe);
        result.IdentifiedRisks.Should().BeEmpty();
        result.SafetyMeasuresActive.Should().BeTrue();
        result.EmergencyProceduresAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePatientSafetyAsync_ShouldDetectPotentialSafetyIssues()
    {
        // Arrange
        var unsafeData = new PatientSafetyTestData
        {
            HasMalformedData = true,
            HasIncorrectPatientId = true,
            HasInvalidMeasurements = true
        };

        // Act
        var result = await _service.ValidatePatientSafetyAsync(unsafeData);

        // Assert
        result.Should().NotBeNull();
        result.SafetyLevel.Should().Be(PatientSafetyLevel.Warning);
        result.IdentifiedRisks.Should().NotBeEmpty();
        result.RequiredActions.Should().NotBeEmpty();
    }

    #endregion

    #region Quality Management System Tests

    [Fact]
    public async Task ValidateQualityManagementSystemAsync_ShouldMeetISO13485Requirements()
    {
        // Act
        var result = await _service.ValidateQualityManagementSystemAsync();

        // Assert
        result.Should().NotBeNull();
        result.QMSCompliance.Should().BeGreaterThan(95);
        result.DocumentControlCompliant.Should().BeTrue();
        result.ManagementResponsibilityCompliant.Should().BeTrue();
        result.ResourceManagementCompliant.Should().BeTrue();
        result.ProductRealizationCompliant.Should().BeTrue();
        result.MeasurementAnalysisCompliant.Should().BeTrue();
        result.ContinuousImprovementActive.Should().BeTrue();
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public async Task ValidateDesignControlsAsync_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var result = await _service.ValidateDesignControlsAsync();

        // Assert
        var duration = DateTime.UtcNow - startTime;
        duration.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(5));
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task MultipleValidations_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var tasks = new List<Task<ComplianceResult>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_service.ValidateDesignControlsAsync());
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.OverallCompliance > 90);
    }

    #endregion

    #region Edge Case and Error Handling Tests

    [Fact]
    public async Task ValidateDesignControlsAsync_ShouldHandleNullInputGracefully()
    {
        // Act & Assert
        var ex = await Record.ExceptionAsync(() => _service.ValidateDesignControlsAsync(null));
        ex.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task ValidateDesignControlsAsync_ShouldRecoverFromTransientFailures()
    {
        // Arrange - Simulate transient failure
        _service.SimulateTransientFailure = true;

        // Act
        var result = await _service.ValidateDesignControlsAsync();

        // Assert
        result.Should().NotBeNull();
        result.HasTransientFailures.Should().BeTrue();
        result.RecoverySuccessful.Should().BeTrue();
    }

    #endregion

    #region Integration with Audit Service Tests

    [Fact]
    public async Task AllComplianceOperations_ShouldGenerateAuditTrail()
    {
        // Act
        await _service.ValidateDesignControlsAsync();
        await _service.PerformRiskAnalysisAsync();
        await _service.ValidateSoftwareLifecycleAsync();

        // Assert - Verify audit logging for each operation
        _mockAuditService.Verify(x => x.LogComplianceEventAsync(
            It.IsAny<string>(), 
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.AtLeast(3));
    }

    #endregion
}

/// <summary>
/// Test fixture for shared test setup
/// </summary>
public class TestFixture : IDisposable
{
    public TestFixture()
    {
        // Setup shared test resources
    }

    public void Dispose()
    {
        // Cleanup shared test resources
    }
}

/// <summary>
/// Test data structures for medical compliance testing
/// </summary>
public class MedicalDeviceValidationRequest
{
    public string DeviceId { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public SoftwareClassification Classification { get; set; }
    public List<string> RequiredStandards { get; set; } = new();
}

public class PatientSafetyTestData
{
    public bool HasMalformedData { get; set; }
    public bool HasIncorrectPatientId { get; set; }
    public bool HasInvalidMeasurements { get; set; }
    public Dictionary<string, object> AdditionalTestData { get; set; } = new();
}

public enum SoftwareClassification
{
    ClassA, // Non-life-threatening, non-critical
    ClassB, // Non-life-threatening
    ClassC  // Life-threatening
}

public enum PatientSafetyLevel
{
    Safe,
    Warning,
    Critical
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}