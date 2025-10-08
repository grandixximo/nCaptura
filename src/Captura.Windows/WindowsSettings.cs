using Captura.Windows.MediaFoundation;

namespace Captura.Windows
{
    public enum CaptureMethod
    {
        WindowsGraphicsCapture,
        DesktopDuplication,
        Gdi
    }
    
    public class WindowsSettings : PropertyStore
    {
        public CaptureMethod ScreenCaptureMethod
        {
            get => Get(CaptureMethod.WindowsGraphicsCapture);
            set => Set(value);
        }

        public MfSettings MediaFoundation { get; } = new MfSettings();
    }
}