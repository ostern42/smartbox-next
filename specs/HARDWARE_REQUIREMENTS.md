# Hardware Requirements für SmartBox Next

## Minimum Requirements (Embedded Appliance)

### CPU
- **x86_64**: Intel Atom x5-Z8350 oder besser
- **ARM**: Raspberry Pi 4 (4GB) oder besser
- **Cores**: Minimum 2, empfohlen 4
- **Takt**: Minimum 1.5 GHz

### RAM
- **Minimum**: 2 GB
- **Empfohlen**: 4 GB
- **Optimal**: 8 GB (für 4K Video)

### Storage
- **System**: 8 GB (OS + Application)
- **Buffer**: 32 GB (für Video-Zwischenspeicherung)
- **Type**: eMMC oder SSD (keine SD-Karten für Production)

### Graphics
- **Minimum**: Intel HD Graphics oder equivalent
- **Video Decode**: H.264 Hardware-Dekodierung
- **Video Encode**: H.264 Hardware-Enkodierung (optional aber empfohlen)

### Capture Hardware

#### Integrierte Optionen
- USB 2.0/3.0 für externe Grabber
- Mini-PCIe Slot für Grabberkarten

#### Unterstützte Grabberkarten
1. **Yuan PCIe Karten**
   - SC542N4 SDI (4-Channel SDI)
   - SC512N1 SDI (Single SDI)
   - MiniPCIe Varianten

2. **USB Grabber**
   - Elgato Cam Link 4K
   - AVerMedia BU110
   - Generic UVC-compliant devices

3. **Professionelle Interfaces**
   - SDI Input (HD/3G/6G)
   - DVI-D Input
   - HDMI Input (mit HDCP Stripping)
   - Composite/S-Video (Legacy)

### Network
- **Ethernet**: Gigabit (1000BASE-T)
- **WiFi**: Optional, 802.11ac
- **Protokolle**: TCP/IP für DICOM

## Formfaktoren

### Option 1: All-in-One Medical Panel PC
- 15-24" Touchscreen
- Fanless Design
- IP65 Front (Medical Grade)
- VESA Mount
- Beispiel: Advantech POC-W242

### Option 2: Mini PC Box
- Lüfterlos
- DIN-Rail montierbar
- Größe: < 200x200x50mm
- Beispiel: Intel NUC Medical

### Option 3: Embedded Board
- Single Board Computer
- Carrier Board mit I/O
- Custom Gehäuse
- Beispiel: Kontron pITX

## Betriebsumgebung

### Temperatur
- Betrieb: 0°C bis 40°C
- Lagerung: -20°C bis 60°C

### Luftfeuchtigkeit
- 10% bis 90% nicht-kondensierend

### Medizinische Standards
- IEC 60601-1 (Medical Electrical Equipment)
- EMC: IEC 60601-1-2
- Reinigbar/Desinfizierbar

## Power Requirements
- **Eingang**: 12-24V DC oder 100-240V AC
- **Verbrauch**: < 35W typisch, < 50W max
- **Medical PSU**: IEC 60601-1 compliant

## Zertifizierungen (für Production)
- CE Medical Device
- FDA 510(k) (wenn USA-Markt)
- RoHS compliant
- FCC Part 15 Class B

## Benchmark-Ziele
- Boot Zeit: < 30 Sekunden
- Capture Latency: < 100ms
- DICOM Export: < 5s für 1080p Bild
- Video Encoding: Realtime für 1080p30

## Kostenrahmen (Hardware Only)
- **Low-End**: 500-800€ (Basic Atom/ARM)
- **Mid-Range**: 800-1500€ (i5 equivalent)
- **High-End**: 1500-3000€ (Medical Grade)

## Beispielkonfiguration (Empfohlen)

### SmartBox Next Standard
- **CPU**: Intel i5-8365UE (Embedded)
- **RAM**: 8GB DDR4
- **Storage**: 128GB M.2 SSD
- **Capture**: Yuan SC512N1-L (Mini-PCIe SDI)
- **Network**: Intel I219-LM Gigabit
- **Gehäuse**: Fanless Medical Box
- **OS**: Windows 10 IoT Enterprise LTSC

**Geschätzte Kosten**: ~1200€