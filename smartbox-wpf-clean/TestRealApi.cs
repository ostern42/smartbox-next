using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Test
{
    /// <summary>
    /// Test the actual KestrelStreamingApi class used by the WPF app
    /// </summary>
    class TestRealApi
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string API_BASE = "http://localhost:5002/api";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Testing Real KestrelStreamingApi...");
            Console.WriteLine("=====================================");
            
            // Create logger factory like WPF app does
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            KestrelStreamingApi? api = null;
            
            try
            {
                Console.WriteLine("Starting KestrelStreamingApi...");
                api = new KestrelStreamingApi(loggerFactory.CreateLogger<KestrelStreamingApi>());
                await api.StartAsync();
                Console.WriteLine("✅ API started successfully");
                
                // Give it a moment to start
                await Task.Delay(2000);
                
                // Run tests
                await RunTestsAsync();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
            }
            finally
            {
                if (api != null)
                {
                    Console.WriteLine("Stopping API...");
                    await api.StopAsync();
                    api.Dispose();
                }
            }
            
            Console.WriteLine("=====================================");
            Console.WriteLine("✅ Real API Test Complete!");
        }
        
        static async Task RunTestsAsync()
        {
            Console.WriteLine("\n🧪 Testing Real API...");
            
            // Test 1: Health Check
            Console.WriteLine("\n1️⃣ Testing Health Check...");
            await TestHealthAsync();
            
            // Test 2: Login
            Console.WriteLine("\n2️⃣ Testing Login...");
            await TestLoginAsync();
        }
        
        static async Task TestHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{API_BASE}/health");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Content: {content}");
                
                if (response.IsSuccessStatusCode && content.Contains("healthy"))
                {
                    Console.WriteLine("   ✅ Health check PASSED");
                }
                else
                {
                    Console.WriteLine("   ❌ Health check FAILED");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Health check ERROR: {ex.Message}");
            }
        }
        
        static async Task TestLoginAsync()
        {
            try
            {
                var loginData = new { username = "admin", password = "SmartBox2024!" };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"   Request: POST {API_BASE}/auth/login");
                Console.WriteLine($"   Body: {json}");
                
                var response = await _httpClient.PostAsync($"{API_BASE}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("   ✅ Login PASSED");
                }
                else
                {
                    Console.WriteLine("   ❌ Login FAILED");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Login ERROR: {ex.Message}");
            }
        }
    }
}