using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// Simple Kestrel-based streaming API (no admin rights needed!)
    /// </summary>
    public class KestrelStreamingApi : IDisposable
    {
        private readonly ILogger<KestrelStreamingApi> _logger;
        private readonly int _port;
        private IHost? _host;
        
        // Simple in-memory user store
        private readonly Dictionary<string, string> _users = new()
        {
            { "admin", "SmartBox2024!" },
            { "operator", "SmartBox2024!" },
            { "viewer", "SmartBox2024!" }
        };

        public KestrelStreamingApi(ILogger<KestrelStreamingApi> logger, int port = 5002)
        {
            _logger = logger;
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
                                // Enable CORS for all origins
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

                                // Enable static files from wwwroot
                                app.UseStaticFiles();
                                
                                app.UseRouting();
                                // Enhanced debug logging middleware
                                app.Use(async (context, next) =>
                                {
                                    var request = context.Request;
                                    _logger.LogInformation($"=== INCOMING REQUEST ===");
                                    _logger.LogInformation($"Method: {request.Method}");
                                    _logger.LogInformation($"Path: {request.Path}");
                                    _logger.LogInformation($"QueryString: {request.QueryString}");
                                    _logger.LogInformation($"ContentType: {request.ContentType}");
                                    _logger.LogInformation($"ContentLength: {request.ContentLength}");
                                    _logger.LogInformation($"Headers: {string.Join(", ", request.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                                    
                                    await next();
                                    
                                    _logger.LogInformation($"Response Status: {context.Response.StatusCode}");
                                    _logger.LogInformation($"=== REQUEST COMPLETED ===");
                                });

                                app.UseEndpoints(endpoints =>
                                {
                                    // Health check endpoint
                                    endpoints.MapGet("/api/health", async context =>
                                    {
                                        _logger.LogInformation("Health check requested");
                                        context.Response.ContentType = "application/json";
                                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                                        {
                                            status = "healthy",
                                            service = "SmartBox Streaming API (Kestrel)",
                                            timestamp = DateTime.UtcNow,
                                            port = _port
                                        }));
                                    });

                                    // Login endpoint
                                    endpoints.MapPost("/api/auth/login", async context =>
                                    {
                                        _logger.LogInformation("🔑 LOGIN ENDPOINT REACHED!");
                                        _logger.LogInformation($"Login request received from {context.Request.Headers["User-Agent"]}");
                                        
                                        try
                                        {
                                            using var reader = new StreamReader(context.Request.Body);
                                            var body = await reader.ReadToEndAsync();
                                            _logger.LogInformation($"Login request body: {body}");
                                            
                                            var loginRequest = JsonSerializer.Deserialize<LoginRequest>(body, new JsonSerializerOptions
                                            {
                                                PropertyNameCaseInsensitive = true
                                            });

                                            if (loginRequest != null && 
                                                _users.TryGetValue(loginRequest.username, out var password) &&
                                                password == loginRequest.password)
                                            {
                                                // Generate simple token (in production, use proper JWT)
                                                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{loginRequest.username}:{DateTime.UtcNow.Ticks}"));
                                                
                                                var response = JsonSerializer.Serialize(new
                                                {
                                                    access_token = token,
                                                    expires_in = 28800,
                                                    user = new
                                                    {
                                                        username = loginRequest.username,
                                                        role = loginRequest.username == "admin" ? "Administrator" : "Operator"
                                                    }
                                                });
                                                
                                                context.Response.ContentType = "application/json";
                                                context.Response.StatusCode = 200;
                                                await context.Response.WriteAsync(response);
                                                _logger.LogInformation($"Login successful for user: {loginRequest.username}");
                                            }
                                            else
                                            {
                                                var errorResponse = JsonSerializer.Serialize(new { error = "Invalid credentials" });
                                                context.Response.ContentType = "application/json";
                                                context.Response.StatusCode = 401;
                                                await context.Response.WriteAsync(errorResponse);
                                                _logger.LogWarning($"Login failed for user: {loginRequest?.username ?? "unknown"}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError(ex, "Login error");
                                            var errorResponse = JsonSerializer.Serialize(new { error = "Invalid request", details = ex.Message });
                                            context.Response.ContentType = "application/json";
                                            context.Response.StatusCode = 400;
                                            await context.Response.WriteAsync(errorResponse);
                                        }
                                    });

                                    // Simple stream start endpoint (mock)
                                    endpoints.MapPost("/api/stream/start", async context =>
                                    {
                                        // Check for auth header
                                        if (!context.Request.Headers.ContainsKey("Authorization"))
                                        {
                                            context.Response.StatusCode = 401;
                                            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
                                            return;
                                        }

                                        var sessionId = Guid.NewGuid().ToString();
                                        context.Response.ContentType = "application/json";
                                        await context.Response.WriteAsync(JsonSerializer.Serialize(new
                                        {
                                            sessionId = sessionId,
                                            streamUrl = $"/api/stream/{sessionId}/stream.m3u8",
                                            startTime = DateTime.UtcNow
                                        }));
                                    });

                                    // Catch-all for unmatched routes
                                    endpoints.MapFallback(async context =>
                                    {
                                        context.Response.StatusCode = 404;
                                        context.Response.ContentType = "application/json";
                                        await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                                        { 
                                            error = "Not found",
                                            path = context.Request.Path.Value,
                                            availableEndpoints = new[]
                                            {
                                                "GET /api/health",
                                                "POST /api/auth/login",
                                                "POST /api/stream/start (requires auth)"
                                            }
                                        }));
                                    });
                                });
                            })
                            .ConfigureLogging(logging =>
                            {
                                logging.ClearProviders();
                                logging.AddConsole();
                                
                                // Add file logging for debugging
                                var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                                Directory.CreateDirectory(logDirectory);
                                var logFile = Path.Combine(logDirectory, $"kestrel_api_{DateTime.Now:yyyyMMdd}.log");
                                
                                // Simple file logger
                                logging.AddProvider(new SimpleFileLoggerProvider(logFile));
                                logging.SetMinimumLevel(LogLevel.Debug);
                            });
                    })
                    .Build();

                await _host.StartAsync();
                _logger.LogInformation($"Kestrel API started on port {_port} - NO ADMIN RIGHTS NEEDED!");
                _logger.LogInformation($"Test with: http://localhost:{_port}/api/health");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Kestrel API");
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

        private class LoginRequest
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
    }

    // Simple file logger for debugging
    public class SimpleFileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;

        public SimpleFileLoggerProvider(string filePath)
        {
            _filePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleFileLogger(_filePath, categoryName);
        }

        public void Dispose() { }
    }

    public class SimpleFileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly string _categoryName;
        private readonly object _lock = new object();

        public SimpleFileLogger(string filePath, string categoryName)
        {
            _filePath = filePath;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += Environment.NewLine + exception.ToString();
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_filePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Ignore file write errors to prevent circular logging
                }
            }
        }
    }
}