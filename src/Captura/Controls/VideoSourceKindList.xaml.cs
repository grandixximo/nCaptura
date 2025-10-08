using System.Windows.Controls;
using System.Windows.Input;
using Captura.Models;
using Captura.Video;

namespace Captura
{
    public partial class VideoSourceKindList
    {
        public VideoSourceKindList()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList] Control loaded.");
                if (((ListView)this.Content).ItemsSource != null)
                {
                    var count = 0;
                    foreach (var item in ((ListView)this.Content).ItemsSource)
                    {
                        count++;
                        if (item is IVideoSourceProvider provider)
                        {
                            System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList] Item {count}: {provider.Name}");
                        }
                    }
                    System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList] Total items: {count}");
                }
            };
        }

        void OnVideoSourceReSelect(object Sender, MouseButtonEventArgs E)
        {
            if (Sender is ListViewItem item && item.IsSelected)
            {
                if (item.DataContext is IVideoSourceProvider provider)
                {
                    provider.OnSelect();
                }
            }
        }
    }
}
