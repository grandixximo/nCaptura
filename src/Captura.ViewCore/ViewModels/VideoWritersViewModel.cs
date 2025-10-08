using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using Captura.SharpAvi;
using Captura.Video;
using Captura.Windows;
using Captura.Windows.MediaFoundation;

namespace Captura.ViewModels
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class VideoWritersViewModel : NotifyPropertyChanged
    {
        public IReadOnlyList<IVideoWriterProvider> VideoWriterProviders { get; }
        readonly ObservableCollection<IVideoWriterItem> _videoWriters = new ObservableCollection<IVideoWriterItem>();
        public ReadOnlyObservableCollection<IVideoWriterItem> AvailableVideoWriters { get; }

        public IEnumerable<IVideoConverter> AvailablePostWriters { get; }

        public VideoWritersViewModel(IEnumerable<IVideoWriterProvider> WriterProviders,
            IEnumerable<IVideoConverter> PostWriters,
            SharpAviWriterProvider SharpAviWriterProvider)
        {
            VideoWriterProviders = WriterProviders.ToList();

            AvailableVideoWriters = new ReadOnlyObservableCollection<IVideoWriterItem>(_videoWriters);

            AvailablePostWriters = PostWriters;
            SelectedPostWriter = PostWriters.FirstOrDefault();

            if (VideoWriterProviders.Count > 0)
                SelectedVideoWriterKind = VideoWriterProviders[0];

            AvailableStepWriters = new IVideoWriterItem[]
            {
                new StepsVideoWriterItem(SharpAviWriterProvider.First()),
                new ImageFolderWriterItem()
            };

            SelectedStepsWriter = AvailableStepWriters[0];
        }

        public void RefreshCodecs()
        {
            // Remember selected codec
            var lastVideoCodecName = SelectedVideoWriter?.ToString();

            _videoWriters.Clear();

            foreach (var writerItem in SelectedVideoWriterKind)
            {
                _videoWriters.Add(writerItem);
            }

            // If the selected provider yields no codecs, fall back to the first provider that has any
            if (_videoWriters.Count == 0)
            {
                var fallbackProvider = VideoWriterProviders.FirstOrDefault(p => p.Any());
                if (fallbackProvider != null && !ReferenceEquals(fallbackProvider, SelectedVideoWriterKind))
                {
                    SelectedVideoWriterKind = fallbackProvider;
                    return;
                }
            }

            // Set First
            if (_videoWriters.Count > 0)
                SelectedVideoWriter = _videoWriters[0];

            // Prefer MF encoder selection from settings if MF is active
            if (SelectedVideoWriterKind is MfWriterProvider)
            {
                var mfSettings = ServiceProvider.Get<WindowsSettings>()?.MediaFoundation;
                var selectedByMfSetting = mfSettings == null
                    ? null
                    : AvailableVideoWriters.FirstOrDefault(M => M.ToString() == mfSettings.SelectedEncoder);

                if (selectedByMfSetting != null)
                {
                    SelectedVideoWriter = selectedByMfSetting;
                    return;
                }
            }

            // Otherwise, try to restore previous selection by name
            var matchingVideoCodec = AvailableVideoWriters.FirstOrDefault(M => M.ToString() == lastVideoCodecName);

            if (matchingVideoCodec != null)
            {
                SelectedVideoWriter = matchingVideoCodec;
            }
        }

        IVideoWriterProvider _writerKind;

        public IVideoWriterProvider SelectedVideoWriterKind
        {
            get => _writerKind;
            set
            {
                if (_writerKind == value)
                    return;

                if (value != null)
                    _writerKind = value;

                OnPropertyChanged();

                RefreshCodecs();
            }
        }

        IVideoWriterItem _writer;

        public IVideoWriterItem SelectedVideoWriter
        {
            get => _writer;
            set
            {
                var newValue = value ?? AvailableVideoWriters.FirstOrDefault();
                if (Set(ref _writer, newValue))
                {
                    // Keep MF encoder picker and writer selection in sync
                    if (SelectedVideoWriterKind is MfWriterProvider)
                    {
                        var mfSettings = ServiceProvider.Get<WindowsSettings>()?.MediaFoundation;
                        if (mfSettings != null)
                        {
                            mfSettings.SelectedEncoder = _writer?.ToString();
                        }
                    }
                }
            }
        }

        IVideoConverter _postWriter;

        public IVideoConverter SelectedPostWriter
        {
            get => _postWriter;
            set => Set(ref _postWriter, value ?? AvailablePostWriters.FirstOrDefault());
        }

        public IReadOnlyList<IVideoWriterItem> AvailableStepWriters { get; }

        IVideoWriterItem _stepsWriter;

        public IVideoWriterItem SelectedStepsWriter
        {
            get => _stepsWriter;
            set => Set(ref _stepsWriter, value ?? AvailableStepWriters[0]);
        }

        public IEnumerable<RecorderMode> AvailableRecorderModes { get; } = Enum
            .GetValues(typeof(RecorderMode))
            .Cast<RecorderMode>();
    }
}