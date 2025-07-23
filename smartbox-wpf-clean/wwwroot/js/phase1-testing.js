/**
 * Phase 1 Foundation Integration Testing & Validation
 * Comprehensive testing suite for all Phase 1 components
 */

class Phase1Testing {
    constructor() {
        this.testResults = new Map();
        this.isRunning = false;
        this.testStartTime = null;
        this.testCount = 0;
        this.passedTests = 0;
        this.failedTests = 0;
    }
    
    async runAllTests(player) {
        if (this.isRunning) {
            console.warn('Tests are already running');
            return this.testResults;
        }
        
        this.isRunning = true;
        this.testStartTime = Date.now();
        this.testResults.clear();
        this.testCount = 0;
        this.passedTests = 0;
        this.failedTests = 0;
        
        console.log('🧪 Starting Phase 1 Foundation Integration Tests');
        
        try {
            // Test WebSocket connection stability
            await this.testWebSocketConnection(player);
            
            // Test FFmpeg API integration
            await this.testFFmpegIntegration(player);
            
            // Test thumbnail system
            await this.testThumbnailSystem(player);
            
            // Test error recovery
            await this.testErrorRecovery(player);
            
            // Test event flow
            await this.testEventFlow(player);
            
            // Performance validation
            await this.validatePerformance(player);
            
        } catch (error) {
            console.error('Test suite error:', error);
            this.recordTest('Test Suite', false, error.message);
        } finally {
            this.isRunning = false;
            const duration = Date.now() - this.testStartTime;
            this.logTestSummary(duration);
        }
        
        return this.testResults;
    }
    
    async testWebSocketConnection(player) {
        console.log('🔌 Testing WebSocket Connection Stability');
        
        // Test 1: WebSocket Handler Creation
        await this.runTest('WebSocket Handler Creation', async () => {
            const handler = new StreamingWebSocketHandler(player);
            if (!handler) throw new Error('Failed to create handler');
            if (typeof handler.connect !== 'function') throw new Error('Missing connect method');
            return true;
        });
        
        // Test 2: Connection State Management
        await this.runTest('Connection State Management', async () => {
            const handler = new StreamingWebSocketHandler(player);
            
            // Initial state should be disconnected
            if (handler.getConnectionState() !== 'disconnected') {
                throw new Error('Initial state should be disconnected');
            }
            
            return true;
        });
        
        // Test 3: Message Queue Functionality
        await this.runTest('Message Queue Functionality', async () => {
            const handler = new StreamingWebSocketHandler(player);
            
            // Test queueing when disconnected
            const result = handler.send({ type: 'test', data: 'queue_test' });
            if (result !== false) throw new Error('Should return false when disconnected');
            
            // Check if message was queued
            if (handler.messageQueue.length === 0) {
                throw new Error('Message should be queued');
            }
            
            return true;
        });
        
        // Test 4: Reconnection Logic
        await this.runTest('Reconnection Logic', async () => {
            const handler = new StreamingWebSocketHandler(player);
            
            // Test reconnection attempt counting
            handler.reconnectAttempts = 0;
            handler.maxReconnectAttempts = 3;
            
            // Simulate disconnection
            const shouldReconnect = handler.shouldAttemptReconnection({ code: 1006 });
            if (!shouldReconnect) throw new Error('Should attempt reconnection on code 1006');
            
            const shouldNotReconnect = handler.shouldAttemptReconnection({ code: 1000 });
            if (shouldNotReconnect) throw new Error('Should not reconnect on normal closure');
            
            return true;
        });
    }
    
    async testFFmpegIntegration(player) {
        console.log('🎬 Testing FFmpeg API Integration');
        
        // Test 1: VideoEngineClient Creation
        await this.runTest('VideoEngineClient Creation', async () => {
            const client = new VideoEngineClient();
            if (!client) throw new Error('Failed to create VideoEngineClient');
            if (typeof client.startRecording !== 'function') throw new Error('Missing startRecording method');
            return true;
        });
        
        // Test 2: API Endpoint Configuration
        await this.runTest('API Endpoint Configuration', async () => {
            const client = new VideoEngineClient();
            if (!client.baseUrl) throw new Error('Missing baseUrl configuration');
            if (!client.baseUrl.startsWith('/api/video')) throw new Error('Invalid baseUrl');
            return true;
        });
        
        // Test 3: Event System
        await this.runTest('Event System', async () => {
            const client = new VideoEngineClient();
            let eventFired = false;
            
            client.on('test', () => { eventFired = true; });
            client.emit('test');
            
            if (!eventFired) throw new Error('Event system not working');
            return true;
        });
        
        // Test 4: Session Management
        await this.runTest('Session Management', async () => {
            const client = new VideoEngineClient();
            
            if (client.isRecording()) throw new Error('Should not be recording initially');
            if (client.getSessionId()) throw new Error('Should not have session ID initially');
            
            return true;
        });
    }
    
