# Video to DICOM Quick Reference Implementation

## Quick Start Checklist

### ✅ Pre-Implementation
- [ ] Identify source video formats (MP4, AVI, MOV, etc.)
- [ ] Determine target PACS system and video support level
- [ ] Define quality requirements (lossy vs lossless)
- [ ] Establish performance targets (videos/hour)
- [ ] Review compliance requirements (HIPAA, GDPR)

### ✅ Technical Setup
- [ ] Choose primary library (pydicom/fo-dicom/DCMTK)
- [ ] Install video processing tools (FFmpeg/OpenCV)
- [ ] Setup development environment
- [ ] Configure test PACS instance
- [ ] Implement logging and monitoring

### ✅ Implementation Steps
1. [ ] Video validation and format detection
2. [ ] Metadata extraction and mapping
3. [ ] Video codec conversion (if needed)
4. [ ] DICOM dataset creation
5. [ ] Video data encapsulation
6. [ ] DICOM validation
7. [ ] PACS storage (C-STORE)
8. [ ] Error handling and retry logic

## Code Templates

### Python (pydicom) Quick Implementation

```python
import pydicom
from pydicom.dataset import Dataset, FileDataset
from pydicom.uid import generate_uid
import numpy as np
import cv2
from datetime import datetime

class QuickVideoToDICOM:
    def __init__(self):
        self.transfer_syntax = '1.2.840.10008.1.2.4.102.1'  # H.264
        
    def convert(self, video_path, patient_info):
        # Create DICOM dataset
        ds = self.create_base_dataset(patient_info)
        
        # Add video-specific attributes
        self.add_video_attributes(ds, video_path)
        
        # Encapsulate video data
        with open(video_path, 'rb') as f:
            ds.PixelData = f.read()
        
        # Set transfer syntax
        ds.file_meta.TransferSyntaxUID = self.transfer_syntax
        
        return ds
    
    def create_base_dataset(self, patient_info):
        # File meta info
        file_meta = pydicom.Dataset()
        file_meta.MediaStorageSOPClassUID = '1.2.840.10008.5.1.4.1.1.77.1.4.1'  # Video Photographic
        file_meta.MediaStorageSOPInstanceUID = generate_uid()
        file_meta.TransferSyntaxUID = self.transfer_syntax
        
        # Main dataset
        ds = FileDataset(None, {}, file_meta=file_meta, preamble=b"\0" * 128)
        
        # Patient info
        ds.PatientName = patient_info.get('name', 'Unknown')
        ds.PatientID = patient_info.get('id', 'Unknown')
        ds.PatientBirthDate = patient_info.get('birth_date', '')
        ds.PatientSex = patient_info.get('sex', '')
        
        # Study info
        ds.StudyInstanceUID = generate_uid()
        ds.StudyDate = datetime.now().strftime('%Y%m%d')
        ds.StudyTime = datetime.now().strftime('%H%M%S')
        ds.StudyDescription = patient_info.get('study_description', 'Video Study')
        
        # Series info
        ds.SeriesInstanceUID = generate_uid()
        ds.SeriesNumber = 1
        ds.Modality = 'XC'  # Video photographic imaging
        
        # Instance info
        ds.SOPClassUID = file_meta.MediaStorageSOPClassUID
        ds.SOPInstanceUID = file_meta.MediaStorageSOPInstanceUID
        ds.InstanceNumber = 1
        
        return ds
    
    def add_video_attributes(self, ds, video_path):
        # Get video properties
        cap = cv2.VideoCapture(video_path)
        
        # Frame information
        ds.NumberOfFrames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
        ds.FrameTime = 1000.0 / cap.get(cv2.CAP_PROP_FPS)  # milliseconds
        ds.CineRate = int(cap.get(cv2.CAP_PROP_FPS))
        
        # Image dimensions
        ds.Rows = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        ds.Columns = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        
        # Pixel information
        ds.SamplesPerPixel = 3  # RGB
        ds.PhotometricInterpretation = 'YBR_PARTIAL_420'
        ds.PlanarConfiguration = 0
        ds.BitsAllocated = 8
        ds.BitsStored = 8
        ds.HighBit = 7
        ds.PixelRepresentation = 0
        
        cap.release()

# Usage example
converter = QuickVideoToDICOM()
patient_info = {
    'name': 'DOE^JOHN',
    'id': '12345',
    'birth_date': '19800101',
    'sex': 'M',
    'study_description': 'Endoscopy Procedure'
}

dicom_dataset = converter.convert('input_video.mp4', patient_info)
dicom_dataset.save_as('output_video.dcm')
```

