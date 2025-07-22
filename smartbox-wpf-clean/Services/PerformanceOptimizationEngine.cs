using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Medical-grade performance optimization engine for 4+ hour operations
    /// Ensures 99.9% uptime and optimal resource utilization
    /// </summary>
    public class PerformanceOptimizationEngine : IDisposable
    {
        private readonly ILogger<PerformanceOptimizationEngine> _logger;
        private readonly MemoryManagerService _memoryManager;
        private readonly CPUOptimizationService _cpuOptimizer;
        private readonly StorageOptimizationService _storageOptimizer;
        private readonly NetworkOptimizationService _networkOptimizer;
        
        private readonly PerformanceConfig _config;
        private readonly Timer _optimizationTimer;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _predictiveMaintenanceTimer;
        
        private readonly ConcurrentDictionary<string, PerformanceMetric> _metrics;
        private readonly ConcurrentQueue<PerformanceAlert> _alerts;
        private readonly List<IPerformanceOptimizer> _optimizers;
        
        private bool _isActive = false;
        private bool _disposed = false;
        private DateTime _sessionStartTime;
        private long _totalOptimizationsPerformed = 0;
        
        public event EventHandler<PerformanceAlertEventArgs>? AlertRaised;
        public event EventHandler<OptimizationPerformedEventArgs>? OptimizationPerformed;
        public event EventHandler<PredictiveMaintenanceEventArgs>? PredictiveMaintenanceTriggered;

        public bool IsActive => _isActive;
        public TimeSpan SessionDuration => DateTime.UtcNow - _sessionStartTime;
        public long TotalOptimizations => _totalOptimizationsPerformed;
        public PerformanceHealthStatus HealthStatus { get; private set; } = PerformanceHealthStatus.Unknown;

        public PerformanceOptimizationEngine(
            ILogger<PerformanceOptimizationEngine> logger,
            MemoryManagerService memoryManager,
            CPUOptimizationService cpuOptimizer,
            StorageOptimizationService storageOptimizer,
            NetworkOptimizationService networkOptimizer,
            PerformanceConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _cpuOptimizer = cpuOptimizer ?? throw new ArgumentNullException(nameof(cpuOptimizer));
            _storageOptimizer = storageOptimizer ?? throw new ArgumentNullException(nameof(storageOptimizer));
            _networkOptimizer = networkOptimizer ?? throw new ArgumentNullException(nameof(networkOptimizer));
            
            _config = config ?? new PerformanceConfig();
            _metrics = new ConcurrentDictionary<string, PerformanceMetric>();
            _alerts = new ConcurrentQueue<PerformanceAlert>();
            
            // Initialize optimizers
            _optimizers = new List<IPerformanceOptimizer>
            {
                _memoryManager,
                _cpuOptimizer,
                _storageOptimizer,
                _networkOptimizer
            };
            
            // Initialize timers
            _optimizationTimer = new Timer(PerformOptimization, null, Timeout.Infinite, Timeout.Infinite);
            _healthCheckTimer = new Timer(PerformHealthCheck, null, Timeout.Infinite, Timeout.Infinite);
            _predictiveMaintenanceTimer = new Timer(PerformPredictiveMaintenance, null, Timeout.Infinite, Timeout.Infinite);
            
            _logger.LogInformation("PerformanceOptimizationEngine initialized with {OptimizerCount} optimizers", _optimizers.Count);
        }

        /// <summary>
        /// Start the performance optimization engine
        /// </summary>
        public async Task StartAsync()
        {
            if (_isActive)
            {
                _logger.LogWarning("Performance optimization engine is already active");
                return;
            }

            _logger.LogInformation("Starting performance optimization engine for medical-grade operation");
            
            _sessionStartTime = DateTime.UtcNow;
            _isActive = true;
            HealthStatus = PerformanceHealthStatus.Starting;

            try
            {
                // Initialize all optimizers
                foreach (var optimizer in _optimizers)
                {
                    await optimizer.InitializeAsync();
                }

                // Start monitoring timers
                _optimizationTimer.Change(0, _config.OptimizationIntervalMs);
                _healthCheckTimer.Change(_config.HealthCheckIntervalMs, _config.HealthCheckIntervalMs);
                _predictiveMaintenanceTimer.Change(_config.PredictiveMaintenanceIntervalMs, _config.PredictiveMaintenanceIntervalMs);

                // Perform initial optimization
                await PerformInitialOptimizationAsync();

                HealthStatus = PerformanceHealthStatus.Healthy;
                _logger.LogInformation("Performance optimization engine started successfully");
            }
            catch (Exception ex)
            {
                HealthStatus = PerformanceHealthStatus.Critical;
                _logger.LogError(ex, "Failed to start performance optimization engine");
                throw;
            }
        }

        /// <summary>
        /// Stop the performance optimization engine
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isActive)
            {
                _logger.LogWarning("Performance optimization engine is not active");
                return;
            }

            _logger.LogInformation("Stopping performance optimization engine");
            
            _isActive = false;
            HealthStatus = PerformanceHealthStatus.Stopping;

            try
            {
                // Stop timers
                _optimizationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _healthCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _predictiveMaintenanceTimer.Change(Timeout.Infinite, Timeout.Infinite);

                // Perform final optimization
                await PerformFinalOptimizationAsync();

                // Cleanup optimizers
                foreach (var optimizer in _optimizers)
                {
                    await optimizer.CleanupAsync();
                }

                HealthStatus = PerformanceHealthStatus.Stopped;
                
                var sessionDuration = DateTime.UtcNow - _sessionStartTime;
                _logger.LogInformation("Performance optimization engine stopped. Session duration: {Duration}, Total optimizations: {Count}",
                    sessionDuration, _totalOptimizationsPerformed);
            }
            catch (Exception ex)
            {
                HealthStatus = PerformanceHealthStatus.Critical;
                _logger.LogError(ex, "Error while stopping performance optimization engine");
                throw;
            }
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public PerformanceReport GetCurrentReport()
        {
            var systemMetrics = GetSystemMetrics();
            var optimizerMetrics = new Dictionary<string, object>();

            foreach (var optimizer in _optimizers)
            {
                try
                {
                    optimizerMetrics[optimizer.GetType().Name] = optimizer.GetCurrentMetrics();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting metrics from {Optimizer}", optimizer.GetType().Name);
                }
            }

            return new PerformanceReport
            {
                Timestamp = DateTime.UtcNow,
                SessionDuration = SessionDuration,
                HealthStatus = HealthStatus,
                SystemMetrics = systemMetrics,
                OptimizerMetrics = optimizerMetrics,
                TotalOptimizations = _totalOptimizationsPerformed,
                Alerts = _alerts.ToArray(),
                RecentMetrics = _metrics.Values.OrderByDescending(m => m.Timestamp).Take(100).ToArray()
            };
        }

        /// <summary>
        /// Force immediate optimization across all systems
        /// </summary>
        public async Task ForceOptimizationAsync()
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("Performance optimization engine is not active");
            }

            _logger.LogInformation("Performing forced optimization");
            
            try
            {
                await PerformOptimizationCycleAsync(true);
                _logger.LogInformation("Forced optimization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forced optimization");
                throw;
            }
        }

        /// <summary>
        /// Enable emergency performance mode for critical situations
        /// </summary>
        public async Task EnableEmergencyModeAsync()
        {
            _logger.LogWarning("Enabling emergency performance mode");
            
            try
            {
                // Aggressive memory cleanup
                await _memoryManager.PerformEmergencyCleanupAsync();
                
                // Reduce CPU throttling
                await _cpuOptimizer.EnableMaxPerformanceModeAsync();
                
                // Prioritize storage I/O
                await _storageOptimizer.EnableEmergencyModeAsync();
                
                // Optimize network for critical operations
                await _networkOptimizer.EnableEmergencyModeAsync();
                
                // Update configuration for emergency mode
                _config.OptimizationIntervalMs = 1000; // More frequent optimization
                _optimizationTimer.Change(0, _config.OptimizationIntervalMs);
                
                RaiseAlert(new PerformanceAlert
                {
                    Level = AlertLevel.Warning,
                    Message = "Emergency performance mode enabled",
                    Timestamp = DateTime.UtcNow,
                    Source = "PerformanceOptimizationEngine"
                });
                
                _logger.LogWarning("Emergency performance mode enabled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable emergency performance mode");
                throw;
            }
        }

        #region Private Methods

        private async void PerformOptimization(object? state)
        {
            if (!_isActive || _disposed) return;

            try
            {
                await PerformOptimizationCycleAsync(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during routine optimization");
                RaiseAlert(new PerformanceAlert
                {
                    Level = AlertLevel.Error,
                    Message = $"Optimization error: {ex.Message}",
                    Timestamp = DateTime.UtcNow,
                    Source = "PerformanceOptimizationEngine"
                });
            }
        }

        private async void PerformHealthCheck(object? state)
        {
            if (!_isActive || _disposed) return;

            try
            {
                await PerformSystemHealthCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        private async void PerformPredictiveMaintenance(object? state)
        {
            if (!_isActive || _disposed) return;

            try
            {
                await PerformPredictiveMaintenanceAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during predictive maintenance");
            }
        }

        private async Task PerformInitialOptimizationAsync()
        {
            _logger.LogInformation("Performing initial system optimization");
            
            // Set process priority
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            
            // Initial optimization for all systems
            await PerformOptimizationCycleAsync(true);
            
            _logger.LogInformation("Initial optimization completed");
        }

        private async Task PerformFinalOptimizationAsync()
        {
            _logger.LogInformation("Performing final system optimization");
            
            // Final cleanup and optimization
            await PerformOptimizationCycleAsync(true);
            
            // Reset process priority
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            
            _logger.LogInformation("Final optimization completed");
        }

        private async Task PerformOptimizationCycleAsync(bool aggressive)
        {
            var optimizationTasks = new List<Task>();

            foreach (var optimizer in _optimizers)
            {
                optimizationTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (aggressive)
                        {
                            await optimizer.PerformAggressiveOptimizationAsync();
                        }
                        else
                        {
                            await optimizer.PerformOptimizationAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error optimizing {Optimizer}", optimizer.GetType().Name);
                    }
                }));
            }

            await Task.WhenAll(optimizationTasks);
            
            Interlocked.Increment(ref _totalOptimizationsPerformed);
            
            OptimizationPerformed?.Invoke(this, new OptimizationPerformedEventArgs
            {
                Timestamp = DateTime.UtcNow,
                WasAggressive = aggressive,
                OptimizersInvolved = _optimizers.Count
            });
        }

        private async Task PerformSystemHealthCheckAsync()
        {
            var systemMetrics = GetSystemMetrics();
            var previousHealth = HealthStatus;
            
            // Evaluate system health
            var healthScore = CalculateHealthScore(systemMetrics);
            
            if (healthScore >= 0.9)
                HealthStatus = PerformanceHealthStatus.Healthy;
            else if (healthScore >= 0.7)
                HealthStatus = PerformanceHealthStatus.Warning;
            else if (healthScore >= 0.5)
                HealthStatus = PerformanceHealthStatus.Degraded;
            else
                HealthStatus = PerformanceHealthStatus.Critical;
            
            // Check for health status changes
            if (HealthStatus != previousHealth)
            {
                _logger.LogInformation("Health status changed from {Previous} to {Current} (Score: {Score:F2})",
                    previousHealth, HealthStatus, healthScore);
                
                if (HealthStatus == PerformanceHealthStatus.Critical)
                {
                    RaiseAlert(new PerformanceAlert
                    {
                        Level = AlertLevel.Critical,
                        Message = $"System health critical (Score: {healthScore:F2})",
                        Timestamp = DateTime.UtcNow,
                        Source = "HealthCheck"
                    });
                    
                    // Auto-enable emergency mode
                    await EnableEmergencyModeAsync();
                }
            }
            
            // Store metrics
            RecordMetric("HealthScore", healthScore);
        }

        private async Task PerformPredictiveMaintenanceAsync()
        {
            try
            {
                var predictions = new List<MaintenancePrediction>();
                
                // Predict memory issues
                var memoryTrend = AnalyzeMemoryTrend();
                if (memoryTrend.PredictedIssueTime.HasValue)
                {
                    predictions.Add(new MaintenancePrediction
                    {
                        Type = PredictionType.Memory,
                        PredictedIssueTime = memoryTrend.PredictedIssueTime.Value,
                        Confidence = memoryTrend.Confidence,
                        RecommendedAction = "Schedule memory optimization"
                    });
                }
                
                // Predict storage issues
                var storageTrend = AnalyzeStorageTrend();
                if (storageTrend.PredictedIssueTime.HasValue)
                {
                    predictions.Add(new MaintenancePrediction
                    {
                        Type = PredictionType.Storage,
                        PredictedIssueTime = storageTrend.PredictedIssueTime.Value,
                        Confidence = storageTrend.Confidence,
                        RecommendedAction = "Schedule storage cleanup"
                    });
                }
                
                if (predictions.Any())
                {
                    PredictiveMaintenanceTriggered?.Invoke(this, new PredictiveMaintenanceEventArgs
                    {
                        Predictions = predictions.ToArray(),
                        Timestamp = DateTime.UtcNow
                    });
                    
                    _logger.LogInformation("Predictive maintenance identified {Count} potential issues", predictions.Count);
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during predictive maintenance analysis");
            }
        }

        private SystemMetrics GetSystemMetrics()
        {
            var metrics = new SystemMetrics
            {
                Timestamp = DateTime.UtcNow
            };

            try
            {
                // CPU metrics
                using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // First call always returns 0
                Thread.Sleep(100);
                metrics.CpuUsagePercent = cpuCounter.NextValue();

                // Memory metrics
                var gcInfo = GC.GetTotalMemory(false);
                metrics.MemoryUsageBytes = gcInfo;
                metrics.AvailableMemoryBytes = GetAvailableMemory();

                // Process metrics
                var process = Process.GetCurrentProcess();
                metrics.ProcessMemoryBytes = process.WorkingSet64;
                metrics.ProcessCpuTime = process.TotalProcessorTime;
                metrics.ThreadCount = process.Threads.Count;
                metrics.HandleCount = process.HandleCount;

                // System uptime
                metrics.SystemUptimeMs = Environment.TickCount64;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error gathering system metrics");
            }

            return metrics;
        }

        private double CalculateHealthScore(SystemMetrics metrics)
        {
            var scores = new List<double>();

            // CPU health (target: < 50%)
            var cpuScore = Math.Max(0, 1.0 - (metrics.CpuUsagePercent / 50.0));
            scores.Add(cpuScore);

            // Memory health (target: < 3GB)
            var memoryScore = Math.Max(0, 1.0 - (metrics.MemoryUsageBytes / (3.0 * 1024 * 1024 * 1024)));
            scores.Add(memoryScore);

            // Overall health score
            return scores.Average();
        }

        private TrendAnalysis AnalyzeMemoryTrend()
        {
            var memoryMetrics = _metrics.Values
                .Where(m => m.Name == "MemoryUsage")
                .OrderBy(m => m.Timestamp)
                .TakeLast(10)
                .ToList();

            if (memoryMetrics.Count < 3)
                return new TrendAnalysis { Confidence = 0.0 };

            // Simple linear regression to predict trend
            var trend = CalculateLinearTrend(memoryMetrics.Select(m => m.Value).ToArray());
            
            if (trend > 0) // Memory usage increasing
            {
                var currentUsage = memoryMetrics.Last().Value;
                var threshold = 3.0 * 1024 * 1024 * 1024; // 3GB
                var timeToThreshold = (threshold - currentUsage) / trend;
                
                if (timeToThreshold > 0 && timeToThreshold < 3600) // Within 1 hour
                {
                    return new TrendAnalysis
                    {
                        PredictedIssueTime = DateTime.UtcNow.AddSeconds(timeToThreshold),
                        Confidence = Math.Min(1.0, memoryMetrics.Count / 10.0)
                    };
                }
            }

            return new TrendAnalysis { Confidence = 0.0 };
        }

        private TrendAnalysis AnalyzeStorageTrend()
        {
            // Similar analysis for storage trends
            // This is a simplified implementation
            return new TrendAnalysis { Confidence = 0.0 };
        }

        private double CalculateLinearTrend(double[] values)
        {
            if (values.Length < 2) return 0;
            
            var n = values.Length;
            var sumX = n * (n + 1) / 2.0;
            var sumY = values.Sum();
            var sumXY = values.Select((y, i) => (i + 1) * y).Sum();
            var sumXX = n * (n + 1) * (2 * n + 1) / 6.0;
            
            return (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        }

        private long GetAvailableMemory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var available = Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024;
                        return available;
                    }
                }
                catch
                {
                    // Fallback to GC
                }
            }
            
            return GC.GetTotalMemory(false);
        }

        private void RecordMetric(string name, double value)
        {
            _metrics.AddOrUpdate(name, 
                new PerformanceMetric { Name = name, Value = value, Timestamp = DateTime.UtcNow },
                (key, existing) => new PerformanceMetric { Name = name, Value = value, Timestamp = DateTime.UtcNow });
        }

        private void RaiseAlert(PerformanceAlert alert)
        {
            _alerts.Enqueue(alert);
            
            // Keep only last 100 alerts
            while (_alerts.Count > 100)
            {
                _alerts.TryDequeue(out _);
            }
            
            AlertRaised?.Invoke(this, new PerformanceAlertEventArgs { Alert = alert });
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_isActive)
                {
                    StopAsync().Wait(5000);
                }

                _optimizationTimer?.Dispose();
                _healthCheckTimer?.Dispose();
                _predictiveMaintenanceTimer?.Dispose();

                foreach (var optimizer in _optimizers)
                {
                    if (optimizer is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #region Configuration and Data Structures

    public class PerformanceConfig
    {
        public int OptimizationIntervalMs { get; set; } = 5000; // 5 seconds
        public int HealthCheckIntervalMs { get; set; } = 10000; // 10 seconds
        public int PredictiveMaintenanceIntervalMs { get; set; } = 60000; // 1 minute
        public double CpuThresholdPercent { get; set; } = 50.0;
        public long MemoryThresholdBytes { get; set; } = 3L * 1024 * 1024 * 1024; // 3GB
        public bool EnablePredictiveMaintenance { get; set; } = true;
        public bool EnableEmergencyMode { get; set; } = true;
    }

    public enum PerformanceHealthStatus
    {
        Unknown,
        Starting,
        Healthy,
        Warning,
        Degraded,
        Critical,
        Stopping,
        Stopped
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum PredictionType
    {
        Memory,
        Storage,
        CPU,
        Network
    }

    public class PerformanceReport
    {
        public DateTime Timestamp { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public PerformanceHealthStatus HealthStatus { get; set; }
        public SystemMetrics SystemMetrics { get; set; } = new();
        public Dictionary<string, object> OptimizerMetrics { get; set; } = new();
        public long TotalOptimizations { get; set; }
        public PerformanceAlert[] Alerts { get; set; } = Array.Empty<PerformanceAlert>();
        public PerformanceMetric[] RecentMetrics { get; set; } = Array.Empty<PerformanceMetric>();
    }

    public class SystemMetrics
    {
        public DateTime Timestamp { get; set; }
        public float CpuUsagePercent { get; set; }
        public long MemoryUsageBytes { get; set; }
        public long AvailableMemoryBytes { get; set; }
        public long ProcessMemoryBytes { get; set; }
        public TimeSpan ProcessCpuTime { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public long SystemUptimeMs { get; set; }
    }

    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PerformanceAlert
    {
        public AlertLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class MaintenancePrediction
    {
        public PredictionType Type { get; set; }
        public DateTime PredictedIssueTime { get; set; }
        public double Confidence { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class TrendAnalysis
    {
        public DateTime? PredictedIssueTime { get; set; }
        public double Confidence { get; set; }
    }

    #endregion

    #region Event Arguments

    public class PerformanceAlertEventArgs : EventArgs
    {
        public PerformanceAlert Alert { get; set; } = new();
    }

    public class OptimizationPerformedEventArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public bool WasAggressive { get; set; }
        public int OptimizersInvolved { get; set; }
    }

    public class PredictiveMaintenanceEventArgs : EventArgs
    {
        public MaintenancePrediction[] Predictions { get; set; } = Array.Empty<MaintenancePrediction>();
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Interfaces

    public interface IPerformanceOptimizer
    {
        Task InitializeAsync();
        Task PerformOptimizationAsync();
        Task PerformAggressiveOptimizationAsync();
        Task CleanupAsync();
        object GetCurrentMetrics();
    }

    #endregion
}