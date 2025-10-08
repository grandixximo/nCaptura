using Captura.Audio;
using Captura.Models;
using System;
using Captura.Video;
using Captura.Webcam;
using Captura.Windows.Gdi;
using Captura.Windows.MediaFoundation;

namespace Captura.Windows
{
    public static class WindowsModule
    {
        public static void Load(IBinder Binder)
        {
            if (Windows8OrAbove)
            {
                try
                {
                    MfManager.Startup();
                    // MF provider now includes GPU detection and compatibility checks
                    // Will automatically disable on incompatible hardware (AMD integrated graphics)
                    Binder.BindAsInterfaceAndClass<IVideoWriterProvider, MfWriterProvider>();
                }
                catch
                {
                    // If MF fails to initialize, silently skip it
                    // User will only see FFmpeg and SharpAvi options
                }
            }

            Binder.BindSingleton<WindowsSettings>();
            Binder.Bind<IPlatformServices, WindowsPlatformServices>();
            Binder.Bind<IDialogService, DialogService>();
            Binder.Bind<IClipboardService, ClipboardService>();
            Binder.Bind<IImagingSystem, DrawingImagingSystem>();
            Binder.Bind<IWebCamProvider, WebcamProvider>();

            foreach (var audioItem in MfAudioItem.Items)
            {
                Binder.Bind<IAudioWriterItem>(() => audioItem);
            }
        }

        public static void Unload()
        {
            if (Windows8OrAbove)
            {
                try
                {
                    MfManager.Shutdown();
                }
                catch
                {
                    // Ignore shutdown errors
                }
            }
        }

        static bool Windows8OrAbove
        {
            get
            {
                var version = new Version(6, 2, 9200, 0);
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                       Environment.OSVersion.Version >= version;
            }
        }
    }
}