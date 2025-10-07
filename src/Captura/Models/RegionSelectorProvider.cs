using System;
using System.Drawing;
using System.Windows;
using Captura.ViewModels;

namespace Captura.Video
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RegionSelectorProvider : IRegionProvider
    {
        readonly Lazy<RegionSelector> _regionSelector;
        readonly RegionSelectorViewModel _viewModel;

        public RegionSelectorProvider(RegionSelectorViewModel ViewModel,
            IPlatformServices PlatformServices,
            IVideoSourcePicker VideoSourcePicker)
        {
            _viewModel = ViewModel;

            _regionSelector = new Lazy<RegionSelector>(() => new RegionSelector(VideoSourcePicker));
        }

        public bool SelectorVisible
        {
            get => _regionSelector.Value.Visibility == Visibility.Visible;
            set
            {
                if (value)
                    _regionSelector.Value.Show();
                else _regionSelector.Value.Hide();
            }
        }

        public Rectangle SelectedRegion
        {
            get => _regionSelector.IsValueCreated ? _regionSelector.Value.SelectedRegion : _viewModel.SelectedRegion;
            set
            {
                System.Diagnostics.Debug.WriteLine($"[RegionSelectorProvider] SelectedRegion setter: {value}");
                _viewModel.SelectedRegion = value;
                
                // Also update the actual RegionSelector window if it's been created
                if (_regionSelector.IsValueCreated)
                {
                    System.Diagnostics.Debug.WriteLine($"[RegionSelectorProvider] Forwarding to RegionSelector window");
                    _regionSelector.Value.SelectedRegion = value;
                }
            }
        }

        public IVideoItem VideoSource
        {
            get
            {
                // Ensure RegionSelector is created so we have a RegionItem to return
                // This ensures there's only one RegionItem instance (the one in RegionSelector)
                return _regionSelector.Value.VideoSource;
            }
        }

        public IntPtr Handle => _regionSelector.Value.Handle;
    }
}
