# Orthanc PACS/MWL Setup for SmartBoxNext

This guide explains how to set up Orthanc as a PACS server with Modality Worklist (MWL) support for SmartBoxNext.

## Quick Start

1. **Create required directories:**
```bash
mkdir -p orthanc-storage worklists orthanc-config
```

2. **Copy configuration files:**
```bash
# Copy orthanc.json to orthanc-config directory
cp orthanc.json orthanc-config/

# Copy sample worklist to worklists directory
cp sample-worklist.xml worklists/worklist1.xml
```

3. **Start Orthanc:**
```bash
docker-compose up -d
```

4. **Verify Orthanc is running:**
   - Web interface: http://localhost:8042
   - DICOM port: 104 (PACS) and 105 (MWL)

## Configuration Details

### Key Fixes Made:

1. **Port Configuration**
   - Port 104: Standard DICOM port for C-STORE operations
   - Port 105: Also maps to 4242 (Orthanc's DICOM port) for MWL queries
   - Port 8042: Web interface

2. **DICOM Settings**
   - `DicomCheckCalledAet`: Set to false to accept connections from any AET
   - `DicomAlwaysAllowStore`: True to accept all C-STORE operations
   - `AllowFindWorklist`: True in modality settings for MWL queries

3. **Modality Worklist**
   - `FilterIssuerAet`: Set to false to return all worklist items
   - Worklist files must be in XML format in `/var/lib/orthanc/worklists`

4. **Transfer Syntaxes**
   - Accepts all common transfer syntaxes including JPEG variants
   - Supports both compressed and uncompressed formats

## SmartBoxNext Configuration

Update your `config.json`:

```json
{
  "Pacs": {
    "ServerHost": "localhost",
    "ServerPort": 104,
    "CalledAeTitle": "ORTHANC",
    "CallingAeTitle": "SMARTBOX",
    "Timeout": 30,
    "EnableTls": false,
    "MaxRetries": 3,
    "RetryDelay": 5
  },
  "MwlSettings": {
    "EnableWorklist": true,
    "MwlServerHost": "localhost",
    "MwlServerPort": 105,
    "MwlServerAET": "ORTHANC",
    "AutoRefreshSeconds": 300,
    "ShowEmergencyFirst": true,
    "CacheExpiryHours": 24
  }
}
```

## Testing

### Test DICOM Echo:
```bash
# Using dcmtk tools
echoscu localhost 104 -aec ORTHANC -aet TEST
```

### Test MWL Query:
```bash
# Query worklist
findscu -W localhost 105 -aec ORTHANC -aet SMARTBOX -k "0008,0050=" -k "0010,0010="
```

### Test C-STORE:
```bash
# Send a DICOM file
storescu localhost 104 -aec ORTHANC -aet SMARTBOX sample.dcm
```

## Adding Worklist Entries

1. Create XML files in the `worklists` directory
2. Each file represents one worklist entry
3. Filename must end with `.xml`
4. Orthanc automatically picks up new files

### Required DICOM Tags for MWL:
- (0008,0050) Accession Number
- (0010,0010) Patient Name
- (0010,0020) Patient ID
- (0040,0100) Scheduled Procedure Step Sequence
  - (0008,0060) Modality (ES for endoscopy)
  - (0040,0002) Scheduled Date
  - (0040,0001) Scheduled Station AET

## Troubleshooting

1. **Check Orthanc logs:**
```bash
docker logs orthanc-mwl
```

2. **Verify ports are open:**
```bash
netstat -an | grep -E "104|105|8042"
```

3. **Test with Orthanc Explorer:**
   - Navigate to http://localhost:8042
   - Check "Modalities" section
   - Verify SMARTBOX is listed

4. **Common Issues:**
   - **MWL returns empty**: Check worklist XML files are valid
   - **C-STORE fails**: Verify transfer syntax compatibility
   - **Connection refused**: Check firewall and Docker networking

## Docker Network Note

If SmartBoxNext runs on the host machine:
- Use `localhost` in SmartBoxNext config
- Use `host.docker.internal` in Orthanc's modality config

If both run in Docker:
- Use service names with shared network
- Update configs accordingly