using System;
using Captura.Video;
using SharpDX.Direct3D11;

namespace Captura.Windows.MediaFoundation
{
    public class MfItem : IVideoWriterItem
    {
        readonly Device _device;
        readonly string _warning;
        readonly string _codecName;
        readonly Guid _formatGuid;
        readonly string _extension;

        public string Extension => _extension;
        
        public string Description
        {
            get
            {
                var baseDesc = $"{_codecName} Hardware encoder (MP4 + AAC audio)";
                return string.IsNullOrEmpty(_warning) ? baseDesc : $"{baseDesc} - {_warning}";
            }
        }

        public MfItem(Device Device, string CodecName, Guid FormatGuid, string Extension, string Warning = null)
        {
            _device = Device;
            _codecName = CodecName;
            _formatGuid = FormatGuid;
            _extension = Extension;
            _warning = Warning;
        }

        public override string ToString() => $"{_codecName} (Hardware)";

        public virtual IVideoFileWriter GetVideoFileWriter(VideoWriterArgs Args)
        {
            return new MfWriter(Args, _device, _formatGuid);
        }
    }
}