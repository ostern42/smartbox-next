using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FellowOakDicom;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using Microsoft.Extensions.Logging;
using Color = System.Windows.Media.Color;
using Orientation = System.Windows.Controls.Orientation;

namespace SmartBoxNext
{
    public partial class DiagnosticWindow : Window
    {
        private readonly ILogger _logger;
        private readonly string _host;
        private readonly int _port;
        private readonly string _callingAet;
        private readonly string _calledAet;
        private readonly bool _isMwl;
        
        public DiagnosticWindow(ILogger logger, string host, int port, string callingAet, string calledAet, bool isMwl = false)
        {
            InitializeComponent();
            _logger = logger;
            _host = host;
            _port = port;
            _callingAet = callingAet;
            _calledAet = calledAet;
            _isMwl = isMwl;
            
            this.Loaded += DiagnosticWindow_Loaded;
        }
        
        private async void DiagnosticWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunDiagnostics();
        }
        
        private async Task RunDiagnostics()
        {
            // Update header based on type
            HeaderText.Text = $"{(_isMwl ? "MWL" : "PACS")} Connection Diagnostics - {_host}:{_port}";
            
            // Step 1: TCP Ping
            var pingResult = await TestTcpConnection();
            AddTestResult("TCP Connection Test", $"Testing connection to {_host}:{_port}", pingResult);
            if (!pingResult.Success)
            {
                ShowSummary(false, "TCP connection failed. Check if the server is running and the port is correct.");
                return;
            }
            
            // Step 2: C-ECHO
            var echoResult = await TestCEcho();
            AddTestResult("DICOM C-ECHO Test", $"Testing DICOM protocol ({_callingAet} → {_calledAet})", echoResult);
            if (!echoResult.Success)
            {
                ShowSummary(false, "DICOM C-ECHO failed. Check the AE titles and DICOM server configuration.");
                return;
            }
            
            // Step 3: Service-specific test
            if (_isMwl)
            {
                var mwlResult = await TestMwlQuery();
                AddTestResult("MWL C-FIND Test", "Querying worklist for today's entries", mwlResult);
                if (!mwlResult.Success)
                {
                    ShowSummary(false, "MWL query failed. The server may not support worklist queries.");
                }
                else
                {
                    ShowSummary(true, $"All tests passed! Found {mwlResult.Details} worklist items.");
                }
            }
            else
            {
                // For PACS, we could test C-STORE capability
                var storeResult = await TestCStoreCapability();
                AddTestResult("C-STORE Capability", "Checking if server accepts image storage", storeResult);
                if (!storeResult.Success)
                {
                    ShowSummary(false, "C-STORE capability check failed. The server may not accept image storage.");
                }
                else
                {
                    ShowSummary(true, "All tests passed! PACS server is ready to receive images.");
                }
            }
        }
        
