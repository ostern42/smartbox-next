using System;

namespace SmartBoxNext.Medical
{
    /// <summary>
    /// Medical device configuration structure
    /// FDA 21 CFR Part 820 compliant configuration management
    /// </summary>
    public class MedicalConfig
    {
        /// <summary>Medical device identification</summary>
        public MedicalDeviceInfo DeviceInfo { get; set; } = new();
        
        /// <summary>Patient safety settings</summary>
        public PatientSafetySettings PatientSafety { get; set; } = new();
        
        /// <summary>Medical network configuration</summary>
        public MedicalNetworkSettings MedicalNetwork { get; set; } = new();
        
        /// <summary>DICOM compliance settings</summary>
        public DicomComplianceSettings DicomCompliance { get; set; } = new();
        
        /// <summary>Audit and compliance settings</summary>
        public AuditComplianceSettings AuditCompliance { get; set; } = new();
        
        /// <summary>Medical UI accessibility settings</summary>
        public MedicalUISettings MedicalUI { get; set; } = new();
    }

    /// <summary>
    /// Medical device identification (FDA traceable)
    /// </summary>
    public class MedicalDeviceInfo
    {
        /// <summary>FDA Device Class</summary>
        public string DeviceClass { get; set; } = MedicalConstants.DEVICE_CLASS;
        
        /// <summary>FDA Device Identifier</summary>
        public string FDADeviceId { get; set; } = MedicalConstants.FDA_DEVICE_ID;
        
        /// <summary>Software version (FDA traceable)</summary>
        public string SoftwareVersion { get; set; } = MedicalConstants.SOFTWARE_VERSION;
        
        /// <summary>Medical device version</summary>
        public string MedicalDeviceVersion { get; set; } = MedicalConstants.MEDICAL_DEVICE_VERSION;
        
        /// <summary>Manufacturer information</summary>
        public string Manufacturer { get; set; } = MedicalConstants.MANUFACTURER;
        
        /// <summary>Device serial number</summary>
        public string SerialNumber { get; set; } = $"{MedicalConstants.DEVICE_SERIAL_PREFIX}-{DateTime.Now:yyyyMMdd}";
        
        /// <summary>Installation date (FDA tracking)</summary>
        public DateTime InstallationDate { get; set; } = DateTime.Now;
        
        /// <summary>Last validation date (IEC 62304 requirement)</summary>
        public DateTime LastValidationDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Patient safety settings (IEC 62304 Safety Class B)
    /// </summary>
    public class PatientSafetySettings
    {
        /// <summary>Enable patient safety monitoring</summary>
        public bool EnablePatientSafetyMonitoring { get; set; } = true;
        
        /// <summary>Patient data access timeout (milliseconds)</summary>
        public int PatientDataTimeoutMs { get; set; } = MedicalConstants.PATIENT_DATA_TIMEOUT_MS;
        
        /// <summary>Critical operation timeout (milliseconds)</summary>
        public int CriticalOperationTimeoutMs { get; set; } = MedicalConstants.CRITICAL_OPERATION_TIMEOUT_MS;
        
        /// <summary>Emergency response timeout (milliseconds)</summary>
        public int EmergencyResponseTimeoutMs { get; set; } = MedicalConstants.EMERGENCY_RESPONSE_TIMEOUT_MS;
        
        /// <summary>Maximum patient session duration (minutes)</summary>
        public int MaxPatientSessionMinutes { get; set; } = MedicalConstants.MAX_PATIENT_SESSION_MINUTES;
        
        /// <summary>Auto-logout on inactivity</summary>
        public bool AutoLogoutOnInactivity { get; set; } = true;
        
        /// <summary>Require confirmation for critical actions</summary>
        public bool RequireCriticalActionConfirmation { get; set; } = true;
        
        /// <summary>Enable emergency mode</summary>
        public bool EnableEmergencyMode { get; set; } = true;
    }

    /// <summary>
    /// Medical network settings (IHE integration ready)
    /// </summary>
    public class MedicalNetworkSettings
    {
        /// <summary>Hospital network domain</summary>
        public string HospitalDomain { get; set; } = "hospital.local";
        
        /// <summary>Medical device subnet</summary>
        public string MedicalDeviceSubnet { get; set; } = "10.0.0.0/8";
        
        /// <summary>Isolation network subnet</summary>
        public string IsolationSubnet { get; set; } = "192.168.1.0/24";
        
        /// <summary>PACS connection timeout (seconds)</summary>
        public int PacsConnectionTimeoutSeconds { get; set; } = MedicalConstants.PACS_CONNECTION_TIMEOUT_S;
        
        /// <summary>MWL query timeout (seconds)</summary>
        public int MwlQueryTimeoutSeconds { get; set; } = MedicalConstants.MWL_QUERY_TIMEOUT_S;
        
        /// <summary>Emergency PACS timeout (seconds)</summary>
        public int EmergencyPacsTimeoutSeconds { get; set; } = MedicalConstants.EMERGENCY_PACS_TIMEOUT_S;
        
        /// <summary>Network priority for emergency traffic</summary>
        public int EmergencyNetworkPriority { get; set; } = MedicalConstants.EMERGENCY_NETWORK_PRIORITY;
        
        /// <summary>Enable secure DICOM communication</summary>
        public bool EnableSecureDicom { get; set; } = true;
        
