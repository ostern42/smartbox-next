using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharedMemory;

namespace SmartBoxNext.CaptureService.Services
{
    /// <summary>
    /// Frame header structure for SharedMemory communication
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameHeader
    {
        public long Timestamp;          // Hardware timestamp (ticks)
        public int FrameNumber;         // Sequential frame number
        public int Width;               // Frame width
        public int Height;              // Frame height
        public int PixelFormat;         // Pixel format (YUY2 = 1, RGB = 2, etc.)
        public int DataSize;            // Size of frame data in bytes
        public bool IsKeyFrame;         // For video recording
        public byte Reserved1;          // Padding
        public byte Reserved2;          // Padding
        public byte Reserved3;          // Padding
    }

    /// <summary>
    /// Manages SharedMemory CircularBuffer for 60 FPS video streaming
    /// </summary>
    public class SharedMemoryManager : IDisposable
    {
        private readonly ILogger<SharedMemoryManager> _logger;
        
        // SharedMemory configuration
        private const string SHARED_MEMORY_NAME = "SmartBoxNextVideo";
        private const int NODE_COUNT = 10;                    // 10 frame buffer
        private const int NODE_BUFFER_SIZE = 4 * 1024 * 1024; // 4MB per frame (1920x1080 YUY2)
        
        private CircularBuffer? _circularBuffer;
        private bool _isInitialized = false;
        private long _frameCounter = 0;

        public bool IsHealthy => _isInitialized && _circularBuffer != null;

        public SharedMemoryManager(ILogger<SharedMemoryManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing SharedMemory CircularBuffer...");
                _logger.LogInformation("Configuration: {NodeCount} nodes × {NodeSize} bytes = {TotalSize} MB",
                    NODE_COUNT, NODE_BUFFER_SIZE, (NODE_COUNT * NODE_BUFFER_SIZE) / (1024 * 1024));

                // Create CircularBuffer - this will be the producer
                _circularBuffer = new CircularBuffer(
                    name: SHARED_MEMORY_NAME,
                    nodeCount: NODE_COUNT,
                    nodeBufferSize: NODE_BUFFER_SIZE);

                _isInitialized = true;
                _frameCounter = 0;

                _logger.LogInformation("SharedMemory CircularBuffer initialized successfully");
                _logger.LogInformation("SharedMemory name: {Name}", SHARED_MEMORY_NAME);
                _logger.LogInformation("Total buffer size: {Size} MB", 
                    (NODE_COUNT * NODE_BUFFER_SIZE) / (1024 * 1024));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SharedMemory CircularBuffer");
                _isInitialized = false;
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task ReinitializeAsync()
        {
            _logger.LogInformation("Reinitializing SharedMemory...");
            
            // Dispose existing buffer
            _circularBuffer?.Dispose();
            _circularBuffer = null;
            _isInitialized = false;

            // Wait a bit for cleanup
            await Task.Delay(100);

            // Reinitialize
            await InitializeAsync();
        }

        public bool WriteFrame(IntPtr frameData, int dataSize, int width, int height, int pixelFormat)
        {
            if (!_isInitialized || _circularBuffer == null)
            {
                _logger.LogWarning("SharedMemory not initialized, dropping frame");
                return false;
            }

            try
            {
                // Create frame header
                var header = new FrameHeader
                {
                    Timestamp = DateTime.UtcNow.Ticks,
                    FrameNumber = (int)Interlocked.Increment(ref _frameCounter),
                    Width = width,
                    Height = height,
                    PixelFormat = pixelFormat,
                    DataSize = dataSize,
                    IsKeyFrame = (_frameCounter % 30) == 0 // Every 30th frame is key frame
                };

                // Calculate total size needed
                var headerSize = Marshal.SizeOf<FrameHeader>();
                var totalSize = headerSize + dataSize;

                if (totalSize > NODE_BUFFER_SIZE)
                {
                    _logger.LogError("Frame too large: {FrameSize} bytes > {NodeSize} bytes", 
                        totalSize, NODE_BUFFER_SIZE);
                    return false;
                }

                // Get a node from the circular buffer
                using (var nodePtr = _circularBuffer.GetWritePointer())
                {
                    if (nodePtr == null)
                    {
                        // Buffer is full, drop this frame
                        _logger.LogDebug("CircularBuffer full, dropping frame {FrameNumber}", header.FrameNumber);
                        return false;
                    }

                    // Write header first
                    Marshal.StructureToPtr(header, nodePtr.Value, false);

                    // Write frame data after header
                    var dataPtr = IntPtr.Add(nodePtr.Value, headerSize);
                    
                    // Use high-performance memory copy
                    CopyMemory(dataPtr, frameData, (uint)dataSize);

                    // Commit the write (this makes it available to readers)
                    // The using statement automatically commits when disposed
                }

                // Log performance stats occasionally
                if (header.FrameNumber % 300 == 0) // Every 10 seconds at 30 FPS
                {
                    _logger.LogDebug("SharedMemory stats: Frame {FrameNumber}, {Width}x{Height}, {DataSize} bytes",
                        header.FrameNumber, width, height, dataSize);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing frame to SharedMemory");
                return false;
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping SharedMemory...");
            
            try
            {
                _isInitialized = false;
                _circularBuffer?.Dispose();
                _circularBuffer = null;
                
                _logger.LogInformation("SharedMemory stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping SharedMemory");
            }

            await Task.CompletedTask;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    }
}