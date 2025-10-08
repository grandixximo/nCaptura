using System;
using System.Threading.Tasks;
using Captura.Video;

namespace Captura.FFmpeg
{
    // ReSharper disable once InconsistentNaming
    class FFmpegVideoConverter : IVideoConverter
    {
        readonly FFmpegVideoCodec _videoCodec;

        public FFmpegVideoConverter(FFmpegVideoCodec VideoCodec)
        {
            _videoCodec = VideoCodec;
        }

        public string Name => $"{_videoCodec.Name} (FFmpeg)";

        public string Extension => _videoCodec.Extension;

        public async Task StartAsync(VideoConverterArgs Args, IProgress<int> Progress)
        {
            if (!FFmpegService.FFmpegExists)
            {
                throw new FFmpegNotFoundException();
            }

            var argsBuilder = new FFmpegArgsBuilder();

            argsBuilder.AddInputFile(Args.InputFile);

            var output = argsBuilder.AddOutputFile(Args.FileName)
                .SetFrameRate(Args.FrameRate);

            _videoCodec.Apply(ServiceProvider.Get<FFmpegSettings>(), Args, output);

            //if (Args.AudioProvider != null)
            {
                _videoCodec.AudioArgsProvider(Args.AudioQuality, output);
            }

            var process = FFmpegService.StartFFmpeg(argsBuilder.GetArgs(), Args.FileName, out var log);

            log.ProgressChanged += Progress.Report;

            // Wait for process with timeout (5 minutes max for conversion)
            var exited = await Task.Run(() => process.WaitForExit(300000));

            if (!exited)
            {
                // Process didn't finish in time, kill it
                try
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
                catch
                {
                    // Process might have already exited
                }
                
                throw new FFmpegException(-1, new TimeoutException("Video conversion timed out after 5 minutes"));
            }

            if (process.ExitCode != 0)
                throw new FFmpegException(process.ExitCode);

            process.Dispose();
            Progress.Report(100);
        }
    }
}
