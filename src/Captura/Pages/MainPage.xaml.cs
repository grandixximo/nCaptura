using System.Windows;

namespace Captura
{
    public partial class MainPage
    {
        // OpenCanvas method removed - Canvas/Image Editor feature was removed from codebase

        void OpenSettings(object Sender, RoutedEventArgs E)
        {
            SettingsWindow.ShowInstance();
        }
    }
}