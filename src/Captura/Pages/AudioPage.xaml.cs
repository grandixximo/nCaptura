using System.Windows;
using System.Windows.Input;
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
        }

        void SetSoundFile(object Sender, MouseButtonEventArgs E)
        {
            if (Sender is FrameworkElement element && element.DataContext is SoundsViewModelItem vm)
            {
                vm.SetCommand.ExecuteIfCan();
            }
        }
    }
}
