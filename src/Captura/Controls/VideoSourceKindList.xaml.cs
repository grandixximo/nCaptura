using System.Linq;
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
                try
                {
                    var vm = ServiceProvider.Get<ViewModels.VideoSourcesViewModel>();
                    var sources = vm.VideoSources.ToList();
                    System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList] Loaded with {sources.Count} sources");
                    foreach (var source in sources)
                    {
                        System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList]   - {source.Name} ({source.GetType().Name})");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[VideoSourceKindList] Error: {ex.Message}");
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
