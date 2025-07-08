using System;

namespace SmartBoxNext
{
    /// <summary>
    /// Represents a DICOM Modality Worklist item
    /// </summary>
    public class WorklistItem
    {
        // Critical: This MUST come from MWL and be used for all images!
        public string StudyInstanceUID { get; set; }
        
        // Patient Demographics
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Sex { get; set; }
        
        // Study Information
        public string AccessionNumber { get; set; }
        public string StudyDescription { get; set; }
        public string RequestedProcedureDescription { get; set; }
        
        // Scheduled Procedure Step
        public DateTime ScheduledDate { get; set; }
        public TimeSpan ScheduledTime { get; set; }
        public string Modality { get; set; }
        public string ScheduledStationAET { get; set; }
        public string ScheduledPerformingPhysician { get; set; }
        public string ScheduledProcedureStepDescription { get; set; }
        public string ScheduledProcedureStepId { get; set; }
        
        // Additional fields for UI
        public bool IsEmergency => 
            PatientName?.Contains("NOTFALL", StringComparison.OrdinalIgnoreCase) == true ||
            PatientName?.Contains("EMERGENCY", StringComparison.OrdinalIgnoreCase) == true ||
            StudyDescription?.Contains("NOTFALL", StringComparison.OrdinalIgnoreCase) == true ||
            StudyDescription?.Contains("EMERGENCY", StringComparison.OrdinalIgnoreCase) == true;
            
        public string DisplayName => FormatPatientName(PatientName);
        
        public string DisplayAge
        {
            get
            {
                if (!BirthDate.HasValue) return "";
                var age = DateTime.Today.Year - BirthDate.Value.Year;
                if (DateTime.Today < BirthDate.Value.AddYears(age)) age--;
                return $"{age}J";
            }
        }
        
        public string DisplayTime => ScheduledTime.ToString(@"hh\:mm");
        
        // Helper to format DICOM patient names (Last^First^Middle)
        private string FormatPatientName(string dicomName)
        {
            if (string.IsNullOrEmpty(dicomName)) return "Unbekannt";
            
            var parts = dicomName.Split('^');
            if (parts.Length >= 2)
                return $"{parts[0]}, {parts[1]}";
            return dicomName;
        }
    }
    
    /// <summary>
    /// Cache container for worklist items
    /// </summary>
    public class MwlCache
    {
        public DateTime LastUpdate { get; set; }
        public List<WorklistItem> Items { get; set; } = new List<WorklistItem>();
        public string ServerInfo { get; set; }
        public bool IsOffline { get; set; }
        
        public TimeSpan Age => DateTime.Now - LastUpdate;
        public bool IsStale => Age.TotalHours > 24;
    }
}