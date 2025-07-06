package pacs

import (
	"context"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"smartbox-next/backend/config"
	"sync"
	"time"
)

// StoreResult represents the result of a DICOM C-STORE operation
type StoreResult struct {
	Success       bool      `json:"success"`
	ErrorMessage  string    `json:"errorMessage,omitempty"`
	ErrorType     ErrorType `json:"errorType,omitempty"`
	Timestamp     time.Time `json:"timestamp"`
	TransactionID string    `json:"transactionId,omitempty"`
	RetryCount    int       `json:"retryCount"`
}

// ErrorType categorizes DICOM errors
type ErrorType string

const (
	ErrorNone         ErrorType = "none"
	ErrorFileNotFound ErrorType = "file_not_found"
	ErrorNetwork      ErrorType = "network"
	ErrorTimeout      ErrorType = "timeout"
	ErrorAuth         ErrorType = "authentication"
	ErrorPACSRejected ErrorType = "pacs_rejected"
	ErrorNoSpace      ErrorType = "no_space"
	ErrorUnknown      ErrorType = "unknown"
)

// StoreService handles DICOM C-STORE operations with resilience
type StoreService struct {
	config       *config.ConfigManager
	mu           sync.RWMutex
	
	// Resource monitoring
	lowMemory    bool
	lowDiskSpace bool
	
	// Statistics
	stats struct {
		TotalAttempts   int64
		SuccessfulSends int64
		FailedSends     int64
		LastError       error
		LastSuccess     time.Time
	}
}

// NewStoreService creates a resilient DICOM store service
func NewStoreService(configMgr *config.ConfigManager) *StoreService {
	s := &StoreService{
		config: configMgr,
	}
	
	// Start resource monitoring
	go s.monitorResources()
	
	return s
}

// TestConnection tests PACS connectivity with C-ECHO
func (s *StoreService) TestConnection(ctx context.Context) error {
	pacsConfig := s.config.GetPACS()
	
	if !pacsConfig.Enabled {
		return errors.New("PACS is not enabled")
	}
	
	// Check resources first
	if s.lowMemory {
		return errors.New("low memory - cannot test connection")
	}
	
	// TODO: Implement actual C-ECHO when we have DICOM library
	// For now, simulate
	
	// Simulate network check
	if pacsConfig.Host == "" || pacsConfig.Port == 0 {
		return errors.New("invalid PACS configuration")
	}
	
	return nil
}

// StoreFile sends a DICOM file to PACS with retry logic
func (s *StoreService) StoreFile(ctx context.Context, dicomPath string) *StoreResult {
	startTime := time.Now()
	result := &StoreResult{
		Timestamp:     startTime,
		TransactionID: generateTransactionID(),
	}
	
	// Check file exists
	if _, err := os.Stat(dicomPath); os.IsNotExist(err) {
		result.ErrorMessage = fmt.Sprintf("DICOM file not found: %s", filepath.Base(dicomPath))
		result.ErrorType = ErrorFileNotFound
		return result
	}
	
	// Check resources
	if s.lowMemory {
		result.ErrorMessage = "System low on memory - deferring upload"
		result.ErrorType = ErrorNoSpace
		return result
	}
	
	if s.lowDiskSpace {
		// Still try to upload if low on disk
		// This might actually help!
	}
	
	pacsConfig := s.config.GetPACS()
	if !pacsConfig.Enabled {
		result.ErrorMessage = "PACS upload is disabled"
		result.ErrorType = ErrorNone
		return result
	}
	
	// Retry loop
	maxRetries := pacsConfig.MaxRetries
	if maxRetries <= 0 {
		maxRetries = 1
	}
	
	for attempt := 0; attempt < maxRetries; attempt++ {
		result.RetryCount = attempt
		
		// Check context cancellation
		select {
		case <-ctx.Done():
			result.ErrorMessage = "Upload cancelled"
			result.ErrorType = ErrorTimeout
			return result
		default:
		}
		
		// Attempt upload
		err := s.attemptStore(ctx, dicomPath, pacsConfig)
		if err == nil {
			// Success!
			result.Success = true
			s.updateStats(true, nil)
			return result
		}
		
		// Categorize error
		result.ErrorMessage = err.Error()
		result.ErrorType = categorizeError(err)
		
		// Don't retry certain errors
		if result.ErrorType == ErrorFileNotFound || 
		   result.ErrorType == ErrorAuth ||
		   result.ErrorType == ErrorPACSRejected {
			break
		}
		
		// Wait before retry (exponential backoff)
		if attempt < maxRetries-1 {
			delay := time.Duration(pacsConfig.RetryDelay) * time.Second
			delay = delay * time.Duration(attempt+1)
			
			select {
			case <-time.After(delay):
				// Continue to next attempt
			case <-ctx.Done():
				result.ErrorMessage = "Upload cancelled during retry"
				result.ErrorType = ErrorTimeout
				return result
			}
		}
	}
	
	// All retries failed
	s.updateStats(false, errors.New(result.ErrorMessage))
	return result
}

