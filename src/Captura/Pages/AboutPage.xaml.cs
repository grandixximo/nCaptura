using System.Windows;
using Captura.Views;
using Microsoft.Win32;

namespace Captura
{
    public partial class AboutPage
    {
        void ViewLicenses(object Sender, RoutedEventArgs E)
        {
            // Modern way - navigate to page instead of window
            NavigationService?.Navigate(new System.Uri("/Pages/LicensesPage.xaml", System.UriKind.Relative));
        }

        void ViewCrashLogs(object Sender, RoutedEventArgs E)
        {
            // Modern way - navigate to page instead of window
            NavigationService?.Navigate(new System.Uri("/Pages/CrashLogsPage.xaml", System.UriKind.Relative));
        }

        void OpenImageEditor(object Sender, RoutedEventArgs E)
        {
            // Image editor functionality removed in modern version
            // Could be re-added or use external editor
            System.Windows.MessageBox.Show("Image Editor has been replaced with External Editor option in settings.", "Info");
        }

        void OpenAudioVideoTrimmer(object Sender, RoutedEventArgs E)
        {
            new TrimmerWindow().ShowAndFocus();
        }

        void OpenImageCropper(object Sender, RoutedEventArgs E)
        {
            // Image cropper functionality removed in modern version
            System.Windows.MessageBox.Show("Image Cropper functionality has been replaced with External Editor option in settings.", "Info");
        }

        async void UploadToImgur(object Sender, RoutedEventArgs E)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.wmp;*.tiff",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (ofd.ShowDialog().GetValueOrDefault())
            {
                var imgSystem = ServiceProvider.Get<IImagingSystem>();

                using var img = imgSystem.LoadBitmap(ofd.FileName);
                await img.UploadImage();
            }
        }
    }
}
