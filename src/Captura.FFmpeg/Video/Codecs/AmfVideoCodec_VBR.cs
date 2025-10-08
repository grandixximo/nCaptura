using Captura.Video;

namespace Captura.FFmpeg
{
    class AmfVideoCodec_VBR : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;
        readonly string _bitrate;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer).";

        AmfVideoCodec_VBR(string Name, string FFmpegCodecName, string Bitrate, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
            _bitrate = Bitrate;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            OutputArgs.AddArg("c:v", _fFmpegCodecName)
                .AddArg("b:v", _bitrate);
        }

        public static AmfVideoCodec_VBR CreateH264()
        {
            return new AmfVideoCodec_VBR(
                "AMD AMF VBR: Mp4 (H.264, AAC)", 
                "h264_amf", 
                "5M",
                "H.264 with AAC using AMD AMF (VBR, 5 Mbps)");
        }

        public static AmfVideoCodec_VBR CreateHevc()
        {
            return new AmfVideoCodec_VBR(
                "AMD AMF VBR: Mp4 (HEVC, AAC)", 
                "hevc_amf", 
                "4M",
                "HEVC with AAC using AMD AMF (VBR, 4 Mbps)");
        }
    }
}
