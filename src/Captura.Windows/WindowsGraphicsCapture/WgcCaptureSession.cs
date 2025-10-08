using System;
using Captura.Video;
using Captura.Windows.DirectX;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public class WgcCaptureSession : IDisposable
    {
        readonly Direct2DEditorSession _editorSession;
        readonly WgcCapture _wgcCapture;
        
        public WgcCaptureSession(IntPtr handle, int width, int height, IPreviewWindow previewWindow, bool isMonitor = false)
        {
            _editorSession = new Direct2DEditorSession(width, height, previewWindow);
            _wgcCapture = new WgcCapture(handle, width, height, isMonitor);
        }
        
        public IEditableFrame Capture()
        {
            try
            {
                if (!_wgcCapture.Get(_editorSession.DesktopTexture))
                    return RepeatFrame.Instance;
            }
            catch
            {
                return RepeatFrame.Instance;
            }
            
            return new Direct2DEditor(_editorSession);
        }
        
        public void Dispose()
        {
            _wgcCapture?.Dispose();
            _editorSession?.Dispose();
        }
    }
}
