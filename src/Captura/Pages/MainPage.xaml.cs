using System.Windows;

namespace Captura
{
    public partial class MainPage
    {
        void OpenSettings(object Sender, RoutedEventArgs E)
        {
            SettingsWindow.ShowInstance();
        }
    }
}