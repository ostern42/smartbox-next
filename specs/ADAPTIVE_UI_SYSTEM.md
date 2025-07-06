# SmartBox Next - Adaptive UI System

## 🎨 Dynamisches UI-Layout System

### Core Concept
Vollständig anpassbare Benutzeroberfläche, die sich an verschiedene Nutzungsszenarien, Umgebungen und Benutzerpräferenzen anpasst.

### Features

#### 1. Responsive Scaling System
```typescript
interface UIScaling {
  // Global Scaling
  globalScale: number;        // 0.5x - 2.0x
  
  // Individual Elements
  buttonScale: number;        // 0.8x - 3.0x
  fontScale: number;          // 0.8x - 2.5x
  iconScale: number;          // 0.8x - 3.0x
  
  // Touch Targets
  minTouchTarget: number;     // 44px - 88px
  
  // Spacing
  compactMode: boolean;       // Reduzierte Abstände
  spacingMultiplier: number;  // 0.5x - 2.0x
}
```

#### 2. Preset Themes

##### Endoskopie-Modus
```yaml
endoscopy_preset:
  theme: dark
  layout: horizontal_split
  primary_view: live_video
  secondary_view: patient_info
  quick_actions:
    - capture_image
    - start_video
    - switch_source
    - patient_overlay
  button_size: large
  font_size: medium
  colors:
    primary: "#0066CC"
    background: "#1a1a1a"
    text: "#ffffff"
```

##### OP-Modus
```yaml
surgery_preset:
  theme: high_contrast
  layout: fullscreen_video
  overlay_position: minimal_corner
  quick_actions:
    - capture_burst
    - mark_critical
    - voice_note
    - stream_toggle
  button_size: extra_large
  font_size: large
  sterile_mode: true  # Größere Touch-Targets
  colors:
    primary: "#00AA44"
    background: "#000000"
    text: "#00FF00"
```

##### Funktionsdiagnostik-Modus
```yaml
diagnostic_preset:
  theme: light
  layout: grid_4x4
  multi_source: true
  quick_actions:
    - source_1_4
    - measurement_tools
    - comparison_mode
    - report_generate
  button_size: medium
  font_size: small
  info_density: high
  colors:
    primary: "#0080FF"
    background: "#f5f5f5"
    text: "#333333"
```

#### 3. Dark Mode & Umgebungsanpassung

##### Automatische Helligkeitsanpassung
```typescript
interface EnvironmentAdaptation {
  // Sensoren
  ambientLight: number;       // Lux-Wert
  timeOfDay: Date;           // Für Auto-Dark-Mode
  
  // Modi
  darkMode: 'on' | 'off' | 'auto';
  nightShift: boolean;       // Reduziert Blauanteil
  
  // Kontrast
  contrastMode: 'normal' | 'high' | 'ultra';
  
  // Spezial-Modi
  surgeryLights: boolean;    // Extra-Dunkel für OP-Lampen
  redLightMode: boolean;     // Für Dunkeladaptation
}
```

### UI Layout Engine

#### Drag & Drop Layout Editor
```vue
<template>
  <div class="layout-editor">
    <GridLayout 
      v-model="layout"
      :col-num="12"
      :row-height="30"
      :is-draggable="editMode"
      :is-resizable="editMode"
    >
      <GridItem
        v-for="item in layout"
        :key="item.i"
        :x="item.x"
        :y="item.y"
        :w="item.w"
        :h="item.h"
      >
        <component :is="item.component" />
      </GridItem>
    </GridLayout>
    
    <FloatingActionButton
      v-if="editMode"
      @click="saveLayout"
      icon="save"
    />
  </div>
</template>
```

