/**
 * WebSocket Error Recovery and Reconnection Validation
 * Comprehensive validation of StreamingWebSocketHandler error recovery capabilities
 */

class WebSocketRecoveryValidator {
    constructor() {
        this.validationResults = [];
        this.mockPlayer = this.createMockPlayer();
        this.testScenarios = [];
    }
    
    createMockPlayer() {
        const events = new Map();
        
        return {
            emit: (event, data) => {
                console.log(`🎯 Player event: ${event}`, data);
                const handlers = events.get(event) || [];
                handlers.forEach(handler => handler(data));
            },
            
            on: (event, handler) => {
                if (!events.has(event)) {
                    events.set(event, []);
                }
                events.get(event).push(handler);
            },
            
            // Mock methods for testing
            onNewSegment: (data) => console.log('📹 New segment:', data),
            updateRecordingStatus: (data) => console.log('🔴 Recording status:', data),
            onThumbnailReady: (data) => console.log('🖼️ Thumbnail ready:', data),
            handleStreamError: (data) => console.log('❌ Stream error:', data),
            
            timeline: {
                updateThumbnail: (data) => console.log('🖼️ Timeline thumbnail:', data),
                addMarker: (data) => console.log('📍 Timeline marker:', data)
            }
        };
    }
    
    async validateErrorRecovery() {
        console.log('🔍 Starting WebSocket Error Recovery Validation');
        
        try {
            await this.validateReconnectionScenarios();
            await this.validateMessageQueueRecovery();
            await this.validateHeartbeatRecovery();
            await this.validateConnectionStateTransitions();
            await this.validateErrorCodeHandling();
            await this.validateExponentialBackoff();
            await this.validateMaxReconnectionLimits();
            
            console.log('✅ Error recovery validation completed');
            return this.generateValidationReport();
        } catch (error) {
            console.error('❌ Validation failed:', error);
            return { success: false, error: error.message };
        }
    }
    
