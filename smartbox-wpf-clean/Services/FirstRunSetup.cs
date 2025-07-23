using System;
using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace SmartBoxNext.Services
{
    public class FirstRunSetup
    {
        private readonly ILogger<FirstRunSetup> _logger;

        public FirstRunSetup(ILogger<FirstRunSetup> logger)
        {
            _logger = logger;
        }

        public bool CheckUrlAclExists(int port)
        {
            try
            {
                // Try to create a listener - if it fails, we need the ACL
                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add($"http://+:{port}/");
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch (HttpListenerException ex)
            {
                _logger.LogWarning($"URL ACL check failed: {ex.Message}");
                return false;
            }
        }

        public bool IsRunningAsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public void PromptForSetup()
        {
            var result = MessageBox.Show(
                "SmartBox needs to configure Windows to allow network access.\n\n" +
                "This is a one-time setup that requires Administrator privileges.\n\n" +
                "Would you like to run the setup now?",
                "SmartBox - Initial Setup Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                RunSetupAsAdmin();
            }
            else
            {
                MessageBox.Show(
                    "Setup cancelled. The streaming features will not work.\n\n" +
                    "You can run 'setup-http-listener.bat' as Administrator later.",
                    "SmartBox - Setup Cancelled",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RunSetupAsAdmin()
        {
            try
            {
                var setupPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "setup-http-listener.bat");

                var startInfo = new ProcessStartInfo
                {
                    FileName = setupPath,
                    UseShellExecute = true,
                    Verb = "runas" // Run as administrator
                };

                var process = Process.Start(startInfo);
                process?.WaitForExit();

                MessageBox.Show(
                    "Setup completed! Please restart SmartBox.",
                    "SmartBox - Setup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Exit application for restart
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run setup");
                MessageBox.Show(
                    $"Setup failed: {ex.Message}\n\n" +
                    "Please run 'setup-http-listener.bat' manually as Administrator.",
                    "SmartBox - Setup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}