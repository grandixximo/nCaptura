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

        public MfCodecsViewModel(WindowsSettings WindowsSettings, IEnumerable<IVideoWriterProvider> WriterProviders)
        {
            this.Settings = WindowsSettings.MediaFoundation;
            _mfWriterProvider = WriterProviders.OfType<MfWriterProvider>().FirstOrDefault();
        }

        public IEnumerable<string> AvailableEncoders
        {
            get
            {
                if (_mfWriterProvider == null)
                    return Enumerable.Empty<string>();

                return _mfWriterProvider.GetAvailableEncoderNames();
            }
        }
    }
}