### C# (fo-dicom) Quick Implementation

```csharp
using Dicom;
using Dicom.Imaging;
using System;
using System.IO;

public class QuickVideoToDicom
{
    private readonly DicomTransferSyntax _transferSyntax = DicomTransferSyntax.MPEG4AVCH264HighProfileLevel41;
    
    public DicomFile Convert(string videoPath, PatientInfo patientInfo)
    {
        // Create new DICOM file
        var file = new DicomFile();
        var dataset = file.Dataset;
        
        // Add patient information
        dataset.Add(DicomTag.PatientName, patientInfo.Name);
        dataset.Add(DicomTag.PatientID, patientInfo.Id);
        dataset.Add(DicomTag.PatientBirthDate, patientInfo.BirthDate);
        dataset.Add(DicomTag.PatientSex, patientInfo.Sex);
        
        // Add study information
        dataset.Add(DicomTag.StudyInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.StudyDate, DateTime.Now);
        dataset.Add(DicomTag.StudyTime, DateTime.Now);
        dataset.Add(DicomTag.StudyDescription, patientInfo.StudyDescription);
        
        // Add series information
        dataset.Add(DicomTag.SeriesInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.SeriesNumber, 1);
        dataset.Add(DicomTag.Modality, "XC");
        
        // Add instance information
        dataset.Add(DicomTag.SOPClassUID, DicomUID.VideoPhotographicImageStorage);
        dataset.Add(DicomTag.SOPInstanceUID, DicomUID.Generate());
        dataset.Add(DicomTag.InstanceNumber, 1);
        
        // Add video data
        var videoBytes = File.ReadAllBytes(videoPath);
        var pixelData = new DicomOtherByteFragment(DicomTag.PixelData);
        pixelData.Fragments.Add(new MemoryByteBuffer(videoBytes));
        dataset.Add(pixelData);
        
        // Set transfer syntax
        file.FileMetaInfo.TransferSyntax = _transferSyntax;
        
        return file;
    }
}

public class PatientInfo
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string BirthDate { get; set; }
    public string Sex { get; set; }
    public string StudyDescription { get; set; }
}
```

## Configuration Templates

### Docker Configuration

```dockerfile
# Dockerfile
FROM python:3.9-slim

# Install system dependencies
RUN apt-get update && apt-get install -y \
    ffmpeg \
    libopencv-dev \
    python3-opencv \
    && rm -rf /var/lib/apt/lists/*

# Install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application
COPY . /app
WORKDIR /app

# Run application
CMD ["python", "video_converter.py"]
```

### Environment Configuration

```yaml
# config.yaml
application:
  name: "Video to DICOM Converter"
  version: "1.0.0"
  
video_processing:
  max_file_size: 5368709120  # 5GB
  supported_formats:
    - mp4
    - avi
    - mov
    - wmv
  output_codec: "h264"
  quality_preset: "high"  # low, medium, high, lossless
  
dicom:
  implementation_class_uid: "1.2.3.4.5.6.7.8.9"
  implementation_version_name: "VIDEO_CONV_1.0"
  transfer_syntax: "1.2.840.10008.1.2.4.102.1"
  
pacs:
  - name: "Main PACS"
    ae_title: "MAIN_PACS"
    host: "pacs.hospital.local"
    port: 11112
    timeout: 60
    
  - name: "Backup PACS"
    ae_title: "BACKUP_PACS"
    host: "backup.hospital.local"
    port: 11112
    timeout: 60
    
performance:
  max_parallel_conversions: 4
  worker_processes: 8
  memory_limit: "8GB"
  gpu_acceleration: true
  
storage:
  temp_directory: "/tmp/video_conversions"
  output_directory: "/data/dicom_output"
  retention_days: 7
  
monitoring:
  metrics_port: 9090
  log_level: "INFO"
  alert_email: "admin@hospital.local"
```

