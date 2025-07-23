using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace SmartBoxNext
{
    /// <summary>
    /// Comprehensive API health check utility
    /// </summary>
    public class ApiHealthCheck
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("SmartBox API Health Check Utility");
            Console.WriteLine("=================================\n");

            // Step 1: Check if port 5002 is in use
            Console.WriteLine("1. Checking port 5002 availability...");
            CheckPort(5002);

            // Step 2: Check for any process listening on port 5002
            Console.WriteLine("\n2. Checking for processes on port 5002...");
            CheckProcessOnPort(5002);

            // Step 3: Try to connect to the API
            Console.WriteLine("\n3. Testing API connectivity...");
            await TestApiConnectivity();

            // Step 4: Check firewall status
            Console.WriteLine("\n4. Checking Windows Firewall...");
            CheckFirewall();

            // Step 5: Test localhost variations
            Console.WriteLine("\n5. Testing different localhost addresses...");
            await TestLocalhostVariations();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void CheckPort(int port)
        {
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endpoints = ipProperties.GetActiveTcpListeners();

            bool portInUse = false;
            foreach (IPEndPoint endpoint in endpoints)
            {
                if (endpoint.Port == port)
                {
                    Console.WriteLine($"✓ Port {port} is being used by: {endpoint.Address}");
                    portInUse = true;
                }
            }

            if (!portInUse)
            {
                Console.WriteLine($"✗ Port {port} is NOT in use - API might not be running");
            }
        }

        private static void CheckProcessOnPort(int port)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string[] lines = output.Split('\n');

                    foreach (string line in lines)
                    {
                        if (line.Contains($":{port}") && line.Contains("LISTENING"))
                        {
                            Console.WriteLine($"✓ Found listening process: {line.Trim()}");
                            
                            // Try to get PID
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                string pidStr = parts[parts.Length - 1];
                                if (int.TryParse(pidStr, out int pid))
                                {
                                    try
                                    {
                                        Process p = Process.GetProcessById(pid);
                                        Console.WriteLine($"  Process: {p.ProcessName} (PID: {pid})");
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not check processes: {ex.Message}");
            }
        }

        private static async Task TestApiConnectivity()
        {
            string[] urls = {
                "http://localhost:5002/api/health",
                "http://127.0.0.1:5002/api/health",
                "http://[::1]:5002/api/health"
            };

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
            {
                foreach (string url in urls)
                {
                    try
                    {
                        Console.WriteLine($"\nTesting: {url}");
                        HttpResponseMessage response = await client.GetAsync(url);
                        Console.WriteLine($"  Status: {response.StatusCode}");
                        
                        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
                        {
                            string content = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"  Response: {content}");
                            Console.WriteLine("✓ API is responding!");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"  ✗ Connection failed: {ex.Message}");
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine($"  ✗ Connection timeout");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  ✗ Error: {ex.Message}");
                    }
                }
            }
        }

        private static void CheckFirewall()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    
                    if (output.Contains("5002"))
                    {
                        Console.WriteLine("✓ Found firewall rules mentioning port 5002");
                    }
                    else
                    {
                        Console.WriteLine("⚠ No firewall rules found for port 5002");
                        Console.WriteLine("  You may need to add an inbound rule for port 5002");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not check firewall: {ex.Message}");
                Console.WriteLine("Try running as Administrator for firewall checks");
            }
        }

        private static async Task TestLocalhostVariations()
        {
            string[] addresses = {
                "localhost",
                "127.0.0.1",
                "[::1]",
                Environment.MachineName,
                Dns.GetHostName()
            };

            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            {
                foreach (string address in addresses)
                {
                    try
                    {
                        string url = $"http://{address}:5002/api/health";
                        Console.WriteLine($"\nTesting {address}...");
                        
                        HttpResponseMessage response = await client.GetAsync(url);
                        Console.WriteLine($"  ✓ {address} works! Status: {response.StatusCode}");
                    }
                    catch
                    {
                        Console.WriteLine($"  ✗ {address} failed");
                    }
                }
            }
        }
    }
}