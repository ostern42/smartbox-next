using System;
using System.Collections.Generic;

namespace SmartBoxNext
{
    /// <summary>
    /// Cache container for MWL data
    /// </summary>
    public class MwlCache
    {
        public DateTime LastUpdate { get; set; } = DateTime.MinValue;
        public List<WorklistItem> Items { get; set; } = new List<WorklistItem>();
        public string ServerInfo { get; set; } = "";
        public bool IsOffline { get; set; } = true;

        /// <summary>
        /// Get cache age
        /// </summary>
        public TimeSpan Age => DateTime.Now - LastUpdate;

        /// <summary>
        /// Check if cache is stale (older than 24 hours)
        /// </summary>
        public bool IsStale => Age.TotalHours > 24;
    }
}