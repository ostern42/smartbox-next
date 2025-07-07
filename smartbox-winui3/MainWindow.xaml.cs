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
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Web.WebView2.Core;

namespace SmartBoxNext
{
    public sealed partial class MainWindow : Window
    {
        private MediaCapture? _mediaCapture;
        private bool _isInitialized = false;
        private bool _isPreviewing = false;
        private bool _isRecording = false;
        private DispatcherQueueTimer? _timer;
        private int _frameCount = 0;
        private AppConfig _config = new AppConfig();
        
        // High-performance mode
        private MediaFrameReader? _frameReader;
        private SoftwareBitmapSource _bitmapSource = new SoftwareBitmapSource();
        private bool _useFrameReader = false;
        private DateTime _lastFpsUpdate = DateTime.Now;
        private double _currentFps = 0;
        
        // Enterprise high-performance capture
        private HighPerformanceCapture? _highPerfCapture;
        private FastYUY2Capture? _fastCapture;
        private VideoStreamCapture? _videoStreamCapture;
        private SimpleVideoCapture? _simpleVideoCapture;
        private ThrottledVideoCapture? _throttledVideoCapture;
        private LocalStreamServer? _streamServer;

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
            
            // Load configuration
            _ = LoadConfigurationAsync();
            
            // Cleanup on close
            this.Closed += async (s, e) =>
            {
                await CleanupCameraAsync();
            };
        }
        
