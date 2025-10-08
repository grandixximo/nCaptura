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
                // Log but don't show error dialog (to avoid dialog crashes)
                // The error will be handled by the caller
                System.Diagnostics.Debug.WriteLine($"Webcam capture failed: {ex.Message}");
                return null;
            }
        }

        public override string ToString() => Name;
    }
}
