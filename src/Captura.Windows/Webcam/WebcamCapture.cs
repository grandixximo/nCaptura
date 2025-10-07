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

        public WebcamCapture(Filter Filter, Action OnClick)
        {
            _filter = Filter;
            _onClick = OnClick;
            
            try
            {
                _captureWebcam = new CaptureWebcam(Filter, OnClick, IntPtr.Zero);
                _captureWebcam.StartPreview();
            }
            catch (COMException ex)
            {
                // Common COM errors when accessing webcam
                var errorMessage = ex.HResult switch
                {
                    unchecked((int)0x80070005) => "Access denied. Please check Windows camera privacy settings:\n" +
                                                  "Settings > Privacy > Camera > Allow desktop apps to access your camera",
                    unchecked((int)0x8007048F) => "Camera is being used by another application. Please close other apps using the camera.",
                    unchecked((int)0x800706BA) => "Camera device not available or disconnected.",
                    _ => $"Failed to initialize camera: {ex.Message}\nHRESULT: 0x{ex.HResult:X8}"
                };
                
                throw new Exception(errorMessage, ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize camera '{Filter.Name}': {ex.Message}", ex);
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

                    try
                    {
                        _captureWebcam = new CaptureWebcam(_filter, _onClick, Window.Handle);
                        _captureWebcam.StartPreview();
                        _lastWin = Window.Handle;
                    }
                    catch (COMException ex)
                    {
                        var errorMessage = ex.HResult switch
                        {
                            unchecked((int)0x80070005) => "Access denied. Please check Windows camera privacy settings:\n" +
                                                          "Settings > Privacy > Camera > Allow desktop apps to access your camera",
                            unchecked((int)0x8007048F) => "Camera is being used by another application. Please close other apps using the camera.",
                            unchecked((int)0x800706BA) => "Camera device not available or disconnected.",
                            _ => $"Failed to start camera preview: {ex.Message}\nHRESULT: 0x{ex.HResult:X8}"
                        };
                        
                        throw new Exception(errorMessage, ex);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to start camera preview for '{_filter.Name}': {ex.Message}", ex);
                    }
                }

                _captureWebcam?.OnPreviewWindowResize(Location.X, Location.Y, Location.Width, Location.Height);
            });
        }
    }
}