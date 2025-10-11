using System.Windows;
using System.Windows.Controls;
using Captura.Views;

namespace Captura
{
    public partial class AboutPage
    {
        void ViewLicenses(object Sender, RoutedEventArgs E)
        {
            // Navigate using the immediate parent Frame (AboutFrame), not the grandparent
            var parentFrame = Parent as Frame;
            if (parentFrame != null)
            {
                parentFrame.Navigate(new LicensesPage());
            }
            else
            {
                NavigationService?.Navigate(new LicensesPage());
            }
        }

        void ViewCrashLogs(object Sender, RoutedEventArgs E)
        {
            // Navigate using the immediate parent Frame (AboutFrame), not the grandparent
            var parentFrame = Parent as Frame;
            if (parentFrame != null)
            {
                parentFrame.Navigate(new CrashLogsPage());
            }
            else
            {
                NavigationService?.Navigate(new CrashLogsPage());
            }
        }
    }
}
