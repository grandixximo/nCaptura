using System;

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
            catch (Exception ex)
            {
                // Show user-friendly error message
                var message = "Failed to start webcam.\n\n" +
                             "Possible solutions:\n" +
                             "• Check Windows camera privacy settings\n" +
                             "• Close other apps using the camera\n" +
                             "• Restart the camera or computer\n" +
                             "• Update camera drivers\n\n" +
                             $"Technical details: {ex.Message}";

                try
                {
                    ServiceProvider.MessageProvider?.ShowError(message, "Webcam Error");
                }
                catch
                {
                    // MessageProvider not available
                }

                return null;
            }
        }

        public override string ToString() => Name;
    }
}
