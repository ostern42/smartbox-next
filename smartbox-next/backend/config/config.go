package config

import (
	"encoding/json"
	"os"
	"path/filepath"
	"sync"
	"time"
)

// Config represents the application configuration
type Config struct {
	mu sync.RWMutex
	
	// PACS Configuration
	PACS PACSConfig `json:"pacs"`
	
	// Application Settings
	App AppConfig `json:"app"`
	
	// Queue Settings
	Queue QueueConfig `json:"queue"`
	
	// Emergency Templates
	EmergencyTemplates []PatientTemplate `json:"emergencyTemplates"`
	
	// Last modified for remote sync
	LastModified time.Time `json:"lastModified"`
	Version      int       `json:"version"`
}

// PACSConfig holds PACS server configuration
type PACSConfig struct {
	Enabled        bool   `json:"enabled"`
	Host           string `json:"host"`
	Port           int    `json:"port"`
	CalledAETitle  string `json:"calledAETitle"`  // PACS AE Title
	CallingAETitle string `json:"callingAETitle"` // Our AE Title
	Timeout        int    `json:"timeout"`        // Seconds
	
	// Retry configuration
	MaxRetries     int `json:"maxRetries"`
	RetryDelay     int `json:"retryDelay"` // Seconds
	
	// TLS (future)
	UseTLS bool `json:"useTLS"`
}

// AppConfig holds general application settings
type AppConfig struct {
	Language      string `json:"language"`
	DarkMode      bool   `json:"darkMode"`
	AutoExport    bool   `json:"autoExport"`
	OutputDir     string `json:"outputDir"`
	LogLevel      string `json:"logLevel"`
	KioskMode     bool   `json:"kioskMode"`
}

// QueueConfig holds upload queue settings
type QueueConfig struct {
	MaxConcurrent  int    `json:"maxConcurrent"`
	RetryInterval  int    `json:"retryInterval"` // Minutes
	MaxQueueSize   int    `json:"maxQueueSize"`
	PersistenceDir string `json:"persistenceDir"`
}

// PatientTemplate for emergency cases
type PatientTemplate struct {
	ID          string `json:"id"`
	Name        string `json:"name"`
	DisplayName string `json:"displayName"`
	PatientName string `json:"patientName"`
	PatientID   string `json:"patientId"`
	Sex         string `json:"sex"`
	BirthDate   string `json:"birthDate"` // Can be relative like "TODAY-5Y"
	StudyDesc   string `json:"studyDescription"`
}

// ConfigManager handles configuration with resilience
type ConfigManager struct {
	config     *Config
	configPath string
	mu         sync.RWMutex
	
	// Backup paths for resilience
	backupPaths []string
	
	// Change callbacks
	onChange []func(*Config)
}

// NewConfigManager creates a resilient config manager
func NewConfigManager(primaryPath string) *ConfigManager {
	homeDir, _ := os.UserHomeDir()
	
	cm := &ConfigManager{
		configPath: primaryPath,
		config:     DefaultConfig(),
		backupPaths: []string{
			filepath.Join(homeDir, ".smartbox-next", "config.json"),
			filepath.Join(os.TempDir(), "smartbox-next-config.json"),
		},
	}
	
	// Try to load existing config
	cm.Load()
	
	return cm
}

// DefaultConfig returns default configuration
func DefaultConfig() *Config {
	homeDir, _ := os.UserHomeDir()
	
	return &Config{
		PACS: PACSConfig{
			Enabled:        false,
			Host:           "localhost",
			Port:           104,
			CalledAETitle:  "ORTHANC",
			CallingAETitle: "SMARTBOX",
			Timeout:        30,
			MaxRetries:     3,
			RetryDelay:     5,
			UseTLS:         false,
		},
		App: AppConfig{
			Language:   "de",
			DarkMode:   false,
			AutoExport: true,
			OutputDir:  filepath.Join(homeDir, "SmartBoxNext", "DICOM"),
			LogLevel:   "info",
			KioskMode:  false,
		},
		Queue: QueueConfig{
			MaxConcurrent:  2,
			RetryInterval:  5,
			MaxQueueSize:   1000,
			PersistenceDir: filepath.Join(homeDir, "SmartBoxNext", "Queue"),
		},
		EmergencyTemplates: []PatientTemplate{
			{
				ID:          "emergency-male",
				Name:        "emergency_male",
				DisplayName: "Notfall männlich",
				PatientName: "Notfall^Männlich",
				PatientID:   "EMERGENCY-M",
				Sex:         "M",
				BirthDate:   "TODAY-40Y",
				StudyDesc:   "Notfalluntersuchung",
			},
			{
				ID:          "emergency-female",
				Name:        "emergency_female",
				DisplayName: "Notfall weiblich",
				PatientName: "Notfall^Weiblich",
				PatientID:   "EMERGENCY-F",
				Sex:         "F",
				BirthDate:   "TODAY-40Y",
				StudyDesc:   "Notfalluntersuchung",
			},
			{
				ID:          "emergency-child",
				Name:        "emergency_child",
				DisplayName: "Notfall Kind",
				PatientName: "Notfall^Kind",
				PatientID:   "EMERGENCY-C",
				Sex:         "O",
				BirthDate:   "TODAY-10Y",
				StudyDesc:   "Notfalluntersuchung Kind",
			},
		},
		LastModified: time.Now(),
		Version:      1,
	}
}

