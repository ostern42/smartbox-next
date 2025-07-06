<template>
  <div id="app">
    <header>
      <h1>🏥 SmartBox Next</h1>
      <div class="status">
        <span :class="['status-dot', { 'online': systemInfo.dicomEnabled }]"></span>
        <span>{{ systemInfo.version }}</span>
      </div>
    </header>

    <main>
      <!-- Patient Info Section -->
      <section class="patient-section">
        <h2>Patient Information</h2>
        <div class="patient-form">
          <input 
            v-model="patient.name" 
            placeholder="Patient Name (Last^First)"
            class="input"
          />
          <input 
            v-model="patient.id" 
            placeholder="Patient ID"
            class="input"
          />
          <input 
            v-model="patient.birthDate" 
            type="date"
            class="input"
            @change="formatBirthDate"
          />
          <select v-model="patient.sex" class="input">
            <option value="">Select Sex</option>
            <option value="M">Male</option>
            <option value="F">Female</option>
            <option value="O">Other</option>
          </select>
        </div>
      </section>

      <!-- Study Info Section -->
      <section class="study-section">
        <h2>Study Information</h2>
        <div class="study-form">
          <input 
            v-model="study.accessionNumber" 
            placeholder="Accession Number"
            class="input"
          />
          <input 
            v-model="study.studyDescription" 
            placeholder="Study Description"
            class="input"
          />
          <input 
            v-model="study.referringPhysician" 
            placeholder="Referring Physician"
            class="input"
          />
          <input 
            v-model="study.institution" 
            placeholder="Institution"
            class="input"
          />
        </div>
      </section>

      <!-- Camera Selection -->
      <section class="camera-section">
        <h2>Video Sources</h2>
        <div class="camera-grid">
          <!-- Browser Cameras -->
          <div 
            v-for="device in mediaDevices" 
            :key="device.deviceId"
            :class="['camera-card', { 'active': selectedDevice?.deviceId === device.deviceId }]"
            @click="selectMediaDevice(device)"
          >
            <div class="camera-icon">📹</div>
            <h3>{{ device.label || `Camera ${device.deviceId.slice(0, 8)}` }}</h3>
            <span class="camera-type">browser</span>
          </div>
        </div>
      </section>

      <!-- Preview Area -->
      <section class="preview-section">
        <div class="preview-container">
          <!-- Live Video -->
          <video 
            v-show="isStreaming && !capturedImage" 
            ref="videoElement" 
            class="video-preview"
            autoplay
            playsinline
            muted
          ></video>
          
          <!-- Captured Image with Overlay -->
          <div v-if="capturedImage" class="capture-container">
            <img 
              :src="capturedImage" 
              alt="Captured image" 
              class="captured-image" 
            />
            <div v-if="showOverlay" class="patient-overlay">
              <div class="overlay-content">
                <div>{{ patient.name || 'No Patient' }}</div>
                <div>ID: {{ patient.id || 'No ID' }}</div>
                <div>{{ new Date().toLocaleString() }}</div>
              </div>
            </div>
          </div>
          
          <!-- Placeholder -->
          <div v-if="!isStreaming && !capturedImage" class="preview-placeholder">
            <p v-if="!selectedDevice">Select a camera to start</p>
            <p v-else>Ready to start preview</p>
          </div>
        </div>
      </section>

      <!-- Controls -->
      <section class="controls-section">
        <button 
          v-if="!isStreaming"
          class="btn btn-primary" 
          @click="startPreview"
          :disabled="!selectedDevice"
          title="Start Preview (S)"
        >
          ▶️ Start Preview
        </button>
        <button 
          v-else
          class="btn btn-secondary" 
          @click="stopPreview"
          title="Stop Preview (S)"
        >
          ⏹️ Stop
        </button>
        
        <button 
          class="btn btn-primary" 
          @click="captureFromVideo"
          :disabled="!isStreaming"
          title="Capture Image (Space/C)"
        >
          📷 Capture
        </button>
        
        <button 
          v-if="capturedImage"
          class="btn btn-success" 
          @click="exportDicom"
          :disabled="!patient.name || !patient.id"
          title="Export DICOM (E)"
        >
          💾 Export DICOM
        </button>
        
        <button 
          v-if="capturedImage"
          class="btn btn-secondary" 
          @click="clearCapture"
          title="Clear Capture (X)"
        >
          🗑️ Clear
        </button>
        
        <label class="checkbox">
          <input type="checkbox" v-model="showOverlay" />
          Show Overlay
        </label>
        
        <button 
          class="btn btn-secondary" 
          @click="openDicomFolder"
        >
          📁 DICOM Folder
        </button>
      </section>

      <!-- Status Messages -->
      <section v-if="message" class="message-section">
        <div :class="['message', messageType]">
          {{ message }}
        </div>
      </section>
      
      <!-- Keyboard Shortcuts Help -->
      <section class="shortcuts-help">
        <details>
          <summary>⌨️ Keyboard Shortcuts</summary>
          <div class="shortcuts-grid">
            <div><kbd>Space</kbd> or <kbd>C</kbd> - Capture</div>
            <div><kbd>S</kbd> - Start/Stop Preview</div>
            <div><kbd>E</kbd> - Export DICOM</div>
            <div><kbd>X</kbd> - Clear Capture</div>
            <div><kbd>O</kbd> - Toggle Overlay</div>
            <div><kbd>Esc</kbd> - Exit</div>
          </div>
        </details>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { 
  GetSystemInfo, 
  SetPatientInfo, 
  SetStudyInfo, 
  ExportDicom,
  OpenDicomFolder,
  Quit 
} from '../wailsjs/go/main/App'

