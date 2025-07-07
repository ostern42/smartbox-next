using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Storage.Streams;
using Microsoft.UI.Dispatching;
using Windows.Graphics.Imaging;
using System.IO;
using Windows.Storage.Pickers;
using System.Linq;
using Windows.Media.Capture.Frames;
using Windows.Media;
using Windows.Storage.Pickers;
using System.Collections.Generic;

namespace SmartBoxNext
{
    public sealed partial class MainWindow : Window
    {
        private MediaCapture? _mediaCapture;
        private bool _isInitialized = false;
        private bool _isPreviewing = false;
        private DispatcherQueueTimer? _timer;
        private int _frameCount = 0;
        
        // High-performance mode
        private MediaFrameReader? _frameReader;
        private SoftwareBitmapSource _bitmapSource = new SoftwareBitmapSource();
        private bool _useFrameReader = false;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private double _currentFps = 0;
        
        // Enterprise high-performance capture
        private HighPerformanceCapture? _highPerfCapture;

        private void AddDebugMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            DispatcherQueue.TryEnqueue(() =>
            {
                if (DebugInfo != null)
                {
                    DebugInfo.Text = $"[{timestamp}] {message}\n" + DebugInfo.Text;
                    // Keep only last 20 messages to prevent UI slowdown
                    var lines = DebugInfo.Text.Split('\n');
                    if (lines.Length > 20)
                    {
                        DebugInfo.Text = string.Join('\n', lines.Take(20));
                    }
                }
            });
            System.Diagnostics.Debug.WriteLine($"[{timestamp}] {message}");
        }

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "SmartBox Next - Medical Imaging";
            
