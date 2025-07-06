//go:build windows
// +build windows

package capture

import (
	"bytes"
	"encoding/base64"
	"fmt"
	"image"
	"image/color"
	"image/jpeg"

	"golang.org/x/sys/windows"
	"golang.org/x/sys/windows/registry"
)

// Windows Media Foundation APIs
var (
	mf                        = windows.NewLazySystemDLL("mf.dll")
	mfplat                    = windows.NewLazySystemDLL("mfplat.dll")
	mfreadwrite              = windows.NewLazySystemDLL("mfreadwrite.dll")
	
	procMFStartup            = mfplat.NewProc("MFStartup")
	procMFShutdown           = mfplat.NewProc("MFShutdown")
	procMFEnumDeviceSources  = mfplat.NewProc("MFEnumDeviceSources")
)

const (
	MF_VERSION = 0x00020070 // Version 2.0
)

// VideoDevice represents a video capture device
type VideoDevice struct {
	ID          string `json:"id"`
	Name        string `json:"name"`
	DevicePath  string `json:"devicePath"`
}

// CaptureManager handles video capture on Windows
type CaptureManager struct {
	initialized bool
	devices     []VideoDevice
}

// NewCaptureManager creates a new capture manager
func NewCaptureManager() *CaptureManager {
	return &CaptureManager{}
}

// Initialize sets up Windows Media Foundation
func (cm *CaptureManager) Initialize() error {
	if cm.initialized {
		return nil
	}

	// Initialize Media Foundation
	ret, _, _ := procMFStartup.Call(
		uintptr(MF_VERSION),
		uintptr(0), // MFSTARTUP_FULL
	)
	
	if ret != 0 {
		return fmt.Errorf("failed to initialize Media Foundation: 0x%x", ret)
	}

	cm.initialized = true
	return nil
}

// GetDevices returns available video capture devices
func (cm *CaptureManager) GetDevices() ([]VideoDevice, error) {
	if !cm.initialized {
		if err := cm.Initialize(); err != nil {
			return nil, err
		}
	}

	// For now, return mock devices
	// TODO: Implement actual device enumeration using MFEnumDeviceSources
	devices := []VideoDevice{
		{
			ID:   "video0",
			Name: "Integrated Webcam",
			DevicePath: "\\\\?\\usb#vid_0000&pid_0000#0#{00000000-0000-0000-0000-000000000000}",
		},
	}

	// Try to detect actual devices using a simpler method
	if realDevices := cm.detectRealDevices(); len(realDevices) > 0 {
		devices = realDevices
	}

	cm.devices = devices
	return devices, nil
}

// detectRealDevices uses a simpler approach to detect cameras
func (cm *CaptureManager) detectRealDevices() []VideoDevice {
	var devices []VideoDevice
	
	// Check for common webcam registry entries
	key, err := registry.OpenKey(registry.LOCAL_MACHINE, 
		`SYSTEM\CurrentControlSet\Control\DeviceClasses\{65e8773d-8f56-11d0-a3b9-00a0c9223196}`, 
		registry.QUERY_VALUE)
	if err == nil {
		defer key.Close()
		// Add detected USB cameras
		devices = append(devices, VideoDevice{
			ID:   "usb_camera_0",
			Name: "USB Camera",
			DevicePath: "usb_camera",
		})
	}

	return devices
}

// CaptureImage captures a single frame from the specified device
func (cm *CaptureManager) CaptureImage(deviceID string) (string, error) {
	// For MVP, create a test image
	img := image.NewRGBA(image.Rect(0, 0, 640, 480))
	
	// Draw a simple pattern
	for y := 0; y < 480; y++ {
		for x := 0; x < 640; x++ {
			r := uint8((x + y) & 255)
			g := uint8((x * 2) & 255)
			b := uint8((y * 2) & 255)
			img.Set(x, y, color.RGBA{r, g, b, 255})
		}
	}

	// Encode to JPEG
	var buf bytes.Buffer
	if err := jpeg.Encode(&buf, img, &jpeg.Options{Quality: 90}); err != nil {
		return "", err
	}

	// Return as base64 data URL
	return "data:image/jpeg;base64," + base64.StdEncoding.EncodeToString(buf.Bytes()), nil
}

// Cleanup releases resources
func (cm *CaptureManager) Cleanup() {
	if cm.initialized {
		procMFShutdown.Call()
		cm.initialized = false
	}
}