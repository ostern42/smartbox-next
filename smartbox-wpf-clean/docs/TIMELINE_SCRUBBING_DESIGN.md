# SmartBox Timeline Scrubbing Design
## Ein innovatives Touch-Interface für medizinische Videobearbeitung

### Philosophie
Die meisten Touch-UIs sind langweilige Übersetzungen von Desktop-Metaphern. Wir nutzen die inhärente Analogie des Touchscreens für ein haptisches, intuitives Erlebnis.

## 1. Das Jogwheel-Konzept

### Physikalisches Modell
```
         ╭─────────────╮
         │   JOGWHEEL  │
         │  ╱─────╲    │
         │ ╱       ╲   │
         ││ ● ─ ─ ─ │  │  ← Rotationsachse
         │ ╲       ╱   │
         │  ╲─────╱    │
         │             │
         ╰─────────────╯
```

### Implementierung
- **Größe**: 200x200px (anpassbar basierend auf Bildschirmgröße)
- **Position**: Rechts unten über der Timeline, halbtransparent
- **Aktivierung**: Long-Press auf Timeline oder dedizierter Button

### Physik-Engine
```javascript
class JogwheelPhysics {
    constructor() {
        this.mass = 2.0;              // Trägheit des Rades
        this.friction = 0.95;         // Reibungskoeffizient
        this.springConstant = 0.1;    // Rückstellkraft zur Nullposition
        this.velocity = 0;            // Aktuelle Geschwindigkeit
        this.angle = 0;               // Aktuelle Rotation
        this.touchHistory = [];       // Für Momentum-Berechnung
    }
    
    // Haptisches Feedback bei bestimmten Positionen
    detents = [
        { angle: 0, strength: 0.8 },      // Nullposition
        { angle: Math.PI/2, strength: 0.3 },   // 90°
        { angle: Math.PI, strength: 0.3 },     // 180°
        { angle: -Math.PI/2, strength: 0.3 }   // -90°
    ];
}
```

### Interaktionsmodi
1. **Präzisionsmodus**: Langsame Drehung = Frame-genaue Navigation
2. **Schnellmodus**: Schnelle Drehung = Sprung über Sekunden/Minuten
3. **Momentum-Modus**: Schwungvolle Drehung mit Nachlauf
4. **Detent-Modus**: Magnetisches Einrasten bei wichtigen Zeitpunkten

## 2. Adaptive Timeline mit Echtzeit-Thumbnails

### Algorithmus für adaptive Zeitskala

```javascript
class AdaptiveTimeline {
    // Zeitstufen in Sekunden
    timeScales = [30, 60, 120, 180, 300, 600, 900, 1800, 3600];
    
    calculateOptimalScale(videoDuration, viewportWidth) {
        // Finde die kleinste Skala, die das gesamte Video anzeigt
        for (let scale of this.timeScales) {
            if (videoDuration <= scale) {
                return scale;
            }
        }
        // Für sehr lange Videos: Minuten-basierte Skalierung
        return Math.ceil(videoDuration / 60) * 60;
    }
    
    calculateThumbnailInterval(timeScale, viewportWidth, thumbnailWidth) {
        const maxThumbnails = Math.floor(viewportWidth / thumbnailWidth);
        const framesInView = timeScale * 25; // 25fps
        return Math.ceil(framesInView / maxThumbnails);
    }
}
```

### Intelligente Thumbnail-Verwaltung

```javascript
class ThumbnailCache {
    constructor() {
        this.cache = new Map();
        this.generationQueue = [];
        this.worker = new Worker('thumbnail-worker.js');
    }
    
    // Knuth-inspirierter Algorithmus für Thumbnail-Recycling
    getOptimalThumbnails(currentScale, previousScale) {
        if (previousScale === 30 && currentScale === 60) {
            // Verwende jedes zweite Thumbnail
            return this.decimateByFactor(2);
        } else if (previousScale === 60 && currentScale === 120) {
            // Verwende jedes zweite der verbleibenden
            return this.decimateByFactor(2);
        } else if (currentScale < previousScale) {
            // Zoom in: Interpoliere neue Thumbnails
            return this.interpolateThumbnails(previousScale / currentScale);
        }
    }
    
    decimateByFactor(factor) {
        // Behalte nur jedes n-te Thumbnail
        const kept = [];
        let index = 0;
        for (let [frame, thumbnail] of this.cache) {
            if (index % factor === 0) {
                kept.push([frame, thumbnail]);
            }
            index++;
        }
        return kept;
    }
}
```

### Timeline-Rendering

```javascript
class TimelineRenderer {
    render(timeScale, thumbnails) {
        // Adaptive Höhe basierend auf verfügbarem Platz
        const baseHeight = 80;
        const maxHeight = 120;
        const height = Math.min(maxHeight, baseHeight * (30 / timeScale));
        
        // Waveform-Overlay für Audio
        this.renderWaveform(height * 0.3);
        
        // Thumbnails mit intelligentem Preloading
        this.renderThumbnails(thumbnails, height * 0.7);
        
        // Zeitmarker
        this.renderTimeRuler(timeScale);
    }
}
```

## 3. Innovative Touch-Gesten

### "Pinch-to-Time" - Zeitachsen-Zoom
- **Zwei-Finger-Pinch**: Zoom in/out der Zeitachse
- **Drei-Finger-Pinch**: Zoom mit Beibehaltung der Playhead-Position

