using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Captura.Video;

namespace Captura.FFmpeg
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class FFmpegWriterProvider : IVideoWriterProvider
    {
        public string Name => "FFmpeg";

        readonly FFmpegSettings _settings;
        readonly IHardwareInfoService _hardwareInfo;

        public FFmpegWriterProvider(FFmpegSettings Settings, IHardwareInfoService HardwareInfo)
        {
            _settings = Settings;
            _hardwareInfo = HardwareInfo;
        }

        public IEnumerator<IVideoWriterItem> GetEnumerator()
        {
            // Always show CPU-based encoders
            yield return new X264VideoCodec();
            yield return new XvidVideoCodec();

            // AMD AMF - only show if AMD GPU detected
            if (_hardwareInfo.HasAmdEncoder)
            {
                yield return AmfVideoCodec_Simple.CreateH264();
                yield return AmfVideoCodec_Simple.CreateHevc();
                yield return AmfVideoCodec.CreateH264();
                yield return AmfVideoCodec.CreateHevc();
                yield return AmfVideoCodec_VBR.CreateH264();
                yield return AmfVideoCodec_VBR.CreateHevc();
            }
            
            // Intel QuickSync - only show if Intel GPU detected
            if (_hardwareInfo.HasIntelQuickSync)
            {
                yield return new QsvHevcVideoCodec();
            }
            
            // NVIDIA NVENC - only show if NVIDIA GPU detected
            if (_hardwareInfo.HasNvidiaEncoder)
            {
                yield return NvencVideoCodec.CreateH264();
                yield return NvencVideoCodec.CreateHevc();
            }

            // Custom - always show user's custom codecs
            foreach (var item in _settings.CustomCodecs)
            {
                yield return new CustomFFmpegVideoCodec(item);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Name;

        public IVideoWriterItem ParseCli(string Cli)
        {
            var ffmpegExists = FFmpegService.FFmpegExists;

            if (ffmpegExists && Regex.IsMatch(Cli, @"^ffmpeg:\d+$"))
            {
                var index = int.Parse(Cli.Substring(7));

                var writers = this.ToArray();

                if (index < writers.Length)
                {
                    return writers[index];
                }
            }

            return null;
        }

        public string Description => @"Use FFmpeg for encoding.
Requires ffmpeg.exe, if not found option for downloading or specifying path is shown.";
    }
}