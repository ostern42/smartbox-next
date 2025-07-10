#!/bin/bash

# Create worklist directory
mkdir -p worklists

# Create a test worklist entry
cat > worklists/test1.xml << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<NativeDicomModel>
  <DicomAttribute tag="0008,0050" vr="SH"><Value>ACC123</Value></DicomAttribute>
  <DicomAttribute tag="0010,0010" vr="PN"><Value>TEST^PATIENT</Value></DicomAttribute>
  <DicomAttribute tag="0010,0020" vr="LO"><Value>12345</Value></DicomAttribute>
  <DicomAttribute tag="0010,0030" vr="DA"><Value>19800101</Value></DicomAttribute>
  <DicomAttribute tag="0010,0040" vr="CS"><Value>M</Value></DicomAttribute>
  <DicomAttribute tag="0040,0100" vr="SQ">
    <Item>
      <DicomAttribute tag="0008,0060" vr="CS"><Value>ES</Value></DicomAttribute>
      <DicomAttribute tag="0040,0002" vr="DA"><Value>20250110</Value></DicomAttribute>
      <DicomAttribute tag="0040,0003" vr="TM"><Value>140000</Value></DicomAttribute>
    </Item>
  </DicomAttribute>
</NativeDicomModel>
EOF

# Update SmartBoxNext config
cat > config-orthanc.json << 'EOF'
{
  "Storage": {
    "PhotosPath": "./Data/Photos",
    "VideosPath": "./Data/Videos", 
    "DicomPath": "./Data/DICOM",
    "QueuePath": "./Data/Queue",
    "TempPath": "./Data/Temp",
    "MaxStorageDays": 30,
    "EnableAutoCleanup": false
  },
  "Pacs": {
    "ServerHost": "localhost",
    "ServerPort": 4242,
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
    "MwlServerPort": 4242,
    "MwlServerAET": "ORTHANC",
    "AutoRefreshSeconds": 300,
    "ShowEmergencyFirst": true,
    "CacheExpiryHours": 24
  },
  "Video": {
    "DefaultResolution": "1280x720",
    "DefaultFrameRate": 30,
    "DefaultQuality": 85,
    "EnableHardwareAcceleration": true,
    "PreferredCamera": ""
  },
  "Application": {
    "Language": "de-DE",
    "Theme": "Light",
    "EnableTouchKeyboard": true,
    "EnableDebugMode": true,
    "AutoStartCapture": true,
    "WebServerPort": 5112,
    "EnableRemoteAccess": false,
    "HideExitButton": false,
    "EnableEmergencyTemplates": true
  }
}
EOF

echo "Setup complete! Now run:"
echo "  docker-compose -f docker-compose-simple.yml up -d"
echo ""
echo "Then update your config.json with config-orthanc.json"
echo "Orthanc web interface: http://localhost:8042"