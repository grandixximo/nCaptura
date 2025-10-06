using System;
using System.Collections.Generic;
using System.Drawing;

namespace Captura.Video
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class VideoSourcePicker : IVideoSourcePicker
    {
        public IWindow PickWindow(Predicate<IWindow> Filter = null)
        {
            // Convert Predicate to list of skipped window handles
            // Modern API uses IEnumerable<IntPtr> instead of Predicate
            // For now, just pass null (no skip windows)
            return VideoSourcePickerWindow.PickWindow((IEnumerable<IntPtr>)null);
        }

        public IScreen PickScreen()
        {
            return ScreenPickerWindow.PickScreen();
        }

        public Rectangle? PickRegion()
        {
            return RegionPickerWindow.PickRegion();
        }
    }
}