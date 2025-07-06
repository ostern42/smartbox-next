# SmartBox Next - Remote Trigger System

## 🎮 Übersicht Remote-Auslösung

### Problem
- Endoskop-Tasten lösen über Dry Contact aus (Klinkenstecker/BNC)
- Touch-Auslösung in steriler Umgebung unpraktisch
- Kabel sind hinderlich im OP
- Verschiedene Hersteller = verschiedene Standards

### Lösung: Multi-Mode Trigger System

## 📡 Trigger-Optionen

### 1. Hardware Trigger Inputs

#### Standard Dry Contact Interface
```
┌─────────────────────────────┐
│  SmartBox Hardware Back     │
├─────────────────────────────┤
│ [TRIGGER 1] 3.5mm Jack      │ ← Foto/Single
│ [TRIGGER 2] 3.5mm Jack      │ ← Video Start/Stop
│ [BNC IN] BNC Connector      │ ← Alternative
│ [USB] Keyboard Emulation    │ ← Wireless Dongles
└─────────────────────────────┘
```

#### Elektrische Specs
```yaml
dry_contact:
  type: "Normally Open (NO)"
  voltage: "3.3V/5V tolerant"
  current: "< 10mA"
  debounce: "50ms software"
  isolation: "Optocoupler"
  
connectors:
  - 3.5mm TRS (Tip = Trigger, Sleeve = GND)
  - BNC (Center = Trigger, Shield = GND)
  - Phoenix Contact (Screw Terminal)
```

### 2. Wireless Solutions

#### A. Bluetooth LE Trigger
```typescript
interface BluetoothTrigger {
  // Kleine Button-Hardware
  device: "Nordic nRF52 based";
  battery: "CR2032 - 1 Jahr";
  range: "10m+";
  latency: "< 20ms";
  
  // Multi-Button Support
  buttons: {
    A: "Capture Photo",
    B: "Start/Stop Video",
    C: "Switch Source",
    D: "Custom Action"
  };
  
  // Beispiel-Hardware
  examples: [
    "Flic Smart Button",
    "SwitchBot",
    "Custom ESP32 Solution"
  ];
}
```

#### B. USB Wireless Dongles
```yaml
usb_wireless:
  # Funktastatur-Emulation (wie Olympus)
  type_1:
    name: "2.4GHz USB HID"
    example: "Logitech Unifying"
    pros: "Keine Treiber, sofort ready"
    cons: "Proprietär"
    
  # Presenter-Clicker
  type_2:
    name: "Presentation Remote"
    example: "Logitech R400"
    mapping:
      page_down: "Capture"
      page_up: "Video Toggle"
      
  # Gaming-Controller
  type_3:
    name: "Xbox Wireless Adapter"
    buttons: "Voll programmierbar"
    range: "Sehr gut"
```

#### C. WiFi/Network Trigger
```javascript
// REST API Trigger
POST /api/trigger/capture
{
  "action": "photo",
  "source": "endoscope_1",
  "metadata": {
    "trigger_source": "wireless_button_1",
    "timestamp": "2025-01-05T14:32:00Z"
  }
}

// WebSocket für Low-Latency
ws.send(JSON.stringify({
  type: "trigger",
  action: "video_start"
}));
```

### 3. Software Integration

#### Trigger Manager Service
```go
package trigger

type TriggerManager struct {
    // Hardware Monitoring
    gpioWatcher    *GPIOWatcher
    usbListener    *USBListener
    
    // Wireless
    bleServer      *BLEServer
    wifiServer     *HTTPServer
    wsServer       *WebSocketServer
    
    // Event Handling
    eventBus       *EventBus
    debouncer      *Debouncer
}

func (tm *TriggerManager) HandleTrigger(source TriggerSource, action TriggerAction) {
    // Debouncing
    if tm.debouncer.ShouldIgnore(source) {
        return
    }
    
    // Route to appropriate handler
    switch action {
    case PhotoCapture:
        tm.eventBus.Emit("capture:photo", source)
    case VideoToggle:
        tm.eventBus.Emit("capture:video:toggle", source)
    case CustomAction:
        tm.handleCustomAction(source)
    }
    
    // Feedback
    tm.provideFeedback(source, action)
}
```

