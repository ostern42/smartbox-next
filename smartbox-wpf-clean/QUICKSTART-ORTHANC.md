# Quick Start: Orthanc + MWL for SmartBoxNext Testing

## Simplest Setup (3 commands):

### 1. Create worklist directory and sample entry:
```bash
mkdir -p worklists
```

### 2. Create a test patient worklist:
```bash
echo '<?xml version="1.0" encoding="UTF-8"?>
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
</NativeDicomModel>' > worklists/test1.xml
```

### 3. Run Orthanc:
```bash
docker run -d \
  --name orthanc-test \
  -p 104:4242 \
  -p 105:4242 \
  -p 8042:8042 \
  -v $(pwd)/worklists:/worklists \
  -e ORTHANC__DICOM_AET=ORTHANC \
  -e ORTHANC__DICOM_ALWAYS_ALLOW_ECHO=true \
  -e ORTHANC__DICOM_ALWAYS_ALLOW_STORE=true \
  -e ORTHANC__DICOM_CHECK_CALLED_AET=false \
  -e 'ORTHANC__PLUGINS=["libModalityWorklist.so"]' \
  -e ORTHANC__WORKLISTS__ENABLE=true \
  -e ORTHANC__WORKLISTS__DATABASE=/worklists \
  jodogne/orthanc-plugins:latest
```

## That's it! 

- **Web Interface**: http://localhost:8042
- **PACS C-STORE Port**: 104
- **MWL Query Port**: 105

## SmartBoxNext is already configured with:

```json
{
  "Pacs": {
    "ServerHost": "localhost",
    "ServerPort": 104,
    "CalledAeTitle": "ORTHANC",
    "CallingAeTitle": "SMARTBOX"
  },
  "MwlSettings": {
    "EnableWorklist": true,
    "MwlServerHost": "localhost", 
    "MwlServerPort": 105,
    "MwlServerAET": "ORTHANC"
  }
}
```

## Test Commands:

```bash
# Stop Orthanc
docker stop orthanc-test

# Remove container
docker rm orthanc-test

# View logs
docker logs orthanc-test
```

## Check Orthanc Web Interface:

1. Go to http://localhost:8042
2. Click on "Plugins" in the menu
3. You should see "worklist" plugin enabled
4. Click on "System" to see DICOM configuration

## Add more test patients:

Create more files in the worklists directory (test2.xml, test3.xml, etc.) with different patient data.