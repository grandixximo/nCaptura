using System.Windows;
using Captura.ViewModels;
using Captura.Webcam;

namespace Captura
{
    public partial class WebcamPage
    {
        public WebcamPage()
        {
            InitializeComponent();
            
            ServiceProvider.Get<MainViewModel>().Refreshed += () =>
            {
                WebcamComboBox?.Shake();
            };
        }

        void Preview_Click(object Sender, RoutedEventArgs E)
        {
            WebCamWindow.Instance.ShowAndFocus();
        }

        void ShowCameraProperties_Click(object sender, RoutedEventArgs e)
        {
            // Access the webcam through the WebCamWindow in classic UI
            var webcamControl = WebCamWindow.Instance.GetWebCamControl();
            var capture = webcamControl?.Capture;
            
            if (capture == null)
            {
                ServiceProvider.MessageProvider?.ShowError("No camera is currently active.\n\nPlease select a camera and start preview first.", "Camera Not Active");
                return;
            }
            
            try
            {
                var properties = capture.GetCameraProperties();
                var window = new CameraPropertiesWindow(properties)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
            catch (System.Exception ex)
            {
                ServiceProvider.MessageProvider?.ShowError($"Failed to get camera properties:\n\n{ex.Message}", "Error");
            }
        }
    }
}