// State
const mediaDevices = ref([])
const selectedDevice = ref(null)
const systemInfo = ref({ version: '...', environment: 'loading', dicomEnabled: false })
const message = ref('')
const messageType = ref('info')
const capturedImage = ref(null)
const isStreaming = ref(false)
const videoElement = ref(null)
const currentStream = ref(null)
const showOverlay = ref(true)

// Patient info
const patient = ref({
  name: 'Lohse^Erwin',
  id: 'PAT001',
  birthDate: '19301112',
  sex: 'M'
})

// Study info
const study = ref({
  accessionNumber: 'ACC001',
  studyDescription: 'SmartBox Capture',
  referringPhysician: 'Dr. Klöbner',
  performingPhysician: 'Dr. Müller-Lüdenscheidt',
  institution: 'Pappa ante Portas Klinik'
})

// Format birth date for DICOM (YYYYMMDD)
const formatBirthDate = (event) => {
  const date = new Date(event.target.value)
  patient.value.birthDate = date.toISOString().slice(0, 10).replace(/-/g, '')
}

// Get browser media devices
const getMediaDevices = async () => {
  try {
    const devices = await navigator.mediaDevices.enumerateDevices()
    mediaDevices.value = devices.filter(device => device.kind === 'videoinput')
    
    if (mediaDevices.value.length > 0 && !selectedDevice.value) {
      selectedDevice.value = mediaDevices.value[0]
    }
  } catch (error) {
    console.error('Failed to enumerate devices:', error)
    showMessage('Failed to access camera devices', 'error')
  }
}

// Select media device
const selectMediaDevice = (device) => {
  selectedDevice.value = device
  showMessage(`Selected: ${device.label || 'Camera'}`, 'info')
  
  if (!isStreaming.value) {
    startPreview()
  }
}

// Start video preview
const startPreview = async () => {
  if (!selectedDevice.value) {
    showMessage('Please select a camera first', 'warning')
    return
  }
  
  try {
    if (currentStream.value) {
      stopPreview()
    }
    
    const constraints = {
      video: {
        deviceId: selectedDevice.value.deviceId,
        width: { ideal: 1920 },
        height: { ideal: 1080 }
      }
    }
    
    const stream = await navigator.mediaDevices.getUserMedia(constraints)
    currentStream.value = stream
    
    if (videoElement.value) {
      videoElement.value.srcObject = stream
      isStreaming.value = true
      showMessage('Preview started', 'success')
    }
  } catch (error) {
    console.error('Failed to start preview:', error)
    showMessage('Failed to access camera: ' + error.message, 'error')
  }
}

// Stop video preview
const stopPreview = () => {
  if (currentStream.value) {
    currentStream.value.getTracks().forEach(track => track.stop())
    currentStream.value = null
  }
  
  if (videoElement.value) {
    videoElement.value.srcObject = null
  }
  
  isStreaming.value = false
}

