using Captura.Models;
using Captura.ViewModels;

namespace Captura
{
    public partial class AudioPage
    {
        public AudioPage()
        {
            IsVisibleChanged += (S, E) =>
            {
                var audioSourceVm = ServiceProvider.Get<AudioSourceViewModel>();

                audioSourceVm.ListeningPeakLevel = IsVisible;
            };

            InitializeComponent();
            
            ServiceProvider.Get<MainViewModel>().Refreshed += () =>
            {
                AudioSourcesPanel?.Shake();
            };
        }
    }
}