// Load attempts to load config from multiple sources
func (cm *ConfigManager) Load() error {
	cm.mu.Lock()
	defer cm.mu.Unlock()
	
	// Try primary path first
	if err := cm.loadFromPath(cm.configPath); err == nil {
		return nil
	}
	
	// Try backup paths
	for _, path := range cm.backupPaths {
		if err := cm.loadFromPath(path); err == nil {
			// Save to primary path
			cm.saveToPath(cm.configPath)
			return nil
		}
	}
	
	// Use defaults and save
	cm.config = DefaultConfig()
	return cm.save()
}

// Save saves config to all locations for resilience
func (cm *ConfigManager) Save() error {
	cm.mu.Lock()
	defer cm.mu.Unlock()
	
	cm.config.LastModified = time.Now()
	cm.config.Version++
	
	return cm.save()
}

// GetPACS returns PACS configuration (thread-safe)
func (cm *ConfigManager) GetPACS() PACSConfig {
	cm.mu.RLock()
	defer cm.mu.RUnlock()
	return cm.config.PACS
}

// SetPACS updates PACS configuration
func (cm *ConfigManager) SetPACS(pacs PACSConfig) error {
	cm.mu.Lock()
	cm.config.PACS = pacs
	cm.mu.Unlock()
	
	if err := cm.Save(); err != nil {
		return err
	}
	
	cm.notifyChange()
	return nil
}

// GetEmergencyTemplates returns emergency templates
func (cm *ConfigManager) GetEmergencyTemplates() []PatientTemplate {
	cm.mu.RLock()
	defer cm.mu.RUnlock()
	
	templates := make([]PatientTemplate, len(cm.config.EmergencyTemplates))
	copy(templates, cm.config.EmergencyTemplates)
	return templates
}

// OnChange registers a callback for config changes
func (cm *ConfigManager) OnChange(callback func(*Config)) {
	cm.mu.Lock()
	defer cm.mu.Unlock()
	cm.onChange = append(cm.onChange, callback)
}

// Internal methods

func (cm *ConfigManager) loadFromPath(path string) error {
	data, err := os.ReadFile(path)
	if err != nil {
		return err
	}
	
	var config Config
	if err := json.Unmarshal(data, &config); err != nil {
		return err
	}
	
	cm.config = &config
	return nil
}

func (cm *ConfigManager) save() error {
	// Save to primary
	if err := cm.saveToPath(cm.configPath); err != nil {
		// Continue even if primary fails
	}
	
	// Save to backups
	for _, path := range cm.backupPaths {
		cm.saveToPath(path)
	}
	
	return nil
}

func (cm *ConfigManager) saveToPath(path string) error {
	// Ensure directory exists
	dir := filepath.Dir(path)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return err
	}
	
	// Marshal with indentation
	data, err := json.MarshalIndent(cm.config, "", "  ")
	if err != nil {
		return err
	}
	
	// Write atomically
	tempPath := path + ".tmp"
	if err := os.WriteFile(tempPath, data, 0644); err != nil {
		return err
	}
	
	return os.Rename(tempPath, path)
}

func (cm *ConfigManager) notifyChange() {
	cm.mu.RLock()
	config := cm.config
	callbacks := cm.onChange
	cm.mu.RUnlock()
	
	for _, cb := range callbacks {
		go cb(config)
	}
}

// ExportForRemote exports config for remote management
func (cm *ConfigManager) ExportForRemote() ([]byte, error) {
	cm.mu.RLock()
	defer cm.mu.RUnlock()
	
	return json.MarshalIndent(cm.config, "", "  ")
}

// ImportFromRemote imports config from remote management
func (cm *ConfigManager) ImportFromRemote(data []byte) error {
	var newConfig Config
	if err := json.Unmarshal(data, &newConfig); err != nil {
		return err
	}
	
	cm.mu.Lock()
	// Only import if newer
	if newConfig.Version > cm.config.Version {
		cm.config = &newConfig
		cm.mu.Unlock()
		
		if err := cm.Save(); err != nil {
			return err
		}
		
		cm.notifyChange()
	} else {
		cm.mu.Unlock()
	}
	
	return nil
}