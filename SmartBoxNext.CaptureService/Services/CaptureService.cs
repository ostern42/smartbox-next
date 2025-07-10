using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.CaptureService.Services
{
    /// <summary>
    /// Main Windows Service for Yuan SC550N1 video capture
    /// </summary>
    public class CaptureService : BackgroundService
    {
        private readonly ILogger<CaptureService> _logger;
        private readonly SharedMemoryManager _sharedMemoryManager;
        private readonly ControlPipeServer _controlPipeServer;
        private readonly YuanCaptureGraph _captureGraph;
        private readonly FrameProcessor _frameProcessor;

        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();

        private const uint COINIT_MULTITHREADED = 0x0;
        private const uint COINIT_APARTMENTTHREADED = 0x2;

        public CaptureService(
            ILogger<CaptureService> logger,
            SharedMemoryManager sharedMemoryManager,
            ControlPipeServer controlPipeServer,
            YuanCaptureGraph captureGraph,
            FrameProcessor frameProcessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sharedMemoryManager = sharedMemoryManager ?? throw new ArgumentNullException(nameof(sharedMemoryManager));
            _controlPipeServer = controlPipeServer ?? throw new ArgumentNullException(nameof(controlPipeServer));
            _captureGraph = captureGraph ?? throw new ArgumentNullException(nameof(captureGraph));
            _frameProcessor = frameProcessor ?? throw new ArgumentNullException(nameof(frameProcessor));
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SmartBoxNext Capture Service starting...");

            try
            {
                // Initialize COM with MTA threading for Session 0 compatibility
                var comResult = CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
                if (comResult != 0 && comResult != 1) // S_OK or S_FALSE
                {
                    _logger.LogError("Failed to initialize COM: HRESULT = 0x{ComResult:X8}", comResult);
                    throw new InvalidOperationException($"COM initialization failed with HRESULT 0x{comResult:X8}");
                }

                _logger.LogInformation("COM initialized with MTA threading");

                // Check if running in Session 0 (Windows Service context)
                var sessionId = GetCurrentSessionId();
                _logger.LogInformation("Running in Session {SessionId}", sessionId);

                if (sessionId == 0)
                {
                    _logger.LogInformation("Running in Session 0 - Windows Service mode");
                }
                else
                {
                    _logger.LogWarning("Not running in Session 0 - this may cause issues with hardware access");
                }

                // Initialize SharedMemory
                _logger.LogInformation("Initializing SharedMemory...");
                await _sharedMemoryManager.InitializeAsync();

                // Initialize Control Pipe Server
                _logger.LogInformation("Starting Control Pipe Server...");
                await _controlPipeServer.StartAsync();

                // Initialize DirectShow capture graph
                _logger.LogInformation("Initializing DirectShow capture graph...");
                await _captureGraph.InitializeAsync();

                // Initialize Frame Processor
                _logger.LogInformation("Initializing Frame Processor...");
                await _frameProcessor.InitializeAsync();

                _logger.LogInformation("SmartBoxNext Capture Service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Capture Service");
                throw;
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Capture Service main loop starting...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Service main loop - monitor health, handle commands, etc.
                    await Task.Delay(1000, stoppingToken);

                    // Check if capture is still running
                    if (!_captureGraph.IsRunning)
                    {
                        _logger.LogWarning("Capture graph is not running, attempting to restart...");
                        try
                        {
                            await _captureGraph.RestartAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to restart capture graph");
                        }
                    }

                    // Check SharedMemory health
                    if (!_sharedMemoryManager.IsHealthy)
                    {
                        _logger.LogWarning("SharedMemory is not healthy, attempting to reinitialize...");
                        try
                        {
                            await _sharedMemoryManager.ReinitializeAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to reinitialize SharedMemory");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Capture Service main loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Capture Service main loop");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SmartBoxNext Capture Service stopping...");

            try
            {
                // Stop components in reverse order
                await _frameProcessor.StopAsync();
                await _captureGraph.StopAsync();
                await _controlPipeServer.StopAsync();
                await _sharedMemoryManager.StopAsync();

                // Uninitialize COM
                CoUninitialize();
                _logger.LogInformation("COM uninitialized");

                _logger.LogInformation("SmartBoxNext Capture Service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Capture Service");
            }

            await base.StopAsync(cancellationToken);
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        private static extern uint ProcessIdToSessionId(uint dwProcessId, out uint pSessionId);

        private uint GetCurrentSessionId()
        {
            var processId = GetCurrentProcessId();
            ProcessIdToSessionId(processId, out var sessionId);
            return sessionId;
        }
    }
}