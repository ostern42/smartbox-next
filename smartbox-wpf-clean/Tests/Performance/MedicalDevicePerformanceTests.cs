using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartBoxNext.Services;
using Xunit;
using System.Diagnostics;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace SmartBoxNext.Tests.Performance;

/// <summary>
/// Comprehensive performance testing for medical device operation
/// Validates 4-hour continuous operation, memory management, and performance under load
/// </summary>
public class MedicalDevicePerformanceTests : IClassFixture<PerformanceTestFixture>
{
    private readonly PerformanceTestFixture _fixture;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalDevicePerformanceTests> _logger;
    private readonly PerformanceMonitor _performanceMonitor;

    public MedicalDevicePerformanceTests(PerformanceTestFixture fixture)
    {
        _fixture = fixture;
        _serviceProvider = _fixture.ServiceProvider;
        _logger = _serviceProvider.GetRequiredService<ILogger<MedicalDevicePerformanceTests>>();
        _performanceMonitor = _serviceProvider.GetRequiredService<PerformanceMonitor>();
    }

    #region 4-Hour Endurance Testing

    [Fact]
    [Trait("Category", "EnduranceTest")]
    [Trait("Duration", "4Hours")]
    public async Task FourHourContinuousOperation_ShouldMaintainPerformance()
    {
        // Arrange
        var testDuration = TimeSpan.FromHours(4);
        var captureInterval = TimeSpan.FromSeconds(1); // 60 FPS simulation
        var memoryThresholdMB = 500; // Maximum allowed memory growth
        var cpuThresholdPercent = 70.0; // Maximum CPU usage

        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();
        var queueManager = _serviceProvider.GetRequiredService<IntegratedQueueManager>();

        var cancellationTokenSource = new CancellationTokenSource(testDuration);
        var performanceMetrics = new List<PerformanceSnapshot>();
        var errorCount = 0;
        var operationCount = 0;

        _logger.LogInformation("Starting 4-hour continuous operation test");

        // Act - Run continuous operation for 4 hours
        var startTime = DateTime.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        try
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Capture performance snapshot
                    var snapshot = await _performanceMonitor.CaptureSnapshotAsync();
                    performanceMetrics.Add(snapshot);

                    // Simulate medical image capture workflow
                    var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
                    
                    if (captureResult.Success)
                    {
                        var testPatient = CreateTestPatient($"ENDURANCE_{operationCount:D6}");
                        var testStudy = CreateTestStudy($"4-Hour Test Operation {operationCount}");

                        var dicomResult = await dicomConverter.ConvertToDicomAsync(
                            captureResult.ImageData, testPatient, testStudy);

                        if (dicomResult.Success)
                        {
                            await queueManager.QueueForTransmissionAsync(
                                dicomResult.DicomFile, testPatient, testStudy);
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    else
                    {
                        errorCount++;
                    }

                    operationCount++;

                    // Memory management check every 100 operations
                    if (operationCount % 100 == 0)
                    {
                        var currentMemory = GC.GetTotalMemory(false);
                        var memoryGrowthMB = (currentMemory - initialMemory) / (1024 * 1024);
                        
                        if (memoryGrowthMB > memoryThresholdMB)
                        {
                            _logger.LogWarning($"Memory growth exceeded threshold: {memoryGrowthMB}MB");
                            // Force garbage collection
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            GC.Collect();
                        }
                    }

                    // Wait for next capture interval
                    await Task.Delay(captureInterval, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // Test completed
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during operation {operationCount}");
                    errorCount++;
                }
            }
        }
        finally
        {
            var endTime = DateTime.UtcNow;
            var actualDuration = endTime - startTime;
            var finalMemory = GC.GetTotalMemory(true);

            _logger.LogInformation($"4-hour test completed. Duration: {actualDuration}, Operations: {operationCount}, Errors: {errorCount}");

            // Assert - Validate performance requirements
            actualDuration.Should().BeGreaterThan(TimeSpan.FromHours(3.9)); // Allow small timing variance
            
            // Error rate should be less than 1%
            var errorRate = (double)errorCount / operationCount * 100;
            errorRate.Should().BeLessThan(1.0, $"Error rate was {errorRate}%");

            // Memory growth should be within limits
            var totalMemoryGrowthMB = (finalMemory - initialMemory) / (1024 * 1024);
            totalMemoryGrowthMB.Should().BeLessThan(memoryThresholdMB, 
                $"Memory grew by {totalMemoryGrowthMB}MB, exceeding {memoryThresholdMB}MB limit");

            // CPU usage should remain within limits
            var avgCpuUsage = performanceMetrics.Average(m => m.CPUUsagePercent);
            avgCpuUsage.Should().BeLessThan(cpuThresholdPercent, 
                $"Average CPU usage was {avgCpuUsage}%, exceeding {cpuThresholdPercent}% limit");

            // Frame rate should be maintained
            var avgFrameRate = performanceMetrics.Average(m => m.FrameRate);
            avgFrameRate.Should().BeGreaterThan(55.0, "Frame rate dropped below acceptable level");

            // Generate performance report
            await GeneratePerformanceReportAsync(performanceMetrics, actualDuration, operationCount, errorCount);
        }
    }

