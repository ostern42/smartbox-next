# Orthanc Worklist Upload Fix

The error "Bad file format" occurs because Orthanc expects DICOM format worklist files, not JSON.

## Quick Solution

1. **Install Python and pydicom**:
   ```
   pip install pydicom
   ```

2. **Run the worklist creation script**:
   ```
   python create-test-worklist.py
   ```

3. **The script will create**: `orthanc-worklists/test001.wl`

4. **Copy to Docker volume** (if using Docker):
   - The worklist file needs to be in the container's worklist directory
   - Mount the local directory: `-v ./orthanc-worklists:/var/lib/orthanc/worklists`

## Alternative: Using dump2dcm

If you have dcmtk installed:
```bash
dump2dcm test-worklist.dump orthanc-worklists/test001.wl
```

## Why JSON doesn't work

- Orthanc's web upload expects DICOM files for worklists
- The JSON format is only for REST API operations
- Worklist files must be in DICOM format with .wl extension

## Testing

After creating the worklist file:
1. Restart Orthanc if needed
2. In SmartBox, go to MWL tab
3. You should see "TEST^PATIENT^ONE" in the list