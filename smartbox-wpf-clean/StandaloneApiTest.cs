using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Test
{
    /// <summary>
    /// Standalone API test that creates and tests the Kestrel server
    /// </summary>
    class StandaloneApiTest
    {
        private static IHost? _host;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string API_BASE = "http://localhost:5002/api";
        
        // Simple in-memory user store (same as KestrelStreamingApi)
        private static readonly Dictionary<string, string> _users = new()
        {
            { "admin", "SmartBox2024!" },
            { "operator", "SmartBox2024!" },
            { "viewer", "SmartBox2024!" }
        };
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Standalone API Test Starting...");
            Console.WriteLine("====================================");
            
            try
            {
                // Start the API server
                Console.WriteLine("Starting Kestrel API server...");
                await StartApiServerAsync();
                
                // Give it a moment to start
                await Task.Delay(2000);
                
                // Run tests
                await RunAllTestsAsync();
                
                // Stop the server
                Console.WriteLine("\nStopping API server...");
                await StopApiServerAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            
            Console.WriteLine("\n====================================");
            Console.WriteLine("✅ Standalone API Test Complete!");
        }
        
        static async Task StartApiServerAsync()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseKestrel(options =>
                        {
                            options.ListenLocalhost(5002);
                        })
                        .Configure(app =>
                        {
                            // Enable CORS for all origins
                            app.Use(async (context, next) =>
                            {
                                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                                
                                if (context.Request.Method == "OPTIONS")
                                {
                                    context.Response.StatusCode = 204;
                                    return;
                                }
                                
                                await next();
                            });

                            app.UseRouting();
                            
                            // Enhanced debug logging middleware
                            app.Use(async (context, next) =>
                            {
                                var request = context.Request;
                                Console.WriteLine($"=== INCOMING REQUEST ===");
                                Console.WriteLine($"Method: {request.Method}");
                                Console.WriteLine($"Path: {request.Path}");
                                Console.WriteLine($"QueryString: {request.QueryString}");
                                Console.WriteLine($"ContentType: {request.ContentType}");
                                Console.WriteLine($"ContentLength: {request.ContentLength}");
                                
                                await next();
                                
                                Console.WriteLine($"Response Status: {context.Response.StatusCode}");
                                Console.WriteLine($"=== REQUEST COMPLETED ===");
                            });

                            app.UseEndpoints(endpoints =>
                            {
                                // Health check endpoint
                                endpoints.MapGet("/api/health", async context =>
                                {
                                    Console.WriteLine("🏥 Health check endpoint reached!");
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                                    {
                                        status = "healthy",
                                        service = "SmartBox Streaming API (Standalone Test)",
                                        timestamp = DateTime.UtcNow,
                                        port = 5002
                                    }));
                                });

                                // Login endpoint
                                endpoints.MapPost("/api/auth/login", async context =>
                                {
                                    Console.WriteLine("🔑 LOGIN ENDPOINT REACHED!");
                                    Console.WriteLine($"Login request received from {context.Request.Headers["User-Agent"]}");
                                    
                                    try
                                    {
                                        using var reader = new StreamReader(context.Request.Body);
                                        var body = await reader.ReadToEndAsync();
                                        Console.WriteLine($"Login request body: {body}");
                                        
                                        var loginRequest = JsonSerializer.Deserialize<LoginRequest>(body, new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true
                                        });

                                        if (loginRequest != null && 
                                            _users.TryGetValue(loginRequest.username, out var password) &&
                                            password == loginRequest.password)
                                        {
                                            // Generate simple token
                                            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{loginRequest.username}:{DateTime.UtcNow.Ticks}"));
                                            
                                            var response = JsonSerializer.Serialize(new
                                            {
                                                access_token = token,
                                                expires_in = 28800,
                                                user = new
                                                {
                                                    username = loginRequest.username,
                                                    role = loginRequest.username == "admin" ? "Administrator" : "Operator"
                                                }
                                            });
                                            
                                            context.Response.ContentType = "application/json";
                                            context.Response.StatusCode = 200;
                                            await context.Response.WriteAsync(response);
                                            Console.WriteLine($"✅ Login successful for user: {loginRequest.username}");
                                        }
                                        else
                                        {
                                            var errorResponse = JsonSerializer.Serialize(new { error = "Invalid credentials" });
                                            context.Response.ContentType = "application/json";
                                            context.Response.StatusCode = 401;
                                            await context.Response.WriteAsync(errorResponse);
                                            Console.WriteLine($"❌ Login failed for user: {loginRequest?.username ?? "unknown"}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"💥 Login error: {ex.Message}");
                                        var errorResponse = JsonSerializer.Serialize(new { error = "Invalid request", details = ex.Message });
                                        context.Response.ContentType = "application/json";
                                        context.Response.StatusCode = 400;
                                        await context.Response.WriteAsync(errorResponse);
                                    }
                                });

                                // Simple stream start endpoint (mock)
                                endpoints.MapPost("/api/stream/start", async context =>
                                {
                                    Console.WriteLine("📹 Stream start endpoint reached!");
                                    
                                    // Check for auth header
                                    if (!context.Request.Headers.ContainsKey("Authorization"))
                                    {
                                        context.Response.StatusCode = 401;
                                        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Unauthorized" }));
                                        return;
                                    }

                                    var sessionId = Guid.NewGuid().ToString();
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                                    {
                                        sessionId = sessionId,
                                        streamUrl = $"/api/stream/{sessionId}/stream.m3u8",
                                        startTime = DateTime.UtcNow
                                    }));
                                });

                                // Catch-all for unmatched routes
                                endpoints.MapFallback(async context =>
                                {
                                    Console.WriteLine($"🚫 Fallback reached for: {context.Request.Method} {context.Request.Path}");
                                    context.Response.StatusCode = 404;
                                    context.Response.ContentType = "application/json";
                                    await context.Response.WriteAsync(JsonSerializer.Serialize(new 
                                    { 
                                        error = "Not found",
                                        path = context.Request.Path.Value,
                                        availableEndpoints = new[]
                                        {
                                            "GET /api/health",
                                            "POST /api/auth/login",
                                            "POST /api/stream/start (requires auth)"
                                        }
                                    }));
                                });
                            });
                        })
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddConsole();
                            logging.SetMinimumLevel(LogLevel.Debug);
                        });
                })
                .Build();

            await _host.StartAsync();
            Console.WriteLine("✅ Kestrel API started on port 5002");
        }
        
        static async Task StopApiServerAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }
        }
        
        static async Task RunAllTestsAsync()
        {
            Console.WriteLine("\n🧪 Running API Tests...");
            Console.WriteLine("========================");
            
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
        
        private class LoginRequest
        {
            public string username { get; set; } = string.Empty;
            public string password { get; set; } = string.Empty;
        }
    }
}