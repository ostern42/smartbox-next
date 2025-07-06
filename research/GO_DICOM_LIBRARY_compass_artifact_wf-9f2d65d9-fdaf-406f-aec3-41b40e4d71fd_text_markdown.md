# Go DICOM Libraries for C-STORE Implementation in SmartBox-Next

## Executive Summary

After extensive research, **there is currently no actively maintained Go library that provides traditional DICOM networking operations (C-STORE, C-ECHO, C-FIND, C-MOVE)**. The most viable option is **kristianvalind/go-netdicom-port**, an active fork of the archived grailbio/go-netdicom library. Alternatively, consider using DICOMweb protocols with **toastcheng/dicomweb-go** if your PACS servers support REST APIs.

## Library Analysis

### 1. suyashkumar/dicom ❌ No Networking
- **Features**: High-performance DICOM file parsing only
- **Networking**: NO support for C-STORE, C-ECHO, C-FIND, C-MOVE
- **License**: MIT (commercial-friendly)
- **Maintenance**: Very active (v1.0+, 994 stars)
- **Windows**: ✅ Full support
- **Character Encoding**: ✅ UTF-8 and ISO-8859-1 support
- **Performance**: ✅ Streaming capabilities, memory-efficient
- **Verdict**: Excellent for file parsing, but cannot handle networking

### 2. grailbio/go-dicom & go-netdicom ⚠️ Archived
- **Status**: ARCHIVED November 2021, no longer maintained
- **Features**: go-netdicom had complete C-STORE, C-FIND, C-GET implementation
- **Quality**: Production-tested with pynetdicom and OsiriX
- **Verdict**: Do not use for new projects despite good implementation

