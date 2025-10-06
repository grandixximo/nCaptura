using System.Windows;
using Captura.Models;
using Captura.Video;
using Captura.ViewModels;

namespace Captura
{
    public partial class WebCamWindow
    {
        WebCamWindow()
        {
            InitializeComponent();
            
            Closing += (S, E) =>
            {
                Hide();

                E.Cancel = true;
            };
        }

        public static WebCamWindow Instance { get; } = new WebCamWindow();

        public WebcamControl GetWebCamControl() => WebCameraControl;

        void CloseButton_Click(object Sender, RoutedEventArgs E) => Close();
        
        async void CaptureImage_OnClick(object Sender, RoutedEventArgs E)
        {
            try
            {
                // Get image from webcam control
                var bitmapLoader = ServiceProvider.Get<IBitmapLoader>();
                var img = WebCameraControl.Capture?.GetFrame(bitmapLoader);
                
                if (img != null)
                {
                    var screenShotViewModel = ServiceProvider.Get<ScreenShotViewModel>();
                    // Use DiskWriter to save the screenshot
                    await screenShotViewModel.DiskWriter.Save(img);
                }
            }
            catch { }
        }
    }
}