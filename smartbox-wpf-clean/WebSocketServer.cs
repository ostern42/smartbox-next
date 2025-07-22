using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    public class WebSocketServer
    {
        private readonly ILogger<WebSocketServer> _logger;
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, WebSocket> _connections;
        private readonly int _port;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _serverTask;

        public event EventHandler<AdminMessageEventArgs> AdminMessageReceived;

        public WebSocketServer(ILogger<WebSocketServer> logger, int port = 5001)
        {
            _logger = logger;
            _port = port;
            _connections = new ConcurrentDictionary<string, WebSocket>();
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
                        _ = Task.Run(() => HandleWebSocketRequestAsync(context));
                    }
                    catch (HttpListenerException) when (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "WebSocket server error");
                    }
                }
            }, _cancellationTokenSource.Token);

            _logger.LogInformation($"WebSocket server started on port {_port}");
        }

        public async Task StopAsync()
        {
            if (!_listener.IsListening)
                return;

            _cancellationTokenSource?.Cancel();
            _listener.Stop();
            _listener.Close();

            // Close all WebSocket connections
            foreach (var connection in _connections.Values)
            {
                try
                {
                    await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server stopping", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket connection");
                }
            }

            if (_serverTask != null)
            {
                try
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning("WebSocket server task did not complete in time");
                }
            }

            _cancellationTokenSource?.Dispose();
            _logger.LogInformation("WebSocket server stopped");
        }

        private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
        {
            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                return;
            }

            try
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var connectionId = Guid.NewGuid().ToString();

                _connections[connectionId] = webSocket;
                _logger.LogInformation($"WebSocket connection established: {connectionId}");

                // Send initial status
                await SendSystemStatusAsync(connectionId);

                await HandleWebSocketConnectionAsync(connectionId, webSocket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket connection");
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
        }

        private async Task HandleWebSocketConnectionAsync(string connectionId, WebSocket webSocket)
        {
            var buffer = new byte[4096];

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ProcessAdminMessageAsync(connectionId, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, $"WebSocket connection {connectionId} closed unexpectedly");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in WebSocket connection {connectionId}");
            }
            finally
            {
                _connections.TryRemove(connectionId, out _);
                _logger.LogInformation($"WebSocket connection {connectionId} removed");
            }
        }

        private async Task ProcessAdminMessageAsync(string connectionId, string message)
        {
            try
            {
                var adminMessage = JsonSerializer.Deserialize<AdminMessage>(message);
                _logger.LogDebug($"Received admin message: {adminMessage.Type} from {connectionId}");

                AdminMessageReceived?.Invoke(this, new AdminMessageEventArgs(connectionId, adminMessage));

                // Send acknowledgment
                await SendToConnectionAsync(connectionId, new AdminMessage
                {
                    Type = "ack",
                    Data = new { originalType = adminMessage.Type, status = "received" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing admin message from {connectionId}");
            }
        }

        public async Task BroadcastStatusAsync(object statusData)
        {
            var message = new AdminMessage
            {
                Type = "status_update",
                Data = statusData,
                Timestamp = DateTime.UtcNow
            };

            await BroadcastMessageAsync(message);
        }

        public async Task BroadcastRecordingStateAsync(object recordingData)
        {
            var message = new AdminMessage
            {
                Type = "recording_state",
                Data = recordingData,
                Timestamp = DateTime.UtcNow
            };

            await BroadcastMessageAsync(message);
        }

        public async Task SendSystemStatusAsync(string connectionId)
        {
            var systemStatus = new
            {
                cpu_usage = await GetCpuUsageAsync(),
                memory_usage = GetMemoryUsage(),
                storage_info = GetStorageInfo(),
                recording_status = GetRecordingStatus(),
                patient_count = GetPatientCount(),
                queue_size = GetQueueSize()
            };

            await SendToConnectionAsync(connectionId, new AdminMessage
            {
                Type = "system_status",
                Data = systemStatus,
                Timestamp = DateTime.UtcNow
            });
        }

        private async Task BroadcastMessageAsync(AdminMessage message)
        {
            var messageJson = JsonSerializer.Serialize(message);
            var messageBytes = Encoding.UTF8.GetBytes(messageJson);

            var tasks = new List<Task>();

            foreach (var kvp in _connections)
            {
                if (kvp.Value.State == WebSocketState.Open)
                {
                    tasks.Add(SendMessageAsync(kvp.Value, messageBytes));
                }
                else
                {
                    // Remove closed connections
                    _connections.TryRemove(kvp.Key, out _);
                }
            }

            await Task.WhenAll(tasks);
        }

        public async Task SendToConnectionAsync(string connectionId, AdminMessage message)
        {
            if (_connections.TryGetValue(connectionId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var messageJson = JsonSerializer.Serialize(message);
                var messageBytes = Encoding.UTF8.GetBytes(messageJson);
                await SendMessageAsync(webSocket, messageBytes);
            }
        }

        private async Task SendMessageAsync(WebSocket webSocket, byte[] messageBytes)
        {
            try
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send WebSocket message");
            }
        }

        // System monitoring methods
        private async Task<double> GetCpuUsageAsync()
        {
            // Simplified CPU usage calculation
            // In production, you might want to use PerformanceCounter or similar
            return await Task.FromResult(Math.Round(new Random().NextDouble() * 100, 1));
        }

        private double GetMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            return Math.Round(totalMemory / 1024.0 / 1024.0, 1); // MB
        }

        private object GetStorageInfo()
        {
            try
            {
                var driveInfo = new System.IO.DriveInfo("C:\\");
                return new
                {
                    total_gb = Math.Round(driveInfo.TotalSize / 1024.0 / 1024.0 / 1024.0, 1),
                    free_gb = Math.Round(driveInfo.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 1),
                    used_percentage = Math.Round((1.0 - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize) * 100, 1)
                };
            }
            catch
            {
                return new { total_gb = 0, free_gb = 0, used_percentage = 0 };
            }
        }

        private object GetRecordingStatus()
        {
            return new
            {
                is_recording = false,
                duration_seconds = 0,
                file_size_mb = 0,
                frame_rate = 0,
                resolution = "1920x1080"
            };
        }

        private int GetPatientCount()
        {
            // This should integrate with your MWL service
            return 0;
        }

        private int GetQueueSize()
        {
            // This should integrate with your queue manager
            return 0;
        }

        public int ConnectionCount => _connections.Count;
    }

    public class AdminMessage
    {
        public string Type { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AdminMessageEventArgs : EventArgs
    {
        public string ConnectionId { get; }
        public AdminMessage Message { get; }

        public AdminMessageEventArgs(string connectionId, AdminMessage message)
        {
            ConnectionId = connectionId;
            Message = message;
        }
    }
}