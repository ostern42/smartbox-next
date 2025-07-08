using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        private MwlService? _mwlService;
        private bool _isInitialized = false;
        
        // Patient context from MWL selection
        private WorklistItem? _selectedWorklistItem;
        
        private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Initialize logger
            _logger = _loggerFactory.CreateLogger<MainWindow>();
            
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
                
                UpdateStatus("Initializing medical components...");
                
                // Initialize medical components
                _dicomExporter = new DicomExporter(_config);
                _pacsSender = new PacsSender(_config);
                _queueManager = new QueueManager(_config);
                _queueProcessor = new QueueProcessor(_config, _queueManager, _pacsSender);
                _mwlService = new MwlService(_config);
                
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
            webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
            
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
                    
                case "photocaptured":
                    await HandlePhotoCaptured(message);
                    break;
                    
                case "savephoto":
                    await SavePhoto(message);
                    break;
                    
                case "videorecorded":
                    await HandleVideoRecorded(message);
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
                    
                case "webcaminitialized":
                    await HandleWebcamInitialized(message);
                    break;
                    
                case "cameraanalysis":
                    await HandleCameraAnalysis(message);
                    break;
                    
                case "requestconfig":
                    await HandleRequestConfig();
                    break;
                    
                case "updateconfig":
                    await UpdateConfiguration(message);
                    break;
                    
                case "browsefolder":
                    await HandleBrowseFolder(message);
                    break;
                    
                case "getsettings":
                    await HandleGetSettings();
                    break;
                    
                case "savesettings":
                    await HandleSaveSettings(message);
                    break;
                    
                case "testpacsconnection":
                    await HandleTestPacsConnection(message);
                    break;
                    
                case "queryworklist":
                    await HandleQueryWorklist(message);
                    break;
                    
                case "refreshworklist":
                    await HandleRefreshWorklist();
                    break;
                    
                case "getworklistcachestatus":
                    await HandleGetWorklistCacheStatus();
                    break;
                    
                case "selectworklistitem":
                    await HandleSelectWorklistItem(message);
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
                var logsPath = Logger.GetLogDirectory();
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
        
        private async Task HandlePhotoCaptured(JObject message)
        {
            try
            {
                var data = message["data"];
                var imageData = data?["imageData"]?.ToString();
                var timestamp = data?["timestamp"]?.ToString();
                var patient = data?["patient"];
                
                if (string.IsNullOrEmpty(imageData))
                {
                    throw new ArgumentException("No image data provided");
                }
                
                // Convert base64 to bytes
                var imageBytes = Convert.FromBase64String(imageData);
                
                // Save to configured photo directory
                var photoDir = _config!.Storage.PhotosPath;
                Directory.CreateDirectory(photoDir);
                
                var fileName = $"IMG_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                var filePath = Path.Combine(photoDir, fileName);
                
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                _logger.LogInformation("Photo saved: {Path}", filePath);
                await SendMessageToWebView(new
                {
                    action = "log",
                    data = new
                    {
                        message = $"Photo saved: {fileName}",
                        level = "info"
                    }
                });
                
                // If DICOM export is requested
                if (patient != null)
                {
                    var patientInfo = new PatientInfo
                    {
                        PatientId = patient["id"]?.ToString(),
                        FirstName = ExtractFirstName(patient["name"]?.ToString()),
                        LastName = ExtractLastName(patient["name"]?.ToString()),
                        BirthDate = ParseBirthDate(patient["birthDate"]?.ToString()),
                        Gender = patient["gender"]?.ToString(),
                        StudyDescription = patient["studyDescription"]?.ToString(),
                        // Critical: Use StudyInstanceUID from selected MWL item
                        StudyInstanceUID = _selectedWorklistItem?.StudyInstanceUID,
                        AccessionNumber = _selectedWorklistItem?.AccessionNumber
                    };
                    
                    // Export as DICOM if configured
                    if (_config.Application.AutoExportDicom)
                    {
                        var dicomPath = await _dicomExporter!.ExportDicomAsync(imageBytes, patientInfo);
                        _queueManager!.Enqueue(dicomPath, patientInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save photo");
                await SendErrorToWebView($"Failed to save photo: {ex.Message}");
            }
        }
        
        private string? ExtractFirstName(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var parts = fullName.Split(',');
            return parts.Length > 1 ? parts[1].Trim() : null;
        }
        
        private string? ExtractLastName(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return null;
            var parts = fullName.Split(',');
            return parts[0].Trim();
        }
        
        private DateTime? ParseBirthDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;
            return DateTime.TryParse(dateStr, out var date) ? date : null;
        }
        
        private async Task SavePhoto(JObject message)
        {
            // Legacy handler - redirect to new handler
            await HandlePhotoCaptured(message);
        }
        
        private async Task HandleVideoRecorded(JObject message)
        {
            try
            {
                var data = message["data"];
                var videoData = data?["videoData"]?.ToString();
                var timestamp = data?["timestamp"]?.ToString();
                var patient = data?["patient"];
                var duration = data?["duration"]?.Value<double>() ?? 0;
                
                if (string.IsNullOrEmpty(videoData))
                {
                    throw new ArgumentException("No video data provided");
                }
                
                // Convert base64 to bytes
                var videoBytes = Convert.FromBase64String(videoData);
                
                // Save to configured video directory
                var videoDir = _config!.Storage.VideosPath;
                Directory.CreateDirectory(videoDir);
                
                var fileName = $"VID_{DateTime.Now:yyyyMMdd_HHmmss}.webm";
                var filePath = Path.Combine(videoDir, fileName);
                
                await File.WriteAllBytesAsync(filePath, videoBytes);
                
                _logger.LogInformation("Video saved: {Path} (Duration: {Duration}s)", filePath, duration);
                await SendMessageToWebView(new
                {
                    action = "log",
                    data = new
                    {
                        message = $"Video saved: {fileName} (Duration: {duration:F1}s)",
                        level = "info"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save video");
                await SendErrorToWebView($"Failed to save video: {ex.Message}");
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
                
                // Parse patient info (matching the JS getPatientInfo format)
                var patientInfo = new PatientInfo
                {
                    PatientId = patientInfoJson?["id"]?.ToString(),
                    FirstName = ExtractFirstName(patientInfoJson?["name"]?.ToString()),
                    LastName = ExtractLastName(patientInfoJson?["name"]?.ToString()),
                    Gender = patientInfoJson?["gender"]?.ToString(),
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
        
        private async Task HandleBrowseFolder(JObject message)
        {
            try
            {
                var data = message["data"];
                var inputId = data?["inputId"]?.ToString();
                var currentPath = data?["currentPath"]?.ToString();
                
                _logger.LogInformation("Browse folder requested for {InputId}", inputId);
                
                // Use Windows Forms FolderBrowserDialog
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    dialog.Description = "Select folder";
                    
                    // Set initial directory if provided
                    if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                    {
                        dialog.SelectedPath = currentPath;
                    }
                    
                    var result = dialog.ShowDialog();
                    
                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        var selectedPath = dialog.SelectedPath;
                        _logger.LogInformation("Folder selected: {Path}", selectedPath);
                        
                        // Send selected path back to web view
                        await SendMessageToWebView(new
                        {
                            action = "folderSelected",
                            data = new
                            {
                                inputId = inputId,
                                path = selectedPath
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to browse folder");
                await SendErrorToWebView($"Failed to browse folder: {ex.Message}");
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
        
        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
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
                    // Emergency exit button removed - using normal window controls
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
            // Alt+F4 with confirmation
            if (e.Key == System.Windows.Input.Key.F4 && 
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) == System.Windows.Input.ModifierKeys.Alt)
            {
                _logger.LogInformation("Alt+F4 pressed");
                
                // If WebView is not ready, just close
                if (webView == null || webView.CoreWebView2 == null)
                {
                    _logger.LogWarning("WebView not ready, closing directly");
                    Close();
                    return;
                }
                
                e.Handled = true; // Prevent immediate close
                
                // Show exit confirmation in web UI
                _ = SendMessageToWebView(new
                {
                    action = "showExitConfirmation"
                });
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
        
        private async Task HandleWebcamInitialized(JObject message)
        {
            try
            {
                var data = message["data"];
                var width = data?["width"]?.Value<int>() ?? 0;
                var height = data?["height"]?.Value<int>() ?? 0;
                var frameRate = data?["frameRate"]?.Value<double>() ?? 0;
                
                _logger.LogInformation("Webcam initialized: {Width}x{Height} @ {FrameRate}fps", width, height, frameRate);
                await SendMessageToWebView(new
                {
                    action = "log",
                    data = new
                    {
                        message = $"Webcam initialized: {width}x{height} @ {frameRate}fps",
                        level = "info"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle webcam initialization");
            }
        }
        
        private async Task HandleCameraAnalysis(JObject message)
        {
            try
            {
                var data = message["data"];
                _logger.LogInformation("Camera analysis received");
                
                // Log camera capabilities
                await SendMessageToWebView(new
                {
                    action = "log",
                    data = new
                    {
                        message = "Camera analysis completed",
                        level = "info"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle camera analysis");
            }
        }
        
        private async Task HandleGetSettings()
        {
            try
            {
                await SendMessageToWebView(new
                {
                    action = "settingsLoaded",
                    data = _config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send settings");
            }
        }
        
        private async Task HandleSaveSettings(JObject message)
        {
            try
            {
                var newSettings = message["data"];
                if (newSettings == null)
                {
                    throw new ArgumentException("No settings data provided");
                }
                
                // Update config object
                var storage = newSettings["Storage"];
                if (storage != null)
                {
                    _config!.Storage.PhotosPath = storage["photosPath"]?.ToString() ?? _config.Storage.PhotosPath;
                    _config.Storage.VideosPath = storage["videosPath"]?.ToString() ?? _config.Storage.VideosPath;
                    _config.Storage.DicomPath = storage["dicomPath"]?.ToString() ?? _config.Storage.DicomPath;
                    _config.Storage.QueuePath = storage["queuePath"]?.ToString() ?? _config.Storage.QueuePath;
                    _config.Storage.TempPath = storage["tempPath"]?.ToString() ?? _config.Storage.TempPath;
                    _config.Storage.MaxStorageDays = storage["maxStorageDays"]?.Value<int>() ?? _config.Storage.MaxStorageDays;
                    _config.Storage.EnableAutoCleanup = storage["enableAutoCleanup"]?.Value<bool>() ?? _config.Storage.EnableAutoCleanup;
                }
                
                var pacs = newSettings["Pacs"];
                if (pacs != null)
                {
                    _config!.Pacs.ServerHost = pacs["serverHost"]?.ToString() ?? _config.Pacs.ServerHost;
                    _config.Pacs.ServerPort = pacs["serverPort"]?.Value<int>() ?? _config.Pacs.ServerPort;
                    _config.Pacs.CalledAeTitle = pacs["calledAeTitle"]?.ToString() ?? _config.Pacs.CalledAeTitle;
                    _config.Pacs.CallingAeTitle = pacs["callingAeTitle"]?.ToString() ?? _config.Pacs.CallingAeTitle;
                    _config.Pacs.Timeout = pacs["timeout"]?.Value<int>() ?? _config.Pacs.Timeout;
                    _config.Pacs.EnableTls = pacs["enableTls"]?.Value<bool>() ?? _config.Pacs.EnableTls;
                    _config.Pacs.MaxRetries = pacs["maxRetries"]?.Value<int>() ?? _config.Pacs.MaxRetries;
                    _config.Pacs.RetryDelay = pacs["retryDelay"]?.Value<int>() ?? _config.Pacs.RetryDelay;
                }
                
                var video = newSettings["Video"];
                if (video != null)
                {
                    _config!.Video.DefaultResolution = video["defaultResolution"]?.ToString() ?? _config.Video.DefaultResolution;
                    _config.Video.DefaultFrameRate = video["defaultFrameRate"]?.Value<int>() ?? _config.Video.DefaultFrameRate;
                    _config.Video.DefaultQuality = video["defaultQuality"]?.Value<int>() ?? _config.Video.DefaultQuality;
                    _config.Video.EnableHardwareAcceleration = video["enableHardwareAcceleration"]?.Value<bool>() ?? _config.Video.EnableHardwareAcceleration;
                    _config.Video.PreferredCamera = video["preferredCamera"]?.ToString() ?? _config.Video.PreferredCamera;
                }
                
                var application = newSettings["Application"];
                if (application != null)
                {
                    _config!.Application.Language = application["language"]?.ToString() ?? _config.Application.Language;
                    _config.Application.Theme = application["theme"]?.ToString() ?? _config.Application.Theme;
                    _config.Application.EnableTouchKeyboard = application["enableTouchKeyboard"]?.Value<bool>() ?? _config.Application.EnableTouchKeyboard;
                    _config.Application.EnableDebugMode = application["enableDebugMode"]?.Value<bool>() ?? _config.Application.EnableDebugMode;
                    _config.Application.AutoStartCapture = application["autoStartCapture"]?.Value<bool>() ?? _config.Application.AutoStartCapture;
                    _config.Application.WebServerPort = application["webServerPort"]?.Value<int>() ?? _config.Application.WebServerPort;
                    _config.Application.EnableRemoteAccess = application["enableRemoteAccess"]?.Value<bool>() ?? _config.Application.EnableRemoteAccess;
                    _config.Application.HideExitButton = application["hideExitButton"]?.Value<bool>() ?? _config.Application.HideExitButton;
                    _config.Application.EnableEmergencyTemplates = application["enableEmergencyTemplates"]?.Value<bool>() ?? _config.Application.EnableEmergencyTemplates;
                }
                
                // Save to file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, json);
                
                _logger.LogInformation("Settings saved successfully");
                
                await SendMessageToWebView(new
                {
                    action = "settingsSaved",
                    data = new { success = true }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                await SendErrorToWebView($"Failed to save settings: {ex.Message}");
            }
        }
        
        private async Task HandleTestPacsConnection(JObject message)
        {
            try
            {
                var data = message["data"];
                var serverHost = data?["serverHost"]?.ToString();
                var serverPort = data?["serverPort"]?.Value<int>() ?? 104;
                var calledAeTitle = data?["calledAeTitle"]?.ToString();
                var callingAeTitle = data?["callingAeTitle"]?.ToString();
                
                _logger.LogInformation("Testing PACS connection to {Host}:{Port}", serverHost, serverPort);
                
                // TODO: Implement actual PACS C-ECHO test
                // For now, just simulate
                await Task.Delay(1000);
                
                bool success = !string.IsNullOrEmpty(serverHost);
                
                await SendMessageToWebView(new
                {
                    action = "pacsTestResult",
                    data = new 
                    { 
                        success = success,
                        error = success ? null : "No server host specified"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test PACS connection");
                await SendMessageToWebView(new
                {
                    action = "pacsTestResult",
                    data = new 
                    { 
                        success = false,
                        error = ex.Message
                    }
                });
            }
        }
        
        private async Task HandleRequestConfig()
        {
            try
            {
                await SendMessageToWebView(new
                {
                    action = "updateConfig",
                    data = new
                    {
                        autoStartWebcam = _config!.Application.AutoStartCapture,
                        defaultFrameRate = _config.Video.DefaultFrameRate,
                        defaultResolution = _config.Video.DefaultResolution,
                        photoFormat = "jpeg",
                        videoFormat = "webm",
                        enableEmergencyTemplates = _config.Application.EnableEmergencyTemplates,
                        enableWorklist = _config.MwlSettings?.EnableWorklist ?? false
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send configuration");
            }
        }
        
        private async Task HandleQueryWorklist(dynamic message)
        {
            try
            {
                DateTime? date = null;
                if (message?.date != null)
                {
                    if (DateTime.TryParse(message.date.ToString(), out DateTime parsedDate))
                    {
                        date = parsedDate;
                    }
                }

                var items = await _mwlService.GetWorklistAsync(date);
                
                await SendMessageToWebView(new
                {
                    action = "worklistResult",
                    data = new
                    {
                        items = items.Select(i => new
                        {
                            studyInstanceUID = i.StudyInstanceUID,
                            accessionNumber = i.AccessionNumber,
                            patientId = i.PatientId,
                            patientName = i.DisplayName,
                            birthDate = i.BirthDate?.ToString("yyyy-MM-dd"),
                            sex = i.Sex,
                            age = i.DisplayAge,
                            scheduledDate = i.ScheduledDate.ToString("yyyy-MM-dd"),
                            scheduledTime = i.DisplayTime,
                            studyDescription = i.StudyDescription,
                            isEmergency = i.IsEmergency
                        }),
                        cacheStatus = _mwlService.GetCacheStatus()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query worklist");
                await SendErrorToWebView($"Worklist query failed: {ex.Message}");
            }
        }
        
        private async Task HandleRefreshWorklist()
        {
            try
            {
                var success = await _mwlService.RefreshCacheAsync();
                
                await SendMessageToWebView(new
                {
                    action = "worklistRefreshResult",
                    data = new
                    {
                        success,
                        cacheStatus = _mwlService.GetCacheStatus()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh worklist");
                await SendErrorToWebView($"Worklist refresh failed: {ex.Message}");
            }
        }
        
        private async Task HandleGetWorklistCacheStatus()
        {
            try
            {
                var status = _mwlService.GetCacheStatus();
                
                await SendMessageToWebView(new
                {
                    action = "worklistCacheStatus",
                    data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get worklist cache status");
                await SendErrorToWebView($"Failed to get cache status: {ex.Message}");
            }
        }
        
        private async Task HandleQueryWorklist(JObject message)
        {
            try
            {
                var date = message["data"]?["date"]?.ToString();
                DateTime? queryDate = null;
                
                if (!string.IsNullOrEmpty(date))
                {
                    queryDate = DateTime.Parse(date);
                }
                
                var items = await _mwlService.GetWorklistAsync(queryDate);
                
                // Convert to JSON-friendly format
                var jsonItems = items.Select(i => new
                {
                    studyInstanceUID = i.StudyInstanceUID,
                    patientId = i.PatientId,
                    patientName = i.DisplayName,
                    birthDate = i.BirthDate?.ToString("yyyy-MM-dd"),
                    sex = i.Sex,
                    age = i.DisplayAge,
                    accessionNumber = i.AccessionNumber,
                    studyDescription = i.StudyDescription,
                    scheduledDate = i.ScheduledDate.ToString("yyyy-MM-dd"),
                    scheduledTime = i.DisplayTime,
                    isEmergency = i.IsEmergency
                }).ToList();
                
                await SendMessageToWebView(new
                {
                    action = "worklistResult",
                    data = new
                    {
                        items = jsonItems,
                        count = jsonItems.Count,
                        isFromCache = _mwlService.GetCacheStatus().IsOffline
                    }
                });
                
                _logger.LogInformation("Sent {Count} worklist items to UI", jsonItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query worklist");
                await SendErrorToWebView($"Failed to query worklist: {ex.Message}");
            }
        }
        
        private async Task HandleSelectWorklistItem(JObject message)
        {
            try
            {
                var data = message["data"];
                var studyInstanceUID = data?["studyInstanceUID"]?.ToString();
                
                if (string.IsNullOrEmpty(studyInstanceUID))
                {
                    _selectedWorklistItem = null;
                    _logger.LogInformation("Cleared worklist selection");
                    return;
                }
                
                // Create WorklistItem from the selected data
                _selectedWorklistItem = new WorklistItem
                {
                    StudyInstanceUID = studyInstanceUID,
                    PatientId = data?["patientId"]?.ToString(),
                    PatientName = data?["patientName"]?.ToString(),
                    AccessionNumber = data?["accessionNumber"]?.ToString(),
                    BirthDate = data?["birthDate"] != null ? DateTime.Parse(data["birthDate"].ToString()) : null,
                    Sex = data?["sex"]?.ToString(),
                    StudyDescription = data?["studyDescription"]?.ToString()
                };
                
                _logger.LogInformation("Selected worklist item: {PatientId} - {PatientName}, StudyUID: {StudyUID}", 
                    _selectedWorklistItem.PatientId, 
                    _selectedWorklistItem.PatientName,
                    _selectedWorklistItem.StudyInstanceUID);
                
                await SendMessageToWebView(new
                {
                    action = "worklistItemSelected",
                    data = new
                    {
                        success = true,
                        studyInstanceUID = _selectedWorklistItem.StudyInstanceUID
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select worklist item");
                await SendErrorToWebView($"Failed to select worklist item: {ex.Message}");
            }
        }
    }
}