#### GPIO Monitoring (Raspberry Pi Style)
```go
// Für Hardware-Trigger
func monitorGPIO() {
    pin := gpio.NewPin(17) // BCM Pin 17
    pin.SetMode(gpio.Input)
    pin.SetPullUp()
    
    pin.Watch(gpio.EdgeFalling, func() {
        triggerManager.HandleTrigger(
            HardwareTrigger1, 
            PhotoCapture,
        )
    })
}
```

### 4. Konfiguration UI

```vue
<template>
  <div class="trigger-config">
    <h3>Remote-Auslösung konfigurieren</h3>
    
    <!-- Hardware Triggers -->
    <div class="hardware-section">
      <h4>Hardware-Eingänge</h4>
      <select v-model="config.trigger1">
        <option value="photo">Foto aufnehmen</option>
        <option value="video">Video Start/Stop</option>
        <option value="burst">Serienaufnahme</option>
        <option value="custom">Benutzerdefiniert...</option>
      </select>
    </div>
    
    <!-- Wireless -->
    <div class="wireless-section">
      <h4>Drahtlose Auslöser</h4>
      <button @click="pairBluetooth">
        Bluetooth-Gerät koppeln
      </button>
      
      <div v-for="device in pairedDevices">
        {{ device.name }}
        <button @click="configureDevice(device)">
          Tasten zuweisen
        </button>
      </div>
    </div>
    
    <!-- Test -->
    <div class="test-section">
      <button @click="testTriggers">
        Trigger testen
      </button>
      <div class="test-log">
        {{ lastTriggerEvent }}
      </div>
    </div>
  </div>
</template>
```

### 5. Spezielle Features

#### Multi-Trigger-Synchronisation
```typescript
// Mehrere Trigger gleichzeitig
interface MultiTriggerConfig {
  mode: 'any' | 'all' | 'sequence';
  
  // "any": Jeder Trigger löst aus
  // "all": Alle müssen gleichzeitig gedrückt werden
  // "sequence": Bestimmte Reihenfolge
  
  triggers: TriggerSource[];
  timeout?: number; // für "all" und "sequence"
}
```

#### Trigger-Makros
```yaml
macros:
  - name: "Kritischer Befund"
    sequence:
      - capture_photo
      - wait: 100ms
      - mark_important
      - capture_video: 10s
      
  - name: "Dokumentations-Serie"
    sequence:
      - capture_photo
      - switch_source
      - capture_photo
      - switch_source: original
```

### 6. Feedback-System

#### Visual Feedback
```typescript
// LED-Anzeige oder Screen-Flash
interface TriggerFeedback {
  led?: {
    color: string;
    duration: number;
    pattern: 'solid' | 'blink' | 'pulse';
  };
  
  screen?: {
    flash: boolean;
    border: boolean;
    notification: string;
  };
  
  audio?: {
    beep: boolean;
    volume: number;
  };
  
  haptic?: {
    enabled: boolean;
    pattern: 'click' | 'double' | 'long';
  };
}
```

### 7. Fertige Hardware-Optionen

#### Empfohlene Wireless-Trigger
1. **Flic 2 Smart Button**
   - Bluetooth LE
   - 3 Aktionen (Click, Double, Hold)
   - Wasserdicht
   - ~30€

2. **XKEYS Wireless Footswitch**
   - USB Dongle
   - Robuste Bauweise
   - Programmierbar
   - ~150€

3. **Stream Deck Pedal**
   - USB-C
   - 3 Pedale
   - Software-programmierbar
   - ~90€

4. **DIY ESP32 Solution**
   - ESP32 + Battery
   - Custom Firmware
   - ~10€ Materialkosten

### 8. Integration mit Endoskop-Systemen

```yaml
endoscope_compatibility:
  olympus:
    trigger: "3.5mm TRS"
    polarity: "NO"
    voltage: "5V"
    
  karl_storz:
    trigger: "BNC"
    polarity: "NO"
    voltage: "3.3V"
    
  pentax:
    trigger: "3.5mm TS"
    special: "Needs pull-up resistor"
```

### 9. Sicherheit & Isolation

```
Endoskop ─── Optokoppler ─── GPIO ─── SmartBox
              │
              └── Galvanische Trennung
```

- Medizinische Isolation (IEC 60601)
- Schutz vor Spannungsspitzen
- ESD-Protection

*"Trigger anywhere, anytime, anyway"*