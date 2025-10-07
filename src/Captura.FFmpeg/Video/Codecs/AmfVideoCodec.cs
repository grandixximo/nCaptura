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
            // Using quality-balanced presets that work well for screen recording
            OutputArgs.AddArg("c:v", _fFmpegCodecName)
                .AddArg("quality", "balanced")           // Quality preset: speed, balanced, or quality
                .AddArg("rc", "vbr_latency")             // Rate control: VBR with low latency (good for recording)
                .AddArg("usage", "ultralowlatency")      // Usage mode optimized for ultra low latency
                .AddArg("profile:v", "high")             // Use high profile for better quality
                .AddArg("level", "auto");                // Auto-detect appropriate level
            
            // Preset mapping (similar to OBS):
            // - speed: fastest encoding, lower quality
            // - balanced: good balance between speed and quality
            // - quality: best quality, slower encoding
            // For screen recording, balanced is a good default
            OutputArgs.AddArg("preset", "balanced");
            
            // Optional: Enable pre-analysis for better quality (like OBS does)
            // This can improve quality at minimal performance cost
            OutputArgs.AddArg("preanalysis", "true");
            
            // Set GOP size for better seeking and compatibility
            // 250 is a good default for 60fps content (about 4 seconds)
            OutputArgs.AddArg("g", "250");
            
            // Optional: Set bitrate if specified in settings
            // AMF works best with VBR, so we set target bitrate
            // OutputArgs.AddArg("b:v", "5M");           // Can be customized based on user settings
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
