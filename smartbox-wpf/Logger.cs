using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartBoxNext
{
    /// <summary>
    /// Simple file logger for medical-grade reliability
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logDirectory;
        private static readonly Timer _flushTimer;
        private static StreamWriter? _currentWriter;
        private static string? _currentLogFile;
        private static DateTime _currentLogDate;
        
        static Logger()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(_logDirectory);
            
            // Flush logs every 5 seconds for reliability
            _flushTimer = new Timer(_ => FlushLogs(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            // Ensure logs are flushed on app exit
            AppDomain.CurrentDomain.ProcessExit += (s, e) => CloseCurrentLog();
        }
        
        public static void LogInfo(string message, params object[] args)
        {
            Log("INFO", message, args);
        }
        
        public static void LogWarning(string message, params object[] args)
        {
            Log("WARN", message, args);
        }
        
        public static void LogError(string message, params object[] args)
        {
            Log("ERROR", message, args);
        }
        
        public static void LogError(Exception ex, string message, params object[] args)
        {
            var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
            Log("ERROR", $"{formattedMessage} - Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
        
        public static void LogDebug(string message, params object[] args)
        {
            Log("DEBUG", message, args);
        }
        
        private static void Log(string level, string message, params object[] args)
        {
            try
            {
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {formattedMessage}";
                
                lock (_lock)
                {
                    EnsureLogFileOpen();
                    _currentWriter?.WriteLine(logEntry);
                    
                    // Also write to console
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // Last resort - write to console
                Console.WriteLine($"[LOGGER ERROR] Failed to write log: {ex.Message}");
            }
        }
        
        private static void EnsureLogFileOpen()
        {
            var today = DateTime.Today;
            
            // Check if we need to rotate to a new file
            if (_currentWriter == null || _currentLogDate != today)
            {
                CloseCurrentLog();
                
                var fileName = $"smartbox_{today:yyyyMMdd}.log";
                _currentLogFile = Path.Combine(_logDirectory, fileName);
                _currentLogDate = today;
                
                // Open file in append mode
                _currentWriter = new StreamWriter(_currentLogFile, append: true, encoding: Encoding.UTF8)
                {
                    AutoFlush = false // We'll control flushing for performance
                };
                
                _currentWriter.WriteLine($"\n=== SmartBox Next Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
            }
        }
        
        private static void FlushLogs()
        {
            lock (_lock)
            {
                try
                {
                    _currentWriter?.Flush();
                }
                catch
                {
                    // Best effort
                }
            }
        }
        
        private static void CloseCurrentLog()
        {
            if (_currentWriter != null)
            {
                try
                {
                    _currentWriter.WriteLine($"\n=== SmartBox Next Log Closed at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
                    _currentWriter.Flush();
                    _currentWriter.Close();
                    _currentWriter.Dispose();
                }
                catch
                {
                    // Best effort
                }
                finally
                {
                    _currentWriter = null;
                }
            }
        }
        
        /// <summary>
        /// Get the path to today's log file
        /// </summary>
        public static string GetCurrentLogPath()
        {
            lock (_lock)
            {
                EnsureLogFileOpen();
                return _currentLogFile ?? Path.Combine(_logDirectory, $"smartbox_{DateTime.Today:yyyyMMdd}.log");
            }
        }
        
        /// <summary>
        /// Get all log files sorted by date (newest first)
        /// </summary>
        public static string[] GetLogFiles()
        {
            try
            {
                return Directory.GetFiles(_logDirectory, "smartbox_*.log")
                    .OrderByDescending(f => f)
                    .ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
        
        /// <summary>
        /// Get the log directory path
        /// </summary>
        public static string GetLogDirectory()
        {
            return _logDirectory;
        }
    }
}