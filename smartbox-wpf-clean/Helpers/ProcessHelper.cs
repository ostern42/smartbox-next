using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SmartBoxNext.Helpers
{
    /// <summary>
    /// Helper class for managing WebView2 and child processes cleanup
    /// </summary>
    public static class ProcessHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        private enum ProcessAccessFlags : uint
        {
            Terminate = 0x00000001,
            QueryInformation = 0x00000400
        }

        /// <summary>
        /// Get all child processes of a parent process
        /// </summary>
        public static List<int> GetChildProcesses(int parentPid)
        {
            var childPids = new List<int>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ProcessId FROM Win32_Process WHERE ParentProcessId = {parentPid}"))
                {
                    foreach (ManagementObject process in searcher.Get())
                    {
                        var childPid = Convert.ToInt32(process["ProcessId"]);
                        childPids.Add(childPid);
                        
                        // Recursively get children of children
                        childPids.AddRange(GetChildProcesses(childPid));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting child processes: {ex.Message}");
            }

            return childPids.Distinct().ToList();
        }

        /// <summary>
        /// Kill a process tree starting from the root process
        /// </summary>
        public static async Task KillProcessTreeAsync(int rootPid, int delayMs = 100)
        {
            var childPids = GetChildProcesses(rootPid);
            
            // Kill children first (bottom-up)
            foreach (var childPid in childPids)
            {
                await KillProcessByIdAsync(childPid);
                if (delayMs > 0) await Task.Delay(delayMs);
            }

            // Finally kill the root
            await KillProcessByIdAsync(rootPid);
        }

        /// <summary>
        /// Kill a single process by ID
        /// </summary>
        private static async Task KillProcessByIdAsync(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                if (!process.HasExited)
                {
                    // Try graceful shutdown first
                    process.CloseMainWindow();
                    
                    if (!process.WaitForExit(1000))
                    {
                        // Force kill if graceful didn't work
                        ForceKillProcess(process);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Process already exited
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error killing process {pid}: {ex.Message}");
            }
        }

        /// <summary>
        /// Force kill a process using Win32 API
        /// </summary>
        private static void ForceKillProcess(Process process)
        {
            var handle = OpenProcess(ProcessAccessFlags.Terminate, false, process.Id);
            if (handle != IntPtr.Zero)
            {
                try
                {
                    TerminateProcess(handle, 1);
                }
                finally
                {
                    CloseHandle(handle);
                }
            }
            else
            {
                // Fallback to managed kill
                process.Kill();
            }
        }

        /// <summary>
        /// Find all WebView2 processes spawned by this application
        /// </summary>
        public static List<Process> FindWebView2Processes()
        {
            var webView2Processes = new List<Process>();
            var currentProcess = Process.GetCurrentProcess();
            
            try
            {
                // Get all child processes
                var childPids = GetChildProcesses(currentProcess.Id);
                
                foreach (var pid in childPids)
                {
                    try
                    {
                        var process = Process.GetProcessById(pid);
                        if (process.ProcessName.Contains("msedgewebview2", StringComparison.OrdinalIgnoreCase) ||
                            process.ProcessName.Contains("WebView2", StringComparison.OrdinalIgnoreCase))
                        {
                            webView2Processes.Add(process);
                        }
                    }
                    catch
                    {
                        // Process might have exited
                    }
                }

                // Also check for orphaned WebView2 processes by command line
                var allWebView2 = Process.GetProcessesByName("msedgewebview2");
                foreach (var process in allWebView2)
                {
                    try
                    {
                        var cmdLine = GetProcessCommandLine(process.Id);
                        if (cmdLine != null && cmdLine.Contains($"--webview-exe-name={currentProcess.ProcessName}"))
                        {
                            webView2Processes.Add(process);
                        }
                    }
                    catch
                    {
                        // Skip if we can't access process info
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error finding WebView2 processes: {ex.Message}");
            }

            return webView2Processes.DistinctBy(p => p.Id).ToList();
        }

        /// <summary>
        /// Get command line of a process
        /// </summary>
        private static string? GetProcessCommandLine(int processId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    var result = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    return result?["CommandLine"]?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Force kill all WebView2 processes
        /// </summary>
        public static async Task ForceKillWebView2Async(int maxWaitMs = 5000)
        {
            var webView2Processes = FindWebView2Processes();
            Debug.WriteLine($"Found {webView2Processes.Count} WebView2 processes to kill");

            if (!webView2Processes.Any())
                return;

            var killTasks = new List<Task>();
            
            foreach (var process in webView2Processes)
            {
                killTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        Debug.WriteLine($"Killing WebView2 process: {process.Id} ({process.ProcessName})");
                        
                        // Skip graceful shutdown - just kill immediately
                        // WebView2 processes don't have main windows anyway
                        if (!process.HasExited)
                        {
                            process.Kill(true); // Kill entire process tree
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error killing WebView2 process {process.Id}: {ex.Message}");
                    }
                }));
            }

            // Wait for all kills to complete with timeout
            if (killTasks.Any())
            {
                var allKillsTask = Task.WhenAll(killTasks);
                var timeoutTask = Task.Delay(maxWaitMs);
                
                await Task.WhenAny(allKillsTask, timeoutTask);
                
                // Small delay for Windows to release handles
                await Task.Delay(200);
            }
        }

        /// <summary>
        /// Clean up all WebView2 user data
        /// </summary>
        public static void CleanupWebView2UserData()
        {
            try
            {
                var userDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "SmartBoxNext",
                    "WebView2"
                );

                if (Directory.Exists(userDataPath))
                {
                    // Kill any processes that might be using these files
                    var lockingProcesses = GetProcessesUsingPath(userDataPath);
                    foreach (var proc in lockingProcesses)
                    {
                        try
                        {
                            proc.Kill();
                            proc.WaitForExit(1000);
                        }
                        catch { }
                    }

                    // Try to delete the folder
                    try
                    {
                        Directory.Delete(userDataPath, true);
                        Debug.WriteLine($"Deleted WebView2 user data folder: {userDataPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Could not delete WebView2 user data: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning WebView2 user data: {ex.Message}");
            }
        }

        /// <summary>
        /// Get processes that have handles to files in a path (simplified version)
        /// </summary>
        private static List<Process> GetProcessesUsingPath(string path)
        {
            var processes = new List<Process>();
            
            // This is a simplified version - for full implementation you'd use
            // handle.exe or Windows Restart Manager API
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.ProcessName.Contains("msedgewebview2", StringComparison.OrdinalIgnoreCase))
                    {
                        processes.Add(process);
                    }
                }
                catch { }
            }
            
            return processes;
        }
    }
}