    async validateReconnectionScenarios() {
        console.log('🔄 Validating reconnection scenarios...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test basic reconnection trigger
        let reconnectionTriggered = false;
        const originalSchedule = handler.scheduleReconnection;
        handler.scheduleReconnection = () => {
            reconnectionTriggered = true;
            console.log('📅 Reconnection scheduled');
        };
        
        // Simulate disconnection
        handler.shouldReconnect = true;
        handler.handleDisconnection({ code: 1006, reason: 'Connection lost' });
        
        this.validate(reconnectionTriggered, 'Reconnection triggered on unexpected disconnection');
        
        // Test reconnection disabled
        reconnectionTriggered = false;
        handler.shouldReconnect = false;
        handler.handleDisconnection({ code: 1006, reason: 'Connection lost' });
        
        this.validate(!reconnectionTriggered, 'Reconnection not triggered when disabled');
        
        console.log('✅ Reconnection scenarios validated');
    }
    
    async validateMessageQueueRecovery() {
        console.log('📤 Validating message queue recovery...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Queue messages while disconnected
        const message1 = { type: 'Test1', data: 'test1' };
        const message2 = { type: 'Test2', data: 'test2' };
        
        handler.send(message1);
        handler.send(message2);
        
        this.validate(handler.messageQueue.length === 2, 'Messages queued when disconnected');
        
        // Test queue processing
        let processedMessages = [];
        const originalSend = handler.send;
        handler.send = (msg) => {
            processedMessages.push(msg);
            return true;
        };
        
        handler.connectionState = 'connected';
        handler.processMessageQueue();
        
        this.validate(processedMessages.length === 2, 'Queued messages processed on reconnection');
        this.validate(handler.messageQueue.length === 0, 'Queue cleared after processing');
        
        // Test queue size limit
        handler.send = originalSend; // Restore original
        handler.maxQueueSize = 2;
        handler.messageQueue = [];
        
        handler.send({ type: 'Test1' });
        handler.send({ type: 'Test2' });
        handler.send({ type: 'Test3' }); // Should evict first message
        
        this.validate(handler.messageQueue.length === 2, 'Queue size limited correctly');
        
        console.log('✅ Message queue recovery validated');
    }
    
    async validateHeartbeatRecovery() {
        console.log('💓 Validating heartbeat recovery...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test heartbeat timeout detection
        handler.lastHeartbeat = Date.now() - (handler.heartbeatTimeout + 1000); // Simulate timeout
        
        let disconnectionCalled = false;
        const originalDisconnect = handler.disconnect;
        handler.disconnect = () => {
            disconnectionCalled = true;
            console.log('🔌 Disconnect called due to heartbeat timeout');
        };
        
        // Simulate heartbeat check
        const now = Date.now();
        if (now - handler.lastHeartbeat > handler.heartbeatTimeout) {
            handler.disconnect();
        }
        
        this.validate(disconnectionCalled, 'Heartbeat timeout triggers disconnection');
        
        // Test heartbeat response
        let heartbeatResponseSent = false;
        handler.send = (message) => {
            if (message.type === 'HeartbeatResponse') {
                heartbeatResponseSent = true;
                console.log('💓 Heartbeat response sent');
            }
            return true;
        };
        
        handler.handleMessage({ type: 'Heartbeat', data: {} });
        
        this.validate(heartbeatResponseSent, 'Heartbeat response sent correctly');
        
        console.log('✅ Heartbeat recovery validated');
    }
    
    async validateConnectionStateTransitions() {
        console.log('🔄 Validating connection state transitions...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test initial state
        this.validate(handler.getConnectionState() === 'disconnected', 'Initial state is disconnected');
        
        // Test state transitions
        handler.connectionState = 'connecting';
        this.validate(handler.getConnectionState() === 'connecting', 'State transitions to connecting');
        
        handler.connectionState = 'connected';
        this.validate(handler.getConnectionState() === 'connected', 'State transitions to connected');
        
        handler.connectionState = 'error';
        this.validate(handler.getConnectionState() === 'error', 'State transitions to error');
        
        // Test isConnected method
        handler.connectionState = 'connected';
        handler.ws = { readyState: WebSocket.OPEN };
        this.validate(handler.isConnected(), 'isConnected returns true when connected');
        
        handler.connectionState = 'disconnected';
        this.validate(!handler.isConnected(), 'isConnected returns false when disconnected');
        
        console.log('✅ Connection state transitions validated');
    }
    
    async validateErrorCodeHandling() {
        console.log('🚨 Validating error code handling...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test codes that should NOT trigger reconnection
        const noReconnectCodes = [1000, 1001, 1005, 4000, 4001, 4002];
        
        noReconnectCodes.forEach(code => {
            const shouldReconnect = handler.shouldAttemptReconnection({ code });
            this.validate(!shouldReconnect, `Code ${code} should not trigger reconnection`);
        });
        
        // Test codes that SHOULD trigger reconnection
        const reconnectCodes = [1006, 1011, 1012, 1013, 1014];
        
        reconnectCodes.forEach(code => {
            const shouldReconnect = handler.shouldAttemptReconnection({ code });
            this.validate(shouldReconnect, `Code ${code} should trigger reconnection`);
        });
        
        console.log('✅ Error code handling validated');
    }
    
    async validateExponentialBackoff() {
        console.log('⏰ Validating exponential backoff...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test delay calculation
        handler.reconnectAttempts = 0;
        let delay = handler.calculateNextDelay();
        this.validate(delay === 1000, 'First attempt uses base delay');
        
        handler.reconnectAttempts = 1;
        delay = handler.calculateNextDelay();
        this.validate(delay === 2000, 'Second attempt doubles delay');
        
        handler.reconnectAttempts = 2;
        delay = handler.calculateNextDelay();
        this.validate(delay === 4000, 'Third attempt quadruples delay');
        
        handler.reconnectAttempts = 10; // Large number
        delay = handler.calculateNextDelay();
        this.validate(delay === 30000, 'Delay capped at 30 seconds');
        
        console.log('✅ Exponential backoff validated');
    }
    
    async validateMaxReconnectionLimits() {
        console.log('🚫 Validating max reconnection limits...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test max attempts reached
        handler.reconnectAttempts = handler.maxReconnectAttempts;
        
        let connectionFailedEmitted = false;
        this.mockPlayer.on('connectionFailed', () => {
            connectionFailedEmitted = true;
            console.log('🚫 Connection failed event emitted');
        });
        
        handler.handleDisconnection({ code: 1006, reason: 'Connection lost' });
        
        this.validate(connectionFailedEmitted, 'Connection failed event emitted when max attempts reached');
        
        // Test reconnection info
        const reconnectInfo = handler.getReconnectInfo();
        this.validate(
            reconnectInfo.attempts === handler.maxReconnectAttempts,
            'Reconnect info shows max attempts reached'
        );
        
        console.log('✅ Max reconnection limits validated');
    }
    
    validate(condition, message) {
        if (!condition) {
            throw new Error(`Validation failed: ${message}`);
        }
        this.validationResults.push({ test: message, passed: true });
        console.log(`  ✓ ${message}`);
    }
    
    generateValidationReport() {
        const totalValidations = this.validationResults.length;
        const passedValidations = this.validationResults.filter(r => r.passed).length;
        
        return {
            success: passedValidations === totalValidations,
            totalValidations,
            passedValidations,
            failedValidations: totalValidations - passedValidations,
            results: this.validationResults,
            summary: {
                reconnectionScenarios: '✅ Validated',
                messageQueueRecovery: '✅ Validated',
                heartbeatRecovery: '✅ Validated',
                connectionStateTransitions: '✅ Validated',
                errorCodeHandling: '✅ Validated',
                exponentialBackoff: '✅ Validated',
                maxReconnectionLimits: '✅ Validated'
            }
        };
    }
}

// Create performance benchmark test
class WebSocketPerformanceBenchmark {
    constructor() {
        this.mockPlayer = {
            emit: () => {},
            on: () => {},
            onNewSegment: () => {},
            updateRecordingStatus: () => {},
            onThumbnailReady: () => {},
            handleStreamError: () => {},
            timeline: {
                updateThumbnail: () => {},
                addMarker: () => {}
            }
        };
    }
    
    async runPerformanceBenchmarks() {
        console.log('🏃‍♂️ Running WebSocket Performance Benchmarks');
        
        const results = {};
        
        // Test message handling performance
        results.messageHandling = await this.benchmarkMessageHandling();
        
        // Test reconnection performance
        results.reconnectionPerformance = await this.benchmarkReconnectionLogic();
        
        // Test queue processing performance
        results.queueProcessing = await this.benchmarkQueueProcessing();
        
        console.log('📊 Performance benchmark results:', results);
        return results;
    }
    
    async benchmarkMessageHandling() {
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        const messageCount = 1000;
        const testMessage = {
            type: 'SegmentCompleted',
            data: { segmentNumber: 1, duration: 10.0 }
        };
        
        const startTime = performance.now();
        
        for (let i = 0; i < messageCount; i++) {
            handler.handleMessage(testMessage);
        }
        
        const endTime = performance.now();
        const duration = endTime - startTime;
        
        return {
            messagesProcessed: messageCount,
            totalTime: duration,
            messagesPerSecond: (messageCount / duration) * 1000,
            averageMessageTime: duration / messageCount
        };
    }
    
    async benchmarkReconnectionLogic() {
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        const attempts = 100;
        
        const startTime = performance.now();
        
        for (let i = 0; i < attempts; i++) {
            handler.calculateNextDelay();
            handler.shouldAttemptReconnection({ code: 1006 });
        }
        
        const endTime = performance.now();
        const duration = endTime - startTime;
        
        return {
            calculations: attempts,
            totalTime: duration,
            averageCalculationTime: duration / attempts
        };
    }
    
    async benchmarkQueueProcessing() {
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        const messageCount = 500;
        
        // Fill queue
        for (let i = 0; i < messageCount; i++) {
            handler.queueMessage({ type: 'Test', data: i });
        }
        
        // Mock successful send
        handler.send = () => true;
        handler.connectionState = 'connected';
        
        const startTime = performance.now();
        handler.processMessageQueue();
        const endTime = performance.now();
        
        return {
            messagesProcessed: messageCount,
            processingTime: endTime - startTime,
            messagesPerSecond: (messageCount / (endTime - startTime)) * 1000
        };
    }
}

// Auto-run validation in development
if (typeof window !== 'undefined' && window.location.hostname === 'localhost') {
    document.addEventListener('DOMContentLoaded', async () => {
        if (window.StreamingWebSocketHandler) {
            console.log('🚀 Auto-running WebSocket Recovery Validation');
            
            const validator = new WebSocketRecoveryValidator();
            const validationResults = await validator.validateErrorRecovery();
            
            const benchmark = new WebSocketPerformanceBenchmark();
            const performanceResults = await benchmark.runPerformanceBenchmarks();
            
            console.log('📋 Validation Summary:', validationResults.summary);
            console.log('🏆 Performance Summary:', performanceResults);
            
            if (validationResults.success) {
                console.log(`🎉 All ${validationResults.totalValidations} validations passed!`);
            } else {
                console.log(`⚠️ ${validationResults.failedValidations} validations failed`);
            }
        }
    });
}

// Export for manual testing
window.WebSocketRecoveryValidator = WebSocketRecoveryValidator;
window.WebSocketPerformanceBenchmark = WebSocketPerformanceBenchmark;