/**
 * StreamingWebSocketHandler
 * Real-time streaming updates handler for Phase 1 Foundation Integration
 * Provides automatic reconnection with exponential backoff and comprehensive message handling
 * 
 * Features:
 * - Automatic reconnection with exponential backoff and jitter
 * - Message type handling for all streaming events (segments, recordings, thumbnails, markers)
 * - Error handling and recovery with connection state management
 * - Heartbeat monitoring for connection health
 * - Message queuing for offline scenarios
 * - Session-based WebSocket connections
 */

class StreamingWebSocketHandler {
    constructor(player) {
        this.player = player;
        this.sessionId = null;
        this.url = null;
        this.ws = null;
        
        // Reconnection management - matches Phase 1 specs
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000; // Base delay in ms
        this.reconnectTimer = null;
        this.isConnecting = false;
        this.shouldReconnect = true;
        
        // Connection state tracking
        this.connectionState = 'disconnected'; // disconnected, connecting, connected, error
        
        // Message queue for offline scenarios
        this.messageQueue = [];
        this.maxQueueSize = 100;
        
        // Heartbeat monitoring for connection health
        this.heartbeatInterval = null;
        this.heartbeatTimeout = 30000; // 30 seconds
        this.lastHeartbeat = null;
        
        // Performance metrics
        this.messageStats = {
            sent: 0,
            received: 0,
            errors: 0,
            reconnections: 0
        };
        
        // Event handlers registry
        this.eventHandlers = new Map();
        this.setupDefaultHandlers();
    }
    
    // Setup default event handlers as per Phase 1 specification
    setupDefaultHandlers() {
        // Core streaming events from Phase 1 spec
        this.eventHandlers.set('SegmentCompleted', (data) => {
            if (this.player.onNewSegment) {
                this.player.onNewSegment(data);
            }
            this.player.emit('segmentCompleted', data);
        });
        
        this.eventHandlers.set('RecordingStatus', (data) => {
            if (this.player.updateRecordingStatus) {
                this.player.updateRecordingStatus(data);
            }
            this.player.emit('recordingStatus', data);
        });
        
        this.eventHandlers.set('ThumbnailReady', (data) => {
            if (this.player.timeline) {
                this.player.timeline.updateThumbnail(data);
            } else if (this.player.onThumbnailReady) {
                this.player.onThumbnailReady(data);
            }
            this.player.emit('thumbnailReady', data);
        });
        
        this.eventHandlers.set('MarkerAdded', (data) => {
            if (this.player.timeline) {
                this.player.timeline.addMarker(data);
            }
            this.player.emit('markerAdded', data);
        });
        
        this.eventHandlers.set('Error', (data) => {
            if (this.player.handleStreamError) {
                this.player.handleStreamError(data);
            }
            this.player.emit('streamError', data);
        });
    }
    
    connect(sessionId) {
        this.sessionId = sessionId;
        this.url = `ws://${window.location.host}/ws/video/${sessionId}`;
        this.shouldReconnect = true;
        this.establishConnection();
    }
    
    establishConnection() {
        if (this.isConnecting || this.connectionState === 'connected') {
            return;
        }
        
        this.isConnecting = true;
        this.connectionState = 'connecting';
        
        try {
            console.log(`Connecting to streaming WebSocket: ${this.url}`);
            this.ws = new WebSocket(this.url);
            this.setupWebSocketEvents();
        } catch (error) {
            console.error('Failed to create WebSocket connection:', error);
            this.isConnecting = false;
            this.handleConnectionError(error);
        }
    }
    
    setupWebSocketEvents() {
        this.ws.onopen = () => {
            console.log(`Streaming WebSocket connected to session: ${this.sessionId}`);
            this.isConnecting = false;
            this.connectionState = 'connected';
            
            // Track reconnection stats
            if (this.reconnectAttempts > 0) {
                this.messageStats.reconnections++;
                console.log(`Reconnection successful after ${this.reconnectAttempts} attempts`);
            }
            this.reconnectAttempts = 0;
            
            // Process queued messages
            this.processMessageQueue();
            
            // Start heartbeat monitoring
            this.startHeartbeat();
            
            // Notify player of successful connection
            this.player.emit('websocketConnected', {
                sessionId: this.sessionId,
                url: this.url,
                reconnected: this.messageStats.reconnections > 0
            });
        };
        
        this.ws.onmessage = (event) => {
            try {
                const message = JSON.parse(event.data);
                this.messageStats.received++;
                this.handleMessage(message);
                
                // Update last heartbeat time for connection health
                this.lastHeartbeat = Date.now();
            } catch (error) {
                console.error('Failed to parse WebSocket message:', error, event.data);
                this.messageStats.errors++;
                this.player.emit('messageParseError', { error, data: event.data });
            }
        };
        
        this.ws.onerror = (error) => {
            console.error('WebSocket error:', error);
            this.connectionState = 'error';
            this.player.emit('websocketError', error);
        };
        
        this.ws.onclose = (event) => {
            console.log('WebSocket disconnected:', event.code, event.reason);
            this.isConnecting = false;
            this.connectionState = 'disconnected';
            
            // Stop heartbeat
            this.stopHeartbeat();
            
            // Notify player
            this.player.emit('websocketDisconnected', { code: event.code, reason: event.reason });
            
            // Handle disconnection and potential reconnection
            this.handleDisconnection(event);
        };
    }
    
