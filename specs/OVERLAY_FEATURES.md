# SmartBox Next - Overlay Features

## Patient Information Overlay

### Overlay-Modi
1. **Burned-In**: Permanent im Bild (DICOM Secondary Capture)
2. **DICOM Overlay**: Als separate DICOM Overlay Plane (60xx Tags)
3. **Presentation State**: DICOM GSPS (kann ein/ausgeblendet werden)
4. **Live Preview Only**: Nur in der Vorschau, nicht im Export

### Standard-Overlays

#### 1. Patient Demographics
```
┌─────────────────────────────────────┐
│ Mustermann, Max (*15.03.1965)       │
│ Pat-ID: 12345678 | m | 58J         │
│ Endoskopie Oberer GI-Trakt         │
│ Dr. Schmidt | 05.01.2025 14:32      │
└─────────────────────────────────────┘
```

#### 2. Minimal Corner Display
```
Mustermann, M. | 12345678
05.01.2025 14:32:15
```

#### 3. Medical Measurement Tools
- Ruler/Scale Bar
- Grid Overlay
- Angle Measurement
- Distance Measurement
- Area Calculation

#### 4. Procedure Information
```
┌─────────────────────────────────────┐
│ Gastroskopie                        │
│ Gerät: Olympus GIF-H290             │
│ Lokalisation: Antrum                │
│ Befund: Erosive Gastritis Typ B    │
└─────────────────────────────────────┘
```

### Konfigurierbare Overlay-Elemente

```yaml
overlay_config:
  position: top_left  # top_left, top_right, bottom_left, bottom_right
  opacity: 0.9        # 0.0 - 1.0
  font_size: 14       # in points
  font_color: white
  background: "rgba(0,0,0,0.7)"
  
  elements:
    - patient_name:
        format: "LAST, First"
        include_birthdate: true
    
    - patient_id:
        prefix: "ID: "
        
    - timestamp:
        format: "DD.MM.YYYY HH:mm:ss"
        timezone: "local"
        
    - procedure:
        show_device: true
        show_physician: true
        
    - custom_text:
        lines:
          - "Klinikum Beispielstadt"
          - "Endoskopie-Abteilung"
          
    - measurements:
        show_scale: true
        units: "mm"
        
    - logo:
        file: "clinic_logo.png"
        position: "bottom_right"
        size: "100x50"
```

### Technische Implementierung

#### 1. Real-Time Overlay (Go + Image Processing)
```go
package overlay

import (
    "image"
    "image/draw"
    "github.com/golang/freetype"
    "github.com/golang/freetype/truetype"
)

type OverlayRenderer struct {
    font     *truetype.Font
    config   OverlayConfig
}

func (r *OverlayRenderer) RenderOverlay(img image.Image, patient PatientInfo) image.Image {
    // Create RGBA copy
    bounds := img.Bounds()
    rgba := image.NewRGBA(bounds)
    draw.Draw(rgba, bounds, img, bounds.Min, draw.Src)
    
    // Create overlay context
    ctx := freetype.NewContext()
    ctx.SetDst(rgba)
    ctx.SetFont(r.font)
    ctx.SetFontSize(r.config.FontSize)
    
    // Render background box
    overlayBounds := r.calculateOverlayBounds(patient)
    r.drawBackground(rgba, overlayBounds)
    
    // Render text elements
    y := overlayBounds.Min.Y + 20
    
    // Patient Name
    if r.config.ShowPatientName {
        text := formatPatientName(patient)
        ctx.DrawString(text, freetype.Pt(overlayBounds.Min.X+10, y))
        y += 20
    }
    
    // Patient ID
    if r.config.ShowPatientID {
        text := fmt.Sprintf("ID: %s", patient.ID)
        ctx.DrawString(text, freetype.Pt(overlayBounds.Min.X+10, y))
        y += 20
    }
    
    // Timestamp
    if r.config.ShowTimestamp {
        text := time.Now().Format("02.01.2006 15:04:05")
        ctx.DrawString(text, freetype.Pt(overlayBounds.Min.X+10, y))
    }
    
    return rgba
}

// Für Video: Frame-by-Frame Processing
func (r *OverlayRenderer) ProcessVideoFrame(frame []byte, patient PatientInfo) []byte {
    img := decodeFrame(frame)
    overlayed := r.RenderOverlay(img, patient)
    return encodeFrame(overlayed)
}
```

