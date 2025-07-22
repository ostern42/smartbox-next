# Project Standards & Workflow Template
**Version**: 1.0
**Created**: 2025-07-14

## 📋 MANDATORY Project Documentation Standard

### Required Files for EVERY Project
1. **PROJECT_INVENTORY.md** - Complete variable/entity inventory
2. **PROPERTY_MAPPING_{YEAR}.md** - UI/Backend property mappings  
3. **NETWORK_REGISTRY.md** - Shared across all projects
4. **GIT_WORKFLOW.md** - Automated git procedures
5. **DEVELOPMENT_PORTS.md** - Port allocation registry

### Documentation Update Triggers
```
🔴 MANDATORY UPDATES when:
- Adding ANY new variable/property
- Changing ANY port number
- Adding ANY IP address/hostname
- Creating ANY new class/service
- Modifying ANY configuration structure
- Adding ANY enum/constant
- Changing ANY file path
```

## 🌐 Global Network Registry Template

### Development Machine Port Registry
```markdown
# DEVELOPMENT_PORTS.md - SHARED ACROSS ALL PROJECTS
## Port Allocation Registry

| Project | Service | Port | Status | Developer |
|---------|---------|------|--------|-----------|
| SmartBoxNext | WebServer | 8080 | ✅ Active | Oliver |
| [Project2] | API | 8081 | ⚠️ Reserved | Oliver |
| [Project3] | WebSocket | 8082 | 🔧 Planned | Oliver |

## IP Address Registry
| Purpose | IP Address | Projects Using | Notes |
|---------|------------|----------------|-------|
| Test PACS | 192.168.1.100 | SmartBoxNext | Shared test server |
| Local DICOM | 192.168.1.101 | [Project2] | Dedicated instance |
```

## 🔄 Automated Git Workflow Integration

### Template for .git/hooks/pre-commit
```bash
#!/bin/bash
# Auto-check documentation updates

echo "🔍 Checking for documentation updates..."

# Check if code changes require doc updates
CHANGED_FILES=$(git diff --cached --name-only)

if echo "$CHANGED_FILES" | grep -E "\.(cs|js|html|json)$"; then
    echo "⚠️  Code changes detected. Documentation review required:"
    echo "   - PROJECT_INVENTORY.md"
    echo "   - PROPERTY_MAPPING_*.md"
    echo "   - Update status: $(date)"
fi

# Auto-update LastUpdated in PROJECT_INVENTORY.md
if [ -f "PROJECT_INVENTORY.md" ]; then
    sed -i "s/\*\*Last Updated\*\*:.*/\*\*Last Updated\*\*: $(date +%Y-%m-%d)/" PROJECT_INVENTORY.md
    git add PROJECT_INVENTORY.md
fi
```

## 📊 Project Health Dashboard Template

### Daily Checks
```markdown
## Project Health - $(date)

### Configuration Integrity
- [ ] All properties documented in PROPERTY_MAPPING
- [ ] All network ports registered in DEVELOPMENT_PORTS
- [ ] All file paths verified in PROJECT_INVENTORY
- [ ] Build status: ✅/❌
- [ ] Tests passing: ✅/❌

### Documentation Freshness
- [ ] Last updated within 7 days
- [ ] All new features documented
- [ ] Property mappings current
- [ ] Network registry updated
```

## 🎯 Implementation Steps for New Projects

### 1. Project Initialization Checklist
```bash
# When starting/importing ANY project:
1. Create PROJECT_INVENTORY.md from template
2. Document ALL existing variables/properties
3. Register ALL network ports in shared registry
4. Set up automated git hooks
5. Create property mapping documentation
6. Initialize health dashboard
```

### 2. Change Management Process
```bash
# BEFORE making ANY change:
1. Check PROJECT_INVENTORY.md for conflicts
2. Reserve ports in DEVELOPMENT_PORTS.md
3. Document planned changes

# AFTER making ANY change:
1. Update PROJECT_INVENTORY.md
2. Update property mappings if applicable
3. Commit with descriptive message
4. Run health check
```

## 🔧 Automation Tools

### PowerShell Script: Update-ProjectDocs.ps1
```powershell
# Auto-generate property mappings from C# code
param([string]$ProjectPath)

Write-Host "🔍 Scanning project for properties..."
$csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse
$properties = @()

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName
    # Extract public properties...
    # Update documentation...
}

Write-Host "✅ Documentation updated"
```

### VS Code Extension Integration
```json
// .vscode/tasks.json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Update Project Inventory",
            "type": "shell",
            "command": "powershell",
            "args": ["-File", "scripts/Update-ProjectDocs.ps1"],
            "group": "build",
            "problemMatcher": []
        }
    ]
}
```

## 📝 Template: New Property Addition Workflow

```markdown
### Adding New Property: Step-by-Step

1. **Code Change**
   ```csharp
   // Add to AppConfig
   public string NewProperty { get; set; } = "default";
   ```

2. **UI Change** 
   ```html
   <!-- Add to settings.html -->
   <input id="section-new-property" type="text" />
   ```

3. **Documentation Update**
   - [ ] Add to PROJECT_INVENTORY.md
   - [ ] Add to PROPERTY_MAPPING_2025.md
   - [ ] Update settings-handler.js mapping
   - [ ] Test and verify

4. **Commit Message Format**
   ```
   feat: Add NewProperty to SectionSettings
   
   - Added NewProperty with default value
   - Updated UI in settings.html
   - Documented in PROJECT_INVENTORY.md
   - Property mapping: section-new-property → Section.NewProperty
   ```
```

---

**🎯 GOAL: Never lose track of ANY variable, port, or configuration!**

This standard should be applied to:
- ✅ SmartBoxNext (implemented)
- 🔄 Future medical imaging projects
- 🔄 Any multi-project development environment
- 🔄 Team collaboration scenarios