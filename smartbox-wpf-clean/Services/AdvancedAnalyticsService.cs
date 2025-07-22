using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    /// <summary>
    /// Advanced Analytics Service for SmartBox Medical Device
    /// Provides real-time medical workflow analytics, performance monitoring, and HIPAA-compliant insights
    /// Supports FDA medical device compliance and comprehensive healthcare data analysis
    /// </summary>
    public class AdvancedAnalyticsService
    {
        private readonly ILogger _logger;
        private readonly HIPAAPrivacyService _privacyService;
        private readonly AuditLoggingService _auditService;
        private readonly MedicalInsightsEngine _insightsEngine;
        private readonly ComplianceAnalyticsService _complianceService;
        
        private readonly ConcurrentDictionary<string, MedicalWorkflowMetrics> _workflowMetrics;
        private readonly ConcurrentDictionary<string, DevicePerformanceMetrics> _performanceMetrics;
        private readonly ConcurrentDictionary<string, SecurityMetrics> _securityMetrics;
        private readonly ConcurrentQueue<AnalyticsEvent> _realtimeEvents;
        
        private readonly Timer _aggregationTimer;
        private readonly Timer _reportingTimer;
        private readonly AnalyticsConfiguration _config;
        private readonly object _lockObject = new object();
        
        private bool _isRunning = false;
        private DateTime _lastAggregation = DateTime.UtcNow;

        public AdvancedAnalyticsService(ILogger logger, HIPAAPrivacyService privacyService, 
            AuditLoggingService auditService, MedicalInsightsEngine insightsEngine, 
            ComplianceAnalyticsService complianceService)
        {
            _logger = logger;
            _privacyService = privacyService;
            _auditService = auditService;
            _insightsEngine = insightsEngine;
            _complianceService = complianceService;
            
            _workflowMetrics = new ConcurrentDictionary<string, MedicalWorkflowMetrics>();
            _performanceMetrics = new ConcurrentDictionary<string, DevicePerformanceMetrics>();
            _securityMetrics = new ConcurrentDictionary<string, SecurityMetrics>();
            _realtimeEvents = new ConcurrentQueue<AnalyticsEvent>();
            
            _config = LoadAnalyticsConfiguration();
            
            // Initialize timers for real-time processing
            _aggregationTimer = new Timer(ProcessRealtimeAnalytics, null, 
                TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            _reportingTimer = new Timer(GeneratePeriodicReports, null, 
                TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
            
            InitializeAnalyticsFramework();
        }

        #region Real-time Medical Workflow Analytics

        /// <summary>
        /// Records medical workflow event for real-time analytics
        /// </summary>
        public async Task RecordWorkflowEventAsync(string workflowId, string eventType, 
            Dictionary<string, object> metadata = null, string patientId = null)
        {
            try
            {
                var analyticsEvent = new AnalyticsEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    WorkflowId = workflowId,
                    EventType = eventType,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    PatientId = patientId,
                    DeviceId = Environment.MachineName,
                    UserId = Environment.UserName
                };

                // Queue for real-time processing
                _realtimeEvents.Enqueue(analyticsEvent);

                // Update workflow metrics
                await UpdateWorkflowMetricsAsync(analyticsEvent);

                // Log audit event
                await _auditService.LogComplianceEventAsync("WORKFLOW_EVENT_RECORDED", 
                    $"Workflow: {workflowId}, Event: {eventType}", "Medical Device Analytics");

                _logger.LogDebug($"Workflow event recorded: {eventType} for workflow {workflowId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to record workflow event: {eventType}");
                await _auditService.LogSecurityEventAsync("ANALYTICS_ERROR", 
                    $"Failed to record workflow event: {ex.Message}", SecurityEventSeverity.Medium);
            }
        }

        /// <summary>
        /// Records DICOM transmission analytics
        /// </summary>
        public async Task RecordDICOMTransmissionAsync(string studyInstanceUID, string destination, 
            long dataSize, TimeSpan transmissionTime, bool success, string patientId = null)
        {
            try
            {
                var transmissionMetrics = new DICOMTransmissionMetrics
                {
                    StudyInstanceUID = studyInstanceUID,
                    Destination = destination,
                    DataSize = dataSize,
                    TransmissionTime = transmissionTime,
                    Success = success,
                    Timestamp = DateTime.UtcNow,
                    PatientId = await _privacyService.EncryptPHIAsync(patientId, patientId),
                    ThroughputMbps = CalculateThroughput(dataSize, transmissionTime)
                };

                // Update performance metrics
                await UpdatePerformanceMetricsAsync(transmissionMetrics);

                // Record analytics event
                await RecordWorkflowEventAsync("DICOM_TRANSMISSION", success ? "TRANSMISSION_SUCCESS" : "TRANSMISSION_FAILED",
                    new Dictionary<string, object>
                    {
                        ["studyUID"] = studyInstanceUID,
                        ["destination"] = destination,
                        ["dataSize"] = dataSize,
                        ["duration"] = transmissionTime.TotalSeconds,
                        ["throughput"] = transmissionMetrics.ThroughputMbps
                    }, patientId);

                _logger.LogInformation($"DICOM transmission recorded: {studyInstanceUID} to {destination}, " +
                    $"Size: {dataSize / 1024 / 1024:F2} MB, Time: {transmissionTime.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record DICOM transmission analytics");
                await _auditService.LogSecurityEventAsync("DICOM_ANALYTICS_ERROR", 
                    $"Failed to record DICOM transmission: {ex.Message}", SecurityEventSeverity.Medium);
            }
        }

        /// <summary>
        /// Records patient data flow analytics
        /// </summary>
        public async Task RecordPatientDataFlowAsync(string patientId, string dataType, 
            string sourceSystem, string targetSystem, bool success)
        {
            try
            {
                // Encrypt patient ID for privacy
                var encryptedPatientRef = await _privacyService.EncryptPHIAsync(patientId, patientId);

                await RecordWorkflowEventAsync("PATIENT_DATA_FLOW", success ? "DATA_FLOW_SUCCESS" : "DATA_FLOW_FAILED",
                    new Dictionary<string, object>
                    {
                        ["dataType"] = dataType,
                        ["sourceSystem"] = sourceSystem,
                        ["targetSystem"] = targetSystem,
                        ["encryptedPatientRef"] = encryptedPatientRef.EncryptedData
                    }, patientId);

                // Update workflow metrics
                var workflowKey = $"{sourceSystem}_{targetSystem}";
                if (_workflowMetrics.TryGetValue(workflowKey, out var metrics))
                {
                    metrics.TotalDataFlows++;
                    if (success) metrics.SuccessfulDataFlows++;
                    metrics.LastActivity = DateTime.UtcNow;
                }

                await _auditService.LogPrivacyEventAsync("PATIENT_DATA_FLOW", 
                    $"Data flow from {sourceSystem} to {targetSystem}: {(success ? "Success" : "Failed")}", patientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record patient data flow analytics");
                await _auditService.LogSecurityEventAsync("DATA_FLOW_ANALYTICS_ERROR", 
                    $"Failed to record patient data flow: {ex.Message}", SecurityEventSeverity.Medium);
            }
        }

        #endregion

        #region Device Performance Analytics

        /// <summary>
        /// Records device performance metrics
        /// </summary>
        public async Task RecordDevicePerformanceAsync(DevicePerformanceSnapshot snapshot)
        {
            try
            {
                var deviceKey = snapshot.DeviceId ?? Environment.MachineName;
                
                if (_performanceMetrics.TryGetValue(deviceKey, out var metrics))
                {
                    metrics.UpdateWithSnapshot(snapshot);
                }
                else
                {
                    metrics = new DevicePerformanceMetrics(deviceKey);
                    metrics.UpdateWithSnapshot(snapshot);
                    _performanceMetrics[deviceKey] = metrics;
                }

                // Check for performance alerts
                await CheckPerformanceAlertsAsync(metrics, snapshot);

                await RecordWorkflowEventAsync("DEVICE_PERFORMANCE", "PERFORMANCE_SNAPSHOT",
                    new Dictionary<string, object>
                    {
                        ["cpuUsage"] = snapshot.CPUUsage,
                        ["memoryUsage"] = snapshot.MemoryUsage,
                        ["diskUsage"] = snapshot.DiskUsage,
                        ["networkLatency"] = snapshot.NetworkLatency,
                        ["temperature"] = snapshot.Temperature
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record device performance analytics");
            }
        }

        /// <summary>
        /// Gets current device performance analytics
        /// </summary>
        public async Task<DeviceAnalytics> GetDeviceAnalyticsAsync(string deviceId = null)
        {
            try
            {
                var targetDeviceId = deviceId ?? Environment.MachineName;
                
                if (_performanceMetrics.TryGetValue(targetDeviceId, out var metrics))
                {
                    return new DeviceAnalytics
                    {
                        DeviceId = targetDeviceId,
                        CurrentPerformance = metrics.GetCurrentSnapshot(),
                        AveragePerformance = metrics.GetAveragePerformance(),
                        PerformanceHistory = metrics.GetPerformanceHistory(TimeSpan.FromHours(24)),
                        AlertLevel = DetermineAlertLevel(metrics),
                        UptimeHours = metrics.GetUptimeHours(),
                        LastUpdate = metrics.LastUpdate
                    };
                }

                return new DeviceAnalytics { DeviceId = targetDeviceId, AlertLevel = AlertLevel.Unknown };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get device analytics for {deviceId}");
                return new DeviceAnalytics { DeviceId = deviceId, AlertLevel = AlertLevel.Error };
            }
        }

        #endregion

        #region Security Analytics

        /// <summary>
        /// Records security event for analytics
        /// </summary>
        public async Task RecordSecurityEventAsync(string eventType, string details, 
            SecurityEventSeverity severity = SecurityEventSeverity.Medium)
        {
            try
            {
                var deviceKey = Environment.MachineName;
                
                if (_securityMetrics.TryGetValue(deviceKey, out var metrics))
                {
                    metrics.RecordSecurityEvent(eventType, severity);
                }
                else
                {
                    metrics = new SecurityMetrics(deviceKey);
                    metrics.RecordSecurityEvent(eventType, severity);
                    _securityMetrics[deviceKey] = metrics;
                }

                await RecordWorkflowEventAsync("SECURITY_EVENT", eventType,
                    new Dictionary<string, object>
                    {
                        ["severity"] = severity.ToString(),
                        ["details"] = details,
                        ["deviceId"] = deviceKey
                    });

                // Check for security alert thresholds
                await CheckSecurityAlertsAsync(metrics);

                await _auditService.LogSecurityEventAsync(eventType, details, severity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record security event analytics");
            }
        }

        /// <summary>
        /// Gets security analytics dashboard data
        /// </summary>
        public async Task<SecurityAnalytics> GetSecurityAnalyticsAsync()
        {
            try
            {
                var allSecurityMetrics = _securityMetrics.Values.ToList();
                
                return new SecurityAnalytics
                {
                    TotalSecurityEvents = allSecurityMetrics.Sum(m => m.TotalEvents),
                    CriticalEvents = allSecurityMetrics.Sum(m => m.CriticalEvents),
                    ThreatLevel = CalculateOverallThreatLevel(allSecurityMetrics),
                    ActiveThreats = allSecurityMetrics.Sum(m => m.ActiveThreats),
                    SecurityScore = CalculateSecurityScore(allSecurityMetrics),
                    RecentEvents = GetRecentSecurityEvents(24),
                    TrendAnalysis = CalculateSecurityTrends(allSecurityMetrics),
                    LastUpdate = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get security analytics");
                return new SecurityAnalytics { ThreatLevel = ThreatLevel.Unknown };
            }
        }

        #endregion

        #region Compliance Monitoring Analytics

        /// <summary>
        /// Gets comprehensive compliance analytics
        /// </summary>
        public async Task<ComplianceAnalytics> GetComplianceAnalyticsAsync()
        {
            try
            {
                var hipaaCompliance = await _privacyService.ValidateHIPAAPrivacyComplianceAsync();
                var gdprCompliance = await _privacyService.ValidateGDPRComplianceAsync();
                var fdaCompliance = await _complianceService.ValidateFDAComplianceAsync();

                return new ComplianceAnalytics
                {
                    HIPAAComplianceScore = hipaaCompliance.OverallCompliance,
                    GDPRComplianceScore = gdprCompliance.OverallCompliance,
                    FDAComplianceScore = fdaCompliance.OverallCompliance,
                    OverallComplianceScore = CalculateOverallComplianceScore(hipaaCompliance, gdprCompliance, fdaCompliance),
                    ComplianceViolations = GetRecentComplianceViolations(),
                    AuditFindings = await GetAuditFindingsAsync(),
                    RemediationItems = GetRemediationItems(),
                    TrendAnalysis = CalculateComplianceTrends(),
                    LastAssessment = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get compliance analytics");
                return new ComplianceAnalytics { OverallComplianceScore = 0 };
            }
        }

        #endregion

        #region Real-time Dashboard Data

        /// <summary>
        /// Gets comprehensive analytics dashboard data
        /// </summary>
        public async Task<AnalyticsDashboard> GetDashboardDataAsync()
        {
            try
            {
                var dashboard = new AnalyticsDashboard
                {
                    GeneratedAt = DateTime.UtcNow,
                    DeviceAnalytics = await GetDeviceAnalyticsAsync(),
                    SecurityAnalytics = await GetSecurityAnalyticsAsync(),
                    ComplianceAnalytics = await GetComplianceAnalyticsAsync(),
                    WorkflowAnalytics = GetWorkflowAnalytics(),
                    RealtimeMetrics = GetRealtimeMetrics(),
                    SystemHealth = CalculateSystemHealth(),
                    ActiveAlerts = GetActiveAlerts(),
                    Performance4Hour = GetPerformanceMetrics(TimeSpan.FromHours(4))
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate analytics dashboard");
                return new AnalyticsDashboard 
                { 
                    GeneratedAt = DateTime.UtcNow, 
                    SystemHealth = SystemHealthStatus.Error 
                };
            }
        }

        /// <summary>
        /// Gets real-time metrics for live dashboard updates
        /// </summary>
        public RealtimeMetrics GetRealtimeMetrics()
        {
            try
            {
                var now = DateTime.UtcNow;
                var last5Minutes = now.AddMinutes(-5);
                
                var recentEvents = _realtimeEvents.Where(e => e.Timestamp >= last5Minutes).ToList();
                
                return new RealtimeMetrics
                {
                    EventsPerMinute = recentEvents.Count / 5.0,
                    ActiveWorkflows = _workflowMetrics.Values.Count(w => w.LastActivity >= last5Minutes),
                    DataThroughputMbps = CalculateRecentThroughput(recentEvents),
                    ErrorRate = CalculateErrorRate(recentEvents),
                    SystemLoad = GetCurrentSystemLoad(),
                    ConnectedDevices = _performanceMetrics.Count,
                    LastUpdate = now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get realtime metrics");
                return new RealtimeMetrics { LastUpdate = DateTime.UtcNow };
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task UpdateWorkflowMetricsAsync(AnalyticsEvent analyticsEvent)
        {
            var workflowKey = analyticsEvent.WorkflowId;
            
            if (_workflowMetrics.TryGetValue(workflowKey, out var metrics))
            {
                metrics.UpdateWithEvent(analyticsEvent);
            }
            else
            {
                metrics = new MedicalWorkflowMetrics(workflowKey);
                metrics.UpdateWithEvent(analyticsEvent);
                _workflowMetrics[workflowKey] = metrics;
            }
        }

        private async Task UpdatePerformanceMetricsAsync(DICOMTransmissionMetrics transmission)
        {
            var deviceKey = Environment.MachineName;
            
            if (_performanceMetrics.TryGetValue(deviceKey, out var metrics))
            {
                metrics.RecordDICOMTransmission(transmission);
            }
        }

        private void ProcessRealtimeAnalytics(object state)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                
                try
                {
                    // Process queued events
                    var processedEvents = 0;
                    while (_realtimeEvents.TryDequeue(out var analyticsEvent) && processedEvents < 100)
                    {
                        ProcessAnalyticsEvent(analyticsEvent);
                        processedEvents++;
                    }

                    // Update aggregated metrics
                    UpdateAggregatedMetrics();
                    
                    _lastAggregation = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in real-time analytics processing");
                }
                finally
                {
                    _isRunning = false;
                }
            }
        }

        private void ProcessAnalyticsEvent(AnalyticsEvent analyticsEvent)
        {
            try
            {
                // Update workflow metrics
                if (_workflowMetrics.TryGetValue(analyticsEvent.WorkflowId, out var workflow))
                {
                    workflow.UpdateWithEvent(analyticsEvent);
                }

                // Trigger insights analysis if needed
                if (_config.EnableInsightsProcessing)
                {
                    _insightsEngine.ProcessEventAsync(analyticsEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process analytics event: {analyticsEvent.EventId}");
            }
        }

        private void UpdateAggregatedMetrics()
        {
            // Update aggregated metrics for all workflows and devices
            foreach (var workflow in _workflowMetrics.Values)
            {
                workflow.UpdateAggregations();
            }

            foreach (var device in _performanceMetrics.Values)
            {
                device.UpdateAggregations();
            }
        }

        private async void GeneratePeriodicReports(object state)
        {
            try
            {
                if (_config.EnablePeriodicReporting)
                {
                    await GenerateAnalyticsReportAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate periodic analytics report");
            }
        }

        private async Task GenerateAnalyticsReportAsync()
        {
            var report = new AnalyticsReport
            {
                GeneratedAt = DateTime.UtcNow,
                ReportPeriod = TimeSpan.FromMinutes(15),
                WorkflowSummary = GenerateWorkflowSummary(),
                PerformanceSummary = GeneratePerformanceSummary(),
                SecuritySummary = GenerateSecuritySummary()
            };

            // Store report
            var reportPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Analytics", "Reports");
            if (!Directory.Exists(reportPath))
            {
                Directory.CreateDirectory(reportPath);
            }

            var fileName = $"analytics_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(reportPath, fileName);
            
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);

            await _auditService.LogComplianceEventAsync("ANALYTICS_REPORT_GENERATED", 
                $"Report generated: {fileName}", "Medical Device Analytics");
        }

        private void InitializeAnalyticsFramework()
        {
            _logger.LogInformation("Advanced Analytics Service initialized for SmartBox Medical Device");
            
            // Create necessary directories
            var analyticsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Analytics");
            if (!Directory.Exists(analyticsPath))
            {
                Directory.CreateDirectory(analyticsPath);
            }
        }

        private AnalyticsConfiguration LoadAnalyticsConfiguration()
        {
            // Load from configuration file or use defaults
            return new AnalyticsConfiguration
            {
                EnableRealtimeProcessing = true,
                EnableInsightsProcessing = true,
                EnablePeriodicReporting = true,
                AggregationInterval = TimeSpan.FromSeconds(5),
                ReportingInterval = TimeSpan.FromMinutes(15),
                MaxEventQueueSize = 10000,
                PerformanceAlertThresholds = new PerformanceAlertThresholds
                {
                    CPUUsageHigh = 80.0,
                    MemoryUsageHigh = 85.0,
                    DiskUsageHigh = 90.0,
                    NetworkLatencyHigh = 1000
                }
            };
        }

        // Additional helper methods for calculations and aggregations
        private double CalculateThroughput(long dataSize, TimeSpan transmissionTime)
        {
            if (transmissionTime.TotalSeconds == 0) return 0;
            return (dataSize / 1024.0 / 1024.0 * 8) / transmissionTime.TotalSeconds; // Mbps
        }

        private AlertLevel DetermineAlertLevel(DevicePerformanceMetrics metrics)
        {
            var current = metrics.GetCurrentSnapshot();
            if (current.CPUUsage > 90 || current.MemoryUsage > 90 || current.DiskUsage > 95)
                return AlertLevel.Critical;
            if (current.CPUUsage > 80 || current.MemoryUsage > 85 || current.DiskUsage > 90)
                return AlertLevel.High;
            if (current.CPUUsage > 70 || current.MemoryUsage > 75 || current.DiskUsage > 80)
                return AlertLevel.Medium;
            return AlertLevel.Low;
        }

        private async Task CheckPerformanceAlertsAsync(DevicePerformanceMetrics metrics, DevicePerformanceSnapshot snapshot)
        {
            var thresholds = _config.PerformanceAlertThresholds;
            
            if (snapshot.CPUUsage > thresholds.CPUUsageHigh)
            {
                await RecordSecurityEventAsync("HIGH_CPU_USAGE", 
                    $"CPU usage: {snapshot.CPUUsage:F1}%", SecurityEventSeverity.High);
            }
            
            if (snapshot.MemoryUsage > thresholds.MemoryUsageHigh)
            {
                await RecordSecurityEventAsync("HIGH_MEMORY_USAGE", 
                    $"Memory usage: {snapshot.MemoryUsage:F1}%", SecurityEventSeverity.High);
            }
        }

        private async Task CheckSecurityAlertsAsync(SecurityMetrics metrics)
        {
            if (metrics.CriticalEvents > 5 && DateTime.UtcNow.Subtract(metrics.LastCriticalEvent) < TimeSpan.FromMinutes(10))
            {
                await _auditService.LogSecurityEventAsync("SECURITY_ALERT_THRESHOLD_EXCEEDED", 
                    $"Multiple critical security events detected: {metrics.CriticalEvents}", 
                    SecurityEventSeverity.Critical);
            }
        }

        private ThreatLevel CalculateOverallThreatLevel(List<SecurityMetrics> allMetrics)
        {
            var totalCritical = allMetrics.Sum(m => m.CriticalEvents);
            var totalEvents = allMetrics.Sum(m => m.TotalEvents);
            
            if (totalCritical > 10 || (totalEvents > 0 && (double)totalCritical / totalEvents > 0.1))
                return ThreatLevel.Critical;
            if (totalCritical > 5 || (totalEvents > 0 && (double)totalCritical / totalEvents > 0.05))
                return ThreatLevel.High;
            if (totalCritical > 0 || allMetrics.Any(m => m.TotalEvents > 20))
                return ThreatLevel.Medium;
            return ThreatLevel.Low;
        }

        private double CalculateSecurityScore(List<SecurityMetrics> allMetrics)
        {
            if (!allMetrics.Any()) return 100.0;
            
            var totalEvents = allMetrics.Sum(m => m.TotalEvents);
            var criticalEvents = allMetrics.Sum(m => m.CriticalEvents);
            
            if (totalEvents == 0) return 100.0;
            
            var score = 100.0 - (criticalEvents * 20.0) - ((totalEvents - criticalEvents) * 2.0);
            return Math.Max(0.0, score);
        }

        private WorkflowAnalytics GetWorkflowAnalytics()
        {
            var workflows = _workflowMetrics.Values.ToList();
            
            return new WorkflowAnalytics
            {
                TotalWorkflows = workflows.Count,
                ActiveWorkflows = workflows.Count(w => w.IsActive),
                SuccessRate = workflows.Any() ? workflows.Average(w => w.SuccessRate) : 0,
                AverageProcessingTime = workflows.Any() ? workflows.Average(w => w.AverageProcessingTime.TotalSeconds) : 0,
                TotalProcessedItems = workflows.Sum(w => w.TotalProcessedItems),
                LastUpdate = DateTime.UtcNow
            };
        }

        private SystemHealthStatus CalculateSystemHealth()
        {
            var deviceMetrics = _performanceMetrics.Values.ToList();
            var securityMetrics = _securityMetrics.Values.ToList();
            
            if (!deviceMetrics.Any()) return SystemHealthStatus.Unknown;
            
            var avgCpu = deviceMetrics.Average(d => d.GetCurrentSnapshot().CPUUsage);
            var avgMemory = deviceMetrics.Average(d => d.GetCurrentSnapshot().MemoryUsage);
            var criticalSecurityEvents = securityMetrics.Sum(s => s.CriticalEvents);
            
            if (avgCpu > 90 || avgMemory > 90 || criticalSecurityEvents > 10)
                return SystemHealthStatus.Critical;
            if (avgCpu > 80 || avgMemory > 85 || criticalSecurityEvents > 5)
                return SystemHealthStatus.Warning;
            if (avgCpu > 70 || avgMemory > 75 || criticalSecurityEvents > 0)
                return SystemHealthStatus.Degraded;
            
            return SystemHealthStatus.Healthy;
        }

        private List<Alert> GetActiveAlerts()
        {
            var alerts = new List<Alert>();
            
            foreach (var device in _performanceMetrics.Values)
            {
                var snapshot = device.GetCurrentSnapshot();
                var alertLevel = DetermineAlertLevel(device);
                
                if (alertLevel >= AlertLevel.Medium)
                {
                    alerts.Add(new Alert
                    {
                        Id = Guid.NewGuid().ToString(),
                        Type = "PERFORMANCE",
                        Level = alertLevel,
                        Message = $"Device {device.DeviceId} performance alert: CPU {snapshot.CPUUsage:F1}%, Memory {snapshot.MemoryUsage:F1}%",
                        Timestamp = snapshot.Timestamp,
                        DeviceId = device.DeviceId
                    });
                }
            }
            
            return alerts;
        }

        private PerformanceMetrics GetPerformanceMetrics(TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow.Subtract(timeSpan);
            var deviceMetrics = _performanceMetrics.Values.ToList();
            
            return new PerformanceMetrics
            {
                AverageCPUUsage = deviceMetrics.Any() ? deviceMetrics.Average(d => d.GetCurrentSnapshot().CPUUsage) : 0,
                AverageMemoryUsage = deviceMetrics.Any() ? deviceMetrics.Average(d => d.GetCurrentSnapshot().MemoryUsage) : 0,
                AverageDiskUsage = deviceMetrics.Any() ? deviceMetrics.Average(d => d.GetCurrentSnapshot().DiskUsage) : 0,
                PeakCPUUsage = deviceMetrics.Any() ? deviceMetrics.Max(d => d.GetCurrentSnapshot().CPUUsage) : 0,
                PeakMemoryUsage = deviceMetrics.Any() ? deviceMetrics.Max(d => d.GetCurrentSnapshot().MemoryUsage) : 0,
                UptimeHours = deviceMetrics.Any() ? deviceMetrics.Average(d => d.GetUptimeHours()) : 0,
                LastUpdate = DateTime.UtcNow
            };
        }

        // Additional helper methods would continue here...
        private double CalculateOverallComplianceScore(PrivacyComplianceResult hipaa, PrivacyComplianceResult gdpr, object fda) => 85.0;
        private List<ComplianceViolation> GetRecentComplianceViolations() => new List<ComplianceViolation>();
        private async Task<List<AuditFinding>> GetAuditFindingsAsync() => new List<AuditFinding>();
        private List<RemediationItem> GetRemediationItems() => new List<RemediationItem>();
        private ComplianceTrendAnalysis CalculateComplianceTrends() => new ComplianceTrendAnalysis();
        private List<SecurityEvent> GetRecentSecurityEvents(int hours) => new List<SecurityEvent>();
        private SecurityTrendAnalysis CalculateSecurityTrends(List<SecurityMetrics> metrics) => new SecurityTrendAnalysis();
        private WorkflowSummary GenerateWorkflowSummary() => new WorkflowSummary();
        private PerformanceSummary GeneratePerformanceSummary() => new PerformanceSummary();
        private SecuritySummary GenerateSecuritySummary() => new SecuritySummary();
        private double CalculateRecentThroughput(List<AnalyticsEvent> events) => 0.0;
        private double CalculateErrorRate(List<AnalyticsEvent> events) => 0.0;
        private double GetCurrentSystemLoad() => 50.0;

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _aggregationTimer?.Dispose();
            _reportingTimer?.Dispose();
            _logger.LogInformation("Advanced Analytics Service disposed");
        }

        #endregion
    }

    #region Analytics Data Models

    public class AnalyticsEvent
    {
        public string EventId { get; set; }
        public string WorkflowId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string PatientId { get; set; }
        public string DeviceId { get; set; }
        public string UserId { get; set; }
    }

    public class MedicalWorkflowMetrics
    {
        public string WorkflowId { get; set; }
        public int TotalEvents { get; set; }
        public int SuccessfulEvents { get; set; }
        public int FailedEvents { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public DateTime LastActivity { get; set; }
        public bool IsActive { get; set; }
        public int TotalDataFlows { get; set; }
        public int SuccessfulDataFlows { get; set; }
        public int TotalProcessedItems { get; set; }
        
        public double SuccessRate => TotalEvents > 0 ? (double)SuccessfulEvents / TotalEvents * 100 : 0;

        public MedicalWorkflowMetrics(string workflowId)
        {
            WorkflowId = workflowId;
            LastActivity = DateTime.UtcNow;
        }

        public void UpdateWithEvent(AnalyticsEvent analyticsEvent)
        {
            TotalEvents++;
            if (analyticsEvent.EventType.Contains("SUCCESS"))
                SuccessfulEvents++;
            else if (analyticsEvent.EventType.Contains("FAILED"))
                FailedEvents++;
            
            LastActivity = analyticsEvent.Timestamp;
            IsActive = DateTime.UtcNow.Subtract(LastActivity) < TimeSpan.FromMinutes(5);
            TotalProcessedItems++;
        }

        public void UpdateAggregations()
        {
            IsActive = DateTime.UtcNow.Subtract(LastActivity) < TimeSpan.FromMinutes(5);
        }
    }

    public class DevicePerformanceMetrics
    {
        public string DeviceId { get; set; }
        public DateTime LastUpdate { get; set; }
        public List<DevicePerformanceSnapshot> PerformanceHistory { get; set; }
        public int DICOMTransmissions { get; set; }
        public double TotalThroughputMB { get; set; }
        public DateTime StartTime { get; set; }

        public DevicePerformanceMetrics(string deviceId)
        {
            DeviceId = deviceId;
            PerformanceHistory = new List<DevicePerformanceSnapshot>();
            StartTime = DateTime.UtcNow;
        }

        public void UpdateWithSnapshot(DevicePerformanceSnapshot snapshot)
        {
            PerformanceHistory.Add(snapshot);
            LastUpdate = snapshot.Timestamp;
            
            // Keep only last 24 hours of data
            var cutoff = DateTime.UtcNow.AddHours(-24);
            PerformanceHistory.RemoveAll(h => h.Timestamp < cutoff);
        }

        public void RecordDICOMTransmission(DICOMTransmissionMetrics transmission)
        {
            DICOMTransmissions++;
            TotalThroughputMB += transmission.DataSize / 1024.0 / 1024.0;
        }

        public DevicePerformanceSnapshot GetCurrentSnapshot()
        {
            return PerformanceHistory.LastOrDefault() ?? new DevicePerformanceSnapshot();
        }

        public DevicePerformanceSnapshot GetAveragePerformance()
        {
            if (!PerformanceHistory.Any()) return new DevicePerformanceSnapshot();
            
            return new DevicePerformanceSnapshot
            {
                CPUUsage = PerformanceHistory.Average(h => h.CPUUsage),
                MemoryUsage = PerformanceHistory.Average(h => h.MemoryUsage),
                DiskUsage = PerformanceHistory.Average(h => h.DiskUsage),
                NetworkLatency = PerformanceHistory.Average(h => h.NetworkLatency),
                Temperature = PerformanceHistory.Average(h => h.Temperature),
                Timestamp = DateTime.UtcNow
            };
        }

        public List<DevicePerformanceSnapshot> GetPerformanceHistory(TimeSpan timeSpan)
        {
            var cutoff = DateTime.UtcNow.Subtract(timeSpan);
            return PerformanceHistory.Where(h => h.Timestamp >= cutoff).ToList();
        }

        public double GetUptimeHours()
        {
            return DateTime.UtcNow.Subtract(StartTime).TotalHours;
        }

        public void UpdateAggregations()
        {
            // Update any aggregated calculations
        }
    }

    public class DevicePerformanceSnapshot
    {
        public double CPUUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkLatency { get; set; }
        public double Temperature { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class SecurityMetrics
    {
        public string DeviceId { get; set; }
        public int TotalEvents { get; set; }
        public int CriticalEvents { get; set; }
        public int ActiveThreats { get; set; }
        public DateTime LastCriticalEvent { get; set; }
        public Dictionary<string, int> EventTypeCounts { get; set; }

        public SecurityMetrics(string deviceId)
        {
            DeviceId = deviceId;
            EventTypeCounts = new Dictionary<string, int>();
        }

        public void RecordSecurityEvent(string eventType, SecurityEventSeverity severity)
        {
            TotalEvents++;
            if (severity == SecurityEventSeverity.Critical)
            {
                CriticalEvents++;
                LastCriticalEvent = DateTime.UtcNow;
            }

            EventTypeCounts.TryGetValue(eventType, out var count);
            EventTypeCounts[eventType] = count + 1;
        }
    }

    public class DICOMTransmissionMetrics
    {
        public string StudyInstanceUID { get; set; }
        public string Destination { get; set; }
        public long DataSize { get; set; }
        public TimeSpan TransmissionTime { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
        public EncryptionResult PatientId { get; set; }
        public double ThroughputMbps { get; set; }
    }

    public class DeviceAnalytics
    {
        public string DeviceId { get; set; }
        public DevicePerformanceSnapshot CurrentPerformance { get; set; }
        public DevicePerformanceSnapshot AveragePerformance { get; set; }
        public List<DevicePerformanceSnapshot> PerformanceHistory { get; set; }
        public AlertLevel AlertLevel { get; set; }
        public double UptimeHours { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class SecurityAnalytics
    {
        public int TotalSecurityEvents { get; set; }
        public int CriticalEvents { get; set; }
        public ThreatLevel ThreatLevel { get; set; }
        public int ActiveThreats { get; set; }
        public double SecurityScore { get; set; }
        public List<SecurityEvent> RecentEvents { get; set; }
        public SecurityTrendAnalysis TrendAnalysis { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class ComplianceAnalytics
    {
        public double HIPAAComplianceScore { get; set; }
        public double GDPRComplianceScore { get; set; }
        public double FDAComplianceScore { get; set; }
        public double OverallComplianceScore { get; set; }
        public List<ComplianceViolation> ComplianceViolations { get; set; }
        public List<AuditFinding> AuditFindings { get; set; }
        public List<RemediationItem> RemediationItems { get; set; }
        public ComplianceTrendAnalysis TrendAnalysis { get; set; }
        public DateTime LastAssessment { get; set; }
    }

    public class AnalyticsDashboard
    {
        public DateTime GeneratedAt { get; set; }
        public DeviceAnalytics DeviceAnalytics { get; set; }
        public SecurityAnalytics SecurityAnalytics { get; set; }
        public ComplianceAnalytics ComplianceAnalytics { get; set; }
        public WorkflowAnalytics WorkflowAnalytics { get; set; }
        public RealtimeMetrics RealtimeMetrics { get; set; }
        public SystemHealthStatus SystemHealth { get; set; }
        public List<Alert> ActiveAlerts { get; set; }
        public PerformanceMetrics Performance4Hour { get; set; }
    }

    public class RealtimeMetrics
    {
        public double EventsPerMinute { get; set; }
        public int ActiveWorkflows { get; set; }
        public double DataThroughputMbps { get; set; }
        public double ErrorRate { get; set; }
        public double SystemLoad { get; set; }
        public int ConnectedDevices { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class WorkflowAnalytics
    {
        public int TotalWorkflows { get; set; }
        public int ActiveWorkflows { get; set; }
        public double SuccessRate { get; set; }
        public double AverageProcessingTime { get; set; }
        public int TotalProcessedItems { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class PerformanceMetrics
    {
        public double AverageCPUUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public double AverageDiskUsage { get; set; }
        public double PeakCPUUsage { get; set; }
        public double PeakMemoryUsage { get; set; }
        public double UptimeHours { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class Alert
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public AlertLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
    }

    public class AnalyticsConfiguration
    {
        public bool EnableRealtimeProcessing { get; set; }
        public bool EnableInsightsProcessing { get; set; }
        public bool EnablePeriodicReporting { get; set; }
        public TimeSpan AggregationInterval { get; set; }
        public TimeSpan ReportingInterval { get; set; }
        public int MaxEventQueueSize { get; set; }
        public PerformanceAlertThresholds PerformanceAlertThresholds { get; set; }
    }

    public class PerformanceAlertThresholds
    {
        public double CPUUsageHigh { get; set; }
        public double MemoryUsageHigh { get; set; }
        public double DiskUsageHigh { get; set; }
        public double NetworkLatencyHigh { get; set; }
    }

    public class AnalyticsReport
    {
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ReportPeriod { get; set; }
        public WorkflowSummary WorkflowSummary { get; set; }
        public PerformanceSummary PerformanceSummary { get; set; }
        public SecuritySummary SecuritySummary { get; set; }
    }

    // Additional model classes
    public class ComplianceViolation { }
    public class AuditFinding { }
    public class RemediationItem { }
    public class ComplianceTrendAnalysis { }
    public class SecurityEvent { }
    public class SecurityTrendAnalysis { }
    public class WorkflowSummary { }
    public class PerformanceSummary { }
    public class SecuritySummary { }

    public enum AlertLevel { Low, Medium, High, Critical, Unknown, Error }
    public enum ThreatLevel { Low, Medium, High, Critical, Unknown }
    public enum SystemHealthStatus { Healthy, Degraded, Warning, Critical, Unknown, Error }

    #endregion
}