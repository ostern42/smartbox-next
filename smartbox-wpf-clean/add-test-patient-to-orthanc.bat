@echo off
echo Adding test patient to Orthanc...
echo.

REM This creates a test DICOM instance in Orthanc
curl -X POST http://localhost:8042/tools/create-dicom ^
  -H "Content-Type: application/json" ^
  -d "{\"Tags\": {\"PatientName\": \"TEST^PATIENT\", \"PatientID\": \"TEST123\", \"PatientBirthDate\": \"19800101\", \"PatientSex\": \"M\", \"StudyDescription\": \"Chest X-Ray\", \"AccessionNumber\": \"ACC001\", \"Modality\": \"CR\", \"SeriesDescription\": \"Test Series\", \"SOPClassUID\": \"1.2.840.10008.5.1.4.1.1.1\", \"StudyDate\": \"20250710\", \"SeriesDate\": \"20250710\", \"StudyTime\": \"090000\", \"SeriesTime\": \"090000\"}}"

echo.
echo Test patient added! Check http://localhost:8042
echo.
echo For worklist functionality:
echo 1. Orthanc needs worklist files in its worklist directory
echo 2. These are DICOM files (not JSON) with .wl extension
echo 3. Place them in the Docker container: /var/lib/orthanc/worklists/
echo.
echo Alternative: Just use manual patient entry in SmartBox!
pause