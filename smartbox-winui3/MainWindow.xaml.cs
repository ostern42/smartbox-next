using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using System.Text.Json;
using System.Collections.Generic;

namespace SmartBoxNext
{
    public sealed partial class MainWindow : Window
    {
        private WebServer? _webServer;
        private AppConfig _config = new AppConfig();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "SmartBox Next - Medical Imaging System";
            
            // Initialize web server and WebView2
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load configuration
                _config = await AppConfig.LoadAsync();
                
                // Start web server
                var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                _webServer = new WebServer(wwwrootPath, 5000);
                await _webServer.StartAsync();

                // Initialize WebView2
                await WebView.EnsureCoreWebView2Async();
                
                // Set up WebView2 settings
                var settings = WebView.CoreWebView2.Settings;
                settings.IsScriptEnabled = true;
                settings.IsWebMessageEnabled = true;
                settings.AreDefaultScriptDialogsEnabled = true;
                
                // Set up message handling
                WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                WebView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                
                // Navigate to the local web server
                WebView.CoreWebView2.Navigate(_webServer.GetServerUrl());
            }
            catch (Exception ex)
            {
                await ShowErrorDialog("Initialization Error", $"Failed to initialize application: {ex.Message}");
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // Hide loading panel
                LoadingPanel.Visibility = Visibility.Collapsed;
                
                // Send initial configuration to web app
                _ = SendConfigurationToWebApp();
            }
            else
            {
                _ = ShowErrorDialog("Navigation Error", "Failed to load the application interface.");
            }
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                var message = JsonSerializer.Deserialize<WebMessage>(messageJson);
                
                if (message == null) return;

                switch (message.action)
                {
                    case "webcamInitialized":
                        await HandleWebcamInitialized(message.data);
                        break;
                        
                    case "photoCaptured":
                        await HandlePhotoCaptured(message.data);
                        break;
                        
                    case "videoRecorded":
                        await HandleVideoRecorded(message.data);
                        break;
                        
                    case "exportDicom":
                        await HandleExportDicom(message.data);
                        break;
                        
                    case "openSettings":
                        await HandleOpenSettings();
                        break;
                        
                    case "cameraAnalysis":
                        await HandleCameraAnalysis(message.data);
                        break;
                        
                    default:
                        Console.WriteLine($"Unknown message action: {message.action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling web message: {ex.Message}");
            }
        }

        private async Task HandleWebcamInitialized(JsonElement data)
        {
            var width = data.GetProperty("width").GetInt32();
            var height = data.GetProperty("height").GetInt32();
            var frameRate = data.GetProperty("frameRate").GetDouble();
            
            await LogToWebApp($"Webcam initialized: {width}x{height} @ {frameRate}fps");
        }

        private async Task HandlePhotoCaptured(JsonElement data)
        {
            try
            {
                var imageData = data.GetProperty("imageData").GetString();
                var timestamp = data.GetProperty("timestamp").GetString();
                var patient = data.GetProperty("patient");
                
                if (imageData == null) return;
                
                // Convert base64 to bytes
                var imageBytes = Convert.FromBase64String(imageData);
                
                // Save to configured photo directory
                var photoDir = _config.GetFullPath(_config.Storage.PhotosPath);
                Directory.CreateDirectory(photoDir);
                
                var fileName = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(photoDir, fileName);
                
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                await LogToWebApp($"Photo saved: {fileName}");
                
                // TODO: Create DICOM file if needed
            }
            catch (Exception ex)
            {
                await LogToWebApp($"Error saving photo: {ex.Message}", "error");
            }
        }

        private async Task HandleVideoRecorded(JsonElement data)
        {
            try
            {
                var videoData = data.GetProperty("videoData").GetString();
                var size = data.GetProperty("size").GetInt64();
                var duration = data.GetProperty("duration").GetInt32();
                
                if (videoData == null) return;
                
                // Convert base64 to bytes
                var videoBytes = Convert.FromBase64String(videoData);
                
                // Save to configured video directory
                var videoDir = _config.GetFullPath(_config.Storage.VideosPath);
                Directory.CreateDirectory(videoDir);
                
                var fileName = $"VID_{DateTime.Now:yyyyMMdd_HHmmss}.webm";
                var filePath = Path.Combine(videoDir, fileName);
                
                await File.WriteAllBytesAsync(filePath, videoBytes);
                
                await LogToWebApp($"Video saved: {fileName} ({duration}s, {size / 1024 / 1024}MB)");
            }
            catch (Exception ex)
            {
                await LogToWebApp($"Error saving video: {ex.Message}", "error");
            }
        }

        private async Task HandleExportDicom(JsonElement data)
        {
            await LogToWebApp("DICOM export not yet implemented");
            // TODO: Implement DICOM export using fo-dicom
        }

        private async Task HandleOpenSettings()
        {
            var settingsWindow = new SettingsWindow(_config);
            settingsWindow.Activate();
            
            // TODO: Implement proper event handling for settings changes
            await Task.CompletedTask;
        }

        private async Task HandleCameraAnalysis(JsonElement data)
        {
            var analysisText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await LogToWebApp($"Camera Analysis:\n{analysisText}");
        }

        private async Task SendConfigurationToWebApp()
        {
            var configMessage = new
            {
                action = "updateConfig",
                data = new
                {
                    autoStartWebcam = _config.Application.AutoStartCapture,
                    defaultFrameRate = _config.Video.PreferredFps,
                    defaultResolution = $"{_config.Video.PreferredWidth}x{_config.Video.PreferredHeight}",
                    photoFormat = "jpeg",
                    videoFormat = _config.Video.VideoFormat
                }
            };
            
            var json = JsonSerializer.Serialize(configMessage);
            await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
        }

        private async Task LogToWebApp(string message, string level = "info")
        {
            var logMessage = new
            {
                action = "log",
                data = new
                {
                    message = message,
                    level = level
                }
            };
            
            var json = JsonSerializer.Serialize(logMessage);
            await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
        }

        private async Task ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            
            await dialog.ShowAsync();
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Clean up
            _webServer?.StopAsync().Wait();
        }
    }

    // Message structure for WebView2 communication
    public class WebMessage
    {
        public string action { get; set; } = "";
        public JsonElement data { get; set; }
        public string timestamp { get; set; } = "";
    }
}