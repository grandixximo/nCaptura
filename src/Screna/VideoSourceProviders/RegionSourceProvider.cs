using System.Drawing;
using Captura.Loc;
using System.ComponentModel;

namespace Captura.Video
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RegionSourceProvider : VideoSourceProviderBase
    {
        readonly IRegionProvider _regionProvider;
        readonly IVideoSourcePicker _videoSourcePicker;

        public RegionSourceProvider(ILocalizationProvider Loc,
            IRegionProvider RegionProvider,
            IVideoSourcePicker VideoSourcePicker,
            IIconSet Icons) : base(Loc)
        {
            _videoSourcePicker = VideoSourcePicker;
            _regionProvider = RegionProvider;

            Source = RegionProvider.VideoSource;
            Icon = Icons.Region;
            
            // Subscribe to property changes on the RegionItem to notify binding updates
            if (Source is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(IVideoItem.Name))
                    {
                        // Notify that Source has changed to force WPF to re-evaluate Source.Name binding
                        RaisePropertyChanged(nameof(Source));
                    }
                };
            }
        }

        public override IVideoItem Source { get; }

        public override string Name => Loc.Region;

        public override string Description { get; } = "Record region selected using Region Selector.";

        public override string Icon { get; }

        // Prevents opening multiple region pickers at the same time
        bool _picking;

        public override bool OnSelect()
        {
            if (_picking)
                return false;

            _picking = true;

            try
            {
                var wasVisible = _regionProvider.SelectorVisible;

                _regionProvider.SelectorVisible = false;

                var region = _videoSourcePicker.PickRegion();

                if (region == null)
                {
                    // Show again if was already visible
                    _regionProvider.SelectorVisible = wasVisible;

                    return false;
                }

                _regionProvider.SelectedRegion = region.Value;

                _regionProvider.SelectorVisible = true;
            }
            finally
            {
                _picking = false;
            }

            return true;
        }

        public override void OnUnselect()
        {
            _regionProvider.SelectorVisible = false;
        }

        public override string Serialize()
        {
            var rect = _regionProvider.SelectedRegion;
            return rect.ConvertToString();
        }

        public override bool Deserialize(string Serialized)
        {
            if (!(Serialized.ConvertToRectangle() is Rectangle rect))
                return false;

            _regionProvider.SelectedRegion = rect;

            _regionProvider.SelectorVisible = true;

            return true;
        }

        public override bool ParseCli(string Arg)
        {
            if (!(Arg.ConvertToRectangle() is Rectangle rect))
                return false;

            _regionProvider.SelectedRegion = rect.Even();

            return true;
        }

        public override IBitmapImage Capture(bool IncludeCursor)
        {
            return ScreenShot.Capture(_regionProvider.SelectedRegion, IncludeCursor);
        }
    }
}