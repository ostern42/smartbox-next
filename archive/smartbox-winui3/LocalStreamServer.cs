using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace SmartBoxNext
{
    /// <summary>
    /// Local HTTP server for MJPEG streaming
    /// Access via http://localhost:8080/stream
    /// </summary>
    public sealed partial class LocalStreamServer : IDisposable
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private SoftwareBitmap? _currentFrame;
        private readonly object _frameLock = new object();
        private bool _isRunning;
        
        public event Action<string>? DebugMessage;
        
        public async Task<bool> StartAsync(int port = 8080)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();
                
                // Start handling requests
                _ = Task.Run(() => HandleRequestsAsync(_cancellationTokenSource.Token));
                
                DebugMessage?.Invoke($"Stream server started at http://localhost:{port}/stream");
                return true;
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Failed to start stream server: {ex.Message}");
                return false;
            }
        }
        
        public void UpdateFrame(SoftwareBitmap frame)
        {
            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = new SoftwareBitmap(frame.BitmapPixelFormat, frame.PixelWidth, frame.PixelHeight, frame.BitmapAlphaMode);
                frame.CopyTo(_currentFrame);
            }
        }
        
        private async Task HandleRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var context = await _listener!.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        DebugMessage?.Invoke($"Request handling error: {ex.Message}");
                    }
                }
            }
        }
        
        private async Task HandleRequest(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                
                if (request.Url?.AbsolutePath == "/stream")
                {
                    // MJPEG stream
                    response.ContentType = "multipart/x-mixed-replace; boundary=frame";
                    response.StatusCode = 200;
                    
                    using (var output = response.OutputStream)
                    {
                        while (_isRunning)
                        {
                            byte[]? jpegData = null;
                            
                            lock (_frameLock)
                            {
                                if (_currentFrame != null)
                                {
                                    jpegData = ConvertToJpeg(_currentFrame).Result;
                                }
                            }
                            
                            if (jpegData != null)
                            {
                                var header = System.Text.Encoding.ASCII.GetBytes(
                                    "--frame\r\n" +
                                    "Content-Type: image/jpeg\r\n" +
                                    $"Content-Length: {jpegData.Length}\r\n\r\n");
                                
                                await output.WriteAsync(header, 0, header.Length);
                                await output.WriteAsync(jpegData, 0, jpegData.Length);
                                await output.WriteAsync(new byte[] { 13, 10 }, 0, 2); // \r\n
                            }
                            
                            await Task.Delay(33); // ~30 FPS
                        }
                    }
                }
                else if (request.Url?.AbsolutePath == "/")
                {
                    // Simple viewer page
                    var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>SmartBox Stream</title>
    <style>
        body { margin: 0; background: #000; display: flex; justify-content: center; align-items: center; height: 100vh; }
        img { max-width: 100%; max-height: 100%; }
    </style>
</head>
<body>
    <img src='/stream' />
</body>
</html>";
                    
                    var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                }
                else if (request.Url?.AbsolutePath == "/webrtc")
                {
                    // Serve WebRTC page
                    var webrtcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "webrtc.html");
                    if (File.Exists(webrtcPath))
                    {
                        var html = await File.ReadAllTextAsync(webrtcPath);
                        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
                        response.ContentType = "text/html";
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }
                    response.Close();
                }
                else
                {
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                DebugMessage?.Invoke($"Request error: {ex.Message}");
            }
        }
        
        private async Task<byte[]> ConvertToJpeg(SoftwareBitmap bitmap)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);
                encoder.SetSoftwareBitmap(bitmap);
                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                encoder.IsThumbnailGenerated = false;
                
                await encoder.FlushAsync();
                
                var bytes = new byte[stream.Size];
                stream.Seek(0);
                using (var reader = new Windows.Storage.Streams.DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(bytes);
                }
                
                return bytes;
            }
        }
        
        public void Stop()
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            
            lock (_frameLock)
            {
                _currentFrame?.Dispose();
                _currentFrame = null;
            }
        }
        
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}