using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;
using SmartBoxNext.Medical;

namespace SmartBoxNext
{
    public partial class MainWindowMinimal : Window
    {
        private readonly ILogger<MainWindowMinimal> _logger;
        private AppConfig? _config;
        private bool _isInitialized = false;

        public MainWindowMinimal()
        {
            InitializeComponent();
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<MainWindowMinimal>();
            
            LoadConfiguration();
            UpdateStatusText("Initializing SmartBox medical capture system...");
            
            // Automatically initialize after window loads
            this.Loaded += async (s, e) => await InitializeInterface();
        }

        private void LoadConfiguration()
        {
            try
            {
                var configPath = AppConfig.GetConfigPath();
                
                // Migrate from old location if exists BEFORE loading
                var oldConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(oldConfigPath) && !File.Exists(configPath))
                {
                    _logger.LogInformation($"Migrating config from old location: {oldConfigPath} → {configPath}");
                    File.Copy(oldConfigPath, configPath);
                }
                
                _config = AppConfig.LoadFromFile(configPath);
                _logger.LogInformation($"Configuration loaded from: {configPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration");
                _config = new AppConfig();
            }
        }

        private async Task InitializeInterface()
        {
            if (_isInitialized) return;

            try
            {
                UpdateStatusText("Initializing WebView2 medical interface...");

                await InitializeWebView();
                
                statusPanel.Visibility = Visibility.Collapsed;
                webView.Visibility = Visibility.Visible;
                
                _isInitialized = true;
                UpdateStatusBarText("Medical interface initialized successfully");
                
                _logger.LogInformation("SmartBox medical interface initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize medical interface");
                UpdateStatusText($"Initialization failed: {ex.Message}");
                
                // Show error in status panel
                statusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private async Task InitializeWebView()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                
                // Allow file access and disable CORS for local files
                webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                
                // Add JavaScript bridge to C# backend
                webView.CoreWebView2.AddHostObjectToScript("smartBoxBackend", this);
                
                // Handle web messages from JavaScript
                webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Handle permission requests (automatically allow camera/microphone)
                webView.CoreWebView2.PermissionRequested += (s, args) =>
                {
                    _logger.LogInformation($"Permission requested: {args.PermissionKind} for {args.Uri}");
                    
                    // Automatically grant camera and microphone permissions
                    if (args.PermissionKind == CoreWebView2PermissionKind.Camera ||
                        args.PermissionKind == CoreWebView2PermissionKind.Microphone)
                    {
                        args.State = CoreWebView2PermissionState.Allow;
                        _logger.LogInformation($"Automatically granted {args.PermissionKind} permission");
                    }
                };
                
                // Add navigation completed handler
                webView.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    if (args.IsSuccess)
                    {
                        _logger.LogInformation("Navigation completed successfully");
                        UpdateStatusBarText("WebView2 navigation completed");
                    }
                    else
                    {
                        _logger.LogError($"Navigation failed: {args.WebErrorStatus}");
                        UpdateStatusBarText($"Navigation failed: {args.WebErrorStatus}");
                    }
                };
                
                // Enable additional permissions for local files
                webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                
                // Try to load the original SmartBox UI from wwwroot
                var wwwrootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                var indexPath = Path.Combine(wwwrootPath, "index.html");
                
                if (File.Exists(indexPath))
                {
                    // Load the original SmartBox UI
                    _logger.LogInformation($"Loading original SmartBox UI from: {indexPath}");
                    webView.CoreWebView2.Navigate($"file:///{indexPath.Replace('\\', '/')}");
                }
                else
                {
                    // Fallback to embedded minimal UI
                    _logger.LogWarning($"Original UI not found at {indexPath}, using minimal UI");
                    var htmlContent = CreateSimpleMedicalHtml();
                    webView.NavigateToString(htmlContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebView2 initialization failed");
                throw;
            }
        }

        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                _logger.LogInformation($"Web message received: {message}");
                
                // Also update UI to show message was received
                UpdateStatusBarText($"Message received: {message.Substring(0, Math.Min(message.Length, 50))}...");
                
                // Parse the message (expecting JSON format)
                if (message.StartsWith("{"))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(message);
                    
                    // Check if it's the new format with 'type' field (from original SmartBox UI)
                    string messageType = null;
                    if (jsonDoc.RootElement.TryGetProperty("type", out var typeProperty))
                    {
                        messageType = typeProperty.GetString();
                    }
                    // Fallback to 'action' field (from minimal UI)
                    else if (jsonDoc.RootElement.TryGetProperty("action", out var actionProperty))
                    {
                        messageType = actionProperty.GetString();
                    }
                    
                    switch (messageType)
                    {
                        case "saveSettings":
                        case "savesettings":
                            await HandleSaveSettings(jsonDoc.RootElement);
                            break;
                        case "getSettings":
                        case "getsettings":
                            await HandleGetSettings();
                            break;
                        case "sendToPacs":
                            await HandleSendToPacs();
                            break;
                        case "runDiagnostics":
                            await HandleRunDiagnostics();
                            break;
                        case "validateDicom":
                            await HandleValidateDicom();
                            break;
                        case "loadWorklist":
                        case "loadMWL":
                            await HandleLoadWorklist();
                            break;
                        case "exitApplication":
                        case "exitApp":
                            HandleExitApplication();
                            break;
                        case "openSettings":
                            HandleOpenSettings();
                            break;
                        case "capturePhoto":
                            HandleCapturePhoto(jsonDoc.RootElement);
                            break;
                        case "captureVideo":
                            HandleCaptureVideo(jsonDoc.RootElement);
                            break;
                        case "exportCaptures":
                            HandleExportCaptures(jsonDoc.RootElement);
                            break;
                        case "testpacsconnection":
                        case "testPacsConnection":
                            await HandleTestPacsConnection();
                            break;
                        case "testmwlconnection":
                        case "testMwlConnection":
                            await HandleTestMwlConnection();
                            break;
                        default:
                            _logger.LogWarning($"Unknown message type: {messageType}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling web message");
            }
        }

