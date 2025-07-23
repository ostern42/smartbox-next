# Streaming System Troubleshooting Guide

## 404 Error Resolution

If you're getting a 404 error when trying to access the streaming API, follow these steps:

### 0. Quick Fix (Try First!)

1. **Run as Administrator**: Right-click SmartBoxNext.exe and select "Run as administrator"
2. **Run Setup Script**: Execute `setup-http-listener.bat` as Administrator
3. **Check Console**: Look for "✓ API verified running on port 5002" in the console output

### 1. Verify Services are Initialized

Check that `MainWindow.xaml.cs` has the streaming server initialization:

```csharp
// Should be in InitializeApplication() method:
var streamingLogger = _loggerFactory.CreateLogger<StreamingServerManager>();
var ffmpegService = new FFmpegService(_loggerFactory.CreateLogger<FFmpegService>());
_streamingServerManager = new StreamingServerManager(streamingLogger, ffmpegService, 
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StreamingOutput"));
await _streamingServerManager.StartAsync();
```

### 2. Check Application Logs

Look for these messages in the console output:
- "Streaming server started on port 5002"
- "Streaming API started on port 5002"
- "Default users created:"

### 3. Test the API

1. Open http://localhost:5000/test-api.html in your browser
2. Click "Check Status" to verify the API is running
3. If offline, check Windows Firewall for port 5002

### 4. Common Issues and Solutions

#### API Not Starting
- **Issue**: Services are commented out as "MINA: Minimal build"
- **Solution**: Uncomment the service declarations and imports

#### Port Already in Use
- **Issue**: Another application is using port 5002
- **Solution**: Change the port in `StreamingApiService` constructor

#### FFmpeg Not Found
- **Issue**: FFmpeg is not installed or not in PATH
- **Solution**: 
  1. Download FFmpeg from https://ffmpeg.org/download.html
  2. Add to system PATH or place in application directory
  3. Verify with `ffmpeg -version` in command prompt

#### CORS Errors
- **Issue**: Browser blocking cross-origin requests
- **Solution**: The API already includes CORS headers. If still having issues, try:
  - Access from http://localhost instead of file://
  - Use the test page at http://localhost:8080/test-api.html

### 5. Quick Test URLs

After starting the application, test these URLs:

1. **Main UI**: http://localhost:8080/
2. **Test Page**: http://localhost:8080/test-api.html
3. **Streaming Player**: http://localhost:8080/streaming-player.html
4. **API Health Check**: http://localhost:5002/api/health (JSON response if running)

### 6. Manual API Test with cURL

```bash
# Test login
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"SmartBox2024!"}'
```

### 7. Verify Project References

Ensure these NuGet packages are installed:
```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.3" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.3" />
```

### 8. Enable Detailed Logging

In `MainWindow` constructor, set logging to Debug:
```csharp
builder.SetMinimumLevel(LogLevel.Debug);
```

## Next Steps

If the API is still not working:

1. Check the Windows Event Viewer for application errors
2. Run the application as Administrator
3. Temporarily disable Windows Defender/Firewall
4. Check if .NET 8 runtime is properly installed
5. Verify all project files are properly included in the build

## Contact Support

If issues persist, provide:
- Full console output from application start
- Screenshot of test-api.html results
- Windows version and .NET version (`dotnet --version`)
- Any error messages from Event Viewer