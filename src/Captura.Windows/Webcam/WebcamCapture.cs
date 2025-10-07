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
        bool _disposed;

        public WebcamCapture(Filter Filter, Action OnClick)
        {
            _filter = Filter ?? throw new ArgumentNullException(nameof(Filter));
            _onClick = OnClick;

            try
            {
                _captureWebcam = new CaptureWebcam(Filter, OnClick, IntPtr.Zero);
                _captureWebcam.StartPreview();
            }
            catch (COMException ex)
            {
                HandleCameraException(ex, "Failed to start webcam");
                throw;
            }
            catch (Exception ex)
            {
                HandleCameraException(ex, "Failed to initialize webcam");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _syncContext.Run(() =>
            {
                try
                {
                    _captureWebcam?.StopPreview();
                    _captureWebcam?.Dispose();
                    _captureWebcam = null;
                }
                catch
                {
                    // Ignore disposal errors
                }
            });
        }

        public IBitmapImage Capture(IBitmapLoader BitmapLoader)
        {
            if (_disposed)
                return null;

            return _syncContext.Run(() =>
            {
                try
                {
                    return _captureWebcam?.GetFrame(BitmapLoader);
                }
                catch
                {
                    return null;
                }
            });
        }

        public int Width => _captureWebcam?.Size.Width ?? 0;
        public int Height => _captureWebcam?.Size.Height ?? 0;

        IntPtr _lastWin;

        public void UpdatePreview(IWindow Window, Rectangle Location)
        {
            if (_disposed)
                return;

            _syncContext.Run(() =>
            {
                try
                {
                    if (Window != null && _lastWin != Window.Handle)
                    {
                        // Recreate capture with new window handle
                        _captureWebcam?.StopPreview();
                        _captureWebcam?.Dispose();

                        _captureWebcam = new CaptureWebcam(_filter, _onClick, Window.Handle);
                        _captureWebcam.StartPreview();

                        _lastWin = Window.Handle;
                    }

                    _captureWebcam?.OnPreviewWindowResize(Location.X, Location.Y, Location.Width, Location.Height);
                }
                catch (COMException ex)
                {
                    HandleCameraException(ex, "Failed to update preview");
                }
                catch (Exception ex)
                {
                    HandleCameraException(ex, "Failed to update preview");
                }
            });
        }

        void HandleCameraException(Exception exception, string context)
        {
            const uint E_ACCESSDENIED = 0x80070005;
            const uint MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED = 0xC00D3E86;
            const uint MF_E_ATTRIBUTENOTFOUND = 0xC00D36E6;

            var comException = exception as COMException;
            var errorCode = (uint)(comException?.ErrorCode ?? 0);

            string message;
            string title;

            if (errorCode == E_ACCESSDENIED)
            {
                title = "Camera Access Denied";
                message = "Windows has blocked camera access.\n\n" +
                         "To fix this:\n" +
                         "1. Open Settings → Privacy & security → Camera\n" +
                         "2. Enable 'Camera access'\n" +
                         "3. Enable 'Let apps access your camera'\n" +
                         "4. Enable 'Let desktop apps access your camera'\n\n" +
                         "Then restart this application.";
            }
            else if (errorCode == MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED)
            {
                title = "Camera Not Available";
                message = "The camera is being used by another application.\n\n" +
                         "Please close other apps using the camera and try again.";
            }
            else if (errorCode == MF_E_ATTRIBUTENOTFOUND)
            {
                title = "Camera Configuration Error";
                message = "Could not configure camera format.\n\n" +
                         "The camera may not be properly installed or may be incompatible.";
            }
            else
            {
                title = $"Camera Error";
                message = $"{context}\n\n" +
                         $"Error: {exception.Message}\n" +
                         (errorCode != 0 ? $"Code: 0x{errorCode:X8}" : "");
            }

            try
            {
                Captura.ServiceProvider.MessageProvider?.ShowError(message, title);
            }
            catch
            {
                // MessageProvider might not be available
            }
        }
    }
}
