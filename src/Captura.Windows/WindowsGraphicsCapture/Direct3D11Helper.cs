using System;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using Windows.Graphics.DirectX.Direct3D11;

namespace Captura.Windows.WindowsGraphicsCapture
{
    public static class Direct3D11Helper
    {
        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        }
        
        public static Texture2D CreateSharpDXTexture2D(IDirect3DSurface surface)
        {
            var access = surface as IDirect3DDxgiInterfaceAccess;
            var d3dPointer = access.GetInterface(typeof(Texture2D).GUID);
            var texture = new Texture2D(d3dPointer);
            return texture;
        }
    }
}
