using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartBoxNext
{
    /// <summary>
    /// Main window hosting the WebView2 control for the medical capture system
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private WebServer? _webServer;
        private AppConfig? _config;
        private DicomExporter? _dicomExporter;
        private PacsSender? _pacsSender;
        private QueueManager? _queueManager;
        private QueueProcessor? _queueProcessor;
        private bool _isInitialized = false;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<MainWindow>();
            
            // Set up keyboard shortcuts
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }
        
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("MainWindow loaded, starting initialization...");
            await InitializeApplication();
        }
        
        private async Task InitializeApplication()
        {
            try
            {
                UpdateStatus("Loading configuration...");
                
                // Load configuration
                _config = await LoadConfiguration();
                
                UpdateStatus("Starting web server...");
                
                // Start web server
                _webServer = new WebServer("wwwroot", _config.Application.WebServerPort);
                await _webServer.StartAsync();
                _logger.LogInformation("Web server started on port {Port}", _config.Application.WebServerPort);
                
                UpdateStatus("Initializing medical components...");
                
                // Initialize medical components
                _dicomExporter = new DicomExporter(_config);
                _pacsSender = new PacsSender(_config);
                _queueManager = new QueueManager(_config);
                _queueProcessor = new QueueProcessor(_config, _queueManager, _pacsSender);
                
                // Start queue processor
                _queueProcessor.Start();
                _logger.LogInformation("Queue processor started");
                
                UpdateStatus("Initializing WebView2...");
                
                // Initialize WebView2
                await InitializeWebView();
                
                _isInitialized = true;
                _logger.LogInformation("Application initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize application");
                ShowError($"Failed to initialize: {ex.Message}");
            }
        }
        
        private async Task<AppConfig> LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                
                if (!File.Exists(configPath))
                {
                    _logger.LogWarning("Config file not found, creating default configuration");
                    var defaultConfig = AppConfig.CreateDefault();
                    var json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                    await File.WriteAllTextAsync(configPath, json);
                    return defaultConfig;
                }
                
                var configJson = await File.ReadAllTextAsync(configPath);
                var config = JsonConvert.DeserializeObject<AppConfig>(configJson) ?? AppConfig.CreateDefault();
                
                _logger.LogInformation("Configuration loaded successfully");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration, using defaults");
                return AppConfig.CreateDefault();
            }
        }
        
        private async Task InitializeWebView()
        {
            try
            {
                // Set WebView2 user data folder
                var userDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Data");
                Directory.CreateDirectory(userDataFolder);
                
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await webView.EnsureCoreWebView2Async(env);
                
                // Configure WebView2
                ConfigureWebView();
                
                // Navigate to the application
                var url = $"http://localhost:{_config!.Application.WebServerPort}";
                webView.Source = new Uri(url);
                
                _logger.LogInformation("WebView2 initialized, navigating to {Url}", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize WebView2");
                throw;
            }
        }
        
        private void ConfigureWebView()
        {
            var settings = webView.CoreWebView2.Settings;
            
            // Security settings for medical application
            settings.IsScriptEnabled = true;
            settings.IsWebMessageEnabled = true;
            settings.IsStatusBarEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsGeneralAutofillEnabled = false;
            
            // Disable unnecessary features
            settings.AreDevToolsEnabled = _config!.Application.EnableDebugMode;
            settings.AreDefaultScriptDialogsEnabled = false;
            settings.IsZoomControlEnabled = false;
            
            // Set up message handling
            webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
            webView.CoreWebView2.WindowCloseRequested += WebView_WindowCloseRequested;
            
            // Handle navigation events
            webView.CoreWebView2.NavigationStarting += WebView_NavigationStarting;
            webView.CoreWebView2.DOMContentLoaded += WebView_DOMContentLoaded;
            
            // Handle permission requests (for camera access)
            webView.CoreWebView2.PermissionRequested += WebView_PermissionRequested;
            
            _logger.LogInformation("WebView2 configured with medical-grade security settings");
        }
        
        private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                _logger.LogDebug("Received message from WebView: {Message}", message);
                
                // Try to parse as JSON
                JObject? messageObj = null;
                try
                {
                    messageObj = JObject.Parse(message);
                }
                catch
                {
                    // If not JSON, treat as simple string command
                    await HandleSimpleCommand(message);
                    return;
                }
                
                // Handle JSON message
                var action = messageObj["action"]?.ToString();
                if (string.IsNullOrEmpty(action))
                {
                    _logger.LogWarning("Received message without action");
                    return;
                }
                
                await HandleAction(action, messageObj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling web message");
                await SendErrorToWebView($"Error: {ex.Message}");
            }
        }
        
        private async Task HandleSimpleCommand(string command)
        {
            _logger.LogInformation("Handling simple command: {Command}", command);
            
            switch (command.ToLower())
            {
                case "ping":
                    await SendMessageToWebView("pong");
                    break;
                    
                case "exit":
                case "close":
                    Close();
                    break;
                    
                default:
                    _logger.LogWarning("Unknown simple command: {Command}", command);
                    break;
            }
        }
        
        private async Task HandleAction(string action, JObject message)
        {
            _logger.LogInformation("Handling action: {Action}", action);
            
            switch (action.ToLower())
            {
                case "openlogs":
                    await OpenLogsFolder();
                    break;
                    
                case "opensettings":
                    await OpenSettings();
                    break;
                    
                case "savephoto":
                    await SavePhoto(message);
                    break;
                    
                case "savevideo":
                    await SaveVideo(message);
                    break;
                    
                case "exportdicom":
                    await ExportDicom(message);
                    break;
                    
                case "sendtopacs":
                    await SendToPacs(message);
                    break;
                    
                case "testwebview":
                    await TestWebView();
                    break;
                    
                case "updateconfig":
                    await UpdateConfiguration(message);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown action: {Action}", action);
                    await SendErrorToWebView($"Unknown action: {action}");
                    break;
            }
        }
        
        private async Task OpenLogsFolder()
        {
            try
            {
                var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logsPath);
                
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = logsPath,
                    UseShellExecute = true
                });
                
                await SendSuccessToWebView("Logs folder opened");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open logs folder");
                await SendErrorToWebView($"Failed to open logs: {ex.Message}");
            }
        }
        
        private async Task OpenSettings()
        {
            try
            {
                await SendMessageToWebView(new
                {
                    type = "showSettings",
                    config = _config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open settings");
                await SendErrorToWebView($"Failed to open settings: {ex.Message}");
            }
        }
        
        private async Task SavePhoto(JObject message)
        {
            try
            {
                var imageData = message["data"]?["imageData"]?.ToString();
                var patientInfo = message["data"]?["patientInfo"];
                
                if (string.IsNullOrEmpty(imageData))
                {
                    throw new ArgumentException("No image data provided");
                }
                
                // Remove data URL prefix if present
                if (imageData.StartsWith("data:image"))
                {
                    imageData = imageData.Substring(imageData.IndexOf(',') + 1);
                }
                
                var imageBytes = Convert.FromBase64String(imageData);
                
                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var patientId = patientInfo?["patientId"]?.ToString() ?? "Unknown";
                var filename = $"{patientId}_{timestamp}.jpg";
                
                var photoPath = Path.Combine(_config!.Storage.PhotosPath, filename);
                Directory.CreateDirectory(Path.GetDirectoryName(photoPath)!);
                
                await File.WriteAllBytesAsync(photoPath, imageBytes);
                
                _logger.LogInformation("Photo saved: {Path}", photoPath);
                await SendSuccessToWebView($"Photo saved: {filename}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save photo");
                await SendErrorToWebView($"Failed to save photo: {ex.Message}");
            }
        }
        
        private async Task SaveVideo(JObject message)
        {
            try
            {
                var videoData = message["data"]?["videoData"]?.ToString();
                var patientInfo = message["data"]?["patientInfo"];
                
                if (string.IsNullOrEmpty(videoData))
                {
                    throw new ArgumentException("No video data provided");
                }
                
                // Remove data URL prefix if present
                if (videoData.StartsWith("data:video"))
                {
                    videoData = videoData.Substring(videoData.IndexOf(',') + 1);
                }
                
                var videoBytes = Convert.FromBase64String(videoData);
                
                // Generate filename
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var patientId = patientInfo?["patientId"]?.ToString() ?? "Unknown";
                var filename = $"{patientId}_{timestamp}.webm";
                
                var videoPath = Path.Combine(_config!.Storage.VideosPath, filename);
                Directory.CreateDirectory(Path.GetDirectoryName(videoPath)!);
                
                await File.WriteAllBytesAsync(videoPath, videoBytes);
                
                _logger.LogInformation("Video saved: {Path}", videoPath);
                await SendSuccessToWebView($"Video saved: {filename}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save video");
                await SendErrorToWebView($"Failed to save video: {ex.Message}");
            }
        }
        
        private async Task ExportDicom(JObject message)
        {
            try
            {
                var imageData = message["data"]?["imageData"]?.ToString();
                var patientInfoJson = message["data"]?["patientInfo"];
                
                if (string.IsNullOrEmpty(imageData))
                {
                    throw new ArgumentException("No image data provided");
                }
                
                // Parse patient info
                var patientInfo = new PatientInfo
                {
                    PatientId = patientInfoJson?["patientId"]?.ToString(),
                    FirstName = patientInfoJson?["firstName"]?.ToString(),
                    LastName = patientInfoJson?["lastName"]?.ToString(),
                    Gender = patientInfoJson?["gender"]?.ToString(),
                    Institution = patientInfoJson?["institution"]?.ToString(),
                    StudyDescription = patientInfoJson?["studyDescription"]?.ToString()
                };
                
                // Parse birth date if provided
                var birthDateStr = patientInfoJson?["birthDate"]?.ToString();
                if (!string.IsNullOrEmpty(birthDateStr) && DateTime.TryParse(birthDateStr, out var birthDate))
                {
                    patientInfo.BirthDate = birthDate;
                }
                
                // Remove data URL prefix if present
                if (imageData.StartsWith("data:image"))
                {
                    imageData = imageData.Substring(imageData.IndexOf(',') + 1);
                }
                
                var imageBytes = Convert.FromBase64String(imageData);
                
                // Export as DICOM
                var dicomPath = await _dicomExporter!.ExportDicomAsync(imageBytes, patientInfo);
                
                // Add to PACS queue if configured
                if (!string.IsNullOrEmpty(_config!.Pacs.ServerHost))
                {
                    _queueManager!.Enqueue(dicomPath, patientInfo);
                    await SendSuccessToWebView($"DICOM exported and queued for PACS upload");
                }
                else
                {
                    await SendSuccessToWebView($"DICOM exported: {Path.GetFileName(dicomPath)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export DICOM");
                await SendErrorToWebView($"Failed to export DICOM: {ex.Message}");
            }
        }
        
        private async Task SendToPacs(JObject message)
        {
            try
            {
                // Get queue stats
                var stats = _queueManager!.GetStats();
                
                await SendMessageToWebView(new
                {
                    type = "queueStatus",
                    stats = stats,
                    message = $"Queue: {stats.Pending} pending, {stats.Processing} processing, {stats.Sent} sent"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue status");
                await SendErrorToWebView($"Failed to get queue status: {ex.Message}");
            }
        }
        
        private async Task TestWebView()
        {
            try
            {
                await SendMessageToWebView(new
                {
                    type = "test",
                    message = "WebView2 communication working!",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebView test failed");
                await SendErrorToWebView($"Test failed: {ex.Message}");
            }
        }
        
        private async Task UpdateConfiguration(JObject message)
        {
            try
            {
                var newConfig = message["data"]?.ToObject<AppConfig>();
                if (newConfig == null)
                {
                    throw new ArgumentException("Invalid configuration data");
                }
                
                // Validate configuration
                // TODO: Add validation logic
                
                // Save configuration
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var json = JsonConvert.SerializeObject(newConfig, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, json);
                
                _config = newConfig;
                
                _logger.LogInformation("Configuration updated successfully");
                await SendSuccessToWebView("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update configuration");
                await SendErrorToWebView($"Failed to save configuration: {ex.Message}");
            }
        }
        
        private async Task SendMessageToWebView(object message)
        {
            var json = JsonConvert.SerializeObject(message);
            await webView.CoreWebView2.ExecuteScriptAsync($"window.receiveMessage({json})");
        }
        
        private async Task SendSuccessToWebView(string message)
        {
            await SendMessageToWebView(new
            {
                type = "success",
                message = message,
                timestamp = DateTime.Now
            });
        }
        
        private async Task SendErrorToWebView(string message)
        {
            await SendMessageToWebView(new
            {
                type = "error",
                message = message,
                timestamp = DateTime.Now
            });
        }
        
        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _logger.LogInformation("Navigation starting: {Uri}", e.Uri);
        }
        
        private void WebView_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            _logger.LogInformation("DOM content loaded");
        }
        
        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                _logger.LogInformation("Navigation completed successfully");
                
                // Hide loading overlay
                loadingOverlay.Visibility = Visibility.Collapsed;
                webView.Visibility = Visibility.Visible;
                
                // Hide emergency exit button if configured
                if (_config?.Application.HideExitButton == true)
                {
                    emergencyExitButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _logger.LogError("Navigation failed: {Error}", e.WebErrorStatus);
                ShowError($"Failed to load application: {e.WebErrorStatus}");
            }
        }
        
        private void WebView_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            // Always allow camera and microphone for medical capture
            if (e.PermissionKind == CoreWebView2PermissionKind.Camera ||
                e.PermissionKind == CoreWebView2PermissionKind.Microphone)
            {
                e.State = CoreWebView2PermissionState.Allow;
                _logger.LogInformation("Permission granted: {Permission}", e.PermissionKind);
            }
            else
            {
                e.State = CoreWebView2PermissionState.Deny;
                _logger.LogWarning("Permission denied: {Permission}", e.PermissionKind);
            }
        }
        
        private void WebView_WindowCloseRequested(object? sender, object e)
        {
            _logger.LogInformation("Window close requested from WebView");
            Close();
        }
        
        private void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                statusText.Text = status;
            });
        }
        
        private void ShowError(string error)
        {
            Dispatcher.Invoke(() =>
            {
                errorText.Text = error;
                errorPanel.Visibility = Visibility.Visible;
            });
        }
        
        private async void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            errorPanel.Visibility = Visibility.Collapsed;
            await InitializeApplication();
        }
        
        private void EmergencyExit_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogWarning("Emergency exit requested");
            Close();
        }
        
        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Alt+F4 for emergency exit
            if (e.Key == System.Windows.Input.Key.F4 && 
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt)
            {
                _logger.LogWarning("Alt+F4 pressed, closing application");
                Close();
            }
            
            // F11 for fullscreen toggle
            if (e.Key == System.Windows.Input.Key.F11)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                    WindowStyle = WindowStyle.SingleBorderWindow;
                }
                else
                {
                    WindowState = WindowState.Maximized;
                    WindowStyle = WindowStyle.None;
                }
            }
        }
        
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _logger.LogInformation("Application closing...");
            
            // Stop queue processor
            _queueProcessor?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            
            // Save queue
            _queueManager?.Dispose();
            
            // Stop web server
            _webServer?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            
            // Clean up WebView2
            if (webView != null && webView.CoreWebView2 != null)
            {
                webView.Dispose();
            }
        }
        
        private async Task CreateEmergencyPatient(JObject message)
        {
            try
            {
                var type = message["data"]?["type"]?.ToString() ?? "unknown";
                var patientInfo = PatientInfo.CreateEmergencyPatient(type);
                
                await SendMessageToWebView(new
                {
                    type = "emergencyPatient",
                    patientInfo = patientInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create emergency patient");
                await SendErrorToWebView($"Failed to create emergency patient: {ex.Message}");
            }
        }
        
        private async Task GetQueueStatus()
        {
            try
            {
                var stats = _queueManager!.GetStats();
                var items = _queueManager.GetAllItems();
                
                await SendMessageToWebView(new
                {
                    type = "queueStatus",
                    stats = stats,
                    items = items.Take(20) // Send only recent 20 items
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue status");
                await SendErrorToWebView($"Failed to get queue status: {ex.Message}");
            }
        }
        
        private async Task TestPacsConnection()
        {
            try
            {
                var success = await _pacsSender!.TestConnectionAsync();
                
                if (success)
                {
                    await SendSuccessToWebView("PACS connection successful");
                }
                else
                {
                    await SendErrorToWebView("PACS connection failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test PACS connection");
                await SendErrorToWebView($"Failed to test PACS connection: {ex.Message}");
            }
        }
    }
}