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

        // OpenImageEditor, OpenImageCropper, and UploadToImgur methods removed - features no longer supported

        void OpenAudioVideoTrimmer(object Sender, RoutedEventArgs E)
        {
            new TrimmerWindow().ShowAndFocus();
        }
    }
}
