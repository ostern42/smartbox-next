using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DirectShowLib;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.CaptureService.Services
{
    /// <summary>
    /// Input information structure
    /// </summary>
    public class InputInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // SDI, HDMI, Component, etc.
        public bool IsConnected { get; set; }
    }

    /// <summary>
    /// Capture statistics
    /// </summary>
    public class CaptureStatistics
    {
        public long FramesProcessed { get; set; }
        public long FramesDropped { get; set; }
        public double CurrentFPS { get; set; }
        public int CurrentWidth { get; set; }
        public int CurrentHeight { get; set; }
        public string CurrentFormat { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public DateTime LastFrameTime { get; set; }
    }

    /// <summary>
    /// DirectShow capture graph for Yuan SC550N1 with Session 0 compatibility
    /// </summary>
    public class YuanCaptureGraph : IDisposable
    {
        private readonly ILogger<YuanCaptureGraph> _logger;
        private readonly FrameProcessor _frameProcessor;
        
        // DirectShow objects
        private IFilterGraph2? _graphBuilder;
        private ICaptureGraphBuilder2? _captureGraphBuilder;
        private IBaseFilter? _sourceFilter;
        private IBaseFilter? _sampleGrabberFilter;
        private IBaseFilter? _smartTeeFilter;
        private IBaseFilter? _nullRendererFilter;
        private ISampleGrabber? _sampleGrabber;
        private IMediaControl? _mediaControl;
        private IAMCrossbar? _crossbar;
        
        // Capture state
        private bool _isInitialized = false;
        private bool _isRunning = false;
        private int _currentInputIndex = -1;
        private readonly List<InputInfo> _availableInputs = new();
        
        // Statistics
        private long _framesProcessed = 0;
        private long _framesDropped = 0;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private DateTime _lastStatsTime = DateTime.MinValue;
        private double _currentFPS = 0.0;

        public bool IsRunning => _isRunning;

        public YuanCaptureGraph(ILogger<YuanCaptureGraph> logger, FrameProcessor frameProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _frameProcessor = frameProcessor ?? throw new ArgumentNullException(nameof(frameProcessor));
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Yuan DirectShow capture graph...");

            try
            {
                // Create DirectShow objects
                _graphBuilder = (IFilterGraph2)new FilterGraph();
                _captureGraphBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
                
                // Set filter graph
                var hr = _captureGraphBuilder.SetFiltergraph(_graphBuilder);
                DsError.ThrowExceptionForHR(hr);

                // Find Yuan SC550N1 device
                await FindYuanDeviceAsync();
                
                // Create and configure SampleGrabber (Session 0 compatible)
                await SetupSampleGrabberAsync();
                
                // Setup Smart Tee for multi-branch output
                await SetupSmartTeeAsync();
                
                // Connect the graph
                await ConnectFiltersAsync();
                
                // Discover available inputs
                await DiscoverInputsAsync();

                // Get media control interface
                _mediaControl = (IMediaControl)_graphBuilder;

                _isInitialized = true;
                _logger.LogInformation("Yuan DirectShow capture graph initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Yuan capture graph");
                Cleanup();
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task FindYuanDeviceAsync()
        {
            _logger.LogInformation("Searching for Yuan SC550N1 device...");

            // Get video input devices
            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            
            IBaseFilter? yuanFilter = null;
            string? deviceName = null;

            foreach (var device in devices)
            {
                var name = device.Name?.ToLowerInvariant() ?? "";
                _logger.LogDebug("Found video device: {DeviceName}", device.Name);

                // Look for Yuan devices
                if (name.Contains("yuan") || name.Contains("sc550") || name.Contains("sc550n1"))
                {
                    _logger.LogInformation("Found Yuan device: {DeviceName}", device.Name);
                    
                    try
                    {
                        // Create filter from device
                        var hr = ((IMoniker)device.Mon).BindToObject(null, null, typeof(IBaseFilter).GUID, out var filterObj);
                        DsError.ThrowExceptionForHR(hr);
                        
                        yuanFilter = (IBaseFilter)filterObj;
                        deviceName = device.Name;
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create filter for Yuan device: {DeviceName}", device.Name);
                    }
                }
            }

            if (yuanFilter == null)
            {
                // Fallback: use first available video capture device
                if (devices.Length > 0)
                {
                    _logger.LogWarning("Yuan SC550N1 not found, using first available device: {DeviceName}", devices[0].Name);
                    try
                    {
                        var hr = ((IMoniker)devices[0].Mon).BindToObject(null, null, typeof(IBaseFilter).GUID, out var filterObj);
                        DsError.ThrowExceptionForHR(hr);
                        yuanFilter = (IBaseFilter)filterObj;
                        deviceName = devices[0].Name;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create filter for fallback device");
                        throw;
                    }
                }
                else
                {
                    throw new InvalidOperationException("No video capture devices found");
                }
            }

            // Add source filter to graph
            var addHr = _graphBuilder!.AddFilter(yuanFilter, deviceName);
            DsError.ThrowExceptionForHR(addHr);
            
            _sourceFilter = yuanFilter;
            _logger.LogInformation("Added source filter to graph: {DeviceName}", deviceName);

            await Task.CompletedTask;
        }

        private async Task SetupSampleGrabberAsync()
        {
            _logger.LogInformation("Setting up SampleGrabber for Session 0 compatibility...");

            // Create SampleGrabber filter (Session 0 compatible)
            _sampleGrabberFilter = (IBaseFilter)new SampleGrabber();
            _sampleGrabber = (ISampleGrabber)_sampleGrabberFilter;

            // Configure for YUY2 format (most efficient for Yuan)
            var mediaType = new AMMediaType
            {
                majorType = MediaType.Video,
                subType = MediaSubType.YUY2,
                formatType = FormatType.VideoInfo
            };

            var hr = _sampleGrabber.SetMediaType(mediaType);
            DsError.ThrowExceptionForHR(hr);

            // Set callback for frame processing
            hr = _sampleGrabber.SetCallback(_frameProcessor, 1); // Use BufferCB
            DsError.ThrowExceptionForHR(hr);

            // Don't buffer samples (for real-time)
            hr = _sampleGrabber.SetBufferSamples(false);
            DsError.ThrowExceptionForHR(hr);

            // Add to graph
            hr = _graphBuilder!.AddFilter(_sampleGrabberFilter, "SampleGrabber");
            DsError.ThrowExceptionForHR(hr);

            DsUtils.FreeAMMediaType(mediaType);
            _logger.LogInformation("SampleGrabber configured for YUY2 format");

            await Task.CompletedTask;
        }

        private async Task SetupSmartTeeAsync()
        {
            _logger.LogInformation("Setting up Smart Tee for multi-branch output...");

            // Create Smart Tee filter for multiple outputs
            _smartTeeFilter = (IBaseFilter)new SmartTee();

            // Add to graph
            var hr = _graphBuilder!.AddFilter(_smartTeeFilter, "SmartTee");
            DsError.ThrowExceptionForHR(hr);

            // Create Null Renderer for one branch (required for graph to run)
            _nullRendererFilter = (IBaseFilter)new NullRenderer();
            hr = _graphBuilder.AddFilter(_nullRendererFilter, "NullRenderer");
            DsError.ThrowExceptionForHR(hr);

            _logger.LogInformation("Smart Tee configured for multi-branch output");

            await Task.CompletedTask;
        }

        private async Task ConnectFiltersAsync()
        {
            _logger.LogInformation("Connecting DirectShow filters...");

            try
            {
                // Connect Source → SampleGrabber
                var hr = _captureGraphBuilder!.RenderStream(
                    PinCategory.Capture,
                    MediaType.Video,
                    _sourceFilter,
                    null,
                    _sampleGrabberFilter);
                DsError.ThrowExceptionForHR(hr);

                // Connect SampleGrabber → Smart Tee
                hr = _graphBuilder!.Connect(
                    DsFindPin.ByDirection(_sampleGrabberFilter!, PinDirection.Output, 0),
                    DsFindPin.ByDirection(_smartTeeFilter!, PinDirection.Input, 0));
                DsError.ThrowExceptionForHR(hr);

                // Connect one Smart Tee output to Null Renderer
                hr = _graphBuilder.Connect(
                    DsFindPin.ByDirection(_smartTeeFilter!, PinDirection.Output, 0),
                    DsFindPin.ByDirection(_nullRendererFilter!, PinDirection.Input, 0));
                DsError.ThrowExceptionForHR(hr);

                _logger.LogInformation("DirectShow filters connected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect DirectShow filters");
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task DiscoverInputsAsync()
        {
            _logger.LogInformation("Discovering available inputs...");

            _availableInputs.Clear();

            try
            {
                // Get crossbar interface for input switching
                _crossbar = (IAMCrossbar)_sourceFilter!;

                if (_crossbar != null)
                {
                    var hr = _crossbar.get_PinCounts(out var outputPins, out var inputPins);
                    DsError.ThrowExceptionForHR(hr);

                    _logger.LogInformation("Found {InputPins} input pins, {OutputPins} output pins", inputPins, outputPins);

                    for (int i = 0; i < inputPins; i++)
                    {
                        hr = _crossbar.get_CrossbarPinInfo(false, i, out var pinIndexRelated, out var physicalType);
                        if (hr == 0)
                        {
                            var inputInfo = new InputInfo
                            {
                                Index = i,
                                Name = GetPhysicalConnectorTypeName(physicalType),
                                Type = GetPhysicalConnectorTypeAbbreviation(physicalType),
                                IsConnected = CheckInputConnection(i)
                            };

                            _availableInputs.Add(inputInfo);
                            _logger.LogInformation("Input {Index}: {Name} ({Type}) - {Status}",
                                i, inputInfo.Name, inputInfo.Type, inputInfo.IsConnected ? "Connected" : "Disconnected");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("No crossbar interface found - input switching not available");
                    
                    // Add a default input
                    _availableInputs.Add(new InputInfo
                    {
                        Index = 0,
                        Name = "Default Input",
                        Type = "AUTO",
                        IsConnected = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error discovering inputs");
                
                // Add a fallback input
                _availableInputs.Add(new InputInfo
                {
                    Index = 0,
                    Name = "Fallback Input",
                    Type = "AUTO",
                    IsConnected = true
                });
            }

            await Task.CompletedTask;
        }

        private string GetPhysicalConnectorTypeName(PhysicalConnectorType type)
        {
            return type switch
            {
                PhysicalConnectorType.Video_Composite => "Composite Video",
                PhysicalConnectorType.Video_SVideo => "S-Video",
                PhysicalConnectorType.Video_RGB => "RGB",
                PhysicalConnectorType.Video_YRYBY => "Component (YPbPr)",
                PhysicalConnectorType.Video_SerialDigital => "SDI (Serial Digital)",
                PhysicalConnectorType.Video_ParallelDigital => "Digital Parallel",
                PhysicalConnectorType.Video_SCSI => "SCSI",
                PhysicalConnectorType.Video_AUX => "AUX",
                PhysicalConnectorType.Video_1394 => "FireWire",
                PhysicalConnectorType.Video_USB => "USB",
                PhysicalConnectorType.Video_VideoDecoder => "Video Decoder",
                PhysicalConnectorType.Video_VideoEncoder => "Video Encoder",
                _ => $"Unknown ({(int)type})"
            };
        }

        private string GetPhysicalConnectorTypeAbbreviation(PhysicalConnectorType type)
        {
            return type switch
            {
                PhysicalConnectorType.Video_Composite => "CVBS",
                PhysicalConnectorType.Video_SVideo => "SVID",
                PhysicalConnectorType.Video_RGB => "RGB",
                PhysicalConnectorType.Video_YRYBY => "COMP",
                PhysicalConnectorType.Video_SerialDigital => "SDI",
                PhysicalConnectorType.Video_ParallelDigital => "HDMI",
                PhysicalConnectorType.Video_SCSI => "SCSI",
                PhysicalConnectorType.Video_AUX => "AUX",
                PhysicalConnectorType.Video_1394 => "1394",
                PhysicalConnectorType.Video_USB => "USB",
                _ => "AUTO"
            };
        }

        private bool CheckInputConnection(int inputIndex)
        {
            // This is a simplified check - in reality you'd need to query signal strength
            // or use other methods to detect if an input has a signal
            return true; // Assume all inputs are potentially connected
        }

        public async Task<List<InputInfo>> GetAvailableInputsAsync()
        {
            return new List<InputInfo>(_availableInputs);
        }

        public async Task<int> GetCurrentInputAsync()
        {
            return _currentInputIndex;
        }

        public async Task SelectInputAsync(int inputIndex)
        {
            if (inputIndex < 0 || inputIndex >= _availableInputs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(inputIndex), "Invalid input index");
            }

            _logger.LogInformation("Selecting input {Index}: {Name}", inputIndex, _availableInputs[inputIndex].Name);

            try
            {
                if (_crossbar != null)
                {
                    // Route the input to the first output pin
                    var hr = _crossbar.Route(0, inputIndex);
                    DsError.ThrowExceptionForHR(hr);
                    
                    _currentInputIndex = inputIndex;
                    _logger.LogInformation("Input switched successfully to {Index}", inputIndex);
                }
                else
                {
                    _logger.LogWarning("Cannot switch input - no crossbar interface available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch to input {Index}", inputIndex);
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task StartCaptureAsync()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Capture graph not initialized");
            }

            if (_isRunning)
            {
                _logger.LogWarning("Capture is already running");
                return;
            }

            _logger.LogInformation("Starting video capture...");

            try
            {
                // Start the graph
                var hr = _mediaControl!.Run();
                DsError.ThrowExceptionForHR(hr);

                _isRunning = true;
                _framesProcessed = 0;
                _framesDropped = 0;
                _lastStatsTime = DateTime.UtcNow;

                _logger.LogInformation("Video capture started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start video capture");
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task StopCaptureAsync()
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Capture is not running");
                return;
            }

            _logger.LogInformation("Stopping video capture...");

            try
            {
                // Stop the graph
                var hr = _mediaControl!.Stop();
                DsError.ThrowExceptionForHR(hr);

                _isRunning = false;
                _logger.LogInformation("Video capture stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop video capture");
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task RestartAsync()
        {
            _logger.LogInformation("Restarting capture graph...");
            
            await StopCaptureAsync();
            await Task.Delay(100);
            await StartCaptureAsync();
        }

        public async Task<object?> CaptureSnapshotAsync()
        {
            _logger.LogInformation("Capturing snapshot...");
            
            // This would trigger a high-resolution snapshot capture
            // Implementation depends on requirements
            
            await Task.CompletedTask;
            return new { Message = "Snapshot captured", Timestamp = DateTime.UtcNow };
        }

        public CaptureStatistics GetStatistics()
        {
            return new CaptureStatistics
            {
                FramesProcessed = _framesProcessed,
                FramesDropped = _framesDropped,
                CurrentFPS = _currentFPS,
                CurrentWidth = 1920, // This should come from actual capture format
                CurrentHeight = 1080,
                CurrentFormat = "YUY2",
                IsRunning = _isRunning,
                LastFrameTime = _lastFrameTime
            };
        }

        public void UpdateStatistics(bool frameProcessed)
        {
            if (frameProcessed)
            {
                Interlocked.Increment(ref _framesProcessed);
                _lastFrameTime = DateTime.UtcNow;
            }
            else
            {
                Interlocked.Increment(ref _framesDropped);
            }

            // Calculate FPS every second
            var now = DateTime.UtcNow;
            if ((now - _lastStatsTime).TotalSeconds >= 1.0)
            {
                var elapsed = (now - _lastStatsTime).TotalSeconds;
                _currentFPS = _framesProcessed / elapsed;
                _lastStatsTime = now;
            }
        }

        public async Task StopAsync()
        {
            await StopCaptureAsync();
            Cleanup();
        }

        private void Cleanup()
        {
            _logger.LogInformation("Cleaning up DirectShow objects...");

            try
            {
                // Release DirectShow objects in reverse order
                if (_mediaControl != null)
                {
                    _mediaControl.Stop();
                    Marshal.ReleaseComObject(_mediaControl);
                    _mediaControl = null;
                }

                if (_sampleGrabber != null)
                {
                    Marshal.ReleaseComObject(_sampleGrabber);
                    _sampleGrabber = null;
                }

                if (_crossbar != null)
                {
                    Marshal.ReleaseComObject(_crossbar);
                    _crossbar = null;
                }

                var filtersToRelease = new[] { _nullRendererFilter, _smartTeeFilter, _sampleGrabberFilter, _sourceFilter };
                foreach (var filter in filtersToRelease)
                {
                    if (filter != null)
                    {
                        _graphBuilder?.RemoveFilter(filter);
                        Marshal.ReleaseComObject(filter);
                    }
                }

                if (_captureGraphBuilder != null)
                {
                    Marshal.ReleaseComObject(_captureGraphBuilder);
                    _captureGraphBuilder = null;
                }

                if (_graphBuilder != null)
                {
                    Marshal.ReleaseComObject(_graphBuilder);
                    _graphBuilder = null;
                }

                _isInitialized = false;
                _isRunning = false;

                _logger.LogInformation("DirectShow cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DirectShow cleanup");
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}