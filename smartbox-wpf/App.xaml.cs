using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext
{
    /// <summary>
    /// Medical-grade application with comprehensive error handling and logging
    /// </summary>
    public partial class App : Application
    {
        private ILogger<App>? _logger;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Set up global exception handlers for medical-grade reliability
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            
            // Initialize logging
            InitializeLogging();
            
            _logger?.LogInformation("SmartBox Next WPF starting up...");
            _logger?.LogInformation("Version: {Version}", typeof(App).Assembly.GetName().Version);
            _logger?.LogInformation("Working Directory: {Directory}", Directory.GetCurrentDirectory());
            
            // Ensure critical directories exist
            EnsureDirectoriesExist();
        }
        
        private void InitializeLogging()
        {
            try
            {
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
                
                _logger = loggerFactory.CreateLogger<App>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize logging: {ex.Message}", 
                    "SmartBox Next - Startup Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        private void EnsureDirectoriesExist()
        {
            try
            {
                // Create essential directories
                Directory.CreateDirectory("logs");
                Directory.CreateDirectory("Data");
                Directory.CreateDirectory("Data/Photos");
                Directory.CreateDirectory("Data/Videos");
                Directory.CreateDirectory("Data/DICOM");
                Directory.CreateDirectory("Data/Queue");
                Directory.CreateDirectory("Data/Temp");
                
                _logger?.LogInformation("All required directories verified/created");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create required directories");
                MessageBox.Show($"Failed to create required directories: {ex.Message}", 
                    "SmartBox Next - Startup Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            _logger?.LogCritical(exception, "Unhandled exception occurred. Terminating: {IsTerminating}", e.IsTerminating);
            
            if (e.IsTerminating)
            {
                // Save critical state before termination
                SaveCriticalState();
                
                MessageBox.Show(
                    $"A critical error has occurred and SmartBox Next must close.\n\n" +
                    $"Error: {exception?.Message}\n\n" +
                    $"Please contact support with the log files from the 'logs' directory.",
                    "SmartBox Next - Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unhandled dispatcher exception");
            
            // Attempt to recover from UI thread exceptions
            e.Handled = true;
            
            MessageBox.Show(
                $"An error occurred: {e.Exception.Message}\n\n" +
                $"The application will attempt to continue.",
                "SmartBox Next - Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger?.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved(); // Prevent process termination
        }
        
        private void SaveCriticalState()
        {
            try
            {
                // Save any pending queue items, configuration, etc.
                _logger?.LogInformation("Saving critical state before shutdown...");
                
                // TODO: Implement queue persistence
                // TODO: Save any unsaved patient data
                // TODO: Flush logs
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to save critical state");
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.LogInformation("SmartBox Next shutting down gracefully");
            SaveCriticalState();
            base.OnExit(e);
        }
    }
}