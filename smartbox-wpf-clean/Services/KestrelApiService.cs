using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Alternative API service using Kestrel (no admin rights needed)
    /// </summary>
    public class KestrelApiService
    {
        private readonly ILogger<KestrelApiService> _logger;
        private readonly AuthenticationService _authService;
        private readonly HLSStreamingService _streamingService;
        private readonly int _port;
        private IHost? _host;

        public KestrelApiService(
            ILogger<KestrelApiService> logger,
            AuthenticationService authService,
            HLSStreamingService streamingService,
            int port = 5002)
        {
            _logger = logger;
            _authService = authService;
            _streamingService = streamingService;
            _port = port;
        }

        public async Task StartAsync()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel()
                        .UseUrls($"http://localhost:{_port}", $"http://127.0.0.1:{_port}")
                        .Configure(app =>
                        {
                            app.UseRouting();
                            app.UseCors(builder =>
                            {
                                builder.AllowAnyOrigin()
                                       .AllowAnyMethod()
                                       .AllowAnyHeader();
                            });

                            app.UseEndpoints(endpoints =>
                            {
                                // Health check
                                endpoints.MapGet("/api/health", async context =>
                                {
                                    await context.Response.WriteAsJsonAsync(new
                                    {
                                        status = "healthy",
                                        service = "SmartBox Streaming API",
                                        timestamp = DateTime.UtcNow
                                    });
                                });

                                // Login endpoint
                                endpoints.MapPost("/api/auth/login", async context =>
                                {
                                    var request = await context.Request.ReadFromJsonAsync<LoginRequest>();
                                    if (request == null || string.IsNullOrEmpty(request.Username))
                                    {
                                        context.Response.StatusCode = 400;
                                        await context.Response.WriteAsJsonAsync(new { error = "Invalid request" });
                                        return;
                                    }

                                    var result = await _authService.AuthenticateAsync(request.Username, request.Password);
                                    
                                    if (result.Success)
                                    {
                                        await context.Response.WriteAsJsonAsync(new
                                        {
                                            access_token = result.AccessToken,
                                            refresh_token = result.RefreshToken,
                                            expires_in = result.ExpiresIn,
                                            user = result.User
                                        });
                                    }
                                    else
                                    {
                                        context.Response.StatusCode = 401;
                                        await context.Response.WriteAsJsonAsync(new { error = result.Error });
                                    }
                                });

                                // Add other endpoints here...
                            });
                        })
                        .ConfigureServices(services =>
                        {
                            services.AddCors();
                            services.AddRouting();
                        });
                })
                .Build();

            await _host.StartAsync();
            _logger.LogInformation($"Kestrel API started on port {_port} (no admin rights needed!)");
        }

        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }

        private class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}