    handleMessage(message) {
        console.log(`WebSocket message received: ${message.type} for session ${this.sessionId}`);
        
        // Use event handlers registry for core Phase 1 events
        const handler = this.eventHandlers.get(message.type);
        if (handler) {
            handler(message.data);
            return;
        }
        
        // Handle additional message types not in core Phase 1 spec
        switch (message.type) {
            case 'Warning':
                console.warn('Stream warning:', message.data);
                this.player.emit('streamWarning', message.data);
                break;
                
            case 'Heartbeat':
                // Respond to server heartbeat
                this.send({ type: 'HeartbeatResponse', timestamp: Date.now() });
                break;
                
            case 'SessionInfo':
                // Session metadata updates
                this.player.emit('sessionInfo', message.data);
                break;
                
            case 'BufferStatus':
                // Buffer health monitoring
                this.player.emit('bufferStatus', message.data);
                break;
                
            case 'RecordingStarted':
                // Recording lifecycle event
                this.player.emit('recordingStarted', message.data);
                break;
                
            case 'RecordingStopped':
                // Recording lifecycle event  
                this.player.emit('recordingStopped', message.data);
                break;
                
            case 'RecordingPaused':
                // Recording lifecycle event
                this.player.emit('recordingPaused', message.data);
                break;
                
            case 'RecordingResumed':
                // Recording lifecycle event
                this.player.emit('recordingResumed', message.data);
                break;
                
            case 'SnapshotTaken':
                // Snapshot capture event
                this.player.emit('snapshotTaken', message.data);
                break;
                
            case 'StatusUpdate':
                // General status updates
                this.player.emit('statusUpdate', message.data);
                break;
                
            case 'EngineWarning':
                // Non-fatal VideoEngine warnings
                console.warn('VideoEngine warning:', message.data);
                this.player.emit('engineWarning', message.data);
                break;
                
            default:
                console.log('Unhandled WebSocket message type:', message.type, message.data);
                this.player.emit('unknownMessage', message);
        }
    }
    
    handleDisconnection(event) {
        if (!this.shouldReconnect) {
            return;
        }
        
        // Determine if we should attempt reconnection
        const shouldAttemptReconnect = this.shouldAttemptReconnection(event);
        
        if (shouldAttemptReconnect && this.reconnectAttempts < this.maxReconnectAttempts) {
            this.scheduleReconnection();
        } else {
            console.error('Max reconnection attempts reached or permanent failure');
            this.player.emit('connectionFailed', {
                attempts: this.reconnectAttempts,
                lastError: event
            });
        }
    }
    
    shouldAttemptReconnection(event) {
        // Don't reconnect on certain close codes
        const noReconnectCodes = [
            1000, // Normal closure
            1001, // Going away
            1005, // No status received
            4000, // Custom: Session ended
            4001, // Custom: Authentication failed
            4002  // Custom: Invalid session
        ];
        
        return !noReconnectCodes.includes(event.code);
    }
    
    scheduleReconnection() {
        if (this.reconnectTimer) {
            clearTimeout(this.reconnectTimer);
        }
        
        this.reconnectAttempts++;
        
        // Exponential backoff with jitter
        const baseDelay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
        const jitter = Math.random() * 1000; // 0-1 second jitter
        const delay = Math.min(baseDelay + jitter, 30000); // Max 30 seconds
        
        console.log(`Scheduling reconnection attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts} in ${delay}ms`);
        
        this.reconnectTimer = setTimeout(() => {
            this.establishConnection();
        }, delay);
    }
    
