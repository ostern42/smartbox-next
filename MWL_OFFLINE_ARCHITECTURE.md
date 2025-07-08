# SmartBox MWL & Offline Architecture

## 🚨 CORE PRINCIPLE: NEVER LOSE DATA!

### 1. MWL Caching System

```csharp
public class MwlCacheService
{
    private const string CACHE_FILE = "./Data/Cache/mwl_cache.json";
    
    public async Task<List<WorklistItem>> GetWorklistAsync()
    {
        // 1. Try online query
        if (IsOnline())
        {
            var freshData = await QueryMwlServer();
            await SaveCache(freshData);
            return freshData;
        }
        
        // 2. Fall back to cache
        return LoadCache();
    }
    
    private async Task SaveCache(List<WorklistItem> items)
    {
        var cache = new MwlCache
        {
            LastUpdate = DateTime.Now,
            Items = items
        };
        
        // Atomic write with temp file
        var json = JsonSerializer.Serialize(cache);
        var tempFile = CACHE_FILE + ".tmp";
        await File.WriteAllTextAsync(tempFile, json);
        File.Move(tempFile, CACHE_FILE, true);
    }
}
```

### 2. StudyInstanceUID Management

```csharp
public class WorklistItem
{
    public string StudyInstanceUID { get; set; } // FROM MWL!
    public string AccessionNumber { get; set; }
    public string PatientId { get; set; }
    // ... other fields
}

// When capturing:
public async Task CaptureImage(WorklistItem selectedPatient)
{
    var dicom = new DicomFile();
    
    // CRITICAL: Use StudyInstanceUID from MWL!
    dicom.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, 
        selectedPatient.StudyInstanceUID);
    
    // This ensures all images belong to same study
}
```

### 3. Multi-Target Upload Architecture

```json
{
  "UploadTargets": [
    {
      "Id": "primary-pacs",
      "Type": "DICOM",
      "Name": "Haupt-PACS",
      "Config": {
        "Host": "pacs.klinik.local",
        "Port": 4242,
        "AET": "HAUPTPACS",
        "CallingAET": "SMARTBOX"
      },
      "Priority": 1,
      "Rules": {
        "Include": ["*"],
        "RetryCount": 3,
        "RetryDelaySeconds": 60
      }
    },
    {
      "Id": "backup-pacs",
      "Type": "DICOM",
      "Name": "Backup-PACS",
      "Config": {
        "Host": "backup.klinik.local",
        "Port": 4242,
        "AET": "BACKUP"
      },
      "Priority": 2,
      "Rules": {
        "Include": ["*"],
        "OnlyWhen": "primary-pacs:failed"
      }
    },
    {
      "Id": "emergency-ftp",
      "Type": "FTP",
      "Name": "Notfall-FTP",
      "Config": {
        "Host": "ftp.klinik.local",
        "Username": "smartbox",
        "Password": "encrypted:...",
        "Path": "/emergency/dicom/"
      },
      "Priority": 3,
      "Rules": {
        "Include": ["Emergency:*", "NOTFALL:*"],
        "OnlyWhen": "all-dicom:failed"
      }
    },
    {
      "Id": "local-share",
      "Type": "FileShare",
      "Name": "Lokale Sicherung",
      "Config": {
        "Path": "\\\\nas\\dicom-backup\\smartbox",
        "CreateDateFolders": true
      },
      "Priority": 4,
      "Rules": {
        "Include": ["*"],
        "Always": true
      }
    }
  ]
}
```

### 4. Queue Manager Enhancement

```csharp
public class EnhancedQueueManager
{
    private readonly List<IUploadTarget> _targets;
    
    public async Task ProcessQueueItem(QueueItem item)
    {
        var results = new Dictionary<string, UploadResult>();
        
        // Try each target based on rules
        foreach (var target in _targets.OrderBy(t => t.Priority))
        {
            if (!ShouldUploadToTarget(item, target, results))
                continue;
                
            try
            {
                await target.UploadAsync(item);
                results[target.Id] = UploadResult.Success;
                
                // Check if we should continue to next target
                if (!target.Rules.Contains("Always"))
                    break;
            }
            catch (Exception ex)
            {
                results[target.Id] = UploadResult.Failed;
                _logger.LogError($"Upload to {target.Name} failed: {ex.Message}");
            }
        }
        
        // Update queue item status
        if (results.Any(r => r.Value == UploadResult.Success))
        {
            item.Status = QueueStatus.Completed;
        }
        else
        {
            item.RetryCount++;
            item.NextRetry = DateTime.Now.AddMinutes(Math.Pow(2, item.RetryCount));
        }
    }
}
```

### 5. Offline Workflow

```
1. SmartBox Start
   ↓
2. Load MWL Cache (./Data/Cache/mwl_cache.json)
   ↓
3. Show cached patients (with age indicator)
   ↓
4. User selects patient
   ↓
5. Capture with MWL StudyInstanceUID
   ↓
6. Queue for upload (./Data/Queue/)
   ↓
7. Background: Try uploads based on rules
   ↓
8. On success: Mark complete
   On fail: Exponential backoff retry
```

### 6. Emergency Scenarios

#### A) Network Completely Down
- Work from MWL cache
- Queue everything locally
- Auto-upload when network returns

#### B) Primary PACS Down
- Automatic failover to backup
- FTP for emergency cases
- Local share always gets copy

#### C) Power Loss During Capture
- Queue file written atomically
- On restart: Resume from queue
- No data loss!

#### D) Corrupted Cache
- Detect on load (JSON parse error)
- Fall back to manual entry
- Log error for admin

### 7. Implementation Priority

1. **Phase 1**: MWL Cache (DONE in planning)
2. **Phase 2**: Multi-target config
3. **Phase 3**: Enhanced queue manager
4. **Phase 4**: FTP/FileShare targets
5. **Phase 5**: Rule engine

### 8. Testing Scenarios

- [ ] Start offline → Select from cache
- [ ] Capture offline → Queue builds up
- [ ] Network returns → Auto-upload
- [ ] Primary PACS down → Backup works
- [ ] Power loss → Queue survives
- [ ] Cache corruption → Graceful fallback

## 🎯 KEY TAKEAWAY

**"The SmartBox MUST work in a disaster. When everything else fails, we still capture and eventually deliver."**