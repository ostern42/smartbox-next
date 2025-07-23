# SmartBox Streaming Quick Start Guide

## URLs and Ports

| Service | URL | Port |
|---------|-----|------|
| Main Web UI | http://localhost:8080/ | 8080 |
| Streaming Player | http://localhost:8080/streaming-player.html | 8080 |
| API Test Page | http://localhost:8080/test-api.html | 8080 |
| Streaming API | http://localhost:5002/api/ | 5002 |
| WebSocket | ws://localhost:8081 | 8081 |

## Default Credentials

- **Admin**: admin / SmartBox2024!
- **Operator**: operator / SmartBox2024!
- **Viewer**: viewer / SmartBox2024!

## Quick Start Steps

1. **Start SmartBox**: Run SmartBoxNext.exe (preferably as Administrator)
2. **Check Console**: Look for "✓ API verified running on port 5002"
3. **Open Browser**: Navigate to http://localhost:8080/streaming-player.html
4. **Login**: Use admin credentials
5. **Start Stream**: Click "Start Stream" and select your camera

## Troubleshooting

If you get errors:
1. Run `setup-http-listener.bat` as Administrator (one time only)
2. Run `run-diagnostics.bat` to check system status
3. Check Windows Firewall isn't blocking ports 5002, 8080, 8081

## API Quick Test

```bash
# Test API health
curl http://localhost:5002/api/health

# Login
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"SmartBox2024!"}'
```

## Features Available

- **Live Streaming**: Real-time HLS video streaming
- **DVR Mode**: 2-hour buffer for replay
- **Navigation**: Frame-accurate seeking
- **In/Out Marks**: Set markers for export
- **Export**: Download video segments
- **Multiple Streams**: Support for multiple concurrent streams