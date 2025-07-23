# SmartBox API Diagnostics Script
Write-Host "SmartBox API Diagnostics" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Function to test URL
function Test-Url {
    param($url)
    try {
        $response = Invoke-WebRequest -Uri $url -Method GET -TimeoutSec 5 -UseBasicParsing
        Write-Host "✓ $url" -ForegroundColor Green
        Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Gray
        return $true
    }
    catch {
        Write-Host "✗ $url" -ForegroundColor Red
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
        return $false
    }
}

# 1. Check if SmartBoxNext.exe is running
Write-Host "1. Checking if SmartBoxNext.exe is running..." -ForegroundColor Yellow
$process = Get-Process -Name "SmartBoxNext" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "✓ SmartBoxNext.exe is running (PID: $($process.Id))" -ForegroundColor Green
} else {
    Write-Host "✗ SmartBoxNext.exe is NOT running" -ForegroundColor Red
    Write-Host "  Please start the application first!" -ForegroundColor Yellow
}

# 2. Check port 5002
Write-Host "`n2. Checking port 5002..." -ForegroundColor Yellow
$tcpConnections = Get-NetTCPConnection -LocalPort 5002 -ErrorAction SilentlyContinue
if ($tcpConnections) {
    Write-Host "✓ Port 5002 is listening" -ForegroundColor Green
    foreach ($conn in $tcpConnections) {
        $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "  Process: $($proc.Name) (PID: $($proc.Id))" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "✗ Port 5002 is NOT listening" -ForegroundColor Red
}

# 3. Check Windows Firewall
Write-Host "`n3. Checking Windows Firewall..." -ForegroundColor Yellow
$firewallRules = Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*5002*" -or $_.DisplayName -like "*SmartBox*" }
if ($firewallRules) {
    Write-Host "✓ Found firewall rules:" -ForegroundColor Green
    foreach ($rule in $firewallRules) {
        Write-Host "  - $($rule.DisplayName) [$($rule.Enabled)]" -ForegroundColor Gray
    }
} else {
    Write-Host "⚠ No specific firewall rules found" -ForegroundColor Yellow
    Write-Host "  Creating firewall rule..." -ForegroundColor Gray
    
    # Try to create firewall rule (requires admin)
    try {
        New-NetFirewallRule -DisplayName "SmartBox API Port 5002" `
            -Direction Inbound -LocalPort 5002 -Protocol TCP -Action Allow `
            -ErrorAction Stop | Out-Null
        Write-Host "✓ Firewall rule created successfully" -ForegroundColor Green
    }
    catch {
        Write-Host "  Could not create firewall rule (run as admin)" -ForegroundColor Gray
    }
}

# 4. Test API endpoints
Write-Host "`n4. Testing API endpoints..." -ForegroundColor Yellow
$endpoints = @(
    "http://localhost:5002/api/health",
    "http://127.0.0.1:5002/api/health",
    "http://localhost:5002/api",
    "http://localhost:5002/"
)

$anySuccess = $false
foreach ($endpoint in $endpoints) {
    if (Test-Url $endpoint) {
        $anySuccess = $true
    }
}

# 5. Check WebView2 ports (5000, 5001)
Write-Host "`n5. Checking WebView2 ports..." -ForegroundColor Yellow
$webPorts = @(5000, 5001)
foreach ($port in $webPorts) {
    $conn = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($conn) {
        Write-Host "✓ Port $port is listening (WebView2)" -ForegroundColor Green
    } else {
        Write-Host "✗ Port $port is not listening" -ForegroundColor Red
    }
}

# 6. Test with curl if available
Write-Host "`n6. Testing with curl..." -ForegroundColor Yellow
if (Get-Command curl -ErrorAction SilentlyContinue) {
    $curlResult = curl -s -w "%{http_code}" -o $null http://localhost:5002/api/health 2>$null
    if ($curlResult -eq "200" -or $curlResult -eq "404") {
        Write-Host "✓ curl test successful (HTTP $curlResult)" -ForegroundColor Green
    } else {
        Write-Host "✗ curl test failed" -ForegroundColor Red
    }
} else {
    Write-Host "  curl not available" -ForegroundColor Gray
}

# Summary
Write-Host "`n========== SUMMARY ==========" -ForegroundColor Cyan
if ($process -and $tcpConnections -and $anySuccess) {
    Write-Host "✓ API appears to be running correctly!" -ForegroundColor Green
    Write-Host "  Open http://localhost:5000/test-api.html in your browser" -ForegroundColor Gray
} else {
    Write-Host "✗ API is not fully functional" -ForegroundColor Red
    Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
    if (-not $process) {
        Write-Host "1. Start SmartBoxNext.exe" -ForegroundColor Gray
    }
    if (-not $tcpConnections) {
        Write-Host "2. Check if StreamingServerManager is initialized in MainWindow.xaml.cs" -ForegroundColor Gray
        Write-Host "3. Look for 'Streaming server started on port 5002' in the console" -ForegroundColor Gray
    }
    if (-not $anySuccess) {
        Write-Host "4. Check Windows Firewall and antivirus settings" -ForegroundColor Gray
        Write-Host "5. Try running as Administrator" -ForegroundColor Gray
    }
}

Write-Host "`nPress any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")