### Quick Deployment Script

```bash
#!/bin/bash
# deploy-video-converter.sh

# Check prerequisites
command -v docker >/dev/null 2>&1 || { echo "Docker required but not installed. Aborting." >&2; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "Docker Compose required but not installed. Aborting." >&2; exit 1; }

# Create directories
mkdir -p /opt/video-converter/{config,logs,temp,output}

# Copy configuration
cp config.yaml /opt/video-converter/config/

# Start services
docker-compose up -d

# Wait for services
echo "Waiting for services to start..."
sleep 10

# Test connectivity
docker exec video-converter python -c "import pydicom; print('pydicom OK')"
docker exec video-converter ffmpeg -version | head -n1

echo "Deployment complete. Access metrics at http://localhost:9090"
```

## Performance Tuning Quick Guide

### Memory Optimization
```python
# Use generators for large video processing
def process_video_chunks(video_path, chunk_size=100):
    cap = cv2.VideoCapture(video_path)
    while True:
        frames = []
        for _ in range(chunk_size):
            ret, frame = cap.read()
            if not ret:
                break
            frames.append(frame)
        
        if not frames:
            break
            
        yield frames
    
    cap.release()
```

### CPU Optimization
```python
# Use multiprocessing for parallel conversion
from multiprocessing import Pool

def parallel_convert(video_list):
    with Pool(processes=cpu_count()) as pool:
        results = pool.map(convert_single_video, video_list)
    return results
```

### GPU Acceleration
```python
# Enable GPU acceleration with OpenCV
cv2.cuda.setDevice(0)
gpu_frame = cv2.cuda_GpuMat()
gpu_frame.upload(cpu_frame)
# Process on GPU...
result = gpu_frame.download()
```

## Common Pitfalls and Solutions

| Pitfall | Solution |
|---------|----------|
| Memory overflow with large videos | Use chunked processing |
| Slow conversion speed | Enable multi-threading/GPU |
| PACS compatibility issues | Test transfer syntax support |
| Missing metadata | Implement comprehensive validation |
| Network timeouts | Implement retry logic with backoff |

## Testing Checklist

### Unit Tests
- [ ] Video format detection
- [ ] Metadata extraction
- [ ] DICOM tag validation
- [ ] Error handling

### Integration Tests
- [ ] End-to-end conversion
- [ ] PACS connectivity
- [ ] Performance benchmarks
- [ ] Quality validation

### Load Tests
- [ ] Concurrent conversions
- [ ] Large file handling
- [ ] Memory usage under load
- [ ] Network bandwidth usage

## Monitoring Queries

### Prometheus Queries
```promql
# Conversion rate
rate(video_conversions_total[5m])

# Error rate
rate(conversion_errors_total[5m]) / rate(video_conversions_total[5m])

# Average conversion time
rate(video_conversion_duration_seconds_sum[5m]) / rate(video_conversion_duration_seconds_count[5m])

# Queue depth
conversion_queue_size
```

### Log Analysis
```bash
# Find conversion errors
grep ERROR /var/log/video-converter.log | grep conversion

# Performance analysis
grep "Conversion completed" /var/log/video-converter.log | awk '{print $NF}' | stats

# Memory usage tracking
grep "Memory usage" /var/log/video-converter.log | tail -100
```

This quick reference provides immediate, actionable implementation guidance for video-to-DICOM conversion projects.