### "Flick-Scroll" - Momentum-basiertes Scrubbing
- **Schneller Wisch**: Timeline scrollt mit Momentum weiter
- **Doppel-Tap**: Springt zu nächstem Schnitt/Marker

### "Pressure-Scrub" (für druckempfindliche Displays)
- **Leichter Druck**: Frame-genaue Navigation
- **Starker Druck**: Schnelles Scrubbing

### "Orbit-Gesture" - Kreisförmige Navigation
```javascript
class OrbitGesture {
    // Zeichne einen Kreis um einen Punkt für zirkuläre Navigation
    // Größe des Kreises = Geschwindigkeit
    // Richtung = Vor/Zurück
    
    detectOrbit(touchPoints) {
        const center = this.calculateCenter(touchPoints);
        const radius = this.calculateRadius(touchPoints, center);
        const direction = this.calculateDirection(touchPoints);
        
        return {
            speed: radius * 0.1, // Größerer Kreis = schneller
            direction: direction // Uhrzeigersinn = vorwärts
        };
    }
}
```

## 4. Visuelles Feedback

### Jogwheel-Visualisierung
```css
.jogwheel {
    background: radial-gradient(
        circle at center,
        rgba(25, 118, 210, 0.1) 0%,
        rgba(25, 118, 210, 0.05) 50%,
        transparent 70%
    );
    border: 2px solid rgba(25, 118, 210, 0.3);
    box-shadow: 
        inset 0 0 20px rgba(25, 118, 210, 0.1),
        0 4px 12px rgba(0, 0, 0, 0.2);
}

.jogwheel-indicator {
    /* Zeigt aktuelle Rotation */
    background: linear-gradient(90deg, 
        transparent 0%, 
        #1976D2 50%, 
        transparent 100%
    );
    transform-origin: center;
    transition: none; /* Direkte Reaktion */
}
```

### Timeline-Übergänge
```css
.timeline-scale-transition {
    /* Sanfter Übergang zwischen Zeitskalen */
    animation: scaleSnap 0.3s cubic-bezier(0.4, 0.0, 0.2, 1);
}

@keyframes scaleSnap {
    0% { transform: scaleX(1); opacity: 1; }
    50% { transform: scaleX(0.95); opacity: 0.8; }
    100% { transform: scaleX(1); opacity: 1; }
}
```

## 5. Performance-Optimierungen

### Web Workers für Thumbnail-Generierung
```javascript
// thumbnail-worker.js
self.onmessage = function(e) {
    const { videoBlob, frameNumber, targetSize } = e.data;
    
    // Verwende OffscreenCanvas für Performance
    const canvas = new OffscreenCanvas(targetSize.width, targetSize.height);
    const ctx = canvas.getContext('2d');
    
    // Intelligentes Frame-Sampling
    extractFrame(videoBlob, frameNumber).then(frame => {
        ctx.drawImage(frame, 0, 0, targetSize.width, targetSize.height);
        
        // Konvertiere zu Blob für Caching
        canvas.convertToBlob({ 
            type: 'image/jpeg', 
            quality: 0.7 
        }).then(blob => {
            self.postMessage({ 
                frameNumber, 
                thumbnail: blob 
            });
        });
    });
};
```

### Predictive Loading
```javascript
class PredictiveLoader {
    predict(currentPosition, velocity, direction) {
        // Lade Thumbnails voraus basierend auf Bewegung
        const futurePosition = currentPosition + (velocity * direction * 2);
        const range = Math.abs(velocity) * 100; // Ladebereich
        
        return {
            start: futurePosition - range,
            end: futurePosition + range,
            priority: Math.abs(velocity) // Schnellere Bewegung = höhere Priorität
        };
    }
}
```

## 6. Implementierungs-Roadmap

### Phase 1: Basis-Timeline (1 Woche)
- Adaptive Zeitskalierung
- Einfache Thumbnail-Generierung
- Touch-basiertes Scrubbing

### Phase 2: Jogwheel (1 Woche)
- Physik-Engine
- Visuelle Implementierung
- Integration mit Timeline

### Phase 3: Erweiterte Gesten (3 Tage)
- Pinch-to-Time
- Momentum-Scrolling
- Orbit-Gesture

### Phase 4: Optimierung (3 Tage)
- Web Worker Integration
- Predictive Loading
- Performance-Tuning

## 7. Besondere Features

### "Szenen-Magnete"
Timeline erkennt Szenenwechsel und bietet magnetisches Einrasten beim Scrubbing.

### "Rhythmus-Modus"
Für medizinische Aufnahmen mit rhythmischen Bewegungen (z.B. Herzschlag): Timeline kann sich am Rhythmus orientieren.

### "Highlight-Spray"
Wichtige Momente werden visuell hervorgehoben - wie Goldstaub auf der Timeline.

## Zusammenfassung

Diese Implementierung nutzt die Stärken des Touchscreens:
- **Analog**: Jogwheel simuliert echte Hardware
- **Intuitiv**: Gesten folgen natürlichen Bewegungen
- **Responsiv**: Direktes haptisches Feedback
- **Effizient**: Intelligente Algorithmen minimieren Rechenaufwand
- **Elegant**: Klare visuelle Hierarchie ohne Überladung

Der Knuth'sche Ansatz: Elegante Algorithmen treffen auf durchdachtes Interface-Design.