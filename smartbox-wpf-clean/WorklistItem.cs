using System;

namespace SmartBoxNext
{
    /// <summary>
    /// Represents a DICOM Modality Worklist item
    /// </summary>
    public class WorklistItem
    {
        // Patient Information
        public string PatientId { get; set; } = "";
        public string PatientName { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string Sex { get; set; } = "";

        // Study Information
        public string StudyInstanceUID { get; set; } = "";
        public string AccessionNumber { get; set; } = "";
        public string StudyDescription { get; set; } = "";
        public string RequestedProcedureDescription { get; set; } = "";

        // Scheduled Procedure Step
        public DateTime ScheduledDate { get; set; } = DateTime.Today;
        public TimeSpan ScheduledTime { get; set; } = TimeSpan.Zero;
        public string Modality { get; set; } = "CR";
        public string ScheduledStationAET { get; set; } = "";
        public string ScheduledPerformingPhysician { get; set; } = "";
        public string ScheduledProcedureStepDescription { get; set; } = "";
        public string ScheduledProcedureStepId { get; set; } = "";

        // Additional Properties
        public bool IsEmergency { get; set; } = false;
        public string Priority { get; set; } = "ROUTINE";

        /// <summary>
        /// Get formatted patient age
        /// </summary>
        public string Age
        {
            get
            {
                if (!BirthDate.HasValue) return "";
                var age = DateTime.Now.Year - BirthDate.Value.Year;
                if (DateTime.Now.DayOfYear < BirthDate.Value.DayOfYear) age--;
                return age.ToString();
            }
        }

        /// <summary>
        /// Get formatted scheduled time string
        /// </summary>
        public string ScheduledTimeString => ScheduledTime.ToString(@"hh\:mm");

        /// <summary>
        /// Get display name (formatted)
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(PatientName)) return PatientId;
                
                // Convert "LASTNAME^FIRSTNAME" to "Lastname, Firstname"
                var parts = PatientName.Split('^');
                if (parts.Length >= 2)
                {
                    return $"{parts[0]}, {parts[1]}";
                }
                return PatientName;
            }
        }
    }
}