using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartBoxNext.Test
{
    /// <summary>
    /// Simple test to verify API routing is working
    /// </summary>
    class ApiRouteTest
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string API_BASE = "http://localhost:5002/api";
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("🧪 API Route Test Starting...");
            Console.WriteLine("================================");
            
            // Test 1: Health Check
            Console.WriteLine("\n1️⃣ Testing Health Check...");
            await TestHealthAsync();
            
            // Test 2: Login - Valid
            Console.WriteLine("\n2️⃣ Testing Valid Login...");
            await TestLoginAsync("admin", "SmartBox2024!");
            
            // Test 3: Login - Invalid  
            Console.WriteLine("\n3️⃣ Testing Invalid Login...");
            await TestLoginAsync("invalid", "wrong");
            
            // Test 4: Non-existent endpoint
            Console.WriteLine("\n4️⃣ Testing Non-existent Endpoint...");
            await TestNonExistentAsync();
            
            Console.WriteLine("\n================================");
            Console.WriteLine("✅ API Route Test Complete!");
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
        
        static async Task TestLoginAsync(string username, string password)
        {
            try
            {
                var loginData = new { username, password };
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                Console.WriteLine($"   Request: POST {API_BASE}/auth/login");
                Console.WriteLine($"   Body: {json}");
                
                var response = await _httpClient.PostAsync($"{API_BASE}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                if (username == "admin" && response.IsSuccessStatusCode)
                {
                    Console.WriteLine("   ✅ Valid login PASSED");
                }
                else if (username == "invalid" && response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine("   ✅ Invalid login correctly rejected");
                }
                else
                {
                    Console.WriteLine($"   ❌ Login test FAILED - unexpected result");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Login ERROR: {ex.Message}");
            }
        }
        
        static async Task TestNonExistentAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{API_BASE}/nonexistent");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Content: {content}");
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("   ✅ Non-existent endpoint correctly returns 404");
                }
                else
                {
                    Console.WriteLine("   ❌ Non-existent endpoint test FAILED");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Non-existent endpoint ERROR: {ex.Message}");
            }
        }
    }
}