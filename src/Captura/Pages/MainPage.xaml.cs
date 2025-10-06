using System.Windows;

namespace Captura
{
    public partial class MainPage
    {
        void OpenCanvas(object Sender, RoutedEventArgs E)
        {
            MessageBox.Show("Image Editor has been removed in this version.", "Feature Removed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void OpenSettings(object Sender, RoutedEventArgs E)
        {
            SettingsWindow.ShowInstance();
        }
    }
}