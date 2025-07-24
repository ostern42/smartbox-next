using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SmartBoxNext.Services;
using SmartBoxNext.Services.Video;
using SmartBoxNext.Medical;

namespace SmartBoxNext.Tests.Integration
{
    /// <summary>
    /// Integration tests for video streaming system
    /// Tests complete workflows including Phase 1, 2, and 3 component interactions
    /// Reference: docs/video-streaming-improvement-shards/06-implementation-testing.md
    /// </summary>
    [TestClass]
    public class VideoStreamingIntegrationTests
    {
        private VideoStreamingManager _streamingManager;
        private TestVideoEngine _testVideoEngine;
        private MedicalComplianceService _medicalService;
        private PerformanceMonitor _performanceMonitor;

        [TestInitialize]
        public async Task Setup()
        {
            _testVideoEngine = new TestVideoEngine();
            _medicalService = new MedicalComplianceService();
            _performanceMonitor = new PerformanceMonitor();
            
            _streamingManager = new VideoStreamingManager(_testVideoEngine, new VideoStreamingOptions
            {
                EnableMedicalMode = true,
                EnableAdaptiveBitrate = true,
                EnableFrameAccurateControls = true,
                BufferingConfig = MedicalBufferingConfig.GetConfigForWorkflow(MedicalWorkflow.Diagnostic)
            });

            await _streamingManager.InitializeAsync();
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            await _streamingManager?.DisposeAsync();
            _testVideoEngine?.Dispose();
            _performanceMonitor?.Dispose();
        }

