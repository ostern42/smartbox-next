# SmartBox Documentation

## Overview
This directory contains comprehensive documentation for the SmartBox medical capture system, including architecture designs, implementation plans, and technical specifications.

## Documentation Structure

### Video System Documentation

#### Implementation & Planning
- [Video System Implementation Plan](VIDEO_SYSTEM_IMPLEMENTATION_PLAN.md) - Original detailed plan for FFmpeg video engine
- [Video System Implementation Summary](VIDEO_SYSTEM_IMPLEMENTATION_SUMMARY.md) - Summary of completed FFmpeg implementation
- [Video Streaming Improvement Plan](VIDEO_STREAMING_IMPROVEMENT_PLAN.md) - Plan to enhance streaming client with FFmpeg integration
- [DICOM Video Implementation Masterplan](DICOM_VIDEO_IMPLEMENTATION_MASTERPLAN.md) - DICOM video export strategy
- [DICOM Video Research Prompt](DICOM_VIDEO_RESEARCH_PROMPT.md) - Research notes on DICOM video standards

#### Streaming System
- [Streaming System README](STREAMING_SYSTEM_README.md) - Overview of HLS streaming implementation
- [Streaming Quick Start](STREAMING_QUICK_START.md) - Quick guide to streaming setup
- [Streaming Troubleshooting](STREAMING_TROUBLESHOOTING.md) - Common streaming issues and solutions

#### Timeline & UI
- [Timeline Implementation](TIMELINE_IMPLEMENTATION.md) - Adaptive timeline component documentation
- [Timeline Scrubbing Design](TIMELINE_SCRUBBING_DESIGN.md) - Design for timeline scrubbing features

### API & Integration Documentation
- [API Interface Documentation](API_INTERFACE_DOCUMENTATION.md) - Complete API reference
- [Comprehensive Context Map](COMPREHENSIVE_CONTEXT_MAP.md) - System architecture overview
- [Port Update Summary](PORT_UPDATE_SUMMARY.md) - Network port configuration changes

### Feature Documentation
- [Wireless Tablet Control](WIRELESS_TABLET_CONTROL.md) - Tablet-based remote control design

## Quick Navigation

### For Developers
1. Start with [Comprehensive Context Map](COMPREHENSIVE_CONTEXT_MAP.md) for system overview
2. Review [Video System Implementation Summary](VIDEO_SYSTEM_IMPLEMENTATION_SUMMARY.md) for current state
3. Check [API Interface Documentation](API_INTERFACE_DOCUMENTATION.md) for integration points

### For Video System Work
1. [Video System Implementation Summary](VIDEO_SYSTEM_IMPLEMENTATION_SUMMARY.md) - Current implementation
2. [Video Streaming Improvement Plan](VIDEO_STREAMING_IMPROVEMENT_PLAN.md) - Next steps
3. [Streaming System README](STREAMING_SYSTEM_README.md) - Streaming details

### For UI/Frontend Work
1. [Timeline Implementation](TIMELINE_IMPLEMENTATION.md) - Timeline component
2. [Timeline Scrubbing Design](TIMELINE_SCRUBBING_DESIGN.md) - Scrubbing features
3. [Wireless Tablet Control](WIRELESS_TABLET_CONTROL.md) - Remote control

## Documentation Standards

### File Naming
- Use UPPERCASE with underscores for documentation files
- Include category prefix (VIDEO_, API_, etc.)
- Use descriptive names that indicate content

### Content Structure
- Start with executive summary
- Include table of contents for long documents
- Use clear headings and subheadings
- Include code examples where relevant
- Add diagrams for complex architectures

### Maintenance
- Update implementation summaries after major changes
- Keep API documentation in sync with code
- Archive obsolete documentation with date prefix
- Review and update quarterly

## Recent Updates

- **2024-01-23**: Created Video Streaming Improvement Plan
- **2024-01-23**: Moved all documentation to /docs directory
- **2024-01-23**: Completed FFmpeg video engine implementation

## Contributing

When adding new documentation:
1. Follow the naming conventions
2. Add entry to this README
3. Cross-reference related documents
4. Include creation/update date
5. Review for accuracy before committing