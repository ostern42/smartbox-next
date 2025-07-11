using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Imaging.Codec;
using FellowOakDicom.IO.Buffer;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SmartBoxNext.Services
{
    public class DicomService
    {
        private readonly ILogger _logger;

        public DicomService(ILogger logger)
        {
            _logger = logger;
            
            // Initialize fo-dicom
            // Note: ImageSharpImageManager is handled automatically in newer fo-dicom versions
        }

        public async Task<DicomFile> CreateDicomFromImageAsync(string imagePath, PatientInfo patient, string modality = "XC")
        {
            try
            {
                _logger.LogInformation($"Creating DICOM from image: {imagePath}");

                var file = new DicomFile();
                var dataset = file.Dataset;

                // File Meta Information
                file.FileMetaInfo.TransferSyntax = DicomTransferSyntax.ExplicitVRLittleEndian;
                file.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.SecondaryCaptureImageStorage;
                file.FileMetaInfo.MediaStorageSOPInstanceUID = DicomUID.Generate();

                // Patient Module
                dataset.AddOrUpdate(DicomTag.PatientName, patient.GetDicomName());
                dataset.AddOrUpdate(DicomTag.PatientID, patient.PatientId ?? "");
                dataset.AddOrUpdate(DicomTag.PatientBirthDate, patient.BirthDate?.ToString("yyyyMMdd") ?? "");
                dataset.AddOrUpdate(DicomTag.PatientSex, patient.Gender ?? "O");

                // Study Module
                var studyUid = patient.StudyInstanceUID ?? DicomUID.Generate().UID;
                dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyUid);
                dataset.AddOrUpdate(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.StudyDescription, patient.StudyDescription ?? "SmartBox Capture");
                dataset.AddOrUpdate(DicomTag.AccessionNumber, patient.AccessionNumber ?? "");

                // Series Module
                dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUID.Generate());
                dataset.AddOrUpdate(DicomTag.SeriesNumber, "1");
                dataset.AddOrUpdate(DicomTag.Modality, modality);
                dataset.AddOrUpdate(DicomTag.SeriesDescription, "External Camera Images");

                // General Equipment Module
                dataset.AddOrUpdate(DicomTag.Manufacturer, "CIRSS Medical Systems");
                dataset.AddOrUpdate(DicomTag.ManufacturerModelName, "SmartBox Next");
                dataset.AddOrUpdate(DicomTag.SoftwareVersions, "2.0.0");

                // SC Equipment Module
                dataset.AddOrUpdate(DicomTag.ConversionType, "WSD"); // Workstation
                dataset.AddOrUpdate(DicomTag.SecondaryCaptureDeviceID, "SMARTBOX");
                dataset.AddOrUpdate(DicomTag.SecondaryCaptureDeviceManufacturer, "CIRSS");
                dataset.AddOrUpdate(DicomTag.SecondaryCaptureDeviceManufacturerModelName, "SmartBox Next");
                dataset.AddOrUpdate(DicomTag.SecondaryCaptureDeviceSoftwareVersions, "2.0.0");

                // General Image Module
                dataset.AddOrUpdate(DicomTag.InstanceNumber, "1");
                dataset.AddOrUpdate(DicomTag.PatientOrientation, "");
                dataset.AddOrUpdate(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.AddOrUpdate(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));
                dataset.AddOrUpdate(DicomTag.ImageType, "ORIGINAL\\PRIMARY");

                // Image Pixel Module
                dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
                dataset.AddOrUpdate(DicomTag.SOPInstanceUID, file.FileMetaInfo.MediaStorageSOPInstanceUID);

                // Load and add the image
                await AddImageToDatasetAsync(dataset, imagePath);

                _logger.LogInformation($"DICOM file created successfully for patient {patient.PatientId}");
                return file;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DICOM file");
                throw;
            }
        }

        private async Task AddImageToDatasetAsync(DicomDataset dataset, string imagePath)
        {
            using var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgb24>(imagePath);
            
            // Set image attributes
            dataset.AddOrUpdate(DicomTag.Columns, (ushort)image.Width);
            dataset.AddOrUpdate(DicomTag.Rows, (ushort)image.Height);
            dataset.AddOrUpdate(DicomTag.BitsAllocated, (ushort)8);
            dataset.AddOrUpdate(DicomTag.BitsStored, (ushort)8);
            dataset.AddOrUpdate(DicomTag.HighBit, (ushort)7);
            dataset.AddOrUpdate(DicomTag.PixelRepresentation, (ushort)0);
            dataset.AddOrUpdate(DicomTag.SamplesPerPixel, (ushort)3);
            dataset.AddOrUpdate(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.AddOrUpdate(DicomTag.PlanarConfiguration, (ushort)0);

            // Extract pixel data
            var pixelData = new byte[image.Width * image.Height * 3];
            var index = 0;
            
            // Process pixel data
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        var pixel = row[x];
                        pixelData[index++] = pixel.R;
                        pixelData[index++] = pixel.G;
                        pixelData[index++] = pixel.B;
                    }
                }
            });

            var buffer = new MemoryByteBuffer(pixelData);
            var pixelDataTag = DicomTag.PixelData;
            dataset.AddOrUpdate(new DicomOtherWord(pixelDataTag, buffer));
        }

        public async Task<string> SaveDicomFileAsync(DicomFile dicomFile, string originalImagePath)
        {
            var dicomPath = Path.ChangeExtension(originalImagePath, ".dcm");
            var dicomDir = Path.Combine(Path.GetDirectoryName(originalImagePath) ?? "", "DICOM");
            
            if (!Directory.Exists(dicomDir))
            {
                Directory.CreateDirectory(dicomDir);
            }

            var finalPath = Path.Combine(dicomDir, Path.GetFileName(dicomPath));
            await dicomFile.SaveAsync(finalPath);
            
            _logger.LogInformation($"DICOM file saved to: {finalPath}");
            return finalPath;
        }
    }
}