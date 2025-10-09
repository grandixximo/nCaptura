using System.Windows;
using Captura.ViewModels;

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
            var webcamModel = ServiceProvider.Get<WebcamModel>();
            var webcam = webcamModel?.Capture;
            
            if (webcam == null)
            {
                ServiceProvider.MessageProvider?.ShowError("No camera is currently active.\n\nPlease select a camera and start preview first.", "Camera Not Active");
                return;
            }
            
            try
            {
                var properties = webcam.GetCameraProperties();
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
