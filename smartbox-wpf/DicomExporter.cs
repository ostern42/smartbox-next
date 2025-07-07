using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// DICOM export functionality for medical images
    /// Based on learnings from CamBridge v1 and SmartBox sessions
    /// </summary>
    public class DicomExporter
    {
        private readonly ILogger<DicomExporter> _logger;
        private readonly AppConfig _config;
        
        public DicomExporter(AppConfig config)
        {
            _config = config;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<DicomExporter>();
        }
        
        /// <summary>
        /// Export image as DICOM file with patient information
        /// </summary>
        public async Task<string> ExportDicomAsync(byte[] imageData, PatientInfo patientInfo, string modality = "OT")
        {
            try
            {
                _logger.LogInformation("Starting DICOM export for patient {PatientId}", patientInfo.PatientId);
                
                // Create DICOM dataset
                var dataset = new DicomDataset();
                
                // Patient Module (Type 2 - required but can be empty)
                dataset.AddOrUpdate(DicomTag.PatientName, patientInfo.GetDicomName());
                dataset.AddOrUpdate(DicomTag.PatientID, patientInfo.PatientId ?? "");
                dataset.AddOrUpdate(DicomTag.PatientBirthDate, patientInfo.BirthDate?.ToString("yyyyMMdd") ?? "");
                dataset.AddOrUpdate(DicomTag.PatientSex, patientInfo.Gender ?? "O"); // M, F, or O (other)
                
                // General Study Module
                dataset.AddOrUpdate(DicomTag.StudyInstanceUID, DicomUID.Generate());
                dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.ReferringPhysicianName, "");
                dataset.AddOrUpdate(DicomTag.StudyID, DateTime.Now.ToString("yyyyMMddHHmmss"));
                dataset.AddOrUpdate(DicomTag.AccessionNumber, "");
                dataset.AddOrUpdate(DicomTag.StudyDescription, patientInfo.StudyDescription ?? "SmartBox Capture");
                
                // General Series Module
                dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate());
                dataset.AddOrUpdate(DicomTag.SeriesNumber, "1");
                dataset.AddOrUpdate(DicomTag.Modality, modality); // OT = Other
                dataset.AddOrUpdate(DicomTag.SeriesDescription, "SmartBox Image Capture");
                dataset.AddOrUpdate(DicomTag.SeriesDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.SeriesTime, DateTime.Now.ToString("HHmmss"));
                
                // General Equipment Module
                dataset.AddOrUpdate(DicomTag.Manufacturer, "CIRSS Medical Systems");
                dataset.AddOrUpdate(DicomTag.InstitutionName, patientInfo.Institution ?? "");
                dataset.AddOrUpdate(DicomTag.StationName, Environment.MachineName);
                dataset.AddOrUpdate(DicomTag.ManufacturerModelName, "SmartBox Next");
                dataset.AddOrUpdate(DicomTag.SoftwareVersions, "2.0.0");
                
                // SC Equipment Module
                dataset.AddOrUpdate(DicomTag.ConversionType, "WSD"); // Workstation
                
                // General Image Module
                dataset.AddOrUpdate(DicomTag.InstanceNumber, "1");
                dataset.AddOrUpdate(DicomTag.PatientOrientation, "");
                dataset.AddOrUpdate(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
                
                // Image Pixel Module - Store JPEG as Secondary Capture
                // For simplicity, we'll store as uncompressed RGB
                await AddSimplePixelDataAsync(dataset, imageData);
                
                // SOP Common Module
                dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate());
                
                // Create DICOM file
                var file = new DicomFile(dataset);
                
                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{patientInfo.PatientId}_{timestamp}.dcm";
                var filePath = Path.Combine(_config.Storage.DicomPath, filename);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                
                // Save file
                await file.SaveAsync(filePath);
                
                _logger.LogInformation("DICOM file saved successfully: {Path}", filePath);
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export DICOM");
                throw;
            }
        }
        
        private async Task AddSimplePixelDataAsync(DicomDataset dataset, byte[] jpegData)
        {
            try
            {
                // For now, create a simple test pattern
                // In production, you would decode the JPEG using a library like ImageSharp
                var width = (ushort)640;
                var height = (ushort)480;
                
                // Set image attributes
                dataset.AddOrUpdate(DicomTag.Columns, width);
                dataset.AddOrUpdate(DicomTag.Rows, height);
                dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
                dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
                dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
                dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
                dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
                dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
                
                // Create a simple test pattern (gray image)
                var pixelData = new byte[width * height * 3];
                for (int i = 0; i < pixelData.Length; i += 3)
                {
                    pixelData[i] = 128;     // R
                    pixelData[i + 1] = 128; // G
                    pixelData[i + 2] = 128; // B
                }
                
                // Add pixel data to dataset
                var buffer = new MemoryByteBuffer(pixelData);
                dataset.AddOrUpdate(DicomTag.PixelData, buffer);
                
                _logger.LogDebug("Added test pixel data: {Width}x{Height}", width, height);
                
                // TODO: In production, use ImageSharp or similar to decode JPEG:
                // using var image = Image.Load<Rgb24>(jpegData);
                // width = (ushort)image.Width;
                // height = (ushort)image.Height;
                // ... convert to RGB byte array ...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add pixel data to DICOM");
                throw;
            }
        }
    }
    
    /// <summary>
    /// Patient information for DICOM export
    /// </summary>
    public class PatientInfo
    {
        public string? PatientId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Institution { get; set; }
        public string? StudyDescription { get; set; }
        
        /// <summary>
        /// Get DICOM formatted patient name (Last^First)
        /// </summary>
        public string GetDicomName()
        {
            if (string.IsNullOrEmpty(LastName) && string.IsNullOrEmpty(FirstName))
            {
                return "";
            }
            
            return $"{LastName ?? ""}^{FirstName ?? ""}".Trim('^');
        }
        
        /// <summary>
        /// Create emergency patient template
        /// </summary>
        public static PatientInfo CreateEmergencyPatient(string type)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            return type.ToLower() switch
            {
                "male" => new PatientInfo
                {
                    PatientId = $"NOTFALL_M_{timestamp}",
                    LastName = "Notfall",
                    FirstName = "Männlich",
                    Gender = "M",
                    StudyDescription = "Notfallaufnahme"
                },
                "female" => new PatientInfo
                {
                    PatientId = $"NOTFALL_W_{timestamp}",
                    LastName = "Notfall",
                    FirstName = "Weiblich",
                    Gender = "F",
                    StudyDescription = "Notfallaufnahme"
                },
                "child" => new PatientInfo
                {
                    PatientId = $"NOTFALL_K_{timestamp}",
                    LastName = "Notfall",
                    FirstName = "Kind",
                    Gender = "O",
                    StudyDescription = "Notfallaufnahme Kind"
                },
                _ => new PatientInfo
                {
                    PatientId = $"NOTFALL_{timestamp}",
                    LastName = "Notfall",
                    FirstName = "Patient",
                    Gender = "O",
                    StudyDescription = "Notfallaufnahme"
                }
            };
        }
    }
}