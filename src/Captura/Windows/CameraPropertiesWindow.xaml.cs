using System.Windows;

namespace Captura
{
    public partial class CameraPropertiesWindow : Window
    {
        public CameraPropertiesWindow(string properties)
        {
            InitializeComponent();
            
            PropertiesTextBox.Text = properties;
        }
        
        void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(PropertiesTextBox.Text);
            }
            catch
            {
                // Silently fail - user will notice if paste doesn't work
            }
        }
        
        void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}