using Captura.Windows.MediaFoundation;

namespace Captura.Windows
{
    public class WindowsSettings : PropertyStore
    {
        public MfSettings MediaFoundation { get; } = new MfSettings();
    }
}