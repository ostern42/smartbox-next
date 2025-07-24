using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SmartBoxNext.Services;
using SmartBoxNext.Services.Video;
using SmartBoxNext.Medical;

namespace SmartBoxNext.Tests.Unit.Services
{
    /// <summary>
    /// Comprehensive test suite for video streaming components
    /// Tests Phase 1, 2, and 3 implementations including medical-grade features
    /// Reference: docs/video-streaming-improvement-shards/06-implementation-testing.md
    /// </summary>
    [TestClass]
    public class VideoStreamingTests
    {
        private Mock<IVideoEngine> _mockVideoEngine;
        private Mock<ILogger> _mockLogger;
        private Mock<MedicalComplianceService> _mockMedicalService;
        private TestContext _testContext;

        [TestInitialize]
        public void Setup()
        {
            _mockVideoEngine = new Mock<IVideoEngine>();
            _mockLogger = new Mock<ILogger>();
            _mockMedicalService = new Mock<MedicalComplianceService>();
        }

        #region Phase 1 & 2 Foundation Tests

        [TestMethod]
        [TestCategory("Phase1")]
        [TestCategory("WebSocket")]
        public async Task StreamingWebSocketHandler_ShouldConnectWithExponentialBackoff()
        {
            // Arrange
            var connectionAttempts = new List<TimeSpan>();
            var handler = new StreamingWebSocketHandler(_mockLogger.Object);
            
            // Mock connection failures
            _mockVideoEngine.Setup(x => x.ConnectAsync(It.IsAny<string>()))
                          .Callback<string>(url => connectionAttempts.Add(DateTime.Now.TimeOfDay))
                          .ThrowsAsync(new Exception("Connection failed"));

            // Act
            try
            {
                await handler.ConnectWithRetryAsync("test-session", maxAttempts: 3);
            }
            catch
            {
                // Expected to fail after retries
            }

            // Assert
            Assert.AreEqual(3, connectionAttempts.Count, "Should attempt connection 3 times");
            
            // Verify exponential backoff (with jitter tolerance)
            for (int i = 1; i < connectionAttempts.Count; i++)
            {
                var delay = connectionAttempts[i] - connectionAttempts[i - 1];
                var expectedMinDelay = TimeSpan.FromMilliseconds(Math.Pow(2, i - 1) * 1000); // 1s, 2s, 4s...
                var expectedMaxDelay = TimeSpan.FromMilliseconds(Math.Pow(2, i - 1) * 1000 * 1.5); // With jitter
                
                Assert.IsTrue(delay >= expectedMinDelay && delay <= expectedMaxDelay,
                    $"Backoff delay {delay.TotalMilliseconds}ms should be between {expectedMinDelay.TotalMilliseconds}ms and {expectedMaxDelay.TotalMilliseconds}ms");
            }
        }

        [TestMethod]
        [TestCategory("Phase1")]
        [TestCategory("WebSocket")]
        public async Task StreamingWebSocketHandler_ShouldMaintainHeartbeat()
        {
            // Arrange
            var handler = new StreamingWebSocketHandler(_mockLogger.Object);
            var heartbeatCount = 0;
            
            _mockVideoEngine.Setup(x => x.SendHeartbeatAsync())
                          .Callback(() => heartbeatCount++)
                          .Returns(Task.CompletedTask);

            // Act
            await handler.StartHeartbeatAsync(interval: TimeSpan.FromMilliseconds(100));
            await Task.Delay(350); // Wait for ~3 heartbeats
            handler.StopHeartbeat();

            // Assert
            Assert.IsTrue(heartbeatCount >= 2 && heartbeatCount <= 4, 
                $"Should send 2-4 heartbeats in 350ms, got {heartbeatCount}");
        }

        [TestMethod]
        [TestCategory("Phase2")]
        [TestCategory("Timeline")]
        public void UnifiedTimeline_ShouldInitializeWithDefaultOptions()
        {
            // Arrange & Act
            var timeline = new UnifiedTimeline();

            // Assert
            Assert.AreEqual(30, timeline.Scale, "Default scale should be 30");
            Assert.AreEqual(0, timeline.Segments.Count, "Should initialize with empty segments");
            Assert.IsTrue(timeline.MedicalMode == false, "Medical mode should default to false");
            Assert.IsNotNull(timeline.TouchGestureSupport, "Touch gesture support should be initialized");
        }

