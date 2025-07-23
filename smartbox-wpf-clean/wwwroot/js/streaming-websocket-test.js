/**
 * StreamingWebSocketHandler Test Suite
 * Tests the enhanced WebSocket handler implementation
 */

class StreamingWebSocketTest {
    constructor() {
        this.testResults = [];
        this.mockPlayer = this.createMockPlayer();
    }
    
    createMockPlayer() {
        const events = new Map();
        
        return {
            // Event emitter functionality
            emit: (event, data) => {
                console.log(`Mock player event: ${event}`, data);
                const handlers = events.get(event) || [];
                handlers.forEach(handler => handler(data));
            },
            
            on: (event, handler) => {
                if (!events.has(event)) {
                    events.set(event, []);
                }
                events.get(event).push(handler);
            },
            
            // Mock player methods
            onNewSegment: (data) => console.log('Mock onNewSegment:', data),
            updateRecordingStatus: (data) => console.log('Mock updateRecordingStatus:', data),
            onThumbnailReady: (data) => console.log('Mock onThumbnailReady:', data),
            handleStreamError: (data) => console.log('Mock handleStreamError:', data),
            
            // Mock timeline
            timeline: {
                updateThumbnail: (data) => console.log('Mock timeline updateThumbnail:', data),
                addMarker: (data) => console.log('Mock timeline addMarker:', data)
            }
        };
    }
    
    async runAllTests() {
        console.log('🧪 Starting StreamingWebSocketHandler Tests');
        
        try {
            this.testConstructor();
            this.testEventHandlerSetup();
            this.testMessageHandling();
            this.testConnectionState();
            this.testReconnectionLogic();
            this.testMessageQueue();
            this.testCustomEventHandlers();
            this.testUtilityMethods();
            
            console.log('✅ All tests completed');
            return this.generateTestReport();
        } catch (error) {
            console.error('❌ Test suite failed:', error);
            return { success: false, error: error.message };
        }
    }
    
