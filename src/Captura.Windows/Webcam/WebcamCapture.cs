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
            catch (Exception ex)
            {
                // Don't show error dialogs during initialization to avoid dialog crashes
                // Just log and rethrow
                System.Diagnostics.Debug.WriteLine($"Webcam initialization failed: {ex.Message}");
                _captureWebcam?.Dispose();
                _captureWebcam = null;
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
            // Common DirectShow/Windows error codes
            const uint E_ACCESSDENIED = 0x80070005;
            const uint VFW_E_NO_CAPTURE_HARDWARE = 0x80040218;
            const uint VFW_E_CANNOT_CONNECT = 0x80040217;
            const uint ERROR_BUSY = 0x800700AA;
            const uint VFW_E_TYPE_NOT_ACCEPTED = 0x8004022A;

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
            else if (errorCode == VFW_E_NO_CAPTURE_HARDWARE || errorCode == ERROR_BUSY)
            {
                title = "Camera Not Available";
                message = "The camera is being used by another application or is unavailable.\n\n" +
                         "Please:\n" +
                         "• Close other apps using the camera (Skype, Zoom, Teams, etc.)\n" +
                         "• Make sure the camera is properly connected\n" +
                         "• Try restarting the application\n\n" +
                         "If the problem persists, restart your computer.";
            }
            else if (errorCode == VFW_E_CANNOT_CONNECT || errorCode == VFW_E_TYPE_NOT_ACCEPTED)
            {
                title = "Camera Configuration Error";
                message = "Could not configure the camera.\n\n" +
                         "This may happen if:\n" +
                         "• The camera driver is outdated or incompatible\n" +
                         "• The camera doesn't support required formats\n" +
                         "• The camera is malfunctioning\n\n" +
                         "Try:\n" +
                         "• Updating your camera drivers\n" +
                         "• Using a different camera\n" +
                         "• Checking Windows Device Manager for errors";
            }
            else
            {
                title = "Camera Error";
                message = $"{context}\n\n" +
                         $"Error: {exception.Message}\n" +
                         (errorCode != 0 ? $"Code: 0x{errorCode:X8}\n\n" : "\n") +
                         "Try:\n" +
                         "• Checking Windows camera privacy settings\n" +
                         "• Closing other apps using the camera\n" +
                         "• Restarting the application\n" +
                         "• Updating camera drivers";
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
