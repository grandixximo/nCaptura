using Captura.Windows.MediaFoundation;

namespace Captura.Windows
{
    public class WindowsSettings : PropertyStore
    {
        public bool UseGdi
        {
            get => Get(false);
            set => Set(value);
        }
        
        public bool UseWgc
        {
            get => Get(true);
            set => Set(value);
        }

        public MfSettings MediaFoundation { get; } = new MfSettings();
    }
}