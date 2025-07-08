using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace SmartBoxNext
{
    public partial class App : Application
    {
        private Window? m_window;

        public App()
        {
            this.InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                m_window = new MainWindow();
                m_window.Activate();
            }
            catch (Exception ex)
            {
                // Log to file if possible
                var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                System.IO.File.WriteAllText(logPath, $"Startup error: {ex.Message}\n{ex.StackTrace}");
                
                // Show error dialog
                var dialog = new ContentDialog
                {
                    Title = "Startup Error",
                    Content = $"Failed to start SmartBox: {ex.Message}",
                    CloseButtonText = "OK"
                };
                
                // Create a temporary window to show the error
                var errorWindow = new Window();
                errorWindow.Content = new Grid();
                errorWindow.Activate();
                dialog.XamlRoot = errorWindow.Content.XamlRoot;
                _ = dialog.ShowAsync();
            }
        }
    }
}