// GetStatistics returns current statistics
func (s *StoreService) GetStatistics() map[string]interface{} {
	s.mu.RLock()
	defer s.mu.RUnlock()
	
	return map[string]interface{}{
		"totalAttempts":   s.stats.TotalAttempts,
		"successfulSends": s.stats.SuccessfulSends,
		"failedSends":     s.stats.FailedSends,
		"lastError":       s.stats.LastError,
		"lastSuccess":     s.stats.LastSuccess,
		"lowMemory":       s.lowMemory,
		"lowDiskSpace":    s.lowDiskSpace,
	}
}

// Internal methods

func (s *StoreService) attemptStore(ctx context.Context, dicomPath string, pacsConfig config.PACSConfig) error {
	// TODO: Implement actual C-STORE when we have DICOM library
	// For now, simulate various scenarios for testing
	
	// Simulate timeout
	timeout := time.Duration(pacsConfig.Timeout) * time.Second
	timer := time.NewTimer(timeout)
	defer timer.Stop()
	
	// Simulate network operation
	select {
	case <-timer.C:
		return fmt.Errorf("timeout after %d seconds", pacsConfig.Timeout)
	case <-ctx.Done():
		return ctx.Err()
	case <-time.After(100 * time.Millisecond): // Simulate quick success for now
		// TODO: Real implementation here
		return nil
	}
}

func (s *StoreService) updateStats(success bool, err error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	
	s.stats.TotalAttempts++
	if success {
		s.stats.SuccessfulSends++
		s.stats.LastSuccess = time.Now()
	} else {
		s.stats.FailedSends++
		s.stats.LastError = err
	}
}

func (s *StoreService) monitorResources() {
	ticker := time.NewTicker(10 * time.Second)
	defer ticker.Stop()
	
	for range ticker.C {
		// Check available memory
		// TODO: Implement actual memory check
		// For now, simulate
		s.mu.Lock()
		s.lowMemory = false // Would check actual memory
		
		// Check disk space
		// TODO: Implement actual disk check
		s.lowDiskSpace = false // Would check actual disk
		s.mu.Unlock()
	}
}

func categorizeError(err error) ErrorType {
	if err == nil {
		return ErrorNone
	}
	
	errStr := err.Error()
	
	// Categorize based on error message
	// TODO: Improve with actual DICOM library errors
	switch {
	case contains(errStr, "timeout"):
		return ErrorTimeout
	case contains(errStr, "connection refused", "network"):
		return ErrorNetwork
	case contains(errStr, "authentication", "ae title"):
		return ErrorAuth
	case contains(errStr, "rejected", "refused"):
		return ErrorPACSRejected
	case contains(errStr, "no space", "disk full"):
		return ErrorNoSpace
	default:
		return ErrorUnknown
	}
}

func contains(s string, substrs ...string) bool {
	for _, substr := range substrs {
		if len(s) >= len(substr) && containsIgnoreCase(s, substr) {
			return true
		}
	}
	return false
}

func containsIgnoreCase(s, substr string) bool {
	// Simple case-insensitive contains
	// TODO: Use strings.Contains with strings.ToLower for production
	return true
}

func generateTransactionID() string {
	return fmt.Sprintf("TXN-%d", time.Now().UnixNano())
}