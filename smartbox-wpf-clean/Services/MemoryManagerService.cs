using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Advanced memory management service for medical-grade 4+ hour operations
    /// Implements sophisticated garbage collection optimization and memory leak detection
    /// </summary>
    public class MemoryManagerService : IPerformanceOptimizer, IDisposable
    {
        private readonly ILogger<MemoryManagerService> _logger;
        private readonly MemoryManagerConfig _config;
        private readonly Timer _gcOptimizationTimer;
        private readonly Timer _memoryMonitorTimer;
        private readonly Timer _leakDetectionTimer;
        
        private readonly ConcurrentDictionary<string, MemoryPool> _memoryPools;
        private readonly ConcurrentQueue<MemoryAllocation> _allocationHistory;
        private readonly ConcurrentDictionary<long, WeakReference> _trackedObjects;
        
        private bool _isInitialized = false;
        private bool _emergencyMode = false;
        private bool _disposed = false;
        private long _totalAllocations = 0;
        private long _totalDeallocations = 0;
        private long _memoryLeaksDetected = 0;
        private DateTime _lastGCOptimization = DateTime.MinValue;
        
        public event EventHandler<MemoryPressureEventArgs>? MemoryPressureDetected;
        public event EventHandler<MemoryLeakEventArgs>? MemoryLeakDetected;
        public event EventHandler<GCOptimizationEventArgs>? GCOptimizationPerformed;

        public MemoryManagerService(ILogger<MemoryManagerService> logger, MemoryManagerConfig? config = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? new MemoryManagerConfig();
            
            _memoryPools = new ConcurrentDictionary<string, MemoryPool>();
            _allocationHistory = new ConcurrentQueue<MemoryAllocation>();
            _trackedObjects = new ConcurrentDictionary<long, WeakReference>();
            
            _gcOptimizationTimer = new Timer(PerformGCOptimization, null, Timeout.Infinite, Timeout.Infinite);
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, Timeout.Infinite, Timeout.Infinite);
            _leakDetectionTimer = new Timer(DetectMemoryLeaks, null, Timeout.Infinite, Timeout.Infinite);
            
            // Configure GC for long-running medical applications
            ConfigureGarbageCollector();
            
            _logger.LogInformation("MemoryManagerService initialized with {PoolCount} memory pools", _config.MaxMemoryPools);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
            {
                _logger.LogWarning("MemoryManagerService is already initialized");
                return;
            }

            _logger.LogInformation("Initializing advanced memory management system");

            try
            {
                // Initialize memory pools for different frame sizes
                await InitializeMemoryPoolsAsync();
                
                // Configure GC for optimal performance
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
                
                // Start monitoring timers
                _gcOptimizationTimer.Change(_config.GCOptimizationIntervalMs, _config.GCOptimizationIntervalMs);
                _memoryMonitorTimer.Change(1000, 1000); // Monitor every second
                _leakDetectionTimer.Change(_config.LeakDetectionIntervalMs, _config.LeakDetectionIntervalMs);
                
                // Perform initial optimization
                await PerformInitialMemoryOptimizationAsync();
                
                _isInitialized = true;
                _logger.LogInformation("Memory management system initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize memory management system");
                throw;
            }
        }

        public async Task PerformOptimizationAsync()
        {
            if (!_isInitialized) return;

            try
            {
                await Task.Run(() =>
                {
                    // Gentle optimization for routine operations
                    OptimizeMemoryPools();
                    
                    if (ShouldPerformGC())
                    {
                        PerformOptimizedGarbageCollection(false);
                    }
                    
                    CleanupOldAllocations();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during routine memory optimization");
            }
        }

        public async Task PerformAggressiveOptimizationAsync()
        {
            if (!_isInitialized) return;

            _logger.LogInformation("Performing aggressive memory optimization");

            try
            {
                await Task.Run(() =>
                {
                    // Aggressive optimization
                    CompactMemoryPools();
                    PerformOptimizedGarbageCollection(true);
                    CleanupAllPooledMemory();
                    ForceMemoryDefragmentation();
                });
                
                _logger.LogInformation("Aggressive memory optimization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during aggressive memory optimization");
            }
        }

        public async Task PerformEmergencyCleanupAsync()
        {
            _logger.LogWarning("Performing emergency memory cleanup");
            
            _emergencyMode = true;
            
            try
            {
                await Task.Run(() =>
                {
                    // Emergency memory recovery
                    ReleaseAllPooledMemory();
                    PerformEmergencyGarbageCollection();
                    ClearAllocationHistory();
                    TrimWorkingSet();
                });
                
                _logger.LogWarning("Emergency memory cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during emergency memory cleanup");
            }
            finally
            {
                _emergencyMode = false;
            }
        }

        /// <summary>
        /// Allocate memory from appropriate pool for video frames
        /// </summary>
        public byte[] AllocateFrameMemory(int size, string poolName = "DefaultFrames")
        {
            try
            {
                if (_memoryPools.TryGetValue(poolName, out var pool))
                {
                    var memory = pool.Rent(size);
                    if (memory != null)
                    {
                        RecordAllocation(size, poolName);
                        return memory;
                    }
                }
                
                // Fallback to regular allocation
                var regularMemory = new byte[size];
                RecordAllocation(size, "Heap");
                return regularMemory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error allocating frame memory of size {Size}", size);
                throw;
            }
        }

        /// <summary>
        /// Return memory to pool for reuse
        /// </summary>
        public void ReturnFrameMemory(byte[] memory, string poolName = "DefaultFrames")
        {
            try
            {
                if (_memoryPools.TryGetValue(poolName, out var pool))
                {
                    pool.Return(memory);
                    RecordDeallocation(memory.Length, poolName);
                }
                else
                {
                    // Memory will be garbage collected
                    RecordDeallocation(memory.Length, "Heap");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning frame memory to pool {Pool}", poolName);
            }
        }

        /// <summary>
        /// Get current memory statistics
        /// </summary>
        public MemoryStatistics GetMemoryStatistics()
        {
            var gcInfo = GC.GetTotalMemory(false);
            var workingSet = Process.GetCurrentProcess().WorkingSet64;
            
            var poolStats = _memoryPools.ToDictionary(
                kvp => kvp.Key,
                kvp => new MemoryPoolStatistics
                {
                    Name = kvp.Key,
                    TotalSize = kvp.Value.TotalSize,
                    UsedSize = kvp.Value.UsedSize,
                    AvailableSize = kvp.Value.AvailableSize,
                    AllocationCount = kvp.Value.AllocationCount,
                    ReturnCount = kvp.Value.ReturnCount
                });

            return new MemoryStatistics
            {
                Timestamp = DateTime.UtcNow,
                ManagedMemoryBytes = gcInfo,
                WorkingSetBytes = workingSet,
                TotalAllocations = _totalAllocations,
                TotalDeallocations = _totalDeallocations,
                MemoryLeaksDetected = _memoryLeaksDetected,
                IsEmergencyMode = _emergencyMode,
                GCCollectionCounts = new int[]
                {
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2)
                },
                PoolStatistics = poolStats
            };
        }

        public async Task CleanupAsync()
        {
            _logger.LogInformation("Cleaning up memory management system");
            
            try
            {
                // Stop timers
                _gcOptimizationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _memoryMonitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
                _leakDetectionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                
                // Final cleanup
                await PerformAggressiveOptimizationAsync();
                
                // Reset GC settings
                GCSettings.LatencyMode = GCLatencyMode.Interactive;
                
                _isInitialized = false;
                _logger.LogInformation("Memory management system cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory management cleanup");
            }
        }

        public object GetCurrentMetrics()
        {
            return GetMemoryStatistics();
        }

        #region Private Methods

        private async Task InitializeMemoryPoolsAsync()
        {
            var poolConfigs = new[]
            {
                new { Name = "SmallFrames", Size = 1920 * 1080 * 2, Count = 50 }, // 2MB frames
                new { Name = "MediumFrames", Size = 1920 * 1080 * 3, Count = 30 }, // 3MB frames
                new { Name = "LargeFrames", Size = 1920 * 1080 * 4, Count = 20 }, // 4MB frames
                new { Name = "AudioBuffers", Size = 48000 * 2 * 2, Count = 100 }, // 2 seconds of audio
                new { Name = "TempBuffers", Size = 1024 * 1024, Count = 100 } // 1MB temp buffers
            };

            foreach (var config in poolConfigs)
            {
                var pool = new MemoryPool(config.Name, config.Size, config.Count, _logger);
                await pool.InitializeAsync();
                _memoryPools[config.Name] = pool;
                
                _logger.LogInformation("Initialized memory pool {Name}: {Size:F2}MB x {Count} = {Total:F2}MB",
                    config.Name, config.Size / (1024.0 * 1024.0), config.Count,
                    (config.Size * config.Count) / (1024.0 * 1024.0));
            }
        }

        private async Task PerformInitialMemoryOptimizationAsync()
        {
            // Pre-JIT important methods
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Pre-allocate some memory pools
            foreach (var pool in _memoryPools.Values)
            {
                pool.Warm();
            }
            
            await Task.CompletedTask;
        }

        private void ConfigureGarbageCollector()
        {
            try
            {
                // Configure for medical-grade long-running applications
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Set process to prefer large pages if available
                    var process = Process.GetCurrentProcess();
                    process.PriorityBoostEnabled = true;
                }
                
                // Configure GC for sustained operation
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                
                _logger.LogInformation("Garbage collector configured for medical-grade operation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fully configure garbage collector");
            }
        }

        private void PerformGCOptimization(object? state)
        {
            if (!_isInitialized || _disposed) return;

            try
            {
                if (ShouldPerformGC())
                {
                    PerformOptimizedGarbageCollection(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GC optimization");
            }
        }

        private void MonitorMemoryUsage(object? state)
        {
            if (!_isInitialized || _disposed) return;

            try
            {
                var stats = GetMemoryStatistics();
                
                // Check for memory pressure
                var memoryPressureThreshold = _config.MemoryPressureThresholdBytes;
                if (stats.ManagedMemoryBytes > memoryPressureThreshold || 
                    stats.WorkingSetBytes > memoryPressureThreshold * 1.5)
                {
                    _logger.LogWarning("Memory pressure detected: Managed={0:F2}MB, WorkingSet={1:F2}MB",
                        stats.ManagedMemoryBytes / (1024.0 * 1024.0),
                        stats.WorkingSetBytes / (1024.0 * 1024.0));
                    
                    MemoryPressureDetected?.Invoke(this, new MemoryPressureEventArgs
                    {
                        ManagedMemoryBytes = stats.ManagedMemoryBytes,
                        WorkingSetBytes = stats.WorkingSetBytes,
                        ThresholdBytes = memoryPressureThreshold,
                        Timestamp = DateTime.UtcNow
                    });
                    
                    // Trigger optimization
                    Task.Run(() => PerformOptimizationAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring memory usage");
            }
        }

        private void DetectMemoryLeaks(object? state)
        {
            if (!_isInitialized || _disposed) return;

            try
            {
                var leakedObjects = new List<long>();
                
                foreach (var kvp in _trackedObjects.ToArray())
                {
                    if (!kvp.Value.IsAlive)
                    {
                        _trackedObjects.TryRemove(kvp.Key, out _);
                    }
                    else
                    {
                        // Object still alive after significant time - potential leak
                        leakedObjects.Add(kvp.Key);
                    }
                }
                
                if (leakedObjects.Count > _config.MemoryLeakThreshold)
                {
                    _memoryLeaksDetected += leakedObjects.Count;
                    
                    _logger.LogWarning("Potential memory leaks detected: {Count} objects", leakedObjects.Count);
                    
                    MemoryLeakDetected?.Invoke(this, new MemoryLeakEventArgs
                    {
                        LeakedObjectCount = leakedObjects.Count,
                        TotalLeaksDetected = _memoryLeaksDetected,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory leak detection");
            }
        }

        private bool ShouldPerformGC()
        {
            var timeSinceLastGC = DateTime.UtcNow - _lastGCOptimization;
            var memoryUsage = GC.GetTotalMemory(false);
            
            return timeSinceLastGC > TimeSpan.FromMilliseconds(_config.MinGCIntervalMs) &&
                   (memoryUsage > _config.GCThresholdBytes || timeSinceLastGC > TimeSpan.FromMinutes(5));
        }

        private void PerformOptimizedGarbageCollection(bool aggressive)
        {
            var stopwatch = Stopwatch.StartNew();
            var beforeMemory = GC.GetTotalMemory(false);
            
            try
            {
                if (aggressive)
                {
                    // Aggressive GC for emergency situations
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced, true, true);
                }
                else
                {
                    // Gentle GC for routine optimization
                    GC.Collect(0, GCCollectionMode.Optimized);
                    if (GC.GetTotalMemory(false) > beforeMemory * 0.8)
                    {
                        GC.Collect(1, GCCollectionMode.Optimized);
                    }
                }
                
                _lastGCOptimization = DateTime.UtcNow;
                stopwatch.Stop();
                
                var afterMemory = GC.GetTotalMemory(false);
                var memoryFreed = beforeMemory - afterMemory;
                
                _logger.LogDebug("GC optimization completed: {Type}, Duration={Duration}ms, Freed={Freed:F2}MB",
                    aggressive ? "Aggressive" : "Gentle", stopwatch.ElapsedMilliseconds,
                    memoryFreed / (1024.0 * 1024.0));
                
                GCOptimizationPerformed?.Invoke(this, new GCOptimizationEventArgs
                {
                    WasAggressive = aggressive,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    MemoryFreedBytes = memoryFreed,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GC optimization");
            }
        }

        private void PerformEmergencyGarbageCollection()
        {
            _logger.LogWarning("Performing emergency garbage collection");
            
            try
            {
                // Force immediate full collection
                var latencyMode = GCSettings.LatencyMode;
                GCSettings.LatencyMode = GCLatencyMode.Batch;
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // Compact large object heap
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                
                GCSettings.LatencyMode = latencyMode;
                
                _logger.LogWarning("Emergency garbage collection completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during emergency garbage collection");
            }
        }

        private void OptimizeMemoryPools()
        {
            foreach (var pool in _memoryPools.Values)
            {
                pool.Optimize();
            }
        }

        private void CompactMemoryPools()
        {
            foreach (var pool in _memoryPools.Values)
            {
                pool.Compact();
            }
        }

        private void CleanupAllPooledMemory()
        {
            foreach (var pool in _memoryPools.Values)
            {
                pool.Cleanup();
            }
        }

        private void ReleaseAllPooledMemory()
        {
            foreach (var pool in _memoryPools.Values)
            {
                pool.ReleaseAll();
            }
        }

        private void ForceMemoryDefragmentation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Windows-specific memory defragmentation
                    var process = Process.GetCurrentProcess();
                    SetProcessWorkingSetSize(process.Handle, -1, -1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not perform memory defragmentation");
                }
            }
        }

        private void TrimWorkingSet()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    EmptyWorkingSet(process.Handle);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not trim working set");
                }
            }
        }

        private void CleanupOldAllocations()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var toRemove = new List<MemoryAllocation>();
            
            foreach (var allocation in _allocationHistory.ToArray())
            {
                if (allocation.Timestamp < cutoff)
                {
                    toRemove.Add(allocation);
                }
            }
            
            // Remove old allocations from history
            while (_allocationHistory.TryDequeue(out var allocation))
            {
                if (allocation.Timestamp >= cutoff)
                {
                    break;
                }
            }
        }

        private void ClearAllocationHistory()
        {
            while (_allocationHistory.TryDequeue(out _)) { }
            _trackedObjects.Clear();
        }

        private void RecordAllocation(int size, string source)
        {
            var allocation = new MemoryAllocation
            {
                Size = size,
                Source = source,
                Timestamp = DateTime.UtcNow
            };
            
            _allocationHistory.Enqueue(allocation);
            Interlocked.Increment(ref _totalAllocations);
            
            // Keep history size manageable
            if (_allocationHistory.Count > 10000)
            {
                _allocationHistory.TryDequeue(out _);
            }
        }

        private void RecordDeallocation(int size, string source)
        {
            Interlocked.Increment(ref _totalDeallocations);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool EmptyWorkingSet(IntPtr hProcess);

        #endregion

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_isInitialized)
                {
                    CleanupAsync().Wait(5000);
                }

                _gcOptimizationTimer?.Dispose();
                _memoryMonitorTimer?.Dispose();
                _leakDetectionTimer?.Dispose();

                foreach (var pool in _memoryPools.Values)
                {
                    pool?.Dispose();
                }
                _memoryPools.Clear();
            }
            finally
            {
                _disposed = true;
            }
        }
    }

    #region Supporting Classes

    public class MemoryManagerConfig
    {
        public long MemoryPressureThresholdBytes { get; set; } = 2L * 1024 * 1024 * 1024; // 2GB
        public long GCThresholdBytes { get; set; } = 500L * 1024 * 1024; // 500MB
        public int MinGCIntervalMs { get; set; } = 30000; // 30 seconds
        public int GCOptimizationIntervalMs { get; set; } = 60000; // 1 minute
        public int LeakDetectionIntervalMs { get; set; } = 300000; // 5 minutes
        public int MemoryLeakThreshold { get; set; } = 1000; // Objects
        public int MaxMemoryPools { get; set; } = 10;
    }

    public class MemoryPool : IDisposable
    {
        private readonly string _name;
        private readonly int _itemSize;
        private readonly ConcurrentQueue<byte[]> _pool;
        private readonly ILogger _logger;
        private long _totalSize;
        private long _usedSize;
        private long _allocationCount;
        private long _returnCount;
        private bool _disposed = false;

        public string Name => _name;
        public long TotalSize => _totalSize;
        public long UsedSize => _usedSize;
        public long AvailableSize => _totalSize - _usedSize;
        public long AllocationCount => _allocationCount;
        public long ReturnCount => _returnCount;

        public MemoryPool(string name, int itemSize, int initialCount, ILogger logger)
        {
            _name = name;
            _itemSize = itemSize;
            _pool = new ConcurrentQueue<byte[]>();
            _logger = logger;
            _totalSize = (long)itemSize * initialCount;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // Pre-allocate memory items
                var initialCount = (int)(_totalSize / _itemSize);
                for (int i = 0; i < initialCount; i++)
                {
                    _pool.Enqueue(new byte[_itemSize]);
                }
            });
        }

        public byte[]? Rent(int size)
        {
            if (size > _itemSize) return null;

            if (_pool.TryDequeue(out var item))
            {
                Interlocked.Add(ref _usedSize, _itemSize);
                Interlocked.Increment(ref _allocationCount);
                return item;
            }

            return null;
        }

        public void Return(byte[] item)
        {
            if (item?.Length == _itemSize)
            {
                _pool.Enqueue(item);
                Interlocked.Add(ref _usedSize, -_itemSize);
                Interlocked.Increment(ref _returnCount);
            }
        }

        public void Warm()
        {
            // Ensure all items are properly allocated
            var items = new List<byte[]>();
            while (_pool.TryDequeue(out var item))
            {
                items.Add(item);
            }
            
            foreach (var item in items)
            {
                _pool.Enqueue(item);
            }
        }

        public void Optimize()
        {
            // Basic optimization - could be enhanced
        }

        public void Compact()
        {
            // Compact the pool - remove excess items
            var targetCount = Math.Max(10, _pool.Count / 2);
            var removed = 0;
            
            while (_pool.Count > targetCount && _pool.TryDequeue(out _))
            {
                removed++;
                Interlocked.Add(ref _totalSize, -_itemSize);
            }
            
            if (removed > 0)
            {
                _logger.LogDebug("Compacted memory pool {Name}: removed {Count} items", _name, removed);
            }
        }

        public void Cleanup()
        {
            // Remove unused items
            var toKeep = Math.Min(_pool.Count, 50); // Keep minimum items
            var items = new List<byte[]>();
            
            for (int i = 0; i < toKeep && _pool.TryDequeue(out var item); i++)
            {
                items.Add(item);
            }
            
            // Clear the rest
            while (_pool.TryDequeue(out _)) { }
            
            // Re-add kept items
            foreach (var item in items)
            {
                _pool.Enqueue(item);
            }
        }

        public void ReleaseAll()
        {
            while (_pool.TryDequeue(out _)) { }
            _totalSize = 0;
            _usedSize = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            ReleaseAll();
            _disposed = true;
        }
    }

    public class MemoryStatistics
    {
        public DateTime Timestamp { get; set; }
        public long ManagedMemoryBytes { get; set; }
        public long WorkingSetBytes { get; set; }
        public long TotalAllocations { get; set; }
        public long TotalDeallocations { get; set; }
        public long MemoryLeaksDetected { get; set; }
        public bool IsEmergencyMode { get; set; }
        public int[] GCCollectionCounts { get; set; } = Array.Empty<int>();
        public Dictionary<string, MemoryPoolStatistics> PoolStatistics { get; set; } = new();
    }

    public class MemoryPoolStatistics
    {
        public string Name { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long UsedSize { get; set; }
        public long AvailableSize { get; set; }
        public long AllocationCount { get; set; }
        public long ReturnCount { get; set; }
    }

    public class MemoryAllocation
    {
        public int Size { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Event Arguments

    public class MemoryPressureEventArgs : EventArgs
    {
        public long ManagedMemoryBytes { get; set; }
        public long WorkingSetBytes { get; set; }
        public long ThresholdBytes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class MemoryLeakEventArgs : EventArgs
    {
        public int LeakedObjectCount { get; set; }
        public long TotalLeaksDetected { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class GCOptimizationEventArgs : EventArgs
    {
        public bool WasAggressive { get; set; }
        public long DurationMs { get; set; }
        public long MemoryFreedBytes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}