using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Silk.NET.Maths;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace SmartBoxNext
{
    public class SilkNetVideoCapture : IDisposable
    {
        private ID3D11Device* _device;
        private ID3D11DeviceContext* _context;
        private IDXGISwapChain1* _swapChain;
        private ID3D11Texture2D* _backBuffer;
        private ID3D11RenderTargetView* _renderTargetView;
        private D3D11 _d3d11;
        private DXGI _dxgi;
        private bool _isInitialized;

        public unsafe bool Initialize(IntPtr windowHandle, int width, int height)
        {
            try
            {
                _d3d11 = D3D11.GetApi();
                _dxgi = DXGI.GetApi();

                // Create D3D11 device with video support
                var flags = (uint)(CreateDeviceFlag.BgraSupport | CreateDeviceFlag.VideoSupport);
                D3DFeatureLevel featureLevel = 0;

                var hr = _d3d11.CreateDevice(
                    null, // Default adapter
                    D3DDriverType.Hardware,
                    0,
                    flags,
                    null,
                    0,
                    D3D11.SdkVersion,
                    &_device,
                    &featureLevel,
                    &_context);

                if (hr != 0) return false;

                // Create swap chain for the window
                IDXGIDevice* dxgiDevice = null;
                hr = _device->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIDevice>(), (void**)&dxgiDevice);
                if (hr != 0) return false;

                IDXGIAdapter* adapter = null;
                dxgiDevice->GetAdapter(&adapter);

                IDXGIFactory2* factory = null;
                adapter->GetParent(SilkMarshal.GuidPtrOf<IDXGIFactory2>(), (void**)&factory);

                // Swap chain description
                var swapChainDesc = new SwapChainDesc1
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    Format = Format.FormatB8G8R8A8Unorm,
                    Stereo = 0,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    BufferUsage = DXGI.UsageRenderTargetOutput,
                    BufferCount = 2,
                    Scaling = Scaling.Stretch,
                    SwapEffect = SwapEffect.FlipDiscard,
                    AlphaMode = AlphaMode.Unspecified,
                    Flags = 0
                };

                hr = factory->CreateSwapChainForHwnd(
                    (IUnknown*)_device,
                    windowHandle,
                    &swapChainDesc,
                    null,
                    null,
                    &_swapChain);

                if (hr != 0) return false;

                // Get back buffer and create render target view
                hr = _swapChain->GetBuffer(0, SilkMarshal.GuidPtrOf<ID3D11Texture2D>(), (void**)&_backBuffer);
                if (hr != 0) return false;

                hr = _device->CreateRenderTargetView((ID3D11Resource*)_backBuffer, null, &_renderTargetView);
                if (hr != 0) return false;

                _context->OMSetRenderTargets(1, &_renderTargetView, null);

                // Set viewport
                var viewport = new Viewport<float>(0, 0, width, height, 0, 1);
                _context->RSSetViewports(1, &viewport);

                _isInitialized = true;
                return true;
            }
            catch
            {
                Cleanup();
                return false;
            }
        }

        public unsafe bool RenderFrame(byte[] imageData, int width, int height)
        {
            if (!_isInitialized || imageData == null) return false;

            try
            {
                // Create texture from image data
                var textureDesc = new Texture2DDesc
                {
                    Width = (uint)width,
                    Height = (uint)height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.FormatB8G8R8A8Unorm,
                    SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
                    Usage = Usage.Default,
                    BindFlags = (uint)BindFlag.ShaderResource,
                    CPUAccessFlags = 0,
                    MiscFlags = 0
                };

                var subresourceData = new SubresourceData
                {
                    PSysMem = Marshal.UnsafeAddrOfPinnedArrayElement(imageData, 0).ToPointer(),
                    SysMemPitch = (uint)(width * 4), // BGRA = 4 bytes per pixel
                    SysMemSlicePitch = 0
                };

                ID3D11Texture2D* texture = null;
                var hr = _device->CreateTexture2D(&textureDesc, &subresourceData, &texture);
                if (hr != 0) return false;

                // Create shader resource view
                ID3D11ShaderResourceView* shaderResourceView = null;
                hr = _device->CreateShaderResourceView((ID3D11Resource*)texture, null, &shaderResourceView);
                if (hr != 0)
                {
                    texture->Release();
                    return false;
                }

                // Copy to back buffer (simplified - in real app you'd use shaders)
                _context->CopyResource((ID3D11Resource*)_backBuffer, (ID3D11Resource*)texture);

                // Present
                _swapChain->Present(1, 0); // VSync on

                // Cleanup
                shaderResourceView->Release();
                texture->Release();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public unsafe void Cleanup()
        {
            if (_renderTargetView != null)
            {
                _renderTargetView->Release();
                _renderTargetView = null;
            }

            if (_backBuffer != null)
            {
                _backBuffer->Release();
                _backBuffer = null;
            }

            if (_swapChain != null)
            {
                _swapChain->Release();
                _swapChain = null;
            }

            if (_context != null)
            {
                _context->Release();
                _context = null;
            }

            if (_device != null)
            {
                _device->Release();
                _device = null;
            }

            _d3d11?.Dispose();
            _dxgi?.Dispose();

            _isInitialized = false;
        }

        public void Dispose()
        {
            Cleanup();
        }
    }

    // Helper class to integrate with MediaCapture
    public class SilkNetCaptureEngine
    {
        private MediaCapture _mediaCapture;
        private SilkNetVideoCapture _videoCapture;
        private bool _isCapturing;

        public async Task<bool> InitializeAsync(IntPtr windowHandle, int width, int height)
        {
            try
            {
                // Initialize MediaCapture
                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                // Initialize Silk.NET renderer
                _videoCapture = new SilkNetVideoCapture();
                if (!_videoCapture.Initialize(windowHandle, width, height))
                {
                    return false;
                }

                return true;
            }
            catch
            {
                Cleanup();
                return false;
            }
        }

        public async Task<bool> StartCaptureAsync()
        {
            if (_isCapturing) return true;

            try
            {
                // Get video properties
                var videoProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
                if (videoProperties == null) return false;

                // Start frame reader
                var frameSource = _mediaCapture.FrameSources.Values.FirstOrDefault(source => source.Info.MediaStreamType == MediaStreamType.VideoPreview);
                if (frameSource == null) return false;

                var reader = await _mediaCapture.CreateFrameReaderAsync(frameSource);
                reader.FrameArrived += OnFrameArrived;
                await reader.StartAsync();

                _isCapturing = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var frame = sender.TryAcquireLatestFrame();
            if (frame?.VideoMediaFrame == null) return;

            var softwareBitmap = frame.VideoMediaFrame.SoftwareBitmap;
            if (softwareBitmap == null) return;

            // Convert to BGRA8 if needed
            if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8);
            }

            // Get pixel data
            using (var buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Read))
            {
                var reference = buffer.CreateReference();
                var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(reference);
                var bytes = new byte[reference.Capacity];
                dataReader.ReadBytes(bytes);

                // Render with Silk.NET
                _videoCapture.RenderFrame(bytes, softwareBitmap.PixelWidth, softwareBitmap.PixelHeight);
            }
        }

        public void Cleanup()
        {
            _isCapturing = false;
            _mediaCapture?.Dispose();
            _videoCapture?.Dispose();
        }
    }
}