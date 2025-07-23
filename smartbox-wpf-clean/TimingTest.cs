using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Test
{
    /// <summary>
    /// Test timing issues - simulate exact WPF app startup sequence
    /// </summary>
    class TimingTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("⏱️ Testing WPF App Timing Scenario...");
            Console.WriteLine("====================================");
            
            // Create logger factory exactly like WPF app
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            KestrelStreamingApi? streamingApi = null;
            
            try
            {
                Console.WriteLine("🚀 Starting streaming API server...");
                streamingApi = new KestrelStreamingApi(loggerFactory.CreateLogger<KestrelStreamingApi>());
                await streamingApi.StartAsync();
                Console.WriteLine("✅ Streaming API started successfully on port 5002");
                
                Console.WriteLine("🧪 Testing streaming API...");
                
                // Test with NO delay (like WPF app does)
                await TestWithDelay("NO DELAY", 0);
                
                // Test with small delays
                await TestWithDelay("100ms delay", 100);
                await TestWithDelay("500ms delay", 500);
                await TestWithDelay("1s delay", 1000);
                await TestWithDelay("2s delay", 2000);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
            finally
            {
                if (streamingApi != null)
                {
                    Console.WriteLine("Stopping API...");
                    await streamingApi.StopAsync();
                    streamingApi.Dispose();
                }
            }
            
            Console.WriteLine("====================================");
            Console.WriteLine("✅ Timing Test Complete!");
        }
        
        static async Task TestWithDelay(string testName, int delayMs)
        {
            Console.WriteLine($"\n🔬 {testName}:");
            
            if (delayMs > 0)
            {
                await Task.Delay(delayMs);
            }
            
            var apiTester = new ApiTester();
            var apiWorking = await apiTester.RunAllTestsAsync();
            apiTester.Dispose();
            
            if (apiWorking)
            {
                Console.WriteLine($"   ✅ {testName} - API self-test passed");
            }
            else
            {
                Console.WriteLine($"   ❌ {testName} - API self-test failed");
            }
        }
    }
}