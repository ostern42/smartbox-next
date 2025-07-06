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
      <!-- Camera Selection -->
      <section class="camera-section">
        <h2>Video Sources</h2>
        <div class="camera-grid">
          <div 
            v-for="camera in cameras" 
            :key="camera.id"
            :class="['camera-card', { 'active': selectedCamera?.id === camera.id }]"
            @click="selectCamera(camera)"
          >
            <div class="camera-icon">{{ getCameraIcon(camera.type) }}</div>
            <h3>{{ camera.name }}</h3>
            <span class="camera-type">{{ camera.type }}</span>
          </div>
          
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
          
          <!-- Captured Image -->
          <img 
            v-if="capturedImage" 
            :src="capturedImage" 
            alt="Captured image" 
            class="captured-image" 
          />
          
          <!-- Placeholder -->
          <div v-if="!isStreaming && !capturedImage" class="preview-placeholder">
            <p v-if="!selectedCamera && !selectedDevice">Select a camera to start</p>
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
        >
          ▶️ Start Preview
        </button>
        <button 
          v-else
          class="btn btn-secondary" 
          @click="stopPreview"
        >
          ⏹️ Stop Preview
        </button>
        
        <button 
          class="btn btn-primary" 
          @click="captureFromVideo"
          :disabled="!isStreaming"
        >
          📷 Capture
        </button>
        
        <button 
          v-if="capturedImage"
          class="btn btn-secondary" 
          @click="clearCapture"
        >
          🗑️ Clear
        </button>
        
        <button 
          class="btn btn-secondary" 
          @click="refreshAll"
        >
          🔄 Refresh
        </button>
      </section>

      <!-- Status Messages -->
      <section v-if="message" class="message-section">
        <div :class="['message', messageType]">
          {{ message }}
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { GetCameras, CaptureImage, GetSystemInfo } from '../wailsjs/go/main/App'

// State
const cameras = ref([])
const mediaDevices = ref([])
const selectedCamera = ref(null)
const selectedDevice = ref(null)
const systemInfo = ref({ version: '...', environment: 'loading', dicomEnabled: false })
const message = ref('')
const messageType = ref('info')
const capturedImage = ref(null)
const isStreaming = ref(false)
const videoElement = ref(null)
const currentStream = ref(null)

// Camera type icons
const getCameraIcon = (type) => {
  const icons = {
    'webcam': '📹',
    'usb': '🔌',
    'virtual': '💻',
    'sdi': '📡',
    'browser': '🌐'
  }
  return icons[type] || '📷'
}

// Get browser media devices
const getMediaDevices = async () => {
  try {
    const devices = await navigator.mediaDevices.enumerateDevices()
    mediaDevices.value = devices.filter(device => device.kind === 'videoinput')
    
    // Auto-select first device if none selected
    if (mediaDevices.value.length > 0 && !selectedDevice.value) {
      selectedDevice.value = mediaDevices.value[0]
    }
  } catch (error) {
    console.error('Failed to enumerate devices:', error)
    showMessage('Failed to access camera devices', 'error')
  }
}

// Load backend cameras
const refreshCameras = async () => {
  try {
    cameras.value = await GetCameras()
  } catch (error) {
    showMessage('Failed to load cameras: ' + error, 'error')
  }
}

// Refresh all
const refreshAll = async () => {
  await refreshCameras()
  await getMediaDevices()
  showMessage('Devices refreshed', 'success')
}

// Select camera (backend)
const selectCamera = (camera) => {
  selectedCamera.value = camera
  selectedDevice.value = null
  showMessage(`Selected: ${camera.name}`, 'info')
}

// Select media device (browser)
const selectMediaDevice = (device) => {
  selectedDevice.value = device
  selectedCamera.value = null
  showMessage(`Selected: ${device.label || 'Camera'}`, 'info')
  
  // Auto-start preview
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
    // Stop any existing stream
    if (currentStream.value) {
      stopPreview()
    }
    
    // Get video stream
    const constraints = {
      video: {
        deviceId: selectedDevice.value.deviceId,
        width: { ideal: 1920 },
        height: { ideal: 1080 }
      }
    }
    
    const stream = await navigator.mediaDevices.getUserMedia(constraints)
    currentStream.value = stream
    
    // Attach to video element
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
  showMessage('Preview stopped', 'info')
}

// Capture from video stream
const captureFromVideo = () => {
  if (!videoElement.value || !isStreaming.value) {
    showMessage('No video stream active', 'warning')
    return
  }
  
  try {
    // Create canvas
    const canvas = document.createElement('canvas')
    canvas.width = videoElement.value.videoWidth
    canvas.height = videoElement.value.videoHeight
    
    // Draw video frame to canvas
    const ctx = canvas.getContext('2d')
    ctx.drawImage(videoElement.value, 0, 0)
    
    // Convert to data URL
    capturedImage.value = canvas.toDataURL('image/jpeg', 0.9)
    showMessage('Image captured!', 'success')
    
    // Stop preview after capture
    stopPreview()
  } catch (error) {
    console.error('Capture failed:', error)
    showMessage('Capture failed: ' + error.message, 'error')
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

// Initialize
onMounted(async () => {
  loadSystemInfo()
  await refreshCameras()
  await getMediaDevices()
  
  // Request camera permissions
  try {
    await navigator.mediaDevices.getUserMedia({ video: true })
    await getMediaDevices() // Re-enumerate after permission
  } catch (error) {
    console.log('Camera permission denied or not available')
  }
})

// Cleanup
onUnmounted(() => {
  stopPreview()
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

/* Controls */
.controls-section {
  display: flex;
  gap: 1rem;
  flex-wrap: wrap;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
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
</style>