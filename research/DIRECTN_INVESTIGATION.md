# DirectN Investigation for 60 FPS Video Capture

## What is DirectN?

DirectN is a .NET wrapper for DirectX and Windows APIs, providing direct access to:
- Direct3D 11/12
- Direct2D
- DirectWrite
- Media Foundation
- DXGI
- WIC (Windows Imaging Component)

GitHub: https://github.com/smourier/DirectN

## Investigation Results ❌

### Critical Issue: .NET Framework Only!

DirectN is **NOT compatible with .NET 8/WinUI3**:
- Package targets: .NET Framework 4.6.1 - 4.8.1
- No .NET Core/.NET 5+ support
- Cannot be used in modern WinUI3 applications

### NuGet Warning
```
warning NU1701: Package 'DirectN 1.17.2' was restored using 
'.NETFramework,Version=v4.6.1, ..., .NETFramework,Version=v4.8.1' 
instead of the project target framework 'net8.0-windows10.0.19041'
```

## Alternative Solutions for .NET 8/WinUI3

### 1. Win32 P/Invoke
- Direct calls to Windows APIs
- More work but full control
- Compatible with .NET 8

### 2. CsWin32
- Source generator for Win32 APIs
- Type-safe P/Invoke
- Microsoft supported

### 3. SharpDX (Deprecated but works)
- Still available on NuGet
- Works with .NET Core
- No longer maintained

### 4. Silk.NET
- Modern DirectX bindings
- Actively maintained
- .NET 5+ support

## Conclusion

DirectN looked promising but is **not viable** for our WinUI3/.NET 8 project. Need to explore other options for 60 FPS video capture.