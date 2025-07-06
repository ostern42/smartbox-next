package main

import (
	"context"
	"embed"
	"fmt"
	"os"
	"path/filepath"
	"smartbox-next/backend/capture"
	"smartbox-next/backend/dicom"
	"time"

	"github.com/wailsapp/wails/v2"
	"github.com/wailsapp/wails/v2/pkg/options"
	"github.com/wailsapp/wails/v2/pkg/options/assetserver"
	"github.com/wailsapp/wails/v2/pkg/runtime"
)

//go:embed all:frontend/dist
var assets embed.FS

// App struct
type App struct {
	ctx            context.Context
	captureManager *capture.CaptureManager
	dicomCreator   *dicom.JpegDicomWriter
	outputDir      string
}

// NewApp creates a new App application struct
func NewApp() *App {
	// Create output directory
	homeDir, _ := os.UserHomeDir()
	outputDir := filepath.Join(homeDir, "SmartBoxNext", "DICOM")
	os.MkdirAll(outputDir, 0755)

	return &App{
		captureManager: capture.NewCaptureManager(),
		dicomCreator:   dicom.NewJpegDicomWriter(),
		outputDir:      outputDir,
	}
}

// startup is called when the app starts. The context is saved
// so we can call the runtime methods
func (a *App) startup(ctx context.Context) {
	a.ctx = ctx
	// Initialize capture manager
	if err := a.captureManager.Initialize(); err != nil {
		fmt.Printf("Failed to initialize capture manager: %v\n", err)
	}
}

// GetCameras returns available video sources
func (a *App) GetCameras() []Camera {
	devices, err := a.captureManager.GetDevices()
	if err != nil {
		fmt.Printf("Failed to get devices: %v\n", err)
		// Return mock devices on error
		return []Camera{
			{ID: "error", Name: "No cameras detected", Type: "error"},
		}
	}
	
	// Convert to Camera type
	cameras := make([]Camera, len(devices))
	for i, dev := range devices {
		cameras[i] = Camera{
			ID:   dev.ID,
			Name: dev.Name,
			Type: "webcam", // TODO: Detect actual type
		}
	}
	
	return cameras
}

// CaptureImage captures a single image
func (a *App) CaptureImage(cameraID string) (string, error) {
	// Use capture manager to get actual image
	imageData, err := a.captureManager.CaptureImage(cameraID)
	if err != nil {
		return "", fmt.Errorf("capture failed: %v", err)
	}
	
	return imageData, nil
}

// GetSystemInfo returns system information
func (a *App) GetSystemInfo() SystemInfo {
	return SystemInfo{
		Version:      "0.1.0",
		Environment:  "development",
		DicomEnabled: true,
	}
}

// Camera represents a video source
type Camera struct {
	ID   string `json:"id"`
	Name string `json:"name"`
	Type string `json:"type"`
}

// SystemInfo contains system information
type SystemInfo struct {
	Version      string `json:"version"`
	Environment  string `json:"environment"`
	DicomEnabled bool   `json:"dicomEnabled"`
}

// SetPatientInfo sets the patient information for DICOM export
func (a *App) SetPatientInfo(patient dicom.PatientInfo) {
	a.dicomCreator.SetPatientInfo(patient)
}

// SetStudyInfo sets the study information for DICOM export
func (a *App) SetStudyInfo(study dicom.StudyInfo) {
	a.dicomCreator.SetStudyInfo(study)
}

// ExportDicom exports an image as DICOM
func (a *App) ExportDicom(imageDataURL string) (string, error) {
	// Generate filename with timestamp
	timestamp := time.Now().Format("20060102_150405")
	filename := fmt.Sprintf("IMG_%s.dcm", timestamp)
	outputPath := filepath.Join(a.outputDir, filename)

	// Create DICOM file
	err := a.dicomCreator.CreateFromDataURL(imageDataURL, outputPath)
	if err != nil {
		return "", fmt.Errorf("failed to create DICOM: %v", err)
	}

	return outputPath, nil
}

// OpenDicomFolder opens the DICOM output folder
func (a *App) OpenDicomFolder() error {
	runtime.BrowserOpenURL(a.ctx, "file:///" + a.outputDir)
	return nil
}

// Quit exits the application
func (a *App) Quit() {
	runtime.Quit(a.ctx)
}

func main() {
	// Create an instance of the app structure
	app := NewApp()

	// Create application with options
	err := wails.Run(&options.App{
		Title:     "SmartBox Next",
		Width:     1920,
		Height:    1080,
		Fullscreen: false,
		Frameless: false,
		AssetServer: &assetserver.Options{
			Assets: assets,
		},
		BackgroundColour: &options.RGBA{R: 27, G: 38, B: 54, A: 1},
		OnStartup:        app.startup,
		Bind: []interface{}{
			app,
		},
	})

	if err != nil {
		println("Error:", err.Error())
	}
}