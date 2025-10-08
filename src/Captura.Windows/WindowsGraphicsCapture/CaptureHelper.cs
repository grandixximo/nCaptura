using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public static class CaptureHelper
    {
        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }
        
        [ComImport]
        [Guid("79C3F95B-31F7-4EC2-A464-632813A1D4B8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
        interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);
            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }
        
        public static GraphicsCaptureItem CreateItemForWindow(IntPtr hwnd)
        {
            try
            {
                var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                var interop = (IGraphicsCaptureItemInterop)factory;
                
                var itemGuid = typeof(GraphicsCaptureItem).GUID;
                var itemPointer = interop.CreateForWindow(hwnd, ref itemGuid);
                
                var captureItem = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
                Marshal.Release(itemPointer);
                
                return captureItem;
            }
            catch
            {
                return null;
            }
        }
        
        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmon)
        {
            try
            {
                var factory = WindowsRuntimeMarshal.GetActivationFactory(typeof(GraphicsCaptureItem));
                var interop = (IGraphicsCaptureItemInterop)factory;
                
                var itemGuid = typeof(GraphicsCaptureItem).GUID;
                var itemPointer = interop.CreateForMonitor(hmon, ref itemGuid);
                
                var captureItem = Marshal.GetObjectForIUnknown(itemPointer) as GraphicsCaptureItem;
                Marshal.Release(itemPointer);
                
                return captureItem;
            }
            catch
            {
                return null;
            }
        }
    }
}
