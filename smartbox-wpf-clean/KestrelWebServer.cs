using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Medical;

namespace SmartBoxNext
{
    /// <summary>
    /// Kestrel-based web server for static files (no admin rights needed!)
    /// </summary>
    public class KestrelWebServer : IDisposable
    {
        private readonly ILogger<KestrelWebServer> _logger;
        private readonly string _rootPath;
        private readonly int _port;
        private IHost? _host;

        public KestrelWebServer(ILogger<KestrelWebServer> logger, string rootPath, int port = MedicalConstants.SMARTBOX_WEB_PORT)
        {
            _logger = logger;
            _rootPath = rootPath;
            _port = port;
        }

        public async Task StartAsync()
        {
            try
            {
                _host = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseKestrel(options =>
                            {
                                options.ListenLocalhost(_port);
                            })
                            .Configure(app =>
                            {
                                // Enable CORS
                                app.Use(async (context, next) =>
                                {
                                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                                    context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                                    context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                                    
                                    if (context.Request.Method == "OPTIONS")
                                    {
                                        context.Response.StatusCode = 204;
                                        return;
                                    }
                                    
                                    await next();
                                });

                                // Serve static files
                                if (Directory.Exists(_rootPath))
                                {
                                    app.UseStaticFiles(new StaticFileOptions
                                    {
                                        FileProvider = new PhysicalFileProvider(_rootPath),
                                        RequestPath = "",
                                        ServeUnknownFileTypes = true,
                                        DefaultContentType = "application/octet-stream"
                                    });
                                }

                                // Default route to index.html
                                app.UseRouting();
                                app.UseEndpoints(endpoints =>
                                {
                                    endpoints.MapGet("/", async context =>
                                    {
                                        var indexPath = Path.Combine(_rootPath, "index.html");
                                        if (File.Exists(indexPath))
                                        {
                                            context.Response.ContentType = "text/html";
                                            await context.Response.SendFileAsync(indexPath);
                                        }
                                        else
                                        {
                                            context.Response.StatusCode = 404;
                                            await context.Response.WriteAsync($"Index file not found at: {indexPath}");
                                        }
                                    });

                                    // Health check for web server
                                    endpoints.MapGet("/health", async context =>
                                    {
                                        context.Response.ContentType = "application/json";
                                        await context.Response.WriteAsync($@"{{
    ""status"": ""healthy"",
    ""service"": ""SmartBox Web Server (Kestrel)"",
    ""timestamp"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffffffZ}"",
    ""port"": {_port},
    ""rootPath"": ""{_rootPath.Replace("\\", "\\\\")}"",
    ""indexExists"": {File.Exists(Path.Combine(_rootPath, "index.html")).ToString().ToLower()}
}}");
                                    });

                                    // Fallback for unknown routes
                                    endpoints.MapFallback(async context =>
                                    {
                                        context.Response.StatusCode = 404;
                                        await context.Response.WriteAsync("File not found");
                                    });
                                });
                            })
                            .ConfigureLogging(logging =>
                            {
                                logging.ClearProviders();
                                logging.AddConsole();
                            });
                    })
                    .Build();

                await _host.StartAsync();
                _logger.LogInformation($"Kestrel Web Server started on port {_port} - NO ADMIN RIGHTS NEEDED!");
                _logger.LogInformation($"Serving files from: {_rootPath}");
                _logger.LogInformation($"Access at: http://localhost:{_port}/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Kestrel Web Server");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }
        }

        public void Dispose()
        {
            _host?.Dispose();
        }
    }
}