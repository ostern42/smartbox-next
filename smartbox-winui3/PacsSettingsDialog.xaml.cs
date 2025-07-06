using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace SmartBoxNext
{
    public sealed partial class PacsSettingsDialog : ContentDialog
    {
        private PacsSettings _settings = new PacsSettings();

        public PacsSettingsDialog()
        {
            this.InitializeComponent();
            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            _settings = await PacsSettings.LoadAsync();
            
            AeTitleBox.Text = _settings.AeTitle;
            ServerAeTitleBox.Text = _settings.ServerAeTitle;
            ServerHostBox.Text = _settings.ServerHost;
            ServerPortBox.Value = _settings.ServerPort;
            LocalPortBox.Value = _settings.LocalPort;
            UseTlsBox.IsChecked = _settings.UseTls;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Save settings
            _settings.AeTitle = AeTitleBox.Text;
            _settings.ServerAeTitle = ServerAeTitleBox.Text;
            _settings.ServerHost = ServerHostBox.Text;
            _settings.ServerPort = (int)ServerPortBox.Value;
            _settings.LocalPort = (int)LocalPortBox.Value;
            _settings.UseTls = UseTlsBox.IsChecked ?? false;

            try
            {
                await _settings.SaveAsync();
                ShowStatus("Settings saved successfully", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowStatus($"Failed to save settings: {ex.Message}", InfoBarSeverity.Error);
                args.Cancel = true;
            }
        }

        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true; // Don't close dialog
            
            ShowStatus("Testing connection...", InfoBarSeverity.Informational);

            try
            {
                var client = DicomClientFactory.Create(
                    _settings.ServerHost,
                    _settings.ServerPort,
                    _settings.UseTls,
                    _settings.AeTitle,
                    _settings.ServerAeTitle);

                // Test with C-ECHO
                var request = new DicomCEchoRequest();
                bool success = false;
                string message = "";

                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        success = true;
                        message = "Connection successful!";
                    }
                    else
                    {
                        message = $"Connection failed: {response.Status}";
                    }
                };

                await client.AddRequestAsync(request);
                await client.SendAsync();

                ShowStatus(message, success ? InfoBarSeverity.Success : InfoBarSeverity.Error);
            }
            catch (Exception ex)
            {
                ShowStatus($"Connection failed: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private void ShowStatus(string message, InfoBarSeverity severity)
        {
            StatusBar.Message = message;
            StatusBar.Severity = severity;
            StatusBar.IsOpen = true;
        }
    }
}