# PROJECT GENESIS - SmartBox-Next Medical AI Integration

## 🏥 MEDICAL-GRADE AI DEVELOPMENT CONTEXT

**Project**: SmartBox-Next - Emergency Department Imaging System
**Mission**: "Ein Gerät das NIEMALS ausfällt - selbst wenn der Strom während der Aufnahme gezogen wird!"
**AI Integration Level**: MAXIMUM (Genesis Protocol Active)

## 🧠 SPECIALIZED MEDICAL AI ASSISTANT INSTRUCTIONS

### CORE MEDICAL CONTEXT
- **DICOM 3.0 Compliance MANDATORY** - Use fo-dicom library patterns
- **99.999% Reliability Target** - Every change must be power-loss tolerant
- **Emergency Workflow Optimization** - Sub-second response times critical
- **German Medical Standards** - Follow German medical device regulations
- **Touch Interface ONLY** - Optimize for medical gloves operation

### TECHNICAL ARCHITECTURE AWARENESS
```
SmartBoxNext.exe (WPF Shell)
├── WebView2 (Full Window HTML UI) ⚠️ Current Issue: C# ↔ JS messaging
├── Local Web Server (Port 5112)
├── Yuan SC550N1 Service (60 FPS SDI/HDMI) ✅ Working
├── WebRTC Capture (70 FPS) ✅ Working
├── DICOM Exporter (fo-dicom) ✅ Working
├── PACS Sender (C-STORE) ⚠️ Current Issue: Occasional hangs
├── Queue Manager (JSON-based) ✅ Working
└── SharedMemory IPC (High Performance) ✅ Working
```

### CURRENT CRITICAL ISSUES (Genesis Priority)
1. **PACS Send Hangs** - `PacsSender.cs:Unknown` - Async/await deadlock suspected
2. **WebView2 Messaging** - `MainWindow.xaml.cs:~3000` - Message routing inconsistent
3. **Queue Processing** - `QueueProcessor.cs:~200` - Retry logic optimization needed

### AI DEVELOPMENT PATTERNS (Learned from 25+ Sessions)

#### Session 87 Prevention Protocol
```
IF (modifying_medical_code) THEN
  1. VERIFY exact property names in DICOM standards
  2. CHECK fo-dicom library documentation
  3. VALIDATE against emergency workflow requirements
  4. TEST power-loss scenarios
END IF
```

#### Medical Code Safety Rules
- **NEVER** assume DICOM tag names - always verify with fo-dicom
- **ALWAYS** implement progress persistence for long operations
- **MANDATORY** error recovery for all PACS operations
- **CRITICAL** maintain queue integrity during power loss

### WISDOM CLAUDE INTEGRATION ACTIVE
- **Memory System**: Cross-session learning enabled
- **Pattern Recognition**: 143 medical development patterns loaded
- **Therapeutic Bond**: 25+ documented sessions with medical context
- **German Localization**: Medical terminology and workflows

### CLAUDE CODE GENESIS FEATURES ENABLED
- **VS Code Extension**: Inline medical code reviews
- **GitHub Integration**: PR reviews with medical compliance checks
- **Plan Mode**: Automatic medical workflow breakdown
- **Claude 4 Models**: Opus 4 for complex medical logic, Sonnet 4 for rapid iterations

### EMERGENCY RESPONSE COMMANDS
- `/medical-emergency` - Immediate bug triage for patient-critical issues
- `/dicom-verify` - Verify DICOM compliance before commit
- `/power-loss-test` - Simulate power failure scenarios
- `/german-medical` - Activate German medical standards mode

## 📋 GENESIS INTEGRATION STATUS

### ✅ COMPLETED
- [x] WISDOM Claude memory system integration
- [x] Medical context awareness
- [x] German localization support
- [x] Technical architecture understanding

### 🔄 IN PROGRESS
- [ ] GitHub repository setup
- [ ] VS Code Claude Code extension configuration
- [ ] Medical-specific agent development
- [ ] PACS Send hang resolution
- [ ] WebView2 messaging optimization

### 🎯 GENESIS GOALS
1. **Zero Patient Impact**: No bugs affect patient care
2. **Sub-Second Response**: All UI interactions < 1 second
3. **100% Queue Integrity**: No data loss under any circumstances
4. **German Compliance**: Full medical device regulation adherence
5. **Emergency Ready**: Always operational during critical situations

---

**Remember**: This is not just code - this is medical device software where bugs can affect human lives. Every change must be tested, verified, and documented with medical-grade precision.

**Genesis Protocol Active**: AI-first development with human oversight for patient safety.