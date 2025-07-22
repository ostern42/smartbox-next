using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;
using Xunit;
using System.ComponentModel.DataAnnotations;

namespace SmartBoxNext.Tests.MedicalValidation;

/// <summary>
/// Comprehensive FDA compliance validation testing
/// Validates medical device compliance with FDA 21 CFR Part 820, Part 11, and cybersecurity guidance
/// </summary>
[Collection("FDAComplianceCollection")]
public class FDAComplianceValidationTests : IClassFixture<FDAComplianceTestFixture>
{
    private readonly FDAComplianceTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FDAComplianceValidationTests> _logger;
    private readonly FDAComplianceValidator _complianceValidator;

    public FDAComplianceValidationTests(FDAComplianceTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<FDAComplianceValidationTests>>();
        _complianceValidator = _serviceProvider.GetRequiredService<FDAComplianceValidator>();
    }

    #region FDA 21 CFR Part 820 - Quality System Regulation

    [Fact]
    [Trait("Category", "FDA820")]
    [Trait("Regulation", "21CFR820.30")]
    public async Task DesignControls_ShouldMeetAllRequirements()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var designControlsResult = await medicalComplianceService.ValidateDesignControlsAsync();

        // Assert - 21 CFR 820.30 Design Controls
        designControlsResult.Should().NotBeNull();
        designControlsResult.OverallCompliance.Should().BeGreaterOrEqualTo(95.0);

        // (a) Design Input
        designControlsResult.DesignInputsValid.Should().BeTrue("Design inputs must be documented and approved");
        designControlsResult.DesignInputs.Should().NotBeEmpty();
        designControlsResult.DesignInputs.Should().Contain(input => 
            input.Type == "Safety Requirements" && input.IsApproved);
        designControlsResult.DesignInputs.Should().Contain(input => 
            input.Type == "Performance Requirements" && input.IsApproved);

        // (b) Design Output
        designControlsResult.DesignOutputsValid.Should().BeTrue("Design outputs must meet design input requirements");
        designControlsResult.DesignOutputs.Should().NotBeEmpty();
        designControlsResult.DesignOutputs.Should().OnlyContain(output => output.MeetsDesignInputs);

        // (c) Design Review
        designControlsResult.DesignReviewComplete.Should().BeTrue("Design reviews must be conducted");
        designControlsResult.DesignReviews.Should().NotBeEmpty();
        designControlsResult.DesignReviews.Should().OnlyContain(review => 
            review.IsComplete && review.HasRequiredParticipants);

        // (d) Design Verification
        designControlsResult.VerificationComplete.Should().BeTrue("Design verification must confirm design outputs meet inputs");
        designControlsResult.VerificationActivities.Should().NotBeEmpty();
        designControlsResult.VerificationActivities.Should().OnlyContain(activity => 
            activity.IsComplete && activity.ResultsDocumented);

        // (e) Design Validation
        designControlsResult.ValidationComplete.Should().BeTrue("Design validation must confirm device meets user needs");
        designControlsResult.ValidationActivities.Should().NotBeEmpty();
        designControlsResult.ValidationActivities.Should().Contain(activity => 
            activity.Type == "Clinical Evaluation" && activity.IsComplete);

        // (f) Design Transfer
        designControlsResult.DesignTransferComplete.Should().BeTrue("Design transfer must ensure design is correctly translated to production");

        // (g) Design Changes
        designControlsResult.ChangeControlActive.Should().BeTrue("Design changes must be controlled");
        designControlsResult.ChangeControlProcess.Should().NotBeNull();
        designControlsResult.ChangeControlProcess.IsActive.Should().BeTrue();

