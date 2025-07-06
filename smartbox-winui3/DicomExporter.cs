using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.IO.Buffer;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace SmartBoxNext
{
    public class DicomExporter
    {
        public static async Task<StorageFile> ExportToDicomAsync(
            StorageFile imageFile,
            string patientName,
            string patientId,
            DateTime? birthDate,
            string gender,
            string studyDescription,
            string accessionNumber)
        {
            try
            {
                // Create DICOM dataset
                var dataset = new DicomDataset();

                // Patient Module
                dataset.Add(DicomTag.PatientName, patientName ?? "Unknown^Patient");
                dataset.Add(DicomTag.PatientID, patientId ?? GeneratePatientId());
                
                if (birthDate.HasValue)
                {
                    dataset.Add(DicomTag.PatientBirthDate, birthDate.Value.ToString("yyyyMMdd"));
                }
                
                dataset.Add(DicomTag.PatientSex, ConvertGender(gender));

                // Study Module
                var studyInstanceUid = DicomUID.Generate();
                dataset.Add(DicomTag.StudyInstanceUID, studyInstanceUid);
                dataset.Add(DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.Add(DicomTag.StudyTime, DateTime.Now.ToString("HHmmss"));
                dataset.Add(DicomTag.StudyDescription, studyDescription ?? "Endoscopy Study");
                dataset.Add(DicomTag.AccessionNumber, accessionNumber ?? "");
                dataset.Add(DicomTag.StudyID, DateTime.Now.ToString("yyyyMMddHHmmss"));

                // Series Module
                var seriesInstanceUid = DicomUID.Generate();
                dataset.Add(DicomTag.SeriesInstanceUID, seriesInstanceUid);
                dataset.Add(DicomTag.SeriesNumber, "1");
                dataset.Add(DicomTag.SeriesDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.Add(DicomTag.SeriesTime, DateTime.Now.ToString("HHmmss"));
                dataset.Add(DicomTag.Modality, "ES"); // Endoscopy
                dataset.Add(DicomTag.SeriesDescription, "Endoscopic Images");

                // General Equipment Module
                dataset.Add(DicomTag.Manufacturer, "SmartBox Next");
                dataset.Add(DicomTag.ManufacturerModelName, "SmartBox-WinUI3");
                dataset.Add(DicomTag.SoftwareVersions, "1.0.0");
                dataset.Add(DicomTag.DeviceSerialNumber, Environment.MachineName);

                // SC Equipment Module (Secondary Capture)
                dataset.Add(DicomTag.ConversionType, "WSD"); // Workstation
                dataset.Add(DicomTag.SecondaryCaptureDeviceID, "SmartBox");
                dataset.Add(DicomTag.SecondaryCaptureDeviceManufacturer, "SmartBox Next");
                dataset.Add(DicomTag.SecondaryCaptureDeviceManufacturerModelName, "SmartBox-WinUI3");
                dataset.Add(DicomTag.SecondaryCaptureDeviceSoftwareVersions, "1.0.0");

                // Image Module
                dataset.Add(DicomTag.InstanceNumber, "1");
                dataset.Add(DicomTag.ImageType, "ORIGINAL\\PRIMARY");
                dataset.Add(DicomTag.ContentDate, DateTime.Now.ToString("yyyyMMdd"));
                dataset.Add(DicomTag.ContentTime, DateTime.Now.ToString("HHmmss"));
                dataset.Add(DicomTag.AcquisitionDateTime, DateTime.Now.ToString("yyyyMMddHHmmss"));

                // SOP Common Module
                var sopInstanceUid = DicomUID.Generate();
                dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
                dataset.Add(DicomTag.SOPInstanceUID, sopInstanceUid);
                dataset.Add(DicomTag.SpecificCharacterSet, "ISO_IR 100");

                // Load image and get pixel data
                using (var stream = await imageFile.OpenAsync(FileAccessMode.Read))
                {
                    var decoder = await BitmapDecoder.CreateAsync(stream);
                    var pixelDataProvider = await decoder.GetPixelDataAsync();
                    var pixels = pixelDataProvider.DetachPixelData();
                    
                    var width = decoder.PixelWidth;
                    var height = decoder.PixelHeight;

                    // Image Pixel Module
                    dataset.Add(DicomTag.SamplesPerPixel, 3);
                    dataset.Add(DicomTag.PhotometricInterpretation, "RGB");
                    dataset.Add(DicomTag.Rows, (ushort)height);
                    dataset.Add(DicomTag.Columns, (ushort)width);
                    dataset.Add(DicomTag.BitsAllocated, 8);
                    dataset.Add(DicomTag.BitsStored, 8);
                    dataset.Add(DicomTag.HighBit, 7);
                    dataset.Add(DicomTag.PixelRepresentation, 0);
                    dataset.Add(DicomTag.PlanarConfiguration, 0);

                    // Convert BGRA to RGB
                    var rgbPixels = ConvertBgraToRgb(pixels, width, height);
                    
                    // Add pixel data
                    var pixelDataBuffer = new MemoryByteBuffer(rgbPixels);
                    var dicomPixelData = DicomPixelData.Create(dataset, true);
                    dicomPixelData.AddFrame(pixelDataBuffer);
                }

                // Create DICOM file
                var dicomFile = new DicomFile(dataset);

                // Save to file
                var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                var dicomFolder = await myPictures.SaveFolder.CreateFolderAsync("SmartBoxNext\\DICOM", CreationCollisionOption.OpenIfExists);
                var fileName = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}_{sopInstanceUid.UID.Substring(sopInstanceUid.UID.LastIndexOf('.') + 1)}.dcm";
                var dicomStorageFile = await dicomFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);

                using (var fileStream = await dicomStorageFile.OpenStreamForWriteAsync())
                {
                    await dicomFile.SaveAsync(fileStream);
                }

                return dicomStorageFile;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export DICOM: {ex.Message}", ex);
            }
        }

        private static byte[] ConvertBgraToRgb(byte[] bgraPixels, uint width, uint height)
        {
            var rgbPixels = new byte[width * height * 3];
            var rgbIndex = 0;

            for (int i = 0; i < bgraPixels.Length; i += 4)
            {
                // BGRA to RGB
                rgbPixels[rgbIndex++] = bgraPixels[i + 2]; // R
                rgbPixels[rgbIndex++] = bgraPixels[i + 1]; // G
                rgbPixels[rgbIndex++] = bgraPixels[i];     // B
                // Skip alpha channel
            }

            return rgbPixels;
        }

        private static string ConvertGender(string gender)
        {
            return gender?.ToUpper() switch
            {
                "MALE" => "M",
                "FEMALE" => "F",
                "OTHER" => "O",
                _ => ""
            };
        }

        private static string GeneratePatientId()
        {
            return $"P{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}