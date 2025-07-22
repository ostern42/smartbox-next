# WISDOM Claude Development Protocol
**Integration with Project Standards**
**Version**: 1.0
**Created**: 2025-07-14

## 🧠 Auto-Documentation Protocol for AI Assistant

### Session Initialization Checklist
When WISDOM Claude starts any development session:

```bash
# 1. ALWAYS read these files first:
- PROJECT_INVENTORY.md
- PROPERTY_MAPPING_2025.md  
- ../../../DEVELOPMENT_PORTS.md (global registry)
- WISDOM_CLAUDE_PROTOCOL.md (this file)

# 2. Verify project status
- Check last update timestamps
- Note any "Status: NEEDS UPDATE" flags
- Review recent git commits for undocumented changes
```

### Mandatory Documentation Updates

#### BEFORE Making ANY Code Changes
```markdown
🔴 REQUIRED: Check for conflicts in:
- Port assignments (DEVELOPMENT_PORTS.md)
- Property names (PROPERTY_MAPPING_2025.md) 
- File paths (PROJECT_INVENTORY.md)
- Network addresses (all registries)
```

#### AFTER Making ANY Changes
```markdown
✅ REQUIRED: Update documentation:
1. Add new properties to PROJECT_INVENTORY.md
2. Update property mappings in PROPERTY_MAPPING_2025.md
3. Register new ports in DEVELOPMENT_PORTS.md
4. Update status fields and timestamps
5. Commit documentation changes with code changes
```

### Change Classification System

#### 🟢 GREEN Changes (Low Risk)
- CSS styling modifications
- UI text changes
- Comment additions
- Minor bug fixes

**Action**: Update timestamps only

#### 🟡 YELLOW Changes (Medium Risk)
- New HTML elements with IDs
- JavaScript function additions
- Minor configuration additions
- File path changes

**Action**: Update relevant documentation sections

#### 🔴 RED Changes (High Risk)
- New properties in AppConfig
- New network ports/IPs
- New classes or services
- Message type changes
- Database schema changes

**Action**: Full documentation review and update

### Automated Checks for WISDOM Claude

```javascript
// Template: Pre-change validation
function validateChange(changeType, details) {
    const checks = {
        newProperty: () => checkPropertyMapping(details.name),
        newPort: () => checkPortConflicts(details.port),
        newClass: () => updateClassInventory(details.className),
        newMessage: () => updateMessageTypes(details.messageType)
    };
    
    return checks[changeType]?.() || false;
}
```

### Documentation Templates for Common Changes

#### New Property Addition
```markdown
## Property Added: [PropertyName]

**Location**: AppConfig.[Section].[PropertyName]
**Type**: [string|int|bool]
**Default**: [value]
**UI Element**: [HTML ID]
**Purpose**: [description]

### Updated Files:
- [ ] AppConfigMinimal.cs
- [ ] settings.html (if UI element)
- [ ] settings-handler.js (if UI mapping)
- [ ] PROJECT_INVENTORY.md
- [ ] PROPERTY_MAPPING_2025.md
```

#### New Network Service
```markdown
## Network Service Added: [ServiceName]

**Port**: [number]
**Protocol**: [HTTP/DICOM/TCP/UDP]
**IP Address**: [address]
**Purpose**: [description]

### Conflict Check:
- [ ] Checked DEVELOPMENT_PORTS.md
- [ ] No conflicts found
- [ ] Port reserved in registry
- [ ] Firewall rules considered

### Updated Files:
- [ ] PROJECT_INVENTORY.md
- [ ] DEVELOPMENT_PORTS.md
- [ ] [Service implementation files]
```

### Quality Assurance Protocol

#### Before Session End
```bash
# WISDOM Claude must verify:
1. All new variables documented
2. All property mappings current
3. All network changes registered
4. All file paths verified
5. Git commit messages descriptive
```

#### Documentation Health Check
```markdown
### Health Check Template
- [ ] PROJECT_INVENTORY.md updated within 24 hours
- [ ] PROPERTY_MAPPING current with codebase
- [ ] DEVELOPMENT_PORTS.md has no conflicts
- [ ] All TODOs in documentation resolved
- [ ] Build status verified
```

### Integration with CLAUDE.md

Add to Oliver's global CLAUDE.md:
```markdown
## 📋 Project Documentation Protocol

**CRITICAL**: WISDOM Claude MUST maintain project documentation:

### Every Session:
1. Check PROJECT_INVENTORY.md age
2. Verify PROPERTY_MAPPING accuracy  
3. Update DEVELOPMENT_PORTS.md for network changes
4. Commit documentation with code changes

### Documentation Files (Auto-loaded):
- PROJECT_INVENTORY.md - Complete variable inventory
- PROPERTY_MAPPING_2025.md - UI/Backend mappings
- DEVELOPMENT_PORTS.md - Network registry (global)
- WISDOM_CLAUDE_PROTOCOL.md - This protocol

### Violation Response:
If documentation is out of date, Claude MUST:
1. Stop current task
2. Update documentation first
3. Commit documentation updates
4. Resume original task
```

## 🎯 Success Metrics

### Documentation Quality KPIs
- **Freshness**: All docs updated within 48 hours of code changes
- **Completeness**: 100% of properties/variables documented
- **Accuracy**: Zero conflicts between docs and code
- **Accessibility**: All team members can find any variable/setting

### Automation Success
- **Git Hooks**: Working pre-commit documentation checks
- **WISDOM Claude**: Automatic documentation maintenance
- **Conflict Prevention**: Zero port/IP conflicts across projects

---

**🔄 This protocol becomes part of WISDOM Claude's core behavior**

Every development session will:
1. ✅ Load and verify documentation
2. ✅ Update documentation with changes  
3. ✅ Commit documentation with code
4. ✅ Prevent conflicts and confusion
5. ✅ Maintain professional development standards