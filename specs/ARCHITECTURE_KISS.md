# SmartBox Next - KISS Architecture
*© 2025 Claude's Improbably Reliable Software Solutions*

## Core Design Principles

### 1. Standalone First, Network Ready
- **Primär**: 10" Touchscreen direkt am Gerät
- **Sekundär**: Web-Interface für Remote/Config
- **Kein zentraler Server nötig** - jede Box ist autonom

### 2. Zero-Config Discovery
```
SmartBox ─┬─> mDNS Broadcast ("_smartbox._tcp")
          ├─> Auto-Discovery von PACS
          └─> Auto-Discovery anderer SmartBoxen
```

### 3. Embedded Web-Server
- **Lokal**: http://localhost:8080
- **Netzwerk**: http://smartbox-[serial].local
- **Same UI** für Touch und Web

## Technische Architektur

```
┌─────────────────────────────────────┐
│      10" Touch UI (Kiosk Mode)      │
│         Vue 3 + Tailwind            │
└──────────────┬──────────────────────┘
               │ 
┌──────────────▼──────────────────────┐
│    Embedded Web Server (Go)         │
│  ┌────────────────────────────┐     │
│  │   REST API + WebSocket     │     │
│  └────────────────────────────┘     │
├─────────────────────────────────────┤
│         Core Services               │
│  ┌─────────┐ ┌──────────┐ ┌──────┐ │
│  │ Capture │ │  DICOM   │ │Config│ │
│  │ Engine  │ │  Engine  │ │ Mgmt │ │
│  └─────────┘ └──────────┘ └──────┘ │
├─────────────────────────────────────┤
│      Hardware Abstraction           │
│  ┌─────────┐ ┌──────────┐ ┌──────┐ │
│  │ Webcam  │ │ Grabber  │ │ PCIe │ │
│  └─────────┘ └──────────┘ └──────┘ │
└─────────────────────────────────────┘
```

## UI Design (Touch-First)

### Main Screen (Capture Mode)
```
┌─────────────────────────────────────┐
│ [≡] SmartBox          [⚙] [?] [🔴] │
├─────────────────────────────────────┤
│                                     │
│         LIVE VIDEO PREVIEW          │
│           (1920x1080)               │
│                                     │
├─────────────────────────────────────┤
│ Patient: Mustermann, Max            │
│ Study: Endoskopie                   │
├─────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────┐ │
│  │ CAPTURE │  │  VIDEO  │  │SERIES│ │
│  │   📷    │  │   🎥    │  │  📁  │ │
│  └─────────┘  └─────────┘  └─────┘ │
└─────────────────────────────────────┘
```

### Config Screen (Web-Based)
- Gleiche UI lokal und remote
- Responsive für Tablet/Desktop
- Dark Mode für OP-Umgebung

## Deployment Scenarios

### 1. Standalone Box
```bash
# Auto-Start on Boot
systemctl enable smartbox
# Kiosk Mode
startx /usr/bin/smartbox --kiosk
```

### 2. Networked Setup
```yaml
# /etc/smartbox/config.yaml
network:
  enable_discovery: true
  enable_remote: true
  allowed_ips: ["10.0.0.0/8"]
```

### 3. Central Management (Optional)
- Jede Box bleibt autonom
- Central Dashboard aggregiert nur Status
- Kein Single Point of Failure

## KISS Implementation

### Phase 1: Core (1 Woche)
```go
// main.go - That's it!
package main

import (
    "github.com/wailsapp/wails/v2"
    "smartbox/internal/capture"
    "smartbox/internal/dicom"
    "smartbox/internal/web"
)

func main() {
    app := wails.CreateApp(&wails.AppConfig{
        Title:     "SmartBox",
        Width:     1024,
        Height:    768,
        Frameless: true, // Kiosk Mode
    })
    
    // Services
    captureService := capture.New()
    dicomService := dicom.New()
    webServer := web.New(":8080")
    
    // Start
    go webServer.Start()
    app.Run()
}
```

### Konkrete Features vs. Konkurrenz

| Feature | Sony/Olympus | SmartBox Next |
|---------|--------------|---------------|
| Boot Zeit | 2-3 Min | < 30 Sek |
| UI | Kompliziert | One-Touch |
| Config | Techniker | Self-Service |
| Updates | Service-Besuch | Auto-OTA |
| Preis | 10-20k€ | < 3k€ |

## Warum es funktioniert

1. **Keine Abhängigkeiten** - Läuft überall
2. **Ein Binary** - Deploy = Copy
3. **Web Standards** - UI überall gleich
4. **Go Simplicity** - Wartbar, schnell
5. **CIRSS Quality** - It just works™

## Next Steps

1. Wails Projekt mit Kiosk-Mode
2. Basic Capture (Webcam first)
3. DICOM Export (aus CamBridge)
4. Touch UI (große Buttons!)
5. Web Config Interface

*"Complexity is the enemy of reliability" - CIRSS Motto*