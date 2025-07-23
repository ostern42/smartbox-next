/**
 * StreamingWebSocketHandler
 * Real-time streaming updates handler for Phase 1 Foundation Integration
 * Provides automatic reconnection with exponential backoff and comprehensive message handling
 */

class StreamingWebSocketHandler {
    constructor(player) {
        this.player = player;
        this.sessionId = null;
        this.url = null;
        this.ws = null;
        
        // Reconnection management
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000; // Base delay in ms
        this.reconnectTimer = null;
        this.isConnecting = false;
        this.shouldReconnect = true;
        
        // Connection state
        this.connectionState = 'disconnected'; // disconnected, connecting, connected, error
        
        // Message queue for offline messages
        this.messageQueue = [];
        this.maxQueueSize = 100;
        
        // Heartbeat
        this.heartbeatInterval = null;
        this.heartbeatTimeout = 30000; // 30 seconds
        this.lastHeartbeat = null;
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
            console.log('Streaming WebSocket connected');
            this.isConnecting = false;
            this.connectionState = 'connected';
            this.reconnectAttempts = 0;
            
            // Process queued messages
            this.processMessageQueue();
            
            // Start heartbeat
            this.startHeartbeat();
            
            // Notify player
            this.player.emit('websocketConnected');
        };
        
        this.ws.onmessage = (event) => {
            try {
                const message = JSON.parse(event.data);
                this.handleMessage(message);
                
                // Update last heartbeat time
                this.lastHeartbeat = Date.now();
            } catch (error) {
                console.error('Failed to parse WebSocket message:', error, event.data);
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
        console.log('WebSocket message received:', message.type);
        
        switch (message.type) {
            case 'SegmentCompleted':
                this.player.onNewSegment(message.data);
                break;
                
            case 'RecordingStatus':
                this.player.updateRecordingStatus(message.data);
                break;
                
            case 'ThumbnailReady':
                if (this.player.timeline) {
                    this.player.timeline.updateThumbnail(message.data);
                } else {
                    this.player.onThumbnailReady(message.data);
                }
                break;
                
            case 'MarkerAdded':
                if (this.player.timeline) {
                    this.player.timeline.addMarker(message.data);
                }
                this.player.emit('markerAdded', message.data);
                break;
                
            case 'Error':
                this.player.handleStreamError(message.data);
                break;
                
            case 'Warning':
                console.warn('Stream warning:', message.data);
                this.player.emit('streamWarning', message.data);
                break;
                
            case 'Heartbeat':
                // Respond to heartbeat
                this.send({ type: 'HeartbeatResponse', timestamp: Date.now() });
                break;
                
            case 'SessionInfo':
                // Update session information
                this.player.emit('sessionInfo', message.data);
                break;
                
            case 'BufferStatus':
                // Buffer health information
                this.player.emit('bufferStatus', message.data);
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
                return true;
            } catch (error) {
                console.error('Failed to send WebSocket message:', error);
                this.queueMessage(message);
                return false;
            }
        } else {
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
    
    // Utility methods
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
            isReconnecting: this.isConnecting && this.reconnectAttempts > 0
        };
    }
    
    // Reset reconnection attempts (useful for manual reconnect)
    resetReconnection() {
        this.reconnectAttempts = 0;
        if (this.reconnectTimer) {
            clearTimeout(this.reconnectTimer);
            this.reconnectTimer = null;
        }
    }
}

// Export for use in other modules
window.StreamingWebSocketHandler = StreamingWebSocketHandler;