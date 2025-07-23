using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartBoxNext
{
    /// <summary>
    /// Simple console app to test the streaming API
    /// </summary>
    public class TestStreamingApi
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private const string API_URL = "http://localhost:5002/api";
        
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SmartBox Streaming API Tester");
            Console.WriteLine("=============================\n");
            
            // Test 1: Check if API is running
            Console.WriteLine("1. Testing API health endpoint...");
            await TestHealthEndpoint();
            
            // Test 2: Test login
            Console.WriteLine("\n2. Testing login endpoint...");
            var token = await TestLogin();
            
            if (!string.IsNullOrEmpty(token))
            {
                // Test 3: Test authenticated endpoint
                Console.WriteLine("\n3. Testing authenticated endpoint...");
                await TestAuthenticatedEndpoint(token);
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        private static async Task TestHealthEndpoint()
        {
            try
            {
                var response = await httpClient.GetAsync($"{API_URL}/health");
                Console.WriteLine($"Status: {response.StatusCode}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {content}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ERROR: Cannot connect to API - {ex.Message}");
                Console.WriteLine("Make sure the SmartBox application is running and the streaming server is started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
        
        private static async Task<string?> TestLogin()
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
                
                var response = await httpClient.PostAsync($"{API_URL}/auth/login", content);
                Console.WriteLine($"Status: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseData.TryGetProperty("access_token", out var tokenElement))
                    {
                        return tokenElement.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            
            return null;
        }
        
        private static async Task TestAuthenticatedEndpoint(string token)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{API_URL}/users");
                request.Headers.Add("Authorization", $"Bearer {token}");
                
                var response = await httpClient.SendAsync(request);
                Console.WriteLine($"Status: {response.StatusCode}");
                
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response: {content}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}