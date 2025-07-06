package dicom

import (
	"bytes"
	"encoding/base64"
	"encoding/binary"
	"fmt"
	"image"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// CambridgeStyleDicomWriter creates DICOM files like CamBridge v1
type CambridgeStyleDicomWriter struct {
	patient PatientInfo
	study   StudyInfo
}

// NewCambridgeStyleDicomWriter creates a new Cambridge-style DICOM writer
func NewCambridgeStyleDicomWriter() *CambridgeStyleDicomWriter {
	return &CambridgeStyleDicomWriter{}
}

// SetPatientInfo sets patient information
func (cw *CambridgeStyleDicomWriter) SetPatientInfo(patient PatientInfo) {
	cw.patient = patient
}

// SetStudyInfo sets study information
func (cw *CambridgeStyleDicomWriter) SetStudyInfo(study StudyInfo) {
	cw.study = study
}

// CreateFromDataURL creates a DICOM file Cambridge-style
func (cw *CambridgeStyleDicomWriter) CreateFromDataURL(dataURL string, outputPath string) error {
	// Parse data URL
	if !strings.HasPrefix(dataURL, "data:image/jpeg;base64,") {
		return fmt.Errorf("unsupported data URL format")
	}

	// Decode base64
	base64Data := strings.TrimPrefix(dataURL, "data:image/jpeg;base64,")
	jpegData, err := base64.StdEncoding.DecodeString(base64Data)
	if err != nil {
		return fmt.Errorf("failed to decode base64: %v", err)
	}

	// Get image dimensions
	img, _, err := image.DecodeConfig(bytes.NewReader(jpegData))
	if err != nil {
		return fmt.Errorf("failed to decode JPEG config: %v", err)
	}
	width := img.Width
	height := img.Height

	// Create directory if needed
	dir := filepath.Dir(outputPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return fmt.Errorf("failed to create directory: %v", err)
	}

	// Create DICOM file
	file, err := os.Create(outputPath)
	if err != nil {
		return fmt.Errorf("failed to create file: %v", err)
	}
	defer file.Close()

	// Write DICOM preamble (128 bytes of zeros)
	preamble := make([]byte, 128)
	file.Write(preamble)

	// Write DICM prefix
	file.Write([]byte("DICM"))

	// Generate UIDs (Cambridge style - shorter)
	timestamp := time.Now().Unix() % 10000000000
	processId := os.Getpid() % 10000
	sopInstanceUID := fmt.Sprintf("1.2.276.0.7230010.3.1.4.%d.%d.1", timestamp, processId)
	studyUID := fmt.Sprintf("1.2.276.0.7230010.3.1.2.%d.%d.1", timestamp, processId)
	seriesUID := fmt.Sprintf("1.2.276.0.7230010.3.1.3.%d.%d.1", timestamp, processId)

	// File Meta Information - EXPLICIT VR
	metaBuf := &bytes.Buffer{}
	
	// (0002,0001) File Meta Information Version
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0001, "OB", []byte{0x00, 0x01})
	
	// (0002,0002) Media Storage SOP Class UID - Secondary Capture
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0002, "UI", cw.padUID("1.2.840.10008.5.1.4.1.1.7"))
	
	// (0002,0003) Media Storage SOP Instance UID
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0003, "UI", cw.padUID(sopInstanceUID))
	
	// (0002,0010) Transfer Syntax UID - JPEG Baseline (like Cambridge!)
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0010, "UI", cw.padUID("1.2.840.10008.1.2.4.50"))
	
	// (0002,0012) Implementation Class UID
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0012, "UI", cw.padUID("1.2.276.0.7230010.3.0.3.6.0"))
	
	// (0002,0013) Implementation Version Name
	cw.writeExplicitVR(metaBuf, 0x0002, 0x0013, "SH", cw.padString("SMARTBOX_1.0"))

	// Write File Meta Information Group Length
	cw.writeExplicitVR(file, 0x0002, 0x0000, "UL", cw.uint32ToBytes(uint32(metaBuf.Len())))
	file.Write(metaBuf.Bytes())

	// Main Dataset - IMPLICIT VR for JPEG (like Cambridge)
	
	// Identifying Information
	cw.writeImplicitVR(file, 0x0008, 0x0005, cw.padString("ISO_IR 192")) // Specific Character Set (UTF-8)
	cw.writeImplicitVR(file, 0x0008, 0x0008, cw.padString("ORIGINAL\\PRIMARY")) // Image Type
	cw.writeImplicitVR(file, 0x0008, 0x0016, cw.padUID("1.2.840.10008.5.1.4.1.1.7")) // SOP Class UID
	cw.writeImplicitVR(file, 0x0008, 0x0018, cw.padUID(sopInstanceUID)) // SOP Instance UID
	cw.writeImplicitVR(file, 0x0008, 0x0020, cw.padString(time.Now().Format("20060102"))) // Study Date
	cw.writeImplicitVR(file, 0x0008, 0x0023, cw.padString(time.Now().Format("20060102"))) // Content Date
	cw.writeImplicitVR(file, 0x0008, 0x0030, cw.padString(time.Now().Format("150405"))) // Study Time
	cw.writeImplicitVR(file, 0x0008, 0x0033, cw.padString(time.Now().Format("150405"))) // Content Time
	cw.writeImplicitVR(file, 0x0008, 0x0050, cw.padString(cw.study.AccessionNumber)) // Accession Number
	cw.writeImplicitVR(file, 0x0008, 0x0060, cw.padString("OT")) // Modality
	cw.writeImplicitVR(file, 0x0008, 0x0064, cw.padString("DI")) // Conversion Type
	cw.writeImplicitVR(file, 0x0008, 0x0070, cw.padString("CIRSS")) // Manufacturer
	cw.writeImplicitVR(file, 0x0008, 0x0090, cw.padString(cw.study.ReferringPhysician)) // Referring Physician
	cw.writeImplicitVR(file, 0x0008, 0x1030, cw.padString(cw.study.StudyDescription)) // Study Description
	cw.writeImplicitVR(file, 0x0008, 0x103E, cw.padString("SmartBox Capture")) // Series Description

	// Patient Information
	cw.writeImplicitVR(file, 0x0010, 0x0010, cw.padString(cw.patient.Name)) // Patient Name
	cw.writeImplicitVR(file, 0x0010, 0x0020, cw.padString(cw.patient.ID)) // Patient ID
	cw.writeImplicitVR(file, 0x0010, 0x0030, cw.padString(cw.patient.BirthDate)) // Birth Date
	cw.writeImplicitVR(file, 0x0010, 0x0040, cw.padString(cw.patient.Sex)) // Sex

	// Relationship Information
	cw.writeImplicitVR(file, 0x0020, 0x000D, cw.padUID(studyUID)) // Study Instance UID
	cw.writeImplicitVR(file, 0x0020, 0x000E, cw.padUID(seriesUID)) // Series Instance UID
	cw.writeImplicitVR(file, 0x0020, 0x0010, cw.padString("1")) // Study ID
	cw.writeImplicitVR(file, 0x0020, 0x0011, cw.padString("1")) // Series Number
	cw.writeImplicitVR(file, 0x0020, 0x0013, cw.padString("1")) // Instance Number

	// Image Presentation - CRITICAL for JPEG!
	cw.writeImplicitVR(file, 0x0028, 0x0002, cw.uint16ToBytes(3)) // Samples per Pixel
	cw.writeImplicitVR(file, 0x0028, 0x0004, cw.padString("YBR_FULL_422")) // Photometric Interpretation for JPEG!
	cw.writeImplicitVR(file, 0x0028, 0x0006, cw.uint16ToBytes(0)) // Planar Configuration
	cw.writeImplicitVR(file, 0x0028, 0x0010, cw.uint16ToBytes(uint16(height))) // Rows
	cw.writeImplicitVR(file, 0x0028, 0x0011, cw.uint16ToBytes(uint16(width))) // Columns
	cw.writeImplicitVR(file, 0x0028, 0x0100, cw.uint16ToBytes(8)) // Bits Allocated
	cw.writeImplicitVR(file, 0x0028, 0x0101, cw.uint16ToBytes(8)) // Bits Stored
	cw.writeImplicitVR(file, 0x0028, 0x0102, cw.uint16ToBytes(7)) // High Bit
	cw.writeImplicitVR(file, 0x0028, 0x0103, cw.uint16ToBytes(0)) // Pixel Representation

	// Pixel Data - with UNDEFINED LENGTH for JPEG (like Cambridge!)
	// Tag
	binary.Write(file, binary.LittleEndian, uint16(0x7FE0))
	binary.Write(file, binary.LittleEndian, uint16(0x0010))
	
	// Length - UNDEFINED (0xFFFFFFFF) for encapsulated JPEG
	binary.Write(file, binary.LittleEndian, uint32(0xFFFFFFFF))
	
	// Basic Offset Table (empty)
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE)) // Item tag
	binary.Write(file, binary.LittleEndian, uint16(0xE000))
	binary.Write(file, binary.LittleEndian, uint32(0)) // Length 0
	
	// JPEG Fragment
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE)) // Item tag
	binary.Write(file, binary.LittleEndian, uint16(0xE000))
	binary.Write(file, binary.LittleEndian, uint32(len(jpegData))) // Fragment length
	file.Write(jpegData) // JPEG data
	
	// Sequence Delimiter
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE))
	binary.Write(file, binary.LittleEndian, uint16(0xE0DD))
	binary.Write(file, binary.LittleEndian, uint32(0))

	return nil
}

