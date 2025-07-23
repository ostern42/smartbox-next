# DICOM Video Encoding Deep Research Request

## Context
I'm implementing DICOM video export for a medical imaging application using C# and fo-dicom library. The application captures videos from medical devices and needs to convert them to DICOM format for PACS storage.

## Research Requirements

### 1. DICOM Video Standards & Compliance
- What are the current NEMA DICOM standards for video encoding (PS3.5)?
- Which video formats are officially supported in DICOM standard?
- What are the mandatory DICOM tags for video objects?
- NEMA conformance requirements for video storage

### 2. Transfer Syntax Details
Please provide complete information about these DICOM video transfer syntaxes:
- MPEG2 Main Profile @ Main Level (1.2.840.10008.1.2.4.100)
- MPEG2 Main Profile @ High Level (1.2.840.10008.1.2.4.101)
- MPEG-4 AVC/H.264 High Profile / Level 4.1 (1.2.840.10008.1.2.4.102)
- MPEG-4 AVC/H.264 BD-compatible (1.2.840.10008.1.2.4.103)
- MPEG-4 AVC/H.264 High Profile / Level 4.2 (1.2.840.10008.1.2.4.104)
- JPEG Baseline (for MJPEG multiframe) (1.2.840.10008.1.2.4.50)

### 3. Implementation Approaches
Compare and contrast:
- **Multiframe Image Storage** (using Secondary Capture Image Storage SOP Class)
  - When to use this approach?
  - Frame rate and timing attributes
  - Maximum frames limitations
  
- **Video Photographic Image Storage** (using VL Photographic Image Storage)
  - Advantages/disadvantages
  - Compatibility with PACS systems
  
- **Cine Module** attributes
  - Frame Time, Frame Time Vector
  - Cine Rate, Frame Delay
  - Recommended Display Frame Rate

### 4. fo-dicom C# Implementation
- How to create DICOM video files with fo-dicom 5.x?
- Setting correct SOP Class UIDs for video
- Handling pixel data for video frames
- Examples of encoding video streams into DICOM

### 5. Video Encoding Best Practices
- Recommended video codecs for medical imaging
- Bit rates and quality settings for diagnostic quality
- Frame rate considerations (15fps, 25fps, 30fps)
- Resolution standards (SD, HD, Full HD)
- Color space requirements (YBR_FULL_422, YBR_PARTIAL_422)

### 6. PACS Compatibility
- Which video formats have the best PACS compatibility?
- Common PACS video viewing limitations
- Fallback strategies for non-video capable PACS

### 7. Required DICOM Modules for Video
Please list all mandatory and conditional modules for:
- Patient Module
- Study Module  
- Series Module
- Equipment Module
- Image Module
- Cine Module
- Multi-frame Module
- SOP Common Module

### 8. Practical Examples
Need code examples for:
- Converting MP4/AVI to DICOM MPEG2
- Creating multiframe DICOM from video frames
- Setting proper video-specific DICOM tags
- Handling audio tracks (if supported)

## Specific Use Case
- Input: MP4/WebM videos from webcam (typically 30fps, 1280x720)
- Output: NEMA-compliant DICOM video files
- Target: Various PACS systems (must be widely compatible)
- Library: fo-dicom 5.x with C# .NET 8

Please provide comprehensive information with special attention to NEMA compliance and real-world PACS compatibility.