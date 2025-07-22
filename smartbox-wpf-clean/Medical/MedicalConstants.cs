using System;
using WpfColor = System.Windows.Media.Color;

namespace SmartBoxNext.Medical
{
    /// <summary>
    /// FDA 21 CFR Part 820 compliant medical device constants
    /// IEC 62304 Medical Device Software Safety Classification
    /// DICOM 3.0 Standard compliance
    /// </summary>
    public static class MedicalConstants
    {
        #region Device Classification (FDA 21 CFR Part 820)
        
        /// <summary>FDA Device Class IIa - Medical Software</summary>
        public const string DEVICE_CLASS = "IIa";
        
        /// <summary>FDA Device Identifier</summary>
        public const string FDA_DEVICE_ID = "SmartBox-Next-2.0";
        
        /// <summary>Software Version (FDA Traceable)</summary>
        public const string SOFTWARE_VERSION = "2.0.0";
        
        /// <summary>Medical Device Version (FDA Compliant)</summary>
        public const string MEDICAL_DEVICE_VERSION = "v2.0.0-FDA";
        
        /// <summary>Manufacturer Information</summary>
        public const string MANUFACTURER = "CIRSS Medical Systems";
        
        /// <summary>Device Serial Number Prefix</summary>
        public const string DEVICE_SERIAL_PREFIX = "SBN-2025";
        
        #endregion

        #region DICOM Compliance (NEMA PS3.x Standards)
        
        /// <summary>DICOM Version Compliance</summary>
        public const string DICOM_VERSION = "3.0";
        
        /// <summary>External-camera Photography (DICOM Standard)</summary>
        public const string MODALITY_EXTERNAL_CAMERA = "XC";
        
        /// <summary>Endoscopy Modality</summary>
        public const string MODALITY_ENDOSCOPY = "ES";
        
        /// <summary>Other Modality</summary>
        public const string MODALITY_OTHER = "OT";
        
        /// <summary>Emergency/X-Ray Angiography</summary>
        public const string MODALITY_EMERGENCY = "XA";
        
        /// <summary>Default DICOM AE Title</summary>
        public const string DEFAULT_AE_TITLE = "SMARTBOX";
        
        /// <summary>Default DICOM Station Name</summary>
        public const string DEFAULT_STATION_NAME = "SMARTBOX-ED";
        
        #endregion

        #region Network Configuration (IHE Integration)
        
        /// <summary>DICOM Default Port (NEMA PS3.8)</summary>
        public const int DICOM_DEFAULT_PORT = 104;
        
        /// <summary>DICOM over TLS Port</summary>
        public const int DICOM_TLS_PORT = 2762;
        
        /// <summary>Modality Worklist Default Port</summary>
        public const int MWL_DEFAULT_PORT = 105;
        
        /// <summary>Secure MWL Port (Hospital Networks)</summary>
        public const int MWL_SECURE_PORT = 11112;
        
        /// <summary>SmartBox WebServer Port (Development)</summary>
        public const int SMARTBOX_WEB_PORT = 8080;
        
        #endregion

        #region Patient Safety Timeouts (IEC 62304)
        
        /// <summary>Maximum time for patient data access (5 seconds)</summary>
        public const int PATIENT_DATA_TIMEOUT_MS = 5000;
        
        /// <summary>Maximum time for critical operations (3 seconds)</summary>
        public const int CRITICAL_OPERATION_TIMEOUT_MS = 3000;
        
        /// <summary>Maximum emergency response time (1 second)</summary>
        public const int EMERGENCY_RESPONSE_TIMEOUT_MS = 1000;
        
        /// <summary>PACS connection timeout (medical grade)</summary>
        public const int PACS_CONNECTION_TIMEOUT_S = 10;
        
        /// <summary>MWL query timeout (medical grade)</summary>
        public const int MWL_QUERY_TIMEOUT_S = 15;
        
        /// <summary>Emergency PACS timeout (fastest response)</summary>
        public const int EMERGENCY_PACS_TIMEOUT_S = 5;
        
        #endregion

        #region Medical UI Standards (WCAG 2.1 AA Compliant)
        
        /// <summary>Emergency Alert Color (High Contrast)</summary>
        public static readonly WpfColor EMERGENCY_RED = WpfColor.FromRgb(204, 0, 0);
        
        /// <summary>Warning Color (Medical Standard)</summary>
        public static readonly WpfColor WARNING_ORANGE = WpfColor.FromRgb(255, 140, 0);
        
        /// <summary>Success Color (Medical Standard)</summary>
        public static readonly WpfColor SUCCESS_GREEN = WpfColor.FromRgb(0, 128, 0);
        
        /// <summary>Information Color (Medical Standard)</summary>
        public static readonly WpfColor INFO_BLUE = WpfColor.FromRgb(0, 100, 200);
        
