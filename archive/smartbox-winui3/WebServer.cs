using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartBoxNext
{
    public class WebServer
    {
        private readonly HttpListener _listener;
        private readonly string _rootPath;
        private readonly int _port;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;

        private readonly Dictionary<string, string> _mimeTypes = new()
        {
            { ".html", "text/html" },
            { ".css", "text/css" },
            { ".js", "application/javascript" },
            { ".json", "application/json" },
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

        public WebServer(string rootPath, int port = 5000)
        {
            _rootPath = rootPath;
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        }

        public async Task StartAsync()
        {
            if (_listener.IsListening)
                return;

            _cancellationTokenSource = new CancellationTokenSource();
            _listener.Start();
            
            _serverTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(context));
                    }
                    catch (HttpListenerException) when (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        // Expected when stopping the server
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Server error: {ex.Message}");
                    }
                }
            }, _cancellationTokenSource.Token);

            Console.WriteLine($"Web server started on http://localhost:{_port}");
        }

        public async Task StopAsync()
        {
            if (!_listener.IsListening)
                return;

            _cancellationTokenSource?.Cancel();
            _listener.Stop();
            
            if (_serverTask != null)
                await _serverTask;

            _cancellationTokenSource?.Dispose();
            Console.WriteLine("Web server stopped");
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Get the requested path
                var path = request.Url.LocalPath;
                if (path == "/")
                    path = "/index.html";

                // Security: Prevent directory traversal
                path = path.Replace("..", "").Replace("//", "/");
                
                var filePath = Path.Combine(_rootPath, path.TrimStart('/'));

                if (File.Exists(filePath))
                {
                    // Serve the file
                    var extension = Path.GetExtension(filePath).ToLower();
                    if (_mimeTypes.TryGetValue(extension, out var mimeType))
                    {
                        response.ContentType = mimeType;
                    }
                    else
                    {
                        response.ContentType = "application/octet-stream";
                    }

                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    response.ContentLength64 = fileBytes.Length;
                    
                    await response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
                else
                {
                    // 404 Not Found
                    response.StatusCode = 404;
                    var errorMessage = Encoding.UTF8.GetBytes("404 - File not found");
                    response.ContentLength64 = errorMessage.Length;
                    await response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
                }
            }
            catch (Exception ex)
            {
                // 500 Internal Server Error
                response.StatusCode = 500;
                var errorMessage = Encoding.UTF8.GetBytes($"500 - Internal server error: {ex.Message}");
                response.ContentLength64 = errorMessage.Length;
                await response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        public string GetServerUrl()
        {
            return $"http://localhost:{_port}";
        }
    }
}