#### Verfügbare UI-Komponenten
```typescript
enum UIComponents {
  // Video/Bild
  LivePreview = 'live-preview',
  MultiSourceGrid = 'multi-source-grid',
  Thumbnail = 'thumbnail-strip',
  
  // Kontrollen
  CaptureControls = 'capture-controls',
  SourceSelector = 'source-selector',
  QuickActions = 'quick-actions',
  
  // Information
  PatientInfo = 'patient-info',
  StudyDetails = 'study-details',
  SystemStatus = 'system-status',
  
  // Tools
  MeasurementTools = 'measurement-tools',
  AnnotationPanel = 'annotation-panel',
  VoiceControl = 'voice-control',
}
```

### Accessibility Features

#### 1. Skalierbare Schriften
```css
/* Fluid Typography */
:root {
  --font-scale: 1;
  --base-font: calc(16px * var(--font-scale));
  --heading-font: calc(24px * var(--font-scale));
  --button-font: calc(18px * var(--font-scale));
}

/* Nutzer kann ändern */
.font-size-slider {
  min: 0.8;
  max: 2.5;
  step: 0.1;
}
```

#### 2. Touch-Target-Anpassung
```typescript
interface TouchTargetConfig {
  minSize: number;           // Minimum 44x44px (WCAG)
  preferredSize: number;     // User preference
  
  // Verschiedene Modi
  precisionMode: boolean;    // Kleinere Targets für Stylus
  gloveMode: boolean;        // Größere Targets für OP-Handschuhe
  
  // Feedback
  hapticFeedback: boolean;
  audioFeedback: boolean;
  visualFeedback: 'ripple' | 'glow' | 'none';
}
```

### Speicherung & Profile

#### Benutzerprofile
```typescript
interface UserProfile {
  id: string;
  name: string;
  role: 'surgeon' | 'nurse' | 'technician';
  
  // UI Präferenzen
  preferredTheme: string;
  customLayouts: Layout[];
  quickActions: Action[];
  scaling: UIScaling;
  
  // Umgebung
  defaultDarkMode: boolean;
  colorBlindMode?: 'protanopia' | 'deuteranopia' | 'tritanopia';
  
  // Shortcuts
  keyboardShortcuts: KeyBinding[];
  gestureControls: GestureBinding[];
}
```

#### Layout-Sharing
```yaml
# Export/Import von Layouts
export_format:
  version: "1.0"
  name: "Dr. Schmidt Endoskopie Setup"
  description: "Optimiert für Gastroskopie"
  layout: {...}
  theme: {...}
  actions: [...]
```

### Implementation

#### Vue 3 Composable
```typescript
// useAdaptiveUI.ts
export function useAdaptiveUI() {
  const currentProfile = ref<UserProfile>()
  const activeTheme = ref<Theme>()
  const scaling = ref<UIScaling>()
  
  // Reaktive Anpassung
  const adaptToEnvironment = () => {
    const lux = getAmbientLight()
    if (lux < 10 && !darkMode.value) {
      showDarkModePrompt()
    }
  }
  
  // Gesture Support
  const enableGestures = () => {
    // Pinch to zoom
    // Swipe to change source
    // Long press for options
  }
  
  return {
    currentProfile,
    activeTheme,
    scaling,
    adaptToEnvironment,
    enableGestures
  }
}
```

### Beispiel-Szenarien

#### 1. Dunkler Untersuchungsraum
- Auto-Dark-Mode aktiviert
- Reduzierte Helligkeit
- Größere Buttons
- High-Contrast Borders

#### 2. Sterile OP-Umgebung
- Glove-Mode (größere Touch-Targets)
- Voice-Control prominent
- Minimale UI
- Kritische Funktionen im Vordergrund

#### 3. Multi-Source Diagnostik
- Grid-Layout
- Kleine Fonts für mehr Info
- Measurement Tools sichtbar
- Vergleichsmodus

### Performance-Optimierung

```typescript
// Layout-Änderungen mit CSS Custom Properties
// Keine Re-Renders nötig
document.documentElement.style.setProperty('--button-scale', scale);
document.documentElement.style.setProperty('--font-scale', fontScale);
```

*"One UI to rule them all, fully customizable for all"*