package pacs

import (
	"context"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"smartbox-next/backend/config"
	"sync"
	"time"
)

// QueueItem represents an item in the upload queue
type QueueItem struct {
	ID           string    `json:"id"`
	DicomPath    string    `json:"dicomPath"`
	PatientName  string    `json:"patientName"`
	PatientID    string    `json:"patientId"`
	StudyDate    string    `json:"studyDate"`
	Status       Status    `json:"status"`
	Priority     Priority  `json:"priority"`
	AddedAt      time.Time `json:"addedAt"`
	LastAttempt  time.Time `json:"lastAttempt,omitempty"`
	AttemptCount int       `json:"attemptCount"`
	ErrorMessage string    `json:"errorMessage,omitempty"`
	ErrorType    ErrorType `json:"errorType,omitempty"`
}

// Status represents queue item status
type Status string

const (
	StatusPending    Status = "pending"
	StatusUploading  Status = "uploading"
	StatusSuccess    Status = "success"
	StatusFailed     Status = "failed"
	StatusCancelled  Status = "cancelled"
)

// Priority for queue items
type Priority int

const (
	PriorityNormal    Priority = 0
	PriorityHigh      Priority = 1
	PriorityEmergency Priority = 2
)

// UploadQueue manages DICOM uploads with persistence
type UploadQueue struct {
	config       *config.ConfigManager
	storeService *StoreService
	
	mu           sync.RWMutex
	items        map[string]*QueueItem
	queue        []string // IDs in order
	
	persistPath  string
	processing   bool
	stopChan     chan struct{}
	
	// Callbacks
	onStatusChange func(item *QueueItem)
}

// NewUploadQueue creates a persistent upload queue
func NewUploadQueue(configMgr *config.ConfigManager, storeService *StoreService) (*UploadQueue, error) {
	queueConfig := configMgr.GetPACS()
	homeDir, _ := os.UserHomeDir()
	
	q := &UploadQueue{
		config:       configMgr,
		storeService: storeService,
		items:        make(map[string]*QueueItem),
		queue:        make([]string, 0),
		persistPath:  filepath.Join(homeDir, "SmartBoxNext", "Queue", "queue.json"),
		stopChan:     make(chan struct{}),
	}
	
	// Load persisted queue
	if err := q.loadFromDisk(); err != nil {
		// Not fatal - just start with empty queue
		fmt.Printf("Could not load queue from disk: %v\n", err)
	}
	
	// Start processing
	go q.processLoop()
	
	return q, nil
}

// Add adds a DICOM file to the upload queue
func (q *UploadQueue) Add(dicomPath string, patientInfo map[string]string, priority Priority) (string, error) {
	q.mu.Lock()
	defer q.mu.Unlock()
	
	// Check if file exists
	if _, err := os.Stat(dicomPath); os.IsNotExist(err) {
		return "", fmt.Errorf("DICOM file not found: %s", dicomPath)
	}
	
	// Check queue size limit
	queueConfig := q.config.GetPACS()
	if len(q.items) >= 1000 { // Hard limit to prevent memory issues
		// Remove oldest completed items
		q.cleanupCompleted()
		
		if len(q.items) >= 1000 {
			return "", fmt.Errorf("queue is full (%d items)", len(q.items))
		}
	}
	
	// Create queue item
	item := &QueueItem{
		ID:          generateQueueID(),
		DicomPath:   dicomPath,
		PatientName: patientInfo["patientName"],
		PatientID:   patientInfo["patientId"],
		StudyDate:   patientInfo["studyDate"],
		Status:      StatusPending,
		Priority:    priority,
		AddedAt:     time.Now(),
	}
	
	// Add to queue
	q.items[item.ID] = item
	
	// Insert based on priority
	inserted := false
	for i, id := range q.queue {
		if q.items[id].Status == StatusPending && q.items[id].Priority < priority {
			// Insert before this item
			q.queue = append(q.queue[:i], append([]string{item.ID}, q.queue[i:]...)...)
			inserted = true
			break
		}
	}
	
	if !inserted {
		q.queue = append(q.queue, item.ID)
	}
	
	// Persist to disk
	q.saveToDisk()
	
	// Notify
	if q.onStatusChange != nil {
		go q.onStatusChange(item)
	}
	
	return item.ID, nil
}

// GetStatus returns the current queue status
func (q *UploadQueue) GetStatus() map[string]interface{} {
	q.mu.RLock()
	defer q.mu.RUnlock()
	
	pending := 0
	uploading := 0
	failed := 0
	success := 0
	
	for _, item := range q.items {
		switch item.Status {
		case StatusPending:
			pending++
		case StatusUploading:
			uploading++
		case StatusFailed:
			failed++
		case StatusSuccess:
			success++
		}
	}
	
	return map[string]interface{}{
		"total":     len(q.items),
		"pending":   pending,
		"uploading": uploading,
		"failed":    failed,
		"success":   success,
		"processing": q.processing,
	}
}

// GetItems returns queue items with optional filtering
func (q *UploadQueue) GetItems(status *Status, limit int) []*QueueItem {
	q.mu.RLock()
	defer q.mu.RUnlock()
	
	items := make([]*QueueItem, 0)
	
	for _, id := range q.queue {
		item := q.items[id]
		if status == nil || item.Status == *status {
			items = append(items, item)
			if limit > 0 && len(items) >= limit {
				break
			}
		}
	}
	
	return items
}

