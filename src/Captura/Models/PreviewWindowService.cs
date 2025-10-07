using System;
using System.Windows.Media.Imaging;

namespace Captura.Video
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PreviewWindowService : IPreviewWindow
    {
        readonly PreviewWindow _previewWindow;

        public void Show()
        {
            // Show the actual PreviewWindow for classic UI
            _previewWindow.ShowAndFocus();
        }

        public bool IsVisible => _previewWindow.IsVisible;

        public PreviewWindowService()
        {
            // Use singleton PreviewWindow instance
            _previewWindow = PreviewWindow.Instance;
        }

        public void Display(IBitmapFrame Frame)
        {
            if (Frame is RepeatFrame)
            {
                Frame.Dispose();
                return;
            }

            if (!IsVisible)
            {
                Frame.Dispose();
                return;
            }

            try
            {
                // Render frame to the Image control
                _previewWindow.Dispatcher.Invoke(() =>
                {
                    var bitmap = new WriteableBitmap(Frame.Width, Frame.Height, 96, 96,
                        System.Windows.Media.PixelFormats.Bgr32, null);

                    bitmap.Lock();
                    try
                    {
                        Frame.CopyTo(bitmap.BackBuffer, bitmap.BackBufferStride);
                        bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, Frame.Width, Frame.Height));
                    }
                    finally
                    {
                        bitmap.Unlock();
                    }

                    _previewWindow.UpdateImage(bitmap);
                });
            }
            catch
            {
                // Ignore preview errors
            }
            finally
            {
                Frame.Dispose();
            }
        }

        public void Dispose()
        {
            // PreviewWindow is singleton, don't close it
        }
    }
}
