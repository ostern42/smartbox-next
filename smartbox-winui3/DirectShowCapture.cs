using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace SmartBoxNext
{
    /// <summary>
    /// DirectShow-based video capture for professional performance
    /// This is how OBS, VirtualDub, and other professional software capture video
    /// </summary>
    public class DirectShowCapture
    {
        // DirectShow GUIDs
        private static readonly Guid CLSID_SystemDeviceEnum = new Guid("62BE5D10-60EB-11d0-BD3B-00A0C911CE86");
        private static readonly Guid CLSID_VideoInputDeviceCategory = new Guid("860BB310-5D01-11d0-BD3B-00A0C911CE86");
        private static readonly Guid IID_IPropertyBag = new Guid("55272A00-42CB-11CE-8135-00AA004BB851");

        // DirectShow Interfaces (simplified for device enumeration)
        [ComImport, Guid("29840822-5B84-11D0-BD3B-00A0C911CE86"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ICreateDevEnum
        {
            [PreserveSig]
            int CreateClassEnumerator([In] ref Guid pType, out IEnumMoniker ppEnumMoniker, [In] int dwFlags);
        }

        [ComImport, Guid("55272A00-42CB-11CE-8135-00AA004BB851"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyBag
        {
            [PreserveSig]
            int Read([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, 
                    [Out, MarshalAs(UnmanagedType.Struct)] out object pVar, 
                    [In] IntPtr pErrorLog);
            
            [PreserveSig]
            int Write([In, MarshalAs(UnmanagedType.LPWStr)] string pszPropName, 
                     [In, MarshalAs(UnmanagedType.Struct)] ref object pVar);
        }

        public class VideoDeviceInfo
        {
            public string Name { get; set; } = "";
            public string DevicePath { get; set; } = "";
            public IMoniker? Moniker { get; set; }
            public List<VideoFormatInfo> SupportedFormats { get; set; } = new List<VideoFormatInfo>();
        }

        public class VideoFormatInfo
        {
            public string Format { get; set; } = "";
            public int Width { get; set; }
            public int Height { get; set; }
            public double FrameRate { get; set; }
            public string Compression { get; set; } = "";
            public int BitDepth { get; set; }
            
            public override string ToString()
            {
                return $"{Format} {Width}x{Height} @ {FrameRate:F1} FPS";
            }
        }

        /// <summary>
        /// Enumerate all video capture devices using DirectShow
        /// This gives us much more detailed information than MediaCapture
        /// </summary>
        public static List<VideoDeviceInfo> EnumerateVideoDevices()
        {
            var devices = new List<VideoDeviceInfo>();
            ICreateDevEnum? devEnum = null;
            IEnumMoniker? enumMoniker = null;

            try
            {
                // Create the system device enumerator
                var type = Type.GetTypeFromCLSID(CLSID_SystemDeviceEnum);
                if (type == null) return devices;
                
                devEnum = Activator.CreateInstance(type) as ICreateDevEnum;
                if (devEnum == null) return devices;

                // Create an enumerator for video input devices
                var hr = devEnum.CreateClassEnumerator(ref CLSID_VideoInputDeviceCategory, out enumMoniker, 0);
                if (hr != 0 || enumMoniker == null) return devices;

                // Enumerate all video input devices
                var monikers = new IMoniker[1];
                while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    var device = GetDeviceInfo(monikers[0]);
                    if (device != null)
                    {
                        devices.Add(device);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DirectShow enumeration error: {ex.Message}");
            }
            finally
            {
                // Clean up COM objects
                if (enumMoniker != null) Marshal.ReleaseComObject(enumMoniker);
                if (devEnum != null) Marshal.ReleaseComObject(devEnum);
            }

            return devices;
        }

        private static VideoDeviceInfo? GetDeviceInfo(IMoniker moniker)
        {
            IPropertyBag? propertyBag = null;
            
            try
            {
                var device = new VideoDeviceInfo { Moniker = moniker };
                
                // Get the property bag for the moniker
                var guid = IID_IPropertyBag;
                moniker.BindToStorage(null, null, ref guid, out var bag);
                propertyBag = bag as IPropertyBag;
                
                if (propertyBag != null)
                {
                    // Get device name
                    object varName;
                    if (propertyBag.Read("FriendlyName", out varName, IntPtr.Zero) == 0)
                    {
                        device.Name = varName?.ToString() ?? "Unknown";
                    }
                    
                    // Get device path
                    object varPath;
                    if (propertyBag.Read("DevicePath", out varPath, IntPtr.Zero) == 0)
                    {
                        device.DevicePath = varPath?.ToString() ?? "";
                    }
                }
                
                return device;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (propertyBag != null) Marshal.ReleaseComObject(propertyBag);
            }
        }

        /// <summary>
        /// Get detailed format information for a device
        /// This would normally query IAMStreamConfig for all supported formats
        /// </summary>
        public static void GetDeviceFormats(VideoDeviceInfo device)
        {
            // This is where we would:
            // 1. Create filter graph
            // 2. Add device filter
            // 3. Query IAMStreamConfig
            // 4. Enumerate all media types
            // 5. Extract format details (YUY2, MJPEG, etc.)
            
            // For now, add placeholder data
            device.SupportedFormats.Add(new VideoFormatInfo 
            { 
                Format = "YUY2", 
                Width = 1920, 
                Height = 1080, 
                FrameRate = 30.0,
                BitDepth = 16,
                Compression = "Uncompressed"
            });
            
            device.SupportedFormats.Add(new VideoFormatInfo 
            { 
                Format = "MJPEG", 
                Width = 1920, 
                Height = 1080, 
                FrameRate = 60.0,
                BitDepth = 24,
                Compression = "JPEG"
            });
        }
    }

    /// <summary>
    /// Example of how to use DirectShow for actual capture
    /// This would be the foundation for a professional capture pipeline
    /// </summary>
    public class DirectShowCaptureSession
    {
        // This is where we would implement:
        // 1. Graph building
        // 2. Format negotiation
        // 3. Sample grabber setup
        // 4. Hardware timestamp extraction
        // 5. Zero-copy to GPU texture
        // 6. Callback for each frame
        
        public delegate void FrameCallback(IntPtr buffer, int width, int height, long timestamp);
        
        public event FrameCallback? OnFrame;
        
        public void StartCapture(DirectShowCapture.VideoDeviceInfo device, 
                                DirectShowCapture.VideoFormatInfo format)
        {
            // Build DirectShow graph
            // Configure for selected format
            // Start streaming
            // Frames arrive in callback
        }
    }
}