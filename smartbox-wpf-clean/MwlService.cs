using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;

namespace SmartBoxNext
{
    /// <summary>
    /// Service for querying and caching DICOM Modality Worklist
    /// </summary>
    public class MwlService
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;
        private readonly string _cacheFilePath;
        private MwlCache _cache;
        private readonly object _cacheLock = new object();

        public MwlService(AppConfig config, Logger logger)
        {
            _config = config;
            _logger = logger;
            
            // Ensure cache directory exists
            var cacheDir = Path.Combine(_config.StoragePath, "Cache");
            Directory.CreateDirectory(cacheDir);
            
            _cacheFilePath = Path.Combine(cacheDir, "mwl_cache.json");
            
            // Load cache on startup
            LoadCache();
        }

        /// <summary>
        /// Get worklist items - from server if online, from cache if offline
        /// </summary>
        public async Task<List<WorklistItem>> GetWorklistAsync(DateTime? date = null)
        {
            try
            {
                // Try to query server if MWL is enabled
                if (_config.MwlSettings?.EnableWorklist == true)
                {
                    var items = await QueryWorklistFromServerAsync(date ?? DateTime.Today);
                    if (items != null && items.Any())
                    {
                        // Update cache with fresh data
                        UpdateCache(items, false);
                        return items;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"MWL query failed, using cache: {ex.Message}");
            }

            // Fall back to cache
            return GetCachedItems(date);
        }

        /// <summary>
        /// Query worklist from DICOM server
        /// </summary>
        private async Task<List<WorklistItem>> QueryWorklistFromServerAsync(DateTime date)
        {
            if (_config.MwlSettings == null || !_config.MwlSettings.EnableWorklist)
                return null;

            var items = new List<WorklistItem>();

            try
            {
                var client = DicomClientFactory.Create(
                    _config.MwlSettings.MwlServerHost,
                    _config.MwlSettings.MwlServerPort,
                    false, // TLS
                    _config.LocalAET ?? "SMARTBOX",
                    _config.MwlSettings.MwlServerAET ?? "ORTHANC"
                );

                // Create C-FIND request for Modality Worklist
                var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Worklist);

                // Add query parameters
                request.Dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepSequence, new DicomDataset
                {
                    { DicomTag.Modality, "CR" }, // Could be configurable
                    { DicomTag.ScheduledProcedureStepStartDate, date.ToString("yyyyMMdd") },
                    { DicomTag.ScheduledProcedureStepStartTime, "" }
                });

                // Request all relevant fields
                request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
                request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
                request.Dataset.AddOrUpdate(DicomTag.PatientBirthDate, "");
                request.Dataset.AddOrUpdate(DicomTag.PatientSex, "");
                request.Dataset.AddOrUpdate(DicomTag.AccessionNumber, "");
                request.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, "");
                request.Dataset.AddOrUpdate(DicomTag.StudyDescription, "");
                request.Dataset.AddOrUpdate(DicomTag.RequestedProcedureDescription, "");

