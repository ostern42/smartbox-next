using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// Lightweight web server for serving the HTML/CSS/JS UI
    /// </summary>
    public class WebServer : IDisposable
    {
        private readonly string _rootPath;
        private readonly int _port;
        private readonly ILogger<WebServer> _logger;
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        
        private static readonly Dictionary<string, string> MimeTypes = new()
        {
            { ".html", "text/html; charset=utf-8" },
            { ".css", "text/css; charset=utf-8" },
            { ".js", "application/javascript; charset=utf-8" },
            { ".json", "application/json; charset=utf-8" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".svg", "image/svg+xml" },
            { ".ico", "image/x-icon" },
            { ".woff", "font/woff" },
            { ".woff2", "font/woff2" },
            { ".ttf", "font/ttf" },
            { ".otf", "font/otf" }
        };
        
        public WebServer(string rootPath, int port)
        {
            _rootPath = Path.GetFullPath(rootPath);
            _port = port;
            
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<WebServer>();
            
            if (!Directory.Exists(_rootPath))
            {
                throw new DirectoryNotFoundException($"Web root directory not found: {_rootPath}");
            }
        }
        
        public Task StartAsync()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Web server is already running");
            }
            
            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
            
            try
            {
                _listener.Start();
                _logger.LogInformation("Web server started on port {Port}, serving from {Root}", _port, _rootPath);
                
                _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
                return Task.CompletedTask;
            }
            catch (HttpListenerException ex)
            {
                _logger.LogError(ex, "Failed to start web server. Port {Port} may be in use.", _port);
                _listener?.Close();
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                throw new InvalidOperationException($"Failed to start web server on port {_port}. The port may be in use.", ex);
            }
        }
        
        public async Task StopAsync()
        {
            if (_listener == null)
            {
                return;
            }
            
            _logger.LogInformation("Stopping web server...");
            
            try
            {
                _cancellationTokenSource?.Cancel();
                
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
                _listener.Close();
                
                if (_listenerTask != null)
                {
                    try
                    {
                        await _listenerTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping web server");
            }
            finally
            {
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
            
            _logger.LogInformation("Web server stopped");
        }
        
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var contextTask = _listener!.GetContextAsync();
                    var completedTask = await Task.WhenAny(contextTask, Task.Delay(-1, cancellationToken));
                    
                    if (completedTask == contextTask)
                    {
                        var context = await contextTask;
                        _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Expected when stopping
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    // Expected when stopping on Windows
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in web server listener");
                }
            }
        }
        
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                _logger.LogDebug("Request: {Method} {Url}", request.HttpMethod, request.Url?.AbsolutePath);
                
                // Set CORS headers for local development
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }
                
                // Get the requested file path
                var urlPath = request.Url?.AbsolutePath ?? "/";
                if (urlPath == "/")
                {
                    urlPath = "/index.html";
                }
                
                // Security: Prevent directory traversal
                urlPath = urlPath.Replace("..", "").Replace("//", "/");
                
                var filePath = Path.Combine(_rootPath, urlPath.TrimStart('/'));
                
                if (File.Exists(filePath))
                {
                    await ServeFileAsync(filePath, response);
                }
                else
                {
                    await Serve404Async(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling request");
                
                try
                {
                    context.Response.StatusCode = 500;
                    var buffer = Encoding.UTF8.GetBytes("Internal Server Error");
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    // Best effort
                }
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch
                {
                    // Best effort
                }
            }
        }
        
        private async Task ServeFileAsync(string filePath, HttpListenerResponse response)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (MimeTypes.TryGetValue(extension, out var mimeType))
            {
                response.ContentType = mimeType;
            }
            else
            {
                response.ContentType = "application/octet-stream";
            }
            
            // Set caching headers
            if (extension == ".html")
            {
                response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            }
            else
            {
                response.Headers.Add("Cache-Control", "public, max-age=3600");
            }
            
            var fileInfo = new FileInfo(filePath);
            response.ContentLength64 = fileInfo.Length;
            response.StatusCode = 200;
            
            using (var fileStream = File.OpenRead(filePath))
            {
                await fileStream.CopyToAsync(response.OutputStream);
            }
            
            _logger.LogDebug("Served file: {File} ({Size} bytes)", filePath, fileInfo.Length);
        }
        
        private async Task Serve404Async(HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.ContentType = "text/html; charset=utf-8";
            
            var html = @"<!DOCTYPE html>
<html>
<head>
    <title>404 - Not Found</title>
    <style>
        body { font-family: 'Segoe UI', sans-serif; text-align: center; padding: 50px; }
        h1 { color: #0078D4; }
    </style>
</head>
<body>
    <h1>404 - Page Not Found</h1>
    <p>The requested resource was not found.</p>
    <a href='/'>Return to Home</a>
</body>
</html>";
            
            var buffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            
            _logger.LogWarning("404 Not Found");
        }
        
        public void Dispose()
        {
            StopAsync().Wait(TimeSpan.FromSeconds(5));
            _cancellationTokenSource?.Dispose();
        }
    }
}