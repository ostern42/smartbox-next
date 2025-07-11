# On-Screen Keyboard AltGr/Shift Display Bug

**Reported**: 2025-07-11 by Oliver
**Status**: Confirmed Bug

## Problem Description

The on-screen keyboard doesn't properly show special characters when Shift or AltGr is pressed/locked:
- Shift characters ($%&) are not visible when Shift is pressed
- AltGr characters (@€{[]} etc.) are not visible when AltGr is pressed
- Even when locked (Caps/AltGr), the alternative characters don't show

## Current Implementation

### What exists:
```javascript
// Maps are defined correctly:
const shiftMap = {
    '1': '!', '2': '"', '3': '§', '4': '$', '5': '%', '6': '&',
    '7': '/', '8': '(', '9': ')', '0': '=', 'ß': '?', '´': '`',
    '+': '*', '#': "'", '<': '>', ',': ';', '.': ':', '-': '_'
};

const altGrMap = {
    'q': '@', 'e': '€', '7': '{', '8': '[', '9': ']', '0': '}',
    'ß': '\\', '+': '~', '<': '|', 'm': 'µ', '2': '²', '3': '³'
};
```

### The Bug:
The `updateKeyLabels()` function only updates for Shift, not AltGr:
```javascript
updateKeyLabels() {
    document.querySelectorAll('.keyboard-key').forEach(button => {
        const key = button.getAttribute('data-key');
        const shiftChar = button.getAttribute('data-shift');
        
        if (this.isShiftActive && shiftChar) {
            button.textContent = shiftChar;  // ✅ Shows shift chars
        } 
        // ❌ NO LOGIC FOR ALTGR CHARACTERS!
    });
}
```

## Fix Required

### 1. Update `updateKeyLabels()` to handle AltGr:
```javascript
updateKeyLabels() {
    document.querySelectorAll('.keyboard-key').forEach(button => {
        const key = button.getAttribute('data-key');
        const shiftChar = button.getAttribute('data-shift');
        const altGrChar = button.getAttribute('data-altgr');
        
        // Reset to default first
        let displayChar = key;
        
        // Priority: AltGr > Shift > Normal
        if (this.isAltGrActive && altGrChar) {
            displayChar = altGrChar;
        } else if (this.isShiftActive && shiftChar) {
            displayChar = shiftChar;
        } else if ((this.isShiftActive || this.isCapsLockActive) && key.match(/^[a-z]$/)) {
            displayChar = key.toUpperCase();
        }
        
        // Update button text (preserve special buttons)
        if (!button.classList.contains('special-key')) {
            button.textContent = displayChar;
        }
    });
}
```

### 2. Visual Feedback for Active Modifiers:
```javascript
// Add visual indicators when modifiers are active
toggleShift() {
    this.isShiftActive = !this.isShiftActive;
    document.querySelectorAll('.key-shift').forEach(key => {
        key.classList.toggle('active', this.isShiftActive);
    });
    this.updateKeyLabels();
}

toggleAltGr() {
    this.isAltGrActive = !this.isAltGrActive;
    document.querySelectorAll('.key-altgr').forEach(key => {
        key.classList.toggle('active', this.isAltGrActive);
    });
    this.updateKeyLabels();
}
```

### 3. CSS for Active State:
```css
.keyboard-key.active {
    background-color: #0078d4;
    color: white;
}
```

## Alternative Solution: Multi-Label Keys

Instead of changing the text, show all possible characters on each key:
```html
<button class="keyboard-key">
    <span class="key-main">2</span>
    <span class="key-shift">"</span>
    <span class="key-altgr">²</span>
</button>
```

With CSS to highlight the active one:
```css
.keyboard-key .key-shift { opacity: 0.3; }
.keyboard-key .key-altgr { opacity: 0.3; }

.shift-active .keyboard-key .key-shift { opacity: 1; color: #0078d4; }
.altgr-active .keyboard-key .key-altgr { opacity: 1; color: #0078d4; }
```

## Testing

After fix, test these scenarios:
1. Press Shift → Should see !"§$%&/()=? etc.
2. Press AltGr → Should see @€{[]}\ etc.
3. Lock Shift → Characters stay visible
4. Lock AltGr → Characters stay visible
5. Press both → AltGr takes priority

## Impact

This affects medical staff entering special characters in:
- Email addresses (@)
- Currency amounts (€)
- Mathematical formulas (²³)
- Special medical notation

---

**Priority**: Medium - Workaround exists (physical keyboard) but reduces touch-only usability