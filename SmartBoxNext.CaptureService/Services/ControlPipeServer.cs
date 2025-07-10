using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SmartBoxNext.CaptureService.Services
{
    /// <summary>
    /// Command message structure for Named Pipe communication
    /// </summary>
    public class ControlCommand
    {
        public string Command { get; set; } = string.Empty;
        public string? Parameters { get; set; }
        public string? RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response message structure
    /// </summary>
    public class ControlResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public string? RequestId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Named Pipe server for control commands from UI application
    /// </summary>
    public class ControlPipeServer : IDisposable
    {
        private readonly ILogger<ControlPipeServer> _logger;
        private readonly YuanCaptureGraph _captureGraph;
        
        private const string PIPE_NAME = "SmartBoxNextControl";
        private const int MAX_CONCURRENT_CONNECTIONS = 4;
        
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _serverTask;
        private bool _isRunning = false;

        public ControlPipeServer(ILogger<ControlPipeServer> logger, YuanCaptureGraph captureGraph)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _captureGraph = captureGraph ?? throw new ArgumentNullException(nameof(captureGraph));
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Starting Control Pipe Server...");
            
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _serverTask = Task.Run(ServerLoopAsync);
                _isRunning = true;
                
                _logger.LogInformation("Control Pipe Server started on pipe: {PipeName}", PIPE_NAME);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Control Pipe Server");
                throw;
            }

            await Task.CompletedTask;
        }

        private async Task ServerLoopAsync()
        {
            _logger.LogInformation("Control Pipe Server loop starting...");

            while (!_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    // Create new pipe server for each connection
                    _pipeServer = new NamedPipeServerStream(
                        pipeName: PIPE_NAME,
                        direction: PipeDirection.InOut,
                        maxNumberOfServerInstances: MAX_CONCURRENT_CONNECTIONS,
                        transmissionMode: PipeTransmissionMode.Message,
                        options: PipeOptions.Asynchronous);

                    _logger.LogDebug("Waiting for pipe connection...");

                    // Wait for client connection
                    await _pipeServer.WaitForConnectionAsync(_cancellationTokenSource.Token);
                    
                    _logger.LogInformation("Client connected to control pipe");

                    // Handle this connection
                    await HandleClientAsync(_pipeServer, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Control Pipe Server cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Control Pipe Server loop");
                    
                    // Wait a bit before retrying
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
                finally
                {
                    _pipeServer?.Dispose();
                    _pipeServer = null;
                }
            }

            _logger.LogInformation("Control Pipe Server loop ended");
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                using var reader = new StreamReader(pipeServer, Encoding.UTF8, leaveOpen: true);
                using var writer = new StreamWriter(pipeServer, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

                while (pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Read command
                        var commandJson = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(commandJson))
                        {
                            break; // Client disconnected
                        }

                        _logger.LogDebug("Received command: {Command}", commandJson);

                        // Parse command
                        var command = JsonConvert.DeserializeObject<ControlCommand>(commandJson);
                        if (command == null)
                        {
                            await SendResponseAsync(writer, false, "Invalid command format", null, null);
                            continue;
                        }

                        // Process command
                        var response = await ProcessCommandAsync(command);
                        
                        // Send response
                        await SendResponseAsync(writer, response.Success, response.Message, response.Data, command.RequestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error handling client command");
                        await SendResponseAsync(writer, false, $"Error: {ex.Message}", null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in client handler");
            }
            finally
            {
                _logger.LogInformation("Client disconnected from control pipe");
            }
        }

        private async Task<ControlResponse> ProcessCommandAsync(ControlCommand command)
        {
            _logger.LogInformation("Processing command: {Command}", command.Command);

            try
            {
                switch (command.Command.ToLowerInvariant())
                {
                    case "start":
                        await _captureGraph.StartCaptureAsync();
                        return new ControlResponse { Success = true, Message = "Capture started" };

                    case "stop":
                        await _captureGraph.StopCaptureAsync();
                        return new ControlResponse { Success = true, Message = "Capture stopped" };

                    case "getinputs":
                        var inputs = await _captureGraph.GetAvailableInputsAsync();
                        return new ControlResponse { Success = true, Message = "Inputs retrieved", Data = inputs };

                    case "selectinput":
                        if (int.TryParse(command.Parameters, out var inputIndex))
                        {
                            await _captureGraph.SelectInputAsync(inputIndex);
                            return new ControlResponse { Success = true, Message = $"Input {inputIndex} selected" };
                        }
                        return new ControlResponse { Success = false, Message = "Invalid input index" };

                    case "getcurrentinput":
                        var currentInput = await _captureGraph.GetCurrentInputAsync();
                        return new ControlResponse { Success = true, Message = "Current input retrieved", Data = currentInput };

                    case "snapshot":
                        var snapshotResult = await _captureGraph.CaptureSnapshotAsync();
                        return new ControlResponse { Success = snapshotResult != null, Message = "Snapshot captured", Data = snapshotResult };

                    case "getstats":
                        var stats = _captureGraph.GetStatistics();
                        return new ControlResponse { Success = true, Message = "Statistics retrieved", Data = stats };

                    case "ping":
                        return new ControlResponse { Success = true, Message = "Pong" };

                    default:
                        _logger.LogWarning("Unknown command: {Command}", command.Command);
                        return new ControlResponse { Success = false, Message = $"Unknown command: {command.Command}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", command.Command);
                return new ControlResponse { Success = false, Message = ex.Message };
            }
        }

        private async Task SendResponseAsync(StreamWriter writer, bool success, string? message, object? data, string? requestId)
        {
            try
            {
                var response = new ControlResponse
                {
                    Success = success,
                    Message = message,
                    Data = data,
                    RequestId = requestId
                };

                var responseJson = JsonConvert.SerializeObject(response);
                await writer.WriteLineAsync(responseJson);
                
                _logger.LogDebug("Sent response: {Response}", responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending response");
            }
        }

        public async Task StopAsync()
        {
            _logger.LogInformation("Stopping Control Pipe Server...");
            
            try
            {
                _isRunning = false;
                
                // Cancel the server loop
                _cancellationTokenSource?.Cancel();
                
                // Close current pipe connection
                _pipeServer?.Dispose();
                
                // Wait for server task to complete
                if (_serverTask != null)
                {
                    await _serverTask;
                }
                
                _logger.LogInformation("Control Pipe Server stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Control Pipe Server");
            }
        }

        public void Dispose()
        {
            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _pipeServer?.Dispose();
        }
    }
}