    async testThumbnailSystem(player) {
        console.log('🖼️ Testing Unified Thumbnail System');
        
        // Test 1: Thumbnail Cache Management
        await this.runTest('Thumbnail Cache Management', async () => {
            if (!player.thumbnailCache) throw new Error('Missing thumbnail cache');
            
            // Test cache operations
            const testUrl = 'data:image/png;base64,test';
            player.thumbnailCache.set('test_123', testUrl);
            
            if (!player.thumbnailCache.has('test_123')) throw new Error('Cache set failed');
            if (player.thumbnailCache.get('test_123') !== testUrl) throw new Error('Cache get failed');
            
            return true;
        });
        
        // Test 2: Fallback Thumbnail Generation
        await this.runTest('Fallback Thumbnail Generation', async () => {
            const fallbackUrl = player.getFallbackThumbnail(123);
            
            if (!fallbackUrl) throw new Error('Should return fallback URL');
            if (!fallbackUrl.startsWith('data:image/svg+xml')) throw new Error('Invalid fallback format');
            
            return true;
        });
        
        // Test 3: Cache Size Management
        await this.runTest('Cache Size Management', async () => {
            const initialSize = player.getThumbnailCacheSize();
            
            // Add test entries
            for (let i = 0; i < 5; i++) {
                player.thumbnailCache.set(`test_${i}`, `url_${i}`);
            }
            
            const newSize = player.getThumbnailCacheSize();
            if (newSize !== initialSize + 5) throw new Error('Cache size tracking failed');
            
            return true;
        });
        
        // Test 4: Cache Cleanup
        await this.runTest('Cache Cleanup', async () => {
            // Add test entries
            player.thumbnailCache.set('cleanup_test', 'test_url');
            
            player.clearThumbnailCache();
            
            if (player.getThumbnailCacheSize() !== 0) throw new Error('Cache cleanup failed');
            
            return true;
        });
    }
    
    async testErrorRecovery(player) {
        console.log('🛡️ Testing Error Recovery System');
        
        // Test 1: Error Recovery Initialization
        await this.runTest('Error Recovery Initialization', async () => {
            const recovery = new Phase1ErrorRecovery(player);
            
            if (!recovery) throw new Error('Failed to create error recovery');
            if (!recovery.recoveryStrategies) throw new Error('Missing recovery strategies');
            
            return true;
        });
        
        // Test 2: Error Categorization
        await this.runTest('Error Categorization', async () => {
            const recovery = new Phase1ErrorRecovery(player);
            
            const wsError = new Error('WebSocket connection failed');
            const category = recovery.categorizeError(wsError);
            
            if (category !== 'WEBSOCKET_CONNECTION_FAILED') {
                throw new Error('Incorrect error categorization');
            }
            
            return true;
        });
        
        // Test 3: Error Logging
        await this.runTest('Error Logging', async () => {
            const recovery = new Phase1ErrorRecovery(player);
            const initialCount = recovery.errorHistory.length;
            
            recovery.logError(new Error('Test error'), { test: true });
            
            if (recovery.errorHistory.length !== initialCount + 1) {
                throw new Error('Error logging failed');
            }
            
            return true;
        });
        
        // Test 4: Recovery Strategy Selection
        await this.runTest('Recovery Strategy Selection', async () => {
            const recovery = new Phase1ErrorRecovery(player);
            
            const strategies = recovery.recoveryStrategies.get('WEBSOCKET_CONNECTION_FAILED');
            if (!strategies || strategies.length === 0) {
                throw new Error('No recovery strategies found');
            }
            
            return true;
        });
    }
    
    async testEventFlow(player) {
        console.log('📡 Testing Event Flow');
        
        // Test 1: Event Emitter Functionality
        await this.runTest('Event Emitter Functionality', async () => {
            let eventReceived = false;
            const testData = { test: 'data' };
            
            player.on('testEvent', (data) => {
                eventReceived = true;
                if (data !== testData) throw new Error('Event data mismatch');
            });
            
            player.emit('testEvent', testData);
            
            if (!eventReceived) throw new Error('Event not received');
            
            return true;
        });
        
        // Test 2: Multiple Event Listeners
        await this.runTest('Multiple Event Listeners', async () => {
            let count = 0;
            
            const listener1 = () => count++;
            const listener2 = () => count++;
            
            player.on('multiTest', listener1);
            player.on('multiTest', listener2);
            
            player.emit('multiTest');
            
            if (count !== 2) throw new Error('Multiple listeners failed');
            
            // Cleanup
            player.off('multiTest', listener1);
            player.off('multiTest', listener2);
            
            return true;
        });
        
        // Test 3: Event Listener Removal
        await this.runTest('Event Listener Removal', async () => {
            let eventReceived = false;
            
            const listener = () => { eventReceived = true; };
            
            player.on('removeTest', listener);
            player.off('removeTest', listener);
            player.emit('removeTest');
            
            if (eventReceived) throw new Error('Event listener not removed');
            
            return true;
        });
    }
    
