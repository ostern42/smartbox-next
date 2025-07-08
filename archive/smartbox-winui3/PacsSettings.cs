using System;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace SmartBoxNext
{
    public class PacsSettings
    {
        public string AeTitle { get; set; } = "SMARTBOX";
        public string ServerAeTitle { get; set; } = "PACS";
        public string ServerHost { get; set; } = "localhost";
        public int ServerPort { get; set; } = 104;
        public int LocalPort { get; set; } = 0; // 0 = auto-select
        public bool UseTls { get; set; } = false;

        private static readonly string SettingsFileName = "pacs_settings.json";

        public static async Task<PacsSettings> LoadAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.GetFileAsync(SettingsFileName);
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<PacsSettings>(json) ?? new PacsSettings();
            }
            catch
            {
                // Return default settings if file doesn't exist or can't be read
                return new PacsSettings();
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(SettingsFileName, CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save PACS settings: {ex.Message}", ex);
            }
        }
    }
}