# PowerShell script to create test worklist entries

# Create worklists directory if not exists
New-Item -ItemType Directory -Force -Path ".\worklists"

# Function to create worklist XML
function Create-WorklistXML {
    param(
        [string]$AccessionNumber,
        [string]$PatientName,
        [string]$PatientID,
        [string]$BirthDate,
        [string]$Sex,
        [string]$StudyDate,
        [string]$StudyTime,
        [string]$Modality = "CR",
        [string]$StationAET = "SMARTBOX",
        [string]$StudyDescription = "Chest X-Ray"
    )
    
    $xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<NativeDicomModel>
    <DicomAttribute tag="0008,0050" vr="SH" keyword="AccessionNumber">$AccessionNumber</DicomAttribute>
    <DicomAttribute tag="0010,0010" vr="PN" keyword="PatientName">$PatientName</DicomAttribute>
    <DicomAttribute tag="0010,0020" vr="LO" keyword="PatientID">$PatientID</DicomAttribute>
    <DicomAttribute tag="0010,0030" vr="DA" keyword="PatientBirthDate">$BirthDate</DicomAttribute>
    <DicomAttribute tag="0010,0040" vr="CS" keyword="PatientSex">$Sex</DicomAttribute>
    <DicomAttribute tag="0020,000D" vr="UI" keyword="StudyInstanceUID">1.2.276.0.7230010.3.1.4.$(Get-Random -Maximum 999999999)</DicomAttribute>
    <DicomAttribute tag="0032,1060" vr="LO" keyword="RequestedProcedureDescription">$StudyDescription</DicomAttribute>
    <DicomAttribute tag="0040,0100" vr="SQ" keyword="ScheduledProcedureStepSequence">
        <Item number="1">
            <DicomAttribute tag="0008,0060" vr="CS" keyword="Modality">$Modality</DicomAttribute>
            <DicomAttribute tag="0040,0001" vr="AE" keyword="ScheduledStationAETitle">$StationAET</DicomAttribute>
            <DicomAttribute tag="0040,0002" vr="DA" keyword="ScheduledProcedureStepStartDate">$StudyDate</DicomAttribute>
            <DicomAttribute tag="0040,0003" vr="TM" keyword="ScheduledProcedureStepStartTime">$StudyTime</DicomAttribute>
            <DicomAttribute tag="0040,0006" vr="PN" keyword="ScheduledPerformingPhysicianName">Dr. Smith</DicomAttribute>
            <DicomAttribute tag="0040,0007" vr="LO" keyword="ScheduledProcedureStepDescription">$StudyDescription</DicomAttribute>
            <DicomAttribute tag="0040,0009" vr="SH" keyword="ScheduledProcedureStepID">SPS$(Get-Random -Maximum 9999)</DicomAttribute>
        </Item>
    </DicomAttribute>
</NativeDicomModel>
"@
    
    return $xml
}

# Get today's date in DICOM format (YYYYMMDD)
$today = (Get-Date).ToString("yyyyMMdd")
$currentTime = (Get-Date).ToString("HHmmss")

# Create test patients
$patients = @(
    @{
        AccessionNumber = "ACC001"
        PatientName = "Mustermann^Max"
        PatientID = "PAT001"
        BirthDate = "19800515"
        Sex = "M"
        StudyDescription = "Chest X-Ray"
    },
    @{
        AccessionNumber = "ACC002"
        PatientName = "Schmidt^Anna"
        PatientID = "PAT002"
        BirthDate = "19920823"
        Sex = "F"
        StudyDescription = "Abdomen X-Ray"
    },
    @{
        AccessionNumber = "ACC003"
        PatientName = "Mueller^Klaus"
        PatientID = "PAT003"
        BirthDate = "19650312"
        Sex = "M"
        StudyDescription = "Hand X-Ray"
    },
    @{
        AccessionNumber = "EMRG001"
        PatientName = "NOTFALL^PATIENT"
        PatientID = "EMRG001"
        BirthDate = "20000101"
        Sex = "O"
        StudyDescription = "Emergency Chest X-Ray"
    }
)

# Create worklist files
foreach ($patient in $patients) {
    $xml = Create-WorklistXML `
        -AccessionNumber $patient.AccessionNumber `
        -PatientName $patient.PatientName `
        -PatientID $patient.PatientID `
        -BirthDate $patient.BirthDate `
        -Sex $patient.Sex `
        -StudyDate $today `
        -StudyTime $currentTime `
        -StudyDescription $patient.StudyDescription
    
    $filename = ".\worklists\wl_$($patient.AccessionNumber).xml"
    $xml | Out-File -FilePath $filename -Encoding UTF8
    Write-Host "Created worklist: $filename"
}

Write-Host "`nWorklist files created successfully!"
Write-Host "Now run: docker-compose -f docker-compose-orthanc-mwl.yml up -d"