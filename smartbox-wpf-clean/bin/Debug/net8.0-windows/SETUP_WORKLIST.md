# Setting Up Worklist for Testing

## Quick Solution - Add Worklist via Orthanc API

Since Orthanc doesn't accept worklist uploads through the web UI, use the REST API:

### 1. Create a test patient/study via API:

```bash
# First, upload a dummy DICOM file to create a patient
curl -X POST http://localhost:8042/tools/create-dicom \
  -H "Content-Type: application/json" \
  -d '{
    "Tags": {
      "PatientName": "TEST^PATIENT",
      "PatientID": "TEST123",
      "PatientBirthDate": "19800101",
      "PatientSex": "M",
      "StudyDescription": "Chest X-Ray",
      "AccessionNumber": "ACC001",
      "Modality": "CR",
      "SeriesDescription": "Test Series",
      "SOPClassUID": "1.2.840.10008.5.1.4.1.1.1"
    }
  }'
```

### 2. Alternative - Use Docker Volume Mount

If using Docker, place worklist files directly in the container:

```bash
# Copy worklist file into running container
docker cp test001.wl orthanc-test:/var/lib/orthanc/worklists/
```

### 3. For Production - Use pydicom

Install pydicom and create proper worklist files:

```bash
pip install pydicom
python create-test-worklist.py
```

## Testing DICOM Export

With `AutoExportDicom: true` in config.json, the system will:

1. **Capture Photo** → Saves as JPEG
2. **Convert to DICOM** → Creates DICOM file with patient info
3. **Queue for PACS** → Adds to upload queue
4. **Send via C-STORE** → QueueProcessor sends to PACS

To verify it's working:
1. Select a patient from MWL (or create emergency patient)
2. Capture a photo
3. Check Orthanc web UI (http://localhost:8042) - image should appear
4. Check logs for "DICOM conversion successful" and "C-STORE successful"

## Current Status

✅ **DICOM Export**: Fully implemented
✅ **PACS C-STORE**: Working with QueueProcessor
✅ **Auto-send**: Enabled with `AutoExportDicom: true`
✅ **Patient Context**: From MWL selection or manual entry

The system IS ready to send images to PACS!