    handleConnectionError(error) {
        this.connectionState = 'error';
        this.player.emit('websocketError', error);
        
        if (this.shouldReconnect) {
            this.handleDisconnection({ code: 1006, reason: 'Connection failed' });
        }
    }
    
    send(message) {
        if (this.connectionState === 'connected' && this.ws.readyState === WebSocket.OPEN) {
            try {
                this.ws.send(JSON.stringify(message));
                this.messageStats.sent++;
                return true;
            } catch (error) {
                console.error('Failed to send WebSocket message:', error);
                this.messageStats.errors++;
                this.queueMessage(message);
                return false;
            }
        } else {
            // Queue message for later delivery when connection is restored
            this.queueMessage(message);
            return false;
        }
    }
    
    queueMessage(message) {
        if (this.messageQueue.length >= this.maxQueueSize) {
            // Remove oldest message
            this.messageQueue.shift();
        }
        
        this.messageQueue.push({
            message,
            timestamp: Date.now()
        });
    }
    
    processMessageQueue() {
        const now = Date.now();
        const maxAge = 60000; // 1 minute
        
        // Filter out old messages and send valid ones
        const validMessages = this.messageQueue.filter(item => 
            now - item.timestamp < maxAge
        );
        
        this.messageQueue = []; // Clear queue
        
        validMessages.forEach(item => {
            this.send(item.message);
        });
    }
    
    startHeartbeat() {
        this.stopHeartbeat(); // Ensure no duplicate intervals
        
        this.lastHeartbeat = Date.now();
        this.heartbeatInterval = setInterval(() => {
            const now = Date.now();
            
            // Check if we've received any message recently
            if (now - this.lastHeartbeat > this.heartbeatTimeout) {
                console.warn('WebSocket heartbeat timeout, connection may be stale');
                this.disconnect();
                this.handleDisconnection({ code: 1006, reason: 'Heartbeat timeout' });
            } else {
                // Send ping
                this.send({ type: 'Ping', timestamp: now });
            }
        }, this.heartbeatTimeout / 2); // Check every 15 seconds
    }
    
    stopHeartbeat() {
        if (this.heartbeatInterval) {
            clearInterval(this.heartbeatInterval);
            this.heartbeatInterval = null;
        }
    }
    
    disconnect() {
        this.shouldReconnect = false;
        
        // Clear reconnection timer
        if (this.reconnectTimer) {
            clearTimeout(this.reconnectTimer);
            this.reconnectTimer = null;
        }
        
        // Stop heartbeat
        this.stopHeartbeat();
        
        // Close WebSocket
        if (this.ws) {
            this.ws.close(1000, 'Client disconnect');
            this.ws = null;
        }
        
        this.connectionState = 'disconnected';
        this.isConnecting = false;
    }
    
    // Utility methods for connection management
    isConnected() {
        return this.connectionState === 'connected' && 
               this.ws && 
               this.ws.readyState === WebSocket.OPEN;
    }
    
    getConnectionState() {
        return this.connectionState;
    }
    
    getReconnectInfo() {
        return {
            attempts: this.reconnectAttempts,
            maxAttempts: this.maxReconnectAttempts,
            isReconnecting: this.isConnecting && this.reconnectAttempts > 0,
            nextReconnectDelay: this.calculateNextDelay()
        };
    }
    
    getMessageStats() {
        return {
            ...this.messageStats,
            queueSize: this.messageQueue.length,
            connectionUptime: this.lastHeartbeat ? Date.now() - this.lastHeartbeat : 0
        };
    }
    
    // Calculate next reconnection delay with exponential backoff
    calculateNextDelay() {
        const baseDelay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts);
        return Math.min(baseDelay, 30000); // Max 30 seconds
    }
    
    // Register custom event handler for specific message type
    registerEventHandler(messageType, handler) {
        this.eventHandlers.set(messageType, handler);
    }
    
    // Unregister event handler
    unregisterEventHandler(messageType) {
        this.eventHandlers.delete(messageType);
    }
    
    // Reset reconnection attempts (useful for manual reconnect)
    resetReconnection() {
        this.reconnectAttempts = 0;
        if (this.reconnectTimer) {
            clearTimeout(this.reconnectTimer);
            this.reconnectTimer = null;
        }
    }
    
    // Force immediate reconnection
    forceReconnect() {
        this.disconnect();
        this.shouldReconnect = true;
        this.resetReconnection();
        setTimeout(() => this.establishConnection(), 100);
    }
}

// Export for use in other modules
window.StreamingWebSocketHandler = StreamingWebSocketHandler;