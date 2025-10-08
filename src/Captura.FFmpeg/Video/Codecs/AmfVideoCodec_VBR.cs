using Captura.Video;

namespace Captura.FFmpeg
{
    /// <summary>
    /// AMD AMF hardware encoder using VBR (Variable Bitrate) rate control.
    /// Alternative implementation if CQP mode doesn't work.
    /// </summary>
    class AmfVideoCodec_VBR : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;
        readonly string _bitrate;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer, or APU with GCN architecture).\n" +
                                  "Inspired by OBS Studio's AMD hardware encoding implementation.\n" +
                                  "If this doesn't work, ensure your AMD drivers are up to date and FFmpeg has AMF support compiled in.";

        AmfVideoCodec_VBR(string Name, string FFmpegCodecName, string Bitrate, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
            _bitrate = Bitrate;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            // AMF encoder using VBR (Variable Bitrate) mode
            // This mode is more compatible across different FFmpeg builds
            
            OutputArgs.AddArg("c:v", _fFmpegCodecName)
                .AddArg("b:v", _bitrate);      // Target bitrate
            
            // Note: We're using the simplest possible settings for maximum compatibility
            // The encoder will use its defaults for everything else
        }

        /// <summary>
        /// Create H.264 AMF encoder with VBR at 5 Mbps.
        /// </summary>
        public static AmfVideoCodec_VBR CreateH264()
        {
            return new AmfVideoCodec_VBR(
                "AMD AMF VBR: Mp4 (H.264, AAC)", 
                "h264_amf", 
                "5M",
                "Encode to Mp4: H.264 with AAC audio using AMD AMF (VBR mode at 5 Mbps)");
        }

        /// <summary>
        /// Create HEVC AMF encoder with VBR at 4 Mbps (HEVC compresses better).
        /// </summary>
        public static AmfVideoCodec_VBR CreateHevc()
        {
            return new AmfVideoCodec_VBR(
                "AMD AMF VBR: Mp4 (HEVC, AAC)", 
                "hevc_amf", 
                "4M",
                "Encode to Mp4: HEVC (H.265) with AAC audio using AMD AMF (VBR mode at 4 Mbps)");
        }
    }
}
