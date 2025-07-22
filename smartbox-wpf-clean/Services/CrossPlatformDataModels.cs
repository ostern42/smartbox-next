using System;
using System.Collections.Generic;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Data models for cross-platform integration services
    /// Provides comprehensive data structures for medical device compliance
    /// </summary>

    #region HIPAA Compliance Models

    public class HIPAAComplianceResult
    {
        public bool IsCompliant { get; set; }
        public double ComplianceScore { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new List<ComplianceViolation>();
        public List<ComplianceRecommendation> Recommendations { get; set; } = new List<ComplianceRecommendation>();
        public string ValidationSummary { get; set; }
        public Dictionary<string, bool> RequirementChecks { get; set; } = new Dictionary<string, bool>();
    }

    public class ComplianceViolation
    {
        public string ViolationType { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string RequirementReference { get; set; }
        public DateTime DetectedAt { get; set; }
        public string RemedyAction { get; set; }
    }

    public class ComplianceRecommendation
    {
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public string ImplementationGuidance { get; set; }
        public TimeSpan EstimatedEffort { get; set; }
    }

    #endregion

    #region Privacy Compliance Models

    public class PrivacyComplianceResult
    {
        public string TestName { get; set; }
        public bool IsCompliant { get; set; }
        public double ComplianceScore { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<PrivacyViolation> Violations { get; set; } = new List<PrivacyViolation>();
        public List<PrivacyRecommendation> Recommendations { get; set; } = new List<PrivacyRecommendation>();
        public Dictionary<string, PrivacyCheckResult> DetailedResults { get; set; } = new Dictionary<string, PrivacyCheckResult>();
    }

    public class PrivacyViolation
    {
        public string ViolationType { get; set; }
        public string Severity { get; set; }
        public string Description { get; set; }
        public string DataCategory { get; set; }
        public DateTime DetectedAt { get; set; }
        public string RequiredAction { get; set; }
    }

    public class PrivacyRecommendation
    {
        public string Category { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public string ImplementationSteps { get; set; }
        public string ComplianceFramework { get; set; }
    }

    public class PrivacyCheckResult
    {
        public bool Passed { get; set; }
        public string Details { get; set; }
        public double Score { get; set; }
        public DateTime CheckTime { get; set; }
    }

    #endregion

    #region Patient Information Models

    public class PatientInfo
    {
        public string PatientId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public string StudyInstanceUID { get; set; }
        public string StudyDescription { get; set; }
        public string AccessionNumber { get; set; }
        public string ReferringPhysician { get; set; }
        public string PatientComments { get; set; }

        public string GetDicomName()
        {
            if (string.IsNullOrEmpty(FirstName) && string.IsNullOrEmpty(LastName))
                return "UNKNOWN^UNKNOWN";
            
            return $"{LastName?.ToUpper() ?? "UNKNOWN"}^{FirstName?.ToUpper() ?? "UNKNOWN"}";
        }
    }

    #endregion

    #region Security Exception Models

    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}