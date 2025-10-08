using Captura.Windows.MediaFoundation;
using System.Collections.Generic;

namespace Captura.Windows
{
    public enum CaptureMethod
    {
        WindowsGraphicsCapture = 0,
        DesktopDuplication = 1,
        Gdi = 2
    }
    
    public class CaptureMethodItem
    {
        public CaptureMethod Value { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        
        public override string ToString() => DisplayName;
    }
    
    public class WindowsSettings : PropertyStore
    {
        public CaptureMethod ScreenCaptureMethod
        {
            get => Get(CaptureMethod.WindowsGraphicsCapture);
            set => Set(value);
        }
        
        public static IEnumerable<CaptureMethodItem> AvailablCaptureMethods { get; } = new[]
        {
            new CaptureMethodItem
            {
                Value = CaptureMethod.WindowsGraphicsCapture,
                DisplayName = "Windows Graphics Capture (WGC) - Recommended",
                Description = "Modern API (Windows 10 1903+). Best performance, especially for AMD hardware."
            },
            new CaptureMethodItem
            {
                Value = CaptureMethod.DesktopDuplication,
                DisplayName = "Desktop Duplication - Legacy",
                Description = "Older API (Windows 8+). May have issues on AMD hardware."
            },
            new CaptureMethodItem
            {
                Value = CaptureMethod.Gdi,
                DisplayName = "GDI - Slowest",
                Description = "WARNING: Software capture. Very slow, causes frame drops."
            }
        };

        public MfSettings MediaFoundation { get; } = new MfSettings();
    }
}