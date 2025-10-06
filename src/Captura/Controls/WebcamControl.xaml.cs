using System.Windows;
using System.Windows.Interop;
using Captura.Webcam;
using Point = System.Drawing.Point;

namespace Captura
{
    public partial class WebcamControl
    {
        public CaptureWebcam Capture { get; private set; }

        public Filter VideoDevice { get; set; }

        public WebcamControl()
        {
            InitializeComponent();
        }

        public void Refresh()
        {
            //To change the video device, a dispose is needed.
            if (Capture != null)
            {
                Capture.Dispose();
                Capture = null;
            }

            //Create capture object.
            if (VideoDevice != null && PresentationSource.FromVisual(this) is HwndSource source)
            {
                Capture = new CaptureWebcam(VideoDevice, null, source.Handle);
                
                SizeChanged += (S, E) => OnSizeChange();

                if (IsVisible)
                    Capture.StartPreview();

                OnSizeChange();
            }
        }

        public void ShowOnMainWindow(Window MainWindow)
        {
            //To change the video device, a dispose is needed.
            if (Capture != null)
            {
                Capture.Dispose();
                Capture = null;
            }

            //Create capture object.
            if (VideoDevice != null && PresentationSource.FromVisual(MainWindow) is HwndSource source)
            {
                Capture = new CaptureWebcam(VideoDevice, null, source.Handle);
                
                Capture.StartPreview();

                Capture.OnPreviewWindowResize(280, 1, 50, 40);
            }
        }

        void OnSizeChange()
        {
            Capture?.OnPreviewWindowResize(5, 40, (int)ActualWidth, (int)ActualHeight);
        }

        void WebcamControl_OnIsVisibleChanged(object Sender, DependencyPropertyChangedEventArgs E)
        {
            if (IsVisible)
            {
                Refresh();
            }
            else
            {
                ShowOnMainWindow(MainWindow.Instance);
            }
        }
    }
}
