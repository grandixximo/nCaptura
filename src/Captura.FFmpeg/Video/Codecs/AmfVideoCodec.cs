using Captura.Video;

namespace Captura.FFmpeg
{
    /// <summary>
    /// AMD Advanced Media Framework (AMF) hardware encoder support.
    /// Inspired by OBS Studio's implementation of AMD hardware encoding.
    /// </summary>
    class AmfVideoCodec : FFmpegVideoCodec
    {
        readonly string _fFmpegCodecName;

        const string AmfSupport = "Requires AMD GPU with AMF support (Radeon HD 7000 series or newer, or APU with GCN architecture).\n" +
                                  "Inspired by OBS Studio's AMD hardware encoding implementation.\n" +
                                  "If this doesn't work, ensure your AMD drivers are up to date and AMF is supported by your GPU.";

        AmfVideoCodec(string Name, string FFmpegCodecName, string Description)
            : base(Name, ".mp4", $"{Description}\n{AmfSupport}")
        {
            _fFmpegCodecName = FFmpegCodecName;
        }

        public override FFmpegAudioArgsProvider AudioArgsProvider => FFmpegAudioItem.Aac;

        public override void Apply(FFmpegSettings Settings, VideoWriterArgs WriterArgs, FFmpegOutputArgs OutputArgs)
        {
            // AMF encoder settings inspired by OBS Studio's implementation
            // Using minimal, reliable settings similar to NVENC
            
            OutputArgs.AddArg("c:v", _fFmpegCodecName)
                .AddArg("rc", "cqp")         // Rate control: Constant QP (most reliable)
                .AddArg("qp", "22");         // Quality: 18-24 is good for screen recording
            
            // Alternative settings for better compatibility if CQP doesn't work:
            // Use VBR with bitrate target instead:
            // .AddArg("rc", "vbr_latency")
            // .AddArg("b:v", "5M");
        }

        /// <summary>
        /// Create H.264 AMF encoder instance.
        /// H.264/AVC is widely supported and provides excellent compatibility.
        /// </summary>
        public static AmfVideoCodec CreateH264()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (H.264, AAC)", 
                "h264_amf", 
                "Encode to Mp4: H.264 with AAC audio using AMD Advanced Media Framework (AMF) hardware encoding");
        }

        /// <summary>
        /// Create HEVC/H.265 AMF encoder instance.
        /// HEVC provides better compression but requires more modern hardware.
        /// </summary>
        public static AmfVideoCodec CreateHevc()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (HEVC, AAC)", 
                "hevc_amf", 
                "Encode to Mp4: HEVC (H.265) with AAC audio using AMD Advanced Media Framework (AMF) hardware encoding");
        }

        /// <summary>
        /// Create AV1 AMF encoder instance.
        /// AV1 is the newest codec with best compression, but requires latest AMD GPUs (RDNA 2+).
        /// </summary>
        public static AmfVideoCodec CreateAv1()
        {
            return new AmfVideoCodec(
                "AMD AMF: Mp4 (AV1, AAC)", 
                "av1_amf", 
                "Encode to Mp4: AV1 with AAC audio using AMD Advanced Media Framework (AMF) hardware encoding.\n" +
                "Requires AMD RDNA 2 or newer (RX 6000 series or later)");
        }
    }
}
