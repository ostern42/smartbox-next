using System;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;

namespace SmartBoxNext
{
    public static class SilkNetSimpleTest
    {
        public static bool TestBasicD3D11()
        {
            try
            {
                unsafe
                {
                    // Get D3D11 API
                    using var d3d11 = D3D11.GetApi();
                    
                    ID3D11Device* device = null;
                    ID3D11DeviceContext* context = null;
                    D3DFeatureLevel featureLevel = 0;

                    // Create device
                    var hr = d3d11.CreateDevice(
                        null, // Default adapter
                        D3DDriverType.Hardware,
                        0, // No software module
                        (uint)CreateDeviceFlag.BgraSupport,
                        null, // Default feature levels
                        0,
                        D3D11.SdkVersion,
                        &device,
                        &featureLevel,
                        &context);

                    if (hr == 0 && device != null && context != null)
                    {
                        Console.WriteLine($"✅ Silk.NET D3D11 device created successfully!");
                        Console.WriteLine($"   Feature level: {featureLevel}");
                        
                        // Clean up
                        context->Release();
                        device->Release();
                        
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create D3D11 device. HRESULT: 0x{hr:X8}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Silk.NET exception: {ex.Message}");
                return false;
            }
        }
    }
}