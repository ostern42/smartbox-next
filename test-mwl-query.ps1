# Test MWL Query using dcmtk (findscu)
# Install dcmtk first: choco install dcmtk

Write-Host "Testing Modality Worklist Query..."
Write-Host "================================="

# Create query file
$queryFile = @"
# Query all worklists for today
(0008,0060) CS [CR]                     # Modality
(0010,0010) PN []                      # Patient Name
(0010,0020) LO []                      # Patient ID
(0010,0030) DA []                      # Birth Date
(0010,0040) CS []                      # Sex
(0040,0100) SQ                         # Scheduled Procedure Step Sequence
(0008,0060) CS []                      # Modality in sequence
(0040,0002) DA []                      # Scheduled Date
(0040,0003) TM []                      # Scheduled Time
ENDSEQ
"@

$queryFile | Out-File -FilePath "mwl-query.txt" -Encoding ASCII

# Run the query
Write-Host "`nQuerying Orthanc MWL..."
Write-Host "Command: findscu -W -k `"ScheduledProcedureStepSequence[0].ScheduledProcedureStepStartDate=$((Get-Date).ToString('yyyyMMdd'))`" localhost 105"

# Alternative using curl to REST API
Write-Host "`nAlternative: Query via REST API..."
Write-Host "curl http://localhost:8043/modalities/SMARTBOX/find-worklist"