        /// <summary>Patient Connected Status</summary>
        public static readonly WpfColor PATIENT_CONNECTED = WpfColor.FromRgb(0, 150, 0);
        
        /// <summary>Patient Disconnected Status</summary>
        public static readonly WpfColor PATIENT_DISCONNECTED = WpfColor.FromRgb(150, 0, 0);
        
        /// <summary>Device Ready Status</summary>
        public static readonly WpfColor DEVICE_READY = WpfColor.FromRgb(0, 100, 200);
        
        /// <summary>Device Error Status</summary>
        public static readonly WpfColor DEVICE_ERROR = WpfColor.FromRgb(200, 0, 0);
        
        /// <summary>Medical Background (Low Eye Strain)</summary>
        public static readonly WpfColor MEDICAL_BACKGROUND = WpfColor.FromRgb(248, 249, 250);
        
        /// <summary>Night Shift Background</summary>
        public static readonly WpfColor NIGHT_SHIFT_BACKGROUND = WpfColor.FromRgb(25, 25, 25);
        
        /// <summary>Primary Text (High Contrast)</summary>
        public static readonly WpfColor PRIMARY_TEXT = WpfColor.FromRgb(33, 37, 41);
        
        /// <summary>Secondary Text (Medium Contrast)</summary>
        public static readonly WpfColor SECONDARY_TEXT = WpfColor.FromRgb(108, 117, 125);
        
        #endregion

        #region Touch UI Standards (Medical Accessibility)
        
        /// <summary>Minimum touch target size (accessibility)</summary>
        public const int MIN_TOUCH_TARGET_SIZE = 44;
        
        /// <summary>Critical action button height</summary>
        public const int CRITICAL_BUTTON_HEIGHT = 60;
        
        /// <summary>Emergency button height (largest for safety)</summary>
        public const int EMERGENCY_BUTTON_HEIGHT = 80;
        
        /// <summary>Standard medical UI padding</summary>
        public const int MEDICAL_FORM_PADDING = 16;
        
        #endregion

        #region Medical Font Sizes (Readability Standards)
        
        /// <summary>Patient name font size (high visibility)</summary>
        public const int PATIENT_NAME_FONT_SIZE = 24;
        
        /// <summary>Medical data font size</summary>
        public const int MEDICAL_DATA_FONT_SIZE = 18;
        
        /// <summary>Standard UI font size</summary>
        public const int STANDARD_UI_FONT_SIZE = 16;
        
        /// <summary>Critical alert font size (maximum visibility)</summary>
        public const int CRITICAL_ALERT_FONT_SIZE = 28;
        
        #endregion

        #region Data Validation (Medical Standards)
        
        /// <summary>Minimum patient ID length</summary>
        public const int PATIENT_ID_MIN_LENGTH = 3;
        
        /// <summary>Maximum patient ID length (DICOM limit)</summary>
        public const int PATIENT_ID_MAX_LENGTH = 64;
        
        /// <summary>Minimum medical image quality (JPEG)</summary>
        public const int MEDICAL_IMAGE_QUALITY_MIN = 85;
        
        /// <summary>Minimum diagnostic image width</summary>
        public const int MIN_MEDICAL_IMAGE_WIDTH = 640;
        
        /// <summary>Minimum diagnostic image height</summary>
        public const int MIN_MEDICAL_IMAGE_HEIGHT = 480;
        
        /// <summary>Maximum DICOM file size (500MB)</summary>
        public const long MAX_DICOM_FILE_SIZE_MB = 500;
        
        #endregion

        #region Audit & Compliance (FDA 21 CFR Part 11)
        
        /// <summary>Audit trail retention (7 years - FDA requirement)</summary>
        public const int AUDIT_TRAIL_RETENTION_DAYS = 2555;
        
        /// <summary>Patient data retention (7 years - legal requirement)</summary>
        public const int PATIENT_DATA_RETENTION_YEARS = 7;
        
        /// <summary>Medical imaging data retention (10 years)</summary>
        public const int IMAGING_DATA_RETENTION_YEARS = 10;
        
        /// <summary>Backup verification interval (daily)</summary>
        public const int BACKUP_VERIFICATION_INTERVAL_HOURS = 24;
        
        /// <summary>Maximum patient session duration (30 minutes)</summary>
        public const int MAX_PATIENT_SESSION_MINUTES = 30;
        
        #endregion

        #region Medical Priority Levels
        
        /// <summary>Emergency network priority (highest)</summary>
        public const int EMERGENCY_NETWORK_PRIORITY = 1;
        
        /// <summary>Normal network priority</summary>
        public const int NORMAL_NETWORK_PRIORITY = 3;
        
        /// <summary>Background network priority (lowest)</summary>
        public const int BACKGROUND_NETWORK_PRIORITY = 5;
        
        #endregion
    }
}