using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using System.IO;

namespace SmartBoxNext
{
    public sealed partial class SettingsWindow : Window
    {
        private AppConfig _config;
        private AppConfig _originalConfig;
        private Dictionary<string, (string Title, string Content)> _helpTexts;
        
        public SettingsWindow(AppConfig config)
        {
            this.InitializeComponent();
            this.Title = "SmartBox Settings";
            
            _config = config;
            _originalConfig = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(
                System.Text.Json.JsonSerializer.Serialize(config));
            
            InitializeHelpTexts();
            LoadConfigToUI();
            
            // Set window size
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1000, Height = 700 });
            
            // Show default help
            ShowHelp("Welcome", "Select any field to see detailed help information.\n\nSettings are organized into collapsible sections. Click the arrows to expand/collapse each section.");
        }
        
        private void InitializeHelpTexts()
        {
            _helpTexts = new Dictionary<string, (string Title, string Content)>
            {
                ["PhotosPath"] = ("Photos Path", 
                    "Directory where captured photos are saved.\n\n" +
                    "• Use relative paths (./Data/Photos) for portability\n" +
                    "• Use absolute paths (C:\\Photos) for fixed installations\n" +
                    "• Directory will be created if it doesn't exist"),
                    
                ["VideosPath"] = ("Videos Path", 
                    "Directory where recorded videos are saved.\n\n" +
                    "• Separate from photos for easier management\n" +
                    "• Consider disk space for video storage\n" +
                    "• Network paths supported for shared storage"),
                    
                ["DicomPath"] = ("DICOM Path", 
                    "Directory for DICOM file exports.\n\n" +
                    "• Used when exporting to DICOM format\n" +
                    "• Can be on network drive for PACS integration\n" +
                    "• Follows DICOM file naming conventions"),
                    
                ["UseRelativePaths"] = ("Relative Paths", 
                    "Enable portable mode with relative paths.\n\n" +
                    "• Checked: Paths relative to app folder (./Data/...)\n" +
                    "• Unchecked: Absolute paths (C:\\...)\n" +
                    "• Portable mode allows running from USB"),
                    
                ["MaxStorageMB"] = ("Maximum Storage", 
                    "Maximum disk space for media storage in MB.\n\n" +
                    "• Oldest files deleted when limit reached\n" +
                    "• Prevents disk full errors\n" +
                    "• Set based on available disk space\n" +
                    "• 10240 MB = 10 GB"),
                    
                ["RetentionDays"] = ("Retention Days", 
                    "Automatic cleanup of old files.\n\n" +
                    "• Files older than this are deleted\n" +
                    "• Helps comply with data retention policies\n" +
                    "• Set to 0 to disable auto-cleanup\n" +
                    "• Cleanup runs on app startup"),
                    
                ["AeTitle"] = ("AE Title", 
                    "Application Entity Title for DICOM.\n\n" +
                    "• Identifies this device to PACS\n" +
                    "• Maximum 16 characters\n" +
                    "• Usually uppercase letters/numbers\n" +
                    "• Must be unique in your network\n" +
                    "• Example: SMARTBOX01"),
                    
                ["RemoteAeTitle"] = ("Remote AE Title", 
                    "PACS server's AE Title.\n\n" +
                    "• Get this from your PACS administrator\n" +
                    "• Must match exactly (case sensitive)\n" +
                    "• Common examples: PACS, MAINPACS\n" +
                    "• Required for C-STORE operations"),
                    
                ["RemoteHost"] = ("Remote Host", 
                    "PACS server address.\n\n" +
                    "• IP address (192.168.1.100) or\n" +
                    "• Hostname (pacs.hospital.local)\n" +
                    "• Must be reachable from this device\n" +
                    "• Test with ping command first"),
                    
                ["RemotePort"] = ("Remote Port", 
                    "DICOM communication port.\n\n" +
                    "• Standard DICOM port is 104\n" +
                    "• Some PACS use custom ports (11112, etc)\n" +
                    "• Must not be blocked by firewall\n" +
                    "• Get correct port from PACS admin"),
                    
                ["TimeoutSeconds"] = ("Connection Timeout", 
                    "Maximum wait time for PACS response.\n\n" +
                    "• Increase for slow networks\n" +
                    "• Decrease for faster failure detection\n" +
                    "• 30 seconds is usually sufficient\n" +
                    "• Applies to C-ECHO and C-STORE"),
                    
                ["UseTls"] = ("Use TLS", 
                    "Secure DICOM communication.\n\n" +
                    "• Encrypts data in transit\n" +
                    "• Requires PACS TLS support\n" +
                    "• May need certificates\n" +
                    "• Slightly slower than plain DICOM"),
                    
                ["VideoResolution"] = ("Video Resolution", 
                    "Default capture resolution.\n\n" +
                    "• Higher = better quality, larger files\n" +
                    "• 1920x1080: Full HD (recommended)\n" +
                    "• 1280x720: HD, good balance\n" +
                    "• 640x480: Low bandwidth/storage"),
                    
                ["PreferredFps"] = ("Frame Rate", 
                    "Video frames per second.\n\n" +
                    "• 60 FPS: Smooth motion (medical procedures)\n" +
                    "• 30 FPS: Standard video\n" +
                    "• Higher FPS = larger files\n" +
                    "• Camera must support selected FPS"),
                    
                ["VideoBitrateMbps"] = ("Video Bitrate", 
                    "Video compression quality in Mbps.\n\n" +
                    "• Higher = better quality, larger files\n" +
                    "• 5 Mbps: Good quality for medical\n" +
                    "• 10 Mbps: High quality\n" +
                    "• 1 Mbps: Low bandwidth"),
                    
                ["JpegQuality"] = ("JPEG Quality", 
                    "Photo compression quality.\n\n" +
                    "• 95-100: Best quality (medical recommended)\n" +
                    "• 85-94: High quality, smaller files\n" +
                    "• 75-84: Good quality, much smaller\n" +
                    "• Below 75: Not recommended for medical"),
                    
                ["Theme"] = ("Application Theme", 
                    "User interface appearance.\n\n" +
                    "• System: Follows Windows settings\n" +
                    "• Light: Always light theme\n" +
                    "• Dark: Always dark theme\n" +
                    "• Changes take effect immediately"),
                    
                ["ShowDebugInfo"] = ("Debug Information", 
                    "Show technical information.\n\n" +
                    "• Displays FPS, status messages\n" +
                    "• Helpful for troubleshooting\n" +
                    "• May clutter interface\n" +
                    "• For advanced users/technicians"),
                    
                ["AutoStartCapture"] = ("Auto-Start Camera", 
                    "Start camera automatically.\n\n" +
                    "• Camera ready when app starts\n" +
                    "• Faster workflow\n" +
                    "• May slow startup slightly\n" +
                    "• Disable if camera not always needed"),
                    
                ["MinimizeToTray"] = ("Minimize to Tray", 
                    "Minimize to system tray.\n\n" +
                    "• App continues running in background\n" +
                    "• Click tray icon to restore\n" +
                    "• Preserves camera connection\n" +
                    "• Good for always-on scenarios"),
                    
                ["StartWithWindows"] = ("Start with Windows", 
                    "Launch on Windows startup.\n\n" +
                    "• App ready when computer starts\n" +
                    "• Good for dedicated workstations\n" +
                    "• Adds to Windows startup folder\n" +
                    "• May slow boot time slightly")
            };
        }
        
        private void LoadConfigToUI()
        {
            // Storage Settings
            PhotosPath.Text = _config.Storage.PhotosPath;
            VideosPath.Text = _config.Storage.VideosPath;
            DicomPath.Text = _config.Storage.DicomPath;
            UseRelativePaths.IsChecked = _config.Storage.UseRelativePaths;
            MaxStorageMB.Value = _config.Storage.MaxStorageSizeMB;
            RetentionDays.Value = _config.Storage.RetentionDays;
            
            // PACS Configuration
            AeTitle.Text = _config.Pacs.AeTitle;
            RemoteAeTitle.Text = _config.Pacs.RemoteAeTitle;
            RemoteHost.Text = _config.Pacs.RemoteHost;
            RemotePort.Value = _config.Pacs.RemotePort;
            TimeoutSeconds.Value = _config.Pacs.TimeoutSeconds;
            UseTls.IsChecked = _config.Pacs.UseTls;
            
            // Video Settings
            var resolution = $"{_config.Video.PreferredWidth}x{_config.Video.PreferredHeight}";
            foreach (ComboBoxItem item in VideoResolution.Items)
            {
                if (item.Tag?.ToString() == resolution)
                {
                    VideoResolution.SelectedItem = item;
                    break;
                }
            }
            PreferredFps.Value = _config.Video.PreferredFps;
            VideoBitrateMbps.Value = _config.Video.VideoBitrateMbps;
            JpegQuality.Value = _config.Video.JpegQuality;
            
            // Application Settings
            Theme.SelectedIndex = _config.Application.Theme switch
            {
                "Light" => 1,
                "Dark" => 2,
                _ => 0
            };
            ShowDebugInfo.IsChecked = _config.Application.ShowDebugInfo;
            AutoStartCapture.IsChecked = _config.Application.AutoStartCapture;
            MinimizeToTray.IsChecked = _config.Application.MinimizeToTray;
            StartWithWindows.IsChecked = _config.Application.StartWithWindows;
        }
        
        private void SaveUIToConfig()
        {
            // Storage Settings
            _config.Storage.PhotosPath = PhotosPath.Text;
            _config.Storage.VideosPath = VideosPath.Text;
            _config.Storage.DicomPath = DicomPath.Text;
            _config.Storage.UseRelativePaths = UseRelativePaths.IsChecked ?? true;
            _config.Storage.MaxStorageSizeMB = (long)MaxStorageMB.Value;
            _config.Storage.RetentionDays = (int)RetentionDays.Value;
            
            // PACS Configuration
            _config.Pacs.AeTitle = AeTitle.Text;
            _config.Pacs.RemoteAeTitle = RemoteAeTitle.Text;
            _config.Pacs.RemoteHost = RemoteHost.Text;
            _config.Pacs.RemotePort = (int)RemotePort.Value;
            _config.Pacs.TimeoutSeconds = (int)TimeoutSeconds.Value;
            _config.Pacs.UseTls = UseTls.IsChecked ?? false;
            
            // Video Settings
            if (VideoResolution.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                var parts = item.Tag.ToString().Split('x');
                if (parts.Length == 2)
                {
                    _config.Video.PreferredWidth = int.Parse(parts[0]);
                    _config.Video.PreferredHeight = int.Parse(parts[1]);
                }
            }
            _config.Video.PreferredFps = (int)PreferredFps.Value;
            _config.Video.VideoBitrateMbps = (int)VideoBitrateMbps.Value;
            _config.Video.JpegQuality = (int)JpegQuality.Value;
            
            // Application Settings
            _config.Application.Theme = Theme.SelectedIndex switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            };
            _config.Application.ShowDebugInfo = ShowDebugInfo.IsChecked ?? false;
            _config.Application.AutoStartCapture = AutoStartCapture.IsChecked ?? true;
            _config.Application.MinimizeToTray = MinimizeToTray.IsChecked ?? true;
            _config.Application.StartWithWindows = StartWithWindows.IsChecked ?? false;
        }
        
        private void OnFieldFocused(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is string tag)
            {
                if (_helpTexts.TryGetValue(tag, out var help))
                {
                    ShowHelp(help.Title, help.Content);
                }
            }
        }
        
        private void ShowHelp(string title, string content)
        {
            HelpTitle.Text = title;
            HelpContent.Text = content;
        }
        
        private async void BrowsePhotosPath_Click(object sender, RoutedEventArgs e)
        {
            var path = await BrowseForFolder("Select Photos Folder");
            if (!string.IsNullOrEmpty(path))
            {
                PhotosPath.Text = MakeRelativeIfNeeded(path);
            }
        }
        
        private async void BrowseVideosPath_Click(object sender, RoutedEventArgs e)
        {
            var path = await BrowseForFolder("Select Videos Folder");
            if (!string.IsNullOrEmpty(path))
            {
                VideosPath.Text = MakeRelativeIfNeeded(path);
            }
        }
        
        private async void BrowseDicomPath_Click(object sender, RoutedEventArgs e)
        {
            var path = await BrowseForFolder("Select DICOM Folder");
            if (!string.IsNullOrEmpty(path))
            {
                DicomPath.Text = MakeRelativeIfNeeded(path);
            }
        }
        
        private async Task<string> BrowseForFolder(string title)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
        
        private string MakeRelativeIfNeeded(string path)
        {
            if (UseRelativePaths.IsChecked == true)
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                if (path.StartsWith(appDir, StringComparison.OrdinalIgnoreCase))
                {
                    return "./" + path.Substring(appDir.Length).Replace('\\', '/');
                }
            }
            return path;
        }
        
        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToConfig();
            var (isValid, errors) = _config.Validate();
            
            if (isValid)
            {
                var dialog = new ContentDialog
                {
                    Title = "Validation Successful",
                    Content = "All settings are valid!",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Validation Failed",
                    Content = string.Join("\n", errors),
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToConfig();
            
            var (isValid, errors) = _config.Validate();
            if (!isValid)
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid Settings",
                    Content = "Please fix the following errors:\n\n" + string.Join("\n", errors),
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            
            try
            {
                await _config.SaveAsync();
                this.Close();
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Save Failed",
                    Content = $"Failed to save settings: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private async void TestPacsConnection_Click(object sender, RoutedEventArgs e)
        {
            SaveUIToConfig();
            
            // TODO: Implement actual PACS C-ECHO test
            var dialog = new ContentDialog
            {
                Title = "PACS Connection Test",
                Content = "PACS connection test will be implemented with DICOM functionality.\n\n" +
                         $"Would test connection to:\n{_config.Pacs.RemoteHost}:{_config.Pacs.RemotePort}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}