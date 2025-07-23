using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace SmartBoxNext.Services.Video
{
    public class PreRecordingBuffer : IDisposable
    {
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly int _bufferSize;
        private readonly int _frameSize;
        private readonly int _maxFrames;
        private readonly int _fps;
        private readonly object _lock = new();
        
        private long _writePosition;
        private long _totalFramesWritten;
        private readonly Queue<FrameMetadata> _frameIndex;
        private bool _disposed;
        
        public PreRecordingBuffer(int seconds, int fps, int width, int height)
        {
            _fps = fps;
            
            // Calculate buffer size (YUV422 = 2 bytes per pixel)
            _frameSize = width * height * 2;
            _maxFrames = seconds * fps;
            _bufferSize = _maxFrames * _frameSize;
            
            // Create memory-mapped file
            var mmfName = $"PreRecord_{Guid.NewGuid():N}";
            _mmf = MemoryMappedFile.CreateNew(mmfName, _bufferSize);
            _accessor = _mmf.CreateViewAccessor();
            
            _frameIndex = new Queue<FrameMetadata>(_maxFrames);
        }
        
        public void WriteFrame(byte[] frameData, TimeSpan timestamp)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PreRecordingBuffer));
            
            if (frameData.Length != _frameSize)
                throw new ArgumentException($"Frame data must be exactly {_frameSize} bytes", nameof(frameData));
            
            lock (_lock)
            {
                // Write frame data
                _accessor.WriteArray(_writePosition, frameData, 0, frameData.Length);
                
                // Update index
                var metadata = new FrameMetadata
                {
                    Position = _writePosition,
                    Timestamp = timestamp,
                    FrameNumber = _totalFramesWritten++
                };
                
                _frameIndex.Enqueue(metadata);
                
                // Remove oldest frame if buffer is full
                if (_frameIndex.Count > _maxFrames)
                {
                    _frameIndex.Dequeue();
                }
                
                // Circular write
                _writePosition = (_writePosition + _frameSize) % _bufferSize;
            }
        }
        
        public async Task DumpToFile(string outputPath)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PreRecordingBuffer));
            
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    // Write all frames from oldest to newest
                    using var output = File.OpenWrite(outputPath);
                    
                    foreach (var frame in _frameIndex.OrderBy(f => f.FrameNumber))
                    {
                        var buffer = new byte[_frameSize];
                        _accessor.ReadArray(frame.Position, buffer, 0, _frameSize);
                        output.Write(buffer, 0, buffer.Length);
                    }
                }
            });
        }
        
        public async Task<string> DumpToFFmpegPipe(string outputPath, string pixelFormat, int width, int height)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PreRecordingBuffer));
            
            // Create a named pipe for FFmpeg input
            var pipeName = $"prerecord_{Guid.NewGuid():N}";
            
            await Task.Run(async () =>
            {
                // Start FFmpeg process to encode the raw frames
                var ffmpegArgs = $"-f rawvideo -pix_fmt {pixelFormat} -s {width}x{height} " +
                               $"-r {_fps} -i pipe: -c:v ffv1 -level 3 \"{outputPath}\"";
                
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = ffmpegArgs,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                
                // Write frames to FFmpeg stdin
                lock (_lock)
                {
                    foreach (var frame in _frameIndex.OrderBy(f => f.FrameNumber))
                    {
                        var buffer = new byte[_frameSize];
                        _accessor.ReadArray(frame.Position, buffer, 0, _frameSize);
                        process.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
                    }
                }
                
                process.StandardInput.Close();
                await process.WaitForExitAsync();
                
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new InvalidOperationException($"FFmpeg failed: {error}");
                }
            });
            
            return outputPath;
        }
        
        public PreRecordStats GetStats()
        {
            lock (_lock)
            {
                return new PreRecordStats
                {
                    Enabled = true,
                    BufferSeconds = _maxFrames / _fps,
                    CurrentSeconds = _frameIndex.Count / _fps,
                    MemoryUsageBytes = _bufferSize,
                    FrameCount = _frameIndex.Count
                };
            }
        }
        
        public int GetFrameCount()
        {
            lock (_lock)
            {
                return _frameIndex.Count;
            }
        }
        
        public TimeSpan GetOldestTimestamp()
        {
            lock (_lock)
            {
                return _frameIndex.Count > 0 
                    ? _frameIndex.Peek().Timestamp 
                    : TimeSpan.Zero;
            }
        }
        
        public TimeSpan GetNewestTimestamp()
        {
            lock (_lock)
            {
                return _frameIndex.Count > 0 
                    ? _frameIndex.Last().Timestamp 
                    : TimeSpan.Zero;
            }
        }
        
        public void Clear()
        {
            lock (_lock)
            {
                _frameIndex.Clear();
                _writePosition = 0;
                _totalFramesWritten = 0;
            }
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
            
            lock (_lock)
            {
                _disposed = true;
                _accessor?.Dispose();
                _mmf?.Dispose();
            }
        }
        
        private class FrameMetadata
        {
            public long Position { get; set; }
            public TimeSpan Timestamp { get; set; }
            public long FrameNumber { get; set; }
        }
    }
}