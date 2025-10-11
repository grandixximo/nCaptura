using System;
using System.Windows;
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

        void AboutBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (AboutFrame.CanGoBack)
            {
                AboutFrame.GoBack();
            }
        }

        void AboutForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (AboutFrame.CanGoForward)
            {
                AboutFrame.GoForward();
            }
        }

        void AboutFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Forcefully hide NavigationUI after navigation
            if (sender is System.Windows.Controls.Frame frame)
            {
                frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
                
                // Also ensure the page itself doesn't have NavigationService chrome
                if (e.Content is System.Windows.Controls.Page page)
                {
                    page.ShowsNavigationUI = false;
                }
            }
        }
    }
}
