using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;

namespace SmartBoxNext
{
    /// <summary>
    /// Manages all streaming-related services
    /// </summary>
    public class StreamingServerManager : IDisposable
    {
        private readonly ILogger<StreamingServerManager> _logger;
        private readonly AuthenticationService _authService;
        private readonly HLSStreamingService _streamingService;
        private readonly StreamingApiService _apiService;
        private readonly FFmpegService _ffmpegService;
        private readonly string _hlsOutputDirectory;
        
        private bool _isRunning = false;
        private Task? _apiTask;
        
        public StreamingServerManager(ILogger<StreamingServerManager> logger, FFmpegService ffmpegService, string outputDirectory)
        {
            _logger = logger;
            _ffmpegService = ffmpegService;
            _hlsOutputDirectory = outputDirectory;
            
            // Initialize services
            _authService = new AuthenticationService();
            
            var streamingLogger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<HLSStreamingService>();
            _streamingService = new HLSStreamingService(streamingLogger, _ffmpegService, _hlsOutputDirectory);
            
            var apiLogger = LoggerFactory.Create(builder => builder.AddConsole())
                .CreateLogger<StreamingApiService>();
            _apiService = new StreamingApiService(apiLogger, _authService, _streamingService);
            
            // Subscribe to streaming events
            _streamingService.StreamingEvent += OnStreamingEvent;
        }
        
        public async Task StartAsync()
        {
            if (_isRunning)
                return;
            
            _logger.LogInformation("Starting streaming server manager...");
            
            // Start API service on a long-running background thread
            _apiTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    await _apiService.StartAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "API service crashed");
                }
            }, TaskCreationOptions.LongRunning).Unwrap();
            
            // Give the API a moment to start
            await Task.Delay(500);
            
            _isRunning = true;
            
            // Create default users for testing
            CreateDefaultUsers();
            
            // Verify API is running
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    var response = await client.GetAsync("http://localhost:5002/api/health");
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogInformation("✓ API verified running on port 5002");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not verify API status: {ex.Message}");
            }
            
            _logger.LogInformation("Streaming server manager started");
            _logger.LogInformation("API endpoint: http://localhost:5002");
            _logger.LogInformation("Default login: admin / SmartBox2024!");
        }
        
        public async Task StopAsync()
        {
            if (!_isRunning)
                return;
            
            _logger.LogInformation("Stopping streaming server manager...");
            
            // Stop API service
            _apiService.Stop();
            
            if (_apiTask != null)
            {
                try
                {
                    await _apiTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("API service did not stop in time");
                }
            }
            
            // Dispose streaming service
            _streamingService.Dispose();
            
            _isRunning = false;
            _logger.LogInformation("Streaming server manager stopped");
        }
        
        private void CreateDefaultUsers()
        {
            // Admin user (already created in AuthenticationService constructor)
            
            // Operator user
            _authService.CreateUser("operator", "SmartBox2024!", UserRole.Operator, "Operator User");
            
            // Viewer user
            _authService.CreateUser("viewer", "SmartBox2024!", UserRole.Viewer, "Viewer User");
            
            _logger.LogInformation("Default users created:");
            _logger.LogInformation("  admin / SmartBox2024! (Administrator)");
            _logger.LogInformation("  operator / SmartBox2024! (Operator)");
            _logger.LogInformation("  viewer / SmartBox2024! (Viewer)");
        }
        
        private void OnStreamingEvent(object? sender, StreamingEventArgs e)
        {
            switch (e.EventType)
            {
                case StreamingEventType.SessionStarted:
                    _logger.LogInformation($"Streaming session started: {e.SessionId}");
                    break;
                case StreamingEventType.SessionStopped:
                    _logger.LogInformation($"Streaming session stopped: {e.SessionId}");
                    break;
                case StreamingEventType.SegmentCreated:
                    if (e.Data is SegmentInfo segment)
                    {
                        _logger.LogDebug($"Segment created: {segment.FileName} ({segment.FileSize} bytes)");
                    }
                    break;
                case StreamingEventType.InPointMarked:
                    _logger.LogInformation($"In point marked at {e.Data}");
                    break;
                case StreamingEventType.OutPointMarked:
                    _logger.LogInformation($"Out point marked at {e.Data}");
                    break;
                case StreamingEventType.Error:
                    _logger.LogError($"Streaming error: {e.Data}");
                    break;
            }
        }
        
        public void Dispose()
        {
            StopAsync().Wait(TimeSpan.FromSeconds(10));
            _streamingService?.Dispose();
        }
    }
}