        private async Task LoadConfigurationAsync()
        {
            try
            {
                _config = await AppConfig.LoadAsync();
                AddDebugMessage($"Configuration loaded. First run: {_config.IsFirstRun}");
                
                // Show debug info based on config
                DebugInfo.Visibility = _config.Application.ShowDebugInfo ? Visibility.Visible : Visibility.Collapsed;
                
                // Check if first run
                if (_config.IsFirstRun)
                {
                    // TODO: Show first run assistant
                    AddDebugMessage("First run detected - assistant would show here");
                    
                    // Mark as not first run anymore
                    _config.IsFirstRun = false;
                    await _config.SaveAsync();
                }
                
                // Auto-start camera if configured
                if (_config.Application.AutoStartCapture && !_isInitialized)
                {
                    AddDebugMessage("Auto-starting camera...");
                    await InitializeWebRTCAsync();
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Failed to load configuration: {ex.Message}");
            }
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

                // Start local stream server first
                try
                {
                    _streamServer = new LocalStreamServer();
                    _streamServer.DebugMessage += AddDebugMessage;
                    
                    if (await _streamServer.StartAsync(8080))
                    {
                        AddDebugMessage("Stream server started! Open http://localhost:8080 in browser");
                        
                        // Add a button or info to open browser
                        WebcamPlaceholder.Text = "Stream at http://localhost:8080\nOr use Initialize Webcam for local preview";
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Stream server failed: {ex.Message}");
                }

                // Try ThrottledVideoCapture first - throttled UI updates
                try
                {
                    AddDebugMessage("Trying ThrottledVideoCapture for video streaming...");
                    _throttledVideoCapture = new ThrottledVideoCapture();
                    _throttledVideoCapture.DebugMessage += AddDebugMessage;
                    _throttledVideoCapture.FrameArrived += OnThrottledVideoFrameArrived;
                    
                    if (await _throttledVideoCapture.InitializeAsync(_mediaCapture))
                    {
                        if (await _throttledVideoCapture.StartAsync())
                        {
                            _isPreviewing = true;
                            _isInitialized = true;
                            WebcamPlaceholder.Visibility = Visibility.Collapsed;
                            AddDebugMessage("THROTTLED VIDEO CAPTURE ACTIVE! 15 FPS UI updates!");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"ThrottledVideoCapture failed: {ex.Message}");
                    _throttledVideoCapture?.Dispose();
                    _throttledVideoCapture = null;
                }

                // Try SimpleVideoCapture as fallback
                try
                {
                    AddDebugMessage("Trying SimpleVideoCapture for video streaming...");
                    _simpleVideoCapture = new SimpleVideoCapture();
                    _simpleVideoCapture.DebugMessage += AddDebugMessage;
                    _simpleVideoCapture.FrameArrived += OnSimpleVideoFrameArrived;
                    
                    if (await _simpleVideoCapture.InitializeAsync(_mediaCapture))
                    {
                        if (await _simpleVideoCapture.StartAsync())
                        {
                            _isPreviewing = true;
                            _isInitialized = true;
                            WebcamPlaceholder.Visibility = Visibility.Collapsed;
                            AddDebugMessage("SIMPLE VIDEO CAPTURE ACTIVE! Real video streaming!");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"SimpleVideoCapture failed: {ex.Message}");
                    _simpleVideoCapture?.Dispose();
                    _simpleVideoCapture = null;
                }

                // Try VideoStreamCapture as second option
                try
                {
                    AddDebugMessage("Trying VideoStreamCapture for proper video streaming...");
                    _videoStreamCapture = new VideoStreamCapture(DispatcherQueue);
                    _videoStreamCapture.DebugMessage += AddDebugMessage;
                    _videoStreamCapture.FpsUpdated += fps => AddDebugMessage($"Stream FPS: {fps:F1}");
                    _videoStreamCapture.FrameArrived += OnVideoStreamFrameArrived;
                    
                    if (await _videoStreamCapture.InitializeAsync(_mediaCapture))
                    {
                        if (await _videoStreamCapture.StartStreamingAsync())
                        {
                            _isPreviewing = true;
                            _isInitialized = true;
                            WebcamPlaceholder.Visibility = Visibility.Collapsed;
                            AddDebugMessage("VIDEO STREAM CAPTURE ACTIVE! Real video streaming!");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"VideoStreamCapture failed: {ex.Message}");
                    _videoStreamCapture?.Dispose();
                    _videoStreamCapture = null;
                }

                // Try enterprise high-performance capture as fallback
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

                // Try FastYUY2Capture for YUY2 cameras
                try
                {
                    AddDebugMessage("Trying FastYUY2Capture for 30 FPS...");
                    _fastCapture = new FastYUY2Capture(DispatcherQueue);
                    _fastCapture.DebugMessage += AddDebugMessage;
                    _fastCapture.FrameArrived += OnFastFrameArrived;
                    
                    if (await _fastCapture.InitializeAsync(_mediaCapture))
                    {
                        _isPreviewing = true;
                        _isInitialized = true;
                        WebcamPlaceholder.Visibility = Visibility.Collapsed;
                        AddDebugMessage("FAST YUY2 CAPTURE ACTIVE! 30 FPS!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"FastYUY2Capture failed: {ex.Message}");
                    _fastCapture?.Dispose();
                    _fastCapture = null;
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

        private async void OnFastFrameArrived(SoftwareBitmap frame)
        {
            try
            {
                await _bitmapSource.SetBitmapAsync(frame);
                
                DispatcherQueue.TryEnqueue(() =>
                {
                    WebcamPreview.Source = _bitmapSource;
                });
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Fast frame display error: {ex.Message}");
            }
        }

        private void OnVideoStreamFrameArrived(SoftwareBitmap frame)
        {
            // Must run on UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await _bitmapSource.SetBitmapAsync(frame);
                    WebcamPreview.Source = _bitmapSource;
                    
                    // Frame is already copied, dispose it
                    frame.Dispose();
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Video stream frame display error: {ex.Message}");
                }
            });
        }

        private void OnSimpleVideoFrameArrived(SoftwareBitmap frame)
        {
            // Debug: Log first few frames
            if (_frameCount < 3)
            {
                AddDebugMessage($"Frame {_frameCount} received! Size: {frame.PixelWidth}x{frame.PixelHeight}, Format: {frame.BitmapPixelFormat}, Alpha: {frame.BitmapAlphaMode}");
            }
            
            // Must run on UI thread
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    // Create a copy of the frame since it will be disposed
                    using (var frameCopy = new SoftwareBitmap(frame.BitmapPixelFormat, frame.PixelWidth, frame.PixelHeight, frame.BitmapAlphaMode))
                    {
                        frame.CopyTo(frameCopy);
                        
                        // Ensure we have a bitmap source
                        if (_bitmapSource == null)
                        {
                            _bitmapSource = new SoftwareBitmapSource();
                            AddDebugMessage("Created new SoftwareBitmapSource");
                        }
                        
                        await _bitmapSource.SetBitmapAsync(frameCopy);
                        
                        // Ensure WebcamPreview is visible and has the source
                        if (WebcamPreview.Source != _bitmapSource)
                        {
                            WebcamPreview.Source = _bitmapSource;
                            AddDebugMessage($"Set WebcamPreview source (frame {_frameCount})");
                        }
                    }
                    
                    _frameCount++;
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Simple video frame display error: {ex.Message}");
                    AddDebugMessage($"Stack trace: {ex.StackTrace}");
                }
            });
            
            // Dispose the original frame
            frame.Dispose();
        }

        private void OnThrottledVideoFrameArrived(SoftwareBitmap frame)
        {
            // Send to stream server
            _streamServer?.UpdateFrame(frame);
            
            // Frame is already a copy, just display it
            DispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    if (_bitmapSource == null)
                    {
                        _bitmapSource = new SoftwareBitmapSource();
                    }
                    
                    await _bitmapSource.SetBitmapAsync(frame);
                    WebcamPreview.Source = _bitmapSource;
                    
                    frame.Dispose();
                }
                catch (Exception ex)
                {
                    AddDebugMessage($"Throttled video frame display error: {ex.Message}");
                }
            });
        }

        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
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
                // Direkt WebRTC initialisieren
                await InitializeWebRTCAsync();
            }
            else
            {
                await ShowInfoDialog("Webcam is already initialized");
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if WebRTC is active
            if (WebRTCPreview.Visibility == Visibility.Visible && WebRTCPreview.CoreWebView2 != null)
            {
                await CapturePhotoFromWebRTC();
                return;
            }
            
            // Fallback to MediaCapture
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

                // Always use regular capture for now - it works reliably
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

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_config);
            settingsWindow.Activate();
            
            // Wait for window to close and reload config
            settingsWindow.Closed += async (s, args) =>
            {
                await LoadConfigurationAsync();
            };
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
                
                // DirectShow enumeration temporarily disabled - DirectShowCapture.cs was removed
                // TODO: Re-implement with FlashCap or other solution
                /*
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
                */
                
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

                _fastCapture?.Dispose();
                _fastCapture = null;

                _videoStreamCapture?.Dispose();
                _videoStreamCapture = null;

                _simpleVideoCapture?.Dispose();
                _simpleVideoCapture = null;

                _throttledVideoCapture?.Dispose();
                _throttledVideoCapture = null;

                _streamServer?.Dispose();
                _streamServer = null;

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

        private async void TestSilkNetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDebugMessage("Testing Silk.NET integration...");
                
                // Run simple test first
                if (SilkNetSimpleTest.TestBasicD3D11())
                {
                    AddDebugMessage("✅ Simple D3D11 test passed!");
                    
                    // List available adapters
                    SilkNetTest.ListAvailableAdapters();
                    
                    // Try to initialize Silk.NET video capture
                    AddDebugMessage("Initializing Silk.NET video capture...");
                    
                    // Get the SwapChainPanel's handle
                    var swapChainPanel = SilkNetPreview;
                    if (swapChainPanel != null)
                    {
                        // Show Silk.NET preview, hide standard preview
                        SilkNetPreview.Visibility = Visibility.Visible;
                        WebcamPreview.Visibility = Visibility.Collapsed;
                        WebcamPlaceholder.Visibility = Visibility.Collapsed;
                        
                        AddDebugMessage("SwapChainPanel ready for Silk.NET rendering!");
                        
                        // TODO: Initialize SilkNetCaptureEngine with SwapChainPanel
                        AddDebugMessage("Note: Full video capture integration coming next!");
                    }
                    else
                    {
                        AddDebugMessage("SwapChainPanel not found in UI!");
                    }
                }
                else
                {
                    AddDebugMessage("❌ Simple D3D11 test failed!");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Silk.NET test failed: {ex.Message}");
                await ShowErrorDialog($"Silk.NET test failed: {ex.Message}");
            }
        }

        private async Task InitializeWebRTCAsync()
        {
            try
            {
                AddDebugMessage("=== Starting WebRTC initialization ===");
                
                // Ensure WebView2 is initialized
                await WebRTCPreview.EnsureCoreWebView2Async();
                
                // Start local stream server if not already running
                if (_streamServer == null)
                {
                    _streamServer = new LocalStreamServer();
                    _streamServer.DebugMessage += AddDebugMessage;
                    
                    if (await _streamServer.StartAsync(8080))
                    {
                        AddDebugMessage("Stream server started on port 8080");
                    }
                }
                
                // Set up WebView2 message handling
                WebRTCPreview.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                
                // Navigate to WebRTC page and wait for it to load
                WebRTCPreview.NavigationCompleted += OnWebRTCNavigationCompleted;
                WebRTCPreview.CoreWebView2.Navigate("http://localhost:8080/webrtc");
                
                // Show WebRTC preview, hide others
                WebRTCPreview.Visibility = Visibility.Visible;
                WebcamPreview.Visibility = Visibility.Collapsed;
                SilkNetPreview.Visibility = Visibility.Collapsed;
                WebcamPlaceholder.Visibility = Visibility.Collapsed;
                
                _isInitialized = true;
                AddDebugMessage("WebRTC preview active! Browser-based 60 FPS capture!");
                
                // Monitor performance
                WebRTCPreview.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    // We can use document title to pass FPS info from JavaScript
                    var title = WebRTCPreview.CoreWebView2.DocumentTitle;
                    if (title.StartsWith("FPS:"))
                    {
                        AddDebugMessage($"WebRTC {title}");
                    }
                };
            }
            catch (Exception ex)
            {
                AddDebugMessage($"WebRTC initialization failed: {ex.Message}");
                await ShowErrorDialog($"WebRTC initialization failed: {ex.Message}");
            }
        }
        
        private TaskCompletionSource<string>? _capturePhotoTcs;
        private TaskCompletionSource<string>? _stopRecordingTcs;
        
        private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var message = e.TryGetWebMessageAsString();
                var json = System.Text.Json.JsonDocument.Parse(message);
                var root = json.RootElement;
                
                var action = root.GetProperty("action").GetString();
                var success = root.GetProperty("success").GetBoolean();
                
                switch (action)
                {
                    case "photoCapture":
                        if (_capturePhotoTcs != null)
                        {
                            if (success)
                            {
                                var data = root.GetProperty("data").GetString();
                                _capturePhotoTcs.SetResult(data!);
                            }
                            else
                            {
                                var error = root.GetProperty("error").GetString();
                                _capturePhotoTcs.SetException(new Exception(error));
                            }
                        }
                        break;
                        
                    case "recordingStopped":
                        if (_stopRecordingTcs != null)
                        {
                            if (success)
                            {
                                var data = root.GetProperty("data").GetString();
                                _stopRecordingTcs.SetResult(data!);
                            }
                            else
                            {
                                var error = root.GetProperty("error").GetString();
                                _stopRecordingTcs.SetException(new Exception(error));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"WebMessage error: {ex.Message}");
            }
        }
        
        private async Task CapturePhotoFromWebRTC()
        {
            try
            {
                AddDebugMessage("Capturing photo from WebRTC...");
                
                // Create task completion source
                _capturePhotoTcs = new TaskCompletionSource<string>();
                
                // Send capture command to WebView2
                var messageId = Guid.NewGuid().ToString();
                var message = $"{{\"action\":\"capturePhoto\",\"id\":\"{messageId}\"}}";
                WebRTCPreview.CoreWebView2.PostWebMessageAsString(message);
                
                // Wait for response (with timeout)
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(_capturePhotoTcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Photo capture timed out");
                }
                
                var base64Data = await _capturePhotoTcs.Task;
                
                // Convert base64 to bytes
                var imageBytes = Convert.FromBase64String(base64Data);
                
                // Save to configured path
                var photosPath = _config.GetFullPath(_config.Storage.PhotosPath);
                
                // Ensure directory exists
                if (!Directory.Exists(photosPath))
                {
                    Directory.CreateDirectory(photosPath);
                }
                
                var photoFilePath = Path.Combine(photosPath, $"Capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                
                await File.WriteAllBytesAsync(photoFilePath, imageBytes);
                
                AddDebugMessage($"Photo saved: {photoFilePath}");
                
                // Show preview dialog
                await ShowCapturePreviewFromPath(photoFilePath);
            }
            catch (Exception ex)
            {
                AddDebugMessage($"WebRTC capture error: {ex.Message}");
                await ShowErrorDialog($"Failed to capture photo: {ex.Message}");
            }
            finally
            {
                _capturePhotoTcs = null;
            }
        }
        
        private void OnWebRTCNavigationCompleted(WebView2 sender, Microsoft.UI.Xaml.Controls.CoreWebView2NavigationCompletedEventArgs args)
        {
            if (args.IsSuccess)
            {
                AddDebugMessage("WebRTC page loaded successfully");
                // Inject a test to check if WebView2 API is available
                _ = WebRTCPreview.CoreWebView2.ExecuteScriptAsync(@"
                    if (window.chrome && window.chrome.webview) {
                        console.log('WebView2 API is available');
                    } else {
                        console.error('WebView2 API is NOT available');
                    }
                ");
            }
            else
            {
                AddDebugMessage($"WebRTC navigation failed: {args.WebErrorStatus}");
            }
        }
        
        private async Task ShowCapturePreview(StorageFile photoFile)
        {
            try
            {
                using (var stream = await photoFile.OpenAsync(FileAccessMode.Read))
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream);
                    
                    var image = new Image
                    {
                        Source = bitmapImage,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        MaxWidth = 800,
                        MaxHeight = 600
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
                AddDebugMessage($"Preview error: {ex.Message}");
            }
        }
        
        private async Task ShowCapturePreviewFromPath(string photoPath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(photoPath);
                using (var stream = new MemoryStream(bytes))
                {
                    var bitmapImage = new BitmapImage();
                    await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                    
                    var image = new Image
                    {
                        Source = bitmapImage,
                        Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform,
                        MaxWidth = 800,
                        MaxHeight = 600
                    };
                    
                    var dialog = new ContentDialog
                    {
                        Title = $"Captured Image: {Path.GetFileName(photoPath)}",
                        Content = new ScrollViewer { Content = image },
                        CloseButtonText = "Close",
                        XamlRoot = this.Content.XamlRoot
                    };
                    
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Preview error: {ex.Message}");
            }
        }
        
        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                await StartVideoRecording();
            }
            else
            {
                await StopVideoRecording();
            }
        }
        
        private async Task StartVideoRecording()
        {
            try
            {
                // Check if WebRTC is active
                if (WebRTCPreview.Visibility == Visibility.Visible && WebRTCPreview.CoreWebView2 != null)
                {
                    AddDebugMessage("Starting WebRTC video recording...");
                    
                    // Send start recording command
                    var messageId = Guid.NewGuid().ToString();
                    var message = $"{{\"action\":\"startRecording\",\"id\":\"{messageId}\"}}";
                    WebRTCPreview.CoreWebView2.PostWebMessageAsString(message);
                    
                    _isRecording = true;
                    
                    // Update UI
                    RecordText.Text = "Stop Recording";
                    RecordIcon.Glyph = "\uE71A"; // Stop icon
                    RecordButton.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                    
                    AddDebugMessage("Recording started!");
                }
                else if (_mediaCapture != null && _isInitialized)
                {
                    // Fallback to MediaCapture recording
                    await ShowInfoDialog("Video recording with MediaCapture not implemented yet");
                }
                else
                {
                    await ShowErrorDialog("Webcam not initialized");
                }
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Start recording error: {ex.Message}");
                await ShowErrorDialog($"Failed to start recording: {ex.Message}");
            }
        }
        
        private async Task StopVideoRecording()
        {
            try
            {
                if (WebRTCPreview.Visibility == Visibility.Visible && WebRTCPreview.CoreWebView2 != null)
                {
                    AddDebugMessage("Stopping WebRTC video recording...");
                    
                    // Create task completion source
                    _stopRecordingTcs = new TaskCompletionSource<string>();
                    
                    // Send stop recording command
                    var messageId = Guid.NewGuid().ToString();
                    var message = $"{{\"action\":\"stopRecording\",\"id\":\"{messageId}\"}}";
                    WebRTCPreview.CoreWebView2.PostWebMessageAsString(message);
                    
                    // Wait for response (with timeout)
                    var timeoutTask = Task.Delay(10000); // 10 seconds for video
                    var completedTask = await Task.WhenAny(_stopRecordingTcs.Task, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException("Stop recording timed out");
                    }
                    
                    var base64Data = await _stopRecordingTcs.Task;
                    
                    // Convert base64 to bytes
                    var videoBytes = Convert.FromBase64String(base64Data);
                    
                    // Save to configured path
                    var videosPath = _config.GetFullPath(_config.Storage.VideosPath);
                    
                    // Ensure directory exists
                    if (!Directory.Exists(videosPath))
                    {
                        Directory.CreateDirectory(videosPath);
                    }
                    
                    var videoFilePath = Path.Combine(videosPath, $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.{_config.Video.VideoFormat}");
                    
                    await File.WriteAllBytesAsync(videoFilePath, videoBytes);
                    
                    AddDebugMessage($"Video saved: {videoFilePath}");
                    
                    // Show confirmation
                    await ShowInfoDialog($"Video saved successfully!\n{Path.GetFileName(videoFilePath)}");
                }
                
                _isRecording = false;
                
                // Update UI
                RecordText.Text = "Start Recording";
                RecordIcon.Glyph = "\uE7C8"; // Record icon
                RecordButton.Style = null; // Remove accent style
            }
            catch (Exception ex)
            {
                AddDebugMessage($"Stop recording error: {ex.Message}");
                await ShowErrorDialog($"Failed to stop recording: {ex.Message}");
                _isRecording = false;
            }
            finally
            {
                _stopRecordingTcs = null;
            }
        }
    }
}