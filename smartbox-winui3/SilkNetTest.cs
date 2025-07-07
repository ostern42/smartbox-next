using System;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;

namespace SmartBoxNext
{
    public static class SilkNetTest
    {
        public static unsafe bool TestSilkNetD3D11()
        {
            try
            {
                // Create DXGI factory
                var dxgi = DXGI.GetApi();
                IDXGIFactory1* factory = null;
                
                var hr = dxgi.CreateDXGIFactory1(SilkMarshal.GuidPtrOf<IDXGIFactory1>(), (void**)&factory);
                if (hr != 0)
                {
                    Console.WriteLine($"Silk.NET Test: Failed to create DXGI factory. HRESULT: 0x{hr:X8}");
                    return false;
                }

                Console.WriteLine("Silk.NET Test: DXGI factory created successfully!");

                // Create D3D11 device
                var d3d11 = D3D11.GetApi();
                ID3D11Device* device = null;
                ID3D11DeviceContext* context = null;
                D3DFeatureLevel featureLevel = 0;

                var flags = (uint)CreateDeviceFlag.BgraSupport;
                
                hr = d3d11.CreateDevice(
                    null, // Default adapter
                    D3DDriverType.Hardware,
                    0, // No software module
                    flags,
                    null, // Default feature levels
                    0,
                    D3D11.SdkVersion,
                    &device,
                    &featureLevel,
                    &context);

                if (hr == 0 && device != null && context != null)
                {
                    Console.WriteLine($"Silk.NET Test: D3D11 device created successfully!");
                    Console.WriteLine($"Silk.NET Test: Feature level: {featureLevel}");
                    
                    // Get device info
                    IDXGIDevice* dxgiDevice = null;
                    var deviceGuid = SilkMarshal.GuidPtrOf<IDXGIDevice>();
                    hr = device->QueryInterface(deviceGuid, (void**)&dxgiDevice);
                    
                    if (hr == 0 && dxgiDevice != null)
                    {
                        IDXGIAdapter* adapter = null;
                        dxgiDevice->GetAdapter(&adapter);
                        
                        if (adapter != null)
                        {
                            AdapterDesc desc;
                            adapter->GetDesc(&desc);
                            
                            var adapterName = new string((char*)desc.Description);
                            Console.WriteLine($"Silk.NET Test: GPU: {adapterName}");
                            
                            adapter->Release();
                        }
                        
                        dxgiDevice->Release();
                    }
                    
                    // Clean up
                    context->Release();
                    device->Release();
                    factory->Release();
                    
                    Console.WriteLine("Silk.NET Test: ✅ All tests passed! Silk.NET is working with .NET 8!");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Silk.NET Test: Failed to create D3D11 device. HRESULT: 0x{hr:X8}");
                    if (factory != null) factory->Release();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Silk.NET Test: Exception - {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public static void ListAvailableAdapters()
        {
            unsafe
            {
                try
                {
                    var dxgi = DXGI.GetApi();
                    IDXGIFactory1* factory = null;
                    
                    var hr = dxgi.CreateDXGIFactory1(SilkMarshal.GuidPtrOf<IDXGIFactory1>(), (void**)&factory);
                    if (hr != 0) return;

                    Console.WriteLine("\nSilk.NET: Available GPU Adapters:");
                    
                    uint i = 0;
                    IDXGIAdapter1* adapter = null;
                    
                    while (factory->EnumAdapters1(i, &adapter) == 0)
                    {
                        AdapterDesc1 desc;
                        adapter->GetDesc1(&desc);
                        
                        var name = new string((char*)desc.Description);
                        var dedicatedMemory = desc.DedicatedVideoMemory / (1024 * 1024);
                        
                        Console.WriteLine($"  [{i}] {name}");
                        Console.WriteLine($"      Dedicated Memory: {dedicatedMemory} MB");
                        Console.WriteLine($"      Device ID: 0x{desc.DeviceId:X4}");
                        
                        adapter->Release();
                        i++;
                    }
                    
                    factory->Release();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Silk.NET: Error listing adapters - {ex.Message}");
                }
            }
        }
    }
}