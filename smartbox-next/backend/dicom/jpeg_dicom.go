package dicom

import (
	"bytes"
	"encoding/base64"
	"encoding/binary"
	"fmt"
	"image/jpeg"
	"os"
	"path/filepath"
	"strings"
	"time"
)

// JpegDicomWriter - DICOM writer with JPEG compression
type JpegDicomWriter struct {
	patient PatientInfo
	study   StudyInfo
}

// NewJpegDicomWriter creates a new JPEG DICOM writer
func NewJpegDicomWriter() *JpegDicomWriter {
	return &JpegDicomWriter{}
}

// SetPatientInfo sets patient information
func (w *JpegDicomWriter) SetPatientInfo(patient PatientInfo) {
	w.patient = patient
}

// GetPatientInfo returns current patient information
func (w *JpegDicomWriter) GetPatientInfo() PatientInfo {
	return w.patient
}

// SetStudyInfo sets study information
func (w *JpegDicomWriter) SetStudyInfo(study StudyInfo) {
	w.study = study
}

// GetStudyInfo returns current study information
func (w *JpegDicomWriter) GetStudyInfo() StudyInfo {
	return w.study
}

// CreateFromDataURL creates a DICOM with JPEG compression
func (w *JpegDicomWriter) CreateFromDataURL(dataURL string, outputPath string) error {
	// Extract JPEG data
	if !strings.HasPrefix(dataURL, "data:image/jpeg;base64,") {
		return fmt.Errorf("unsupported format")
	}
	
	base64Data := strings.TrimPrefix(dataURL, "data:image/jpeg;base64,")
	jpegData, err := base64.StdEncoding.DecodeString(base64Data)
	if err != nil {
		return err
	}
	
	// Get actual dimensions from JPEG
	img, err := jpeg.DecodeConfig(bytes.NewReader(jpegData))
	if err != nil {
		return fmt.Errorf("failed to decode JPEG config: %v", err)
	}
	width := uint16(img.Width)
	height := uint16(img.Height)
	
	// Create file
	os.MkdirAll(filepath.Dir(outputPath), 0755)
	file, err := os.Create(outputPath)
	if err != nil {
		return err
	}
	defer file.Close()
	
	// DICOM Header
	file.Write(make([]byte, 128)) // Preamble
	file.Write([]byte("DICM"))     // Prefix
	
	// File Meta Information - JPEG Transfer Syntax
	w.tagExplicit(file, 0x0002, 0x0000, "UL", w.ul(166)) // Group Length (adjusted for JPEG)
	w.tagExplicit(file, 0x0002, 0x0001, "OB", []byte{0x00, 0x01}) // Version
	w.tagExplicit(file, 0x0002, 0x0002, "UI", w.ui("1.2.840.10008.5.1.4.1.1.7")) // SOP Class (Secondary Capture)
	w.tagExplicit(file, 0x0002, 0x0003, "UI", w.ui(w.generateUID())) // SOP Instance UID
	w.tagExplicit(file, 0x0002, 0x0010, "UI", w.ui("1.2.840.10008.1.2.4.50")) // Transfer Syntax: JPEG Baseline
	w.tagExplicit(file, 0x0002, 0x0012, "UI", w.ui("1.2.3.4")) // Implementation Class UID
	
	// Main Dataset - MUST use Explicit VR with JPEG!
	w.tagExplicit(file, 0x0008, 0x0016, "UI", w.ui("1.2.840.10008.5.1.4.1.1.7")) // SOP Class
	w.tagExplicit(file, 0x0008, 0x0018, "UI", w.ui(w.generateUID())) // SOP Instance
	w.tagExplicit(file, 0x0008, 0x0020, "DA", w.da(time.Now())) // Study Date
	w.tagExplicit(file, 0x0008, 0x0030, "TM", w.tm(time.Now())) // Study Time
	w.tagExplicit(file, 0x0008, 0x0060, "CS", w.cs("OT")) // Modality
	
	w.tagExplicit(file, 0x0010, 0x0010, "PN", w.pn(w.patient.Name)) // Patient Name
	w.tagExplicit(file, 0x0010, 0x0020, "LO", w.lo(w.patient.ID)) // Patient ID
	
	w.tagExplicit(file, 0x0020, 0x000D, "UI", w.ui(w.generateUID())) // Study Instance UID
	w.tagExplicit(file, 0x0020, 0x000E, "UI", w.ui(w.generateUID())) // Series Instance UID
	
	// Image attributes for JPEG with actual dimensions
	w.tagExplicit(file, 0x0028, 0x0002, "US", w.us(3)) // Samples per Pixel
	w.tagExplicit(file, 0x0028, 0x0004, "CS", w.cs("YBR_FULL_422")) // Photometric for JPEG
	w.tagExplicit(file, 0x0028, 0x0006, "US", w.us(0)) // Planar Configuration - REQUIRED!
	w.tagExplicit(file, 0x0028, 0x0010, "US", w.us(height)) // Rows
	w.tagExplicit(file, 0x0028, 0x0011, "US", w.us(width)) // Columns
	w.tagExplicit(file, 0x0028, 0x0100, "US", w.us(8)) // Bits Allocated
	w.tagExplicit(file, 0x0028, 0x0101, "US", w.us(8)) // Bits Stored
	w.tagExplicit(file, 0x0028, 0x0102, "US", w.us(7)) // High Bit
	w.tagExplicit(file, 0x0028, 0x0103, "US", w.us(0)) // Pixel Representation
	
	// Encapsulated Pixel Data
	w.writeEncapsulatedPixelData(file, jpegData)
	
	return nil
}

