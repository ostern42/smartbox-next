# Port Update Summary

## Port Assignments

The SmartBox system uses the following ports:

| Service | Old Port | New Port | Description |
|---------|----------|----------|-------------|
| Web UI | 5000 | **8080** | Main web interface and static files |
| WebSocket | 5001 | **8081** | Real-time communication |
| Streaming API | 5002 | **5002** | No change - HLS streaming API |

## Files Updated

### Documentation Files
- ✅ `README.md` - Updated web server port references
- ✅ `STREAMING_SYSTEM_README.md` - Updated player URLs and WebSocket port
- ✅ `WIRELESS_TABLET_CONTROL.md` - Updated all interface URLs and ports
- ✅ `STREAMING_TROUBLESHOOTING.md` - Updated test URLs
- ✅ `STREAMING_QUICK_START.md` - Created with correct ports

### HTML Files  
- ✅ `wwwroot/test-api.html` - Updated test URLs display
- ✅ `wwwroot/streaming-player.html` - Already uses correct port 5002

### JavaScript Files
- ✅ `wwwroot/js/admin-control.js` - Updated WebSocket to port 8081

### Code Files
- ✅ `Services/StreamingServerManager.cs` - Fixed API thread management
- `setup-http-listener.bat` - Created for port configuration
- `run-diagnostics.bat` - Created for troubleshooting
- `diagnose-api.ps1` - Created for PowerShell diagnostics
- `ApiHealthCheck.cs` - Created for API testing

## Quick Reference

### Access URLs
- **Main UI**: http://localhost:8080/
- **Streaming Player**: http://localhost:8080/streaming-player.html  
- **Test API Page**: http://localhost:8080/test-api.html
- **Admin Interface**: http://localhost:8080/admin.html
- **Admin Demo**: http://localhost:8080/admin-demo.html

### API Endpoints
- **Streaming API**: http://localhost:5002/api/
- **Health Check**: http://localhost:5002/api/health
- **WebSocket**: ws://localhost:8081/

## Migration Notes

If you have any bookmarks or saved URLs, update them:
- Replace `:5000` with `:8080` for web interface
- Replace `:5001` with `:8081` for WebSocket connections
- Port 5002 remains unchanged for the streaming API

## Firewall Rules

If you have specific firewall rules, update them to allow:
- TCP Port 8080 (Web UI)
- TCP Port 8081 (WebSocket)
- TCP Port 5002 (Streaming API)

Run `setup-http-listener.bat` as Administrator to configure Windows HTTP listener.