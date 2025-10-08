using Captura.Video;

namespace Captura.FFmpeg
{
    /// <summary>
    /// AMD AMF hardware encoder using default settings only.
    /// Most compatible version - uses encoder defaults for everything.
    /// </summary>
    class AmfVideoCodec_Simple : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer, or APU with GCN architecture).\n" +
                                  "Uses default encoder settings for maximum compatibility.\n" +
                                  "If this doesn't work, your FFmpeg may not have AMF support compiled in.";

        AmfVideoCodec_Simple(string Name, string FFmpegCodecName, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            // Absolute minimum settings - just specify the codec
            // Let FFmpeg use all default values for the AMF encoder
            OutputArgs.AddArg("c:v", _fFmpegCodecName);
            
            // No other parameters - maximum compatibility
            // The encoder will use reasonable defaults
        }

        /// <summary>
        /// Create H.264 AMF encoder with default settings.
        /// </summary>
        public static AmfVideoCodec_Simple CreateH264()
        {
            return new AmfVideoCodec_Simple(
                "AMD AMF Simple: Mp4 (H.264, AAC)", 
                "h264_amf",
                "Encode to Mp4: H.264 with AAC audio using AMD AMF (default settings)");
        }

        /// <summary>
        /// Create HEVC AMF encoder with default settings.
        /// </summary>
        public static AmfVideoCodec_Simple CreateHevc()
        {
            return new AmfVideoCodec_Simple(
                "AMD AMF Simple: Mp4 (HEVC, AAC)", 
                "hevc_amf",
                "Encode to Mp4: HEVC (H.265) with AAC audio using AMD AMF (default settings)");
        }
    }
}
