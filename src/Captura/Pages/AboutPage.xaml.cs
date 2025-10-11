using System.Windows;
using Captura.Views;

namespace Captura
{
    public partial class AboutPage
    {
        void ViewLicenses(object Sender, RoutedEventArgs E)
        {
            NavigationService?.Navigate(new LicensesPage());
        }

        void ViewCrashLogs(object Sender, RoutedEventArgs E)
        {
            NavigationService?.Navigate(new CrashLogsPage());
        }
    }
}