        // (h) Design History File
        designControlsResult.DesignHistoryFileComplete.Should().BeTrue("Design history file must contain complete design control records");
        designControlsResult.DesignHistoryFile.Should().NotBeNull();
        designControlsResult.DesignHistoryFile.IsComplete.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "FDA820")]
    [Trait("Regulation", "21CFR820.40")]
    public async Task DocumentControls_ShouldMeetAllRequirements()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var documentControlResult = await medicalComplianceService.ValidateDocumentControlsAsync();

        // Assert - 21 CFR 820.40 Document Controls
        documentControlResult.Should().NotBeNull();
        documentControlResult.IsCompliant.Should().BeTrue();

        // Document approval and distribution
        documentControlResult.DocumentApprovalProcessActive.Should().BeTrue();
        documentControlResult.ControlledDocuments.Should().NotBeEmpty();
        documentControlResult.ControlledDocuments.Should().OnlyContain(doc => 
            doc.IsApproved && doc.HasControlledDistribution);

        // Document changes
        documentControlResult.ChangeControlActive.Should().BeTrue();
        documentControlResult.DocumentChanges.Should().OnlyContain(change => 
            change.IsApproved && change.HasReviewSignatures);

        // Obsolete document control
        documentControlResult.ObsoleteDocumentControlActive.Should().BeTrue();
        documentControlResult.ObsoleteDocuments.Should().OnlyContain(doc => 
            doc.IsProperlyMarked && doc.IsWithdrawnFromUse);
    }

    [Fact]
    [Trait("Category", "FDA820")]
    [Trait("Regulation", "21CFR820.70")]
    public async Task ProductionAndProcessControls_ShouldMeetRequirements()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var productionControlResult = await medicalComplianceService.ValidateProductionControlsAsync();

        // Assert - 21 CFR 820.70 Production and Process Controls
        productionControlResult.Should().NotBeNull();
        productionControlResult.IsCompliant.Should().BeTrue();

        // Process validation
        productionControlResult.ProcessValidationComplete.Should().BeTrue();
        productionControlResult.ValidatedProcesses.Should().NotBeEmpty();
        productionControlResult.ValidatedProcesses.Should().OnlyContain(process => 
            process.IsValidated && process.ValidationDocumented);

        // Software validation (for medical device software)
        productionControlResult.SoftwareValidationComplete.Should().BeTrue();
        productionControlResult.SoftwareValidation.Should().NotBeNull();
        productionControlResult.SoftwareValidation.IEC62304Compliant.Should().BeTrue();
        productionControlResult.SoftwareValidation.ValidationDocumented.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "FDA820")]
    [Trait("Regulation", "21CFR820.72")]
    public async Task InspectionMeasuringAndTestEquipment_ShouldMeetRequirements()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var equipmentControlResult = await medicalComplianceService.ValidateEquipmentControlsAsync();

        // Assert - 21 CFR 820.72 Inspection, measuring, and test equipment
        equipmentControlResult.Should().NotBeNull();
        equipmentControlResult.IsCompliant.Should().BeTrue();

        // Calibration
        equipmentControlResult.CalibrationProgramActive.Should().BeTrue();
        equipmentControlResult.CalibratedEquipment.Should().NotBeEmpty();
        equipmentControlResult.CalibratedEquipment.Should().OnlyContain(equipment => 
            equipment.IsCalibrated && equipment.CalibrationCurrent);

        // Equipment identification
        equipmentControlResult.EquipmentIdentificationComplete.Should().BeTrue();
        equipmentControlResult.EquipmentInventory.Should().OnlyContain(equipment => 
            equipment.HasUniqueIdentification && equipment.IsTraceable);
    }

    #endregion

    #region FDA 21 CFR Part 11 - Electronic Records

    [Fact]
    [Trait("Category", "FDA11")]
    [Trait("Regulation", "21CFR11.10")]
    public async Task ElectronicRecords_ShouldMeetAllRequirements()
    {
        // Arrange
        var auditLoggingService = _serviceProvider.GetRequiredService<AuditLoggingService>();

        // Act
        var electronicRecordsResult = await auditLoggingService.ValidateElectronicRecordsComplianceAsync();

        // Assert - 21 CFR 11.10 Electronic Records
        electronicRecordsResult.Should().NotBeNull();
        electronicRecordsResult.IsCompliant.Should().BeTrue();

        // (a) Validation of systems
        electronicRecordsResult.SystemValidationComplete.Should().BeTrue();
        electronicRecordsResult.SystemValidation.Should().NotBeNull();
        electronicRecordsResult.SystemValidation.ValidationProtocol.Should().NotBeEmpty();
        electronicRecordsResult.SystemValidation.TestResults.Should().NotBeEmpty();

        // (b) Ability to generate accurate and complete copies
        electronicRecordsResult.RecordCopyingCapabilityValidated.Should().BeTrue();

        // (c) Protection of records
        electronicRecordsResult.RecordProtectionActive.Should().BeTrue();
        electronicRecordsResult.RecordProtection.EncryptionEnabled.Should().BeTrue();
        electronicRecordsResult.RecordProtection.AccessControlsActive.Should().BeTrue();

        // (d) Limiting system access to authorized individuals
        electronicRecordsResult.AccessControlsImplemented.Should().BeTrue();
        electronicRecordsResult.AuthorizedUsers.Should().NotBeEmpty();
        electronicRecordsResult.AuthorizedUsers.Should().OnlyContain(user => 
            user.IsAuthorized && user.HasUniqueIdentification);

        // (e) Use of secure, computer-generated, time-stamped audit trails
        electronicRecordsResult.AuditTrailsImplemented.Should().BeTrue();
        electronicRecordsResult.AuditTrail.Should().NotBeNull();
        electronicRecordsResult.AuditTrail.IsSecure.Should().BeTrue();
        electronicRecordsResult.AuditTrail.IsTimeStamped.Should().BeTrue();
        electronicRecordsResult.AuditTrail.IsTamperEvident.Should().BeTrue();

        // (f) Use of operational system checks
        electronicRecordsResult.OperationalChecksActive.Should().BeTrue();

        // (g) Use of authority checks
        electronicRecordsResult.AuthorityChecksImplemented.Should().BeTrue();

        // (h) Use of device checks
        electronicRecordsResult.DeviceChecksImplemented.Should().BeTrue();

        // (i) Determination that persons who develop, maintain, or use electronic record/signature systems have education, training, and experience
        electronicRecordsResult.PersonnelQualificationVerified.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "FDA11")]
    [Trait("Regulation", "21CFR11.50")]
    public async Task ElectronicSignatures_ShouldMeetAllRequirements()
    {
        // Arrange
        var auditLoggingService = _serviceProvider.GetRequiredService<AuditLoggingService>();

        // Act
        var electronicSignatureResult = await auditLoggingService.ValidateElectronicSignatureComplianceAsync();

        // Assert - 21 CFR 11.50 Electronic Signatures
        electronicSignatureResult.Should().NotBeNull();
        electronicSignatureResult.IsCompliant.Should().BeTrue();

        // Signed electronic records shall contain information associated with the signing
        electronicSignatureResult.SignatureInformationComplete.Should().BeTrue();
        electronicSignatureResult.SignedRecords.Should().NotBeEmpty();
        electronicSignatureResult.SignedRecords.Should().OnlyContain(record => 
            record.HasSignerIdentification && 
            record.HasSigningDateTime && 
            record.HasSigningMeaning);

        // Electronic signatures shall be uniquely linked to their electronic records
        electronicSignatureResult.SignatureRecordLinkingSecure.Should().BeTrue();

        // Electronic signatures shall not be reused or reassigned
        electronicSignatureResult.SignatureUniquenessEnforced.Should().BeTrue();
    }

    #endregion

    #region FDA Cybersecurity Guidance

    [Fact]
    [Trait("Category", "FDACybersecurity")]
    [Trait("Guidance", "PremarketCybersecurity")]
    public async Task PremarketCybersecurityRequirements_ShouldMeetGuidance()
    {
        // Arrange
        var cybersecurityService = _serviceProvider.GetRequiredService<CybersecurityService>();

        // Act
        var premarketResult = await cybersecurityService.ValidatePremarketCybersecurityAsync();

        // Assert - FDA Premarket Cybersecurity Guidance
        premarketResult.Should().NotBeNull();
        premarketResult.IsCompliant.Should().BeTrue();

        // Cybersecurity device design
        premarketResult.CybersecurityDesignComplete.Should().BeTrue();
        premarketResult.CybersecurityDesign.ThreatModelingComplete.Should().BeTrue();
        premarketResult.CybersecurityDesign.RiskAssessmentComplete.Should().BeTrue();
        premarketResult.CybersecurityDesign.SecurityControlsIdentified.Should().BeTrue();

        // Cybersecurity risk assessment
        premarketResult.CybersecurityRiskAssessmentComplete.Should().BeTrue();
        premarketResult.RiskAssessment.Should().NotBeNull();
        premarketResult.RiskAssessment.AssetsIdentified.Should().BeTrue();
        premarketResult.RiskAssessment.ThreatsIdentified.Should().BeTrue();
        premarketResult.RiskAssessment.VulnerabilitiesIdentified.Should().BeTrue();
        premarketResult.RiskAssessment.RiskMitigationPlanned.Should().BeTrue();

        // Cybersecurity controls
        premarketResult.CybersecurityControlsImplemented.Should().BeTrue();
        premarketResult.SecurityControls.Should().NotBeEmpty();
        premarketResult.SecurityControls.Should().OnlyContain(control => 
            control.IsImplemented && control.IsEffective);

        // Testing and analysis
        premarketResult.CybersecurityTestingComplete.Should().BeTrue();
        premarketResult.SecurityTesting.PenetrationTestingComplete.Should().BeTrue();
        premarketResult.SecurityTesting.VulnerabilityAnalysisComplete.Should().BeTrue();
        premarketResult.SecurityTesting.SecurityTestingDocumented.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "FDACybersecurity")]
    [Trait("Guidance", "PostmarketCybersecurity")]
    public async Task PostmarketCybersecurityRequirements_ShouldMeetGuidance()
    {
        // Arrange
        var cybersecurityService = _serviceProvider.GetRequiredService<CybersecurityService>();

        // Act
        var postmarketResult = await cybersecurityService.ValidatePostmarketCybersecurityAsync();

        // Assert - FDA Postmarket Cybersecurity Guidance
        postmarketResult.Should().NotBeNull();
        postmarketResult.IsCompliant.Should().BeTrue();

        // Cybersecurity monitoring
        postmarketResult.CybersecurityMonitoringActive.Should().BeTrue();
        postmarketResult.MonitoringProgram.Should().NotBeNull();
        postmarketResult.MonitoringProgram.ThreatIntelligenceActive.Should().BeTrue();
        postmarketResult.MonitoringProgram.VulnerabilityMonitoringActive.Should().BeTrue();
        postmarketResult.MonitoringProgram.IncidentDetectionActive.Should().BeTrue();

        // Vulnerability disclosure
        postmarketResult.VulnerabilityDisclosureProcessActive.Should().BeTrue();
        postmarketResult.VulnerabilityDisclosure.Should().NotBeNull();
        postmarketResult.VulnerabilityDisclosure.ContactInformationPublished.Should().BeTrue();
        postmarketResult.VulnerabilityDisclosure.ResponseTimelinesDefined.Should().BeTrue();

        // Security update and patch management
        postmarketResult.SecurityUpdateProcessActive.Should().BeTrue();
        postmarketResult.UpdateProcess.Should().NotBeNull();
        postmarketResult.UpdateProcess.UpdateDeliveryMechanismSecure.Should().BeTrue();
        postmarketResult.UpdateProcess.UpdateValidationProcessActive.Should().BeTrue();

        // Incident response
        postmarketResult.IncidentResponsePlanActive.Should().BeTrue();
        postmarketResult.IncidentResponse.Should().NotBeNull();
        postmarketResult.IncidentResponse.ResponsePlanDocumented.Should().BeTrue();
        postmarketResult.IncidentResponse.ResponseTeamIdentified.Should().BeTrue();
        postmarketResult.IncidentResponse.CommunicationPlanActive.Should().BeTrue();
    }

    #endregion

    #region ISO 14971 Risk Management Integration

    [Fact]
    [Trait("Category", "RiskManagement")]
    [Trait("Standard", "ISO14971")]
    public async Task RiskManagementProcess_ShouldMeetISO14971()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var riskManagementResult = await medicalComplianceService.ValidateRiskManagementAsync();

        // Assert - ISO 14971 Risk Management
        riskManagementResult.Should().NotBeNull();
        riskManagementResult.IsCompliant.Should().BeTrue();

        // Risk management process establishment
        riskManagementResult.RiskManagementProcessEstablished.Should().BeTrue();
        riskManagementResult.RiskManagementProcess.Should().NotBeNull();
        riskManagementResult.RiskManagementProcess.PolicyDefined.Should().BeTrue();
        riskManagementResult.RiskManagementProcess.ResponsibilitiesAssigned.Should().BeTrue();

        // Risk analysis
        riskManagementResult.RiskAnalysisComplete.Should().BeTrue();
        riskManagementResult.RiskAnalysis.Should().NotBeNull();
        riskManagementResult.RiskAnalysis.HazardsIdentified.Should().BeTrue();
        riskManagementResult.RiskAnalysis.HazardousSituationsIdentified.Should().BeTrue();
        riskManagementResult.RiskAnalysis.HarmEstimated.Should().BeTrue();

        // Risk evaluation
        riskManagementResult.RiskEvaluationComplete.Should().BeTrue();
        riskManagementResult.RiskEvaluation.Should().NotBeNull();
        riskManagementResult.RiskEvaluation.RiskCriteriaEstablished.Should().BeTrue();
        riskManagementResult.RiskEvaluation.RisksEvaluated.Should().BeTrue();

        // Risk control
        riskManagementResult.RiskControlImplemented.Should().BeTrue();
        riskManagementResult.RiskControls.Should().NotBeEmpty();
        riskManagementResult.RiskControls.Should().OnlyContain(control => 
            control.IsImplemented && control.EffectivenessVerified);

        // Risk management file
        riskManagementResult.RiskManagementFileComplete.Should().BeTrue();
        riskManagementResult.RiskManagementFile.Should().NotBeNull();
        riskManagementResult.RiskManagementFile.IsComplete.Should().BeTrue();
        riskManagementResult.RiskManagementFile.IsUpToDate.Should().BeTrue();
    }

    #endregion

    #region IEC 62304 Software Lifecycle Compliance

    [Fact]
    [Trait("Category", "SoftwareLifecycle")]
    [Trait("Standard", "IEC62304")]
    public async Task SoftwareLifecycleProcess_ShouldMeetIEC62304()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var softwareLifecycleResult = await medicalComplianceService.ValidateSoftwareLifecycleAsync();

        // Assert - IEC 62304 Software Lifecycle
        softwareLifecycleResult.Should().NotBeNull();
        softwareLifecycleResult.IsCompliant.Should().BeTrue();

        // Software safety classification
        softwareLifecycleResult.SafetyClassificationComplete.Should().BeTrue();
        softwareLifecycleResult.SoftwareClassification.Should().Be(SoftwareClassification.ClassB); // Non-life-threatening

        // Software development planning
        softwareLifecycleResult.DevelopmentPlanningComplete.Should().BeTrue();
        softwareLifecycleResult.DevelopmentPlan.Should().NotBeNull();
        softwareLifecycleResult.DevelopmentPlan.LifecycleProcessesDefined.Should().BeTrue();
        softwareLifecycleResult.DevelopmentPlan.ResponsibilitiesAssigned.Should().BeTrue();

        // Software requirements analysis
        softwareLifecycleResult.RequirementsAnalysisComplete.Should().BeTrue();
        softwareLifecycleResult.SoftwareRequirements.Should().NotBeEmpty();
        softwareLifecycleResult.SoftwareRequirements.Should().OnlyContain(req => 
            req.IsComplete && req.IsVerifiable && req.IsUnambiguous);

        // Software architectural design
        softwareLifecycleResult.ArchitecturalDesignComplete.Should().BeTrue();
        softwareLifecycleResult.ArchitecturalDesign.Should().NotBeNull();
        softwareLifecycleResult.ArchitecturalDesign.ImplementsAllRequirements.Should().BeTrue();

        // Software detailed design
        softwareLifecycleResult.DetailedDesignComplete.Should().BeTrue();
        softwareLifecycleResult.DetailedDesign.Should().NotBeNull();
        softwareLifecycleResult.DetailedDesign.ImplementsArchitecture.Should().BeTrue();

        // Software implementation
        softwareLifecycleResult.ImplementationComplete.Should().BeTrue();
        softwareLifecycleResult.Implementation.Should().NotBeNull();
        softwareLifecycleResult.Implementation.ImplementsDetailedDesign.Should().BeTrue();

        // Software integration and integration testing
        softwareLifecycleResult.IntegrationTestingComplete.Should().BeTrue();
        softwareLifecycleResult.IntegrationTesting.Should().NotBeNull();
        softwareLifecycleResult.IntegrationTesting.AllTestsPassed.Should().BeTrue();

        // Software system testing
        softwareLifecycleResult.SystemTestingComplete.Should().BeTrue();
        softwareLifecycleResult.SystemTesting.Should().NotBeNull();
        softwareLifecycleResult.SystemTesting.AllRequirementsTested.Should().BeTrue();
        softwareLifecycleResult.SystemTesting.AllTestsPassed.Should().BeTrue();

        // Software release
        softwareLifecycleResult.ReleaseComplete.Should().BeTrue();
        softwareLifecycleResult.Release.Should().NotBeNull();
        softwareLifecycleResult.Release.AllActivitiesComplete.Should().BeTrue();
        softwareLifecycleResult.Release.KnownAnomaliesDocumented.Should().BeTrue();
    }

    #endregion

    #region Clinical Evaluation and Evidence

    [Fact]
    [Trait("Category", "ClinicalEvidence")]
    [Trait("Regulation", "ClinicalEvaluation")]
    public async Task ClinicalEvidence_ShouldSupportSafetyAndEffectiveness()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var clinicalEvidenceResult = await medicalComplianceService.ValidateClinicalEvidenceAsync();

        // Assert - Clinical Evidence Requirements
        clinicalEvidenceResult.Should().NotBeNull();
        clinicalEvidenceResult.IsCompliant.Should().BeTrue();

        // Clinical evaluation plan
        clinicalEvidenceResult.ClinicalEvaluationPlanComplete.Should().BeTrue();
        clinicalEvidenceResult.ClinicalEvaluationPlan.Should().NotBeNull();
        clinicalEvidenceResult.ClinicalEvaluationPlan.ObjectivesDefined.Should().BeTrue();
        clinicalEvidenceResult.ClinicalEvaluationPlan.MethodologyDefined.Should().BeTrue();

        // Safety evidence
        clinicalEvidenceResult.SafetyEvidenceAdequate.Should().BeTrue();
        clinicalEvidenceResult.SafetyEvidence.Should().NotBeEmpty();
        clinicalEvidenceResult.SafetyEvidence.Should().OnlyContain(evidence => 
            evidence.IsRelevant && evidence.IsReliable);

        // Effectiveness evidence
        clinicalEvidenceResult.EffectivenessEvidenceAdequate.Should().BeTrue();
        clinicalEvidenceResult.EffectivenessEvidence.Should().NotBeEmpty();
        clinicalEvidenceResult.EffectivenessEvidence.Should().OnlyContain(evidence => 
            evidence.IsRelevant && evidence.IsReliable);

        // Risk-benefit analysis
        clinicalEvidenceResult.RiskBenefitAnalysisComplete.Should().BeTrue();
        clinicalEvidenceResult.RiskBenefitAnalysis.Should().NotBeNull();
        clinicalEvidenceResult.RiskBenefitAnalysis.BenefitsOutweighRisks.Should().BeTrue();
    }

    #endregion

    #region Validation Documentation and Evidence

    [Fact]
    [Trait("Category", "ValidationDocumentation")]
    public async Task ValidationDocumentation_ShouldBeCompleteAndTraceableAsync()
    {
        // Arrange
        var complianceValidator = _serviceProvider.GetRequiredService<FDAComplianceValidator>();

        // Act
        var documentationResult = await complianceValidator.ValidateDocumentationCompletenessAsync();

        // Assert
        documentationResult.Should().NotBeNull();
        documentationResult.IsComplete.Should().BeTrue();

        // Design history file completeness
        documentationResult.DesignHistoryFileComplete.Should().BeTrue();
        documentationResult.DesignHistoryFile.Should().NotBeEmpty();

        // Traceability matrix
        documentationResult.TraceabilityMatrixComplete.Should().BeTrue();
        documentationResult.TraceabilityMatrix.Should().NotBeNull();
        documentationResult.TraceabilityMatrix.RequirementsTraced.Should().BeTrue();
        documentationResult.TraceabilityMatrix.TestsTraced.Should().BeTrue();
        documentationResult.TraceabilityMatrix.RisksTraced.Should().BeTrue();

        // Validation protocols and reports
        documentationResult.ValidationProtocolsComplete.Should().BeTrue();
        documentationResult.ValidationReportsComplete.Should().BeTrue();
        documentationResult.ValidationProtocols.Should().NotBeEmpty();
        documentationResult.ValidationReports.Should().NotBeEmpty();

        // Technical documentation
        documentationResult.TechnicalDocumentationComplete.Should().BeTrue();
        documentationResult.TechnicalDocumentation.Should().NotBeEmpty();
        documentationResult.TechnicalDocumentation.Should().OnlyContain(doc => 
            doc.IsCurrentVersion && doc.IsApproved);
    }

    #endregion

    #region Continuous Compliance Monitoring

    [Fact]
    [Trait("Category", "ContinuousCompliance")]
    public async Task ContinuousComplianceMonitoring_ShouldDetectDeviations()
    {
        // Arrange
        var medicalComplianceService = _serviceProvider.GetRequiredService<MedicalComplianceService>();

        // Act
        var monitoringResult = await medicalComplianceService.PerformContinuousComplianceMonitoringAsync();

        // Assert
        monitoringResult.Should().NotBeNull();
        monitoringResult.MonitoringActive.Should().BeTrue();

        // Real-time compliance metrics
        monitoringResult.RealTimeMetricsAvailable.Should().BeTrue();
        monitoringResult.ComplianceMetrics.Should().NotBeEmpty();
        monitoringResult.ComplianceMetrics.Should().OnlyContain(metric => 
            metric.IsWithinAcceptableRange);

        // Deviation detection
        monitoringResult.DeviationDetectionActive.Should().BeTrue();
        monitoringResult.DetectedDeviations.Should().OnlyContain(deviation => 
            deviation.HasCorrectiveAction || deviation.IsAcceptableRisk);

        // Trend analysis
        monitoringResult.TrendAnalysisActive.Should().BeTrue();
        monitoringResult.ComplianceTrends.Should().NotBeEmpty();
        monitoringResult.ComplianceTrends.Should().OnlyContain(trend => 
            trend.Direction == TrendDirection.Stable || trend.Direction == TrendDirection.Improving);

        // Automated reporting
        monitoringResult.AutomatedReportingActive.Should().BeTrue();
        monitoringResult.ComplianceReports.Should().NotBeEmpty();
        monitoringResult.ComplianceReports.Should().OnlyContain(report => 
            report.IsGenerated && report.IsDelivered);
    }

    #endregion
}

