using Captura.Video;

namespace Captura.FFmpeg
{
    class FFmpegVideoWriterArgs : VideoWriterArgs
    {
        public static FFmpegVideoWriterArgs FromVideoWriterArgs(VideoWriterArgs Args, FFmpegVideoCodec VideoCodec)
        {
            var ffArgs = new FFmpegVideoWriterArgs
            {
                FileName = Args.FileName,
                ImageProvider = Args.ImageProvider,
                FrameRate = Args.FrameRate,
                VideoQuality = Args.VideoQuality,
                VideoCodec = VideoCodec,
                AudioQuality = Args.AudioQuality,
                AudioProvider = Args.AudioProvider
            };

            // Auto-configure audio pipe format to match provider
            if (Args.AudioProvider != null)
            {
                var wf = Args.AudioProvider.WaveFormat;
                ffArgs.Frequency = wf.SampleRate;
                ffArgs.Channels = wf.Channels;
            }

            return ffArgs;
        }

        public FFmpegVideoCodec VideoCodec { get; set; }
        public int Frequency { get; set; } = 44100;
        public int Channels { get; set; } = 2;
    }
}