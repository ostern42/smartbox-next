//go:build !windows
// +build !windows

package capture

// Fallback implementation for non-Windows platforms

type VideoDevice struct {
	ID         string `json:"id"`
	Name       string `json:"name"`
	DevicePath string `json:"devicePath"`
}

type CaptureManager struct{}

func NewCaptureManager() *CaptureManager {
	return &CaptureManager{}
}

func (cm *CaptureManager) Initialize() error {
	return nil
}

func (cm *CaptureManager) GetDevices() ([]VideoDevice, error) {
	return []VideoDevice{
		{ID: "mock", Name: "Mock Camera", DevicePath: "mock"},
	}, nil
}

func (cm *CaptureManager) CaptureImage(deviceID string) (string, error) {
	return "data:image/jpeg;base64,/9j/4AAQSkZJRg==", nil
}

func (cm *CaptureManager) Cleanup() {}