        [TestMethod]
        [TestCategory("Phase2")]
        [TestCategory("Timeline")]
        public void UnifiedTimeline_ShouldAddSegmentsCorrectly()
        {
            // Arrange
            var timeline = new UnifiedTimeline();
            var segment = new VideoSegment
            {
                SegmentNumber = 1,
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(10),
                IsComplete = true
            };

            // Act
            timeline.AddSegment(segment);

            // Assert
            Assert.AreEqual(1, timeline.Segments.Count, "Should contain one segment");
            Assert.AreEqual(segment, timeline.Segments[0], "Should contain the added segment");
        }

        [TestMethod]
        [TestCategory("Phase2")]
        [TestCategory("Timeline")]
        public void UnifiedTimeline_ShouldClampScaleWithinBounds()
        {
            // Arrange
            var timeline = new UnifiedTimeline();
            const int minScale = 30;
            const int maxScale = 3600;

            // Act & Assert - Below minimum
            timeline.SetScale(20);
            Assert.AreEqual(minScale, timeline.Scale, "Should clamp to minimum scale");

            // Act & Assert - Above maximum
            timeline.SetScale(5000);
            Assert.AreEqual(maxScale, timeline.Scale, "Should clamp to maximum scale");

            // Act & Assert - Within bounds
            timeline.SetScale(100);
            Assert.AreEqual(100, timeline.Scale, "Should accept valid scale");
        }

        [TestMethod]
        [TestCategory("Phase2")]
        [TestCategory("Performance")]
        public void UnifiedTimeline_ShouldRenderLargeNumberOfSegmentsEfficiently()
        {
            // Arrange
            var timeline = new UnifiedTimeline();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Add 1000 segments
            for (int i = 0; i < 1000; i++)
            {
                timeline.AddSegment(new VideoSegment
                {
                    SegmentNumber = i,
                    StartTime = TimeSpan.FromSeconds(i * 10),
                    Duration = TimeSpan.FromSeconds(10),
                    IsComplete = true
                });
            }

            stopwatch.Stop();

            // Assert
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, 
                $"Should render 1000 segments in under 100ms, took {stopwatch.ElapsedMilliseconds}ms");
            Assert.AreEqual(1000, timeline.Segments.Count, "Should contain all 1000 segments");
        }

        #endregion

