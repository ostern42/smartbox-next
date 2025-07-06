package main

import (
	"context"
	"embed"
	"fmt"
	"os"
	"path/filepath"
	"smartbox-next/backend/capture"
	"smartbox-next/backend/config"
	"smartbox-next/backend/dicom"
	"smartbox-next/backend/pacs"
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
	configManager  *config.ConfigManager
	storeService   *pacs.StoreService
	uploadQueue    *pacs.UploadQueue
}

// NewApp creates a new App application struct
func NewApp() *App {
	// Create output directory
	homeDir, _ := os.UserHomeDir()
	outputDir := filepath.Join(homeDir, "SmartBoxNext", "DICOM")
	os.MkdirAll(outputDir, 0755)

	// Create config manager
	configPath := filepath.Join(homeDir, "SmartBoxNext", "config.json")
	configManager := config.NewConfigManager(configPath)

	// Create PACS services
	storeService := pacs.NewStoreService(configManager)
	uploadQueue, _ := pacs.NewUploadQueue(configManager, storeService)

	return &App{
		captureManager: capture.NewCaptureManager(),
		dicomCreator:   dicom.NewJpegDicomWriter(),
		outputDir:      outputDir,
		configManager:  configManager,
		storeService:   storeService,
		uploadQueue:    uploadQueue,
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

	// Add to PACS queue if enabled
	pacsConfig := a.configManager.GetPACS()
	if pacsConfig.Enabled && a.uploadQueue != nil {
		patientInfo := map[string]string{
			"patientName": a.dicomCreator.GetPatientInfo().Name,
			"patientId":   a.dicomCreator.GetPatientInfo().ID,
			"studyDate":   time.Now().Format("20060102"),
		}
		
		// Add with normal priority (emergency would be higher)
		a.uploadQueue.Add(outputPath, patientInfo, pacs.PriorityNormal)
	}

	return outputPath, nil
}

// OpenDicomFolder opens the DICOM output folder
func (a *App) OpenDicomFolder() error {
	runtime.BrowserOpenURL(a.ctx, "file:///" + a.outputDir)
	return nil
}

// PACS Configuration Methods

// GetPACSConfig returns current PACS configuration
func (a *App) GetPACSConfig() config.PACSConfig {
	return a.configManager.GetPACS()
}

// SetPACSConfig updates PACS configuration
func (a *App) SetPACSConfig(pacsConfig config.PACSConfig) error {
	return a.configManager.SetPACS(pacsConfig)
}

// TestPACSConnection tests PACS connectivity
func (a *App) TestPACSConnection() error {
	ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
	defer cancel()
	
	return a.storeService.TestConnection(ctx)
}

// GetQueueStatus returns upload queue status
func (a *App) GetQueueStatus() map[string]interface{} {
	return a.uploadQueue.GetStatus()
}

// GetQueueItems returns queue items
func (a *App) GetQueueItems(statusFilter string, limit int) []*pacs.QueueItem {
	var status *pacs.Status
	if statusFilter != "" {
		s := pacs.Status(statusFilter)
		status = &s
	}
	return a.uploadQueue.GetItems(status, limit)
}

// RetryQueueItem retries a failed upload
func (a *App) RetryQueueItem(id string) error {
	return a.uploadQueue.Retry(id)
}

// CancelQueueItem cancels a pending upload
func (a *App) CancelQueueItem(id string) error {
	return a.uploadQueue.Cancel(id)
}

// Emergency Patient Methods

// GetEmergencyTemplates returns available emergency templates
func (a *App) GetEmergencyTemplates() []config.PatientTemplate {
	return a.configManager.GetEmergencyTemplates()
}

// ApplyEmergencyTemplate applies an emergency template
func (a *App) ApplyEmergencyTemplate(templateID string) (dicom.PatientInfo, dicom.StudyInfo, error) {
	templates := a.configManager.GetEmergencyTemplates()
	
	for _, template := range templates {
		if template.ID == templateID {
			// Calculate birth date if relative
			birthDate := template.BirthDate
			if birthDate == "TODAY-40Y" {
				birthDate = time.Now().AddDate(-40, 0, 0).Format("20060102")
			} else if birthDate == "TODAY-10Y" {
				birthDate = time.Now().AddDate(-10, 0, 0).Format("20060102")
			}
			
			patient := dicom.PatientInfo{
				Name:      template.PatientName,
				ID:        template.PatientID + "-" + time.Now().Format("150405"),
				BirthDate: birthDate,
				Sex:       template.Sex,
			}
			
			study := dicom.StudyInfo{
				Description:     template.StudyDesc,
				AccessionNumber: "EMRG-" + time.Now().Format("20060102150405"),
			}
			
			// Apply to current DICOM creator
			a.dicomCreator.SetPatientInfo(patient)
			a.dicomCreator.SetStudyInfo(study)
			
			return patient, study, nil
		}
	}
	
	return dicom.PatientInfo{}, dicom.StudyInfo{}, fmt.Errorf("template not found: %s", templateID)
}

// Quit exits the application
func (a *App) Quit() {
	if a.uploadQueue != nil {
		a.uploadQueue.Stop()
	}
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