            // Set window size
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 800 });
            
            // Cleanup on close
            this.Closed += async (s, e) =>
            {
                await CleanupCameraAsync();
            };
        }

        private async Task InitializeWebcamAsync()
        {
            try
            {
                AddDebugMessage("=== Starting webcam initialization (Simple approach) ===");
                
                // Check camera permissions
                var capability = AppCapability.Create("webcam");
                var accessStatus = capability.CheckAccess();
                AddDebugMessage($"Camera access status: {accessStatus}");
                
                if (accessStatus != AppCapabilityAccessStatus.Allowed)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Camera Access Required",
                        Content = "Please grant camera access in Windows Settings > Privacy & Security > Camera",
                        PrimaryButtonText = "Open Settings",
                        CloseButtonText = "Cancel",
                        XamlRoot = this.Content.XamlRoot
                    };

                    if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-webcam"));
                    }
                    return;
                }

                // Find camera devices
                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                
                AddDebugMessage($"Found {devices.Count} camera devices");
                foreach (var device in devices)
                {
                    AddDebugMessage($"  - {device.Name} (ID: {device.Id})");
                }
                
                if (devices.Count == 0)
                {
                    WebcamPlaceholder.Text = "No camera found";
                    return;
                }

                // Initialize MediaCapture
                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = devices[0].Id,
                    StreamingCaptureMode = StreamingCaptureMode.Video
                };
                
                await _mediaCapture.InitializeAsync(settings);
                AddDebugMessage("MediaCapture initialized successfully");

                // Try enterprise high-performance capture first
                try
                {
                    _highPerfCapture = new HighPerformanceCapture(DispatcherQueue, targetFps: 30);
                    _highPerfCapture.DebugMessage += AddDebugMessage;
                    _highPerfCapture.FrameArrived += OnHighPerfFrameArrived;
                    
                    if (await _highPerfCapture.InitializeAsync(_mediaCapture))
                    {
                        await _highPerfCapture.StartCaptureAsync();
                        _isPreviewing = true;
                        _isInitialized = true;
                        WebcamPlaceholder.Visibility = Visibility.Collapsed;
                        AddDebugMessage("HIGH PERFORMANCE CAPTURE ACTIVE! Target: 30 FPS");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"High-performance capture failed: {ex.Message}");
                    _highPerfCapture?.Dispose();
                    _highPerfCapture = null;
                }

                // Try to use MediaFrameReader for better performance
                AddDebugMessage($"Available frame sources: {_mediaCapture.FrameSources.Count}");
                foreach (var source in _mediaCapture.FrameSources.Values)
                {
                    AddDebugMessage($"  - {source.Info.Id}: {source.Info.SourceKind}, {source.Info.MediaStreamType}");
                }
                
                var frameSourceInfo = _mediaCapture.FrameSources.Values
                    .FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview 
                                           && source.Info.SourceKind == MediaFrameSourceKind.Color);

                if (frameSourceInfo == null)
                {
                    // Fallback to any color source
                    frameSourceInfo = _mediaCapture.FrameSources.Values
                        .FirstOrDefault(source => source.Info.SourceKind == MediaFrameSourceKind.Color);
                }

                if (frameSourceInfo != null)
                {
                    try
                    {
                        AddDebugMessage($"Found frame source: {frameSourceInfo.Info.Id}");
                        AddDebugMessage($"Source kind: {frameSourceInfo.Info.SourceKind}");
                        AddDebugMessage($"Media stream type: {frameSourceInfo.Info.MediaStreamType}");
                        
                        _frameReader = await _mediaCapture.CreateFrameReaderAsync(frameSourceInfo);
                        _frameReader.FrameArrived += OnFrameArrived;
                        
                        var status = await _frameReader.StartAsync();
                        AddDebugMessage($"FrameReader start status: {status}");
                        
                        if (status == MediaFrameReaderStartStatus.Success)
                        {
                            _useFrameReader = true;
                            AddDebugMessage("HIGH PERFORMANCE MODE: Using MediaFrameReader for 30-60 FPS!");
                        }
                        else
                        {
                            AddDebugMessage($"MediaFrameReader failed to start: {status}. Falling back to timer mode.");
                            _frameReader = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddDebugMessage($"MediaFrameReader error: {ex.Message}");
                        AddDebugMessage($"Stack trace: {ex.StackTrace}");
                        _frameReader = null;
                    }
                }
                else
                {
                    AddDebugMessage("No suitable frame source found for MediaFrameReader");
                }

                // If MediaFrameReader didn't work, use timer-based approach
                if (!_useFrameReader)
                {
                    _timer = DispatcherQueue.CreateTimer();
                    _timer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
                    _timer.Tick += async (s, e) => await UpdatePreviewAsync();
                    _timer.Start();
                    AddDebugMessage("Using timer-based preview (5-10 FPS)");
                }

                _isPreviewing = true;
                _isInitialized = true;
                
                WebcamPlaceholder.Visibility = Visibility.Collapsed;
                AddDebugMessage("Webcam initialization complete!");
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorDialog("Webcam access denied. Please check Windows privacy settings:\n\nSettings > Privacy > Camera > Allow apps to access your camera");
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Error: {ex.GetType().Name} - {ex.Message}");
                await ShowErrorDialog($"Failed to initialize webcam: {ex.Message}\n\nDetails: {ex.GetType().Name}");
            }
        }

        private async void OnHighPerfFrameArrived(SoftwareBitmap frame)
        {
            try
            {
                // Convert to BGRA8 with premultiplied alpha if needed
                SoftwareBitmap convertedFrame = frame;
                if (frame.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                    frame.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    convertedFrame = SoftwareBitmap.Convert(
                        frame, 
                        BitmapPixelFormat.Bgra8, 
                        BitmapAlphaMode.Premultiplied);
                }
                
                await _bitmapSource.SetBitmapAsync(convertedFrame);
                WebcamPreview.Source = _bitmapSource;
                
                // Dispose converted frame if it's different from original
                if (convertedFrame != frame)
                {
                    convertedFrame.Dispose();
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"High-perf frame display error: {ex.Message}");
            }
        }

        private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            // Log first frame arrival
            if (_frameCount == 0)
            {
                AddDebugMessage("First frame arrived from MediaFrameReader!");
            }
            
            using (var frame = sender.TryAcquireLatestFrame())
            {
                if (frame?.VideoMediaFrame?.SoftwareBitmap != null)
                {
                    var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
                    
                    // Log frame details on first frame
                    if (_frameCount == 0)
                    {
                        AddDebugMessage($"Frame format: {softwareBitmap.BitmapPixelFormat}, Alpha: {softwareBitmap.BitmapAlphaMode}");
                        AddDebugMessage($"Frame size: {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight}");
                    }
                    
                    // Convert to BGRA8 with premultiplied alpha if needed
                    if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 || 
                        softwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                    {
                        softwareBitmap = SoftwareBitmap.Convert(
                            softwareBitmap, 
                            BitmapPixelFormat.Bgra8, 
                            BitmapAlphaMode.Premultiplied);
                    }
                    
                    // Update UI on dispatcher thread
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        try
                        {
                            await _bitmapSource.SetBitmapAsync(softwareBitmap);
                            WebcamPreview.Source = _bitmapSource;
                            
                            // Update FPS counter
                            UpdateFpsCounter();
                        }
                        catch (Exception ex)
                        {
                            // Only log first error to avoid spam
                            if (_frameCount == 0)
                            {
                                AddDebugMessage($"Frame display error: {ex.Message}");
                            }
                        }
                    });
                    
                    softwareBitmap?.Dispose();
                }
                else
                {
                    // Log why frame is null
                    if (_frameCount % 100 == 0)
                    {
                        AddDebugMessage($"Frame null - Frame: {frame != null}, VideoMediaFrame: {frame?.VideoMediaFrame != null}, SoftwareBitmap: {frame?.VideoMediaFrame?.SoftwareBitmap != null}");
                    }
                }
            }
        }

        private void UpdateFpsCounter()
        {
            _frameCount++;
            var now = DateTime.Now;
            var elapsed = (now - _lastFpsUpdate).TotalSeconds;
            
            if (elapsed >= 1.0)
            {
                _currentFps = _frameCount / elapsed;
                AddDebugMessage($"FPS: {_currentFps:F1} (High Performance Mode)");
                _frameCount = 0;
                _lastFpsUpdate = now;
            }
        }

        private async Task UpdatePreviewAsync()
        {
            if (_mediaCapture == null || !_isPreviewing) return;

            try
            {
                _frameCount++;
                
                // Capture to memory stream
                var stream = new InMemoryRandomAccessStream();
                await _mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream);
                stream.Seek(0);

                // Create bitmap and display
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream);
                WebcamPreview.Source = bitmapImage;

                // Update debug info every 10 frames
                if (_frameCount % 10 == 0)
                {
                    AddDebugMessage($"Preview frames: {_frameCount}");
                }
            }
            catch (Exception ex)
            {
                // Don't spam errors, just log once
                if (_frameCount == 1)
                {
                    AddDebugMessage($"Preview error: {ex.Message}");
                }
            }
        }

        private async void InitWebcamButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                await InitializeWebcamAsync();
            }
            else
            {
                await ShowInfoDialog("Webcam is already initialized");
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaCapture == null || !_isInitialized)
            {
                await ShowErrorDialog("Webcam not initialized");
                return;
            }

            try
            {
                // Create storage file
                var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                var captureFolder = await myPictures.SaveFolder.CreateFolderAsync("SmartBoxNext", CreationCollisionOption.OpenIfExists);
                var photoFile = await captureFolder.CreateFileAsync($"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg", CreationCollisionOption.GenerateUniqueName);

                // Capture photo
                var imageEncodingProperties = ImageEncodingProperties.CreateJpeg();
                await _mediaCapture.CapturePhotoToStorageFileAsync(imageEncodingProperties, photoFile);

                // Load and display the captured image
                using (var stream = await photoFile.OpenAsync(FileAccessMode.Read))
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    
                    var image = new Image
                    {
                        Source = bitmapImage,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        MaxHeight = 600,
                        MaxWidth = 800
                    };
                    
                    var dialog = new ContentDialog
                    {
                        Title = $"Captured Image: {photoFile.Name}",
                        Content = new ScrollViewer { Content = image },
                        CloseButtonText = "Close",
                        XamlRoot = this.Content.XamlRoot
                    };
                    
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Capture failed: {ex.Message}");
            }
        }

        private async void ExportDicomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if we have a captured image
                var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                var captureFolder = await myPictures.SaveFolder.GetFolderAsync("SmartBoxNext");
                var files = await captureFolder.GetFilesAsync();
                
                if (files.Count == 0)
                {
                    await ShowErrorDialog("No captured images found. Please capture an image first.");
                    return;
                }

                // Use the most recent capture
                var mostRecentFile = files[0];
                foreach (var file in files)
                {
                    if (file.DateCreated > mostRecentFile.DateCreated)
                    {
                        mostRecentFile = file;
                    }
                }

                AddDebugMessage($"Exporting {mostRecentFile.Name} to DICOM...");

                // Get patient information from UI
                var patientName = PatientName.Text;
                var patientId = PatientID.Text;
                DateTime? birthDate = null;
                if (BirthDate.Date != null)
                {
                    birthDate = BirthDate.Date.DateTime;
                }
                var gender = (Gender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "O";
                var studyDescription = StudyDescription.Text;
                var accessionNumber = AccessionNumber.Text;

                // Export to DICOM
                var dicomFile = await DicomExporter.ExportToDicomAsync(
                    mostRecentFile,
                    patientName,
                    patientId,
                    birthDate,
                    gender,
                    studyDescription,
                    accessionNumber);

                AddDebugMessage($"DICOM exported: {dicomFile.Name}");

                // Show success dialog
                var dialog = new ContentDialog
                {
                    Title = "DICOM Export Successful",
                    Content = $"Image exported to:\n{dicomFile.Path}\n\nFile: {dicomFile.Name}",
                    PrimaryButtonText = "Open Folder",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    await Windows.System.Launcher.LaunchFolderAsync(await dicomFile.GetParentAsync());
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"DICOM export error: {ex.Message}");
                await ShowErrorDialog($"Failed to export DICOM: {ex.Message}");
            }
        }

        private async void PacsSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PacsSettingsDialog
            {
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            var debugInfo = "=== Webcam Debug Info ===\n\n";
            
            try
            {
                // Check camera permissions
                var capability = AppCapability.Create("webcam");
                var accessStatus = capability.CheckAccess();
                debugInfo += $"Camera Access: {accessStatus}\n\n";
                
                // List all cameras
                var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(
                    Windows.Devices.Enumeration.DeviceClass.VideoCapture);
                debugInfo += $"Found {devices.Count} cameras:\n";
                foreach (var device in devices)
                {
                    debugInfo += $"- {device.Name}\n  ID: {device.Id}\n\n";
                }
                
                // Current status
                debugInfo += $"\nInitialized: {_isInitialized}\n";
                debugInfo += $"Previewing: {_isPreviewing}\n";
                debugInfo += $"Mode: {(_useFrameReader ? "HIGH PERFORMANCE (MediaFrameReader)" : "Timer-based")}\n";
                debugInfo += $"Current FPS: {_currentFps:F1}\n";
                debugInfo += $"MediaCapture: {(_mediaCapture != null ? "Created" : "Null")}\n";
                debugInfo += $"FrameReader: {(_frameReader != null ? "Active" : "Null")}\n";
                debugInfo += $"Timer: {(_timer != null ? (_timer.IsRunning ? "Running" : "Stopped") : "Null")}\n";
                debugInfo += $"Preview frames: {_frameCount}\n";
            }
            catch (Exception ex)
            {
                debugInfo += $"\nError getting debug info: {ex.Message}\n";
            }
            
            // Create a dialog with a TextBox for copyable text
            var textBox = new TextBox
            {
                Text = debugInfo,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                MinHeight = 400,
                MaxHeight = 600
            };
            
            var scrollViewer = new ScrollViewer
            {
                Content = textBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            
            var dialog = new ContentDialog
            {
                Title = "Webcam Debug Information",
                Content = scrollViewer,
                CloseButtonText = "Close",
                XamlRoot = this.Content.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };
            
            await dialog.ShowAsync();
        }

        private async Task ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowInfoDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Information",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDebugMessage("Starting deep camera analysis...");
                
                // Analyze all cameras
                var cameras = await CameraAnalyzer.AnalyzeAllCamerasAsync();
                var report = CameraAnalyzer.GenerateReport(cameras);
                
                // Also try DirectShow enumeration
                AddDebugMessage("Trying DirectShow enumeration...");
                try
                {
                    var dsDevices = DirectShowCapture.EnumerateVideoDevices();
                    report += "\n\n=== DIRECTSHOW DEVICES ===\n";
                    foreach (var device in dsDevices)
                    {
                        report += $"\nDevice: {device.Name}\n";
                        report += $"Path: {device.DevicePath}\n";
                        DirectShowCapture.GetDeviceFormats(device);
                        foreach (var format in device.SupportedFormats)
                        {
                            report += $"  - {format}\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    report += $"\nDirectShow enumeration failed: {ex.Message}\n";
                }
                
                // Show report in dialog
                var textBox = new TextBox
                {
                    Text = report,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    MinHeight = 400,
                    MaxHeight = 600
                };
                
                var scrollViewer = new ScrollViewer
                {
                    Content = textBox,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                
                var dialog = new ContentDialog
                {
                    Title = "Camera Hardware Analysis",
                    Content = scrollViewer,
                    PrimaryButtonText = "Save Report",
                    CloseButtonText = "Close",
                    XamlRoot = this.Content.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };
                
                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    // Save report
                    var savePicker = new FileSavePicker();
                    var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                    
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    savePicker.FileTypeChoices.Add("Text Files", new List<string>() { ".txt" });
                    savePicker.SuggestedFileName = $"CameraAnalysis_{DateTime.Now:yyyyMMdd_HHmmss}";
                    
                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        await FileIO.WriteTextAsync(file, report);
                        AddDebugMessage($"Report saved to: {file.Path}");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Analysis failed: {ex.Message}");
            }
        }

        private async Task CleanupCameraAsync()
        {
            try
            {
                _isInitialized = false;
                _isPreviewing = false;

                if (_frameReader != null)
                {
                    await _frameReader.StopAsync();
                    _frameReader.FrameArrived -= OnFrameArrived;
                    _frameReader.Dispose();
                    _frameReader = null;
                }

                if (_timer != null)
                {
                    _timer.Stop();
                    _timer = null;
                }

                if (_mediaCapture != null)
                {
                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
        }
    }
}