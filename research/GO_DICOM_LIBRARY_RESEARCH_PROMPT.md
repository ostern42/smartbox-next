# Web Research Request: Go DICOM Libraries for C-STORE Implementation

## Research Objective
Find and evaluate Go libraries that support DICOM networking operations, specifically C-STORE (file upload to PACS), C-ECHO (connection test), and ideally C-FIND/C-MOVE for future features.

## Background Context
SmartBox-Next needs to send DICOM files to PACS servers. We have already implemented:
- DICOM file creation (working, opens in MicroDicom)
- Upload queue with persistence
- PACS configuration management
- Retry logic with exponential backoff

Now we need the actual DICOM networking implementation.

## Specific Questions

### 1. Available Go DICOM Libraries
- Which Go libraries support DICOM networking (not just parsing)?
- Comparison of features, maturity, and maintenance status
- License compatibility (need commercial-friendly)
- Community size and support quality

### 2. C-STORE Implementation
- How to implement DICOM C-STORE in Go?
- Code examples for sending files to PACS
- Handling different transfer syntaxes
- Error handling and status codes
- Timeout and retry best practices

### 3. Library Comparison
Please research and compare:
- **suyashkumar/dicom** - Most popular, but networking support?
- **grailbio/go-dicom** - Alternative implementation
- **gradienthealth/dicom** - Fork with improvements?
- **cylab/dicom** - Networking focused?
- Any other relevant libraries

### 4. C-ECHO Implementation
- How to test PACS connectivity?
- C-ECHO implementation examples
- Connection validation best practices
- AE Title negotiation

### 5. Authentication & Security
- How to handle AE Title authentication?
- TLS support for secure DICOM?
- Called/Calling AE Title configuration
- Port and network considerations

### 6. Integration Considerations
- Thread safety for concurrent uploads
- Memory efficiency for large files
- Streaming vs loading entire file
- Progress callbacks for UI updates

### 7. Testing Tools
- How to test without real PACS?
- Orthanc setup for development
- DICOM test servers available?
- Debugging DICOM network traffic

### 8. Common Pitfalls
- What are common C-STORE implementation mistakes?
- Transfer syntax negotiation issues
- Character encoding problems
- Network timeout handling
- Large file considerations

## Search Strategies
- GitHub searches for "go dicom c-store"
- Go package documentation sites
- DICOM developer forums
- Medical imaging developer communities
- Stack Overflow DICOM + Go questions

## Expected Deliverables
1. **Library recommendation** with justification
2. **Code example** for basic C-STORE implementation
3. **Integration guide** for our existing code
4. **Testing strategy** with Orthanc
5. **Error handling** best practices
6. **Performance considerations** for medical use

## Additional Context
- Must work on Windows 10/11
- Need to handle German character encoding (ä, ö, ü)
- Files are typically 50KB-1MB (JPEG compressed)
- Network may be unreliable (hospital WiFi)
- Must support multiple PACS vendors

## Code Context
Our current stub implementation in `backend/pacs/store_service.go`:
```go
func (s *StoreService) attemptStore(ctx context.Context, dicomPath string, pacsConfig config.PACSConfig) error {
    // TODO: Implement actual C-STORE when we have DICOM library
    // Need: Connection, file sending, status handling
}
```

We need to replace this with actual DICOM networking code.