using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public static class MonitorHelper
    {
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
        
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromRect(ref RECT lprc, uint dwFlags);
        
        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        const uint MONITOR_DEFAULTTONULL = 0;
        const uint MONITOR_DEFAULTTOPRIMARY = 1;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        
        public static IntPtr GetMonitorFromRect(Rectangle rect)
        {
            var nativeRect = new RECT
            {
                Left = rect.Left,
                Top = rect.Top,
                Right = rect.Right,
                Bottom = rect.Bottom
            };
            
            return MonitorFromRect(ref nativeRect, MONITOR_DEFAULTTONEAREST);
        }
    }
}