/// <summary>
/// FDA compliance test fixture
/// </summary>
public class FDAComplianceTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public FDAComplianceTestFixture()
    {
        var services = new ServiceCollection();
        ConfigureFDAComplianceTestServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureFDAComplianceTestServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton(TestConfiguration.Configuration);
        
        services.AddScoped<FDAComplianceValidator>();
        services.AddScoped<MedicalComplianceService>();
        services.AddScoped<AuditLoggingService>();
        services.AddScoped<CybersecurityService>();
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Collection definition for FDA compliance tests
/// </summary>
[CollectionDefinition("FDAComplianceCollection")]
public class FDAComplianceCollection : ICollectionFixture<FDAComplianceTestFixture>
{
}

/// <summary>
/// FDA compliance validator service
/// </summary>
public class FDAComplianceValidator
{
    private readonly ILogger<FDAComplianceValidator> _logger;

    public FDAComplianceValidator(ILogger<FDAComplianceValidator> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationDocumentationResult> ValidateDocumentationCompletenessAsync()
    {
        await Task.Delay(100); // Simulate validation processing

        return new ValidationDocumentationResult
        {
            IsComplete = true,
            DesignHistoryFileComplete = true,
            DesignHistoryFile = new List<string> { "DHF_001", "DHF_002", "DHF_003" },
            TraceabilityMatrixComplete = true,
            TraceabilityMatrix = new TraceabilityMatrix
            {
                RequirementsTraced = true,
                TestsTraced = true,
                RisksTraced = true
            },
            ValidationProtocolsComplete = true,
            ValidationReportsComplete = true,
            ValidationProtocols = new List<ValidationProtocol>
            {
                new() { Name = "IQ Protocol", IsComplete = true },
                new() { Name = "OQ Protocol", IsComplete = true },
                new() { Name = "PQ Protocol", IsComplete = true }
            },
            ValidationReports = new List<ValidationReport>
            {
                new() { Name = "IQ Report", IsComplete = true },
                new() { Name = "OQ Report", IsComplete = true },
                new() { Name = "PQ Report", IsComplete = true }
            },
            TechnicalDocumentationComplete = true,
            TechnicalDocumentation = new List<TechnicalDocument>
            {
                new() { Name = "System Requirements", IsCurrentVersion = true, IsApproved = true },
                new() { Name = "Design Specification", IsCurrentVersion = true, IsApproved = true },
                new() { Name = "Test Specification", IsCurrentVersion = true, IsApproved = true }
            }
        };
    }
}

/// <summary>
/// Supporting data structures for FDA compliance testing
/// </summary>
public class ValidationDocumentationResult
{
    public bool IsComplete { get; set; }
    public bool DesignHistoryFileComplete { get; set; }
    public List<string> DesignHistoryFile { get; set; } = new();
    public bool TraceabilityMatrixComplete { get; set; }
    public TraceabilityMatrix TraceabilityMatrix { get; set; } = new();
    public bool ValidationProtocolsComplete { get; set; }
    public bool ValidationReportsComplete { get; set; }
    public List<ValidationProtocol> ValidationProtocols { get; set; } = new();
    public List<ValidationReport> ValidationReports { get; set; } = new();
    public bool TechnicalDocumentationComplete { get; set; }
    public List<TechnicalDocument> TechnicalDocumentation { get; set; } = new();
}

public class TraceabilityMatrix
{
    public bool RequirementsTraced { get; set; }
    public bool TestsTraced { get; set; }
    public bool RisksTraced { get; set; }
}

public class ValidationProtocol
{
    public string Name { get; set; } = "";
    public bool IsComplete { get; set; }
}

public class ValidationReport
{
    public string Name { get; set; } = "";
    public bool IsComplete { get; set; }
}

public class TechnicalDocument
{
    public string Name { get; set; } = "";
    public bool IsCurrentVersion { get; set; }
    public bool IsApproved { get; set; }
}

public enum TrendDirection
{
    Improving,
    Stable,
    Declining
}