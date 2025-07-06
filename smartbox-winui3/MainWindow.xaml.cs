using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Security.Authorization.AppCapabilityAccess;
using Windows.Media.Capture.Frames;
using System.Linq;

namespace SmartBoxNext
{
    public sealed partial class MainWindow : Window
    {
        private MediaCapture? _mediaCapture;
        private MediaPlayer? _mediaPlayer;
        private bool _isInitialized = false;
        private bool _isPreviewing = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "SmartBox Next - Medical Imaging";
            
            // Set window size
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = 1200, Height = 800 });
            
            // Initialize webcam on load
            this.Activated += async (s, e) =>
            {
                if (!_isInitialized)
                {
                    await InitializeWebcamAsync();
                }
            };
            
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
                // Check camera permissions for unpackaged app
                var capability = AppCapability.Create("webcam");
                if (capability.CheckAccess() != AppCapabilityAccessStatus.Allowed)
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
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    MemoryPreference = MediaCaptureMemoryPreference.Auto
                };
                
                await _mediaCapture.InitializeAsync(settings);

                // Find the best video source
                var frameSource = _mediaCapture.FrameSources.Values
                    .FirstOrDefault(source => 
                        source.Info.MediaStreamType == MediaStreamType.VideoPreview &&
                        source.Info.SourceKind == MediaFrameSourceKind.Color);

                if (frameSource == null)
                {
                    frameSource = _mediaCapture.FrameSources.Values
                        .FirstOrDefault(source => 
                            source.Info.MediaStreamType == MediaStreamType.VideoRecord &&
                            source.Info.SourceKind == MediaFrameSourceKind.Color);
                }

                if (frameSource == null)
                {
                    await ShowErrorDialog("No suitable video source found");
                    return;
                }

                // Create MediaPlayer with MediaSource from frame source
                var mediaSource = MediaSource.CreateFromMediaFrameSource(frameSource);
                _mediaPlayer = new MediaPlayer
                {
                    Source = mediaSource,
                    RealTimePlayback = true,
                    AutoPlay = true
                };

                // Set MediaPlayer on MediaPlayerElement
                WebcamPreview.SetMediaPlayer(_mediaPlayer);
                
                _isPreviewing = true;
                _isInitialized = true;
                
                WebcamPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch (UnauthorizedAccessException)
            {
                await ShowErrorDialog("Webcam access denied. Please check Windows privacy settings:\n\nSettings > Privacy > Camera > Allow apps to access your camera");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Failed to initialize webcam: {ex.Message}\n\nDetails: {ex.GetType().Name}");
            }
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaCapture == null || !_isPreviewing)
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

                await ShowInfoDialog($"Image captured: {photoFile.Name}");
            }
            catch (Exception ex)
            {
                await ShowErrorDialog($"Capture failed: {ex.Message}");
            }
        }

        private async void ExportDicomButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement DICOM export with fo-dicom
            await ShowInfoDialog("DICOM export will be implemented with fo-dicom");
        }

        private async void PacsSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show PACS settings dialog
            await ShowInfoDialog("PACS settings dialog coming soon");
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

        private async Task CleanupCameraAsync()
        {
            try
            {
                _isInitialized = false;
                _isPreviewing = false;

                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Pause();
                    _mediaPlayer.Source = null;
                    _mediaPlayer.Dispose();
                    _mediaPlayer = null;
                }

                WebcamPreview.SetMediaPlayer(null);

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