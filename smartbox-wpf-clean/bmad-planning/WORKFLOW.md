# BMAd Workflow for SmartBox Video System

## Current Status ✓
- [x] Brief created (`01-BRIEFS/video-system.md`)
- [x] Implementation plan exists (`../VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md`)
- [ ] Generate formal PRD
- [ ] Create technical architecture 
- [ ] Shard into stories
- [ ] Implement with SuperClaude

## Step-by-Step Workflow

### Step 1: Generate PRD (Do this now!)

1. Open `01-BRIEFS/video-system.md` in your editor
2. Copy the entire content
3. Go to Claude.ai or ChatGPT
4. Use this prompt:

```
You are an experienced Product Manager for medical imaging systems.

Based on the following brief, create a comprehensive Product Requirements Document (PRD) that includes:

1. Executive Summary
2. User Personas (doctors, technicians, administrators)
3. User Stories (in "As a... I want... So that..." format)
4. Functional Requirements (detailed, numbered list)
5. Non-Functional Requirements (performance, security, compliance)
6. DICOM Compliance Requirements
7. UI/UX Requirements (maintaining existing interface)
8. Integration Requirements (YUAN grabber, FFmpeg)
9. Success Metrics and KPIs
10. Risk Assessment and Mitigation
11. Timeline and Milestones
12. Acceptance Criteria

[PASTE YOUR BRIEF HERE]
```

5. Save the output to `02-PRD/video-system-prd.md`

### Step 2: Create Architecture

1. Take the PRD you just created
2. Use this prompt:

```
You are a Senior Software Architect specializing in video processing systems.

Based on this PRD, design a detailed technical architecture that includes:

1. System Architecture Overview
   - High-level component diagram
   - Technology stack justification

2. Component Design
   - FFmpeg Engine Component
   - Video Source Management
   - Segment Storage System
   - HLS Streaming Service
   - Frontend Integration Layer

3. Data Flow Diagrams
   - Recording flow
   - Preview streaming flow
   - Export/DICOM flow

4. Interface Definitions
   - C# interfaces
   - REST API contracts
   - WebSocket protocols

5. Storage Architecture
   - File organization
   - Memory management
   - Caching strategy

6. Security Architecture
   - Data protection
   - Access control
   - HIPAA compliance

7. Deployment Architecture
   - Process management
   - Configuration
   - Monitoring

[PASTE YOUR PRD HERE]
```

3. Save to `03-ARCHITECTURE/video-system-arch.md`

### Step 3: Shard into Stories

Create these files in `05-STORIES/`:

#### `01-ffmpeg-engine.md`
```markdown
# Story: FFmpeg Video Engine Core

## Context from PRD
[Extract relevant requirements]

## Context from Architecture
[Extract FFmpeg component design]

## Implementation Tasks
- [ ] Create IVideoEngine interface
- [ ] Implement FFmpegEngine class
- [ ] Add process management
- [ ] Implement segment writer
- [ ] Add error handling

## Acceptance Criteria
- [ ] Can start/stop FFmpeg process
- [ ] Generates 10-second segments
- [ ] Handles crashes gracefully
```

#### `02-video-sources.md`
```markdown
# Story: Video Source Management

## Context
[YUAN grabber and webcam requirements]

## Implementation Tasks
- [ ] Create IVideoSource interface
- [ ] Implement DirectShow enumeration
- [ ] Add YUAN device detection
- [ ] Implement webcam fallback
```

(Continue for remaining stories...)

### Step 4: Use with SuperClaude

In a new Claude Code session:

```bash
# First, analyze your existing plan
/analyze @VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md --think-hard

# Then implement each story
/implement @bmad-planning/05-STORIES/01-ffmpeg-engine.md --test-first
/build @bmad-planning/05-STORIES/02-video-sources.md --validate

# Or implement everything at once
/task implement-video-system @bmad-planning/05-STORIES --wave-mode
```

## Why This Works Better

1. **Context Preservation**: Each story has full context
2. **Parallel Development**: Stories can be done independently
3. **Reduced Errors**: AI has complete requirements
4. **Better Testing**: Clear acceptance criteria
5. **Incremental Progress**: Ship features as completed

## Tips

- Keep stories focused (1-2 days of work)
- Include code examples in stories
- Reference specific line numbers from existing code
- Add diagrams where helpful
- Test each story independently