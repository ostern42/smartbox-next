# PowerShell script to install SmartBoxNext Capture Service
# Usage: .\install-service.ps1 [-Uninstall]

param(
    [switch]$Uninstall,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Restart,
    [switch]$Status
)

$ServiceName = "SmartBoxNextCapture"
$ServiceDisplayName = "SmartBoxNext Capture Service"
$ServiceDescription = "Video capture service for Yuan SC550N1 with SharedMemory IPC to SmartBoxNext UI"

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Get service executable path
$ServicePath = Join-Path $PSScriptRoot "bin\Release\net48\SmartBoxNext.CaptureService.exe"

if (-not (Test-Path $ServicePath) -and -not $Uninstall -and -not $Status)
{
    Write-Host "ERROR: Service executable not found: $ServicePath" -ForegroundColor Red
    Write-Host "Please build the service first with: dotnet build -c Release" -ForegroundColor Yellow
    exit 1
}

function Show-ServiceStatus {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "Service Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq "Running") { "Green" } else { "Yellow" })
        Write-Host "Start Type: $($service.StartType)" -ForegroundColor Gray
        Write-Host "Display Name: $($service.DisplayName)" -ForegroundColor Gray
        
        # Show recent log entries
        try {
            $events = Get-WinEvent -LogName Application -FilterXPath "*[System[Provider[@Name='SmartBoxNext Capture Service']]]" -MaxEvents 5 -ErrorAction SilentlyContinue
            if ($events) {
                Write-Host "`nRecent Log Entries:" -ForegroundColor Cyan
                foreach ($event in $events) {
                    Write-Host "  $($event.TimeCreated): $($event.LevelDisplayName) - $($event.Message)" -ForegroundColor Gray
                }
            }
        } catch {
            Write-Host "Could not retrieve recent log entries" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Service is not installed" -ForegroundColor Red
    }
}

# Handle status request
if ($Status) {
    Show-ServiceStatus
    exit 0
}

# Handle uninstall request
if ($Uninstall) {
    Write-Host "Uninstalling $ServiceDisplayName..." -ForegroundColor Yellow
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq "Running") {
            Write-Host "Stopping service..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 3
        }
        
        Write-Host "Removing service..." -ForegroundColor Yellow
        sc.exe delete $ServiceName
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service uninstalled successfully" -ForegroundColor Green
        } else {
            Write-Host "Failed to uninstall service" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "Service is not installed" -ForegroundColor Yellow
    }
    exit 0
}

# Handle start request
if ($Start) {
    Write-Host "Starting $ServiceDisplayName..." -ForegroundColor Yellow
    
    try {
        Start-Service -Name $ServiceName
        Write-Host "Service started successfully" -ForegroundColor Green
        Show-ServiceStatus
    } catch {
        Write-Host "Failed to start service: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    exit 0
}

# Handle stop request
if ($Stop) {
    Write-Host "Stopping $ServiceDisplayName..." -ForegroundColor Yellow
    
    try {
        Stop-Service -Name $ServiceName -Force
        Write-Host "Service stopped successfully" -ForegroundColor Green
        Show-ServiceStatus
    } catch {
        Write-Host "Failed to stop service: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    exit 0
}

# Handle restart request
if ($Restart) {
    Write-Host "Restarting $ServiceDisplayName..." -ForegroundColor Yellow
    
    try {
        Restart-Service -Name $ServiceName -Force
        Write-Host "Service restarted successfully" -ForegroundColor Green
        Show-ServiceStatus
    } catch {
        Write-Host "Failed to restart service: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    exit 0
}

# Default: Install service
Write-Host "Installing $ServiceDisplayName..." -ForegroundColor Green
Write-Host "Service Path: $ServicePath" -ForegroundColor Gray

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists. Stopping and removing first..." -ForegroundColor Yellow
    
    if ($existingService.Status -eq "Running") {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 3
    }
    
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Install the service
Write-Host "Creating service..." -ForegroundColor Yellow
sc.exe create $ServiceName binPath= $ServicePath DisplayName= $ServiceDisplayName start= auto

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to create service" -ForegroundColor Red
    exit 1
}

# Set service description
sc.exe description $ServiceName $ServiceDescription

# Configure service recovery options
Write-Host "Configuring service recovery options..." -ForegroundColor Yellow
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000

# Grant Log on as a service right (requires additional setup in production)
Write-Host "Service installed successfully!" -ForegroundColor Green

# Show current status
Show-ServiceStatus

Write-Host ""
Write-Host "Service Installation Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Management Commands:" -ForegroundColor Cyan
Write-Host "  Start Service:   .\install-service.ps1 -Start" -ForegroundColor White
Write-Host "  Stop Service:    .\install-service.ps1 -Stop" -ForegroundColor White
Write-Host "  Restart Service: .\install-service.ps1 -Restart" -ForegroundColor White
Write-Host "  Service Status:  .\install-service.ps1 -Status" -ForegroundColor White
Write-Host "  Uninstall:       .\install-service.ps1 -Uninstall" -ForegroundColor White
Write-Host ""
Write-Host "The service is configured to start automatically on system boot." -ForegroundColor Yellow
Write-Host "You can also manage it through Services.msc or PowerShell Get-Service commands." -ForegroundColor Yellow
Write-Host ""

# Ask if user wants to start the service now
$response = Read-Host "Would you like to start the service now? (y/N)"
if ($response -match '^[Yy]') {
    Write-Host "Starting service..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 2
    Show-ServiceStatus
}