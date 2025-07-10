# Changelog - July 10, 2025 Session

## Added
- **Diagnostic Windows** - Comprehensive step-by-step connection testing for PACS/MWL
  - TCP connection test
  - DICOM C-ECHO test  
  - Service-specific tests (C-STORE for PACS, C-FIND for MWL)
- **Layout Manager System** - Foundation for multiple layouts and themes
  - Standard, Compact, Minimal, MWL Focus layouts
  - Default, Dark, Night, High Contrast themes
- **Worklist Documentation** - Clear setup instructions for Orthanc worklist
- **Auto-export DICOM** - Enabled automatic PACS upload on capture

## Changed
- **Window Sizing**
  - Main window: 1920x1080 (was 1600x1150)
  - Diagnostic window: 700x650 (was 450x600)
- **Layout Proportions** - 1:2 ratio (preview:MWL) instead of 3:1
- **MWL List** - Compact design showing 20+ entries
  - Item height: 36px (was 60px)
  - Font size: 13px (was 14px)
  - Max height: 600px (was 400px)
- **Settings Back Button** - Added chevron icon and "Back" text
- **Server Hosts** - Updated from "127" to "localhost" in configs

## Fixed
- **MWL Connection Test** - Now uses C-ECHO instead of worklist query
- **Folder Selection** - Fixed fieldId/inputId mismatch in C# code
- **Naming Conventions** - Unified PascalCase for C#/JSON communication
- **Config Missing Fields** - Added AutoExportDicom to config.json

## Known Issues
- Patient info form still single column (CSS ready, HTML needs update)
- PACS upload may not be triggering on capture
- Worklist upload requires DICOM .wl files, not JSON