using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Captura.Models;
using Captura.Video;

namespace Captura.Webcam
{
    class WebcamCapture : IWebcamCapture
    {
        readonly Filter _filter;
        readonly Action _onClick;
        CaptureWebcam _captureWebcam;
        readonly SyncContextManager _syncContext = new SyncContextManager();
        bool _permissionWarned;

        public WebcamCapture(Filter Filter, Action OnClick)
        {
            _filter = Filter;
            _onClick = OnClick;
            _captureWebcam = new CaptureWebcam(Filter, OnClick, IntPtr.Zero);

            try
            {
                _captureWebcam.StartPreview();
            }
            catch (COMException ex)
            {
                HandlePreviewStartException(ex);
            }
            catch (Exception ex)
            {
                Captura.ServiceProvider.MessageProvider.ShowException(ex, "Failed to start webcam preview");
            }
        }

        public void Dispose()
        {
            _syncContext.Run(() =>
            {
                _captureWebcam.StopPreview();
                _captureWebcam.Dispose();
            });
        }

        public IBitmapImage Capture(IBitmapLoader BitmapLoader)
        {
            return _syncContext.Run(() => _captureWebcam.GetFrame(BitmapLoader));
        }

        public int Width => _captureWebcam.Size.Width;
        public int Height => _captureWebcam.Size.Height;

        IntPtr _lastWin;

        public void UpdatePreview(IWindow Window, Rectangle Location)
        {
            _syncContext.Run(() =>
            {
                if (Window != null && _lastWin != Window.Handle)
                {
                    Dispose();

                    _captureWebcam = new CaptureWebcam(_filter, _onClick, Window.Handle);

                    try
                    {
                        _captureWebcam.StartPreview();
                    }
                    catch (COMException ex)
                    {
                        HandlePreviewStartException(ex);
                    }
                    catch (Exception ex)
                    {
                        Captura.ServiceProvider.MessageProvider.ShowException(ex, "Failed to start webcam preview");
                    }

                    _lastWin = Window.Handle;
                }

                _captureWebcam.OnPreviewWindowResize(Location.X, Location.Y, Location.Width, Location.Height);
            });
        }

        void HandlePreviewStartException(COMException exception)
        {
            // E_ACCESSDENIED
            const uint accessDenied = 0x80070005u;

            if ((uint)exception.ErrorCode == accessDenied)
            {
                if (_permissionWarned)
                    return;

                _permissionWarned = true;

                var message =
                    "Windows has blocked camera access for desktop apps.\n\n" +
                    "To enable:\n" +
                    "1) Open Settings > Privacy & security > Camera\n" +
                    "2) Turn on 'Camera access' and 'Let apps access your camera'\n" +
                    "3) Scroll down and turn on 'Let desktop apps access your camera'\n\n" +
                    "After enabling, restart Captura and try again.\n\n" +
                    $"Error: 0x{accessDenied:X} (Access Denied)";

                Captura.ServiceProvider.MessageProvider.ShowError(message, "Webcam access blocked by Windows privacy");
            }
            else
            {
                Captura.ServiceProvider.MessageProvider.ShowException(exception, "Failed to start webcam preview");
            }
        }
    }
}