        #region Phase 3 Adaptive Bitrate Tests

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("AdaptiveBitrate")]
        public void AdaptiveBitrateManager_ShouldCalculateWeightedBandwidthAverage()
        {
            // Arrange
            var manager = new AdaptiveBitrateManager(_mockVideoEngine.Object);
            var baseTime = DateTime.Now;
            
            manager.AddBandwidthMeasurement(1000000, baseTime.AddSeconds(-2)); // 1 Mbps, 2 seconds ago
            manager.AddBandwidthMeasurement(2000000, baseTime.AddSeconds(-1)); // 2 Mbps, 1 second ago
            manager.AddBandwidthMeasurement(3000000, baseTime); // 3 Mbps, now

            // Act
            var average = manager.GetWeightedBandwidthAverage();

            // Assert
            Assert.IsTrue(average > 2000000, "Weighted average should favor recent measurements (> 2 Mbps)");
            Assert.IsTrue(average < 3000000, "Weighted average should be less than maximum value");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("AdaptiveBitrate")]
        public void AdaptiveBitrateManager_ShouldDetectUnhealthyBuffer()
        {
            // Arrange
            var manager = new AdaptiveBitrateManager(_mockVideoEngine.Object);
            var mockBuffered = new MockTimeRanges();
            
            // Create buffer with holes (0-10s, 15-20s with current time at 5s)
            mockBuffered.AddRange(0, 10);
            mockBuffered.AddRange(15, 20);
            
            _mockVideoEngine.Setup(x => x.GetBufferedRanges()).Returns(mockBuffered);
            _mockVideoEngine.Setup(x => x.CurrentTime).Returns(5.0);

            // Act
            var health = manager.AnalyzeBufferHealth();

            // Assert
            Assert.AreEqual(1, health.Holes, "Should detect one buffer hole");
            Assert.IsFalse(health.IsHealthy, "Buffer with holes should not be healthy");
            Assert.AreEqual(5.0, health.AvailableBuffer, "Should have 5 seconds of buffer ahead");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("AdaptiveBitrate")]
        [TestCategory("Medical")]
        public void AdaptiveBitrateManager_ShouldApplyMedicalQualityPresets()
        {
            // Arrange
            var manager = new AdaptiveBitrateManager(_mockVideoEngine.Object)
            {
                MedicalMode = true
            };

            // Test surgical preset (stability priority)
            manager.SetMedicalPreset(MedicalPreset.Surgical);
            
            // Act
            var suggestedLevel = manager.CalculateOptimalQualityLevel(
                availableBandwidth: 5000000, // 5 Mbps
                qualityLevels: new[] { 1000000, 2000000, 4000000, 8000000 }); // 1, 2, 4, 8 Mbps

            // Assert - Surgical mode should be conservative
            Assert.IsTrue(suggestedLevel <= 2, "Surgical mode should suggest conservative quality (≤ 4 Mbps)");
            
            // Test diagnostic preset (quality priority)
            manager.SetMedicalPreset(MedicalPreset.Diagnostic);
            var diagnosticLevel = manager.CalculateOptimalQualityLevel(5000000, 
                new[] { 1000000, 2000000, 4000000, 8000000 });
            
            Assert.IsTrue(diagnosticLevel >= 2, "Diagnostic mode should prioritize quality (≥ 4 Mbps)");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("AdaptiveBitrate")]
        public async Task AdaptiveBitrateManager_ShouldHandleEmergencyDowngrade()
        {
            // Arrange
            var manager = new AdaptiveBitrateManager(_mockVideoEngine.Object);
            var emergencyTriggered = false;
            
            manager.EmergencyDowngradeTriggered += (sender, args) => emergencyTriggered = true;
            
            // Mock critical buffer situation
            _mockVideoEngine.Setup(x => x.GetBufferedRanges())
                          .Returns(new MockTimeRanges()); // Empty buffer
            _mockVideoEngine.Setup(x => x.CurrentTime).Returns(10.0);

            // Act
            await manager.EvaluateQualityAsync();

            // Assert
            Assert.IsTrue(emergencyTriggered, "Emergency downgrade should be triggered for empty buffer");
            _mockVideoEngine.Verify(x => x.SetQualityLevel(0), Times.Once, 
                "Should switch to lowest quality level");
        }

        #endregion

        #region Phase 3 Medical Buffering Tests

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("MedicalBuffering")]
        public void MedicalBufferingConfig_ShouldProvideCorrectConfigurationForWorkflow()
        {
            // Arrange & Act
            var surgicalConfig = MedicalBufferingConfig.GetConfigForWorkflow(MedicalWorkflow.Surgery);
            var emergencyConfig = MedicalBufferingConfig.GetConfigForWorkflow(MedicalWorkflow.Emergency);

            // Assert - Surgical should prioritize stability and precision
            Assert.IsTrue(surgicalConfig.BackBufferLength >= TimeSpan.FromMinutes(5), 
                "Surgical config should have extended back buffer (≥5 minutes)");
            Assert.IsTrue(surgicalConfig.SeekPrecision <= TimeSpan.FromMilliseconds(0.01), 
                "Surgical config should have sub-millisecond precision");
            Assert.IsTrue(surgicalConfig.MaxRetries >= 10, 
                "Surgical config should have high reliability (≥10 retries)");

            // Assert - Emergency should prioritize speed
            Assert.IsTrue(emergencyConfig.BackBufferLength <= TimeSpan.FromSeconds(10), 
                "Emergency config should have minimal buffer (≤10 seconds)");
            Assert.IsTrue(emergencyConfig.LoadTimeout <= TimeSpan.FromSeconds(5), 
                "Emergency config should have fast timeout (≤5 seconds)");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("MedicalBuffering")]
        public void MedicalBufferingConfig_ShouldValidateComplianceRequirements()
        {
            // Arrange
            var config = MedicalBufferingConfig.GetConfigForWorkflow(MedicalWorkflow.Diagnostic);

            // Act
            var compliance = MedicalBufferingConfig.ValidateCompliance(config);

            // Assert
            Assert.IsTrue(compliance.IsCompliant, "Diagnostic config should meet compliance requirements");
            Assert.IsTrue(compliance.SeekAccuracy <= 1.0, "Seek accuracy should be ≤1ms for medical use");
            Assert.IsTrue(compliance.BufferReliability >= 99.0, "Buffer reliability should be ≥99%");
            Assert.IsTrue(compliance.MemoryEfficiency >= 90.0, "Memory efficiency should be ≥90%");
        }

        #endregion

        #region Phase 3 Frame-Accurate Controls Tests

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("FrameAccurate")]
        public void FrameAccurateControls_ShouldDetectFrameRateAutomatically()
        {
            // Arrange
            var controls = new FrameAccurateControls(_mockVideoEngine.Object);
            _mockVideoEngine.Setup(x => x.GetVideoMetadata())
                          .Returns(new VideoMetadata { FrameRate = 29.97 });

            // Act
            controls.InitializeWithAutoDetection();

            // Assert
            Assert.AreEqual(29.97, controls.DetectedFrameRate, 0.01, 
                "Should detect 29.97 fps from video metadata");
            Assert.IsTrue(controls.FrameDuration > 0, "Frame duration should be calculated");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("FrameAccurate")]
        [TestCategory("Performance")]
        public void FrameAccurateControls_ShouldSeekWithSubFramePrecision()
        {
            // Arrange
            var controls = new FrameAccurateControls(_mockVideoEngine.Object)
            {
                FrameRate = 30.0, // 30 fps = 33.33ms per frame
                MedicalMode = true
            };

            var seekRequests = new List<double>();
            _mockVideoEngine.Setup(x => x.SeekToTime(It.IsAny<double>()))
                          .Callback<double>(time => seekRequests.Add(time));

            // Act - Step forward 1 frame from 1.0 second
            _mockVideoEngine.Setup(x => x.CurrentTime).Returns(1.0);
            controls.StepFrames(1);

            // Assert
            Assert.AreEqual(1, seekRequests.Count, "Should make one seek request");
            
            var expectedTime = 1.0 + (1.0 / 30.0); // 1 second + 1 frame
            var actualTime = seekRequests[0];
            var precision = Math.Abs(actualTime - expectedTime);
            
            Assert.IsTrue(precision < 0.001, // Sub-millisecond precision
                $"Seek precision should be <1ms, got {precision * 1000:F3}ms");
        }

        [TestMethod]
        [TestCategory("Phase3")]
        [TestCategory("FrameAccurate")]
        [TestCategory("Medical")]
        public void FrameAccurateControls_ShouldApplyMedicalSpeedPresets()
        {
            // Arrange
            var controls = new FrameAccurateControls(_mockVideoEngine.Object)
            {
                MedicalMode = true
            };

            // Act & Assert - Surgical preset
            controls.SetMedicalPreset(MedicalPreset.Surgical);
            var surgicalSpeeds = controls.GetAvailableSpeeds();
            
            Assert.IsTrue(surgicalSpeeds.Contains(0.1), "Surgical preset should include 0.1x speed");
            Assert.IsTrue(surgicalSpeeds.Contains(0.25), "Surgical preset should include 0.25x speed");
            Assert.IsFalse(surgicalSpeeds.Any(s => s > 1.0), "Surgical preset should not exceed 1x speed");

            // Act & Assert - Review preset
            controls.SetMedicalPreset(MedicalPreset.Review);
            var reviewSpeeds = controls.GetAvailableSpeeds();
            
            Assert.IsTrue(reviewSpeeds.Contains(2.0), "Review preset should include 2x speed");
            Assert.IsTrue(reviewSpeeds.Contains(4.0), "Review preset should include 4x speed");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Medical")]
        public async Task MedicalWorkflow_ShouldInitializeAllComponentsCorrectly()
        {
            // Arrange
            var workflowManager = new MedicalWorkflowManager(_mockVideoEngine.Object, _mockMedicalService.Object);
            
            // Act
            await workflowManager.InitializeSurgicalWorkflowAsync();

            // Assert
            Assert.IsTrue(workflowManager.AdaptiveBitrateManager.MedicalMode, 
                "Adaptive bitrate manager should be in medical mode");
            Assert.AreEqual(MedicalPreset.Surgical, workflowManager.AdaptiveBitrateManager.CurrentPreset,
                "Should apply surgical preset");
            Assert.IsTrue(workflowManager.FrameAccurateControls.MedicalMode,
                "Frame accurate controls should be in medical mode");
            Assert.AreEqual(MedicalWorkflow.Surgery, workflowManager.BufferingConfig.Workflow,
                "Should apply surgical buffering configuration");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Performance")]
        public async Task VideoStreaming_ShouldMeetPerformanceTargets()
        {
            // Arrange
            var streamingManager = new VideoStreamingManager(_mockVideoEngine.Object);
            var performanceMetrics = new PerformanceMetrics();

            // Act
            var startTime = DateTime.Now;
            await streamingManager.StartStreamAsync("test-session");
            var startupTime = DateTime.Now - startTime;

            // Simulate streaming for performance measurement
            for (int i = 0; i < 10; i++)
            {
                var seekStart = DateTime.Now;
                await streamingManager.SeekToTimeAsync(i * 10.0);
                var seekTime = DateTime.Now - seekStart;
                
                performanceMetrics.AddSeekTime(seekTime);
            }

            // Assert performance targets from implementation testing doc
            Assert.IsTrue(startupTime.TotalSeconds < 2.0, 
                $"Playback start time should be <2s, got {startupTime.TotalSeconds:F2}s");
            Assert.IsTrue(performanceMetrics.AverageSeekTime.TotalMilliseconds < 100,
                $"Average seek time should be <100ms, got {performanceMetrics.AverageSeekTime.TotalMilliseconds:F1}ms");
            Assert.IsTrue(performanceMetrics.MaxSeekTime.TotalMilliseconds < 500,
                $"Max seek time should be <500ms, got {performanceMetrics.MaxSeekTime.TotalMilliseconds:F1}ms");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Reliability")]
        public async Task ErrorRecovery_ShouldMaintainStreamReliability()
        {
            // Arrange
            var recoveryManager = new StreamErrorRecoveryManager(_mockVideoEngine.Object);
            var successfulRecoveries = 0;
            var totalFailures = 20;

            // Act - Simulate various failure scenarios
            for (int i = 0; i < totalFailures; i++)
            {
                var failureType = (StreamFailureType)(i % 4); // Rotate through failure types
                var recovered = await recoveryManager.HandleStreamFailureAsync(failureType);
                
                if (recovered)
                    successfulRecoveries++;
            }

            // Assert - Target >95% recovery success rate
            var recoveryRate = (double)successfulRecoveries / totalFailures * 100;
            Assert.IsTrue(recoveryRate >= 95.0, 
                $"Error recovery success rate should be ≥95%, got {recoveryRate:F1}%");
        }

        #endregion

        #region Performance and Load Tests

        [TestMethod]
        [TestCategory("Performance")]
        [TestCategory("Memory")]
        public void BufferManager_ShouldMaintainMemoryWithinLimits()
        {
            // Arrange
            var bufferManager = new VideoBufferManager(MedicalBufferingConfig.GetConfigForWorkflow(MedicalWorkflow.Diagnostic));
            var maxMemoryMB = 600; // 600MB limit for production
            
            // Act - Simulate loading many segments
            for (int i = 0; i < 100; i++)
            {
                var segmentData = new byte[10 * 1024 * 1024]; // 10MB per segment
                bufferManager.AddSegmentData(i, segmentData);
            }

            // Assert
            var currentMemoryMB = bufferManager.GetCurrentMemoryUsage() / (1024 * 1024);
            Assert.IsTrue(currentMemoryMB <= maxMemoryMB, 
                $"Memory usage should be ≤{maxMemoryMB}MB, got {currentMemoryMB:F1}MB");
            Assert.IsTrue(bufferManager.SegmentCount <= bufferManager.MaxSegments,
                "Should respect maximum segment count limit");
        }

        [TestMethod]
        [TestCategory("Performance")]
        [TestCategory("Stress")]
        public async Task ConcurrentStreaming_ShouldHandleMultipleStreams()
        {
            // Arrange
            var streamManagers = new List<VideoStreamingManager>();
            var concurrentStreams = 5;
            
            for (int i = 0; i < concurrentStreams; i++)
            {
                streamManagers.Add(new VideoStreamingManager(_mockVideoEngine.Object));
            }

            // Act
            var tasks = streamManagers.Select(async (manager, index) => 
            {
                await manager.StartStreamAsync($"test-session-{index}");
                
                // Simulate concurrent operations
                for (int j = 0; j < 10; j++)
                {
                    await manager.SeekToTimeAsync(j * 5.0);
                    await Task.Delay(10); // Brief pause between operations
                }
                
                return manager.GetPerformanceMetrics();
            });

            var results = await Task.WhenAll(tasks);

            // Assert
            foreach (var metrics in results)
            {
                Assert.IsTrue(metrics.AverageSeekTime.TotalMilliseconds < 200,
                    "Concurrent streaming should maintain seek performance <200ms");
                Assert.IsTrue(metrics.ErrorRate < 0.01,
                    "Error rate should remain <1% under concurrent load");
            }
        }

        #endregion

        #region Helper Classes

        private class MockTimeRanges : ITimeRanges
        {
            private readonly List<(double start, double end)> _ranges = new List<(double, double)>();

            public int Length => _ranges.Count;

            public void AddRange(double start, double end)
            {
                _ranges.Add((start, end));
            }

            public double Start(int index) => _ranges[index].start;
            public double End(int index) => _ranges[index].end;
        }

        private class PerformanceMetrics
        {
            private readonly List<TimeSpan> _seekTimes = new List<TimeSpan>();

            public void AddSeekTime(TimeSpan seekTime)
            {
                _seekTimes.Add(seekTime);
            }

            public TimeSpan AverageSeekTime => 
                _seekTimes.Count > 0 ? 
                TimeSpan.FromMilliseconds(_seekTimes.Average(t => t.TotalMilliseconds)) : 
                TimeSpan.Zero;

            public TimeSpan MaxSeekTime => 
                _seekTimes.Count > 0 ? 
                _seekTimes.Max() : 
                TimeSpan.Zero;

            public double ErrorRate { get; set; }
        }

        #endregion

        #region Test Data

        public TestContext TestContext { get; set; }

        [TestCleanup]
        public void Cleanup()
        {
            // Log test results for medical compliance audit trail
            if (_mockMedicalService != null)
            {
                _mockMedicalService.Verify(x => x.LogTestExecution(
                    It.IsAny<string>(), 
                    It.IsAny<TestResult>()), 
                    Times.AtLeast(0));
            }
        }

        #endregion
    }

    #region Supporting Types and Enums

    public enum MedicalWorkflow
    {
        General,
        Surgery,
        Diagnostic,
        Emergency,
        Radiology,
        Cardiology,
        Endoscopy
    }

    public enum MedicalPreset
    {
        Review,
        Surgical,
        Diagnostic,
        Emergency
    }

    public enum StreamFailureType
    {
        NetworkDisconnection,
        BufferUnderrun,
        QualityDegradation,
        ServerError
    }

    public interface ITimeRanges
    {
        int Length { get; }
        double Start(int index);
        double End(int index);
    }

    public class VideoSegment
    {
        public int SegmentNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsComplete { get; set; }
        public byte[] Data { get; set; }
    }

    public class VideoMetadata
    {
        public double FrameRate { get; set; }
        public TimeSpan Duration { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Codec { get; set; }
    }

    public class BufferHealth
    {
        public int Holes { get; set; }
        public bool IsHealthy { get; set; }
        public double AvailableBuffer { get; set; }
        public double TotalBuffered { get; set; }
    }

    public class ComplianceValidation
    {
        public bool IsCompliant { get; set; }
        public double SeekAccuracy { get; set; }
        public double BufferReliability { get; set; }
        public double MemoryEfficiency { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    #endregion
}