using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Helpers;

namespace SmartBoxNext.Services.Video
{
    /// <summary>
    /// Service to host the Video API within the WPF application
    /// </summary>
    public class VideoApiHostService : IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VideoApiHostService> _logger;
        private readonly int _port;
        private IHost _host;
        private CancellationTokenSource _cancellationTokenSource;
        
        public VideoApiHostService(AppConfig configuration, ILogger<VideoApiHostService> logger)
        {
            _configuration = BuildConfiguration(configuration);
            _logger = logger;
            _port = configuration.Application.WebServerPort;
            _cancellationTokenSource = new CancellationTokenSource();
        }
        
        private IConfiguration BuildConfiguration(AppConfig appConfig)
        {
            // Convert AppConfig to IConfiguration for ASP.NET Core
            var configBuilder = new ConfigurationBuilder();
            
            // Add in-memory configuration
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["SmartBox:VideoEngine:Type"] = "FFmpeg",
                ["SmartBox:VideoEngine:FFmpegPath"] = "ffmpeg",
                ["SmartBox:VideoEngine:EnableTestSource"] = "true",
                ["SmartBox:VideoEngine:Recording:Resolution"] = "1920x1080",
                ["SmartBox:VideoEngine:Recording:FrameRate"] = "60",
                ["SmartBox:VideoEngine:Recording:MasterCodec"] = "FFV1",
                ["SmartBox:VideoEngine:Recording:PixelFormat"] = "yuv422p",
                ["SmartBox:VideoEngine:Recording:PreRecordSeconds"] = "60",
                ["SmartBox:VideoEngine:Recording:SegmentDuration"] = "10",
                ["SmartBox:VideoEngine:Preview:Codec"] = "H264",
                ["SmartBox:VideoEngine:Preview:Bitrate"] = "5000",
                ["SmartBox:VideoEngine:Preview:Protocol"] = "HLS",
                ["SmartBox:VideoEngine:Preview:Latency"] = "UltraLow",
                ["SmartBox:VideoEngine:Storage:RecordingPath"] = "D:\\SmartBoxRecordings",
                ["SmartBox:VideoEngine:Storage:TempPath"] = "D:\\SmartBoxTemp",
                ["SmartBox:VideoEngine:Storage:SegmentNaming"] = "segment_{0:D5}.mkv",
                ["SmartBox:VideoEngine:Storage:AutoCleanup"] = "true",
                ["SmartBox:VideoEngine:Storage:RetentionDays"] = "30",
                ["SmartBox:Storage:VideoPath"] = appConfig.Storage.VideosPath,
                ["SmartBox:Storage:TempPath"] = appConfig.Storage.TempPath
            });
            
            return configBuilder.Build();
        }
        
        public async Task StartAsync()
        {
            try
            {
                _logger.LogInformation("Starting Video API on port {Port}", _port);
                
                _host = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseKestrel(options =>
                            {
                                options.ListenLocalhost(_port);
                            })
                            .UseStartup<VideoApiStartup>()
                            .ConfigureServices(services =>
                            {
                                // Add configuration
                                services.AddSingleton(_configuration);
                                
                                // Register WebSocket handler
                                services.AddSingleton<VideoWebSocketHandler>();
                            });
                    })
                    .ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();
                        logging.AddDebug();
                    })
                    .Build();
                
                await _host.StartAsync(_cancellationTokenSource.Token);
                
                _logger.LogInformation("Video API started successfully on port {Port}", _port);
                _logger.LogInformation("Video API endpoints available at:");
                _logger.LogInformation("  - http://localhost:{Port}/api/video/sources", _port);
                _logger.LogInformation("  - http://localhost:{Port}/api/video/recording/start", _port);
                _logger.LogInformation("  - http://localhost:{Port}/api/health", _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Video API");
                throw;
            }
        }
        
        public async Task StopAsync()
        {
            try
            {
                _logger.LogInformation("Stopping Video API...");
                
                _cancellationTokenSource?.Cancel();
                
                if (_host != null)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    _host.Dispose();
                    _host = null;
                }
                
                _logger.LogInformation("Video API stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Video API");
            }
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _host?.Dispose();
        }
        
        public bool IsRunning => _host != null;
        
        public string BaseUrl => $"http://localhost:{_port}";
    }
}