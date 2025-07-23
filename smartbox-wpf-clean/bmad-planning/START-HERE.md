# SmartBox Video System - BMAd Planning Guide

## Quick Start

Since you need Node.js 20+ for the official BMAd tool and you have v18, I've created a manual BMAd-style planning structure.

## Option 1: Upgrade Node.js (Recommended)

```bash
# In WSL or Windows:
# Install Node Version Manager (nvm)
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash

# Install Node.js 20
nvm install 20
nvm use 20

# Then install BMAd
npm install -g bmad-method

# Run BMAd UI
bmad ui
```

## Option 2: Use Manual BMAd Structure (Current Setup)

### Your Planning Workflow:

1. **Start with the Brief** (already created):
   - `01-BRIEFS/video-system.md` ✓

2. **Generate PRD using AI**:
   ```
   Copy the brief content and use this prompt in Claude/ChatGPT:
   
   "Act as a Product Manager for a medical imaging system. 
   Based on this brief, create a comprehensive PRD with:
   - Detailed user stories
   - Functional/non-functional requirements  
   - Technical specifications
   - DICOM compliance requirements
   - Success metrics and KPIs"
   ```
   Save output to: `02-PRD/video-system-prd.md`

3. **Design Architecture**:
   ```
   Use the PRD and this prompt:
   
   "Act as a Software Architect specializing in video systems.
   Design a detailed architecture for this FFmpeg-based video system including:
   - Component diagrams
   - Sequence diagrams for recording flow
   - FFmpeg integration architecture
   - HLS streaming design
   - Data flow and storage architecture"
   ```
   Save to: `03-ARCHITECTURE/video-system-arch.md`

4. **Shard into Stories**:
   Break the architecture into 5-7 implementation stories:
   - `05-STORIES/01-ffmpeg-engine.md`
   - `05-STORIES/02-video-sources.md`
   - `05-STORIES/03-hls-streaming.md`
   - `05-STORIES/04-segment-management.md`
   - `05-STORIES/05-frontend-integration.md`
   - `05-STORIES/06-dicom-export.md`
   - `05-STORIES/07-configuration-ui.md`

5. **Implement with SuperClaude**:
   In a new Claude Code session, use your existing plan:
   ```bash
   /analyze @VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md --think-hard
   /implement @05-STORIES/01-ffmpeg-engine.md --test-first
   ```

## Directory Structure

```
bmad-planning/
├── START-HERE.md           # This file
├── BMAD-MANUAL-SETUP.md    # Manual BMAd guide
├── 01-BRIEFS/             
│   └── video-system.md    ✓ Created
├── 02-PRD/                
│   └── (generate with AI)
├── 03-ARCHITECTURE/       
│   └── (generate with AI)
├── 04-UX/                 
│   └── (optional - you have existing UI)
├── 05-STORIES/            
│   └── (shard from architecture)
└── 06-QA/                 
    └── (test plans)
```

## Why This Approach?

1. **Context Preservation**: Each story contains full context from PRD/Architecture
2. **Parallel Development**: Stories can be implemented independently
3. **SuperClaude Integration**: Each story becomes a focused `/implement` task
4. **Reduced Hallucination**: AI has complete context for each component
5. **Better Testing**: Each story has clear acceptance criteria

## Next Steps

1. Generate the PRD using the brief
2. Create the architecture document
3. Shard into implementation stories
4. Start implementing with SuperClaude

Your existing `VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md` already contains much of what you need - you can use it as a reference while creating the formal BMAd structure.