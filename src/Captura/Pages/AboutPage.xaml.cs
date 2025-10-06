using System.Windows;
using Captura.Models;
using Captura.Views;
using Microsoft.Win32;

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

        void OpenImageEditor(object Sender, RoutedEventArgs E)
        {
            MessageBox.Show("Image Editor has been removed in this version.", "Feature Removed", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void OpenAudioVideoTrimmer(object Sender, RoutedEventArgs E)
        {
            new TrimmerWindow().ShowAndFocus();
        }

        void OpenImageCropper(object Sender, RoutedEventArgs E)
        {
            MessageBox.Show("Image Cropper has been removed in this version.", "Feature Removed", MessageBoxButton.OK, MessageBoxImage.Information);
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
