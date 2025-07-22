using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace SmartBoxNext.Services
{
    public class DicomServiceMinimal
    {
        private readonly ILogger<DicomServiceMinimal> _logger;
        private readonly AppConfig _config;

        public DicomServiceMinimal(ILogger<DicomServiceMinimal> logger, AppConfig config)
        {
            _logger = logger;
            _config = config;
            EnsureOutputDirectory();
        }

        private void EnsureOutputDirectory()
        {
            try
            {
                var outputDir = _config.Dicom.OutputDirectory;
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                    _logger.LogInformation($"Created DICOM output directory: {outputDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DICOM output directory");
            }
        }

        public async Task<string> CreateDicomFromImageAsync(string imagePath, string patientId = "TEST001", string patientName = "Test Patient")
        {
            try
            {
                _logger.LogInformation($"Creating DICOM from image: {imagePath}");

                // Create basic DICOM dataset
                var dataset = new DicomDataset
                {
                    // Patient Information
                    { DicomTag.PatientID, patientId },
                    { DicomTag.PatientName, patientName },
                    { DicomTag.PatientBirthDate, "19800101" },
                    { DicomTag.PatientSex, "O" },

                    // Study Information
                    { DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                    { DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd") },
                    { DicomTag.StudyTime, DateTime.Now.ToString("HHmmss") },
                    { DicomTag.StudyDescription, "SmartBox Emergency Imaging" },

                    // Series Information
                    { DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                    { DicomTag.SeriesNumber, "1" },
                    { DicomTag.SeriesDescription, "Emergency Capture" },
                    { DicomTag.Modality, "XC" }, // External-camera Photography

                    // Instance Information
                    { DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID() },
                    { DicomTag.SOPClassUID, DicomUID.VLPhotographicImageStorage },
                    { DicomTag.InstanceNumber, "1" },

                    // Equipment Information
                    { DicomTag.Manufacturer, "CIRSS Medical Systems" },
                    { DicomTag.ManufacturerModelName, "SmartBox Next" },
                    { DicomTag.StationName, _config.Dicom.StationName },
                    { DicomTag.SoftwareVersions, "2.0.0" },

                    // Acquisition Information
                    { DicomTag.AcquisitionDate, DateTime.Now.ToString("yyyyMMdd") },
                    { DicomTag.AcquisitionTime, DateTime.Now.ToString("HHmmss") },
                    { DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd") },
                    { DicomTag.ContentTime, DateTime.Now.ToString("HHmmss") }
                };

                // If image file exists, add it to DICOM
                if (File.Exists(imagePath))
                {
                    // This is a simplified approach - in reality you'd convert the image properly
                    var imageBytes = await File.ReadAllBytesAsync(imagePath);
                    
                    // For now, create a basic DICOM without pixel data
                    // Real implementation would use DicomPixelData
                    dataset.Add(DicomTag.PhotometricInterpretation, "RGB");
                    dataset.Add(DicomTag.SamplesPerPixel, "3");
                    dataset.Add(DicomTag.BitsAllocated, "8");
                    dataset.Add(DicomTag.BitsStored, "8");
                    dataset.Add(DicomTag.HighBit, "7");
                    dataset.Add(DicomTag.PixelRepresentation, "0");
                    dataset.Add(DicomTag.Rows, "480");
                    dataset.Add(DicomTag.Columns, "640");
                }

                // Create DICOM file
                var dicomFile = new DicomFile(dataset);
                
                // Generate output filename
                var outputFileName = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}_{patientId}.dcm";
                var outputPath = Path.Combine(_config.Dicom.OutputDirectory, outputFileName);
                
                // Save DICOM file
                await dicomFile.SaveAsync(outputPath);
                
                _logger.LogInformation($"DICOM file created successfully: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create DICOM from image: {imagePath}");
                throw;
            }
        }

        public async Task<DicomDataset[]> LoadDicomFilesAsync()
        {
            try
            {
                var outputDir = _config.Dicom.OutputDirectory;
                var dicomFiles = Directory.GetFiles(outputDir, "*.dcm");
                var datasets = new DicomDataset[dicomFiles.Length];

                for (int i = 0; i < dicomFiles.Length; i++)
                {
                    var file = await DicomFile.OpenAsync(dicomFiles[i]);
                    datasets[i] = file.Dataset;
                }

                _logger.LogInformation($"Loaded {datasets.Length} DICOM files");
                return datasets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load DICOM files");
                return Array.Empty<DicomDataset>();
            }
        }

        public async Task<bool> ValidateDicomFileAsync(string filePath)
        {
            try
            {
                var file = await DicomFile.OpenAsync(filePath);
                
                // Basic validation checks
                var dataset = file.Dataset;
                var hasPatientId = dataset.Contains(DicomTag.PatientID);
                var hasStudyUID = dataset.Contains(DicomTag.StudyInstanceUID);
                var hasSeriesUID = dataset.Contains(DicomTag.SeriesInstanceUID);
                var hasSOPUID = dataset.Contains(DicomTag.SOPInstanceUID);

                var isValid = hasPatientId && hasStudyUID && hasSeriesUID && hasSOPUID;
                
                _logger.LogInformation($"DICOM validation for {filePath}: {(isValid ? "VALID" : "INVALID")}");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"DICOM validation failed for {filePath}");
                return false;
            }
        }

        public string GetDicomInfo(string filePath)
        {
            try
            {
                var file = DicomFile.Open(filePath);
                var dataset = file.Dataset;

                var patientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "Unknown");
                var patientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, "Unknown");
                var studyDate = dataset.GetSingleValueOrDefault(DicomTag.StudyDate, "Unknown");
                var modality = dataset.GetSingleValueOrDefault(DicomTag.Modality, "Unknown");

                return $"Patient: {patientName} (ID: {patientId}), Study: {studyDate}, Modality: {modality}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get DICOM info for {filePath}");
                return "Error reading DICOM file";
            }
        }
    }
}