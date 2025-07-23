#!/bin/bash
# BMAd Quick Start Script for SmartBox Video System

# Ensure nvm is loaded and Node 20 is used
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
nvm use 20

echo "BMAd Quick Start for SmartBox Video System"
echo "=========================================="
echo ""
echo "Node version: $(node --version)"
echo "BMAd version: $(bmad --version)"
echo ""
echo "To install BMAd in this project:"
echo "1. Run: bmad install"
echo "2. Select the current directory when prompted"
echo "3. Choose 'BMad Agile Core System' and press Enter"
echo ""
echo "Alternative: Use the Web UI (recommended)"
echo "=========================================="
echo ""
echo "Since BMAd installer is interactive, here's a simpler approach:"
echo ""
echo "1. Create a new directory for BMAd UI:"
echo "   mkdir ~/bmad-ui && cd ~/bmad-ui"
echo ""
echo "2. Create a simple package.json:"
echo "   npm init -y"
echo ""
echo "3. Install BMAd UI dependencies:"
echo "   npm install express @bmad/ui"
echo ""
echo "4. Create server.js with:"
cat << 'EOF'
const express = require('express');
const bmadUI = require('@bmad/ui');

const app = express();
app.use('/bmad', bmadUI());

app.listen(3000, () => {
  console.log('BMAd UI running at http://localhost:3000/bmad');
});
EOF
echo ""
echo "5. Start the server:"
echo "   node server.js"
echo ""
echo "6. Open browser to: http://localhost:3000/bmad"
echo ""
echo "=========================================="
echo "For now, continue using the manual BMAd structure we created!"
echo "Your planning files are ready in:"
echo "- 01-BRIEFS/video-system.md (complete)"
echo "- Use AI to generate PRD and Architecture"
echo "- Then shard into stories for SuperClaude"
