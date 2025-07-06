# SmartBox Next - Lizenzierungs-Implementation

## Quick Implementation mit go-license

### 1. License Server (Einfache Variante)
```go
// license-server/main.go
package main

import (
    "crypto/ed25519"
    "encoding/json"
    "time"
    "github.com/gin-gonic/gin"
)

type LicenseRequest struct {
    LicenseKey string `json:"license_key"`
    MachineID  string `json:"machine_id"`
    Version    string `json:"version"`
}

type LicenseResponse struct {
    License    SignedLicense `json:"license"`
    Message    string        `json:"message"`
}

type SignedLicense struct {
    // Lizenz-Daten
    ID         string    `json:"id"`
    Type       string    `json:"type"`
    Customer   string    `json:"customer"`
    MachineID  string    `json:"machine_id"`
    Features   Features  `json:"features"`
    IssuedAt   time.Time `json:"issued_at"`
    ExpiresAt  time.Time `json:"expires_at"`
    
    // Signatur
    Signature  string    `json:"signature"`
}

func main() {
    r := gin.Default()
    
    // Private key für Signierung (KEEP SECRET!)
    privateKey := loadPrivateKey()
    
    r.POST("/activate", func(c *gin.Context) {
        var req LicenseRequest
        c.BindJSON(&req)
        
        // Validiere License Key in Datenbank
        licenseInfo := validateLicenseKey(req.LicenseKey)
        if licenseInfo == nil {
            c.JSON(400, gin.H{"error": "Invalid license key"})
            return
        }
        
        // Erstelle signierte Lizenz
        license := SignedLicense{
            ID:        generateID(),
            Type:      licenseInfo.Type,
            Customer:  licenseInfo.Customer,
            MachineID: req.MachineID,
            Features:  getFeatures(licenseInfo.Type),
            IssuedAt:  time.Now(),
            ExpiresAt: time.Now().AddDate(1, 0, 0),
        }
        
        // Signiere mit Ed25519
        license.Signature = signLicense(license, privateKey)
        
        c.JSON(200, LicenseResponse{
            License: license,
            Message: "License activated successfully",
        })
    })
    
    r.Run(":8443") // HTTPS!
}
```

### 2. Client-Side Validation
```go
// internal/license/validator.go
package license

import (
    "crypto/ed25519"
    "encoding/json"
    "io/ioutil"
    "time"
)

var publicKey = ed25519.PublicKey{
    // Public Key (kann im Code sein)
    0x12, 0x34, 0x56, // ... 
}

type LicenseManager struct {
    currentLicense *SignedLicense
    machineID      string
}

func (lm *LicenseManager) ValidateLicense() error {
    // 1. Lade Lizenz von Disk
    data, err := ioutil.ReadFile(getLicensePath())
    if err != nil {
        return ErrNoLicense
    }
    
    var license SignedLicense
    json.Unmarshal(data, &license)
    
    // 2. Prüfe Signatur
    if !verifySignature(license, publicKey) {
        return ErrInvalidSignature
    }
    
    // 3. Prüfe Machine ID
    if license.MachineID != lm.machineID {
        return ErrWrongMachine
    }
    
    // 4. Prüfe Ablaufdatum
    if time.Now().After(license.ExpiresAt) {
        return ErrExpiredLicense
    }
    
    lm.currentLicense = &license
    return nil
}

func (lm *LicenseManager) HasFeature(feature string) bool {
    if lm.currentLicense == nil {
        return false
    }
    
    switch feature {
    case "video_capture":
        return lm.currentLicense.Features.VideoCapture
    case "dicom_export":
        return lm.currentLicense.Features.DicomExport
    // ...
    }
    return false
}
```

### 3. UI Integration
```go
// internal/ui/license_dialog.go
func ShowLicenseDialog(app *wails.App) {
    if !licenseManager.IsValid() {
        app.ShowDialog(wails.DialogOptions{
            Title: "Lizenz erforderlich",
            Message: "Bitte geben Sie Ihren Lizenzschlüssel ein",
            Buttons: []string{"Aktivieren", "Trial starten", "Abbrechen"},
        })
    }
}

// Feature-Gates in der UI
func (a *App) CaptureVideo() {
    if !a.license.HasFeature("video_capture") {
        a.ShowUpgradeDialog("Video-Aufnahme", "Professional")
        return
    }
    // ... capture video
}
```

### 4. Trial-Modus
```go
func (lm *LicenseManager) StartTrial() error {
    trial := SignedLicense{
        ID:        "TRIAL-" + generateID(),
        Type:      "TRIAL",
        Customer:  "Trial User",
        MachineID: lm.machineID,
        Features:  getAllFeatures(), // Alle Features!
        IssuedAt:  time.Now(),
        ExpiresAt: time.Now().AddDate(0, 0, 30), // 30 Tage
    }
    
    // Speichere Trial lokal
    return saveLicense(trial)
}
```

### 5. Offline-Aktivierung
```go
// 1. Generate Activation Request
func GenerateActivationRequest(licenseKey string) string {
    req := map[string]string{
        "license_key": licenseKey,
        "machine_id":  getMachineID(),
        "timestamp":   time.Now().Format(time.RFC3339),
    }
    
    data, _ := json.Marshal(req)
    return base64.StdEncoding.EncodeToString(data)
}

// 2. Process Activation Response
func ProcessActivationResponse(responseFile string) error {
    data, _ := ioutil.ReadFile(responseFile)
    
    var license SignedLicense
    json.Unmarshal(data, &license)
    
    return saveLicense(license)
}
```

## Einfaches Lizenz-Portal

### Admin Dashboard (Web)
```html
<!-- license-portal/index.html -->
<div class="container">
    <h1>SmartBox Lizenz-Portal</h1>
    
    <div class="stats">
        <div class="stat-card">
            <h3>Aktive Lizenzen</h3>
            <p class="number">247</p>
        </div>
        <div class="stat-card">
            <h3>Trial-Nutzer</h3>
            <p class="number">89</p>
        </div>
    </div>
    
    <div class="license-generator">
        <h2>Neue Lizenz erstellen</h2>
        <form>
            <input type="text" placeholder="Kundenname">
            <select>
                <option>Professional</option>
                <option>Enterprise</option>
            </select>
            <button>Lizenzschlüssel generieren</button>
        </form>
    </div>
</div>
```

## Lizenzschlüssel-Format
```
SBNX-PPPP-XXXX-XXXX-CCCC

SBNX: Prefix (SmartBox Next)
PPPP: Product Code
  PRO1: Professional
  ENT1: Enterprise  
  TRL1: Trial
XXXX: Random Bytes
CCCC: Checksum
```

## Anti-Crack Maßnahmen (Pragmatisch)

### Do's ✅
- Online-Aktivierung (einmalig)
- Hardware-Binding
- Regelmäßige Updates
- Feature-Flags statt Versions

### Don'ts ❌
- Übertriebener Kopierschutz
- Ständige Online-Checks
- Aggressive Anti-Debug
- Dongles (nervig!)

## Geschäftsmodell-Validierung

### Warum Kunden zahlen werden:
1. **Support**: Bei Medical wichtig
2. **Updates**: Neue Features, DICOM-Standards
3. **Compliance**: Zertifikate, Dokumentation
4. **Warranty**: Haftung bei Problemen
5. **Customization**: Anpassungen

### Trial → Conversion Strategy
- 30 Tage ALLE Features
- Sanfte Erinnerungen (Tag 7, 14, 25)
- Discount für frühe Aktivierung
- Export-Limit nach Trial (10 Bilder/Tag)

*"Make it so good they want to pay, not because they have to"*