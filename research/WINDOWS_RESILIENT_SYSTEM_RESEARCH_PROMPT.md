# Web Research Request: Windows Resilient Medical System Configuration

## Research Objective
Find comprehensive information on configuring Windows (10, 11, and Embedded) for maximum resilience in medical device scenarios, specifically focusing on RAM-based operation and power failure recovery.

## Background Context
SmartBox-Next is a medical imaging device that MUST remain operational under all circumstances. The system needs to:
- Survive sudden power loss during image capture
- Boot quickly and reliably
- Operate from RAM to minimize disk wear
- Recover gracefully from any failure state
- Never show error screens that block operation

## Specific Questions

### 1. Windows Embedded for Medical Devices
- What are the differences between Windows 10 IoT Enterprise and Windows 11 IoT Enterprise?
- Which Windows Embedded features are specifically designed for medical devices?
- How to configure Windows Embedded for kiosk/appliance mode?
- What are the licensing implications for medical devices?

### 2. RAM-Based Operation (Windows PE/RE Style)
- How to create a Windows system that runs entirely from RAM?
- What is Windows PE and can it be used for production medical devices?
- How to implement RAM disk for temporary files and swap?
- What are the size limitations for RAM-based Windows?
- How to preload the entire OS into RAM at boot?

### 3. Write Filter Technologies
- How does Unified Write Filter (UWF) work?
- Enhanced Write Filter (EWF) vs UWF - which for medical devices?
- How to configure write filters for selective persistence?
- Best practices for managing filter exceptions?
- Impact on Windows Updates and application updates?

### 4. Power Failure Protection
- How to configure Windows for instant recovery after power loss?
- Disabling disk write caching safely
- Journaling file systems and their configuration
- UPS integration and graceful shutdown triggers
- BIOS/UEFI settings for auto-power-on after failure

### 5. Boot Optimization
- How to achieve sub-10 second boot times?
- Windows Fast Startup vs Full shutdown
- Removing unnecessary services and drivers
- Custom Windows images with DISM
- UEFI optimization for medical devices

### 6. Error Recovery and Kiosk Mode
- Preventing Windows error dialogs and blue screens
- Auto-login and application auto-start
- Watchdog services for application monitoring
- Preventing user access to Windows UI
- Remote management without breaking kiosk mode

### 7. Storage Strategies
- SSD vs eMMC vs CFast for medical devices
- Wear leveling and write endurance calculations
- Separating OS, Application, and Data partitions
- Network boot (PXE) as alternative to local storage
- Hybrid approaches (OS in RAM, data on SSD)

### 8. Medical Device Specific Considerations
- IEC 62304 compliance for Windows configuration
- FDA guidance on COTS (Commercial Off-The-Shelf) OS
- Cybersecurity requirements for medical devices
- Update strategies that don't break FDA approval
- Audit logging requirements

## Search Strategies
- Primary sources: Microsoft documentation, Windows IoT documentation
- Medical device forums and communities
- IEC 62304 and FDA guidance documents
- Case studies of Windows-based medical devices
- Technical blogs from medical device manufacturers

## Expected Deliverables
1. Step-by-step configuration guide for resilient Windows
2. Comparison table of different Windows editions for medical use
3. Best practices checklist for power-failure resilience
4. Sample PowerShell scripts for system configuration
5. Architecture recommendations for different use cases
6. Regulatory compliance considerations

## Additional Context
- The device will be used in emergency rooms and operating theaters
- Network connectivity may be intermittent
- Users are medical staff, not IT professionals
- Device must be serviceable by field technicians
- Cost is less important than reliability