#### 2. DICOM Overlay Plane (60xx Tags)
```go
func AddDICOMOverlay(dataset *dicom.Dataset, overlayData []byte) {
    // Overlay Plane 0 (6000,xxxx)
    dataset.Add(dicom.Tag{0x6000, 0x0010}, "US", []int{1, 1}) // Rows
    dataset.Add(dicom.Tag{0x6000, 0x0011}, "US", []int{2048, 2048}) // Columns
    dataset.Add(dicom.Tag{0x6000, 0x0040}, "CS", []string{"G"}) // Type (Graphics)
    dataset.Add(dicom.Tag{0x6000, 0x0050}, "SS", []int{1, 1}) // Origin
    dataset.Add(dicom.Tag{0x6000, 0x0100}, "US", []int{1}) // Bits Allocated
    dataset.Add(dicom.Tag{0x6000, 0x0102}, "US", []int{0}) // Bit Position
    dataset.Add(dicom.Tag{0x6000, 0x3000}, "OW", overlayData) // Overlay Data
}
```

#### 3. Privacy-Aware Overlays
```go
type PrivacyMode int

const (
    PrivacyNone PrivacyMode = iota
    PrivacyInitials    // "Mustermann, M."
    PrivacyID          // Nur Pat-ID
    PrivacyAnonymous   // "Patient 1"
)

func (r *OverlayRenderer) SetPrivacyMode(mode PrivacyMode) {
    r.config.PrivacyMode = mode
}
```

### UI Integration

```vue
<template>
  <div class="overlay-config">
    <h3>Overlay-Einstellungen</h3>
    
    <div class="overlay-preview">
      <img :src="previewImage" />
      <div class="overlay" :style="overlayStyle">
        {{ overlayText }}
      </div>
    </div>
    
    <div class="controls">
      <label>
        <input type="checkbox" v-model="config.burnIn">
        Permanent einbrennen
      </label>
      
      <label>
        <input type="checkbox" v-model="config.showName">
        Patientenname
      </label>
      
      <label>
        <input type="checkbox" v-model="config.showID">
        Patienten-ID
      </label>
      
      <label>
        <input type="checkbox" v-model="config.showTime">
        Zeitstempel
      </label>
      
      <select v-model="config.position">
        <option value="top-left">Oben Links</option>
        <option value="top-right">Oben Rechts</option>
        <option value="bottom-left">Unten Links</option>
        <option value="bottom-right">Unten Rechts</option>
      </select>
      
      <input type="range" v-model="config.opacity" 
             min="0" max="100" step="10">
      
      <input type="color" v-model="config.textColor">
    </div>
  </div>
</template>
```

### Spezial-Features

#### 1. QR-Code Overlay
```go
// QR-Code mit Patient-ID + Study-UID
qrData := fmt.Sprintf("DICOM:%s:%s", patientID, studyUID)
qrCode := generateQRCode(qrData)
r.drawQRCode(img, qrCode, position)
```

#### 2. Anatomische Marker
```yaml
anatomical_markers:
  endoscopy:
    - "Ösophagus 25cm"
    - "Z-Linie"
    - "Pylorus"
  colonoscopy:
    - "Rektum"
    - "Sigma"
    - "Coecum"
```

#### 3. Live-Annotations
- Freihand-Zeichnen während Capture
- Pfeile und Markierungen
- Text-Annotationen
- Speicherung als DICOM Presentation State

### Compliance & Legal

#### Burned-In Overlays
- ⚠️ Permanent - kann nicht entfernt werden
- ⚠️ Datenschutz beachten
- ✅ Gut für Dokumentation
- ❌ Schlecht für Anonymisierung

#### Best Practice
1. **Default**: DICOM Overlay (kann deaktiviert werden)
2. **Option**: Burn-In für spezielle Fälle
3. **Privacy**: Anonymisierungs-Modus verfügbar
4. **Audit**: Log wer Overlays aktiviert/deaktiviert

### Performance-Optimierung

```go
// GPU-beschleunigte Overlays für Video
type GPUOverlayRenderer struct {
    shader *ComputeShader
}

func (r *GPUOverlayRenderer) RenderBatch(frames []Frame) {
    // Batch-Processing auf GPU
    r.shader.Execute(frames)
}
```

*"Information where you need it, when you need it"*