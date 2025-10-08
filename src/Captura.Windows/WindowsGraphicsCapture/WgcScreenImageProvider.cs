using System;
using System.Drawing;
using Captura.Video;
using Captura.Windows.DirectX;

namespace Captura.Windows.WindowsGraphicsCapture
{
    class WgcScreenImageProvider : IImageProvider
    {
        readonly WgcCaptureSession _capture;
        readonly Rectangle _screenBounds;
        
        public WgcScreenImageProvider(Rectangle screenBounds, IPreviewWindow previewWindow, IntPtr? monitorHandle = null)
        {
            _screenBounds = screenBounds;
            Width = screenBounds.Width;
            Height = screenBounds.Height;
            
            PointTransform = P => new Point(P.X - screenBounds.Left, P.Y - screenBounds.Top);
            
            // Use provided monitor handle if available, otherwise fallback to MonitorHelper
            var hmon = monitorHandle ?? MonitorHelper.GetMonitorFromRect(screenBounds);
            _capture = new WgcCaptureSession(hmon, Width, Height, previewWindow, isMonitor: true);
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
    }
}
