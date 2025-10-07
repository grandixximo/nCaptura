using System.Collections.Generic;
using System.Linq;
using Captura.Video;
using Captura.Windows;
using Captura.Windows.MediaFoundation;

namespace Captura.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class MfCodecsViewModel : NotifyPropertyChanged
    {
        public MfSettings Settings { get; }

        readonly MfWriterProvider _mfWriterProvider;
        readonly VideoWritersViewModel _videoWritersViewModel;

        public MfCodecsViewModel(WindowsSettings WindowsSettings, 
            IEnumerable<IVideoWriterProvider> WriterProviders,
            VideoWritersViewModel VideoWritersViewModel)
        {
            this.Settings = WindowsSettings.MediaFoundation;
            _mfWriterProvider = WriterProviders.OfType<MfWriterProvider>().FirstOrDefault();
            _videoWritersViewModel = VideoWritersViewModel;
            
            // When encoder selection changes, refresh the available video writers
            Settings.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Settings.SelectedEncoder))
                {
                    _videoWritersViewModel?.RefreshCodecs();
                }
            };
        }

        public IEnumerable<string> AvailableEncoders
        {
            get
            {
                if (_mfWriterProvider == null)
                {
                    // Fallback if MF provider isn't available
                    return new[] { "H.264" };
                }

                var encoders = _mfWriterProvider.GetAvailableEncoderNames();
                
                // Ensure we always return at least H.264
                if (encoders == null || !encoders.Any())
                {
                    return new[] { "H.264" };
                }
                
                return encoders;
            }
        }
    }
}