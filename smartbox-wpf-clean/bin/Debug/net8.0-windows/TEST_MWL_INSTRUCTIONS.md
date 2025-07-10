# Testing MWL (Modality Worklist) with Orthanc

## Quick Start with Docker Compose

```bash
# Start Orthanc with MWL support
docker-compose -f docker-compose-orthanc.yml up -d

# Create worklists directory
mkdir -p orthanc-worklists
```

## Add Test Worklist Entry

**Important**: Orthanc worklists must be in DICOM format, not JSON!

### Option 1: Create with Python (Recommended)
```bash
# Install pydicom
pip install pydicom

# Create test worklist
python create-test-worklist.py
```

### Option 2: Use dcmtk tools
```bash
# Install dcmtk (on Ubuntu/Debian)
sudo apt-get install dcmtk

# Create from dump file
dump2dcm test-worklist.dump orthanc-worklists/test001.wl
```

### Option 3: Manual file placement
```bash
# Install pydicom if needed
pip install pydicom

# Convert JSON to DICOM worklist
python json2dcm.py test-mwl-entry.dcm.json orthanc-worklists/test001.wl
```

2. **Update today's date in the worklist**:
   Edit test-mwl-entry.dcm.json and change line:
   ```json
   "0040,0002": { "vr": "DA", "Value": ["20250110"] },
   ```
   To today's date in YYYYMMDD format.

3. **Test in SmartBox**:
   - Go to Settings → MWL Settings
   - Verify settings:
     - Host: localhost
     - Port: 105
     - AET: ORTHANC
   - Click "Test Connection" - should show diagnostic window
   - Go back to main screen
   - Click on MWL tab - should show the test patient

## Creating More Test Entries

Copy test-mwl-entry.dcm.json and modify:
- Patient Name: `"0010,0010"` 
- Patient ID: `"0010,0020"`
- Accession Number: `"0008,0050"`
- Study UID: `"0020,000D"` (must be unique)
- Scheduled Date: `"0040,0002"` (inside the sequence)

## Troubleshooting

1. **MWL not showing entries**:
   - Check the scheduled date matches today
   - Verify Orthanc is running: http://localhost:8042
   - Check Orthanc logs for MWL queries

2. **Connection test fails**:
   - Ensure Docker is running
   - Check ports 104 (PACS) and 105 (MWL) are exposed
   - Verify firewall settings

3. **Orthanc Docker command** (if needed):
   ```bash
   docker run -p 4242:4242 -p 8042:8042 -p 104:104 -p 105:105 \
              -e ORTHANC__DICOM_MODALITIES_WORKLIST_ENABLE=true \
              --name orthanc jodogne/orthanc
   ```