// Capture from video stream
const captureFromVideo = () => {
  if (!videoElement.value || !isStreaming.value) {
    showMessage('No video stream active', 'warning')
    return
  }
  
  try {
    const canvas = document.createElement('canvas')
    canvas.width = videoElement.value.videoWidth
    canvas.height = videoElement.value.videoHeight
    
    const ctx = canvas.getContext('2d')
    ctx.drawImage(videoElement.value, 0, 0)
    
    // Add overlay if enabled
    if (showOverlay.value) {
      ctx.fillStyle = 'rgba(0, 0, 0, 0.7)'
      ctx.fillRect(0, 0, canvas.width, 80)
      
      ctx.fillStyle = 'white'
      ctx.font = '20px Arial'
      ctx.fillText(patient.value.name || 'No Patient', 20, 30)
      ctx.fillText(`ID: ${patient.value.id || 'No ID'}`, 20, 55)
      ctx.fillText(new Date().toLocaleString(), canvas.width - 250, 30)
    }
    
    capturedImage.value = canvas.toDataURL('image/jpeg', 0.7)
    showMessage('Image captured!', 'success')
    // Don't stop preview - keep streaming
  } catch (error) {
    console.error('Capture failed:', error)
    showMessage('Capture failed: ' + error.message, 'error')
  }
}

// Export to DICOM
const exportDicom = async () => {
  if (!capturedImage.value) {
    showMessage('No image to export', 'warning')
    return
  }
  
  if (!patient.value.name || !patient.value.id) {
    showMessage('Patient name and ID are required', 'warning')
    return
  }
  
  try {
    // Update patient info
    await SetPatientInfo(patient.value)
    
    // Update study info
    await SetStudyInfo(study.value)
    
    // Export DICOM
    const outputPath = await ExportDicom(capturedImage.value)
    showMessage(`DICOM exported: ${outputPath}`, 'success')
  } catch (error) {
    console.error('DICOM export failed:', error)
    showMessage('DICOM export failed: ' + error.message, 'error')
  }
}

// Open DICOM folder
const openDicomFolder = async () => {
  try {
    await OpenDicomFolder()
  } catch (error) {
    showMessage('Failed to open folder', 'error')
  }
}

// Clear captured image
const clearCapture = () => {
  capturedImage.value = null
  showMessage('Capture cleared', 'info')
}

// Show message
const showMessage = (text, type = 'info') => {
  message.value = text
  messageType.value = type
  setTimeout(() => {
    message.value = ''
  }, 3000)
}

// Load system info
const loadSystemInfo = async () => {
  try {
    systemInfo.value = await GetSystemInfo()
  } catch (error) {
    console.error('Failed to load system info:', error)
  }
}

// Keyboard shortcuts
const handleKeydown = (event) => {
  // Ignore if typing in input
  if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA') {
    return
  }
  
  switch(event.code) {
    case 'Space':
    case 'KeyC':
      event.preventDefault()
      captureImage()
      break
    case 'KeyS':
      event.preventDefault()
      if (isStreaming.value) {
        stopPreview()
      } else {
        startPreview()
      }
      break
    case 'KeyE':
      event.preventDefault()
      if (capturedImage.value) {
        exportDicom()
      }
      break
    case 'KeyX':
      event.preventDefault()
      clearCapture()
      break
    case 'KeyO':
      event.preventDefault()
      showOverlay.value = !showOverlay.value
      break
    case 'Escape':
      event.preventDefault()
      if (confirm('Exit SmartBox?')) {
        Quit()
      }
      break
  }
}

// Initialize
onMounted(async () => {
  loadSystemInfo()
  await getMediaDevices()
  
  // Add keyboard listener
  window.addEventListener('keydown', handleKeydown)
  
  try {
    await navigator.mediaDevices.getUserMedia({ video: true })
    await getMediaDevices()
  } catch (error) {
    console.log('Camera permission denied or not available')
  }
})

// Cleanup
onUnmounted(() => {
  stopPreview()
  window.removeEventListener('keydown', handleKeydown)
})
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  background: #0a0e1a;
  color: #e0e6ed;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