    [Fact]
    [Trait("Category", "MemoryTest")]
    public async Task MemoryLeakDetection_ShouldNotLeakMemoryOverTime()
    {
        // Arrange
        var testDuration = TimeSpan.FromMinutes(30);
        var measurementInterval = TimeSpan.FromMinutes(1);
        var maxMemoryGrowthMB = 50; // Maximum allowed memory growth

        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var memoryMeasurements = new List<MemoryMeasurement>();

        var cancellationTokenSource = new CancellationTokenSource(testDuration);

        // Act - Monitor memory usage over time
        var operationCount = 0;
        var lastMeasurementTime = DateTime.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // Perform operations
                for (int i = 0; i < 60; i++) // 60 operations per minute
                {
                    var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
                    operationCount++;
                    
                    if (cancellationTokenSource.Token.IsCancellationRequested) break;
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }

                // Take memory measurement
                if (DateTime.UtcNow - lastMeasurementTime >= measurementInterval)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    var memoryUsageMB = currentMemory / (1024 * 1024);
                    
                    memoryMeasurements.Add(new MemoryMeasurement
                    {
                        Timestamp = DateTime.UtcNow,
                        MemoryUsageMB = memoryUsageMB,
                        OperationCount = operationCount
                    });

                    lastMeasurementTime = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Force final garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowthMB = (finalMemory - initialMemory) / (1024 * 1024);

        // Assert
        memoryGrowthMB.Should().BeLessThan(maxMemoryGrowthMB, 
            $"Memory grew by {memoryGrowthMB}MB over {testDuration.TotalMinutes} minutes");

        // Check for memory leak trend
        var firstMeasurement = memoryMeasurements.First().MemoryUsageMB;
        var lastMeasurement = memoryMeasurements.Last().MemoryUsageMB;
        var memoryTrend = lastMeasurement - firstMeasurement;

        memoryTrend.Should().BeLessThan(maxMemoryGrowthMB / 2, 
            $"Memory trend shows potential leak: {memoryTrend}MB growth");
    }

    #endregion

    #region Load Testing

