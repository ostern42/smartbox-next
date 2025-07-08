# PowerShell script to test MWL queries

Write-Host "Testing Orthanc MWL via REST API..." -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# First check if Orthanc is running
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8043/system" -Method Get
    $system = $response.Content | ConvertFrom-Json
    Write-Host "✓ Orthanc is running: $($system.Name) version $($system.Version)" -ForegroundColor Green
} catch {
    Write-Host "✗ Cannot connect to Orthanc on port 8043" -ForegroundColor Red
    Write-Host "  Make sure docker-compose is running!" -ForegroundColor Yellow
    exit
}

# Check if MWL plugin is loaded
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8043/plugins" -Method Get
    $plugins = $response.Content | ConvertFrom-Json
    if ($plugins -contains "worklists") {
        Write-Host "✓ Modality Worklist plugin is loaded" -ForegroundColor Green
    } else {
        Write-Host "✗ Modality Worklist plugin NOT found" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Cannot check plugins" -ForegroundColor Red
}

# Query all worklists
Write-Host "`nQuerying all worklists..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8043/worklists" -Method Get
    $worklists = $response.Content | ConvertFrom-Json
    
    Write-Host "Found $($worklists.Count) worklist(s):" -ForegroundColor Green
    foreach ($wl in $worklists) {
        Write-Host "  - $wl"
    }
} catch {
    Write-Host "✗ Cannot query worklists directly" -ForegroundColor Red
    Write-Host "  This is normal - worklists are queried via DICOM protocol" -ForegroundColor Yellow
}

# Alternative: Use DICOM echo to test connection
Write-Host "`nTesting DICOM connection on port 105..." -ForegroundColor Yellow
try {
    # This would need dcmtk installed
    Write-Host "To test DICOM connection, install dcmtk and run:" -ForegroundColor Cyan
    Write-Host "  echoscu localhost 105" -ForegroundColor White
    Write-Host "  findscu -W localhost 105" -ForegroundColor White
} catch {
    Write-Host "DICOM tools not available" -ForegroundColor Yellow
}

# Show how to query via fo-dicom in C#
Write-Host "`nTo query from SmartBox (C# code):" -ForegroundColor Cyan
Write-Host @'
var client = new DicomClient("localhost", 105, false, "SMARTBOX", "ORTHANC");
var request = new DicomCFindRequest(DicomQueryRetrieveLevel.Worklist);

// Add query parameters
request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
request.Dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, DateTime.Today.ToString("yyyyMMdd"));

// Handle responses
request.OnResponseReceived += (req, response) => {
    if (response.Status == DicomStatus.Pending && response.HasDataset) {
        var patientName = response.Dataset.GetSingleValue<string>(DicomTag.PatientName);
        var patientId = response.Dataset.GetSingleValue<string>(DicomTag.PatientID);
        Console.WriteLine($"Found: {patientName} ({patientId})");
    }
};

await client.AddRequestAsync(request);
await client.SendAsync();
'@ -ForegroundColor White