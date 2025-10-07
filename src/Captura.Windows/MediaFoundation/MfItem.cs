using Captura.Video;
using SharpDX.Direct3D11;

namespace Captura.Windows.MediaFoundation
{
    public class MfItem : IVideoWriterItem
    {
        readonly Device _device;
        readonly string _warning;

        public string Extension => ".mp4";
        
        public string Description
        {
            get
            {
                var baseDesc = "H.264 Hardware encoder (MP4 + AAC audio)";
                return string.IsNullOrEmpty(_warning) ? baseDesc : $"{baseDesc} - {_warning}";
            }
        }

        readonly string _name = "H.264 (Hardware)";

        public MfItem(Device Device, string Warning = null)
        {
            _device = Device;
            _warning = Warning;
        }

        public override string ToString() => _name;

        public virtual IVideoFileWriter GetVideoFileWriter(VideoWriterArgs Args)
        {
            return new MfWriter(Args, _device);
        }
    }
}