# Windows Resilience Configuration für SmartBox-Next

## Kritische Erkenntnisse aus der Research

### 1. Windows Version
- **Windows 11 IoT Enterprise LTSC 2024** (10 Jahre Support!)
- Kein Windows PE (72h Limit)
- Fixed-purpose device Lizenz

### 2. RAM-Based Operation
- **8GB RAM Disk** mit ImDisk
- 10,000+ MB/s Read/Write Speed
- Auto-Backup alle 5 Minuten
- SmartBox-Next Queue/Temp auf RAM Disk

### 3. Write Protection (UWF)
- System Drive geschützt
- 8GB Disk-based Overlay
- Exclusions für:
  - ~/SmartBoxNext/DICOM/
  - ~/SmartBoxNext/Queue/
  - ~/SmartBoxNext/config.json

### 4. Power Failure Recovery
- BIOS: Auto-Power-On nach Stromausfall
- Write Cache: DISABLED auf allen Disks
- Fast Startup: Enabled
- Boot Zeit: 5-8 Sekunden!

### 5. Kiosk Mode
- Shell Launcher statt Explorer
- SmartBox-Next als Shell
- Keine Windows Dialoge/Errors
- Auto-Login als "MedicalDevice" User

### 6. Watchdog Service
```powershell
# Überwacht SmartBox-Next
# Startet neu bei Crash
# Logging für Compliance
```

### 7. Hardware Requirements
- 32GB RAM (16GB System + 16GB RAM Disk)
- NVMe SSD für OS
- Industrial SSD für Daten
- Medical-grade UPS

## Implementation für SmartBox-Next

### Phase 1: Basis Setup
1. Windows 11 IoT Enterprise LTSC installieren
2. RAM Disk einrichten (8GB)
3. UWF konfigurieren mit Exclusions
4. Auto-Login einrichten

### Phase 2: SmartBox Integration
1. Queue auf RAM Disk: `R:\SmartBoxNext\Queue\`
2. Temp Files auf RAM Disk: `R:\SmartBoxNext\Temp\`
3. Backup Service alle 5 Min zu `C:\SmartBoxNext\`
4. Watchdog für SmartBox-Next.exe

### Phase 3: Hardening
1. Shell Launcher aktivieren
2. Error Dialoge deaktivieren
3. Boot optimieren (<10 Sek)
4. Power Recovery testen

### Critical Scripts Needed
1. **CONFIGURE_SMARTBOX_RESILIENT.ps1**
2. **SETUP_RAM_DISK.ps1**
3. **CONFIGURE_UWF_EXCLUSIONS.ps1**
4. **WATCHDOG_SERVICE.ps1**

## SmartBox-Next Anpassungen

### Config Paths Update
```go
// RAM Disk Paths
const (
    QueuePath = "R:\\SmartBoxNext\\Queue\\"
    TempPath  = "R:\\SmartBoxNext\\Temp\\"
    BackupPath = "C:\\SmartBoxNext\\Backup\\"
)
```

### Watchdog Integration
- SmartBox muss Heartbeat schreiben
- Watchdog prüft alle 30 Sek
- Auto-Restart bei Ausfall

### Compliance Logging
- Alle Actions loggen
- 7 Jahre Aufbewahrung
- Audit Trail für FDA

## Testing Checklist
- [ ] Stromkabel ziehen während Capture
- [ ] Boot Zeit < 10 Sekunden
- [ ] Kein Windows sichtbar
- [ ] Auto-Start SmartBox
- [ ] Queue überlebt Neustart
- [ ] RAM Disk Performance
- [ ] UWF Protection aktiv
- [ ] Watchdog funktioniert

## Nächste Schritte
1. PowerShell Scripts erstellen
2. Test-System aufsetzen
3. Performance messen
4. FDA Compliance dokumentieren