using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace SmartBoxNext
{
    public class Logger : IDisposable
    {
        private static Logger? _instance;
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
        private readonly Timer _flushTimer;
        private bool _disposed;

        public static Logger Instance => _instance ??= new Logger();

        private Logger()
        {
            // Create logs directory in app folder
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);

            // Create daily log file
            var logFileName = $"smartbox_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(_logDirectory, logFileName);

            // Start flush timer (flush every second)
            _flushTimer = new Timer(async _ => await FlushLogsAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

            // Log startup
            LogInfo("SmartBox Next started");
            LogInfo($"Log file: {_logFilePath}");
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARN", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        public void LogDebug(string message)
        {
            Log("DEBUG", message);
        }

        private void Log(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] [{level}] {message}";
            
            // Add to queue
            _logQueue.Enqueue(logEntry);

            // Also write to console in debug mode
#if DEBUG
            Console.WriteLine(logEntry);
#endif
        }

        private async Task FlushLogsAsync()
        {
            if (_disposed || _logQueue.IsEmpty)
                return;

            await _writeSemaphore.WaitAsync();
            try
            {
                var entries = new List<string>();
                while (_logQueue.TryDequeue(out var entry))
                {
                    entries.Add(entry);
                }

                if (entries.Count > 0)
                {
                    await File.AppendAllLinesAsync(_logFilePath, entries);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write logs: {ex.Message}");
            }
            finally
            {
                _writeSemaphore.Release();
            }
        }

        public async Task<string[]> GetRecentLogsAsync(int lines = 100)
        {
            await FlushLogsAsync(); // Ensure all logs are written

            if (!File.Exists(_logFilePath))
                return Array.Empty<string>();

            var allLines = await File.ReadAllLinesAsync(_logFilePath);
            var startIndex = Math.Max(0, allLines.Length - lines);
            var recentLines = new string[Math.Min(lines, allLines.Length)];
            Array.Copy(allLines, startIndex, recentLines, 0, recentLines.Length);
            return recentLines;
        }

        public string GetLogDirectory() => _logDirectory;
        public string GetCurrentLogFile() => _logFilePath;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _flushTimer?.Dispose();
            FlushLogsAsync().Wait(TimeSpan.FromSeconds(5));
            _writeSemaphore?.Dispose();
        }
    }
}