// writeExplicitVR writes with explicit VR (for File Meta Information)
func (cw *CambridgeStyleDicomWriter) writeExplicitVR(w interface{}, group, element uint16, vr string, data []byte) {
	writer := w.(interface{ Write([]byte) (int, error) })
	
	// Tag
	binary.Write(writer, binary.LittleEndian, group)
	binary.Write(writer, binary.LittleEndian, element)
	
	// VR
	writer.Write([]byte(vr))
	
	// Length
	if vr == "OB" || vr == "OW" || vr == "SQ" || vr == "UN" {
		writer.Write([]byte{0x00, 0x00}) // Reserved
		binary.Write(writer, binary.LittleEndian, uint32(len(data)))
	} else {
		binary.Write(writer, binary.LittleEndian, uint16(len(data)))
	}
	
	// Value
	writer.Write(data)
}

// writeImplicitVR writes with implicit VR (for main dataset)
func (cw *CambridgeStyleDicomWriter) writeImplicitVR(w interface{}, group, element uint16, data []byte) {
	writer := w.(interface{ Write([]byte) (int, error) })
	
	// Tag
	binary.Write(writer, binary.LittleEndian, group)
	binary.Write(writer, binary.LittleEndian, element)
	
	// Length
	binary.Write(writer, binary.LittleEndian, uint32(len(data)))
	
	// Value
	writer.Write(data)
}

func (cw *CambridgeStyleDicomWriter) padString(s string) []byte {
	b := []byte(s)
	if len(b)%2 != 0 {
		b = append(b, 0x20) // Space padding
	}
	return b
}

func (cw *CambridgeStyleDicomWriter) padUID(uid string) []byte {
	b := []byte(uid)
	if len(b)%2 != 0 {
		b = append(b, 0x00) // Null padding for UID
	}
	return b
}

func (cw *CambridgeStyleDicomWriter) uint16ToBytes(v uint16) []byte {
	b := make([]byte, 2)
	binary.LittleEndian.PutUint16(b, v)
	return b
}

func (cw *CambridgeStyleDicomWriter) uint32ToBytes(v uint32) []byte {
	b := make([]byte, 4)
	binary.LittleEndian.PutUint32(b, v)
	return b
}