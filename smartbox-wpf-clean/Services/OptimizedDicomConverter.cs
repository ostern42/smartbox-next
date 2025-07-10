using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SmartBoxNext.Services;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Optimized DICOM converter supporting YUY2, RGB, and JPEG inputs
    /// Based on research findings and CamBridge implementation patterns
    /// </summary>
    public class OptimizedDicomConverter
    {
        private readonly ILogger<OptimizedDicomConverter> _logger;
        private readonly AppConfig _config;
        
        // DICOM constants
        private const string MODALITY_OTHER = "OT";
        private const string MODALITY_ENDOSCOPY = "ES";
        private const string MODALITY_PHOTO = "XC"; // External-camera Photography
        
        public OptimizedDicomConverter(ILogger<OptimizedDicomConverter> logger, AppConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Convert YUY2 frame data directly to DICOM Secondary Capture
        /// </summary>
        public async Task<string> ConvertYUY2ToDicomAsync(
            byte[] yuy2Data, 
            int width, 
            int height, 
            PatientInfo patientInfo,
            string modality = MODALITY_OTHER)
        {
            _logger.LogInformation("Converting YUY2 frame ({Width}x{Height}) to DICOM for patient {PatientId}", 
                width, height, patientInfo.PatientId);

            try
            {
                // Convert YUY2 to RGB24 for DICOM storage
                var rgbData = YUY2Converter.ConvertToRGB24(yuy2Data, width, height);
                
                // Create DICOM with RGB pixel data
                return await CreateDicomFromRGBAsync(rgbData, width, height, patientInfo, modality, "YUY2_CAPTURE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert YUY2 to DICOM");
                throw;
            }
        }

        /// <summary>
        /// Convert BitmapSource (from WPF/WebRTC) to DICOM
        /// </summary>
        public async Task<string> ConvertBitmapSourceToDicomAsync(
            BitmapSource bitmap,
            PatientInfo patientInfo,
            string modality = MODALITY_PHOTO)
        {
            _logger.LogInformation("Converting BitmapSource ({Width}x{Height}) to DICOM for patient {PatientId}", 
                bitmap.PixelWidth, bitmap.PixelHeight, patientInfo.PatientId);

            try
            {
                // Convert BitmapSource to RGB24 byte array
                var rgbData = ConvertBitmapSourceToRGB24(bitmap);
                
                // Create DICOM with RGB pixel data
                return await CreateDicomFromRGBAsync(
                    rgbData, 
                    bitmap.PixelWidth, 
                    bitmap.PixelHeight, 
                    patientInfo, 
                    modality, 
                    "WEBRTC_CAPTURE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert BitmapSource to DICOM");
                throw;
            }
        }

        /// <summary>
        /// Convert JPEG byte array to DICOM (legacy support)
        /// </summary>
        public async Task<string> ConvertJpegToDicomAsync(
            byte[] jpegData,
            PatientInfo patientInfo,
            string modality = MODALITY_PHOTO)
        {
            _logger.LogInformation("Converting JPEG ({Size} bytes) to DICOM for patient {PatientId}", 
                jpegData.Length, patientInfo.PatientId);

            try
            {
                // For JPEG, we have two options:
                // 1. Store as-is with JPEG transfer syntax (more efficient)
                // 2. Decompress to RGB (better compatibility)
                
                // Option 2: Decompress for maximum compatibility
                using var image = SixLabors.ImageSharp.Image.Load<Rgb24>(jpegData);
                var rgbData = ExtractRGB24FromImage(image);
                
                return await CreateDicomFromRGBAsync(
                    rgbData, 
                    image.Width, 
                    image.Height, 
                    patientInfo, 
                    modality, 
                    "JPEG_CAPTURE");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert JPEG to DICOM");
                throw;
            }
        }

        /// <summary>
        /// Create high-resolution snapshot DICOM with enhanced metadata
        /// </summary>
        public async Task<string> CreateHighResSnapshotDicomAsync(
            byte[] frameData,
            int width,
            int height,
            FrameFormat format,
            PatientInfo patientInfo,
            SnapshotMetadata metadata)
        {
            _logger.LogInformation("Creating high-resolution snapshot DICOM: {Width}x{Height} {Format}", 
                width, height, format);

            try
            {
                byte[] rgbData;
                
                // Convert based on input format
                switch (format)
                {
                    case FrameFormat.YUY2:
                        rgbData = YUY2Converter.ConvertToRGB24(frameData, width, height);
                        break;
                        
                    case FrameFormat.RGB24:
                        rgbData = frameData;
                        break;
                        
                    case FrameFormat.JPEG:
                        using (var image = SixLabors.ImageSharp.Image.Load<Rgb24>(frameData))
                        {
                            rgbData = ExtractRGB24FromImage(image);
                            width = image.Width;
                            height = image.Height;
                        }
                        break;
                        
                    default:
                        throw new ArgumentException($"Unsupported frame format: {format}");
                }

                // Create DICOM with enhanced metadata
                return await CreateDicomFromRGBAsync(
                    rgbData, 
                    width, 
                    height, 
                    patientInfo, 
                    metadata.Modality ?? MODALITY_OTHER, 
                    "HIGH_RES_SNAPSHOT",
                    metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create high-resolution snapshot DICOM");
                throw;
            }
        }

        /// <summary>
        /// Core method to create DICOM from RGB24 pixel data
        /// </summary>
        private async Task<string> CreateDicomFromRGBAsync(
            byte[] rgbData,
            int width,
            int height,
            PatientInfo patientInfo,
            string modality,
            string captureType,
            SnapshotMetadata? metadata = null)
        {
            try
            {
                // Create DICOM dataset
                var dataset = new DicomDataset();
                
                // Patient Module (Type 2)
                dataset.AddOrUpdate(DicomTag.PatientName, patientInfo.GetDicomName());
                dataset.AddOrUpdate(DicomTag.PatientID, patientInfo.PatientId ?? "");
                dataset.AddOrUpdate(DicomTag.PatientBirthDate, patientInfo.BirthDate?.ToString("yyyyMMdd") ?? "");
                dataset.AddOrUpdate(DicomTag.PatientSex, patientInfo.Gender ?? "O");
                
                // General Study Module
                var studyInstanceUID = !string.IsNullOrEmpty(patientInfo.StudyInstanceUID) 
                    ? patientInfo.StudyInstanceUID 
                    : DicomUID.Generate().UID;
                dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUID);
                dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.ReferringPhysicianName, "");
                dataset.AddOrUpdate(DicomTag.StudyID, DateTime.Now.ToString("yyyyMMddHHmmss"));
                dataset.AddOrUpdate(DicomTag.AccessionNumber, patientInfo.AccessionNumber ?? "");
                dataset.AddOrUpdate(DicomTag.StudyDescription, patientInfo.StudyDescription ?? "SmartBoxNext Capture");
                
                // General Series Module
                dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate());
                dataset.AddOrUpdate(DicomTag.SeriesNumber, "1");
                dataset.AddOrUpdate(DicomTag.Modality, modality);
                dataset.AddOrUpdate(DicomTag.SeriesDescription, $"SmartBoxNext {captureType}");
                dataset.AddOrUpdate(DicomTag.SeriesDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.SeriesTime, DateTime.Now.ToString("HHmmss"));
                
                // General Equipment Module
                dataset.AddOrUpdate(DicomTag.Manufacturer, "CIRSS Medical Systems");
                dataset.AddOrUpdate(DicomTag.InstitutionName, patientInfo.Institution ?? "");
                dataset.AddOrUpdate(DicomTag.StationName, Environment.MachineName);
                dataset.AddOrUpdate(DicomTag.ManufacturerModelName, "SmartBoxNext");
                dataset.AddOrUpdate(DicomTag.SoftwareVersions, "2.0.0");
                
                // Enhanced metadata for snapshots
                if (metadata != null)
                {
                    if (!string.IsNullOrEmpty(metadata.DeviceSerialNumber))
                        dataset.AddOrUpdate(DicomTag.DeviceSerialNumber, metadata.DeviceSerialNumber);
                    
                    if (metadata.ExposureTime.HasValue)
                        dataset.AddOrUpdate(DicomTag.ExposureTime, metadata.ExposureTime.Value.ToString());
                    
                    if (!string.IsNullOrEmpty(metadata.Comments))
                        dataset.AddOrUpdate(DicomTag.ImageComments, metadata.Comments);
                }
                
                // SC Equipment Module
                dataset.AddOrUpdate(DicomTag.ConversionType, "WSD"); // Workstation
                
                // General Image Module
                dataset.AddOrUpdate(DicomTag.InstanceNumber, "1");
                dataset.AddOrUpdate(DicomTag.PatientOrientation, "");
                dataset.AddOrUpdate(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
                
                // Image Pixel Module - RGB24 uncompressed
                dataset.AddOrUpdate(DicomTag.Columns, (ushort)width);
                dataset.AddOrUpdate(DicomTag.Rows, (ushort)height);
                dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
                dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
                dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
                dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
                dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
                dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, "RGB");
                dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);
                
                // Add pixel data
                var buffer = new MemoryByteBuffer(rgbData);
                dataset.AddOrUpdate(DicomTag.PixelData, buffer);
                
                // SOP Common Module
                dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUID.Generate());
                
                // Create DICOM file
                var file = new DicomFile(dataset);
                
                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"{patientInfo.PatientId}_{captureType}_{timestamp}.dcm";
                var filePath = Path.Combine(_config.Storage.DicomPath, filename);
                
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                
                // Save file
                await file.SaveAsync(filePath);
                
                _logger.LogInformation("DICOM file saved: {Path} ({Width}x{Height}, {Size} KB)", 
                    filePath, width, height, new FileInfo(filePath).Length / 1024);
                
                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DICOM from RGB data");
                throw;
            }
        }

        /// <summary>
        /// Convert BitmapSource to RGB24 byte array
        /// </summary>
        private byte[] ConvertBitmapSourceToRGB24(BitmapSource bitmap)
        {
            // Convert to Bgr24 format for consistent processing
            var convertedBitmap = new FormatConvertedBitmap(bitmap, System.Windows.Media.PixelFormats.Bgr24, null, 0);
            
            var width = convertedBitmap.PixelWidth;
            var height = convertedBitmap.PixelHeight;
            var stride = width * 3; // 3 bytes per pixel for BGR24
            
            var bgrData = new byte[height * stride];
            convertedBitmap.CopyPixels(bgrData, stride, 0);
            
            // Convert BGR to RGB
            var rgbData = new byte[bgrData.Length];
            for (int i = 0; i < bgrData.Length; i += 3)
            {
                rgbData[i] = bgrData[i + 2];     // R = B
                rgbData[i + 1] = bgrData[i + 1]; // G = G
                rgbData[i + 2] = bgrData[i];     // B = R
            }
            
            return rgbData;
        }

        /// <summary>
        /// Extract RGB24 data from ImageSharp image
        /// </summary>
        private byte[] ExtractRGB24FromImage(SixLabors.ImageSharp.Image<Rgb24> image)
        {
            var rgbData = new byte[image.Width * image.Height * 3];
            var index = 0;
            
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var pixel = image[x, y];
                    rgbData[index++] = pixel.R;
                    rgbData[index++] = pixel.G;
                    rgbData[index++] = pixel.B;
                }
            }
            
            return rgbData;
        }

        /// <summary>
        /// Get conversion performance metrics
        /// </summary>
        public ConversionMetrics GetPerformanceMetrics(int width, int height, FrameFormat format)
        {
            var pixelCount = width * height;
            var inputSize = format switch
            {
                FrameFormat.YUY2 => pixelCount * 2,
                FrameFormat.RGB24 => pixelCount * 3,
                FrameFormat.JPEG => pixelCount * 1, // Estimate (highly variable)
                _ => pixelCount * 3
            };
            
            var rgbSize = pixelCount * 3;
            var dicomOverhead = 1024; // Approximate DICOM header size
            var estimatedDicomSize = rgbSize + dicomOverhead;
            
            // Estimate conversion time based on format and resolution
            var baseTimeMs = format switch
            {
                FrameFormat.YUY2 => 8.0,    // Most efficient
                FrameFormat.RGB24 => 2.0,   // Direct copy
                FrameFormat.JPEG => 15.0,   // Requires decompression
                _ => 10.0
            };
            
            var scaleFactor = (double)pixelCount / (1920 * 1080);
            var estimatedTimeMs = baseTimeMs * scaleFactor;
            
            return new ConversionMetrics
            {
                Width = width,
                Height = height,
                PixelCount = pixelCount,
                InputFormat = format,
                InputSizeBytes = inputSize,
                RGB24SizeBytes = rgbSize,
                EstimatedDicomSizeBytes = estimatedDicomSize,
                EstimatedConversionTimeMs = estimatedTimeMs,
                MaxFPSAtThisResolution = estimatedTimeMs > 0 ? 1000.0 / estimatedTimeMs : 0
            };
        }
    }

    /// <summary>
    /// Frame format enumeration
    /// </summary>
    public enum FrameFormat
    {
        YUY2,
        RGB24,
        JPEG,
        BGRA32
    }

    /// <summary>
    /// Enhanced snapshot metadata
    /// </summary>
    public class SnapshotMetadata
    {
        public string? Modality { get; set; }
        public string? DeviceSerialNumber { get; set; }
        public double? ExposureTime { get; set; }
        public string? Comments { get; set; }
        public DateTime CaptureTime { get; set; } = DateTime.UtcNow;
        public string? InputSource { get; set; } // "Yuan_SDI", "Yuan_HDMI", "WebRTC", etc.
    }

    /// <summary>
    /// Conversion performance metrics
    /// </summary>
    public class ConversionMetrics
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int PixelCount { get; set; }
        public FrameFormat InputFormat { get; set; }
        public int InputSizeBytes { get; set; }
        public int RGB24SizeBytes { get; set; }
        public int EstimatedDicomSizeBytes { get; set; }
        public double EstimatedConversionTimeMs { get; set; }
        public double MaxFPSAtThisResolution { get; set; }

        public override string ToString()
        {
            return $"{Width}x{Height} {InputFormat}: ~{EstimatedConversionTimeMs:F1}ms conversion, " +
                   $"max {MaxFPSAtThisResolution:F0} FPS, " +
                   $"DICOM size ~{EstimatedDicomSizeBytes / 1024:F0} KB";
        }
    }
}