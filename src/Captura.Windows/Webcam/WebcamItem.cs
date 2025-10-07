using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Captura.Models;

namespace Captura.Webcam
{
    class WebcamItem : IWebcamItem
    {
        public WebcamItem(Filter Cam)
        {
            this.Cam = Cam ?? throw new ArgumentNullException(nameof(Cam));
            Name = Cam.Name;
        }

        public Filter Cam { get; }

        public string Name { get; }

        public IWebcamCapture BeginCapture(Action OnClick)
        {
            try
            {
                return new WebcamCapture(Cam, OnClick);
            }
            catch (COMException ex)
            {
                ShowCameraAccessHelp(ex);
            }
            catch (Exception ex)
            {
                ShowCameraAccessHelp(ex);
            }

            return null;
        }

        public override string ToString() => Name;

        static void ShowCameraAccessHelp(Exception Exception)
        {
            // Try to provide an actionable message for common Windows privacy/permission blocks
            var header = "Unable to start camera preview";

            var message =
                "Captura could not access your camera. On Windows 10/11, check Settings → Privacy & security → Camera:\n\n" +
                "- Turn on 'Camera access'\n" +
                "- Turn on 'Let desktop apps access your camera'\n\n" +
                "Also close other apps that may be using the camera, then try again.";

            try
            {
                if (ServiceProvider.MessageProvider.ShowYesNo(message + "\n\nOpen Windows camera privacy settings now?", header))
                {
                    try { Process.Start("ms-settings:privacy-webcam"); }
                    catch { /* ignore */ }
                }
            }
            catch
            {
                // As a fallback if MessageProvider isn't available yet
            }
        }
    }
}