namespace SmartBoxNext
{
    /// <summary>
    /// Configuration for DICOM Modality Worklist
    /// </summary>
    public class MwlConfig
    {
        public bool EnableWorklist { get; set; } = false;
        public string MwlServerHost { get; set; } = "localhost";
        public int MwlServerPort { get; set; } = 105;
        public string MwlServerAET { get; set; } = "ORTHANC";
        public int AutoRefreshSeconds { get; set; } = 300;
        public bool ShowEmergencyFirst { get; set; } = true;
        public int CacheExpiryHours { get; set; } = 24;
    }
}