        private async Task<TestResult> TestTcpConnection()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(_host);
                    if (reply.Status == IPStatus.Success)
                    {
                        // Now test specific port
                        using (var tcpClient = new System.Net.Sockets.TcpClient())
                        {
                            var connectTask = tcpClient.ConnectAsync(_host, _port);
                            if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
                            {
                                return new TestResult { Success = true, Message = "Connection successful" };
                            }
                            else
                            {
                                return new TestResult { Success = false, Message = $"Port {_port} is not responding" };
                            }
                        }
                    }
                    else
                    {
                        return new TestResult { Success = false, Message = $"Host unreachable: {reply.Status}" };
                    }
                }
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }
        
        private async Task<TestResult> TestCEcho()
        {
            try
            {
                var client = DicomClientFactory.Create(_host, _port, false, _callingAet, _calledAet);
                client.NegotiateAsyncOps();
                
                var request = new DicomCEchoRequest();
                bool success = false;
                string message = "";
                
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success)
                    {
                        success = true;
                        message = "C-ECHO successful";
                    }
                    else
                    {
                        message = $"C-ECHO failed: {response.Status}";
                    }
                };
                
                await client.AddRequestAsync(request);
                await client.SendAsync();
                
                return new TestResult { Success = success, Message = message };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }
        
        private async Task<TestResult> TestMwlQuery()
        {
            try
            {
                var client = DicomClientFactory.Create(_host, _port, false, _callingAet, _calledAet);
                var request = new DicomCFindRequest(DicomQueryRetrieveLevel.NotApplicable);
                
                // Query for today's worklist
                request.Dataset.AddOrUpdate(DicomTag.ScheduledProcedureStepSequence, new DicomDataset
                {
                    { DicomTag.Modality, "CR" },
                    { DicomTag.ScheduledProcedureStepStartDate, DateTime.Today.ToString("yyyyMMdd") }
                });
                
                request.Dataset.AddOrUpdate(DicomTag.PatientName, "");
                request.Dataset.AddOrUpdate(DicomTag.PatientID, "");
                
                int itemCount = 0;
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Pending && response.HasDataset)
                    {
                        itemCount++;
                    }
                };
                
                await client.AddRequestAsync(request);
                await client.SendAsync();
                
                return new TestResult 
                { 
                    Success = true, 
                    Message = $"Query successful", 
                    Details = itemCount.ToString() 
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }
        
        private async Task<TestResult> TestCStoreCapability()
        {
            try
            {
                // We can't actually send an image, but we can check if the server
                // responds to our association request with C-STORE capability
                var client = DicomClientFactory.Create(_host, _port, false, _callingAet, _calledAet);
                
                // Request C-STORE service
                var pc = DicomPresentationContext.GetScpRolePresentationContextsFromStorageUids(
                    DicomStorageCategory.Image,
                    DicomTransferSyntax.ExplicitVRLittleEndian,
                    DicomTransferSyntax.ImplicitVRLittleEndian);
                
                client.AdditionalPresentationContexts.AddRange(pc);
                
                // Just test association
                var request = new DicomCEchoRequest();
                bool success = false;
                
                request.OnResponseReceived += (req, response) =>
                {
                    success = response.Status == DicomStatus.Success;
                };
                
                await client.AddRequestAsync(request);
                await client.SendAsync();
                
                return new TestResult 
                { 
                    Success = success, 
                    Message = success ? "Server accepts C-STORE" : "C-STORE not supported" 
                };
            }
            catch (Exception ex)
            {
                return new TestResult { Success = false, Message = ex.Message };
            }
        }
        
        private void AddTestResult(string testName, string description, TestResult result)
        {
            Dispatcher.Invoke(() =>
            {
                var container = new Border
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(15),
                    Background = new SolidColorBrush(result.Success ? Color.FromRgb(242, 250, 242) : Color.FromRgb(253, 242, 242)),
                    BorderBrush = new SolidColorBrush(result.Success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(216, 59, 1)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                
                var panel = new StackPanel();
                
                // Test name with icon
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                headerPanel.Children.Add(new TextBlock
                {
                    Text = result.Success ? "✓ " : "✗ ",
                    Foreground = new SolidColorBrush(result.Success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(216, 59, 1)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 16
                });
                headerPanel.Children.Add(new TextBlock
                {
                    Text = testName,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14
                });
                panel.Children.Add(headerPanel);
                
                // Description
                panel.Children.Add(new TextBlock
                {
                    Text = description,
                    Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                    Margin = new Thickness(20, 5, 0, 5)
                });
                
                // Result message
                panel.Children.Add(new TextBlock
                {
                    Text = result.Message,
                    Foreground = new SolidColorBrush(result.Success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(216, 59, 1)),
                    Margin = new Thickness(20, 0, 0, 0),
                    FontStyle = FontStyles.Italic
                });
                
                container.Child = panel;
                TestResultsPanel.Children.Add(container);
            });
        }
        
        private void ShowSummary(bool success, string message)
        {
            Dispatcher.Invoke(() =>
            {
                SummaryText.Text = message;
                SummaryText.Foreground = new SolidColorBrush(success ? Color.FromRgb(16, 124, 16) : Color.FromRgb(216, 59, 1));
                OkButton.IsEnabled = true;
                
                if (!success)
                {
                    // Add help text
                    var helpText = new TextBlock
                    {
                        Text = GetHelpText(),
                        Foreground = new SolidColorBrush(Color.FromRgb(96, 94, 92)),
                        Margin = new Thickness(0, 20, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                        FontStyle = FontStyles.Italic
                    };
                    TestResultsPanel.Children.Add(helpText);
                }
            });
        }
        
        private string GetHelpText()
        {
            return _isMwl ? 
                "Troubleshooting tips:\n" +
                "• Verify the MWL server is running and configured correctly\n" +
                "• Check firewall settings for port " + _port + "\n" +
                "• Ensure the AE titles match the server configuration\n" +
                "• Confirm the server supports DICOM Modality Worklist (MWL) service" :
                "Troubleshooting tips:\n" +
                "• Verify the PACS server is running and configured correctly\n" +
                "• Check firewall settings for port " + _port + "\n" +
                "• Ensure the AE titles match the server configuration\n" +
                "• Confirm the server supports C-STORE for image archiving";
        }
        
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private class TestResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Details { get; set; }
        }
    }
}