// Retry retries a failed item
func (q *UploadQueue) Retry(id string) error {
	q.mu.Lock()
	defer q.mu.Unlock()
	
	item, exists := q.items[id]
	if !exists {
		return fmt.Errorf("item not found: %s", id)
	}
	
	if item.Status != StatusFailed {
		return fmt.Errorf("can only retry failed items")
	}
	
	// Reset status
	item.Status = StatusPending
	item.AttemptCount = 0
	item.ErrorMessage = ""
	item.ErrorType = ErrorNone
	
	// Move to front of queue based on priority
	q.reorderQueue()
	
	// Persist
	q.saveToDisk()
	
	return nil
}

// Cancel cancels a pending item
func (q *UploadQueue) Cancel(id string) error {
	q.mu.Lock()
	defer q.mu.Unlock()
	
	item, exists := q.items[id]
	if !exists {
		return fmt.Errorf("item not found: %s", id)
	}
	
	if item.Status != StatusPending {
		return fmt.Errorf("can only cancel pending items")
	}
	
	item.Status = StatusCancelled
	
	// Persist
	q.saveToDisk()
	
	// Notify
	if q.onStatusChange != nil {
		go q.onStatusChange(item)
	}
	
	return nil
}

// SetStatusCallback sets the status change callback
func (q *UploadQueue) SetStatusCallback(callback func(*QueueItem)) {
	q.mu.Lock()
	defer q.mu.Unlock()
	q.onStatusChange = callback
}

// Stop stops the queue processing
func (q *UploadQueue) Stop() {
	close(q.stopChan)
}

// Internal methods

func (q *UploadQueue) processLoop() {
	ticker := time.NewTicker(5 * time.Second)
	defer ticker.Stop()
	
	for {
		select {
		case <-q.stopChan:
			return
		case <-ticker.C:
			q.processNext()
		}
	}
}

func (q *UploadQueue) processNext() {
	q.mu.Lock()
	
	// Check if already processing
	if q.processing {
		q.mu.Unlock()
		return
	}
	
	// Find next pending item
	var nextItem *QueueItem
	for _, id := range q.queue {
		item := q.items[id]
		if item.Status == StatusPending {
			nextItem = item
			break
		}
	}
	
	if nextItem == nil {
		q.mu.Unlock()
		return
	}
	
	// Mark as uploading
	nextItem.Status = StatusUploading
	nextItem.LastAttempt = time.Now()
	nextItem.AttemptCount++
	q.processing = true
	q.saveToDisk()
	q.mu.Unlock()
	
	// Notify
	if q.onStatusChange != nil {
		q.onStatusChange(nextItem)
	}
	
	// Attempt upload
	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Minute)
	defer cancel()
	
	result := q.storeService.StoreFile(ctx, nextItem.DicomPath)
	
	// Update status
	q.mu.Lock()
	if result.Success {
		nextItem.Status = StatusSuccess
		nextItem.ErrorMessage = ""
		nextItem.ErrorType = ErrorNone
	} else {
		nextItem.Status = StatusFailed
		nextItem.ErrorMessage = result.ErrorMessage
		nextItem.ErrorType = result.ErrorType
	}
	
	q.processing = false
	q.saveToDisk()
	q.mu.Unlock()
	
	// Notify
	if q.onStatusChange != nil {
		q.onStatusChange(nextItem)
	}
}

func (q *UploadQueue) loadFromDisk() error {
	// Ensure directory exists
	dir := filepath.Dir(q.persistPath)
	if err := os.MkdirAll(dir, 0755); err != nil {
		return err
	}
	
	// Read file
	data, err := os.ReadFile(q.persistPath)
	if err != nil {
		if os.IsNotExist(err) {
			return nil // Empty queue is OK
		}
		return err
	}
	
	// Parse JSON
	var saved struct {
		Items []QueueItem `json:"items"`
		Queue []string    `json:"queue"`
	}
	
	if err := json.Unmarshal(data, &saved); err != nil {
		return err
	}
	
	// Restore queue
	q.items = make(map[string]*QueueItem)
	for i := range saved.Items {
		item := &saved.Items[i]
		// Reset uploading status to pending
		if item.Status == StatusUploading {
			item.Status = StatusPending
		}
		q.items[item.ID] = item
	}
	
	q.queue = saved.Queue
	
	return nil
}

func (q *UploadQueue) saveToDisk() error {
	// Prepare data
	items := make([]QueueItem, 0, len(q.items))
	for _, item := range q.items {
		items = append(items, *item)
	}
	
	saved := struct {
		Items []QueueItem `json:"items"`
		Queue []string    `json:"queue"`
	}{
		Items: items,
		Queue: q.queue,
	}
	
	// Marshal
	data, err := json.MarshalIndent(saved, "", "  ")
	if err != nil {
		return err
	}
	
	// Write atomically
	tempPath := q.persistPath + ".tmp"
	if err := os.WriteFile(tempPath, data, 0644); err != nil {
		return err
	}
	
	return os.Rename(tempPath, q.persistPath)
}

func (q *UploadQueue) cleanupCompleted() {
	// Remove old successful items (keep last 100)
	successItems := make([]*QueueItem, 0)
	for _, item := range q.items {
		if item.Status == StatusSuccess {
			successItems = append(successItems, item)
		}
	}
	
	if len(successItems) > 100 {
		// Sort by time and remove oldest
		// TODO: Implement sorting
		for i := 0; i < len(successItems)-100; i++ {
			delete(q.items, successItems[i].ID)
		}
	}
}

func (q *UploadQueue) reorderQueue() {
	// Reorder queue based on priority and status
	// TODO: Implement proper sorting
}

func generateQueueID() string {
	return fmt.Sprintf("Q-%d", time.Now().UnixNano())
}