using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartBoxNext.Controllers;
using SmartBoxNext.Services.Video;
using System.Text.Json.Serialization;

namespace SmartBoxNext.Services.Video
{
    public class VideoApiStartup
    {
        public VideoApiStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC controllers
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            // Add CORS
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            // Register video services
            services.AddSingleton<VideoSourceManager>();
            services.AddSingleton<IVideoEngine, FFmpegVideoEngine>();
            
            // Add WebSocket support
            services.AddSignalR();
            
            // Add logging
            services.AddLogging();

            // Configure settings
            services.Configure<VideoEngineSettings>(Configuration.GetSection("SmartBox:VideoEngine"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors();

            // Enable WebSocket support
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = System.TimeSpan.FromSeconds(120)
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // WebSocket endpoint
                endpoints.Map("/ws/video/{sessionId}", async context =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var sessionId = context.Request.RouteValues["sessionId"]?.ToString();
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        
                        var handler = context.RequestServices.GetRequiredService<VideoWebSocketHandler>();
                        await handler.HandleConnection(context, webSocket, sessionId);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                });
                
                // Health check
                endpoints.MapGet("/api/health", async context =>
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        status = "healthy",
                        service = "SmartBox Video Engine API",
                        timestamp = System.DateTime.UtcNow
                    });
                });
            });
        }
    }

    public class VideoEngineSettings
    {
        public string Type { get; set; } = "FFmpeg";
        public string FFmpegPath { get; set; } = "ffmpeg";
        public bool EnableTestSource { get; set; } = false;
        public SourceSettings Source { get; set; } = new();
        public RecordingSettings Recording { get; set; } = new();
        public PreviewSettings Preview { get; set; } = new();
        public StorageSettings Storage { get; set; } = new();
    }

    public class SourceSettings
    {
        public string PreferredType { get; set; } = "MedicalGrabber";
        public string PreferredDevice { get; set; } = "YUAN SC542N6";
        public bool FallbackToWebcam { get; set; } = true;
    }

    public class RecordingSettings
    {
        public string QualityPreset { get; set; } = "Medical";
        public string MasterCodec { get; set; } = "FFV1";
        public string Resolution { get; set; } = "1920x1080";
        public int FrameRate { get; set; } = 60;
        public string PixelFormat { get; set; } = "yuv422p";
        public int PreRecordSeconds { get; set; } = 60;
        public int SegmentDuration { get; set; } = 10;
    }

    public class PreviewSettings
    {
        public string Codec { get; set; } = "H264";
        public int Bitrate { get; set; } = 5000;
        public string Protocol { get; set; } = "HLS";
        public string Latency { get; set; } = "UltraLow";
    }

    public class StorageSettings
    {
        public string RecordingPath { get; set; } = "D:\\SmartBoxRecordings";
        public string TempPath { get; set; } = "D:\\SmartBoxTemp";
        public string SegmentNaming { get; set; } = "segment_{0:D5}.mkv";
        public bool AutoCleanup { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
    }
}