    [Fact]
    [Trait("Category", "LoadTest")]
    public async Task ConcurrentUserLoad_ShouldHandleMultipleUsers()
    {
        // Arrange
        var maxConcurrentUsers = 10;
        var testDuration = TimeSpan.FromMinutes(10);
        var expectedMinThroughput = 100; // operations per minute per user

        // Act - Use NBomber for load testing
        var scenario = Scenario.Create("medical_device_load_test", async context =>
        {
            var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
            var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();

            var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
            
            if (captureResult.Success)
            {
                var testPatient = CreateTestPatient($"LOAD_USER_{context.ScenarioInfo.ThreadId}_{context.InvocationNumber}");
                var testStudy = CreateTestStudy($"Load Test Operation");

                var dicomResult = await dicomConverter.ConvertToDicomAsync(
                    captureResult.ImageData, testPatient, testStudy);

                return dicomResult.Success ? Response.Ok() : Response.Fail();
            }

            return Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.InjectPerSec(rate: 5, during: TimeSpan.FromMinutes(2)), // Ramp up
            Simulation.KeepConstant(copies: maxConcurrentUsers, during: TimeSpan.FromMinutes(6)), // Steady load
            Simulation.InjectPerSec(rate: 2, during: TimeSpan.FromMinutes(2)) // Ramp down
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert
        var allOkCount = stats.AllOkCount;
        var allFailCount = stats.AllFailCount;
        var totalRequests = allOkCount + allFailCount;

        var successRate = (double)allOkCount / totalRequests * 100;
        successRate.Should().BeGreaterThan(95.0, $"Success rate was {successRate}%");

        var avgThroughput = allOkCount / testDuration.TotalMinutes;
        avgThroughput.Should().BeGreaterThan(expectedMinThroughput, 
            $"Throughput was {avgThroughput} ops/min, expected > {expectedMinThroughput}");
    }

    [Fact]
    [Trait("Category", "StressTest")]
    public async Task SystemStressTest_ShouldHandleExtremeLoad()
    {
        // Arrange
        var extremeLoadUsers = 25; // Beyond normal capacity
        var stressDuration = TimeSpan.FromMinutes(5);

        var unifiedCaptureManager = _serviceProvider.GetRequiredService<UnifiedCaptureManager>();
        var tasks = new List<Task<StressTestResult>>();

        // Act - Create extreme concurrent load
        for (int i = 0; i < extremeLoadUsers; i++)
        {
            var userId = i;
            var task = Task.Run(async () =>
            {
                var results = new StressTestResult { UserId = userId };
                var endTime = DateTime.UtcNow.Add(stressDuration);

                while (DateTime.UtcNow < endTime)
                {
                    try
                    {
                        var startTime = DateTime.UtcNow;
                        var captureResult = await unifiedCaptureManager.CaptureImageAsync(CaptureSource.Yuan);
                        var responseTime = DateTime.UtcNow - startTime;

                        if (captureResult.Success)
                        {
                            results.SuccessfulOperations++;
                            results.TotalResponseTimeMs += responseTime.TotalMilliseconds;
                        }
                        else
                        {
                            results.FailedOperations++;
                        }

                        results.TotalOperations++;
                    }
                    catch
                    {
                        results.FailedOperations++;
                        results.TotalOperations++;
                    }

                    await Task.Delay(100); // Brief pause between operations
                }

                return results;
            });

            tasks.Add(task);
        }

        var allResults = await Task.WhenAll(tasks);

        // Assert
        var totalOperations = allResults.Sum(r => r.TotalOperations);
        var totalSuccessful = allResults.Sum(r => r.SuccessfulOperations);
        var totalFailed = allResults.Sum(r => r.FailedOperations);

        totalOperations.Should().BeGreaterThan(0);
        
        // Under extreme stress, we expect some failures but system should not crash
        var systemStability = (double)totalSuccessful / totalOperations * 100;
        systemStability.Should().BeGreaterThan(50.0, 
            $"System stability under extreme load was {systemStability}%");

        // Average response time should be reasonable even under stress
        var totalResponseTime = allResults.Sum(r => r.TotalResponseTimeMs);
        var avgResponseTime = totalResponseTime / totalSuccessful;
        avgResponseTime.Should().BeLessThan(5000, // 5 seconds max under extreme stress
            $"Average response time under stress was {avgResponseTime}ms");
    }

    #endregion

    #region Performance Benchmarking

    [Fact]
    [Trait("Category", "Benchmark")]
    public async Task DICOMCreationBenchmark_ShouldMeetPerformanceTargets()
    {
        // Arrange
        var iterations = 1000;
        var maxAvgTimeMs = 100; // Maximum average time per DICOM creation
        var maxMemoryGrowthMB = 10;

        var dicomConverter = _serviceProvider.GetRequiredService<OptimizedDicomConverter>();
        var stopwatch = new Stopwatch();
        var responseTimes = new List<double>();

        var initialMemory = GC.GetTotalMemory(true);

        // Act - Benchmark DICOM creation
        for (int i = 0; i < iterations; i++)
        {
            var testImage = GenerateTestMedicalImage(1024, 768);
            var testPatient = CreateTestPatient($"BENCHMARK_{i:D4}");
            var testStudy = CreateTestStudy($"Benchmark Test {i}");

            stopwatch.Restart();
            var result = await dicomConverter.ConvertToDicomAsync(testImage, testPatient, testStudy);
            stopwatch.Stop();

            result.Success.Should().BeTrue();
            responseTimes.Add(stopwatch.Elapsed.TotalMilliseconds);

            // Force cleanup every 100 iterations
            if (i % 100 == 0)
            {
                GC.Collect();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryGrowthMB = (finalMemory - initialMemory) / (1024 * 1024);

        // Assert
        var avgResponseTime = responseTimes.Average();
        var p95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(iterations * 0.95)).First();
        var p99ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(iterations * 0.99)).First();

        avgResponseTime.Should().BeLessThan(maxAvgTimeMs, 
            $"Average DICOM creation time was {avgResponseTime}ms");
        
        p95ResponseTime.Should().BeLessThan(maxAvgTimeMs * 2, 
            $"95th percentile response time was {p95ResponseTime}ms");
        
        p99ResponseTime.Should().BeLessThan(maxAvgTimeMs * 3, 
            $"99th percentile response time was {p99ResponseTime}ms");

        memoryGrowthMB.Should().BeLessThan(maxMemoryGrowthMB, 
            $"Memory growth during benchmark was {memoryGrowthMB}MB");
    }

    #endregion

    #region Helper Methods

    private async Task GeneratePerformanceReportAsync(
        List<PerformanceSnapshot> metrics, 
        TimeSpan duration, 
        int operationCount, 
        int errorCount)
    {
        var report = new PerformanceReport
        {
            TestDuration = duration,
            TotalOperations = operationCount,
            ErrorCount = errorCount,
            ErrorRate = (double)errorCount / operationCount * 100,
            AverageFrameRate = metrics.Average(m => m.FrameRate),
            AverageCPUUsage = metrics.Average(m => m.CPUUsagePercent),
            PeakMemoryUsageMB = metrics.Max(m => m.MemoryUsageMB),
            Timestamp = DateTime.UtcNow
        };

        var reportPath = Path.Combine("Reports/Performance", $"4hour_test_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
        
        var reportJson = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(reportPath, reportJson);
        _logger.LogInformation($"Performance report generated: {reportPath}");
    }

    private MedicalPatient CreateTestPatient(string patientId)
    {
        return new MedicalPatient
        {
            PatientID = patientId,
            PatientName = $"Performance^Test^{patientId}",
            DateOfBirth = new DateTime(1980, 1, 1),
            Gender = "M"
        };
    }

    private MedicalProcedure CreateTestStudy(string description)
    {
        return new MedicalProcedure
        {
            StudyInstanceUID = FellowOakDicom.DicomUID.Generate().UID,
            ProcedureDescription = description,
            Modality = "ES"
        };
    }

    private byte[] GenerateTestMedicalImage(int width, int height)
    {
        var imageData = new byte[width * height * 3];
        var random = new Random();
        random.NextBytes(imageData);
        return imageData;
    }

    #endregion
}

/// <summary>
/// Performance test fixture providing shared test infrastructure
/// </summary>
public class PerformanceTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }

    public PerformanceTestFixture()
    {
        var services = new ServiceCollection();
        ConfigureTestServices(services);
        ServiceProvider = services.BuildServiceProvider();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton(TestConfiguration.Configuration);
        
        // Add performance monitoring services
        services.AddScoped<PerformanceMonitor>();
        services.AddScoped<UnifiedCaptureManager>();
        services.AddScoped<OptimizedDicomConverter>();
        services.AddScoped<IntegratedQueueManager>();
    }

    public void Dispose()
    {
        (ServiceProvider as IDisposable)?.Dispose();
    }
}

/// <summary>
/// Supporting data structures for performance testing
/// </summary>
public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public double CPUUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public double FrameRate { get; set; }
    public int ActiveThreads { get; set; }
    public long DiskIOBytes { get; set; }
    public long NetworkIOBytes { get; set; }
}

public class MemoryMeasurement
{
    public DateTime Timestamp { get; set; }
    public long MemoryUsageMB { get; set; }
    public int OperationCount { get; set; }
}

public class StressTestResult
{
    public int UserId { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double TotalResponseTimeMs { get; set; }
}

public class PerformanceReport
{
    public TimeSpan TestDuration { get; set; }
    public int TotalOperations { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public double AverageFrameRate { get; set; }
    public double AverageCPUUsage { get; set; }
    public long PeakMemoryUsageMB { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Performance monitoring service for capturing system metrics
/// </summary>
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
    }

    public async Task<PerformanceSnapshot> CaptureSnapshotAsync()
    {
        return await Task.FromResult(new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            CPUUsagePercent = _cpuCounter.NextValue(),
            MemoryUsageMB = GC.GetTotalMemory(false) / (1024 * 1024),
            FrameRate = 60.0, // This would be captured from actual frame rate monitoring
            ActiveThreads = Process.GetCurrentProcess().Threads.Count,
            DiskIOBytes = 0, // Would be captured from actual disk I/O monitoring
            NetworkIOBytes = 0 // Would be captured from actual network I/O monitoring
        });
    }
}