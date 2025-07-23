using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services.Video
{
    public class VideoWebSocketHandler
    {
        private readonly IVideoEngine _videoEngine;
        private readonly ILogger<VideoWebSocketHandler> _logger;
        private readonly ConcurrentDictionary<string, WebSocketConnection> _connections;
        
        public VideoWebSocketHandler(IVideoEngine videoEngine, ILogger<VideoWebSocketHandler> logger)
        {
            _videoEngine = videoEngine;
            _logger = logger;
            _connections = new ConcurrentDictionary<string, WebSocketConnection>();
            
            // Subscribe to video engine events
            _videoEngine.RecordingEvent += OnRecordingEvent;
            _videoEngine.SegmentCompleted += OnSegmentCompleted;
            _videoEngine.Error += OnError;
        }
        
        public async Task HandleConnection(HttpContext context, WebSocket webSocket, string sessionId)
        {
            var connectionId = Guid.NewGuid().ToString();
            var connection = new WebSocketConnection
            {
                Id = connectionId,
                SessionId = sessionId,
                WebSocket = webSocket
            };
            
            _connections[connectionId] = connection;
            _logger.LogInformation("WebSocket connected: {ConnectionId} for session {SessionId}", connectionId, sessionId);
            
            try
            {
                // Send initial status
                var session = _videoEngine.GetSession(sessionId);
                if (session != null)
                {
                    await SendMessage(connection, new
                    {
                        type = "RecordingStatus",
                        timestamp = DateTime.UtcNow,
                        data = new
                        {
                            sessionId = session.SessionId,
                            status = session.Status.ToString(),
                            startTime = session.StartTime,
                            segments = session.Segments.Count
                        }
                    });
                }
                
                // Keep connection alive
                var buffer = new ArraySegment<byte>(new byte[4096]);
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                        await HandleMessage(connection, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket error for connection {ConnectionId}", connectionId);
            }
            finally
            {
                _connections.TryRemove(connectionId, out _);
                
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                }
                
                _logger.LogInformation("WebSocket disconnected: {ConnectionId}", connectionId);
            }
        }
        
        private async Task HandleMessage(WebSocketConnection connection, string message)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<WebSocketMessage>(message);
                if (msg == null) return;
                
                _logger.LogDebug("Received WebSocket message: {Type} for session {SessionId}", 
                    msg.Type, connection.SessionId);
                
                switch (msg.Type?.ToLowerInvariant())
                {
                    case "ping":
                        await SendMessage(connection, new { type = "pong", timestamp = DateTime.UtcNow });
                        break;
                        
                    case "getstatus":
                        var status = _videoEngine.GetStatus(connection.SessionId);
                        await SendMessage(connection, new 
                        { 
                            type = "RecordingStatus",
                            timestamp = DateTime.UtcNow,
                            data = new { status = status.ToString() }
                        });
                        break;
                        
                    case "getsegments":
                        var segments = await _videoEngine.GetEditableSegments(connection.SessionId);
                        await SendMessage(connection, new
                        {
                            type = "SegmentList",
                            timestamp = DateTime.UtcNow,
                            data = segments
                        });
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown WebSocket message type: {Type}", msg.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling WebSocket message");
            }
        }
        
        private async Task SendMessage(WebSocketConnection connection, object message)
        {
            if (connection.WebSocket.State != WebSocketState.Open)
                return;
            
            try
            {
                var json = JsonSerializer.Serialize(message);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                await connection.WebSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WebSocket message");
            }
        }
        
        private async Task BroadcastToSession(string sessionId, object message)
        {
            var tasks = new List<Task>();
            
            foreach (var connection in _connections.Values)
            {
                if (connection.SessionId == sessionId)
                {
                    tasks.Add(SendMessage(connection, message));
                }
            }
            
            await Task.WhenAll(tasks);
        }
        
        private async void OnRecordingEvent(object sender, RecordingEventArgs e)
        {
            await BroadcastToSession(e.SessionId, new
            {
                type = e.EventType.ToString(),
                timestamp = e.Timestamp,
                data = e.Data
            });
        }
        
        private async void OnSegmentCompleted(object sender, SegmentEventArgs e)
        {
            await BroadcastToSession(e.SessionId, new
            {
                type = "SegmentCompleted",
                timestamp = DateTime.UtcNow,
                data = new
                {
                    segmentNumber = e.Segment.Number,
                    fileName = e.Segment.FileName,
                    duration = e.Segment.Duration.TotalSeconds,
                    fileSize = e.Segment.FileSize,
                    isComplete = e.Segment.IsComplete
                }
            });
        }
        
        private async void OnError(object sender, ErrorEventArgs e)
        {
            await BroadcastToSession(e.SessionId, new
            {
                type = "Error",
                timestamp = DateTime.UtcNow,
                data = new
                {
                    message = e.Message,
                    severity = e.Severity.ToString()
                }
            });
        }
        
        private class WebSocketConnection
        {
            public string Id { get; set; }
            public string SessionId { get; set; }
            public WebSocket WebSocket { get; set; }
        }
        
        private class WebSocketMessage
        {
            public string Type { get; set; }
            public JsonElement Data { get; set; }
        }
    }
}