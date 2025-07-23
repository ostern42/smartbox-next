using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartBoxNext
{
    /// <summary>
    /// Self-testing API client to verify Kestrel API is working
    /// </summary>
    public class ApiTester
    {
        private readonly HttpClient _httpClient;
        private const string API_BASE = "http://localhost:5002/api";
        
        public ApiTester()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<bool> RunAllTestsAsync()
        {
            Console.WriteLine("🧪 Starting API Self-Test...");
            Console.WriteLine("================================");
            
            bool allPassed = true;
            
            // Test 1: Health Check
            Console.WriteLine("\n1️⃣ Testing Health Check...");
            if (await TestHealthCheckAsync())
            {
                Console.WriteLine("✅ Health check passed");
            }
            else
            {
                Console.WriteLine("❌ Health check failed");
                allPassed = false;
            }
            
            // Test 2: Login
            Console.WriteLine("\n2️⃣ Testing Login...");
            var token = await TestLoginAsync();
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine("✅ Login passed");
            }
            else
            {
                Console.WriteLine("❌ Login failed");
                allPassed = false;
            }
            
            // Test 3: Invalid Login
            Console.WriteLine("\n3️⃣ Testing Invalid Login...");
            if (await TestInvalidLoginAsync())
            {
                Console.WriteLine("✅ Invalid login properly rejected");
            }
            else
            {
                Console.WriteLine("❌ Invalid login test failed");
                allPassed = false;
            }
            
            // Test 4: Stream Start (if we have token)
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine("\n4️⃣ Testing Stream Start...");
                if (await TestStreamStartAsync(token))
                {
                    Console.WriteLine("✅ Stream start passed");
                }
                else
                {
                    Console.WriteLine("❌ Stream start failed");
                    allPassed = false;
                }
            }
            
            Console.WriteLine("\n================================");
            if (allPassed)
            {
                Console.WriteLine("🎉 ALL TESTS PASSED! API is working correctly.");
            }
            else
            {
                Console.WriteLine("⚠️ Some tests failed. Check implementation.");
            }
            
            return allPassed;
        }
        
        private async Task<bool> TestHealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{API_BASE}/health");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {content}");
                
                return response.IsSuccessStatusCode && content.Contains("healthy");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
                return false;
            }
        }
        
        private async Task<string?> TestLoginAsync()
        {
            try
            {
                var loginData = new
                {
                    username = "admin",
                    password = "SmartBox2024!"
                };
                
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{API_BASE}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (responseData.TryGetProperty("access_token", out var tokenElement))
                        {
                            return tokenElement.GetString();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"   Parse error: {ex.Message}");
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
                return null;
            }
        }
        
        private async Task<bool> TestInvalidLoginAsync()
        {
            try
            {
                var loginData = new
                {
                    username = "invalid",
                    password = "wrong"
                };
                
                var json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{API_BASE}/auth/login", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                // Should return 401 Unauthorized
                return response.StatusCode == System.Net.HttpStatusCode.Unauthorized;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> TestStreamStartAsync(string token)
        {
            try
            {
                var streamData = new
                {
                    inputType = 0,
                    deviceName = "Test Camera",
                    enableDVR = true,
                    resolution = "1280x720"
                };
                
                var json = JsonSerializer.Serialize(streamData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE}/stream/start");
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Content = content;
                
                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"   Status: {response.StatusCode}");
                Console.WriteLine($"   Response: {responseContent}");
                
                return response.IsSuccessStatusCode && responseContent.Contains("sessionId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
                return false;
            }
        }
        
        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}