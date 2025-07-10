# PowerShell script to create worklist entries for testing
param(
    [string]$OutputDir = "./worklists",
    [int]$Count = 5
)

# Create directory if it doesn't exist
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

# Sample patient data
$patients = @(
    @{Name="MUELLER^HANS^JOSEF"; ID="10001"; BirthDate="19650420"; Sex="M"; Procedure="Gastroscopy"},
    @{Name="SCHMIDT^MARIA^ANNA"; ID="10002"; BirthDate="19780815"; Sex="F"; Procedure="Colonoscopy"},
    @{Name="BECKER^THOMAS"; ID="10003"; BirthDate="19550210"; Sex="M"; Procedure="Upper GI Endoscopy"},
    @{Name="WEBER^SABINE"; ID="10004"; BirthDate="19901125"; Sex="F"; Procedure="Sigmoidoscopy"},
    @{Name="EMERGENCY^PATIENT"; ID="99999"; BirthDate="19700101"; Sex="O"; Procedure="Emergency Endoscopy"}
)

# Generate worklist entries
for ($i = 0; $i -lt [Math]::Min($Count, $patients.Count); $i++) {
    $patient = $patients[$i]
    $date = Get-Date -Format "yyyyMMdd"
    $time = Get-Date -Format "HHmmss"
    $accession = "ACC" + (Get-Date -Format "yyyyMMddHHmmss") + $i
    $studyUID = "1.2.826.0.1.3680043.8.1055.1." + (Get-Date -Format "yyyyMMddHHmmss") + "." + $i
    
    $xml = @"
<?xml version="1.0" encoding="UTF-8"?>
<NativeDicomModel>
  <DicomAttribute tag="0008,0050" vr="SH">
    <Value>$accession</Value>
  </DicomAttribute>
  
  <DicomAttribute tag="0040,0100" vr="SQ">
    <Item>
      <DicomAttribute tag="0040,0001" vr="AE">
        <Value>SMARTBOX</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0002" vr="DA">
        <Value>$date</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0003" vr="TM">
        <Value>$time</Value>
      </DicomAttribute>
      <DicomAttribute tag="0008,0060" vr="CS">
        <Value>ES</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0006" vr="PN">
        <Value>Dr. Meyer</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0007" vr="LO">
        <Value>$($patient.Procedure)</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0010" vr="SH">
        <Value>ENDO_SUITE_1</Value>
      </DicomAttribute>
      <DicomAttribute tag="0040,0009" vr="SH">
        <Value>SPS$i</Value>
      </DicomAttribute>
    </Item>
  </DicomAttribute>
  
  <DicomAttribute tag="0010,0010" vr="PN">
    <Value>$($patient.Name)</Value>
  </DicomAttribute>
  <DicomAttribute tag="0010,0020" vr="LO">
    <Value>$($patient.ID)</Value>
  </DicomAttribute>
  <DicomAttribute tag="0010,0030" vr="DA">
    <Value>$($patient.BirthDate)</Value>
  </DicomAttribute>
  <DicomAttribute tag="0010,0040" vr="CS">
    <Value>$($patient.Sex)</Value>
  </DicomAttribute>
  <DicomAttribute tag="0020,000D" vr="UI">
    <Value>$studyUID</Value>
  </DicomAttribute>
  <DicomAttribute tag="0032,1060" vr="LO">
    <Value>$($patient.Procedure)</Value>
  </DicomAttribute>
  <DicomAttribute tag="0040,1001" vr="SH">
    <Value>RP$i</Value>
  </DicomAttribute>
</NativeDicomModel>
"@
    
    $filename = Join-Path $OutputDir "worklist_$i.xml"
    $xml | Out-File -FilePath $filename -Encoding UTF8
    Write-Host "Created: $filename"
}

Write-Host "`nWorklist entries created in $OutputDir"
Write-Host "Restart Orthanc or wait for it to detect the new files."