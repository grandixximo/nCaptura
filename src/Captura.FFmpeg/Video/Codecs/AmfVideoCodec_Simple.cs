using Captura.Video;

namespace Captura.FFmpeg
{
    class AmfVideoCodec_Simple : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer).\n" +
                                  "Uses encoder defaults for maximum compatibility.";

        AmfVideoCodec_Simple(string Name, string FFmpegCodecName, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            OutputArgs.AddArg("c:v", _fFmpegCodecName);
        }

        public static AmfVideoCodec_Simple CreateH264()
        {
            return new AmfVideoCodec_Simple(
                "AMD AMF Simple: Mp4 (H.264, AAC)", 
                "h264_amf",
                "H.264 with AAC using AMD AMF (defaults)");
        }

        public static AmfVideoCodec_Simple CreateHevc()
        {
            return new AmfVideoCodec_Simple(
                "AMD AMF Simple: Mp4 (HEVC, AAC)", 
                "hevc_amf",
                "HEVC with AAC using AMD AMF (defaults)");
        }
    }
}
