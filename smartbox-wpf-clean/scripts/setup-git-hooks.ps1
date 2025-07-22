# Setup Git Hooks for Automatic Documentation
# Run this once after project setup

Write-Host "🔧 Setting up Git hooks for automatic documentation updates..."

$hookPath = ".git/hooks/pre-commit"
$hookContent = @"
#!/bin/bash
# Auto-documentation update hook for SmartBoxNext
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

echo "🔍 SmartBoxNext: Checking documentation updates..."

# Get list of changed files
CHANGED_FILES=`$(git diff --cached --name-only)

# Check if critical files were modified
CODE_CHANGED=false
CONFIG_CHANGED=false

if echo "`$CHANGED_FILES" | grep -E "\.(cs|js|html)$" > /dev/null; then
    CODE_CHANGED=true
fi

if echo "`$CHANGED_FILES" | grep -E "(config\.json|\.csproj|AppConfig)" > /dev/null; then
    CONFIG_CHANGED=true
fi

# Auto-update PROJECT_INVENTORY.md timestamp
if [ -f "PROJECT_INVENTORY.md" ]; then
    sed -i "s/\*\*Last Updated\*\*:.*/\*\*Last Updated\*\*: `$(date +%Y-%m-%d)/" PROJECT_INVENTORY.md
    git add PROJECT_INVENTORY.md
    echo "✅ Updated PROJECT_INVENTORY.md timestamp"
fi

# Warn about documentation updates needed
if [ "`$CODE_CHANGED" = true ]; then
    echo "⚠️  Code changes detected! Please verify:"
    echo "   📋 PROJECT_INVENTORY.md - Variables/properties current?"
    echo "   🔗 PROPERTY_MAPPING_2025.md - UI mappings current?"
    echo "   🌐 ../../../DEVELOPMENT_PORTS.md - Network changes?"
fi

if [ "`$CONFIG_CHANGED" = true ]; then
    echo "🔴 Configuration changes detected! UPDATE REQUIRED:"
    echo "   📋 PROJECT_INVENTORY.md - Update config structure"
    echo "   🔗 PROPERTY_MAPPING_2025.md - Update property mappings"
fi

# Check for common port changes
if echo "`$CHANGED_FILES" | grep -E "(8080|8081|104|105|11112)" > /dev/null; then
    echo "🚨 PORT CHANGE DETECTED! Update DEVELOPMENT_PORTS.md immediately!"
fi

echo "✅ Pre-commit checks complete"
"@

# Write the hook file
$hookContent | Out-File -FilePath $hookPath -Encoding UTF8

# Make executable (if on Unix-like system)
if ($IsLinux -or $IsMacOS) {
    chmod +x $hookPath
}

Write-Host "✅ Git hooks installed successfully!"
Write-Host ""
Write-Host "📋 The hook will now:"
Write-Host "   - Auto-update PROJECT_INVENTORY.md timestamps"
Write-Host "   - Warn about documentation updates needed"
Write-Host "   - Alert on port/network changes"
Write-Host ""
Write-Host "🎯 Next steps:"
Write-Host "   1. Always update documentation when warned"
Write-Host "   2. Check ../../../DEVELOPMENT_PORTS.md for conflicts"
Write-Host "   3. Commit documentation changes separately if needed"