    async validatePerformance(player) {
        console.log('⚡ Validating Performance');
        
        // Test 1: Thumbnail Loading Performance
        await this.runTest('Thumbnail Loading Performance', async () => {
            const startTime = performance.now();
            
            // Load multiple fallback thumbnails
            const promises = [];
            for (let i = 0; i < 10; i++) {
                promises.push(Promise.resolve(player.getFallbackThumbnail(i)));
            }
            
            await Promise.all(promises);
            
            const duration = performance.now() - startTime;
            
            if (duration > 100) { // 100ms threshold
                throw new Error(`Thumbnail loading too slow: ${duration}ms`);
            }
            
            return true;
        });
        
        // Test 2: Event System Performance
        await this.runTest('Event System Performance', async () => {
            const startTime = performance.now();
            
            // Emit many events
            for (let i = 0; i < 1000; i++) {
                player.emit('perfTest', { data: i });
            }
            
            const duration = performance.now() - startTime;
            
            if (duration > 50) { // 50ms threshold
                throw new Error(`Event system too slow: ${duration}ms`);
            }
            
            return true;
        });
        
        // Test 3: Memory Usage
        await this.runTest('Memory Usage Validation', async () => {
            if (performance.memory) {
                const initialMemory = performance.memory.usedJSHeapSize;
                
                // Create and destroy many objects
                const objects = [];
                for (let i = 0; i < 1000; i++) {
                    objects.push({ test: 'data', index: i });
                }
                objects.length = 0; // Clear array
                
                // Force garbage collection if available
                if (window.gc) {
                    window.gc();
                }
                
                const finalMemory = performance.memory.usedJSHeapSize;
                const increase = finalMemory - initialMemory;
                
                // Allow up to 1MB increase
                if (increase > 1024 * 1024) {
                    throw new Error(`Memory increase too high: ${increase} bytes`);
                }
            }
            
            return true;
        });
    }
    
    async runTest(testName, testFunction) {
        this.testCount++;
        
        try {
            const startTime = performance.now();
            const result = await testFunction();
            const duration = performance.now() - startTime;
            
            this.recordTest(testName, true, null, duration);
            this.passedTests++;
            console.log(`✅ ${testName} - ${duration.toFixed(2)}ms`);
            
            return result;
        } catch (error) {
            this.recordTest(testName, false, error.message);
            this.failedTests++;
            console.error(`❌ ${testName} - ${error.message}`);
            
            return false;
        }
    }
    
    recordTest(testName, passed, error, duration = 0) {
        this.testResults.set(testName, {
            passed,
            error,
            duration,
            timestamp: Date.now()
        });
    }
    
    logTestSummary(totalDuration) {
        console.log('\n📊 Phase 1 Test Summary');
        console.log('========================');
        console.log(`Total Tests: ${this.testCount}`);
        console.log(`Passed: ${this.passedTests} ✅`);
        console.log(`Failed: ${this.failedTests} ❌`);
        console.log(`Success Rate: ${((this.passedTests / this.testCount) * 100).toFixed(1)}%`);
        console.log(`Total Duration: ${totalDuration.toFixed(2)}ms`);
        
        // Log failed tests
        if (this.failedTests > 0) {
            console.log('\n❌ Failed Tests:');
            for (const [testName, result] of this.testResults.entries()) {
                if (!result.passed) {
                    console.log(`  • ${testName}: ${result.error}`);
                }
            }
        }
        
        // Performance summary
        const performanceTests = Array.from(this.testResults.entries())
            .filter(([name]) => name.includes('Performance') || name.includes('performance'));
        
        if (performanceTests.length > 0) {
            console.log('\n⚡ Performance Summary:');
            performanceTests.forEach(([name, result]) => {
                if (result.passed) {
                    console.log(`  • ${name}: ${result.duration.toFixed(2)}ms`);
                }
            });
        }
    }
    
    getTestReport() {
        return {
            summary: {
                totalTests: this.testCount,
                passed: this.passedTests,
                failed: this.failedTests,
                successRate: (this.passedTests / this.testCount) * 100,
                duration: this.testStartTime ? Date.now() - this.testStartTime : 0
            },
            results: Object.fromEntries(this.testResults),
            timestamp: Date.now()
        };
    }
}

// Export for use in other modules
window.Phase1Testing = Phase1Testing;

// Auto-run tests if in development mode
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    window.addEventListener('load', () => {
        setTimeout(() => {
            if (window.StreamingPlayer && typeof window.StreamingPlayer === 'function') {
                console.log('🧪 Auto-running Phase 1 tests in development mode');
                const testing = new Phase1Testing();
                const player = new StreamingPlayer();
                testing.runAllTests(player);
            }
        }, 2000);
    });
}