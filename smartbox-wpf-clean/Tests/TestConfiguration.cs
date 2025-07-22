using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SmartBoxNext.Tests;

/// <summary>
/// Central configuration for all medical device testing
/// Ensures consistency across unit, integration, performance, and security tests
/// </summary>
public static class TestConfiguration
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public static ILogger Logger { get; private set; } = null!;

    static TestConfiguration()
    {
        InitializeConfiguration();
        InitializeServices();
    }

    private static void InitializeConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("TestConfigurations/test-config.json", optional: false)
            .AddJsonFile("TestConfigurations/medical-validation-config.json", optional: false)
            .AddJsonFile("TestConfigurations/security-test-config.json", optional: false)
            .AddEnvironmentVariables("SMARTBOX_TEST_");

        Configuration = builder.Build();
    }

    private static void InitializeServices()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Test-specific services
        services.AddScoped<MedicalTestDataManager>();
        services.AddScoped<DICOMTestManager>();
        services.AddScoped<SecurityTestManager>();
        services.AddScoped<PerformanceTestManager>();
        services.AddScoped<ComplianceValidator>();

        // Mock medical services for testing
        services.AddScoped<IMockMedicalComplianceService, MockMedicalComplianceService>();
        services.AddScoped<IMockHIPAAPrivacyService, MockHIPAAPrivacyService>();
        services.AddScoped<IMockDICOMSecurityService, MockDICOMSecurityService>();

        ServiceProvider = services.BuildServiceProvider();
        Logger = ServiceProvider.GetRequiredService<ILogger<TestConfiguration>>();
    }

    /// <summary>
    /// Gets test configuration specific to medical device requirements
    /// </summary>
    public static MedicalTestSettings GetMedicalTestSettings()
    {
        var settings = Configuration.GetSection("MedicalTesting").Get<MedicalTestSettings>();
        return settings ?? throw new InvalidOperationException("Medical test settings not configured");
    }

    /// <summary>
    /// Gets DICOM test server configuration
    /// </summary>
    public static DICOMTestSettings GetDICOMTestSettings()
    {
        var settings = Configuration.GetSection("DICOMTesting").Get<DICOMTestSettings>();
        return settings ?? throw new InvalidOperationException("DICOM test settings not configured");
    }

    /// <summary>
    /// Gets security test configuration including penetration test parameters
    /// </summary>
    public static SecurityTestSettings GetSecurityTestSettings()
    {
        var settings = Configuration.GetSection("SecurityTesting").Get<SecurityTestSettings>();
        return settings ?? throw new InvalidOperationException("Security test settings not configured");
    }

    /// <summary>
    /// Gets performance test configuration for 4-hour operation validation
    /// </summary>
    public static PerformanceTestSettings GetPerformanceTestSettings()
    {
        var settings = Configuration.GetSection("PerformanceTesting").Get<PerformanceTestSettings>();
        return settings ?? throw new InvalidOperationException("Performance test settings not configured");
    }
}

/// <summary>
/// Medical device test settings including FDA compliance requirements
/// </summary>
public class MedicalTestSettings
{
    public bool EnableFDACompliance { get; set; } = true;
    public bool EnableHIPAACompliance { get; set; } = true;
    public bool EnableGDPRCompliance { get; set; } = true;
    public bool ValidatePatientSafety { get; set; } = true;
    public int MaxTestPatients { get; set; } = 100;
    public TimeSpan MaxTestDuration { get; set; } = TimeSpan.FromHours(4);
    public string TestDataDirectory { get; set; } = "TestData";
    public string ComplianceReportDirectory { get; set; } = "Reports/Compliance";
}

/// <summary>
/// DICOM conformance test settings
/// </summary>
public class DICOMTestSettings
{
    public string TestServerHost { get; set; } = "localhost";
    public int TestServerPort { get; set; } = 11112;
    public string TestCallingAET { get; set; } = "SMARTBOX_TEST";
    public string TestCalledAET { get; set; } = "ORTHANC_TEST";
    public bool EnableTLSValidation { get; set; } = true;
    public string TestCertificatePath { get; set; } = "TestData/Certificates";
    public List<string> SupportedSOPClasses { get; set; } = new();
    public bool ValidateConformance { get; set; } = true;
}

/// <summary>
/// Security and penetration test settings
/// </summary>
public class SecurityTestSettings
{
    public bool EnablePenetrationTesting { get; set; } = true;
    public bool EnableVulnerabilityScanning { get; set; } = true;
    public bool ValidateEncryption { get; set; } = true;
    public bool TestAccessControls { get; set; } = true;
    public string SecurityScanToolPath { get; set; } = "";
    public List<string> AllowedCipherSuites { get; set; } = new();
    public int MinimumKeyLength { get; set; } = 2048;
    public TimeSpan SecurityTestTimeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Performance test settings for medical device operation validation
/// </summary>
public class PerformanceTestSettings
{
    public int MaxConcurrentUsers { get; set; } = 10;
    public int MaxFramesPerSecond { get; set; } = 60;
    public TimeSpan FourHourTestDuration { get; set; } = TimeSpan.FromHours(4);
    public int MemoryLeakThresholdMB { get; set; } = 100;
    public double CPUUsageThresholdPercent { get; set; } = 70.0;
    public int MaxResponseTimeMs { get; set; } = 1000;
    public string PerformanceReportDirectory { get; set; } = "Reports/Performance";
    public bool EnableStressTest { get; set; } = true;
    public bool EnableEnduranceTest { get; set; } = true;
}