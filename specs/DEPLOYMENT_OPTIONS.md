# SmartBox Next - Deployment-Optionen & Lizenzierung

## Deployment-Varianten

### 1. SmartBox Appliance (Hardware + Software)
- Fertige Box mit 10" Touch
- Windows 10 IoT Enterprise LTSC
- Vorinstalliert & vorkonfiguriert
- **Preis**: 2.999€ - 4.999€

### 2. SmartBox Software (Windows-Installer)
- Läuft auf jedem Windows 10/11 PC
- Bring Your Own Hardware
- Ideal für bestehende PCs in Praxen
- **Preis**: 999€ - 1.999€

### 3. SmartBox DICOMizer (Light Version)
- Nur DICOM-Konvertierung
- Keine Live-Capture
- Batch-Processing
- **Preis**: 499€

### 4. SmartBox Cloud (SaaS)
- Web-basiert
- Keine Installation
- Monthly Subscription
- **Preis**: 99€/Monat

## Windows-First Strategie

### Vorteile
- **Marktdurchdringung**: 90% der Praxen nutzen Windows
- **Hardware-Support**: Beste Treiber-Unterstützung
- **Grabberkarten**: DirectShow/Media Foundation
- **IT-Akzeptanz**: Bekannte Umgebung

### Technische Umsetzung
```go
// Wails unterstützt Windows nativ
// main.go
//go:build windows

package main

import (
    "github.com/wailsapp/wails/v2"
    "smartbox/internal/capture/windows"
)

func main() {
    // Windows-spezifische Features
    capture := windows.NewDirectShowCapture()
    // ...
}
```

## Lizenzmanagement-System

### 1. Lizenztypen
```yaml
license_types:
  - TRIAL:        # 30 Tage, alle Features
  - BASIC:        # Einzelplatz, Basis-Features  
  - PROFESSIONAL: # Einzelplatz, alle Features
  - ENTERPRISE:   # Mehrplatz, Central Management
  - OEM:          # Für Hardware-Partner
```

### 2. Technische Implementierung

#### Online-Aktivierung (Preferred)
```go
type License struct {
    ID          string    `json:"id"`
    Type        string    `json:"type"`
    Customer    string    `json:"customer"`
    Features    []string  `json:"features"`
    ValidUntil  time.Time `json:"valid_until"`
    Signature   string    `json:"signature"`
}

// Aktivierung
POST https://license.smartbox-next.com/activate
{
    "license_key": "SBNX-XXXX-XXXX-XXXX",
    "hardware_id": "{{machine_fingerprint}}",
    "version": "1.0.0"
}
```

#### Offline-Aktivierung (Fallback)
```
1. Generate Request File
2. Send to license@smartbox-next.com
3. Receive License File
4. Import License File
```

### 3. Hardware-Fingerprinting
```go
func GetMachineID() string {
    // Kombination aus:
    // - CPU ID
    // - Motherboard Serial
    // - Primary MAC Address
    // - Windows Product ID
    
    hash := sha256.Sum256([]byte(
        cpuID + motherboardSerial + macAddress + windowsID,
    ))
    return base64.URLEncoding.EncodeToString(hash[:])
}
```

### 4. Feature-Flags
```go
type Features struct {
    MaxCameras      int  `json:"max_cameras"`
    VideoCapture    bool `json:"video_capture"`
    DicomExport     bool `json:"dicom_export"`
    WorklistQuery   bool `json:"worklist_query"`
    RemoteAccess    bool `json:"remote_access"`
    CloudBackup     bool `json:"cloud_backup"`
    AIFeatures      bool `json:"ai_features"`
    CustomBranding  bool `json:"custom_branding"`
}
```

## Lizenz-UI Integration

### In-App Lizenzmanagement
```
┌─────────────────────────────────────┐
│ SmartBox Next - Lizenzinformation   │
├─────────────────────────────────────┤
│ Status: ✅ Aktiviert                │
│ Typ: Professional                   │
│ Kunde: Praxis Dr. Müller           │
│ Gültig bis: 31.12.2025            │
├─────────────────────────────────────┤
│ Features:                           │
│ ✅ Unbegrenzte Kameras             │
│ ✅ Video-Aufzeichnung              │
│ ✅ DICOM Export                    │
│ ✅ Worklist-Abfrage                │
│ ❌ Cloud-Backup (Upgrade?)         │
├─────────────────────────────────────┤
│ [Lizenz ändern] [Upgrade] [Support] │
└─────────────────────────────────────┘
```

## Anti-Piracy Maßnahmen

### Soft Protection
- Online-Aktivierung erforderlich
- Periodische Validierung (30 Tage)
- Hardware-Binding
- Lizenz-Wasserzeichen in DICOM

### Hard Protection (Optional)
- USB-Dongle für Enterprise
- TPM-Integration
- Code-Obfuscation
- Anti-Debugging

### Fair Use Policy
- 3 Aktivierungen pro Lizenz
- Hardware-Wechsel erlaubt (Support-Ticket)
- Backup-Lizenz für Notfälle

## Preismodell (Software-Only)

### SmartBox DICOMizer
- **Features**: Import, Convert, Export
- **Preis**: 499€ Einmalig
- **Support**: Community

### SmartBox Professional  
- **Features**: Live Capture, DICOM, Worklist
- **Preis**: 999€ Einmalig + 199€/Jahr Updates
- **Support**: Email

### SmartBox Enterprise
- **Features**: Alles + Central Management
- **Preis**: 1.999€ + 399€/Jahr
- **Support**: Phone + Remote

### Volume Licensing
- 5+ Lizenzen: 20% Rabatt
- 10+ Lizenzen: 30% Rabatt  
- 25+ Lizenzen: Individual Pricing

## Distribution

### 1. Direct Download
- Trial Version (30 Tage)
- Sofort-Aktivierung nach Kauf
- Auto-Update Mechanismus

### 2. Microsoft Store (Optional)
- Größere Reichweite
- Vertrauen durch MS-Prüfung
- 30% Revenue Share 😢

### 3. Partner Channel
- Medical IT Distributoren
- Hardware-Bundling
- White-Label Option

## Beispiel-Lizenzschlüssel
```
SBNX-PRO1-A7K9-M2P5-Q8R3
│    │    └─── Prüfsumme
│    └──────── Zufallscode  
└───────────── Produkttyp
```

## ROI für Kunden
"Die Software zahlt sich nach 10 Untersuchungen selbst"
- Zeitersparnis: 5 Min/Untersuchung
- Weniger Fehler durch Automation
- Keine teure Hardware nötig

*"Why buy a 20k€ box when 999€ software does it better?"*