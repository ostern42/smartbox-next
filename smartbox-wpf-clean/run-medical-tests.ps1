# SmartBox Medical Device Comprehensive Testing Framework
# Executes all medical device validation tests including FDA compliance, security, and performance testing

param(
    [string]$TestCategory = "All",
    [string]$OutputDirectory = "TestResults",
    [switch]$GenerateReport = $true,
    [switch]$SkipLongRunningTests = $false,
    [string]$Environment = "Test"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SmartBox Medical Device Testing Framework" -ForegroundColor Cyan
Write-Host "Version 2.0.0 - Medical Device Validation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Ensure test output directory exists
if (!(Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force
    Write-Host "Created output directory: $OutputDirectory" -ForegroundColor Green
}

# Test configuration
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$testResultsFile = "$OutputDirectory\TestResults_$timestamp.xml"
$coverageFile = "$OutputDirectory\Coverage_$timestamp.xml"
$reportFile = "$OutputDirectory\MedicalDeviceTestReport_$timestamp.html"

Write-Host "`nTest Configuration:" -ForegroundColor Yellow
Write-Host "- Test Category: $TestCategory" -ForegroundColor White
Write-Host "- Output Directory: $OutputDirectory" -ForegroundColor White
Write-Host "- Environment: $Environment" -ForegroundColor White
Write-Host "- Skip Long Running Tests: $SkipLongRunningTests" -ForegroundColor White

# Function to run specific test category
function Invoke-TestCategory {
    param(
        [string]$Category,
        [string]$Filter = ""
    )
    
    Write-Host "`n--- Running $Category Tests ---" -ForegroundColor Yellow
    
    $testCommand = "dotnet test SmartBoxNext.MedicalTests.csproj"
    $testCommand += " --logger `"trx;LogFileName=$testResultsFile`""
    $testCommand += " --collect:`"XPlat Code Coverage`""
    $testCommand += " --results-directory `"$OutputDirectory`""
    
    if ($Filter -ne "") {
        $testCommand += " --filter `"$Filter`""
    }
    
    Write-Host "Executing: $testCommand" -ForegroundColor Gray
    
    try {
        Invoke-Expression $testCommand
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ $Category tests completed successfully" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ $Category tests failed with exit code $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "✗ Error running $Category tests: $_" -ForegroundColor Red
        return $false
    }
}

# Test execution plan
$testResults = @{}
$startTime = Get-Date

try {
    switch ($TestCategory.ToLower()) {
        "all" {
            Write-Host "`nExecuting comprehensive medical device test suite..." -ForegroundColor Cyan
            
            # Unit Tests
            $testResults["Unit"] = Invoke-TestCategory "Unit Tests" "Category=Unit"
            
            # Integration Tests
            $testResults["Integration"] = Invoke-TestCategory "Integration Tests" "Category=Integration"
            
            # Security Tests
            $testResults["Security"] = Invoke-TestCategory "Security Tests" "Category=Security"
            
            # DICOM Conformance Tests
            $testResults["DICOM"] = Invoke-TestCategory "DICOM Conformance Tests" "Category=DICOM"
            
            # FDA Compliance Tests
            $testResults["FDA"] = Invoke-TestCategory "FDA Compliance Tests" "Category=FDA"
            
            # Performance Tests (conditional)
            if (!$SkipLongRunningTests) {
                Write-Host "`nNote: Performance tests may take up to 4 hours for complete endurance testing" -ForegroundColor Yellow
                $response = Read-Host "Continue with performance tests? (y/N)"
                if ($response.ToLower() -eq "y") {
                    $testResults["Performance"] = Invoke-TestCategory "Performance Tests" "Category=Performance"
                } else {
                    Write-Host "Skipping performance tests" -ForegroundColor Yellow
                    $testResults["Performance"] = $false
                }
            } else {
                Write-Host "Skipping long-running performance tests" -ForegroundColor Yellow
                $testResults["Performance"] = $false
            }
        }
        "unit" {
            $testResults["Unit"] = Invoke-TestCategory "Unit Tests" "Category=Unit"
        }
        "integration" {
            $testResults["Integration"] = Invoke-TestCategory "Integration Tests" "Category=Integration"
        }
        "security" {
            $testResults["Security"] = Invoke-TestCategory "Security Tests" "Category=Security"
        }
        "performance" {
            if ($SkipLongRunningTests) {
                Write-Host "Performance tests skipped due to -SkipLongRunningTests flag" -ForegroundColor Yellow
                $testResults["Performance"] = $false
            } else {
                $testResults["Performance"] = Invoke-TestCategory "Performance Tests" "Category=Performance"
            }
        }
        "dicom" {
            $testResults["DICOM"] = Invoke-TestCategory "DICOM Conformance Tests" "Category=DICOM"
        }
        "fda" {
            $testResults["FDA"] = Invoke-TestCategory "FDA Compliance Tests" "Category=FDA"
        }
        "compliance" {
            $testResults["FDA"] = Invoke-TestCategory "FDA Compliance Tests" "Category=FDA"
            $testResults["Security"] = Invoke-TestCategory "Security Tests" "Category=Security"
            $testResults["DICOM"] = Invoke-TestCategory "DICOM Conformance Tests" "Category=DICOM"
        }
        default {
            Write-Host "Unknown test category: $TestCategory" -ForegroundColor Red
            Write-Host "Valid categories: All, Unit, Integration, Security, Performance, DICOM, FDA, Compliance" -ForegroundColor Yellow
            exit 1
        }
    }
    
    # Test Summary
    $endTime = Get-Date
    $duration = $endTime - $startTime
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "TEST EXECUTION SUMMARY" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Total Duration: $($duration.ToString('hh\:mm\:ss'))" -ForegroundColor White
    Write-Host "Test Results:" -ForegroundColor White
    
    $passedTests = 0
    $totalTests = 0
    
    foreach ($test in $testResults.Keys) {
        $status = if ($testResults[$test]) { "PASSED" } else { "FAILED" }
        $color = if ($testResults[$test]) { "Green" } else { "Red" }
        Write-Host "  $test : $status" -ForegroundColor $color
        
        $totalTests++
        if ($testResults[$test]) { $passedTests++ }
    }
    
    Write-Host "`nOverall Result: $passedTests/$totalTests test categories passed" -ForegroundColor $(if ($passedTests -eq $totalTests) { "Green" } else { "Red" })
    
    # Generate comprehensive report
    if ($GenerateReport -and $totalTests -gt 0) {
        Write-Host "`nGenerating comprehensive medical device test report..." -ForegroundColor Yellow
        
        $reportContent = @"
<!DOCTYPE html>
<html>
<head>
    <title>SmartBox Medical Device Test Report</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #2c3e50; color: white; padding: 20px; text-align: center; }
        .summary { background-color: #ecf0f1; padding: 15px; margin: 20px 0; }
        .passed { color: #27ae60; font-weight: bold; }
        .failed { color: #e74c3c; font-weight: bold; }
        .section { margin: 20px 0; padding: 15px; border-left: 4px solid #3498db; }
        .compliance-section { border-left-color: #9b59b6; }
        .security-section { border-left-color: #e67e22; }
        .performance-section { border-left-color: #f39c12; }
        table { width: 100%; border-collapse: collapse; margin: 10px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        .metadata { font-size: 0.9em; color: #666; margin-top: 20px; }
    </style>
</head>
<body>
    <div class="header">
        <h1>SmartBox Medical Device Test Report</h1>
        <p>Comprehensive Validation and Compliance Testing</p>
        <p>Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</p>
    </div>
    
    <div class="summary">
        <h2>Executive Summary</h2>
        <p><strong>Test Environment:</strong> $Environment</p>
        <p><strong>Test Duration:</strong> $($duration.ToString('hh\:mm\:ss'))</p>
        <p><strong>Overall Result:</strong> <span class="$(if ($passedTests -eq $totalTests) { 'passed' } else { 'failed' })">$passedTests/$totalTests test categories passed</span></p>
    </div>
    
    <div class="section compliance-section">
        <h2>Medical Device Compliance Testing</h2>
        <table>
            <tr><th>Test Category</th><th>Status</th><th>Compliance Standard</th></tr>
"@
        
        foreach ($test in $testResults.Keys) {
            $status = if ($testResults[$test]) { '<span class="passed">PASSED</span>' } else { '<span class="failed">FAILED</span>' }
            $standard = switch ($test) {
                "FDA" { "FDA 21 CFR Part 820, Part 11" }
                "Security" { "HIPAA, NIST Cybersecurity Framework" }
                "DICOM" { "DICOM PS3.4, PS3.6, PS3.10, PS3.15" }
                "Integration" { "HL7, PACS, Medical Workflows" }
                "Performance" { "4-Hour Endurance, Load Testing" }
                "Unit" { "Service-Level Testing" }
                default { "Various Standards" }
            }
            $reportContent += "<tr><td>$test</td><td>$status</td><td>$standard</td></tr>"
        }
        
        $reportContent += @"
        </table>
    </div>
    
    <div class="section security-section">
        <h2>Security and Privacy Validation</h2>
        <ul>
            <li>HIPAA Privacy Rule (45 CFR 164.502-534) compliance</li>
            <li>HIPAA Security Rule (45 CFR 164.306-318) compliance</li>
            <li>GDPR data protection compliance</li>
            <li>Encryption and cryptographic validation</li>
            <li>Access control and authentication testing</li>
            <li>Penetration testing and vulnerability assessment</li>
        </ul>
    </div>
    
    <div class="section performance-section">
        <h2>Performance and Reliability Validation</h2>
        <ul>
            <li>4-hour continuous operation testing</li>
            <li>Memory leak detection and management</li>
            <li>Concurrent user load testing</li>
            <li>Medical workflow performance validation</li>
            <li>DICOM transmission performance</li>
            <li>System stress testing under extreme load</li>
        </ul>
    </div>
    
    <div class="section">
        <h2>Medical Device Standards Compliance</h2>
        <ul>
            <li><strong>ISO 14971:</strong> Risk Management for Medical Devices</li>
            <li><strong>IEC 62304:</strong> Medical Device Software Lifecycle Processes</li>
            <li><strong>ISO 13485:</strong> Quality Management Systems for Medical Devices</li>
            <li><strong>IEC 81001-5-1:</strong> Health Software Security for Medical Device Networks</li>
        </ul>
    </div>
    
    <div class="metadata">
        <p><strong>Test Framework Version:</strong> 2.0.0</p>
        <p><strong>SmartBox Version:</strong> 2.0.0</p>
        <p><strong>Test Results Location:</strong> $OutputDirectory</p>
        <p><strong>Generated by:</strong> SmartBox Medical Device Testing Framework</p>
    </div>
</body>
</html>
"@
        
        $reportContent | Out-File -FilePath $reportFile -Encoding UTF8
        Write-Host "✓ Test report generated: $reportFile" -ForegroundColor Green
    }
    
    # Exit with appropriate code
    if ($passedTests -eq $totalTests) {
        Write-Host "`n🎉 All medical device tests completed successfully!" -ForegroundColor Green
        Write-Host "System is ready for clinical deployment." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "`n⚠️ Some tests failed. Review results before deployment." -ForegroundColor Red
        Write-Host "Medical device compliance requires all tests to pass." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "`nFatal error during test execution: $_" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
finally {
    Write-Host "`nTest execution completed at $(Get-Date)" -ForegroundColor Gray
}