        /// <summary>Enable network redundancy</summary>
        public bool EnableNetworkRedundancy { get; set; } = false;
    }

    /// <summary>
    /// DICOM compliance settings (NEMA PS3.x standards)
    /// </summary>
    public class DicomComplianceSettings
    {
        /// <summary>DICOM version compliance</summary>
        public string DicomVersion { get; set; } = MedicalConstants.DICOM_VERSION;
        
        /// <summary>Default modality</summary>
        public string DefaultModality { get; set; } = MedicalConstants.MODALITY_EXTERNAL_CAMERA;
        
        /// <summary>Station AE Title</summary>
        public string StationAeTitle { get; set; } = MedicalConstants.DEFAULT_AE_TITLE;
        
        /// <summary>Station name</summary>
        public string StationName { get; set; } = MedicalConstants.DEFAULT_STATION_NAME;
        
        /// <summary>Validate all DICOM tags</summary>
        public bool ValidateAllDicomTags { get; set; } = true;
        
        /// <summary>Enforce DICOM conformance</summary>
        public bool EnforceDicomConformance { get; set; } = true;
        
        /// <summary>Generate DICOM SR (Structured Reports)</summary>
        public bool GenerateDicomSR { get; set; } = false;
        
        /// <summary>Enable DICOM audit logging</summary>
        public bool EnableDicomAuditLogging { get; set; } = true;
        
        /// <summary>Minimum image quality for medical use</summary>
        public int MinimumImageQuality { get; set; } = MedicalConstants.MEDICAL_IMAGE_QUALITY_MIN;
        
        /// <summary>Maximum DICOM file size (MB)</summary>
        public long MaxDicomFileSizeMB { get; set; } = MedicalConstants.MAX_DICOM_FILE_SIZE_MB;
    }

    /// <summary>
    /// Audit and compliance settings (FDA 21 CFR Part 11)
    /// </summary>
    public class AuditComplianceSettings
    {
        /// <summary>Enable comprehensive audit logging</summary>
        public bool EnableAuditLogging { get; set; } = true;
        
        /// <summary>Audit trail retention period (days)</summary>
        public int AuditRetentionDays { get; set; } = MedicalConstants.AUDIT_TRAIL_RETENTION_DAYS;
        
        /// <summary>Patient data retention period (years)</summary>
        public int PatientDataRetentionYears { get; set; } = MedicalConstants.PATIENT_DATA_RETENTION_YEARS;
        
        /// <summary>Imaging data retention period (years)</summary>
        public int ImagingDataRetentionYears { get; set; } = MedicalConstants.IMAGING_DATA_RETENTION_YEARS;
        
        /// <summary>Backup verification interval (hours)</summary>
        public int BackupVerificationIntervalHours { get; set; } = MedicalConstants.BACKUP_VERIFICATION_INTERVAL_HOURS;
        
        /// <summary>Enable data integrity verification</summary>
        public bool EnableDataIntegrityVerification { get; set; } = true;
        
        /// <summary>Audit all patient data access</summary>
        public bool AuditAllPatientAccess { get; set; } = true;
        
        /// <summary>Audit all data modifications</summary>
        public bool AuditAllDataModifications { get; set; } = true;
        
        /// <summary>Audit all system configuration changes</summary>
        public bool AuditAllSystemChanges { get; set; } = true;
        
        /// <summary>Generate compliance reports</summary>
        public bool GenerateComplianceReports { get; set; } = true;
    }

    /// <summary>
    /// Medical UI accessibility settings (WCAG 2.1 AA compliant)
    /// </summary>
    public class MedicalUISettings
    {
        /// <summary>High contrast mode for medical visibility</summary>
        public bool HighContrastMode { get; set; } = false;
        
        /// <summary>Large text mode for accessibility</summary>
        public bool LargeTextMode { get; set; } = false;
        
        /// <summary>Night shift mode (low blue light)</summary>
        public bool NightShiftMode { get; set; } = false;
        
        /// <summary>Emergency mode UI (red tinting)</summary>
        public bool EmergencyModeUI { get; set; } = false;
        
        /// <summary>Minimum touch target size (pixels)</summary>
        public int MinTouchTargetSize { get; set; } = MedicalConstants.MIN_TOUCH_TARGET_SIZE;
        
        /// <summary>Critical button height (pixels)</summary>
        public int CriticalButtonHeight { get; set; } = MedicalConstants.CRITICAL_BUTTON_HEIGHT;
        
        /// <summary>Emergency button height (pixels)</summary>
        public int EmergencyButtonHeight { get; set; } = MedicalConstants.EMERGENCY_BUTTON_HEIGHT;
        
        /// <summary>Patient name font size</summary>
        public int PatientNameFontSize { get; set; } = MedicalConstants.PATIENT_NAME_FONT_SIZE;
        
        /// <summary>Critical alert font size</summary>
        public int CriticalAlertFontSize { get; set; } = MedicalConstants.CRITICAL_ALERT_FONT_SIZE;
        
        /// <summary>Auto-adjust UI for screen distance</summary>
        public bool AutoAdjustForScreenDistance { get; set; } = true;
        
        /// <summary>Enable voice announcements for critical events</summary>
        public bool EnableVoiceAnnouncements { get; set; } = false;
    }
}