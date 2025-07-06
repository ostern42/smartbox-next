using System;
using System.IO;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Windows.Storage;

namespace SmartBoxNext
{
    public class PacsSender
    {
        public static async Task<bool> SendDicomFileAsync(StorageFile dicomFile, PacsSettings settings)
        {
            try
            {
                // Load DICOM file
                DicomFile dicom;
                using (var stream = await dicomFile.OpenStreamForReadAsync())
                {
                    dicom = await DicomFile.OpenAsync(stream);
                }

                // Create C-STORE request
                var request = new DicomCStoreRequest(dicom);
                bool success = false;
                string responseMessage = "";

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        success = true;
                        responseMessage = "Image sent successfully";
                    }
                    else
                    {
                        responseMessage = $"Failed: {response.Status}";
                    }
                };

                // Create client
                var client = DicomClientFactory.Create(
                    settings.ServerHost,
                    settings.ServerPort,
                    settings.UseTls,
                    settings.AeTitle,
                    settings.ServerAeTitle);

                // Add request and send
                await client.AddRequestAsync(request);
                await client.SendAsync();

                if (!success)
                {
                    throw new Exception(responseMessage);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to send DICOM to PACS: {ex.Message}", ex);
            }
        }

        public static async Task<bool> TestConnectionAsync(PacsSettings settings)
        {
            try
            {
                var client = DicomClientFactory.Create(
                    settings.ServerHost,
                    settings.ServerPort,
                    settings.UseTls,
                    settings.AeTitle,
                    settings.ServerAeTitle);

                // Test with C-ECHO
                var request = new DicomCEchoRequest();
                bool success = false;

                request.OnResponseReceived += (req, response) =>
                {
                    success = response.Status == DicomStatus.Success;
                };

                await client.AddRequestAsync(request);
                await client.SendAsync();

                return success;
            }
            catch
            {
                return false;
            }
        }
    }
}