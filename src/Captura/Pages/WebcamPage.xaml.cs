using System.Windows;

namespace Captura
{
    public partial class WebcamPage
    {
        public WebcamPage()
        {
            InitializeComponent();
            
            // MainViewModel.Refreshed event removed - shake animations no longer needed
        }

        void Preview_Click(object Sender, RoutedEventArgs E)
        {
            WebCamWindow.Instance.ShowAndFocus();
        }
    }
}