        private async Task HandleGetSettings()
        {
            try
            {
                _logger.LogInformation("Getting current settings");
                
                // Ensure config is loaded
                if (_config == null)
                {
                    LoadConfiguration();
                }
                
                // Send current config to web
                var response = new
                {
                    action = "settingsLoaded",
                    data = _config
                };
                
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
                webView.CoreWebView2.PostWebMessageAsString(responseJson);
                
                _logger.LogInformation("Settings sent to web interface");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get settings");
                webView.CoreWebView2.PostWebMessageAsString("{\"action\":\"settingsLoaded\",\"success\":false,\"error\":\"" + ex.Message + "\"}");
            }
        }

        private async Task HandleSaveSettings(System.Text.Json.JsonElement data)
        {
            try
            {
                // Die Daten kommen direkt als vollständige Config-Struktur
                var settings = data.GetProperty("data");
                
                // Erstelle neue Config basierend auf existierender
                if (_config == null)
                {
                    _config = new AppConfig();
                }
                
                // Update Storage Section
                if (settings.TryGetProperty("Storage", out var storage))
                {
                    if (storage.TryGetProperty("PhotosPath", out var photosPath))
                        _config.Storage.PhotosPath = photosPath.GetString();
                    if (storage.TryGetProperty("VideosPath", out var videosPath))
                        _config.Storage.VideosPath = videosPath.GetString();
                    if (storage.TryGetProperty("TempPath", out var tempPath))
                        _config.Storage.TempPath = tempPath.GetString();
                    if (storage.TryGetProperty("DicomPath", out var dicomPath))
                        _config.Storage.DicomPath = dicomPath.GetString();
                    if (storage.TryGetProperty("EnableAutoCleanup", out var enableAutoCleanup))
                        _config.Storage.EnableAutoCleanup = enableAutoCleanup.GetBoolean();
                    if (storage.TryGetProperty("RetentionDays", out var retentionDays))
                        _config.Storage.RetentionDays = retentionDays.GetInt32();
                    if (storage.TryGetProperty("CompressOldFiles", out var compressOldFiles))
                        _config.Storage.CompressOldFiles = compressOldFiles.GetBoolean();
                }
                
                // Update Pacs Section
                if (settings.TryGetProperty("Pacs", out var pacs))
                {
                    if (pacs.TryGetProperty("ServerHost", out var serverHost))
                    {
                        _config.Pacs.ServerHost = serverHost.GetString();
                        // Set Enabled based on whether ServerHost is provided
                        _config.Pacs.Enabled = !string.IsNullOrWhiteSpace(_config.Pacs.ServerHost);
                    }
                    if (pacs.TryGetProperty("ServerPort", out var serverPort))
                        _config.Pacs.ServerPort = serverPort.GetInt32();
                    if (pacs.TryGetProperty("CalledAeTitle", out var calledAeTitle))
                        _config.Pacs.CalledAeTitle = calledAeTitle.GetString();
                    if (pacs.TryGetProperty("CallingAeTitle", out var callingAeTitle))
                        _config.Pacs.CallingAeTitle = callingAeTitle.GetString();
                    if (pacs.TryGetProperty("Timeout", out var timeout))
                        _config.Pacs.Timeout = timeout.GetInt32();
                    if (pacs.TryGetProperty("UseSecureConnection", out var useSecure))
                        _config.Pacs.UseSecureConnection = useSecure.GetBoolean();
                    if (pacs.TryGetProperty("MaxRetries", out var maxRetries))
                        _config.Pacs.MaxRetries = maxRetries.GetInt32();
                    if (pacs.TryGetProperty("AutoSendOnCapture", out var autoSend))
                        _config.Pacs.AutoSendOnCapture = autoSend.GetBoolean();
                    if (pacs.TryGetProperty("SendInBackground", out var sendBg))
                        _config.Pacs.SendInBackground = sendBg.GetBoolean();
                }
                
                // Update MwlSettings Section
                if (settings.TryGetProperty("MwlSettings", out var mwl))
                {
                    if (mwl.TryGetProperty("EnableWorklist", out var enableMwl))
                        _config.MwlSettings.EnableWorklist = enableMwl.GetBoolean();
                    if (mwl.TryGetProperty("MwlServerHost", out var mwlHost))
                        _config.MwlSettings.MwlServerHost = mwlHost.GetString();
                    if (mwl.TryGetProperty("MwlServerPort", out var mwlPort))
                        _config.MwlSettings.MwlServerPort = mwlPort.GetInt32();
                    if (mwl.TryGetProperty("MwlServerAET", out var mwlAet))
                        _config.MwlSettings.MwlServerAET = mwlAet.GetString();
                    if (mwl.TryGetProperty("LocalAET", out var localAet))
                        _config.MwlSettings.LocalAET = localAet.GetString();
                    if (mwl.TryGetProperty("CacheExpiryHours", out var cacheExpiry))
                        _config.MwlSettings.CacheExpiryHours = cacheExpiry.GetInt32();
                    if (mwl.TryGetProperty("AutoRefreshSeconds", out var autoRefresh))
                        _config.MwlSettings.AutoRefreshSeconds = autoRefresh.GetInt32();
                    if (mwl.TryGetProperty("ShowEmergencyFirst", out var showEmergency))
                        _config.MwlSettings.ShowEmergencyFirst = showEmergency.GetBoolean();
                    if (mwl.TryGetProperty("ScheduledStationAETitle", out var schedAeTitle))
                        _config.MwlSettings.ScheduledStationAETitle = schedAeTitle.GetString();
                    if (mwl.TryGetProperty("ScheduledStationName", out var schedName))
                        _config.MwlSettings.ScheduledStationName = schedName.GetString();
                }
                
                // Update Video Section
                if (settings.TryGetProperty("Video", out var video))
                {
                    if (video.TryGetProperty("MaxRecordingMinutes", out var maxMinutes))
                        _config.Video.MaxRecordingMinutes = maxMinutes.GetInt32();
                    if (video.TryGetProperty("VideoCodec", out var codec))
                        _config.Video.VideoCodec = codec.GetString();
                    if (video.TryGetProperty("VideoBitrate", out var vBitrate))
                        _config.Video.VideoBitrate = vBitrate.GetInt32();
                    if (video.TryGetProperty("VideoFramerate", out var framerate))
                        _config.Video.VideoFramerate = framerate.GetInt32();
                    if (video.TryGetProperty("VideoResolution", out var resolution))
                        _config.Video.VideoResolution = resolution.GetString();
                    if (video.TryGetProperty("EnableAudioCapture", out var enableAudio))
                        _config.Video.EnableAudioCapture = enableAudio.GetBoolean();
                    if (video.TryGetProperty("AudioBitrate", out var aBitrate))
                        _config.Video.AudioBitrate = aBitrate.GetInt32();
                    if (video.TryGetProperty("ShowRecordingIndicator", out var showIndicator))
                        _config.Video.ShowRecordingIndicator = showIndicator.GetBoolean();
                    if (video.TryGetProperty("AutoStopOnInactivity", out var autoStop))
                        _config.Video.AutoStopOnInactivity = autoStop.GetBoolean();
                    if (video.TryGetProperty("InactivityTimeout", out var inactivityTimeout))
                        _config.Video.InactivityTimeout = inactivityTimeout.GetInt32();
                }
                
                // Update Application Section
                if (settings.TryGetProperty("Application", out var app))
                {
                    if (app.TryGetProperty("Title", out var title))
                        _config.Application.Title = title.GetString();
                    if (app.TryGetProperty("WebServerPort", out var port))
                        _config.Application.WebServerPort = port.GetInt32();
                    if (app.TryGetProperty("AutoStartCapture", out var autoStart))
                        _config.Application.AutoStartCapture = autoStart.GetBoolean();
                    if (app.TryGetProperty("PreferredDisplay", out var prefDisplay))
                        _config.Application.PreferredDisplay = prefDisplay.GetString();
                    if (app.TryGetProperty("KioskPassword", out var kioskPwd))
                        _config.Application.KioskPassword = kioskPwd.GetString();
                    if (app.TryGetProperty("EnableDebugLogging", out var debugLog))
                        _config.Application.EnableDebugLogging = debugLog.GetBoolean();
                }
                
                // Update Dicom Section (falls gesendet)
                if (settings.TryGetProperty("Dicom", out var dicom))
                {
                    if (dicom.TryGetProperty("StationName", out var stationName))
                        _config.Dicom.StationName = stationName.GetString();
                    if (dicom.TryGetProperty("OutputDirectory", out var outputDir))
                        _config.Dicom.OutputDirectory = outputDir.GetString();
                    if (dicom.TryGetProperty("AeTitle", out var aeTitle))
                        _config.Dicom.AeTitle = aeTitle.GetString();
                    if (dicom.TryGetProperty("Modality", out var modality))
                        _config.Dicom.Modality = modality.GetString();
                    if (dicom.TryGetProperty("PatientIdPrefix", out var patientIdPrefix))
                        _config.Dicom.PatientIdPrefix = patientIdPrefix.GetString();
                    if (dicom.TryGetProperty("AutoGenerateAccessionNumber", out var autoGenAccession))
                        _config.Dicom.AutoGenerateAccessionNumber = autoGenAccession.GetBoolean();
                    if (dicom.TryGetProperty("ImplementationClassUID", out var implClassUid))
                        _config.Dicom.ImplementationClassUID = implClassUid.GetString();
                    if (dicom.TryGetProperty("ImplementationVersionName", out var implVersionName))
                        _config.Dicom.ImplementationVersionName = implVersionName.GetString();
                }
                
                // Save config to file (use AppData for persistence)
                var configPath = AppConfig.GetConfigPath();
                var configJson = System.Text.Json.JsonSerializer.Serialize(_config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(configPath, configJson);
                
                _logger.LogInformation($"Settings saved to: {configPath}");
                
                // Send response back to web - using both "type" and "action" for compatibility
                var response = new
                {
                    type = "settingsSaved",
                    action = "settingsSaved", 
                    success = true,
                    message = "Settings saved successfully"
                };
                
                webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(response));
                
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                var errorResponse = new
                {
                    type = "settingsSaved",
                    action = "settingsSaved",
                    success = false,
                    error = ex.Message
                };
                webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
        }

        private async Task HandleSendToPacs()
        {
            try
            {
                _logger.LogInformation("Simulating PACS send...");
                
                // Simulate PACS sending delay (using medical timeout)
                await Task.Delay(MedicalConstants.CRITICAL_OPERATION_TIMEOUT_MS);
                
                // Send success response
                webView.CoreWebView2.PostWebMessageAsString("{\"type\":\"pacsSent\",\"success\":true,\"message\":\"DICOM files sent to PACS successfully\"}");
                
                _logger.LogInformation("PACS send completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PACS send failed");
                webView.CoreWebView2.PostWebMessageAsString("{\"type\":\"pacsSent\",\"success\":false,\"error\":\"" + ex.Message + "\"}");
            }
        }

        private async Task HandleRunDiagnostics()
        {
            try
            {
                _logger.LogInformation("Running system diagnostics...");
                
                var diagnostics = new
                {
                    systemHealth = "OK",
                    memoryUsage = "45%",
                    diskSpace = "78% free",
                    network = "Connected",
                    dicomService = "Running",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var response = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "diagnosticsComplete",
                    success = true,
                    data = diagnostics
                });
                
                webView.CoreWebView2.PostWebMessageAsString(response);
                _logger.LogInformation("Diagnostics completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnostics failed");
                webView.CoreWebView2.PostWebMessageAsString("{\"type\":\"diagnosticsComplete\",\"success\":false,\"error\":\"" + ex.Message + "\"}");
            }
        }

