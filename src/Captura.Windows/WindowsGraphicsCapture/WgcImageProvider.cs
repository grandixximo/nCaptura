using System;
using System.Drawing;
using Captura.Video;
using Captura.Windows.DirectX;

namespace Captura.Windows.WindowsGraphicsCapture
{
    class WgcImageProvider : IImageProvider
    {
        readonly WgcCaptureSession _capture;
        
        public WgcImageProvider(IntPtr windowHandle, int width, int height, IPreviewWindow previewWindow)
        {
            Width = width;
            Height = height;
            PointTransform = P => P;
            
            _capture = new WgcCaptureSession(windowHandle, width, height, previewWindow);
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
