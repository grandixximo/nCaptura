using System.Windows;

namespace Captura
{
    public partial class MainPageClassic
    {
        public MainPageClassic()
        {
            InitializeComponent();
        }

        void OpenSettings(object Sender, RoutedEventArgs E)
        {
            SettingsWindow.ShowInstance();
        }
    }
}