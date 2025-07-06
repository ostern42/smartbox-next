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

// WorkingDicomWriter - MINIMAL working DICOM writer
type WorkingDicomWriter struct {
	patient PatientInfo
	study   StudyInfo
}

// NewWorkingDicomWriter creates a new working DICOM writer
func NewWorkingDicomWriter() *WorkingDicomWriter {
	return &WorkingDicomWriter{}
}

// SetPatientInfo sets patient information
func (w *WorkingDicomWriter) SetPatientInfo(patient PatientInfo) {
	w.patient = patient
}

// SetStudyInfo sets study information
func (w *WorkingDicomWriter) SetStudyInfo(study StudyInfo) {
	w.study = study
}

// CreateFromDataURL creates a MINIMAL DICOM that works
func (w *WorkingDicomWriter) CreateFromDataURL(dataURL string, outputPath string) error {
	// Decode JPEG
	if !strings.HasPrefix(dataURL, "data:image/jpeg;base64,") {
		return fmt.Errorf("unsupported format")
	}
	
	base64Data := strings.TrimPrefix(dataURL, "data:image/jpeg;base64,")
	jpegData, err := base64.StdEncoding.DecodeString(base64Data)
	if err != nil {
		return err
	}
	
	// Get dimensions
	img, err := jpeg.Decode(bytes.NewReader(jpegData))
	if err != nil {
		return err
	}
	width := img.Bounds().Dx()
	height := img.Bounds().Dy()
	
	// Convert to RGB (MicroDicom needs this!)
	rgbData := make([]byte, width*height*3)
	idx := 0
	for y := 0; y < height; y++ {
		for x := 0; x < width; x++ {
			r, g, b, _ := img.At(x, y).RGBA()
			rgbData[idx] = byte(r >> 8)
			rgbData[idx+1] = byte(g >> 8)
			rgbData[idx+2] = byte(b >> 8)
			idx += 3
		}
	}
	
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
	
	// File Meta - MINIMAL with EXPLICIT VR
	w.tagExplicit(file, 0x0002, 0x0000, "UL", w.ul(132)) // Group Length
	w.tagExplicit(file, 0x0002, 0x0001, "OB", []byte{0x00, 0x01}) // Version
	w.tagExplicit(file, 0x0002, 0x0002, "UI", w.ui("1.2.840.10008.5.1.4.1.1.7")) // SOP Class
	w.tagExplicit(file, 0x0002, 0x0003, "UI", w.ui("1.2.3.4.5")) // SOP Instance
	w.tagExplicit(file, 0x0002, 0x0010, "UI", w.ui("1.2.840.10008.1.2.1")) // Transfer Syntax Explicit VR
	w.tagExplicit(file, 0x0002, 0x0012, "UI", w.ui("1.2.3.4")) // Implementation Class
	
	// Main Dataset - EXPLICIT VR for consistency
	w.tagExplicit(file, 0x0008, 0x0016, "UI", w.ui("1.2.840.10008.5.1.4.1.1.7")) // SOP Class
	w.tagExplicit(file, 0x0008, 0x0018, "UI", w.ui("1.2.3.4.5")) // SOP Instance
	w.tagExplicit(file, 0x0008, 0x0020, "DA", w.da(time.Now())) // Study Date
	w.tagExplicit(file, 0x0008, 0x0030, "TM", w.tm(time.Now())) // Study Time
	w.tagExplicit(file, 0x0008, 0x0060, "CS", w.cs("OT")) // Modality
	
	w.tagExplicit(file, 0x0010, 0x0010, "PN", w.pn(w.patient.Name)) // Patient Name
	w.tagExplicit(file, 0x0010, 0x0020, "LO", w.lo(w.patient.ID)) // Patient ID
	
	w.tagExplicit(file, 0x0020, 0x000D, "UI", w.ui("1.2.3.4.6")) // Study UID
	w.tagExplicit(file, 0x0020, 0x000E, "UI", w.ui("1.2.3.4.7")) // Series UID
	
	// Image - CRITICAL FOR MICRODICOM
	w.tagExplicit(file, 0x0028, 0x0002, "US", w.us(3)) // Samples per Pixel
	w.tagExplicit(file, 0x0028, 0x0004, "CS", w.cs("RGB")) // Photometric
	w.tagExplicit(file, 0x0028, 0x0006, "US", w.us(0)) // Planar Config
	w.tagExplicit(file, 0x0028, 0x0010, "US", w.us(uint16(height))) // Rows
	w.tagExplicit(file, 0x0028, 0x0011, "US", w.us(uint16(width))) // Columns
	w.tagExplicit(file, 0x0028, 0x0100, "US", w.us(8)) // Bits Allocated
	w.tagExplicit(file, 0x0028, 0x0101, "US", w.us(8)) // Bits Stored
	w.tagExplicit(file, 0x0028, 0x0102, "US", w.us(7)) // High Bit
	w.tagExplicit(file, 0x0028, 0x0103, "US", w.us(0)) // Pixel Representation
	
	// Pixel Data
	w.tagExplicit(file, 0x7FE0, 0x0010, "OB", rgbData)
	
	return nil
}

// tagExplicit writes a tag with explicit VR
func (w *WorkingDicomWriter) tagExplicit(file *os.File, group, elem uint16, vr string, data []byte) {
	binary.Write(file, binary.LittleEndian, group)
	binary.Write(file, binary.LittleEndian, elem)
	file.Write([]byte(vr))
	
	// OB, OW, OF, SQ, UT, UN need 4-byte length
	if vr == "OB" || vr == "OW" || vr == "OF" || vr == "SQ" || vr == "UT" || vr == "UN" {
		file.Write([]byte{0, 0}) // Reserved
		binary.Write(file, binary.LittleEndian, uint32(len(data)))
	} else {
		binary.Write(file, binary.LittleEndian, uint16(len(data)))
	}
	
	file.Write(data)
}

// Helper functions - MINIMAL
func (w *WorkingDicomWriter) ui(s string) []byte {
	b := []byte(s)
	if len(b)%2 != 0 { b = append(b, 0) }
	return b
}

func (w *WorkingDicomWriter) cs(s string) []byte {
	b := []byte(s)
	if len(b)%2 != 0 { b = append(b, ' ') }
	return b
}

func (w *WorkingDicomWriter) pn(s string) []byte {
	return w.cs(s)
}

func (w *WorkingDicomWriter) lo(s string) []byte {
	return w.cs(s)
}

func (w *WorkingDicomWriter) da(t time.Time) []byte {
	return []byte(t.Format("20060102"))
}

func (w *WorkingDicomWriter) tm(t time.Time) []byte {
	return []byte(t.Format("150405"))
}

func (w *WorkingDicomWriter) us(v uint16) []byte {
	b := make([]byte, 2)
	binary.LittleEndian.PutUint16(b, v)
	return b
}

func (w *WorkingDicomWriter) ul(v uint32) []byte {
	b := make([]byte, 4)
	binary.LittleEndian.PutUint32(b, v)
	return b
}