// writeEncapsulatedPixelData writes JPEG data in encapsulated format
func (w *JpegDicomWriter) writeEncapsulatedPixelData(file *os.File, jpegData []byte) {
	// Pixel Data tag with Explicit VR
	binary.Write(file, binary.LittleEndian, uint16(0x7FE0))
	binary.Write(file, binary.LittleEndian, uint16(0x0010))
	file.Write([]byte("OB"))
	file.Write([]byte{0x00, 0x00}) // Reserved
	binary.Write(file, binary.LittleEndian, uint32(0xFFFFFFFF)) // Undefined length
	
	// Basic Offset Table (empty)
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE)) // Item tag
	binary.Write(file, binary.LittleEndian, uint16(0xE000))
	binary.Write(file, binary.LittleEndian, uint32(0)) // Empty table
	
	// Fragment with JPEG data
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE)) // Item tag
	binary.Write(file, binary.LittleEndian, uint16(0xE000))
	binary.Write(file, binary.LittleEndian, uint32(len(jpegData)))
	file.Write(jpegData)
	
	// Sequence Delimiter
	binary.Write(file, binary.LittleEndian, uint16(0xFFFE))
	binary.Write(file, binary.LittleEndian, uint16(0xE0DD))
	binary.Write(file, binary.LittleEndian, uint32(0))
}

// tagExplicit writes a tag with explicit VR (for File Meta)
func (w *JpegDicomWriter) tagExplicit(file *os.File, group, elem uint16, vr string, data []byte) {
	binary.Write(file, binary.LittleEndian, group)
	binary.Write(file, binary.LittleEndian, elem)
	file.Write([]byte(vr))
	
	if vr == "OB" || vr == "OW" || vr == "OF" || vr == "SQ" || vr == "UT" || vr == "UN" {
		file.Write([]byte{0, 0})
		binary.Write(file, binary.LittleEndian, uint32(len(data)))
	} else {
		binary.Write(file, binary.LittleEndian, uint16(len(data)))
	}
	
	file.Write(data)
}



// Helper functions
func (w *JpegDicomWriter) ui(s string) []byte {
	b := []byte(s)
	if len(b)%2 != 0 { b = append(b, 0) }
	return b
}

func (w *JpegDicomWriter) cs(s string) []byte {
	b := []byte(s)
	if len(b)%2 != 0 { b = append(b, ' ') }
	return b
}

func (w *JpegDicomWriter) pn(s string) []byte {
	return w.cs(s)
}

func (w *JpegDicomWriter) lo(s string) []byte {
	return w.cs(s)
}

func (w *JpegDicomWriter) da(t time.Time) []byte {
	return []byte(t.Format("20060102"))
}

func (w *JpegDicomWriter) tm(t time.Time) []byte {
	return []byte(t.Format("150405"))
}

func (w *JpegDicomWriter) us(v uint16) []byte {
	b := make([]byte, 2)
	binary.LittleEndian.PutUint16(b, v)
	return b
}

func (w *JpegDicomWriter) ul(v uint32) []byte {
	b := make([]byte, 4)
	binary.LittleEndian.PutUint32(b, v)
	return b
}

func (w *JpegDicomWriter) generateUID() string {
	// Simple UID generation (in production use proper UID generation)
	return fmt.Sprintf("1.2.3.4.%d.%d", time.Now().Unix(), time.Now().Nanosecond())
}