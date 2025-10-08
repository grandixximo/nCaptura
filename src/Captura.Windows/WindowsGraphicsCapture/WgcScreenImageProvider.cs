using System;
using System.Drawing;
using System.Linq;
using Captura.Video;
using Captura.Windows.DirectX;
using SharpDX.DXGI;

namespace Captura.Windows.WindowsGraphicsCapture
{
    class WgcScreenImageProvider : IImageProvider
    {
        readonly WgcCaptureSession _capture;
        readonly Rectangle _screenBounds;
        
        public WgcScreenImageProvider(Rectangle screenBounds, IPreviewWindow previewWindow)
        {
            _screenBounds = screenBounds;
            Width = screenBounds.Width;
            Height = screenBounds.Height;
            
            PointTransform = P => new Point(P.X - screenBounds.Left, P.Y - screenBounds.Top);
            
            var hmon = MonitorHelper.GetMonitorFromRect(screenBounds);
            var adapter = FindAdapterForScreen(screenBounds);
            _capture = new WgcCaptureSession(hmon, Width, Height, previewWindow, adapter, isMonitor: true);
        }
        
        public int Height { get; }
        public int Width { get; }
        public Func<Point, Point> PointTransform { get; }
        
        public IEditableFrame Capture()
        {
            return _capture.Capture();
        }
        
        public void Dispose()
        {
            _capture?.Dispose();
        }
        
        public IBitmapFrame DummyFrame => Texture2DFrame.DummyFrame;
        
        static Adapter FindAdapterForScreen(Rectangle screenBounds)
        {
            try
            {
                using var factory = new Factory1();
                var outputs = factory.Adapters1.SelectMany(a => a.Outputs.Select(o => new { Adapter = a, Output = o }));
                
                var match = outputs.FirstOrDefault(item =>
                {
                    var bounds = item.Output.Description.DesktopBounds;
                    return bounds.Left == screenBounds.Left
                           && bounds.Right == screenBounds.Right
                           && bounds.Top == screenBounds.Top
                           && bounds.Bottom == screenBounds.Bottom;
                });
                
                return match?.Adapter;
            }
            catch
            {
                return null;
            }
        }
    }
}
