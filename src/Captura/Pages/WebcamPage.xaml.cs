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
    }
}