/* Header */
header {
  background: #1a1f2e;
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #2d3748;
}

header h1 {
  font-size: 1.5rem;
  font-weight: 600;
}

.status {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #a0aec0;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #e53e3e;
}

.status-dot.online {
  background: #48bb78;
}

/* Main */
main {
  flex: 1;
  padding: 2rem;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

/* Sections */
section {
  margin-bottom: 2rem;
}

section h2 {
  font-size: 1.25rem;
  margin-bottom: 1rem;
  color: #cbd5e0;
}

/* Forms */
.patient-form,
.study-form {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 1rem;
}

.input {
  background: #1a1f2e;
  border: 1px solid #2d3748;
  border-radius: 6px;
  padding: 0.75rem;
  color: #e0e6ed;
  font-size: 0.875rem;
}

.input:focus {
  outline: none;
  border-color: #4299e1;
}

/* Camera Grid */
.camera-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1rem;
}

.camera-card {
  background: #1a1f2e;
  border: 2px solid #2d3748;
  border-radius: 8px;
  padding: 1.5rem;
  text-align: center;
  cursor: pointer;
  transition: all 0.2s;
}

.camera-card:hover {
  border-color: #4a5568;
  transform: translateY(-2px);
}

.camera-card.active {
  border-color: #4299e1;
  background: #1e293b;
}

.camera-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.camera-card h3 {
  font-size: 1rem;
  margin-bottom: 0.25rem;
}

.camera-type {
  font-size: 0.75rem;
  color: #718096;
  text-transform: uppercase;
}

/* Preview */
.preview-container {
  background: #1a1f2e;
  border: 2px solid #2d3748;
  border-radius: 8px;
  aspect-ratio: 16/9;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  position: relative;
}

.preview-placeholder {
  text-align: center;
  color: #718096;
}

.video-preview,
.captured-image {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.video-preview {
  background: #000;
}

.capture-container {
  position: relative;
  width: 100%;
  height: 100%;
}

.patient-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  background: rgba(0, 0, 0, 0.7);
  color: white;
  padding: 1rem;
  font-size: 0.875rem;
}

.overlay-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

/* Controls */
.controls-section {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
  align-items: center;
}

.btn {
  min-height: 44px;
  min-width: 44px;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  touch-action: manipulation;
}

.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-primary {
  background: #4299e1;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #3182ce;
}

.btn-secondary {
  background: #2d3748;
  color: #e2e8f0;
}

.btn-secondary:hover:not(:disabled) {
  background: #4a5568;
}

.btn-success {
  background: #48bb78;
  color: white;
}

.btn-success:hover:not(:disabled) {
  background: #38a169;
}

.checkbox {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  cursor: pointer;
}

.checkbox input {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

/* Messages */
.message {
  padding: 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
}

.message.info {
  background: #2b6cb0;
  color: #bee3f8;
}

.message.success {
  background: #2f855a;
  color: #c6f6d5;
}

.message.warning {
  background: #c05621;
  color: #feebc8;
}

.message.error {
  background: #c53030;
  color: #fed7d7;
}

/* Keyboard Shortcuts */
.shortcuts-help {
  margin-top: 2rem;
  padding: 0 2rem;
  max-width: 1200px;
  margin-left: auto;
  margin-right: auto;
}

.shortcuts-help details {
  background: #1a1f2e;
  border: 1px solid #2d3748;
  border-radius: 6px;
  padding: 1rem;
}

.shortcuts-help summary {
  cursor: pointer;
  font-weight: 500;
  user-select: none;
}

.shortcuts-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 0.75rem;
  margin-top: 1rem;
  font-size: 0.875rem;
}

.shortcuts-grid kbd {
  background: #2d3748;
  border: 1px solid #4a5568;
  border-radius: 4px;
  padding: 0.25rem 0.5rem;
  font-family: monospace;
  font-size: 0.875rem;
}

/* Dark mode support */
@media (prefers-color-scheme: dark) {
  body {
    background: #0a0e1a;
    color: #e0e6ed;
  }
}

/* Touch-friendly adjustments */
@media (pointer: coarse) {
  .btn {
    min-height: 48px;
    font-size: 1.125rem;
  }
  
  .input {
    min-height: 48px;
    font-size: 1rem;
  }
}
</style>