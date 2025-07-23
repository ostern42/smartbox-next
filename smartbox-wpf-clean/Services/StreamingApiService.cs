using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;

namespace SmartBoxNext
{
    /// <summary>
    /// HTTP API service for video streaming with authentication
    /// </summary>
    public class StreamingApiService
    {
        private readonly ILogger<StreamingApiService> _logger;
        private readonly AuthenticationService _authService;
        private readonly HLSStreamingService _streamingService;
        private readonly HttpListener _listener;
        private readonly int _port;
        private readonly string _corsOrigin = "*";
        
        public StreamingApiService(
            ILogger<StreamingApiService> logger,
            AuthenticationService authService,
            HLSStreamingService streamingService,
            int port = 5002)
        {
            _logger = logger;
            _authService = authService;
            _streamingService = streamingService;
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        }
        
        public async Task StartAsync()
        {
            _listener.Start();
            _logger.LogInformation($"Streaming API started on port {_port}");
            
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling request");
                }
            }
        }
        
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            _logger.LogInformation("Streaming API stopped");
        }
        
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            // Add CORS headers
            response.Headers.Add("Access-Control-Allow-Origin", _corsOrigin);
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
            response.Headers.Add("Access-Control-Expose-Headers", "Content-Length, Content-Range");
            
            // Handle preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }
            
            try
            {
                var path = request.Url?.AbsolutePath ?? "/";
                var method = request.HttpMethod;
                
                _logger.LogDebug($"Handling request: {method} {path}");
                
                // Health check endpoint
                if (path == "/api/health" && method == "GET")
                {
                    await SendJsonResponseAsync(response, new { 
                        status = "healthy", 
                        service = "SmartBox Streaming API",
                        timestamp = DateTime.UtcNow
                    });
                    return;
                }
                
                // Public endpoints (no auth required)
                if (path == "/api/auth/login" && method == "POST")
                {
                    await HandleLoginAsync(request, response);
                    return;
                }
                
                if (path == "/api/auth/refresh" && method == "POST")
                {
                    await HandleRefreshTokenAsync(request, response);
                    return;
                }
                
                // All other endpoints require authentication
                var principal = ValidateAuthToken(request);
                if (principal == null)
                {
                    await SendUnauthorizedAsync(response);
                    return;
                }
                
                // Authenticated endpoints
                if (path.StartsWith("/api/stream/") && method == "GET")
                {
                    await HandleStreamRequestAsync(request, response, principal);
                }
                else if (path == "/api/stream/start" && method == "POST")
                {
                    await HandleStartStreamAsync(request, response, principal);
                }
                else if (path == "/api/stream/stop" && method == "POST")
                {
                    await HandleStopStreamAsync(request, response, principal);
                }
                else if (path.StartsWith("/api/stream/mark/") && method == "POST")
                {
                    await HandleMarkPointAsync(request, response, principal);
                }
                else if (path.StartsWith("/api/stream/export/") && method == "GET")
                {
                    await HandleExportRangeAsync(request, response, principal);
                }
                else if (path == "/api/users" && method == "GET")
                {
                    await HandleGetUsersAsync(request, response, principal);
                }
                else if (path == "/api/users" && method == "POST")
                {
                    await HandleCreateUserAsync(request, response, principal);
                }
                else
                {
                    response.StatusCode = 404;
                    await SendJsonResponseAsync(response, new { 
                        error = "Not found",
                        path = path,
                        method = method,
                        availableEndpoints = new[]
                        {
                            "GET /api/health",
                            "POST /api/auth/login",
                            "POST /api/auth/refresh",
                            "POST /api/stream/start (requires auth)",
                            "POST /api/stream/stop (requires auth)",
                            "GET /api/stream/{sessionId}/stream.m3u8 (requires auth)",
                            "POST /api/stream/mark/{sessionId}/in (requires auth)",
                            "POST /api/stream/mark/{sessionId}/out (requires auth)",
                            "GET /api/users (requires admin)",
                            "POST /api/users (requires admin)"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                response.StatusCode = 500;
                await SendJsonResponseAsync(response, new { error = "Internal server error" });
            }
        }
        
        private async Task HandleLoginAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBodyAsync<LoginRequest>(request);
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            var result = await _authService.AuthenticateAsync(body.Username, body.Password);
            
            if (result.Success)
            {
                await SendJsonResponseAsync(response, new
                {
                    access_token = result.AccessToken,
                    refresh_token = result.RefreshToken,
                    expires_in = result.ExpiresIn,
                    user = result.User
                });
            }
            else
            {
                response.StatusCode = 401;
                await SendJsonResponseAsync(response, new { error = result.Error });
            }
        }
        
        private async Task HandleRefreshTokenAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var body = await ReadRequestBodyAsync<RefreshTokenRequest>(request);
            if (body == null || string.IsNullOrEmpty(body.RefreshToken))
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            var result = await _authService.RefreshTokenAsync(body.RefreshToken);
            
            if (result.Success)
            {
                await SendJsonResponseAsync(response, new
                {
                    access_token = result.AccessToken,
                    refresh_token = result.RefreshToken,
                    expires_in = result.ExpiresIn,
                    user = result.User
                });
            }
            else
            {
                response.StatusCode = 401;
                await SendJsonResponseAsync(response, new { error = result.Error });
            }
        }
        
        private async Task HandleStreamRequestAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            var path = request.Url?.AbsolutePath ?? "";
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
            {
                response.StatusCode = 404;
                await SendJsonResponseAsync(response, new { error = "Invalid stream path" });
                return;
            }
            
            var sessionId = parts[2];
            var fileName = parts[3];
            
            if (fileName.EndsWith(".m3u8"))
            {
                // Serve playlist
                var isLive = request.QueryString["dvr"] != "true";
                var playlist = await _streamingService.GetPlaylistAsync(sessionId, isLive);
                
                response.ContentType = "application/vnd.apple.mpegurl";
                response.Headers.Add("Cache-Control", "no-cache");
                var buffer = Encoding.UTF8.GetBytes(playlist);
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else if (fileName.EndsWith(".ts"))
            {
                // Serve segment
                var segment = await _streamingService.GetSegmentAsync(sessionId, fileName);
                if (segment == null)
                {
                    response.StatusCode = 404;
                    await SendJsonResponseAsync(response, new { error = "Segment not found" });
                    return;
                }
                
                response.ContentType = "video/mp2t";
                response.ContentLength64 = segment.Length;
                await response.OutputStream.WriteAsync(segment, 0, segment.Length);
            }
            else
            {
                response.StatusCode = 404;
                await SendJsonResponseAsync(response, new { error = "Invalid file type" });
            }
            
            response.Close();
        }
        
        private async Task HandleStartStreamAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            var body = await ReadRequestBodyAsync<StartStreamRequest>(request);
            if (body == null)
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            // Check permissions
            if (!IsInRole(principal, "Operator", "Administrator"))
            {
                response.StatusCode = 403;
                await SendJsonResponseAsync(response, new { error = "Insufficient permissions" });
                return;
            }
            
            try
            {
                var sessionId = Guid.NewGuid().ToString();
                var input = new StreamInput
                {
                    InputType = body.InputType,
                    DeviceName = body.DeviceName,
                    Url = body.Url,
                    FilePath = body.FilePath
                };
                
                var options = new StreamingOptions
                {
                    EnableDVR = body.EnableDVR ?? true,
                    IncludeAudio = body.IncludeAudio ?? true,
                    VideoCodec = body.VideoCodec ?? "libx264",
                    VideoBitrate = body.VideoBitrate ?? "2000k",
                    AudioBitrate = body.AudioBitrate ?? "128k",
                    Framerate = body.Framerate ?? 30,
                    Resolution = body.Resolution ?? "1280x720"
                };
                
                var session = await _streamingService.StartStreamingSessionAsync(sessionId, input, options);
                
                await SendJsonResponseAsync(response, new
                {
                    sessionId = session.SessionId,
                    streamUrl = $"/api/stream/{session.SessionId}/stream.m3u8",
                    startTime = session.StartTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting stream");
                response.StatusCode = 500;
                await SendJsonResponseAsync(response, new { error = ex.Message });
            }
        }
        
        private async Task HandleStopStreamAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            var body = await ReadRequestBodyAsync<StopStreamRequest>(request);
            if (body == null || string.IsNullOrEmpty(body.SessionId))
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            // Check permissions
            if (!IsInRole(principal, "Operator", "Administrator"))
            {
                response.StatusCode = 403;
                await SendJsonResponseAsync(response, new { error = "Insufficient permissions" });
                return;
            }
            
            await _streamingService.StopStreamingSessionAsync(body.SessionId);
            await SendJsonResponseAsync(response, new { success = true });
        }
        
        private async Task HandleMarkPointAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            var path = request.Url?.AbsolutePath ?? "";
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
            {
                response.StatusCode = 404;
                await SendJsonResponseAsync(response, new { error = "Invalid path" });
                return;
            }
            
            var sessionId = parts[3];
            var markType = parts[4]; // "in" or "out"
            
            var body = await ReadRequestBodyAsync<MarkPointRequest>(request);
            if (body == null)
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            var timestamp = TimeSpan.FromSeconds(body.Timestamp);
            
            if (markType == "in")
            {
                _streamingService.MarkInPoint(sessionId, timestamp);
            }
            else if (markType == "out")
            {
                _streamingService.MarkOutPoint(sessionId, timestamp);
            }
            else
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid mark type" });
                return;
            }
            
            await SendJsonResponseAsync(response, new { success = true, timestamp = timestamp.TotalSeconds });
        }
        
        private async Task HandleExportRangeAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            var path = request.Url?.AbsolutePath ?? "";
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 4)
            {
                response.StatusCode = 404;
                await SendJsonResponseAsync(response, new { error = "Invalid path" });
                return;
            }
            
            var sessionId = parts[3];
            var ranges = _streamingService.GetMarkedRanges(sessionId);
            
            await SendJsonResponseAsync(response, new
            {
                sessionId,
                ranges = ranges.Select(r => new
                {
                    inPoint = r.InPoint.TotalSeconds,
                    outPoint = r.OutPoint?.TotalSeconds,
                    label = r.Label
                })
            });
        }
        
        private async Task HandleGetUsersAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            // Check admin permissions
            if (!IsInRole(principal, "Administrator"))
            {
                response.StatusCode = 403;
                await SendJsonResponseAsync(response, new { error = "Insufficient permissions" });
                return;
            }
            
            // In a real implementation, this would query a database
            await SendJsonResponseAsync(response, new
            {
                users = new[]
                {
                    new { username = "admin", role = "Administrator", displayName = "Admin User" }
                }
            });
        }
        
        private async Task HandleCreateUserAsync(HttpListenerRequest request, HttpListenerResponse response, ClaimsPrincipal principal)
        {
            // Check admin permissions
            if (!IsInRole(principal, "Administrator"))
            {
                response.StatusCode = 403;
                await SendJsonResponseAsync(response, new { error = "Insufficient permissions" });
                return;
            }
            
            var body = await ReadRequestBodyAsync<CreateUserRequest>(request);
            if (body == null || string.IsNullOrEmpty(body.Username) || string.IsNullOrEmpty(body.Password))
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "Invalid request" });
                return;
            }
            
            UserRole role = body.Role switch
            {
                "Administrator" => UserRole.Administrator,
                "Operator" => UserRole.Operator,
                _ => UserRole.Viewer
            };
            
            var success = _authService.CreateUser(body.Username, body.Password, role, body.DisplayName);
            
            if (success)
            {
                await SendJsonResponseAsync(response, new { success = true });
            }
            else
            {
                response.StatusCode = 400;
                await SendJsonResponseAsync(response, new { error = "User already exists" });
            }
        }
        
        private ClaimsPrincipal? ValidateAuthToken(HttpListenerRequest request)
        {
            var authHeader = request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }
            
            var token = authHeader.Substring("Bearer ".Length);
            return _authService.ValidateToken(token);
        }
        
        private bool IsInRole(ClaimsPrincipal principal, params string[] roles)
        {
            return roles.Any(role => principal.IsInRole(role));
        }
        
        private async Task<T?> ReadRequestBodyAsync<T>(HttpListenerRequest request) where T : class
        {
            try
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }
        
        private async Task SendJsonResponseAsync(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var buffer = Encoding.UTF8.GetBytes(json);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }
        
        private async Task SendUnauthorizedAsync(HttpListenerResponse response)
        {
            response.StatusCode = 401;
            response.Headers.Add("WWW-Authenticate", "Bearer");
            await SendJsonResponseAsync(response, new { error = "Unauthorized" });
        }
        
        // Request/Response models
        private class LoginRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        
        private class RefreshTokenRequest
        {
            public string RefreshToken { get; set; } = string.Empty;
        }
        
        private class StartStreamRequest
        {
            public StreamInputType InputType { get; set; }
            public string? DeviceName { get; set; }
            public string? Url { get; set; }
            public string? FilePath { get; set; }
            public bool? EnableDVR { get; set; }
            public bool? IncludeAudio { get; set; }
            public string? VideoCodec { get; set; }
            public string? VideoBitrate { get; set; }
            public string? AudioBitrate { get; set; }
            public int? Framerate { get; set; }
            public string? Resolution { get; set; }
        }
        
        private class StopStreamRequest
        {
            public string SessionId { get; set; } = string.Empty;
        }
        
        private class MarkPointRequest
        {
            public double Timestamp { get; set; }
            public string? Label { get; set; }
        }
        
        private class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? Role { get; set; }
            public string? DisplayName { get; set; }
        }
    }
}