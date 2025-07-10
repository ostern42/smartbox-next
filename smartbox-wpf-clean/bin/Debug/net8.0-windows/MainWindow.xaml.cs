using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartBoxNext.Helpers;
using SmartBoxNext.Services;

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
        
        // New unified capture system
        private UnifiedCaptureManager? _unifiedCaptureManager;
        private OptimizedDicomConverter? _optimizedDicomConverter;
        private IntegratedQueueManager? _integratedQueueManager;
        
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
                
                // Initialize legacy medical components
                _dicomExporter = new DicomExporter(_config);
                _pacsSender = new PacsSender(_config);
                _queueManager = new QueueManager(_config);
                _queueProcessor = new QueueProcessor(_config, _queueManager, _pacsSender);
                _mwlService = new MwlService(_config);
                
                // Initialize new unified capture system
                var dicomLogger = _loggerFactory.CreateLogger<OptimizedDicomConverter>();
                var captureLogger = _loggerFactory.CreateLogger<UnifiedCaptureManager>();
                var queueLogger = _loggerFactory.CreateLogger<IntegratedQueueManager>();
                
                _optimizedDicomConverter = new OptimizedDicomConverter(dicomLogger, _config);
                _unifiedCaptureManager = new UnifiedCaptureManager(captureLogger);
                _integratedQueueManager = new IntegratedQueueManager(
                    queueLogger, _queueManager, _optimizedDicomConverter, _unifiedCaptureManager, _config);
                
                // Initialize unified capture manager
                await _unifiedCaptureManager.InitializeAsync();
                
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
        
        private CoreWebView2Environment? _webViewEnvironment;
        private string? _webViewUserDataFolder;
        
        private async Task InitializeWebView()
        {
            try
            {
                _logger.LogInformation("Starting WebView2 initialization...");
                UpdateStatus("Creating WebView2 environment...");
                
                // Set WebView2 user data folder
                _webViewUserDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebView2Data");
                Directory.CreateDirectory(_webViewUserDataFolder);
                _logger.LogInformation("WebView2 user data folder: {Folder}", _webViewUserDataFolder);
                
                UpdateStatus("Creating CoreWebView2Environment...");
                _logger.LogInformation("Creating CoreWebView2Environment...");
                
                try
                {
                    // Store the environment reference for cleanup
                    _webViewEnvironment = await CoreWebView2Environment.CreateAsync(null, _webViewUserDataFolder);
                    _logger.LogInformation("CoreWebView2Environment created successfully");
                }
                catch (Exception envEx)
                {
                    _logger.LogError(envEx, "Failed to create CoreWebView2Environment - WebView2 Runtime may not be installed!");
                    UpdateStatus("ERROR: WebView2 Runtime not found!");
                    throw new InvalidOperationException("WebView2 Runtime is not installed. Please install it from https://go.microsoft.com/fwlink/p/?LinkId=2124703", envEx);
                }
                
                UpdateStatus("Initializing CoreWebView2...");
                _logger.LogInformation("Ensuring CoreWebView2 is initialized...");
                
                try
                {
                    await webView.EnsureCoreWebView2Async(_webViewEnvironment);
                    _logger.LogInformation("CoreWebView2 initialized successfully");
                }
                catch (Exception coreEx)
                {
                    _logger.LogError(coreEx, "Failed to initialize CoreWebView2");
                    UpdateStatus("ERROR: Failed to initialize WebView2!");
                    throw;
                }
                
                UpdateStatus("Configuring WebView2 settings...");
                _logger.LogInformation("CoreWebView2 initialized, configuring settings...");
                // Configure WebView2
                await ConfigureWebView();
                
                // Navigate to the application
                var url = $"http://localhost:{_config!.Application.WebServerPort}/index.html";
                UpdateStatus($"Navigating to {url}...");
                _logger.LogInformation("Navigating to: {Url}", url);
                webView.Source = new Uri(url);
                
                UpdateStatus("WebView2 ready!");
                _logger.LogInformation("WebView2 initialization complete!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize WebView2");
                throw;
            }
        }
        
        private async Task ConfigureWebView()
        {
            _logger.LogInformation("Configuring WebView2 settings...");
            
            if (webView.CoreWebView2 == null)
            {
                _logger.LogError("CoreWebView2 is null in ConfigureWebView!");
                throw new InvalidOperationException("CoreWebView2 is not initialized");
            }
            
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
            
            _logger.LogInformation("WebMessageReceived handler attached");
            
            // Test if we can execute script
            try
            {
                var result = await webView.CoreWebView2.ExecuteScriptAsync("console.log('C# can execute scripts!'); 'test';");
                _logger.LogInformation("Script execution test result: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute test script");
            }
            
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
                _logger.LogInformation("=== WebView Message Received ===");
                _logger.LogInformation("Raw message: {Message}", message);
                
                // Send immediate feedback to debug page
                await webView.CoreWebView2.ExecuteScriptAsync($"console.log('C# received: {message.Replace("'", "\\'")}');");
                
                // Try to parse as JSON
                JObject? messageObj = null;
                try
                {
                    messageObj = JObject.Parse(message);
                    _logger.LogInformation("Parsed as JSON successfully");
                }
                catch (Exception parseEx)
                {
                    _logger.LogInformation("Not JSON, treating as simple command: {Error}", parseEx.Message);
                    // If not JSON, treat as simple string command
                    await HandleSimpleCommand(message);
                    return;
                }
                
                // Handle JSON message
                var action = messageObj["action"]?.ToString();
                _logger.LogInformation("Action extracted: {Action}", action);
                
                if (string.IsNullOrEmpty(action))
                {
                    _logger.LogWarning("Received message without action");
                    return;
                }
                
                _logger.LogInformation("Calling HandleAction for: {Action}", action);
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
            _logger.LogInformation("=== HandleAction called ===");
            _logger.LogInformation("Action: '{Action}'", action);
            _logger.LogInformation("Message: {Message}", message.ToString());
            
            // Send feedback to browser
            await webView.CoreWebView2.ExecuteScriptAsync($"console.log('C# HandleAction: {action}');");
            
            switch (action)
            {
                case "log":
                    await HandleLog(message);
                    break;
                    
                case "openLogs":
                    await OpenLogsFolder();
                    break;
                    
                case "openSettings":
                    await OpenSettings();
                    break;
                    
                case "photoCaptured":
                    await HandlePhotoCaptured(message);
                    break;
                    
                case "savePhoto":
                    await SavePhoto(message);
                    break;
                    
                case "videoRecorded":
                    await HandleVideoRecorded(message);
                    break;
                    
                case "saveVideo":
                    await SaveVideo(message);
                    break;
                    
                case "exportDicom":
                    await ExportDicom(message);
                    break;
                    
                case "sendToPacs":
                    await SendToPacs(message);
                    break;
                    
                case "testWebView":
                    await TestWebView();
                    break;
                    
                case "webcamInitialized":
                    await HandleWebcamInitialized(message);
                    break;
                    
                case "cameraAnalysis":
                    await HandleCameraAnalysis(message);
                    break;
                    
                case "requestConfig":
                    await HandleRequestConfig();
                    break;
                    
                case "updateConfig":
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
                    
                case "testmwlconnection":
                    await HandleTestMwlConnection(message);
                    break;
                    
                case "queryWorklist":
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
                    
                // New Yuan capture handlers
                case "connectyuan":
                    await HandleConnectYuan();
                    break;
                    
                case "disconnectyuan":
                    await HandleDisconnectYuan();
                    break;
                    
                case "getyuaninputs":
                    await HandleGetYuanInputs();
                    break;
                    
                case "selectyuaninput":
                    await HandleSelectYuanInput(message);
                    break;
                    
                case "setactivesource":
                    await HandleSetActiveSource(message);
                    break;
                    
                case "capturehighres":
                    await HandleCaptureHighRes(message);
                    break;
                    
                case "getcapturestats":
                    await HandleGetCaptureStats();
                    break;
                    
                case "getunifiedstatus":
                    await HandleGetUnifiedStatus();
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
        
        private async Task HandleLog(JObject message)
        {
            try
            {
                var data = message["data"];
                var logMessage = data?["message"]?.ToString() ?? "No message";
                var level = data?["level"]?.ToString() ?? "info";
                var timestamp = data?["timestamp"]?.ToString();
                
                // Log to file using our Logger
                switch (level.ToLower())
                {
                    case "error":
                        Logger.LogError($"[WebUI] {logMessage}");
                        break;
                    case "warn":
                    case "warning":
                        Logger.LogWarning($"[WebUI] {logMessage}");
                        break;
                    case "debug":
                        Logger.LogDebug($"[WebUI] {logMessage}");
                        break;
                    default:
                        Logger.LogInfo($"[WebUI] {logMessage}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to handle log message from WebUI");
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
                var source = data?["source"]?.ToString() ?? "webrtc"; // webrtc or yuan
                
                if (string.IsNullOrEmpty(imageData))
                {
                    throw new ArgumentException("No image data provided");
                }
                
                // Convert base64 to bytes
                var imageBytes = Convert.FromBase64String(imageData);
                
                // Save to configured photo directory (legacy support)
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
                
                // Use unified capture system for DICOM processing
                if (patient != null && _integratedQueueManager != null)
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
                    
                    // Use integrated queue manager for DICOM conversion and queueing
                    if (_config.Application.AutoExportDicom)
                    {
                        var result = await _integratedQueueManager.ConvertAndQueueAsync(
                            imageBytes, 
                            FrameFormat.JPEG, 
                            0, 0, // Width/height will be determined from JPEG
                            patientInfo,
                            new SnapshotMetadata 
                            { 
                                InputSource = source.ToUpper(),
                                Comments = $"Captured from {source}"
                            },
                            queueForPacs: true);
                        
                        if (result.Success)
                        {
                            await SendMessageToWebView(new
                            {
                                action = "log",
                                data = new
                                {
                                    message = $"Photo converted to DICOM and queued for PACS",
                                    level = "info"
                                }
                            });
                        }
                        else
                        {
                            await SendErrorToWebView($"DICOM conversion failed: {result.ErrorMessage}");
                        }
                    }
                }
                
                // Fallback to legacy system if unified system not available
                else if (patient != null)
                {
                    var patientInfo = new PatientInfo
                    {
                        PatientId = patient["id"]?.ToString(),
                        FirstName = ExtractFirstName(patient["name"]?.ToString()),
                        LastName = ExtractLastName(patient["name"]?.ToString()),
                        BirthDate = ParseBirthDate(patient["birthDate"]?.ToString()),
                        Gender = patient["gender"]?.ToString(),
                        StudyDescription = patient["studyDescription"]?.ToString(),
                        StudyInstanceUID = _selectedWorklistItem?.StudyInstanceUID,
                        AccessionNumber = _selectedWorklistItem?.AccessionNumber
                    };
                    
                    // Export as DICOM if configured (legacy)
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
                var fieldId = data?["fieldId"]?.ToString();
                var currentPath = data?["currentPath"]?.ToString();
                
                _logger.LogInformation("Browse folder requested for {FieldId}", fieldId);
                
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
                            fieldId = fieldId,
                            path = selectedPath
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
        
        private bool _isClosing = false;
        
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            // Prevent re-entry
            if (_isClosing)
            {
                return;
            }
            
            _logger.LogInformation("Application closing...");
            
            // Cancel the close for now to do cleanup
            e.Cancel = true;
            _isClosing = true;
            
            try
            {
                // Stop all services gracefully
                var stopTasks = new List<Task>();
                
                // Stop queue processor
                if (_queueProcessor != null)
                {
                    stopTasks.Add(_queueProcessor.StopAsync());
                }
                
                // Stop web server
                if (_webServer != null)
                {
                    stopTasks.Add(_webServer.StopAsync());
                }
                
                // Wait for all stops with a reasonable timeout
                await Task.WhenAny(Task.WhenAll(stopTasks), Task.Delay(2000));
                
                // Dispose new unified capture system
                _integratedQueueManager?.Dispose();
                _unifiedCaptureManager?.Dispose();
                
                // Save queue (legacy)
                _queueManager?.Dispose();
                
                // Clean up WebView2 properly using ProcessHelper
                if (webView != null)
                {
                    _logger.LogInformation("Starting WebView2 cleanup...");
                    
                    // Remove event handlers first
                    if (webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.WebMessageReceived -= WebView_WebMessageReceived;
                        webView.CoreWebView2.WindowCloseRequested -= WebView_WindowCloseRequested;
                        webView.CoreWebView2.NavigationStarting -= WebView_NavigationStarting;
                        webView.CoreWebView2.NavigationCompleted -= WebView_NavigationCompleted;
                        webView.CoreWebView2.DOMContentLoaded -= WebView_DOMContentLoaded;
                        webView.CoreWebView2.PermissionRequested -= WebView_PermissionRequested;
                        
                        // Stop all content
                        try
                        {
                            await webView.CoreWebView2.ExecuteScriptAsync("window.stop();");
                        }
                        catch { }
                    }
                    
                    // Navigate to about:blank to release resources
                    try
                    {
                        webView.CoreWebView2?.Navigate("about:blank");
                        await Task.Delay(500); // Give it more time to clean up
                    }
                    catch { }
                    
                    // Close the WebView2 controller
                    try
                    {
                        webView.CoreWebView2?.Stop();
                    }
                    catch { }
                    
                    // Give time for navigation to complete
                    await Task.Delay(200);
                    
                    // Dispose the WebView2 control
                    webView.Dispose();
                    _logger.LogInformation("WebView2 disposed");
                }
                
                // Clean up the WebView2 environment
                if (_webViewEnvironment != null)
                {
                    try
                    {
                        // Force garbage collection to release COM objects
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        
                        _logger.LogInformation("WebView2 environment cleanup completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up WebView2 environment");
                    }
                }
                
                // Use ProcessHelper to force kill all WebView2 processes
                _logger.LogInformation("Force killing all WebView2 processes...");
                await ProcessHelper.ForceKillWebView2Async();
                
                // Clean up WebView2 user data
                ProcessHelper.CleanupWebView2UserData();
                
                _logger.LogInformation("Cleanup completed, shutting down application");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during shutdown");
            }
            finally
            {
                // Final attempt to kill any remaining child processes
                try
                {
                    var currentPid = Process.GetCurrentProcess().Id;
                    await ProcessHelper.KillProcessTreeAsync(currentPid);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing child processes");
                }
                
                // Force exit if needed
                Environment.Exit(0);
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
                    _config!.Storage.PhotosPath = storage["PhotosPath"]?.ToString() ?? _config.Storage.PhotosPath;
                    _config.Storage.VideosPath = storage["VideosPath"]?.ToString() ?? _config.Storage.VideosPath;
                    _config.Storage.DicomPath = storage["DicomPath"]?.ToString() ?? _config.Storage.DicomPath;
                    _config.Storage.QueuePath = storage["QueuePath"]?.ToString() ?? _config.Storage.QueuePath;
                    _config.Storage.TempPath = storage["TempPath"]?.ToString() ?? _config.Storage.TempPath;
                    _config.Storage.MaxStorageDays = storage["MaxStorageDays"]?.Value<int>() ?? _config.Storage.MaxStorageDays;
                    _config.Storage.EnableAutoCleanup = storage["EnableAutoCleanup"]?.Value<bool>() ?? _config.Storage.EnableAutoCleanup;
                }
                
                var pacs = newSettings["Pacs"];
                if (pacs != null)
                {
                    _config!.Pacs.ServerHost = pacs["ServerHost"]?.ToString() ?? _config.Pacs.ServerHost;
                    _config.Pacs.ServerPort = pacs["ServerPort"]?.Value<int>() ?? _config.Pacs.ServerPort;
                    _config.Pacs.CalledAeTitle = pacs["CalledAeTitle"]?.ToString() ?? _config.Pacs.CalledAeTitle;
                    _config.Pacs.CallingAeTitle = pacs["CallingAeTitle"]?.ToString() ?? _config.Pacs.CallingAeTitle;
                    _config.Pacs.Timeout = pacs["Timeout"]?.Value<int>() ?? _config.Pacs.Timeout;
                    _config.Pacs.EnableTls = pacs["EnableTls"]?.Value<bool>() ?? _config.Pacs.EnableTls;
                    _config.Pacs.MaxRetries = pacs["MaxRetries"]?.Value<int>() ?? _config.Pacs.MaxRetries;
                    _config.Pacs.RetryDelay = pacs["RetryDelay"]?.Value<int>() ?? _config.Pacs.RetryDelay;
                }
                
                var video = newSettings["Video"];
                if (video != null)
                {
                    _config!.Video.DefaultResolution = video["DefaultResolution"]?.ToString() ?? _config.Video.DefaultResolution;
                    _config.Video.DefaultFrameRate = video["DefaultFrameRate"]?.Value<int>() ?? _config.Video.DefaultFrameRate;
                    _config.Video.DefaultQuality = video["DefaultQuality"]?.Value<int>() ?? _config.Video.DefaultQuality;
                    _config.Video.EnableHardwareAcceleration = video["EnableHardwareAcceleration"]?.Value<bool>() ?? _config.Video.EnableHardwareAcceleration;
                    _config.Video.PreferredCamera = video["PreferredCamera"]?.ToString() ?? _config.Video.PreferredCamera;
                }
                
                var application = newSettings["Application"];
                if (application != null)
                {
                    _config!.Application.Language = application["Language"]?.ToString() ?? _config.Application.Language;
                    _config.Application.Theme = application["Theme"]?.ToString() ?? _config.Application.Theme;
                    _config.Application.EnableTouchKeyboard = application["EnableTouchKeyboard"]?.Value<bool>() ?? _config.Application.EnableTouchKeyboard;
                    _config.Application.EnableDebugMode = application["EnableDebugMode"]?.Value<bool>() ?? _config.Application.EnableDebugMode;
                    _config.Application.AutoStartCapture = application["AutoStartCapture"]?.Value<bool>() ?? _config.Application.AutoStartCapture;
                    _config.Application.WebServerPort = application["WebServerPort"]?.Value<int>() ?? _config.Application.WebServerPort;
                    _config.Application.EnableRemoteAccess = application["EnableRemoteAccess"]?.Value<bool>() ?? _config.Application.EnableRemoteAccess;
                    _config.Application.HideExitButton = application["HideExitButton"]?.Value<bool>() ?? _config.Application.HideExitButton;
                    _config.Application.EnableEmergencyTemplates = application["EnableEmergencyTemplates"]?.Value<bool>() ?? _config.Application.EnableEmergencyTemplates;
                }
                
                var mwlSettings = newSettings["MwlSettings"];
                if (mwlSettings != null && _config!.MwlSettings != null)
                {
                    _config.MwlSettings.EnableWorklist = mwlSettings["EnableWorklist"]?.Value<bool>() ?? _config.MwlSettings.EnableWorklist;
                    _config.MwlSettings.MwlServerHost = mwlSettings["MwlServerHost"]?.ToString() ?? _config.MwlSettings.MwlServerHost;
                    _config.MwlSettings.MwlServerPort = mwlSettings["MwlServerPort"]?.Value<int>() ?? _config.MwlSettings.MwlServerPort;
                    _config.MwlSettings.MwlServerAET = mwlSettings["MwlServerAET"]?.ToString() ?? _config.MwlSettings.MwlServerAET;
                    // Note: Modality, StationName, CacheDurationHours, AutoRefresh are not in MwlConfig
                }
                
                // Save to file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, json);
                
                _logger.LogInformation("Settings saved successfully");
                
                await SendMessageToWebView(new
                {
                    action = "settingsSaved",
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                await SendMessageToWebView(new
                {
                    action = "settingsSaved",
                    success = false,
                    error = ex.Message,
                    message = ex.Message
                });
            }
        }
        
        private async Task HandleTestPacsConnection(JObject message)
        {
            try
            {
                var data = message["data"];
                var serverHost = data?["ServerHost"]?.ToString();
                var serverPort = data?["ServerPort"]?.Value<int>() ?? 104;
                var calledAeTitle = data?["CalledAeTitle"]?.ToString();
                var callingAeTitle = data?["CallingAeTitle"]?.ToString();
                
                _logger.LogInformation("Testing PACS connection to {Host}:{Port}", serverHost, serverPort);
                
                if (string.IsNullOrEmpty(serverHost) || string.IsNullOrEmpty(calledAeTitle) || string.IsNullOrEmpty(callingAeTitle))
                {
                    await SendMessageToWebView(new
                    {
                        action = "testConnectionResult",
                        data = new 
                        { 
                            success = false,
                            message = "Missing required PACS configuration",
                            type = "pacs"
                        }
                    });
                    return;
                }
                
                // Open diagnostic window
                await Dispatcher.InvokeAsync(() =>
                {
                    var diagnosticWindow = new DiagnosticWindow(
                        _logger,
                        serverHost,
                        serverPort,
                        callingAeTitle,
                        calledAeTitle,
                        isMwl: false
                    );
                    diagnosticWindow.Owner = this;
                    diagnosticWindow.ShowDialog();
                });
                
                // Return simple success message
                await SendMessageToWebView(new
                {
                    action = "testConnectionResult",
                    data = new 
                    { 
                        success = true,
                        message = "Diagnostic test completed",
                        type = "pacs"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test PACS connection");
                await SendMessageToWebView(new
                {
                    action = "testConnectionResult",
                    data = new 
                    { 
                        success = false,
                        message = $"PACS test error: {ex.Message}",
                        type = "pacs"
                    }
                });
            }
        }
        
        private async Task HandleTestMwlConnection(JObject message)
        {
            try
            {
                var data = message["data"];
                var serverHost = data?["MwlServerHost"]?.ToString();
                var serverPort = data?["MwlServerPort"]?.Value<int>() ?? 105;
                var serverAeTitle = data?["MwlServerAET"]?.ToString();
                var localAeTitle = data?["LocalAET"]?.ToString() ?? "SMARTBOX";
                
                _logger.LogInformation("Testing MWL connection to {Host}:{Port}", serverHost, serverPort);
                
                if (string.IsNullOrEmpty(serverHost) || string.IsNullOrEmpty(serverAeTitle) || string.IsNullOrEmpty(localAeTitle))
                {
                    await SendMessageToWebView(new
                    {
                        action = "testConnectionResult",
                        data = new 
                        { 
                            success = false,
                            message = "Missing required MWL configuration",
                            type = "mwl"
                        }
                    });
                    return;
                }
                
                // Open diagnostic window
                await Dispatcher.InvokeAsync(() =>
                {
                    var diagnosticWindow = new DiagnosticWindow(
                        _logger,
                        serverHost,
                        serverPort,
                        localAeTitle,
                        serverAeTitle,
                        isMwl: true
                    );
                    diagnosticWindow.Owner = this;
                    diagnosticWindow.ShowDialog();
                });
                
                // Return simple success message
                await SendMessageToWebView(new
                {
                    action = "testConnectionResult",
                    data = new 
                    { 
                        success = true,
                        message = "Diagnostic test completed",
                        type = "mwl"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test MWL connection");
                await SendMessageToWebView(new
                {
                    action = "testConnectionResult",
                    data = new 
                    { 
                        success = false,
                        message = $"MWL test error: {ex.Message}",
                        type = "mwl"
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
        
        // New Yuan capture handlers
        
        private async Task HandleConnectYuan()
        {
            try
            {
                if (_unifiedCaptureManager == null)
                {
                    await SendErrorToWebView("Unified capture manager not initialized");
                    return;
                }

                var connected = await _unifiedCaptureManager.ConnectToYuanAsync();
                
                await SendMessageToWebView(new
                {
                    action = "yuanConnectionResult",
                    data = new
                    {
                        connected,
                        message = connected ? "Connected to Yuan capture service" : "Failed to connect to Yuan service"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Yuan");
                await SendErrorToWebView($"Failed to connect to Yuan: {ex.Message}");
            }
        }

        private async Task HandleDisconnectYuan()
        {
            try
            {
                if (_unifiedCaptureManager == null)
                {
                    await SendErrorToWebView("Unified capture manager not initialized");
                    return;
                }

                await _unifiedCaptureManager.DisconnectFromYuanAsync();
                
                await SendMessageToWebView(new
                {
                    action = "yuanDisconnected",
                    data = new
                    {
                        success = true,
                        message = "Disconnected from Yuan capture service"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from Yuan");
                await SendErrorToWebView($"Failed to disconnect from Yuan: {ex.Message}");
            }
        }

        private async Task HandleGetYuanInputs()
        {
            try
            {
                if (_unifiedCaptureManager == null || !_unifiedCaptureManager.IsYuanConnected)
                {
                    await SendErrorToWebView("Yuan capture service not connected");
                    return;
                }

                var inputs = await _unifiedCaptureManager.GetYuanInputsAsync();
                
                await SendMessageToWebView(new
                {
                    action = "yuanInputsResult",
                    data = new
                    {
                        success = true,
                        inputs
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Yuan inputs");
                await SendErrorToWebView($"Failed to get Yuan inputs: {ex.Message}");
            }
        }

        private async Task HandleSelectYuanInput(JObject message)
        {
            try
            {
                if (_unifiedCaptureManager == null || !_unifiedCaptureManager.IsYuanConnected)
                {
                    await SendErrorToWebView("Yuan capture service not connected");
                    return;
                }

                var inputIndex = message["data"]?["inputIndex"]?.Value<int>();
                if (!inputIndex.HasValue)
                {
                    await SendErrorToWebView("Invalid input index");
                    return;
                }

                var success = await _unifiedCaptureManager.SelectYuanInputAsync(inputIndex.Value);
                
                await SendMessageToWebView(new
                {
                    action = "yuanInputSelected",
                    data = new
                    {
                        success,
                        inputIndex = inputIndex.Value,
                        message = success ? $"Selected input {inputIndex.Value}" : "Failed to select input"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to select Yuan input");
                await SendErrorToWebView($"Failed to select Yuan input: {ex.Message}");
            }
        }

        private async Task HandleSetActiveSource(JObject message)
        {
            try
            {
                if (_unifiedCaptureManager == null)
                {
                    await SendErrorToWebView("Unified capture manager not initialized");
                    return;
                }

                var sourceName = message["data"]?["source"]?.ToString();
                if (string.IsNullOrEmpty(sourceName))
                {
                    await SendErrorToWebView("Invalid source name");
                    return;
                }

                var source = sourceName.ToLowerInvariant() == "yuan" ? CaptureSource.Yuan : CaptureSource.WebRTC;
                await _unifiedCaptureManager.SetActiveSourceAsync(source);
                
                await SendMessageToWebView(new
                {
                    action = "activeSourceChanged",
                    data = new
                    {
                        source = source.ToString(),
                        message = $"Active source set to {source}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set active source");
                await SendErrorToWebView($"Failed to set active source: {ex.Message}");
            }
        }

        private async Task HandleCaptureHighRes(JObject message)
        {
            try
            {
                if (_integratedQueueManager == null)
                {
                    await SendErrorToWebView("Integrated queue manager not initialized");
                    return;
                }

                var data = message["data"];
                var patient = data?["patient"];
                
                if (patient == null)
                {
                    await SendErrorToWebView("Patient information required for high-res capture");
                    return;
                }

                var patientInfo = new PatientInfo
                {
                    PatientId = patient["id"]?.ToString(),
                    FirstName = ExtractFirstName(patient["name"]?.ToString()),
                    LastName = ExtractLastName(patient["name"]?.ToString()),
                    BirthDate = ParseBirthDate(patient["birthDate"]?.ToString()),
                    Gender = patient["gender"]?.ToString(),
                    StudyDescription = patient["studyDescription"]?.ToString(),
                    StudyInstanceUID = _selectedWorklistItem?.StudyInstanceUID,
                    AccessionNumber = _selectedWorklistItem?.AccessionNumber
                };

                var metadata = new SnapshotMetadata
                {
                    Modality = "ES", // Endoscopy for high-res Yuan snapshots
                    InputSource = "Yuan_HighRes",
                    Comments = "High-resolution snapshot from Yuan capture"
                };

                var result = await _integratedQueueManager.CaptureHighResSnapshotAsync(patientInfo, metadata);
                
                if (result.Success)
                {
                    await SendMessageToWebView(new
                    {
                        action = "highResCaptured",
                        data = new
                        {
                            success = true,
                            dicomPath = result.DicomPath,
                            queuedForPacs = result.QueuedForPacs,
                            message = "High-resolution snapshot captured and processed"
                        }
                    });
                }
                else
                {
                    await SendErrorToWebView($"High-res capture failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture high-res snapshot");
                await SendErrorToWebView($"Failed to capture high-res snapshot: {ex.Message}");
            }
        }

        private async Task HandleGetCaptureStats()
        {
            try
            {
                var stats = _unifiedCaptureManager?.GetStatistics();
                var queueStatus = _integratedQueueManager?.GetQueueStatus();
                
                await SendMessageToWebView(new
                {
                    action = "captureStatsResult",
                    data = new
                    {
                        captureStats = stats,
                        queueStatus = queueStatus,
                        timestamp = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get capture stats");
                await SendErrorToWebView($"Failed to get capture stats: {ex.Message}");
            }
        }

        private async Task HandleGetUnifiedStatus()
        {
            try
            {
                var status = new
                {
                    yuanConnected = _unifiedCaptureManager?.IsYuanConnected ?? false,
                    webrtcActive = _unifiedCaptureManager?.IsWebRTCActive ?? false,
                    activeSource = _unifiedCaptureManager?.ActiveSource.ToString() ?? "Unknown",
                    autoQueue = _integratedQueueManager?.AutoQueueEnabled ?? false,
                    autoConvert = _integratedQueueManager?.AutoConvertEnabled ?? false,
                    queueStats = _queueManager?.GetStats(),
                    serviceAvailable = _unifiedCaptureManager != null && _integratedQueueManager != null
                };
                
                await SendMessageToWebView(new
                {
                    action = "unifiedStatusResult",
                    data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unified status");
                await SendErrorToWebView($"Failed to get unified status: {ex.Message}");
            }
        }
    }
}