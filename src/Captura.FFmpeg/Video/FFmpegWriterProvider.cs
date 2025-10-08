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

        public FFmpegWriterProvider(FFmpegSettings Settings)
        {
            _settings = Settings;
        }

        public IEnumerator<IVideoWriterItem> GetEnumerator()
        {
            yield return new X264VideoCodec();
            yield return new XvidVideoCodec();

            // Hardware encoders
            // AMD AMF (inspired by OBS Studio) - Multiple options for compatibility
            // Try these in order: Simple -> CQP -> VBR
            yield return AmfVideoCodec_Simple.CreateH264();  // Most compatible - try this first
            yield return AmfVideoCodec_Simple.CreateHevc();
            yield return AmfVideoCodec.CreateH264();         // CQP mode - better quality control
            yield return AmfVideoCodec.CreateHevc();
            yield return AmfVideoCodec_VBR.CreateH264();     // VBR mode - fixed bitrate
            yield return AmfVideoCodec_VBR.CreateHevc();
            
            // Intel QuickSync
            yield return new QsvHevcVideoCodec();
            
            // NVIDIA NVENC
            yield return NvencVideoCodec.CreateH264();
            yield return NvencVideoCodec.CreateHevc();

            // Custom
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