                // Handle responses
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Pending && response.HasDataset)
                    {
                        try
                        {
                            var item = ParseWorklistResponse(response.Dataset);
                            if (item != null)
                            {
                                items.Add(item);
                                _logger.LogInfo($"MWL: Found patient {item.PatientName} ({item.PatientId})");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error parsing MWL response: {ex.Message}");
                        }
                    }
                };

                await client.AddRequestAsync(request);
                await client.SendAsync();

                _logger.LogInfo($"MWL query completed: {items.Count} items found");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError($"MWL query error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Parse DICOM dataset into WorklistItem
        /// </summary>
        private WorklistItem ParseWorklistResponse(DicomDataset dataset)
        {
            var item = new WorklistItem();

            // Critical: Get StudyInstanceUID from MWL
            item.StudyInstanceUID = dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, "");
            if (string.IsNullOrEmpty(item.StudyInstanceUID))
            {
                _logger.LogWarning("MWL response missing StudyInstanceUID!");
            }

            // Patient demographics
            item.PatientId = dataset.GetSingleValueOrDefault(DicomTag.PatientID, "");
            item.PatientName = dataset.GetSingleValueOrDefault(DicomTag.PatientName, "");
            
            var birthDateStr = dataset.GetSingleValueOrDefault(DicomTag.PatientBirthDate, "");
            if (!string.IsNullOrEmpty(birthDateStr) && birthDateStr.Length == 8)
            {
                if (DateTime.TryParseExact(birthDateStr, "yyyyMMdd", null, 
                    System.Globalization.DateTimeStyles.None, out var birthDate))
                {
                    item.BirthDate = birthDate;
                }
            }
            
            item.Sex = dataset.GetSingleValueOrDefault(DicomTag.PatientSex, "");

            // Study information
            item.AccessionNumber = dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, "");
            item.StudyDescription = dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, "");
            item.RequestedProcedureDescription = dataset.GetSingleValueOrDefault(DicomTag.RequestedProcedureDescription, "");

            // Scheduled Procedure Step (may be in sequence)
            var spsSequence = dataset.GetSequence(DicomTag.ScheduledProcedureStepSequence);
            if (spsSequence != null && spsSequence.Any())
            {
                var sps = spsSequence.First();
                
                var dateStr = sps.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepStartDate, "");
                if (!string.IsNullOrEmpty(dateStr) && dateStr.Length == 8)
                {
                    if (DateTime.TryParseExact(dateStr, "yyyyMMdd", null,
                        System.Globalization.DateTimeStyles.None, out var schedDate))
                    {
                        item.ScheduledDate = schedDate;
                    }
                }

                var timeStr = sps.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepStartTime, "");
                if (!string.IsNullOrEmpty(timeStr) && timeStr.Length >= 4)
                {
                    if (TimeSpan.TryParseExact(timeStr.Substring(0, 4), @"hhmm", null, out var schedTime))
                    {
                        item.ScheduledTime = schedTime;
                    }
                }

                item.Modality = sps.GetSingleValueOrDefault(DicomTag.Modality, "CR");
                item.ScheduledStationAET = sps.GetSingleValueOrDefault(DicomTag.ScheduledStationAETitle, "");
                item.ScheduledPerformingPhysician = sps.GetSingleValueOrDefault(DicomTag.ScheduledPerformingPhysicianName, "");
                item.ScheduledProcedureStepDescription = sps.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepDescription, "");
                item.ScheduledProcedureStepId = sps.GetSingleValueOrDefault(DicomTag.ScheduledProcedureStepID, "");
            }

            return item;
        }

        /// <summary>
        /// Update cache with new items
        /// </summary>
        private void UpdateCache(List<WorklistItem> items, bool isOffline)
        {
            lock (_cacheLock)
            {
                _cache = new MwlCache
                {
                    LastUpdate = DateTime.Now,
                    Items = items,
                    ServerInfo = $"{_config.MwlSettings?.MwlServerHost}:{_config.MwlSettings?.MwlServerPort}",
                    IsOffline = isOffline
                };

                SaveCache();
            }
        }

        /// <summary>
        /// Save cache to disk (atomic write)
        /// </summary>
        private void SaveCache()
        {
            try
            {
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Atomic write with temp file
                var tempFile = _cacheFilePath + ".tmp";
                File.WriteAllText(tempFile, json);
                File.Move(tempFile, _cacheFilePath, true);

                _logger.LogInfo($"MWL cache saved: {_cache.Items.Count} items");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save MWL cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Load cache from disk
        /// </summary>
        private void LoadCache()
        {
            try
            {
                if (File.Exists(_cacheFilePath))
                {
                    var json = File.ReadAllText(_cacheFilePath);
                    _cache = JsonSerializer.Deserialize<MwlCache>(json);
                    _logger.LogInfo($"MWL cache loaded: {_cache?.Items?.Count ?? 0} items, age: {_cache?.Age}");
                }
                else
                {
                    _cache = new MwlCache();
                    _logger.LogInfo("No MWL cache found, starting fresh");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load MWL cache: {ex.Message}");
                _cache = new MwlCache();
            }
        }

        /// <summary>
        /// Get items from cache, optionally filtered by date
        /// </summary>
        private List<WorklistItem> GetCachedItems(DateTime? date)
        {
            lock (_cacheLock)
            {
                if (_cache?.Items == null) return new List<WorklistItem>();

                var items = _cache.Items;
                
                // Filter by date if specified
                if (date.HasValue)
                {
                    items = items.Where(i => i.ScheduledDate.Date == date.Value.Date).ToList();
                }

                // Sort: Emergency first, then by time
                return items
                    .OrderByDescending(i => i.IsEmergency)
                    .ThenBy(i => i.ScheduledTime)
                    .ToList();
            }
        }

        /// <summary>
        /// Force refresh from server
        /// </summary>
        public async Task<bool> RefreshCacheAsync()
        {
            try
            {
                var items = await QueryWorklistFromServerAsync(DateTime.Today);
                if (items != null)
                {
                    UpdateCache(items, false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cache refresh failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Get cache status for UI
        /// </summary>
        public MwlCacheStatus GetCacheStatus()
        {
            lock (_cacheLock)
            {
                return new MwlCacheStatus
                {
                    ItemCount = _cache?.Items?.Count ?? 0,
                    LastUpdate = _cache?.LastUpdate ?? DateTime.MinValue,
                    Age = _cache?.Age ?? TimeSpan.MaxValue,
                    IsStale = _cache?.IsStale ?? true,
                    IsOffline = _cache?.IsOffline ?? true,
                    ServerInfo = _cache?.ServerInfo ?? "Not configured"
                };
            }
        }
    }

    /// <summary>
    /// Cache status for UI display
    /// </summary>
    public class MwlCacheStatus
    {
        public int ItemCount { get; set; }
        public DateTime LastUpdate { get; set; }
        public TimeSpan Age { get; set; }
        public bool IsStale { get; set; }
        public bool IsOffline { get; set; }
        public string ServerInfo { get; set; }
    }
}