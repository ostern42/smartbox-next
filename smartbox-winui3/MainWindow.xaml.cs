using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Text.Json;
using System.Collections.Generic;

namespace SmartBoxNext
{
    public sealed partial class MainWindow : Window
    {
        private WebServer? _webServer;
        private AppConfig _config = new AppConfig();
        private Logger _logger = Logger.Instance;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "SmartBox Next - Medical Imaging System";
            
            // Set window size and position
            var bounds = Microsoft.UI.Windowing.DisplayArea.Primary.WorkArea;
            var width = Math.Min(1400, bounds.Width - 100);
            var height = Math.Min(900, bounds.Height - 100);
            
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));
            this.AppWindow.Move(new Windows.Graphics.PointInt32(
                (bounds.Width - width) / 2,
                (bounds.Height - height) / 2
            ));
            
            // Show the window
            this.Activate();
            
            // Initialize web server and WebView2
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load configuration
                _config = await AppConfig.LoadAsync();
                
                // Log startup info
                _logger.LogInfo($"App directory: {AppDomain.CurrentDomain.BaseDirectory}");
                _logger.LogInfo($"Config loaded from: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")}");
                _logger.LogInfo($"Videos will be saved to: {_config.GetFullPath(_config.Storage.VideosPath)}");
                _logger.LogInfo($"Photos will be saved to: {_config.GetFullPath(_config.Storage.PhotosPath)}");
                
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
                        
                    case "requestConfig":
                        await HandleRequestConfig();
                        break;
                        
                    case "saveConfig":
                        await HandleSaveConfig(message.data);
                        break;
                        
                    case "testPacs":
                        await HandleTestPacs(message.data);
                        break;
                        
                    case "browseFolder":
                        await HandleBrowseFolder(message.data);
                        break;
                        
                    case "openLogs":
                        await HandleOpenLogs();
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
                
                await LogToWebApp($"Video saved: {filePath} ({duration}s, {size / 1024 / 1024}MB)");
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
            // Settings are now handled in HTML/JavaScript
            // The web app will show the settings modal
            await Task.CompletedTask;
        }

        private async Task HandleCameraAnalysis(JsonElement data)
        {
            var analysisText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await LogToWebApp($"Camera Analysis:\n{analysisText}");
        }

        private async Task HandleRequestConfig()
        {
            var configMessage = new
            {
                action = "configLoaded",
                data = _config
            };
            
            var json = JsonSerializer.Serialize(configMessage);
            await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
        }

        private async Task HandleSaveConfig(JsonElement data)
        {
            try
            {
                // Update config from JSON data
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var newConfig = JsonSerializer.Deserialize<AppConfig>(data.GetRawText(), options);
                
                if (newConfig != null)
                {
                    _config = newConfig;
                    await _config.SaveAsync();
                    
                    // Notify web app of successful save
                    var response = new { action = "configSaved", data = new { success = true } };
                    var json = JsonSerializer.Serialize(response);
                    await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
                    
                    await LogToWebApp("Configuration saved successfully");
                }
            }
            catch (Exception ex)
            {
                await LogToWebApp($"Error saving configuration: {ex.Message}", "error");
            }
        }

        private async Task HandleTestPacs(JsonElement data)
        {
            try
            {
                var serverAe = data.GetProperty("ServerAeTitle").GetString();
                var serverIp = data.GetProperty("ServerIp").GetString();
                var serverPort = data.GetProperty("ServerPort").GetInt32();
                var localAe = data.GetProperty("LocalAeTitle").GetString();
                var localPort = data.GetProperty("LocalPort").GetInt32();
                
                // Create a simple C-ECHO test
                var pacsSender = new PacsSender();
                bool success = await pacsSender.TestConnection(serverAe, serverIp, serverPort, localAe);
                
                var response = new
                {
                    action = "pacsTestResult",
                    data = new
                    {
                        success = success,
                        error = success ? null : "Connection failed - check server settings"
                    }
                };
                
                var json = JsonSerializer.Serialize(response);
                await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
            }
            catch (Exception ex)
            {
                var response = new
                {
                    action = "pacsTestResult",
                    data = new
                    {
                        success = false,
                        error = ex.Message
                    }
                };
                
                var json = JsonSerializer.Serialize(response);
                await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
            }
        }

        private async Task HandleBrowseFolder(JsonElement data)
        {
            try
            {
                var inputId = data.GetProperty("inputId").GetString();
                var currentPath = data.GetProperty("currentPath").GetString();
                
                // Use Windows Storage Pickers
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                folderPicker.FileTypeFilter.Add("*");
                
                // Get the window handle
                var window = this;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                
                var folder = await folderPicker.PickSingleFolderAsync();
                
                if (folder != null)
                {
                    var response = new
                    {
                        action = "folderSelected",
                        data = new
                        {
                            inputId = inputId,
                            path = folder.Path
                        }
                    };
                    
                    var json = JsonSerializer.Serialize(response);
                    await WebView.CoreWebView2.ExecuteScriptAsync($"window.postMessage({json}, '*');");
                }
            }
            catch (Exception ex)
            {
                await LogToWebApp($"Error browsing folder: {ex.Message}", "error");
            }
        }

        private async Task HandleOpenLogs()
        {
            try
            {
                var logsPath = _logger.GetLogDirectory();
                await LogToWebApp($"Opening logs folder: {logsPath}");
                
                // Open the logs folder in Windows Explorer
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = logsPath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                await LogToWebApp($"Error opening logs folder: {ex.Message}", "error");
            }
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
            // Log to file
            switch (level.ToLower())
            {
                case "error":
                    _logger.LogError(message);
                    break;
                case "warn":
                case "warning":
                    _logger.LogWarning(message);
                    break;
                case "debug":
                    _logger.LogDebug(message);
                    break;
                default:
                    _logger.LogInfo(message);
                    break;
            }

            // Log to web app
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