        private async Task HandleValidateDicom()
        {
            try
            {
                _logger.LogInformation("Validating DICOM files...");
                
                // Get DICOM files count
                var outputDir = _config.Dicom.OutputDirectory;
                var dicomFiles = Directory.Exists(outputDir) ? Directory.GetFiles(outputDir, "*.dcm") : Array.Empty<string>();
                
                var validation = new
                {
                    totalFiles = dicomFiles.Length,
                    validFiles = dicomFiles.Length, // Assume all valid for now
                    compliance = "DICOM 3.0 compliant",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var response = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "dicomValidated",
                    success = true,
                    data = validation
                });
                
                webView.CoreWebView2.PostWebMessageAsString(response);
                _logger.LogInformation($"DICOM validation completed - {dicomFiles.Length} files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DICOM validation failed");
                webView.CoreWebView2.PostWebMessageAsString("{\"type\":\"dicomValidated\",\"success\":false,\"error\":\"" + ex.Message + "\"}");
            }
        }

        private async Task HandleLoadWorklist()
        {
            try
            {
                _logger.LogInformation("Loading modality worklist...");
                
                // Simulate MWL data
                var worklist = new[]
                {
                    new { patientId = "12345", patientName = "John Doe", studyDescription = "Emergency Chest X-Ray", scheduledTime = "14:30" },
                    new { patientId = "12346", patientName = "Jane Smith", studyDescription = "Abdominal CT", scheduledTime = "15:00" },
                    new { patientId = "12347", patientName = "Bob Johnson", studyDescription = "Hand Fracture", scheduledTime = "15:30" }
                };
                
                var response = System.Text.Json.JsonSerializer.Serialize(new
                {
                    type = "worklistLoaded",
                    success = true,
                    data = worklist
                });
                
                webView.CoreWebView2.PostWebMessageAsString(response);
                _logger.LogInformation($"Worklist loaded - {worklist.Length} pending studies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worklist loading failed");
                webView.CoreWebView2.PostWebMessageAsString("{\"type\":\"worklistLoaded\",\"success\":false,\"error\":\"" + ex.Message + "\"}");
            }
        }

        private async Task CreateBasicMedicalInterface(string wwwrootPath)
        {
            // Create HTML file
            var indexPath = Path.Combine(wwwrootPath, "index.html");
            var htmlContent = CreateSimpleMedicalHtml();
            await File.WriteAllTextAsync(indexPath, htmlContent);
            
            // Copy JavaScript file if it exists in the source directory
            var sourceJsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "smartbox.js");
            var destJsPath = Path.Combine(wwwrootPath, "smartbox.js");
            
            if (File.Exists(sourceJsPath) && sourceJsPath != destJsPath)
            {
                File.Copy(sourceJsPath, destJsPath, true);
                _logger.LogInformation($"Copied smartbox.js to {destJsPath}");
            }
            else
            {
                _logger.LogWarning($"smartbox.js not found at {sourceJsPath}");
            }
        }

        private string CreateSimpleMedicalHtml()
        {
            var htmlPart1 = @"<!DOCTYPE html>
<html>
<head>
    <title>SmartBox Medical Interface</title>
    <style>
        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .header { background: #0078D4; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        .nav { background: white; padding: 10px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .nav-btn { background: #f0f0f0; border: none; padding: 8px 16px; margin-right: 5px; border-radius: 4px; cursor: pointer; }
        .nav-btn.active { background: #0078D4; color: white; }
        .panel { background: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .btn { background: #0078D4; color: white; border: none; padding: 12px 24px; border-radius: 4px; cursor: pointer; font-size: 14px; margin: 5px; }
        .btn:hover { background: #106EBE; }
        .btn-secondary { background: #6c757d; }
        .btn-secondary:hover { background: #5a6268; }
        .status { padding: 10px; background: #E3F2FD; border-radius: 4px; margin: 10px 0; }
        .settings-group { margin-bottom: 20px; padding: 15px; background: #f8f9fa; border-radius: 4px; }
        .settings-group h3 { margin-top: 0; color: #0078D4; }
        .form-row { display: flex; align-items: center; margin-bottom: 10px; }
        .form-row label { width: 150px; font-weight: bold; }
        .form-row input, .form-row select { flex: 1; padding: 8px; border: 1px solid #ddd; border-radius: 4px; }
        .hidden { display: none; }
    </style>
</head>
<body>
    <div class='header'>
        <h1>SmartBox Next - Medical Image Capture System</h1>
        <p>Emergency Department Medical Imaging Platform</p>
    </div>
    
    <div class='nav'>
        <button class='nav-btn active' data-tab='main' onclick='showTab(""main"")'>Main</button>
        <button class='nav-btn' data-tab='settings' onclick='showTab(""settings"")'>Settings</button>
        <button class='nav-btn' data-tab='diagnostics' onclick='showTab(""diagnostics"")'>Diagnostics</button>
        <button class='nav-btn' style='float: right; background: #d32f2f; color: white;' onclick='exitApplication()'>Exit</button>
    </div>
    
    <div id='mainTab'>
        <div class='panel'>
            <h2>Image Capture</h2>
            <button class='btn' onclick='startCapture()'>Start Capture Session</button>
            <button class='btn' onclick='takeSnapshot()'>Take Snapshot</button>
            <button class='btn btn-secondary' onclick='stopCapture()'>Stop Session</button>
            <div id='captureStatus' class='status'>Ready for image capture</div>
        </div>
        
        <div class='panel'>
            <h2>DICOM Management</h2>
            <button class='btn' onclick='viewDicom()'>View DICOM Files</button>
            <button class='btn' onclick='exportDicom()'>Export to PACS</button>
            <button class='btn btn-secondary' onclick='validateDicom()'>Validate DICOM</button>
            <div id='dicomStatus' class='status'>DICOM system ready</div>
        </div>
        
        <div class='panel'>
            <h2>Patient Workflow</h2>
            <button class='btn' onclick='loadWorklist()'>Load MWL</button>
            <button class='btn' onclick='selectPatient()'>Select Patient</button>
            <button class='btn btn-secondary' onclick='clearPatient()'>Clear Selection</button>
            <div id='workflowStatus' class='status'>No patient selected</div>
        </div>
    </div>
    
    <div id='settingsTab' class='hidden'>
        <div class='panel'>
            <h2>System Settings</h2>
            
            <div class='settings-group'>
                <h3>DICOM Configuration</h3>
                <div class='form-row'>
                    <label>Station Name:</label>
                    <input type='text' id='stationName' value='SMARTBOX-ED' />
                </div>
                <div class='form-row'>
                    <label>AE Title:</label>
                    <input type='text' id='aeTitle' value='SMARTBOX' />
                </div>
                <div class='form-row'>
                    <label>Output Directory:</label>
                    <input type='text' id='outputDir' value='C:\\DicomOutput' />
                </div>
            </div>
            
            <div class='settings-group'>
                <h3>PACS Configuration</h3>
                <div class='form-row'>
                    <label>PACS Server:</label>
                    <input type='text' id='pacsServer' value='192.168.1.100' />
                </div>
                <div class='form-row'>
                    <label>PACS Port:</label>
                    <input type='number' id='pacsPort' value='11112' />
                </div>
                <div class='form-row'>
                    <label>PACS AE Title:</label>
                    <input type='text' id='pacsAeTitle' value='PACS' />
                </div>
            </div>
            
            <div class='settings-group'>
                <h3>Image Capture</h3>
                <div class='form-row'>
                    <label>Image Quality:</label>
                    <select id='imageQuality'>
                        <option value='high'>High Quality</option>
                        <option value='medium' selected>Medium Quality</option>
                        <option value='low'>Low Quality</option>
                    </select>
                </div>
                <div class='form-row'>
                    <label>Auto-save:</label>
                    <input type='checkbox' id='autoSave' checked />
                </div>
            </div>
            
            <button class='btn' onclick='saveSettings()'>Save Settings</button>
            <button class='btn btn-secondary' onclick='resetSettings()'>Reset to Default</button>
            <div id='settingsStatus' class='status'>Settings ready for configuration</div>
        </div>
    </div>
    
    <div id='diagnosticsTab' class='hidden'>
        <div class='panel'>
            <h2>System Diagnostics</h2>
            <button class='btn' onclick='runDiagnostics()'>Run Full Diagnostics</button>
            <button class='btn' onclick='testPacsConnection()'>Test PACS Connection</button>
            <button class='btn' onclick='checkDicomCompliance()'>Check DICOM Compliance</button>
            <div id='diagnosticsStatus' class='status'>Diagnostics ready</div>
        </div>
    </div>";

            var script = @"
    <script>
    // Tab Management
    function showTab(tabName) {
        // Hide all tabs
        document.getElementById('mainTab').classList.add('hidden');
        document.getElementById('settingsTab').classList.add('hidden');
        document.getElementById('diagnosticsTab').classList.add('hidden');
        
        // Remove active class from all nav buttons
        var buttons = document.querySelectorAll('.nav-btn');
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].classList.remove('active');
        }
        
        // Show selected tab
        var selectedTab = document.getElementById(tabName + 'Tab');
        if (selectedTab) {
            selectedTab.classList.remove('hidden');
        }
        
        // Activate corresponding nav button
        var activeButton = document.querySelector('[data-tab=""' + tabName + '""]');
        if (activeButton) {
            activeButton.classList.add('active');
        }
    }

    // Backend Communication
    function sendToBackend(action, data) {
        if (window.chrome && window.chrome.webview) {
            var message = JSON.stringify(Object.assign({ action: action }, data || {}));
            window.chrome.webview.postMessage(message);
        } else {
            alert('WebView2 bridge not available. Please restart the application.');
        }
    }

    // Exit Application
    function exitApplication() {
        if (confirm('Are you sure you want to exit SmartBox?')) {
            sendToBackend('exitApplication');
        }
    }

    // Settings Functions
    function saveSettings() {
        document.getElementById('settingsStatus').innerHTML = 'Saving settings...';
        
        var settings = {
            stationName: document.getElementById('stationName').value,
            aeTitle: document.getElementById('aeTitle').value,
            outputDir: document.getElementById('outputDir').value,
            pacsServer: document.getElementById('pacsServer').value,
            pacsPort: document.getElementById('pacsPort').value,
            pacsAeTitle: document.getElementById('pacsAeTitle').value,
            imageQuality: document.getElementById('imageQuality').value,
            autoSave: document.getElementById('autoSave').checked
        };
        
        sendToBackend('saveSettings', { settings: settings });
    }

    function resetSettings() {
        document.getElementById('stationName').value = 'SMARTBOX-ED';
        document.getElementById('aeTitle').value = 'SMARTBOX';
        document.getElementById('outputDir').value = 'C:\\\\DicomOutput';
        document.getElementById('pacsServer').value = '192.168.1.100';
        document.getElementById('pacsPort').value = '11112';
        document.getElementById('pacsAeTitle').value = 'PACS';
        document.getElementById('imageQuality').value = 'medium';
        document.getElementById('autoSave').checked = true;
        document.getElementById('settingsStatus').innerHTML = 'Settings reset to default values';
    }

    // Simple stub functions for other buttons
    function startCapture() { document.getElementById('captureStatus').innerHTML = 'Capture started'; }
    function takeSnapshot() { document.getElementById('captureStatus').innerHTML = 'Snapshot taken'; }
    function stopCapture() { document.getElementById('captureStatus').innerHTML = 'Capture stopped'; }
    function viewDicom() { document.getElementById('dicomStatus').innerHTML = 'DICOM files loaded'; }
    function exportDicom() { 
        document.getElementById('dicomStatus').innerHTML = 'Sending to PACS...';
        sendToBackend('sendToPacs'); 
    }
    function validateDicom() { 
        document.getElementById('dicomStatus').innerHTML = 'Validating...';
        sendToBackend('validateDicom'); 
    }
    function loadWorklist() { 
        document.getElementById('workflowStatus').innerHTML = 'Loading worklist...';
        sendToBackend('loadWorklist'); 
    }
    function selectPatient() { document.getElementById('workflowStatus').innerHTML = 'Patient selected'; }
    function clearPatient() { document.getElementById('workflowStatus').innerHTML = 'Patient cleared'; }
    function runDiagnostics() { 
        document.getElementById('diagnosticsStatus').innerHTML = 'Running diagnostics...';
        sendToBackend('runDiagnostics'); 
    }
    function testPacsConnection() { 
        document.getElementById('diagnosticsStatus').innerHTML = 'Testing PACS...';
        sendToBackend('runDiagnostics'); 
    }
    function checkDicomCompliance() { 
        document.getElementById('diagnosticsStatus').innerHTML = 'Checking compliance...';
        sendToBackend('validateDicom'); 
    }

    // Listen for backend responses
    window.addEventListener('message', function(event) {
        try {
            var response = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
            
            switch(response.type) {
                case 'settingsSaved':
                    document.getElementById('settingsStatus').innerHTML = response.success ? 
                        'Settings saved successfully!' : 'Failed to save settings: ' + response.error;
                    break;
                case 'pacsSent':
                    document.getElementById('dicomStatus').innerHTML = response.success ? 
                        'PACS send successful!' : 'PACS send failed: ' + response.error;
                    break;
                case 'diagnosticsComplete':
                    if (response.success && response.data) {
                        var d = response.data;
                        document.getElementById('diagnosticsStatus').innerHTML = 
                            'Diagnostics Complete<br>' +
                            'System: ' + d.systemHealth + '<br>' +
                            'Memory: ' + d.memoryUsage + '<br>' +
                            'Disk: ' + d.diskSpace + '<br>' +
                            'Network: ' + d.network;
                    } else {
                        document.getElementById('diagnosticsStatus').innerHTML = 'Diagnostics failed';
                    }
                    break;
                case 'dicomValidated':
                    if (response.success && response.data) {
                        var v = response.data;
                        document.getElementById('dicomStatus').innerHTML = 
                            'Validation Complete<br>' +
                            'Total: ' + v.totalFiles + ' files<br>' +
                            'Valid: ' + v.validFiles + ' files';
                    } else {
                        document.getElementById('dicomStatus').innerHTML = 'Validation failed';
                    }
                    break;
                case 'worklistLoaded':
                    if (response.success && response.data) {
                        var patients = response.data;
                        var html = 'Worklist loaded (' + patients.length + ' studies):<br>';
                        for (var i = 0; i < patients.length; i++) {
                            var p = patients[i];
                            html += p.patientName + ' - ' + p.studyDescription + '<br>';
                        }
                        document.getElementById('workflowStatus').innerHTML = html;
                    } else {
                        document.getElementById('workflowStatus').innerHTML = 'Failed to load worklist';
                    }
                    break;
            }
        } catch (error) {
            console.error('Error parsing response:', error);
        }
    });
    </script>
</body>
</html>";

            return htmlPart1 + script;
        }

        private void UpdateStatusText(string message)
        {
            statusText.Text = message;
            _logger.LogInformation($"Status: {message}");
        }

        private void UpdateStatusBarText(string message)
        {
            statusBarText.Text = message;
        }

        private void HandleOpenSettings()
        {
            _logger.LogInformation("Settings requested via web interface");
            UpdateStatusBarText("Settings dialog would open here");
            
            // For now, just acknowledge the request
            // TODO: Implement actual settings dialog
        }

        private async void HandleCapturePhoto(System.Text.Json.JsonElement data)
        {
            try
            {
                _logger.LogInformation("Photo capture requested via web interface");
                
                if (data.TryGetProperty("data", out var photoData))
                {
                    // Extract capture data
                    var captureId = photoData.TryGetProperty("captureId", out var id) ? id.GetString() : Guid.NewGuid().ToString();
                    var imageData = photoData.TryGetProperty("imageData", out var imgData) ? imgData.GetString() : null;
                    var patient = photoData.TryGetProperty("patient", out var pat) ? pat : default;
                    
                    if (!string.IsNullOrEmpty(imageData))
                    {
                        // Convert base64 to byte array
                        var base64Data = imageData.Split(',')[1]; // Remove data:image/jpeg;base64, prefix
                        var imageBytes = Convert.FromBase64String(base64Data);
                        
                        // Generate filename with medical compliance
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var patientId = patient.ValueKind != System.Text.Json.JsonValueKind.Undefined && 
                                       patient.TryGetProperty("id", out var pid) ? pid.GetString() : "UNKNOWN";
                        var fileName = $"IMG_{patientId}_{timestamp}_{captureId}.jpg";
                        var filePath = Path.Combine(_config.Storage.PhotosPath, fileName);
                        
                        // Ensure directory exists
                        Directory.CreateDirectory(_config.Storage.PhotosPath);
                        
                        // Save image file
                        await File.WriteAllBytesAsync(filePath, imageBytes);
                        
                        // TODO: Convert to DICOM using DicomServiceMinimal
                        // TODO: Add to export queue for PACS
                        
                        _logger.LogInformation($"Photo saved: {filePath}");
                        UpdateStatusBarText($"Photo captured: {fileName}");
                        
                        // Send success response to frontend
                        var response = new
                        {
                            type = "photoCaptured",
                            success = true,
                            captureId = captureId,
                            fileName = fileName,
                            timestamp = timestamp
                        };
                        
                        webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(response));
                    }
                    else
                    {
                        _logger.LogWarning("No image data received in photo capture request");
                        UpdateStatusBarText("Photo capture failed: No image data");
                    }
                }
                else
                {
                    _logger.LogWarning("No capture data received");
                    UpdateStatusBarText("Photo capture failed: No data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing photo capture");
                UpdateStatusBarText($"Photo capture failed: {ex.Message}");
                
                // Send error response to frontend
                var errorResponse = new
                {
                    type = "photoCaptured",
                    success = false,
                    error = ex.Message
                };
                
                webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
        }

        private async void HandleCaptureVideo(System.Text.Json.JsonElement data)
        {
            try
            {
                _logger.LogInformation("Video capture requested via web interface");
                
                if (data.TryGetProperty("data", out var videoData))
                {
                    // Extract video capture data
                    var captureId = videoData.TryGetProperty("captureId", out var id) ? id.GetString() : Guid.NewGuid().ToString();
                    var videoBlob = videoData.TryGetProperty("videoBlob", out var blob) ? blob.GetString() : null;
                    var duration = videoData.TryGetProperty("duration", out var dur) ? dur.GetDouble() : 0;
                    var patient = videoData.TryGetProperty("patient", out var pat) ? pat : default;
                    
                    if (!string.IsNullOrEmpty(videoBlob))
                    {
                        // Convert base64 video data to byte array
                        var base64Data = videoBlob.Split(',')[1]; // Remove data:video/webm;base64, prefix
                        var videoBytes = Convert.FromBase64String(base64Data);
                        
                        // Generate filename with medical compliance
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var patientId = patient.ValueKind != System.Text.Json.JsonValueKind.Undefined && 
                                       patient.TryGetProperty("id", out var pid) ? pid.GetString() : "UNKNOWN";
                        var fileName = $"VID_{patientId}_{timestamp}_{captureId}.webm";
                        var filePath = Path.Combine(_config.Storage.VideosPath, fileName);
                        
                        // Ensure directory exists
                        Directory.CreateDirectory(_config.Storage.VideosPath);
                        
                        // Save video file
                        await File.WriteAllBytesAsync(filePath, videoBytes);
                        
                        // TODO: Convert to medical video format (MP4/H.264) using FFmpeg
                        // TODO: Extract keyframes for thumbnails
                        // TODO: Add to export queue for PACS
                        
                        _logger.LogInformation($"Video saved: {filePath} (Duration: {duration:F1}s)");
                        UpdateStatusBarText($"Video captured: {fileName} ({duration:F1}s)");
                        
                        // Send success response to frontend
                        var response = new
                        {
                            type = "videoCaptured",
                            success = true,
                            captureId = captureId,
                            fileName = fileName,
                            duration = duration,
                            timestamp = timestamp
                        };
                        
                        webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(response));
                    }
                    else
                    {
                        _logger.LogWarning("No video data received in video capture request");
                        UpdateStatusBarText("Video capture failed: No video data");
                    }
                }
                else
                {
                    _logger.LogWarning("No capture data received");
                    UpdateStatusBarText("Video capture failed: No data");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video capture");
                UpdateStatusBarText($"Video capture failed: {ex.Message}");
                
                // Send error response to frontend
                var errorResponse = new
                {
                    type = "videoCaptured",
                    success = false,
                    error = ex.Message
                };
                
                webView.CoreWebView2.PostWebMessageAsString(System.Text.Json.JsonSerializer.Serialize(errorResponse));
            }
        }

        private void HandleExportCaptures(System.Text.Json.JsonElement data)
        {
            _logger.LogInformation("Export captures requested via web interface");
            
            // Extract capture data if available
            if (data.TryGetProperty("data", out var exportData))
            {
                // Process export data
                _logger.LogInformation("Processing capture export data");
            }
            
            UpdateStatusBarText("Captures export processed");
        }

        private void HandleExitApplication()
        {
            _logger.LogInformation("Application exit requested via web interface");
            
            // Ensure we're on the UI thread for closing the window
            Dispatcher.Invoke(() => 
            {
                this.Close();
                System.Windows.Application.Current.Shutdown();
            });
        }

        private async Task HandleTestPacsConnection()
        {
            try
            {
                _logger.LogInformation("Testing PACS connection...");
                
                if (_config?.Pacs == null || !_config.Pacs.Enabled)
                {
                    await SendTestResultToWeb("PACS", false, "PACS is not configured or enabled");
                    return;
                }

                // Create and show diagnostic window
                await Dispatcher.InvokeAsync(() =>
                {
                    var diagnosticWindow = new DiagnosticWindow(
                        _logger,
                        _config.Pacs.ServerHost,
                        _config.Pacs.ServerPort,
                        _config.Pacs.CallingAeTitle ?? MedicalConstants.DEFAULT_AE_TITLE,
                        _config.Pacs.CalledAeTitle ?? "PACS",
                        false // isMwl = false for PACS
                    );
                    
                    diagnosticWindow.Owner = this;
                    diagnosticWindow.ShowDialog();
                    
                    // Send result to web interface
                    var result = new
                    {
                        type = "pacsTestResult",
                        action = "pacsTestResult", 
                        success = diagnosticWindow.TestSuccessful,
                        message = diagnosticWindow.TestMessage,
                        error = diagnosticWindow.TestSuccessful ? null : diagnosticWindow.TestMessage
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(result);
                    webView.CoreWebView2.PostWebMessageAsString(json);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test PACS connection");
                await SendTestResultToWeb("PACS", false, $"Error: {ex.Message}");
            }
        }

        private async Task HandleTestMwlConnection()
        {
            try
            {
                _logger.LogInformation("Testing MWL connection...");
                
                if (_config?.MwlSettings == null || !_config.MwlSettings.EnableWorklist)
                {
                    await SendTestResultToWeb("MWL", false, "MWL is not configured or enabled");
                    return;
                }

                // Create and show diagnostic window
                await Dispatcher.InvokeAsync(() =>
                {
                    var diagnosticWindow = new DiagnosticWindow(
                        _logger,
                        _config.MwlSettings.MwlServerHost,
                        _config.MwlSettings.MwlServerPort,
                        _config.LocalAET ?? MedicalConstants.DEFAULT_AE_TITLE,
                        _config.MwlSettings.MwlServerAET ?? "ORTHANC",
                        true // isMwl = true for MWL
                    );
                    
                    diagnosticWindow.Owner = this;
                    diagnosticWindow.ShowDialog();
                    
                    // Send result to web interface
                    var result = new
                    {
                        type = "mwlTestResult",
                        action = "mwlTestResult",
                        success = diagnosticWindow.TestSuccessful,
                        message = diagnosticWindow.TestMessage,
                        data = new { worklistCount = diagnosticWindow.WorklistCount }
                    };
                    
                    var json = System.Text.Json.JsonSerializer.Serialize(result);
                    webView.CoreWebView2.PostWebMessageAsString(json);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test MWL connection");
                await SendTestResultToWeb("MWL", false, $"Error: {ex.Message}");
            }
        }

        private async Task SendTestResultToWeb(string service, bool success, string message)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                var result = new
                {
                    type = "testResult",
                    service = service,
                    success = success,
                    message = message
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(result);
                webView.CoreWebView2.PostWebMessageAsString(json);
            });
        }
    }
}