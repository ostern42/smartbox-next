# PACS Setup Guide for SmartBox-Next

## Quick Start with Orthanc

### 1. Install Orthanc
- Download from: http://www.orthanc-server.com/download-windows.php
- Install as Windows Service
- Default ports: 4242 (DICOM), 8042 (HTTP)

### 2. Configure SmartBox-Next
1. Run `update-deps.bat` to fetch Go dependencies
2. Run `start.bat` to start the development server
3. In the app:
   - Click the **PACS Settings** button
   - Enable PACS
   - Configure:
     - Host: `localhost`
     - Port: `4242`
     - Local AE Title: `SMARTBOX`
     - Remote AE Title: `ORTHANC`
   - Click **Test Connection**

### 3. Test DICOM Upload
1. Capture an image with the webcam
2. Fill in patient information
3. Click **Export DICOM**
4. The file will be:
   - Saved locally in `~/SmartBoxNext/Exports/`
   - Queued for PACS upload
   - Automatically sent to Orthanc

### 4. View in Orthanc
- Open browser: http://localhost:8042
- Your images should appear in the patient list

## Troubleshooting

### Connection Test Failed
- Check Orthanc is running: `netstat -an | findstr 4242`
- Verify AE titles match
- Check Windows Firewall

### Upload Failed
- Check the queue status in SmartBox
- Look for error messages
- Verify disk space

## Production Setup
For production, configure:
- Proper AE titles
- Network timeouts
- Retry policies
- Queue persistence location