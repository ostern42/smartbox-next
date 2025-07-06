package dicom

import (
	"encoding/base64"
	"fmt"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// PatientInfo contains patient demographic data
type PatientInfo struct {
	Name      string `json:"name"`
	ID        string `json:"id"`
	BirthDate string `json:"birthDate"` // YYYYMMDD
	Sex       string `json:"sex"`       // M/F/O
}

// StudyInfo contains study information
type StudyInfo struct {
	AccessionNumber      string `json:"accessionNumber"`
	StudyDescription     string `json:"studyDescription"`
	ReferringPhysician   string `json:"referringPhysician"`
	PerformingPhysician  string `json:"performingPhysician"`
	Institution          string `json:"institution"`
}

// DicomCreator handles DICOM file creation
type DicomCreator struct {
	patient      PatientInfo
	study        StudyInfo
}

// NewDicomCreator creates a new DICOM creator instance
func NewDicomCreator() *DicomCreator {
	return &DicomCreator{}
}

// SetPatientInfo sets patient information
func (dc *DicomCreator) SetPatientInfo(patient PatientInfo) {
	dc.patient = patient
}

// SetStudyInfo sets study information
func (dc *DicomCreator) SetStudyInfo(study StudyInfo) {
	dc.study = study
}

// CreateFromDataURL creates a DICOM-like file from a data URL
// For now, this creates a simple file with metadata
func (dc *DicomCreator) CreateFromDataURL(dataURL string, outputPath string) error {
	// Parse data URL
	if !strings.HasPrefix(dataURL, "data:image/jpeg;base64,") {
		return fmt.Errorf("unsupported data URL format")
	}

	// Decode base64
	base64Data := strings.TrimPrefix(dataURL, "data:image/jpeg;base64,")
	imageData, err := base64.StdEncoding.DecodeString(base64Data)
	if err != nil {
		return fmt.Errorf("failed to decode base64: %v", err)
	}

	// For MVP, save as JPEG with metadata in filename
	// Real DICOM implementation would use proper DICOM format
	
	// Create directory if needed
	dir := filepath.Dir(outputPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create directory: %v", err)
	}

	// Change extension to .jpg for now
	outputPath = strings.TrimSuffix(outputPath, ".dcm") + ".jpg"

	// Write image file
	if err := os.WriteFile(outputPath, imageData, 0644); err != nil {
		return fmt.Errorf("failed to write file: %v", err)
	}

	// Create metadata file
	metadataPath := strings.TrimSuffix(outputPath, ".jpg") + "_metadata.txt"
	metadata := fmt.Sprintf(
		"DICOM Metadata\n"+
		"==============\n"+
		"Patient Name: %s\n"+
		"Patient ID: %s\n"+
		"Birth Date: %s\n"+
		"Sex: %s\n"+
		"Study Date: %s\n"+
		"Study Time: %s\n"+
		"Study Description: %s\n"+
		"Accession Number: %s\n"+
		"Institution: %s\n"+
		"Referring Physician: %s\n"+
		"Modality: OT\n"+
		"Manufacturer: CIRSS\n"+
		"Model: SmartBox Next\n",
		dc.patient.Name,
		dc.patient.ID,
		dc.patient.BirthDate,
		dc.patient.Sex,
		time.Now().Format("20060102"),
		time.Now().Format("150405"),
		dc.study.StudyDescription,
		dc.study.AccessionNumber,
		dc.study.Institution,
		dc.study.ReferringPhysician,
	)

	if err := os.WriteFile(metadataPath, []byte(metadata), 0644); err != nil {
		return fmt.Errorf("failed to write metadata: %v", err)
	}

	return nil
}

// CreateTestDicom creates a test DICOM file
func (dc *DicomCreator) CreateTestDicom(outputPath string) error {
	// For testing, create a simple test image
	testDataURL := "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCwAA8A/9k="
	return dc.CreateFromDataURL(testDataURL, outputPath)
}