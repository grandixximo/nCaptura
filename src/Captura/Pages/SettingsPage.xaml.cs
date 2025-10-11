using System;
using System.Windows.Navigation;

namespace Captura
{
    public partial class SettingsPage
    {
        bool _webcamPageNavigated;

        public SettingsPage()
        {
            InitializeComponent();
            
            Loaded += (s, e) =>
            {
                // Navigate to WebcamPlacementPreviewPage singleton instance for the WebCam tab (only once)
                if (WebcamTabFrame != null && !_webcamPageNavigated)
                {
                    _webcamPageNavigated = true;
                    var webcamPage = ServiceProvider.Get<WebcamPlacementPreviewPage>();
                    WebcamTabFrame.Navigate(webcamPage);
                    
                    // Setup preview event handlers only once
                    webcamPage.SetupPreview();
                }
            };
        }
    }
}
