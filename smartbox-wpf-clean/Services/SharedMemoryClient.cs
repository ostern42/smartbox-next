using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedMemory;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Frame header structure (must match service definition)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameHeader
    {
        public long Timestamp;          // Hardware timestamp (ticks)
        public int FrameNumber;         // Sequential frame number
        public int Width;               // Frame width
        public int Height;              // Frame height
        public int PixelFormat;         // Pixel format (YUY2 = 1, RGB = 2, etc.)
        public int DataSize;            // Size of frame data in bytes
        public bool IsKeyFrame;         // For video recording
        public byte Reserved1;          // Padding
        public byte Reserved2;          // Padding
        public byte Reserved3;          // Padding
    }

    /// <summary>
    /// Control command for service communication
    /// </summary>
    public class ServiceCommand
    {
        public string Command { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public string? RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Service response
    /// </summary>
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public string? RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Frame received event args
    /// </summary>
    public class FrameReceivedEventArgs : EventArgs
    {
        public FrameHeader Header { get; set; }
        public byte[] FrameData { get; set; } = Array.Empty<byte>();
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// SharedMemory client for receiving frames from capture service
    /// </summary>
    public class SharedMemoryClient : IDisposable
    {
        private readonly ILogger<SharedMemoryClient> _logger;
        
        // SharedMemory configuration (must match service)
        private const string SHARED_MEMORY_NAME = "SmartBoxNextVideo";
        private const string CONTROL_PIPE_NAME = "SmartBoxNextControl";
        
        private CircularBuffer? _circularBuffer;
        private NamedPipeClientStream? _controlPipe;
        private bool _isConnected = false;
        private bool _isReceiving = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _frameReaderTask;
        private Task? _controlPipeTask;

        // Statistics
        private long _framesReceived = 0;
        private long _framesDropped = 0;
        private DateTime _lastFrameTime = DateTime.MinValue;
        private DateTime _connectionTime = DateTime.MinValue;

        // Events
        public event EventHandler<FrameReceivedEventArgs>? FrameReceived;
        public event EventHandler<bool>? ConnectionStateChanged;

        public bool IsConnected => _isConnected;
        public bool IsReceiving => _isReceiving;
        public long FramesReceived => _framesReceived;
        public long FramesDropped => _framesDropped;
        public DateTime LastFrameTime => _lastFrameTime;

        public SharedMemoryClient(ILogger<SharedMemoryClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ConnectAsync(int timeoutMs = 5000)
        {
            if (_isConnected)
            {
                _logger.LogWarning("Already connected to capture service");
                return true;
            }

            _logger.LogInformation("Connecting to SmartBoxNext Capture Service...");

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                // Connect to SharedMemory
                await ConnectToSharedMemoryAsync();

                // Connect to Control Pipe
                await ConnectToControlPipeAsync(timeoutMs);

                // Start frame reader
                _frameReaderTask = Task.Run(FrameReaderLoopAsync);

                // Start control pipe reader
                _controlPipeTask = Task.Run(ControlPipeLoopAsync);

                _isConnected = true;
                _connectionTime = DateTime.UtcNow;
                _framesReceived = 0;
                _framesDropped = 0;

                _logger.LogInformation("Connected to capture service successfully");
                ConnectionStateChanged?.Invoke(this, true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to capture service");
                await DisconnectAsync();
                return false;
            }
        }

        private async Task ConnectToSharedMemoryAsync()
        {
            _logger.LogInformation("Connecting to SharedMemory: {Name}", SHARED_MEMORY_NAME);

            try
            {
                // Connect to existing SharedMemory created by service
                _circularBuffer = new CircularBuffer(SHARED_MEMORY_NAME);
                _logger.LogInformation("Connected to SharedMemory successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SharedMemory");
                throw new InvalidOperationException("Capture service may not be running", ex);
            }

            await Task.CompletedTask;
        }

        private async Task ConnectToControlPipeAsync(int timeoutMs)
        {
            _logger.LogInformation("Connecting to Control Pipe: {Name}", CONTROL_PIPE_NAME);

            try
            {
                _controlPipe = new NamedPipeClientStream(".", CONTROL_PIPE_NAME, PipeDirection.InOut);
                await _controlPipe.ConnectAsync(timeoutMs);
                
                _logger.LogInformation("Connected to Control Pipe successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Control Pipe");
                throw new InvalidOperationException("Cannot communicate with capture service", ex);
            }
        }

        private async Task FrameReaderLoopAsync()
        {
            _logger.LogInformation("Frame reader loop starting...");
            _isReceiving = true;

            try
            {
                while (!_cancellationTokenSource!.Token.IsCancellationRequested && _circularBuffer != null)
                {
                    try
                    {
                        // TODO: Implement proper SharedMemory.CircularBuffer API
                        // For now, just simulate waiting for frames to get build working
                        await Task.Delay(10, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading frame from SharedMemory");
                        Interlocked.Increment(ref _framesDropped);
                        
                        // Wait a bit before retrying
                        await Task.Delay(100, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Frame reader loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Frame reader loop error");
            }
            finally
            {
                _isReceiving = false;
                _logger.LogInformation("Frame reader loop ended");
            }
        }

        private async Task ControlPipeLoopAsync()
        {
            _logger.LogInformation("Control pipe loop starting...");

            try
            {
                using var reader = new StreamReader(_controlPipe!, Encoding.UTF8, leaveOpen: true);
                
                while (!_cancellationTokenSource!.Token.IsCancellationRequested && _controlPipe!.IsConnected)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line))
                        {
                            _logger.LogWarning("Control pipe disconnected");
                            break;
                        }

                        // Handle incoming service messages (if needed)
                        _logger.LogDebug("Received from service: {Message}", line);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading from control pipe");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Control pipe loop error");
            }
            finally
            {
                _logger.LogInformation("Control pipe loop ended");
            }
        }

        public async Task<ServiceResponse?> SendCommandAsync(string command, string? parameters = null, int timeoutMs = 5000)
        {
            if (!_isConnected || _controlPipe == null || !_controlPipe.IsConnected)
            {
                throw new InvalidOperationException("Not connected to capture service");
            }

            var requestId = Guid.NewGuid().ToString();
            var serviceCommand = new ServiceCommand
            {
                Command = command,
                Parameters = parameters,
                RequestId = requestId
            };

            try
            {
                _logger.LogDebug("Sending command: {Command} (ID: {RequestId})", command, requestId);

                using var writer = new StreamWriter(_controlPipe, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
                using var reader = new StreamReader(_controlPipe, Encoding.UTF8, leaveOpen: true);

                // Send command
                var commandJson = JsonConvert.SerializeObject(serviceCommand);
                await writer.WriteLineAsync(commandJson);

                // Wait for response
                using var cts = new CancellationTokenSource(timeoutMs);
                var responseJson = await reader.ReadLineAsync();

                if (string.IsNullOrEmpty(responseJson))
                {
                    throw new InvalidOperationException("No response from service");
                }

                var response = JsonConvert.DeserializeObject<ServiceResponse>(responseJson);
                
                _logger.LogDebug("Received response: {Success} - {Message}", response?.Success, response?.Message);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command: {Command}", command);
                throw;
            }
        }

        public async Task<bool> StartCaptureAsync()
        {
            var response = await SendCommandAsync("start");
            return response?.Success == true;
        }

        public async Task<bool> StopCaptureAsync()
        {
            var response = await SendCommandAsync("stop");
            return response?.Success == true;
        }

        public async Task<object?> GetAvailableInputsAsync()
        {
            var response = await SendCommandAsync("getinputs");
            return response?.Data;
        }

        public async Task<bool> SelectInputAsync(int inputIndex)
        {
            var response = await SendCommandAsync("selectinput", inputIndex.ToString());
            return response?.Success == true;
        }

        public async Task<int?> GetCurrentInputAsync()
        {
            var response = await SendCommandAsync("getcurrentinput");
            if (response?.Success == true && response.Data is int currentInput)
            {
                return currentInput;
            }
            return null;
        }

        public async Task<object?> CaptureSnapshotAsync()
        {
            var response = await SendCommandAsync("snapshot");
            return response?.Data;
        }

        public async Task<object?> GetServiceStatisticsAsync()
        {
            var response = await SendCommandAsync("getstats");
            return response?.Data;
        }

        public async Task<bool> PingServiceAsync()
        {
            try
            {
                var response = await SendCommandAsync("ping");
                return response?.Success == true && response.Message == "Pong";
            }
            catch
            {
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            _logger.LogInformation("Disconnecting from capture service...");

            try
            {
                _isConnected = false;
                _isReceiving = false;

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Wait for tasks to complete
                var tasks = new List<Task>();
                if (_frameReaderTask != null) tasks.Add(_frameReaderTask);
                if (_controlPipeTask != null) tasks.Add(_controlPipeTask);

                if (tasks.Count > 0)
                {
                    await Task.WhenAll(tasks);
                }

                // Clean up resources
                _controlPipe?.Dispose();
                _controlPipe = null;

                _circularBuffer?.Dispose();
                _circularBuffer = null;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _logger.LogInformation("Disconnected from capture service");
                ConnectionStateChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disconnect");
            }
        }

        public void Dispose()
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}