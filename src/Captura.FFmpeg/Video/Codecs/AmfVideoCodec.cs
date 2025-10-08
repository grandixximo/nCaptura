using Captura.Video;

namespace Captura.FFmpeg
{
    class AmfVideoCodec : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer).\n" +
                                  "Ensure AMD drivers are up to date.";

        AmfVideoCodec(string Name, string FFmpegCodecName, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            OutputArgs.AddArg("c:v", _fFmpegCodecName)
                .AddArg("rc", "cqp")
                .AddArg("qp", "22");
        }

        public static AmfVideoCodec CreateH264()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (H.264, AAC)", 
                "h264_amf", 
                "H.264 with AAC audio using AMD AMF hardware encoding");
        }

        public static AmfVideoCodec CreateHevc()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (HEVC, AAC)", 
                "hevc_amf", 
                "HEVC (H.265) with AAC audio using AMD AMF hardware encoding");
        }

        public static AmfVideoCodec CreateAv1()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (AV1, AAC)", 
                "av1_amf", 
                "AV1 with AAC audio using AMD AMF (requires RDNA 2+ / RX 6000+)");
        }
    }
}
