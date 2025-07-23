# BMAd Manual Planning Setup for SmartBox Video System

Since BMAd requires Node.js 20+ and you have v18, here's a manual approach that follows BMAd methodology.

## Directory Structure

```
bmad-planning/
├── 01-BRIEFS/           # Initial ideas and requirements
├── 02-PRD/              # Product Requirements Documents
├── 03-ARCHITECTURE/     # Technical architecture
├── 04-UX/               # User experience designs
├── 05-STORIES/          # Sharded implementation stories
└── 06-QA/               # Test plans and validation
```

## Workflow

### Phase 1: Planning (Use with Claude/ChatGPT)

1. **Create Initial Brief** (01-BRIEFS/video-system.md)
2. **Generate PRD** using this prompt:
   ```
   Act as a Product Manager. Based on the brief in 01-BRIEFS/video-system.md,
   create a comprehensive PRD including:
   - User stories
   - Functional requirements
   - Non-functional requirements
   - Success metrics
   - Risk assessment
   ```

3. **Design Architecture** using this prompt:
   ```
   Act as a Software Architect. Based on the PRD in 02-PRD/video-system-prd.md,
   design a technical architecture including:
   - System components
   - Data flow diagrams
   - Technology choices
   - Integration points
   - Scalability considerations
   ```

### Phase 2: Sharding for SuperClaude

4. **Create Implementation Stories** (05-STORIES/)
   - Break architecture into 5-10 focused stories
   - Each story should be self-contained
   - Include relevant context from PRD and Architecture

5. **Use with SuperClaude**:
   ```bash
   # In a new Claude Code session:
   /implement @05-STORIES/ffmpeg-backend.md --test-first
   /build @05-STORIES/hls-streaming.md --validate
   ```

## Templates

### Brief Template (01-BRIEFS/template.md)
```markdown
# [Feature Name] Brief

## Vision
[What we want to achieve]

## Current State
[What exists today]

## Desired State
[What we want to build]

## Key Requirements
- Requirement 1
- Requirement 2

## Constraints
- Technical constraints
- Business constraints
```

### Story Template (05-STORIES/template.md)
```markdown
# Story: [Component Name]

## Context
[Relevant PRD section]
[Relevant Architecture section]

## Implementation Requirements
- [ ] Requirement 1
- [ ] Requirement 2

## Technical Details
[Specific implementation guidance]

## Acceptance Criteria
- [ ] Test 1
- [ ] Test 2

## Dependencies
- Other stories
- External systems
```