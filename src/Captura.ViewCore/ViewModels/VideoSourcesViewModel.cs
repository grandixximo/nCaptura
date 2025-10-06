using System.Collections.Generic;
using System.Linq;
using Captura.Video;

namespace Captura.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class VideoSourcesViewModel : NotifyPropertyChanged
    {
        readonly FullScreenSourceProvider _fullScreenProvider;
        readonly Settings _settings;
        public NoVideoSourceProvider NoVideoSourceProvider { get; }

        public IEnumerable<IVideoSourceProvider> VideoSources { get; }

        public VideoSourcesViewModel(FullScreenSourceProvider FullScreenProvider,
            NoVideoSourceProvider NoVideoSourceProvider,
            IEnumerable<IVideoSourceProvider> SourceProviders,
            Settings Settings)
        {
            this.NoVideoSourceProvider = NoVideoSourceProvider;
            _fullScreenProvider = FullScreenProvider;
            _settings = Settings;
            VideoSources = SourceProviders;

            // DEBUG LOGGING
            var sourcesList = SourceProviders.ToList();
            System.Diagnostics.Debug.WriteLine($"[VideoSources] Total sources: {sourcesList.Count}");
            foreach (var source in sourcesList)
            {
                System.Diagnostics.Debug.WriteLine($"[VideoSources]   - {source.GetType().Name}: {source.Name}");
            }

            SetDefaultSource();
        }

        public void SetDefaultSource()
        {
            SelectedVideoSourceKind = _fullScreenProvider;
        }

        void ChangeSource(IVideoSourceProvider NewSourceProvider, bool CallOnSelect)
        {
            try
            {
                if (NewSourceProvider == null || _videoSourceKind == NewSourceProvider)
                    return;

                // Doesn't support Steps mode
                if (_settings.Video.RecorderMode == RecorderMode.Steps && !NewSourceProvider.SupportsStepsMode)
                    return;

                if (CallOnSelect && !NewSourceProvider.OnSelect())
                {
                    return;
                }

                _videoSourceKind?.OnUnselect();

                _videoSourceKind = NewSourceProvider;
            }
            finally
            {
                // Delay parameter needs to be used with Binding for handling cancellation
                RaisePropertyChanged(nameof(SelectedVideoSourceKind));
            }
        }

        IVideoSourceProvider _videoSourceKind;

        public IVideoSourceProvider SelectedVideoSourceKind
        {
            get => _videoSourceKind;
            set => ChangeSource(value, true);
        }

        public void RestoreSourceKind(IVideoSourceProvider SourceProvider)
        {
            ChangeSource(SourceProvider, false);
        }
    }
}