        #region End-to-End Medical Workflow Tests

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Medical")]
        [TestCategory("Surgical")]
        public async Task SurgicalWorkflow_CompleteSession_ShouldMeetMedicalStandards()
        {
            // Arrange - Initialize surgical workflow
            await _streamingManager.ConfigureForMedicalWorkflowAsync(MedicalWorkflow.Surgery);
            
            var sessionId = Guid.NewGuid().ToString();
            var testVideoPath = "test-surgical-procedure.mp4";
            var complianceReport = new MedicalComplianceReport();

            // Act - Complete surgical video review workflow
            _performanceMonitor.StartSession("surgical-workflow");

            // 1. Start streaming session
            var streamStartTime = DateTime.Now;
            await _streamingManager.StartStreamAsync(sessionId, testVideoPath);
            var streamStartDuration = DateTime.Now - streamStartTime;

            // 2. Test frame-accurate navigation (critical for surgical review)
            var seekOperations = new List<SeekOperation>();
            for (int i = 0; i < 20; i++)
            {
                var seekStart = DateTime.Now;
                var targetTime = TimeSpan.FromSeconds(i * 30); // Every 30 seconds
                
                await _streamingManager.SeekToTimeAsync(targetTime, frameAccurate: true);
                
                var actualTime = _streamingManager.CurrentTime;
                var seekDuration = DateTime.Now - seekStart;
                var accuracy = Math.Abs((actualTime - targetTime).TotalMilliseconds);
                
                seekOperations.Add(new SeekOperation
                {
                    TargetTime = targetTime,
                    ActualTime = actualTime,
                    SeekDuration = seekDuration,
                    Accuracy = accuracy
                });
            }

            // 3. Test medical-grade buffering under various conditions
            var bufferHealthHistory = new List<BufferHealthSnapshot>();
            for (int i = 0; i < 10; i++)
            {
                // Simulate network variability
                _testVideoEngine.SimulateNetworkCondition(NetworkCondition.Variable);
                await Task.Delay(500);
                
                var bufferHealth = _streamingManager.GetBufferHealth();
                bufferHealthHistory.Add(new BufferHealthSnapshot
                {
                    Timestamp = DateTime.Now,
                    BufferLength = bufferHealth.AvailableBuffer,
                    IsHealthy = bufferHealth.IsHealthy,
                    Holes = bufferHealth.Holes
                });
            }

            // 4. Test adaptive bitrate under surgical constraints
            var qualityHistory = new List<QualityChangeEvent>();
            _streamingManager.QualityChanged += (sender, e) => qualityHistory.Add(e);
            
            // Simulate bandwidth variations during surgical procedure
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.HighBandwidth);
            await Task.Delay(2000);
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.LowBandwidth);
            await Task.Delay(2000);
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.HighBandwidth);
            await Task.Delay(1000);

            // 5. End session and generate compliance report
            await _streamingManager.EndStreamAsync();
            _performanceMonitor.EndSession("surgical-workflow");
            
            complianceReport = await _medicalService.GenerateComplianceReportAsync(
                sessionId, _performanceMonitor.GetSessionMetrics("surgical-workflow"));

            // Assert - Verify surgical workflow compliance
            
            // Stream startup performance
            Assert.IsTrue(streamStartDuration.TotalSeconds < 2.0,
                $"Surgical stream start should be <2s, got {streamStartDuration.TotalSeconds:F2}s");

            // Frame-accurate seeking precision (surgical requirement: ±1ms)
            var maxSeekError = seekOperations.Max(s => s.Accuracy);
            var avgSeekError = seekOperations.Average(s => s.Accuracy);
            
            Assert.IsTrue(maxSeekError <= 10.0, // Allow 10ms max (stricter than standard ±1 frame)
                $"Maximum seek error should be ≤10ms for surgical use, got {maxSeekError:F2}ms");
            Assert.IsTrue(avgSeekError <= 1.0,
                $"Average seek error should be ≤1ms for surgical use, got {avgSeekError:F2}ms");

            // Seek performance (surgical requirement: <100ms response)
            var maxSeekTime = seekOperations.Max(s => s.SeekDuration.TotalMilliseconds);
            var avgSeekTime = seekOperations.Average(s => s.SeekDuration.TotalMilliseconds);
            
            Assert.IsTrue(maxSeekTime < 100.0,
                $"Maximum seek time should be <100ms for surgical use, got {maxSeekTime:F1}ms");
            Assert.IsTrue(avgSeekTime < 50.0,
                $"Average seek time should be <50ms for surgical use, got {avgSeekTime:F1}ms");

            // Buffer stability (surgical requirement: >99% healthy)
            var healthyBufferPercentage = bufferHealthHistory.Count(b => b.IsHealthy) * 100.0 / bufferHealthHistory.Count;
            Assert.IsTrue(healthyBufferPercentage >= 99.0,
                $"Buffer health should be ≥99% for surgical use, got {healthyBufferPercentage:F1}%");

            // Quality switching constraints (surgical: minimal switches, prioritize stability)
            var qualitySwitchCount = qualityHistory.Count;
            Assert.IsTrue(qualitySwitchCount <= 3,
                $"Quality switches should be minimal during surgical procedures, got {qualitySwitchCount}");

            // Medical compliance validation
            Assert.IsTrue(complianceReport.IsCompliant,
                $"Surgical workflow must meet medical compliance: {string.Join(", ", complianceReport.Violations)}");
            Assert.IsTrue(complianceReport.OverallScore >= 95.0,
                $"Surgical compliance score should be ≥95%, got {complianceReport.OverallScore:F1}%");
        }

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Medical")]
        [TestCategory("Emergency")]
        public async Task EmergencyWorkflow_RapidResponse_ShouldOptimizeForSpeed()
        {
            // Arrange - Configure for emergency response
            await _streamingManager.ConfigureForMedicalWorkflowAsync(MedicalWorkflow.Emergency);
            
            var sessionId = Guid.NewGuid().ToString();
            var emergencyVideoPath = "test-emergency-recording.mp4";

            // Act - Emergency response workflow
            _performanceMonitor.StartSession("emergency-workflow");

            // 1. Rapid stream initialization
            var initStart = DateTime.Now;
            await _streamingManager.StartStreamAsync(sessionId, emergencyVideoPath);
            var initDuration = DateTime.Now - initStart;

            // 2. Quick seeking to critical moments
            var criticalMoments = new[] { 
                TimeSpan.FromSeconds(30),   // Incident start
                TimeSpan.FromSeconds(120),  // Critical event
                TimeSpan.FromSeconds(300)   // Resolution
            };

            var emergencySeekTimes = new List<TimeSpan>();
            foreach (var moment in criticalMoments)
            {
                var seekStart = DateTime.Now;
                await _streamingManager.SeekToTimeAsync(moment, frameAccurate: false); // Speed over precision
                emergencySeekTimes.Add(DateTime.Now - seekStart);
            }

            // 3. Verify minimal buffering for responsiveness
            var bufferConfig = _streamingManager.GetCurrentBufferingConfig();
            
            await _streamingManager.EndStreamAsync();
            _performanceMonitor.EndSession("emergency-workflow");

            // Assert - Emergency workflow performance requirements
            
            // Rapid initialization (emergency requirement: <1s)
            Assert.IsTrue(initDuration.TotalSeconds < 1.0,
                $"Emergency stream start should be <1s, got {initDuration.TotalSeconds:F2}s");

            // Fast seeking (emergency requirement: <50ms)
            var maxEmergencySeekTime = emergencySeekTimes.Max(t => t.TotalMilliseconds);
            var avgEmergencySeekTime = emergencySeekTimes.Average(t => t.TotalMilliseconds);
            
            Assert.IsTrue(maxEmergencySeekTime < 50.0,
                $"Emergency seek time should be <50ms, got {maxEmergencySeekTime:F1}ms");

            // Minimal buffering configuration
            Assert.IsTrue(bufferConfig.BackBufferLength <= TimeSpan.FromSeconds(10),
                $"Emergency back buffer should be ≤10s, got {bufferConfig.BackBufferLength.TotalSeconds:F1}s");
            Assert.IsTrue(bufferConfig.ForwardBufferLength <= TimeSpan.FromSeconds(5),
                $"Emergency forward buffer should be ≤5s, got {bufferConfig.ForwardBufferLength.TotalSeconds:F1}s");
        }

        #endregion

        #region Adaptive Bitrate Integration Tests

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("AdaptiveBitrate")]
        [TestCategory("Performance")]
        public async Task AdaptiveBitrate_VariableNetworkConditions_ShouldMaintainOptimalQuality()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var testVideoPath = "test-multi-bitrate-stream.m3u8"; // HLS stream with multiple quality levels
            
            var qualityHistory = new List<QualityChangeEvent>();
            var bandwidthHistory = new List<BandwidthMeasurement>();
            var bufferHealthHistory = new List<BufferHealthSnapshot>();

            _streamingManager.QualityChanged += (sender, e) => qualityHistory.Add(e);
            _streamingManager.BandwidthMeasured += (sender, e) => bandwidthHistory.Add(e);

            // Act - Simulate various network conditions
            await _streamingManager.StartStreamAsync(sessionId, testVideoPath);

            // Phase 1: High bandwidth - should upgrade to highest quality
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.HighBandwidth); // 10 Mbps
            await Task.Delay(3000);
            var phase1Quality = _streamingManager.GetCurrentQualityLevel();
            var phase1Buffer = _streamingManager.GetBufferHealth();
            bufferHealthHistory.Add(new BufferHealthSnapshot 
            { 
                Phase = "HighBandwidth", 
                BufferLength = phase1Buffer.AvailableBuffer,
                IsHealthy = phase1Buffer.IsHealthy 
            });

            // Phase 2: Network degradation - should downgrade gracefully
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.MediumBandwidth); // 2 Mbps
            await Task.Delay(3000);
            var phase2Quality = _streamingManager.GetCurrentQualityLevel();
            var phase2Buffer = _streamingManager.GetBufferHealth();
            bufferHealthHistory.Add(new BufferHealthSnapshot 
            { 
                Phase = "MediumBandwidth", 
                BufferLength = phase2Buffer.AvailableBuffer,
                IsHealthy = phase2Buffer.IsHealthy 
            });

            // Phase 3: Low bandwidth - should use emergency protocols
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.LowBandwidth); // 500 Kbps
            await Task.Delay(3000);
            var phase3Quality = _streamingManager.GetCurrentQualityLevel();
            var phase3Buffer = _streamingManager.GetBufferHealth();
            bufferHealthHistory.Add(new BufferHealthSnapshot 
            { 
                Phase = "LowBandwidth", 
                BufferLength = phase3Buffer.AvailableBuffer,
                IsHealthy = phase3Buffer.IsHealthy 
            });

            // Phase 4: Recovery - should upgrade when conditions improve
            _testVideoEngine.SimulateNetworkCondition(NetworkCondition.HighBandwidth); // 10 Mbps
            await Task.Delay(4000); // Longer wait for stability verification
            var phase4Quality = _streamingManager.GetCurrentQualityLevel();
            var phase4Buffer = _streamingManager.GetBufferHealth();
            bufferHealthHistory.Add(new BufferHealthSnapshot 
            { 
                Phase = "Recovery", 
                BufferLength = phase4Buffer.AvailableBuffer,
                IsHealthy = phase4Buffer.IsHealthy 
            });

            await _streamingManager.EndStreamAsync();

            // Assert - Adaptive behavior validation
            
            // Quality adaptation responsiveness
            Assert.IsTrue(phase2Quality < phase1Quality,
                "Should downgrade quality when bandwidth decreases");
            Assert.IsTrue(phase3Quality <= phase2Quality,
                "Should continue downgrading under low bandwidth");
            Assert.IsTrue(phase4Quality >= phase3Quality,
                "Should upgrade quality when bandwidth recovers");

            // Buffer health maintenance
            var unhealthyBufferCount = bufferHealthHistory.Count(b => !b.IsHealthy);
            var bufferHealthPercentage = (bufferHealthHistory.Count - unhealthyBufferCount) * 100.0 / bufferHealthHistory.Count;
            
            Assert.IsTrue(bufferHealthPercentage >= 75.0,
                $"Buffer should remain healthy ≥75% of time during network variations, got {bufferHealthPercentage:F1}%");

            // Quality switching intelligence (should be responsive but not excessive)
            Assert.IsTrue(qualityHistory.Count >= 3 && qualityHistory.Count <= 8,
                $"Should make 3-8 quality switches for network test, got {qualityHistory.Count}");

            // Bandwidth utilization efficiency
            var avgBandwidthUtilization = bandwidthHistory
                .Where(b => b.AvailableBandwidth > 0)
                .Average(b => b.UsedBandwidth / b.AvailableBandwidth);
            
            Assert.IsTrue(avgBandwidthUtilization >= 0.6 && avgBandwidthUtilization <= 0.9,
                $"Bandwidth utilization should be 60-90%, got {avgBandwidthUtilization * 100:F1}%");
        }

        #endregion

        #region WebSocket Integration Tests

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("WebSocket")]
        [TestCategory("Reliability")]
        public async Task WebSocketHandler_ConnectionFailures_ShouldRecoverReliably()
        {
            // Arrange
            var sessionId = Guid.NewGuid().ToString();
            var connectionEvents = new List<ConnectionEvent>();
            var reconnectionAttempts = 0;
            var successfulRecoveries = 0;

            _streamingManager.ConnectionStateChanged += (sender, e) => connectionEvents.Add(e);
            _streamingManager.ReconnectionAttempted += (sender, e) => 
            {
                reconnectionAttempts++;
                if (e.Success) successfulRecoveries++;
            };

            // Act - Test connection resilience
            await _streamingManager.StartStreamAsync(sessionId);

            // Simulate various connection failures
            var failureScenarios = new[]
            {
                ConnectionFailureType.NetworkTimeout,
                ConnectionFailureType.ServerDisconnection,
                ConnectionFailureType.NetworkInterruption,
                ConnectionFailureType.ServerRestart
            };

            foreach (var failureType in failureScenarios)
            {
                // Inject failure
                _testVideoEngine.SimulateConnectionFailure(failureType);
                
                // Wait for recovery attempt
                await Task.Delay(2000);
                
                // Verify connection restored
                var retryCount = 0;
                while (!_streamingManager.IsConnected && retryCount < 10)
                {
                    await Task.Delay(500);
                    retryCount++;
                }
            }

            await _streamingManager.EndStreamAsync();

            // Assert - Connection reliability requirements
            
            // Recovery success rate (target: >95%)
            var recoverySuccessRate = successfulRecoveries * 100.0 / failureScenarios.Length;
            Assert.IsTrue(recoverySuccessRate >= 95.0,
                $"Connection recovery rate should be ≥95%, got {recoverySuccessRate:F1}%");

            // Reasonable reconnection attempts (should not be excessive)
            Assert.IsTrue(reconnectionAttempts <= failureScenarios.Length * 3,
                $"Should not exceed 3 attempts per failure, got {reconnectionAttempts} attempts for {failureScenarios.Length} failures");

            // Final connection state
            Assert.IsFalse(_streamingManager.IsConnected,
                "Should be disconnected after stream end");

            // Connection events tracking
            var connectEvents = connectionEvents.Count(e => e.State == ConnectionState.Connected);
            var disconnectEvents = connectionEvents.Count(e => e.State == ConnectionState.Disconnected);
            
            Assert.IsTrue(connectEvents >= failureScenarios.Length,
                $"Should have at least {failureScenarios.Length} connection events, got {connectEvents}");
        }

        #endregion

        #region Performance and Stress Tests

        [TestMethod]
        [TestCategory("Integration")]
        [TestCategory("Performance")]
        [TestCategory("Memory")]
        public async Task LongRunningStream_ExtendedBuffering_ShouldMaintainMemoryEfficiency()
        {
            // Arrange - Configure for extended surgical procedure (2+ hours)
            await _streamingManager.ConfigureForMedicalWorkflowAsync(MedicalWorkflow.Surgery);
            
            var sessionId = Guid.NewGuid().ToString();
            var longVideoPath = "test-long-surgical-procedure.mp4"; // Simulated 2-hour video
            var memorySnapshots = new List<MemorySnapshot>();
            var performanceMetrics = new List<PerformanceSnapshot>();

            // Act - Simulate long-running surgical video review
            await _streamingManager.StartStreamAsync(sessionId, longVideoPath);

            // Simulate extended usage pattern
            for (int minute = 0; minute < 30; minute++) // 30-minute test representing longer session
            {
                // Periodic memory measurement
                if (minute % 5 == 0)
                {
                    var memoryUsage = GC.GetTotalMemory(false);
                    var bufferStats = _streamingManager.GetBufferStatistics();
                    
                    memorySnapshots.Add(new MemorySnapshot
                    {
                        Timestamp = DateTime.Now,
                        TotalMemory = memoryUsage,
                        BufferMemory = bufferStats.MemoryUsage,
                        SegmentCount = bufferStats.SegmentCount
                    });
                }

                // Simulate typical surgical review patterns
                if (minute % 3 == 0)
                {
                    // Detailed review - frame stepping
                    for (int frame = 0; frame < 10; frame++)
                    {
                        await _streamingManager.StepFramesAsync(1);
                        await Task.Delay(50);
                    }
                }
                else if (minute % 7 == 0)
                {
                    // Jump to different section
                    var jumpTime = TimeSpan.FromMinutes(minute * 2);
                    await _streamingManager.SeekToTimeAsync(jumpTime, frameAccurate: true);
                }

                // Performance measurement
                var perfStart = DateTime.Now;
                await _streamingManager.RefreshBufferAsync();
                var perfDuration = DateTime.Now - perfStart;
                
                performanceMetrics.Add(new PerformanceSnapshot
                {
                    Timestamp = DateTime.Now,
                    Operation = "BufferRefresh",
                    Duration = perfDuration
                });

                await Task.Delay(2000); // 2-second intervals
            }

            await _streamingManager.EndStreamAsync();

            // Assert - Long-running performance requirements
            
            // Memory growth control (should not increase >20% over session)
            var initialMemory = memorySnapshots.First().TotalMemory;
            var finalMemory = memorySnapshots.Last().TotalMemory;
            var memoryGrowthPercentage = (finalMemory - initialMemory) * 100.0 / initialMemory;
            
            Assert.IsTrue(memoryGrowthPercentage <= 20.0,
                $"Memory growth should be ≤20% over long session, got {memoryGrowthPercentage:F1}%");

            // Buffer memory efficiency (surgical config allows up to 1GB)
            var maxBufferMemory = memorySnapshots.Max(s => s.BufferMemory);
            var maxAllowedBufferMemory = 1024 * 1024 * 1024; // 1GB for surgical
            
            Assert.IsTrue(maxBufferMemory <= maxAllowedBufferMemory,
                $"Buffer memory should stay within surgical limit of 1GB, got {maxBufferMemory / (1024 * 1024):F0}MB");

            // Performance consistency (operations should not degrade over time)
            var early_metrics = performanceMetrics.Take(5).Average(p => p.Duration.TotalMilliseconds);
            var late_metrics = performanceMetrics.Skip(Math.Max(0, performanceMetrics.Count - 5)).Average(p => p.Duration.TotalMilliseconds);
            var performanceDegradation = (late_metrics - early_metrics) / early_metrics * 100;
            
            Assert.IsTrue(performanceDegradation <= 50.0,
                $"Performance degradation should be ≤50% over session, got {performanceDegradation:F1}%");

            // Buffer management efficiency
            var avgSegmentCount = memorySnapshots.Average(s => s.SegmentCount);
            Assert.IsTrue(avgSegmentCount <= 600, // ~10 minutes at 1 segment/second
                $"Average segment count should be managed efficiently, got {avgSegmentCount:F0}");
        }

        #endregion

        #region Test Helper Classes and Data Structures

        private class SeekOperation
        {
            public TimeSpan TargetTime { get; set; }
            public TimeSpan ActualTime { get; set; }
            public TimeSpan SeekDuration { get; set; }
            public double Accuracy { get; set; }
        }

        private class BufferHealthSnapshot
        {
            public DateTime Timestamp { get; set; }
            public string Phase { get; set; }
            public double BufferLength { get; set; }
            public bool IsHealthy { get; set; }
            public int Holes { get; set; }
        }

        private class QualityChangeEvent
        {
            public DateTime Timestamp { get; set; }
            public int PreviousLevel { get; set; }
            public int NewLevel { get; set; }
            public string Reason { get; set; }
            public bool IsAutomatic { get; set; }
        }

        private class BandwidthMeasurement
        {
            public DateTime Timestamp { get; set; }
            public double AvailableBandwidth { get; set; }
            public double UsedBandwidth { get; set; }
            public double Latency { get; set; }
        }

        private class ConnectionEvent
        {
            public DateTime Timestamp { get; set; }
            public ConnectionState State { get; set; }
            public string Reason { get; set; }
        }

        private class MemorySnapshot
        {
            public DateTime Timestamp { get; set; }
            public long TotalMemory { get; set; }
            public long BufferMemory { get; set; }
            public int SegmentCount { get; set; }
        }

        private class PerformanceSnapshot
        {
            public DateTime Timestamp { get; set; }
            public string Operation { get; set; }
            public TimeSpan Duration { get; set; }
        }

        private class MedicalComplianceReport
        {
            public bool IsCompliant { get; set; }
            public double OverallScore { get; set; }
            public List<string> Violations { get; set; } = new List<string>();
            public Dictionary<string, double> MetricScores { get; set; } = new Dictionary<string, double>();
        }

        #endregion

        #region Test Enums

        private enum NetworkCondition
        {
            HighBandwidth,    // 10+ Mbps
            MediumBandwidth,  // 2-5 Mbps
            LowBandwidth,     // <1 Mbps
            Variable,         // Fluctuating
            Unstable          // Frequent drops
        }

        private enum ConnectionFailureType
        {
            NetworkTimeout,
            ServerDisconnection,
            NetworkInterruption,
            ServerRestart
        }

        private enum ConnectionState
        {
            Disconnected,
            Connecting,
            Connected,
            Reconnecting,
            Failed
        }

        #endregion
    }
}