    testConstructor() {
        console.log('Testing constructor...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        this.assert(handler.player === this.mockPlayer, 'Player reference set correctly');
        this.assert(handler.maxReconnectAttempts === 5, 'Default max reconnect attempts');
        this.assert(handler.reconnectDelay === 1000, 'Default reconnect delay');
        this.assert(handler.connectionState === 'disconnected', 'Initial connection state');
        this.assert(handler.eventHandlers instanceof Map, 'Event handlers map created');
        this.assert(handler.eventHandlers.size > 0, 'Default event handlers registered');
        
        console.log('✅ Constructor test passed');
    }
    
    testEventHandlerSetup() {
        console.log('Testing event handler setup...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test that core Phase 1 events are registered
        const expectedEvents = [
            'SegmentCompleted',
            'RecordingStatus', 
            'ThumbnailReady',
            'MarkerAdded',
            'Error'
        ];
        
        expectedEvents.forEach(eventType => {
            this.assert(
                handler.eventHandlers.has(eventType),
                `Event handler registered for ${eventType}`
            );
        });
        
        console.log('✅ Event handler setup test passed');
    }
    
    testMessageHandling() {
        console.log('Testing message handling...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        let eventEmitted = false;
        
        // Test core event handling
        this.mockPlayer.on('segmentCompleted', (data) => {
            eventEmitted = true;
            this.assert(data.segmentNumber === 5, 'Segment data passed correctly');
        });
        
        handler.handleMessage({
            type: 'SegmentCompleted',
            data: { segmentNumber: 5, duration: 10.0 }
        });
        
        this.assert(eventEmitted, 'Event emitted for core message type');
        
        // Test unknown message handling
        let unknownEventEmitted = false;
        this.mockPlayer.on('unknownMessage', () => {
            unknownEventEmitted = true;
        });
        
        handler.handleMessage({
            type: 'UnknownMessageType',
            data: { test: 'data' }
        });
        
        this.assert(unknownEventEmitted, 'Unknown message handled correctly');
        
        console.log('✅ Message handling test passed');
    }
    
    testConnectionState() {
        console.log('Testing connection state management...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        this.assert(handler.getConnectionState() === 'disconnected', 'Initial state');
        this.assert(!handler.isConnected(), 'Initially not connected');
        
        // Simulate connection
        handler.connectionState = 'connected';
        this.assert(handler.getConnectionState() === 'connected', 'Connected state');
        
        console.log('✅ Connection state test passed');
    }
    
    testReconnectionLogic() {
        console.log('Testing reconnection logic...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test reconnection info
        const reconnectInfo = handler.getReconnectInfo();
        this.assert(reconnectInfo.attempts === 0, 'Initial attempts is 0');
        this.assert(reconnectInfo.maxAttempts === 5, 'Max attempts is 5');
        this.assert(!reconnectInfo.isReconnecting, 'Initially not reconnecting');
        
        // Test delay calculation
        handler.reconnectAttempts = 2;
        const delay = handler.calculateNextDelay();
        this.assert(delay === 4000, 'Exponential backoff calculation correct');
        
        // Test reset
        handler.resetReconnection();
        this.assert(handler.reconnectAttempts === 0, 'Reconnection attempts reset');
        
        console.log('✅ Reconnection logic test passed');
    }
    
    testMessageQueue() {
        console.log('Testing message queue...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Queue a message when disconnected
        const result = handler.send({ type: 'Test', data: 'test' });
        this.assert(!result, 'Send returns false when disconnected');
        this.assert(handler.messageQueue.length === 1, 'Message queued');
        
        // Test queue size limit
        handler.maxQueueSize = 2;
        handler.send({ type: 'Test2', data: 'test2' });
        handler.send({ type: 'Test3', data: 'test3' }); // Should remove first message
        
        this.assert(handler.messageQueue.length === 2, 'Queue size limited correctly');
        
        console.log('✅ Message queue test passed');
    }
    
    testCustomEventHandlers() {
        console.log('Testing custom event handlers...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        let customEventFired = false;
        handler.registerEventHandler('CustomEvent', (data) => {
            customEventFired = true;
            this.assert(data.custom === true, 'Custom data passed correctly');
        });
        
        this.assert(handler.eventHandlers.has('CustomEvent'), 'Custom handler registered');
        
        handler.handleMessage({
            type: 'CustomEvent',
            data: { custom: true }
        });
        
        this.assert(customEventFired, 'Custom event handler executed');
        
        // Test unregistration
        handler.unregisterEventHandler('CustomEvent');
        this.assert(!handler.eventHandlers.has('CustomEvent'), 'Custom handler unregistered');
        
        console.log('✅ Custom event handlers test passed');
    }
    
    testUtilityMethods() {
        console.log('Testing utility methods...');
        
        const handler = new StreamingWebSocketHandler(this.mockPlayer);
        
        // Test message stats
        const stats = handler.getMessageStats();
        this.assert(typeof stats.sent === 'number', 'Stats contains sent count');
        this.assert(typeof stats.received === 'number', 'Stats contains received count');
        this.assert(typeof stats.errors === 'number', 'Stats contains error count');
        this.assert(typeof stats.queueSize === 'number', 'Stats contains queue size');
        
        console.log('✅ Utility methods test passed');
    }
    
    assert(condition, message) {
        if (!condition) {
            throw new Error(`Assertion failed: ${message}`);
        }
        this.testResults.push({ test: message, passed: true });
    }
    
    generateTestReport() {
        const totalTests = this.testResults.length;
        const passedTests = this.testResults.filter(r => r.passed).length;
        
        return {
            success: passedTests === totalTests,
            totalTests,
            passedTests,
            failedTests: totalTests - passedTests,
            results: this.testResults
        };
    }
}

// Auto-run tests in development
if (typeof window !== 'undefined' && window.location.hostname === 'localhost') {
    document.addEventListener('DOMContentLoaded', async () => {
        if (window.StreamingWebSocketHandler) {
            const tester = new StreamingWebSocketTest();
            const results = await tester.runAllTests();
            
            console.log('📊 Test Results:', results);
            
            // Display results in console
            if (results.success) {
                console.log(`🎉 All ${results.totalTests} tests passed!`);
            } else {
                console.log(`⚠️ ${results.failedTests} of ${results.totalTests} tests failed`);
            }
        }
    });
}

// Export for manual testing
window.StreamingWebSocketTest = StreamingWebSocketTest;