@echo off
echo Creating test worklist entry...

REM Create worklists directory if it doesn't exist
if not exist "orthanc-worklists" mkdir orthanc-worklists

REM Convert JSON to DICOM using dcm2xml (requires DCMTK)
REM For now, we'll create a simple placeholder
echo This script requires DCMTK tools to convert JSON to DICOM format.
echo.
echo Manual steps:
echo 1. Use Orthanc web interface at http://localhost:8042
echo 2. Go to "Upload" menu
echo 3. Select the test-mwl-entry.dcm.json file
echo.
echo Or use the Python script: python json2dcm.py test-mwl-entry.dcm.json orthanc-worklists/test001.wl
pause