### 3. gradienthealth/dicom ❌ No Networking
- **Features**: Fork of gillesdemey/go-dicom (not suyashkumar's)
- **Networking**: NO support
- **Maintenance**: Less active than suyashkumar/dicom
- **Verdict**: Not suitable for C-STORE requirements

### 4. cylab/dicom ❌ Does Not Exist
- **Finding**: No Go library exists under this name
- **Note**: cylab-tw provides JavaScript/Node.js DICOM solutions, not Go

### 5. kristianvalind/go-netdicom-port ✅ Best Option
- **Repository**: https://github.com/kristianvalind/go-netdicom-port
- **Features**: Active fork of grailbio/go-netdicom
- **Networking**: C-STORE, C-ECHO, C-FIND, C-GET support
- **Maintenance**: Active development, updated to use suyashkumar/dicom
- **License**: Apache 2.0 (commercial-friendly)
- **Verdict**: **RECOMMENDED** for traditional DICOM networking

### 6. toastcheng/dicomweb-go 🔄 Alternative Approach
- **Features**: DICOMweb client (REST-based, not traditional DIMSE)
- **Protocols**: QIDO-RS, WADO-RS, STOW-RS
- **License**: MIT
- **Maintenance**: Active
- **Verdict**: Good alternative if PACS supports DICOMweb

## Feature Comparison Matrix

| Library | C-STORE | C-ECHO | C-FIND | C-MOVE | License | Active | Windows | German Chars |
|---------|---------|---------|---------|---------|---------|---------|---------|--------------|
| kristianvalind/go-netdicom-port | ✅ | ✅ | ✅ | ❌ | Apache 2.0 | ✅ | ✅ | ✅ |
| suyashkumar/dicom | ❌ | ❌ | ❌ | ❌ | MIT | ✅ | ✅ | ✅ |
| toastcheng/dicomweb-go | 🔄 STOW-RS | ❌ | 🔄 QIDO-RS | ❌ | MIT | ✅ | ✅ | ✅ |
| grailbio/go-netdicom | ✅ | ✅ | ✅ | ❌ | Apache 2.0 | ❌ | ✅ | ✅ |

## Recommended Implementation Approach

### Primary Recommendation: kristianvalind/go-netdicom-port

```go
package pacs

import (
    "context"
    "fmt"
    "log"
    "time"
    
    "github.com/kristianvalind/go-netdicom-port/netdicom"
    "github.com/kristianvalind/go-netdicom-port/sopclass"
    "github.com/suyashkumar/dicom"
)

type StoreService struct {
    callingAE   string
    calledAE    string
    serverAddr  string
    timeout     time.Duration
    maxRetries  int
}

func NewStoreService(callingAE, calledAE, serverAddr string) *StoreService {
    return &StoreService{
        callingAE:  callingAE,
        calledAE:   calledAE,
        serverAddr: serverAddr,
        timeout:    30 * time.Second,
        maxRetries: 3,
    }
}

// C-STORE implementation with retry logic
func (s *StoreService) Store(ctx context.Context, filePath string, progress func(int, int)) error {
    var lastErr error
    
    for attempt := 1; attempt <= s.maxRetries; attempt++ {
        err := s.performStore(ctx, filePath, progress)
        if err == nil {
            return nil
        }
        
        lastErr = err
        if attempt < s.maxRetries {
            time.Sleep(time.Duration(attempt) * time.Second)
        }
    }
    
    return fmt.Errorf("C-STORE failed after %d attempts: %w", s.maxRetries, lastErr)
}

func (s *StoreService) performStore(ctx context.Context, filePath string, progress func(int, int)) error {
    // Create service user with transfer syntax negotiation
    user, err := netdicom.NewServiceUser(netdicom.ServiceUserParams{
        CallingAETitle: s.callingAE,
        CalledAETitle:  s.calledAE,
        SOPClasses:     sopclass.StorageClasses,
        TransferSyntaxes: []string{
            "1.2.840.10008.1.2.1",    // Explicit VR Little Endian
            "1.2.840.10008.1.2",      // Implicit VR Little Endian
            "1.2.840.10008.1.2.4.50", // JPEG Baseline
        },
    })
    if err != nil {
        return fmt.Errorf("failed to create service user: %w", err)
    }
    defer user.Release()

    // Connect with timeout
    if err := user.ConnectContext(ctx, s.serverAddr); err != nil {
        return fmt.Errorf("connection failed: %w", err)
    }

    // Progress callback
    if progress != nil {
        progress(1, 3)
    }

    // Read DICOM file
    dataset, err := dicom.ParseFile(filePath, nil)
    if err != nil {
        return fmt.Errorf("failed to parse DICOM file: %w", err)
    }

    if progress != nil {
        progress(2, 3)
    }

    // Send C-STORE
    if err := user.CStore(dataset); err != nil {
        return fmt.Errorf("C-STORE failed: %w", err)
    }

    if progress != nil {
        progress(3, 3)
    }

    return nil
}

// C-ECHO for connection testing
func (s *StoreService) Echo(ctx context.Context) error {
    user, err := netdicom.NewServiceUser(netdicom.ServiceUserParams{
        CallingAETitle: s.callingAE,
        CalledAETitle:  s.calledAE,
        SOPClasses:     sopclass.VerificationClasses,
    })
    if err != nil {
        return err
    }
    defer user.Release()

    if err := user.ConnectContext(ctx, s.serverAddr); err != nil {
        return err
    }

    return user.CEcho()
}
```

### Alternative: DICOMweb Approach

```go
package pacs

import (
    "context"
    "github.com/toastcheng/dicomweb-go/dicomweb"
)

type DICOMWebService struct {
    client *dicomweb.Client
}

func NewDICOMWebService(baseURL string) *DICOMWebService {
    return &DICOMWebService{
        client: dicomweb.NewClient(baseURL),
    }
}

func (s *DICOMWebService) StoreInstance(ctx context.Context, filePath string) error {
    return s.client.StoreInstances(ctx, filePath)
}
```

## Integration Guide for SmartBox-Next

### 1. Replace Stub in backend/pacs/store_service.go

```go
package pacs

import (
    "context"
    "sync"
    "github.com/kristianvalind/go-netdicom-port/netdicom"
)

type UploadQueue struct {
    jobs      chan UploadJob
    workers   int
    wg        sync.WaitGroup
    service   *StoreService
}

type UploadJob struct {
    FilePath    string
    StudyUID    string
    RetryCount  int
    MaxRetries  int
}

func (q *UploadQueue) ProcessJob(job UploadJob) {
    ctx := context.Background()
    
    // Progress callback for UI updates
    progress := func(current, total int) {
        // Send progress to UI via WebSocket or SSE
        NotifyProgress(job.StudyUID, current, total)
    }
    
    err := q.service.Store(ctx, job.FilePath, progress)
    if err != nil && job.RetryCount < job.MaxRetries {
        job.RetryCount++
        q.jobs <- job // Re-queue for retry
    }
}
```

### 2. Character Encoding for German Text

```go
// Set German character encoding
func setGermanEncoding(dataset *dicom.Dataset) {
    dataset.Set(dicom.Tag{0x0008, 0x0005}, "ISO_IR 100") // Latin-1 for German
}
```

### 3. Concurrent Upload Support

```go
func (q *UploadQueue) Start() {
    for i := 0; i < q.workers; i++ {
        q.wg.Add(1)
        go q.worker(i)
    }
}

func (q *UploadQueue) worker(id int) {
    defer q.wg.Done()
    
    for job := range q.jobs {
        q.ProcessJob(job)
    }
}
```

## Testing with Orthanc

### Quick Orthanc Setup on Windows

1. **Download and Install**:
   ```bash
   # Download from http://www.orthanc-server.com/download-windows.php
   # Install as Windows Service
   ```

2. **Configure orthanc.json**:
   ```json
   {
     "DicomAet": "ORTHANC",
     "DicomPort": 4242,
     "RemoteAccessAllowed": true,
     "DicomModalities": {
       "smartbox": ["SMARTBOX", "127.0.0.1", 11112]
     }
   }
   ```

3. **Test Connection**:
   ```go
   service := NewStoreService("SMARTBOX", "ORTHANC", "localhost:4242")
   err := service.Echo(context.Background())
   ```

## Common Pitfalls and Solutions

### 1. Transfer Syntax Negotiation
- **Issue**: Private GE transfer syntaxes cause failures
- **Solution**: Filter out private syntaxes, support standard ones

### 2. Character Encoding
- **Issue**: German umlauts appear corrupted
- **Solution**: Set SpecificCharacterSet to "ISO_IR 100"

### 3. Network Timeouts
- **Issue**: Hospital WiFi causes connection drops
- **Solution**: Implement aggressive retry with exponential backoff

### 4. Memory Usage
- **Issue**: Large files exhaust memory
- **Solution**: Use streaming APIs from suyashkumar/dicom

### 5. Concurrent Uploads
- **Issue**: Thread safety concerns
- **Solution**: Use Go channels and proper synchronization

## Performance Considerations

1. **Connection Pooling**: Reuse connections for multiple uploads
2. **Chunk Size**: Use 1MB chunks for optimal network usage
3. **Concurrency**: Limit to 4-8 concurrent uploads
4. **Memory**: Stream files instead of loading fully
5. **Retry Strategy**: Exponential backoff with jitter

## Final Recommendations

1. **Use kristianvalind/go-netdicom-port** for traditional DICOM networking
2. **Consider DICOMweb** as a modern alternative if PACS supports it
3. **Implement robust error handling** with custom error types
4. **Test thoroughly with Orthanc** before production deployment
5. **Monitor the ecosystem** - suyashkumar/dicom may add networking in the future
6. **Consider hybrid approach**: Use existing library for prototyping, implement custom networking layer for production if needed

The Go DICOM ecosystem has limitations for networking, but kristianvalind/go-netdicom-port provides a workable solution for SmartBox-Next's C-STORE requirements. The examples and patterns provided should enable successful integration with your existing upload queue and retry logic.