using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartBoxNext.CaptureService.Services;

namespace SmartBoxNext.CaptureService
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                // Create host builder
                var hostBuilder = Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options =>
                    {
                        options.ServiceName = "SmartBoxNext Capture Service";
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Register services
                        services.AddHostedService<CaptureService>();
                        services.AddSingleton<SharedMemoryManager>();
                        services.AddSingleton<ControlPipeServer>();
                        services.AddSingleton<YuanCaptureGraph>();
                        services.AddSingleton<FrameProcessor>();
                        
                        // Configure logging
                        services.AddLogging(builder =>
                        {
                            builder.AddConsole();
                            builder.AddEventLog(settings =>
                            {
                                settings.SourceName = "SmartBoxNext Capture Service";
                                settings.LogName = "Application";
                            });
                        });
                    });

                // Build and run
                var host = hostBuilder.Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                // Log startup errors
                Console.WriteLine($"Service startup failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Exit with error code
                Environment.Exit(1);
            }
        }
    }
}