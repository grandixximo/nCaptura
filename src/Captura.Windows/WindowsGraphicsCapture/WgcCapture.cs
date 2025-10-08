using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public class WgcCapture : IDisposable
    {
        readonly Device _device;
        readonly GraphicsCaptureItem _captureItem;
        readonly Direct3D11CaptureFramePool _framePool;
        readonly GraphicsCaptureSession _session;
        
        Texture2D _lastFrame;
        readonly object _syncLock = new object();
        
        readonly int _width;
        readonly int _height;
        
        public WgcCapture(IntPtr handle, int width, int height, bool isMonitor = false)
        {
            _width = width;
            _height = height;
            
            _device = new Device(SharpDX.Direct3D.DriverType.Hardware, 
                DeviceCreationFlags.BgraSupport);
            
            _captureItem = isMonitor 
                ? CaptureHelper.CreateItemForMonitor(handle)
                : CaptureHelper.CreateItemForWindow(handle);
            
            if (_captureItem == null)
                throw new Exception("Failed to create GraphicsCaptureItem");
            
            var d3dDevice = CreateDirect3DDevice(_device);
            
            _framePool = Direct3D11CaptureFramePool.Create(
                d3dDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                _captureItem.Size);
            
            _session = _framePool.CreateCaptureSession(_captureItem);
            _session.IsCursorCaptureEnabled = false;
            
            _framePool.FrameArrived += OnFrameArrived;
            _session.StartCapture();
        }
        
        void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            lock (_syncLock)
            {
                using var frame = sender.TryGetNextFrame();
                if (frame == null) return;
                
                _lastFrame?.Dispose();
                _lastFrame = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface);
            }
        }
        
        public bool Get(Texture2D targetTexture)
        {
            lock (_syncLock)
            {
                if (_lastFrame == null)
                    return false;
                
                _device.ImmediateContext.CopyResource(_lastFrame, targetTexture);
                return true;
            }
        }
        
        public void Dispose()
        {
            lock (_syncLock)
            {
                if (_framePool != null)
                {
                    _framePool.FrameArrived -= OnFrameArrived;
                }
                
                _session?.Dispose();
                _framePool?.Dispose();
                _lastFrame?.Dispose();
                _captureItem?.Close();
                _device?.Dispose();
            }
        }
        
        static IDirect3DDevice CreateDirect3DDevice(Device d3dDevice)
        {
            using var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>();
            var inspectable = DXGIGetDirect3D11Device(dxgiDevice.NativePointer);
            var d3dDeviceInterface = Marshal.GetObjectForIUnknown(inspectable) as IDirect3DDevice;
            Marshal.Release(inspectable);
            return d3dDeviceInterface;
        }
        
        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        static extern UInt32 CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);
        
        static IntPtr DXGIGetDirect3D11Device(IntPtr dxgiDevice)
        {
            CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out IntPtr device);
            return device;
        }
    }
}
