using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DirectShowLib;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.CaptureService.Services
{
    /// <summary>
    /// Frame processor implementing ISampleGrabberCB for 60 FPS video processing
    /// </summary>
    public class FrameProcessor : ISampleGrabberCB, IDisposable
    {
        private readonly ILogger<FrameProcessor> _logger;
        private readonly SharedMemoryManager _sharedMemoryManager;
        private readonly YuanCaptureGraph _captureGraph;

        // Frame processing configuration
        private const int EXPECTED_WIDTH = 1920;
        private const int EXPECTED_HEIGHT = 1080;
        private const int YUY2_BYTES_PER_PIXEL = 2;
        private const int EXPECTED_FRAME_SIZE = EXPECTED_WIDTH * EXPECTED_HEIGHT * YUY2_BYTES_PER_PIXEL;
        
        // Pixel format constants
        private const int PIXEL_FORMAT_YUY2 = 1;
        private const int PIXEL_FORMAT_RGB = 2;
        
        // Performance tracking
        private long _totalFrames = 0;
        private long _droppedFrames = 0;
        private DateTime _lastLogTime = DateTime.MinValue;
        private readonly object _statsLock = new object();

        public FrameProcessor(
            ILogger<FrameProcessor> logger,
            SharedMemoryManager sharedMemoryManager,
            YuanCaptureGraph captureGraph)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sharedMemoryManager = sharedMemoryManager ?? throw new ArgumentNullException(nameof(sharedMemoryManager));
            _captureGraph = captureGraph ?? throw new ArgumentNullException(nameof(captureGraph));
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Frame Processor...");
            
            // Reset statistics
            lock (_statsLock)
            {
                _totalFrames = 0;
                _droppedFrames = 0;
                _lastLogTime = DateTime.UtcNow;
            }
            
            _logger.LogInformation("Frame Processor initialized for {Width}x{Height} YUY2 frames",
                EXPECTED_WIDTH, EXPECTED_HEIGHT);

            await Task.CompletedTask;
        }

        /// <summary>
        /// ISampleGrabberCB.SampleCB - not used, we use BufferCB for better performance
        /// </summary>
        public int SampleCB(double sampleTime, IMediaSample pSample)
        {
            // Not used - we implement BufferCB for better performance
            return 0;
        }

        /// <summary>
        /// ISampleGrabberCB.BufferCB - main frame processing callback
        /// Called for every frame at 60 FPS
        /// </summary>
        public int BufferCB(double sampleTime, IntPtr pBuffer, int bufferLen)
        {
            try
            {
                // Increment frame counter
                var frameNumber = Interlocked.Increment(ref _totalFrames);

                // Validate buffer size
                if (bufferLen <= 0 || pBuffer == IntPtr.Zero)
                {
                    _logger.LogWarning("Invalid buffer: len={BufferLen}, ptr={Pointer}", bufferLen, pBuffer);
                    RecordDroppedFrame();
                    return -1;
                }

                // Log frame info occasionally for debugging
                if (frameNumber % 1800 == 0) // Every 30 seconds at 60 FPS
                {
                    _logger.LogDebug("Processing frame {FrameNumber}: {BufferLen} bytes, time={SampleTime:F3}",
                        frameNumber, bufferLen, sampleTime);
                }

                // Determine frame dimensions from buffer size
                int width, height;
                if (bufferLen == EXPECTED_FRAME_SIZE)
                {
                    width = EXPECTED_WIDTH;
                    height = EXPECTED_HEIGHT;
                }
                else
                {
                    // Try to determine dimensions from buffer size
                    // For YUY2: bufferLen = width * height * 2
                    var pixelCount = bufferLen / YUY2_BYTES_PER_PIXEL;
                    
                    // Common resolutions
                    if (pixelCount == 1280 * 720)
                    {
                        width = 1280;
                        height = 720;
                    }
                    else if (pixelCount == 640 * 480)
                    {
                        width = 640;
                        height = 480;
                    }
                    else if (pixelCount == 720 * 576)
                    {
                        width = 720;
                        height = 576;
                    }
                    else if (pixelCount == 720 * 480)
                    {
                        width = 720;
                        height = 480;
                    }
                    else
                    {
                        // Assume 16:9 aspect ratio
                        height = (int)Math.Sqrt(pixelCount * 9.0 / 16.0);
                        width = pixelCount / height;
                        
                        _logger.LogDebug("Unknown frame size {BufferLen} bytes, estimated {Width}x{Height}",
                            bufferLen, width, height);
                    }
                }

                // Write frame to SharedMemory
                var success = _sharedMemoryManager.WriteFrame(
                    frameData: pBuffer,
                    dataSize: bufferLen,
                    width: width,
                    height: height,
                    pixelFormat: PIXEL_FORMAT_YUY2);

                if (!success)
                {
                    RecordDroppedFrame();
                }

                // Update capture graph statistics
                _captureGraph.UpdateStatistics(success);

                // Log performance statistics periodically
                LogPerformanceStats();

                return 0; // Success
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BufferCB frame processing");
                RecordDroppedFrame();
                return -1; // Error
            }
        }

        private void RecordDroppedFrame()
        {
            Interlocked.Increment(ref _droppedFrames);
        }

        private void LogPerformanceStats()
        {
            var now = DateTime.UtcNow;
            
            lock (_statsLock)
            {
                // Log stats every 10 seconds
                if ((now - _lastLogTime).TotalSeconds >= 10.0)
                {
                    var totalFrames = _totalFrames;
                    var droppedFrames = _droppedFrames;
                    var dropRate = totalFrames > 0 ? (double)droppedFrames / totalFrames * 100.0 : 0.0;
                    
                    _logger.LogInformation(
                        "Frame Stats: {TotalFrames} total, {DroppedFrames} dropped ({DropRate:F2}%), " +
                        "SharedMemory healthy: {IsHealthy}",
                        totalFrames, droppedFrames, dropRate, _sharedMemoryManager.IsHealthy);
                    
                    _lastLogTime = now;
                }
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping Frame Processor...");
            
            // Log final statistics
            lock (_statsLock)
            {
                var dropRate = _totalFrames > 0 ? (double)_droppedFrames / _totalFrames * 100.0 : 0.0;
                _logger.LogInformation(
                    "Final Frame Stats: {TotalFrames} total, {DroppedFrames} dropped ({DropRate:F2}%)",
                    _totalFrames, _droppedFrames, dropRate);
            }
            
            _logger.LogInformation("Frame Processor stopped");
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}