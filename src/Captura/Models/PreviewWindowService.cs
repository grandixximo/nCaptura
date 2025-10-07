using System;
using System.Windows;
using Captura.Windows.DirectX;
using Reactive.Bindings.Extensions;
using SharpDX.Direct3D9;

namespace Captura.Video
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PreviewWindowService : IPreviewWindow
    {
        D3D9PreviewAssister _d3D9PreviewAssister;
        IntPtr _backBufferPtr;
        Texture _texture;
        readonly PreviewWindow _previewWindow;

        public void Show()
        {
            _previewWindow.ShowAndFocus();
        }

        public bool IsVisible => _previewWindow.IsVisible;

        public PreviewWindowService()
        {
            _previewWindow = new PreviewWindow();
        }

        IBitmapFrame _lastFrame;

        public void Display(IBitmapFrame Frame)
        {
            if (Frame is RepeatFrame)
                return;

            if (!IsVisible)
            {
                Frame.Dispose();
                return;
            }

            // Display frame in preview window
            try
            {
                if (_d3D9PreviewAssister == null || _texture == null || _backBufferPtr != _previewWindow.GetBackBufferPtr())
                {
                    _d3D9PreviewAssister?.Dispose();
                    _texture?.Dispose();

                    _backBufferPtr = _previewWindow.GetBackBufferPtr();
                    _d3D9PreviewAssister = new D3D9PreviewAssister(_backBufferPtr);
                    _texture = _d3D9PreviewAssister.CreateTexture(Frame.Width, Frame.Height);
                }

                _d3D9PreviewAssister.Render(_texture, Frame);

                _lastFrame?.Dispose();
                _lastFrame = Frame;
            }
            catch
            {
                Frame?.Dispose();
            }
        }

        public void Dispose()
        {
            _lastFrame?.Dispose();
            _d3D9PreviewAssister?.Dispose();
            _texture?.Dispose();
            _previewWindow?.Close();
        }
    }
}
