using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public static class MonitorHelper
    {
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        
        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }
        
        const uint MONITOR_DEFAULTTONULL = 0;
        const uint MONITOR_DEFAULTTOPRIMARY = 1;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        
        public static IntPtr GetMonitorFromRect(Rectangle rect)
        {
            var center = new POINT
            {
                X = rect.Left + rect.Width / 2,
                Y = rect.Top + rect.Height / 2
            };
            
            return MonitorFromPoint(center, MONITOR_DEFAULTTONEAREST);
        }
    }
}
