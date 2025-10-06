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
        readonly VisualSettings _visualSettings;

        public void Show()
        {
            _visualSettings.Expanded = true;
        }

        public bool IsVisible { get; private set; }

        public PreviewWindowService(VisualSettings VisualSettings)
        {
            _visualSettings = VisualSettings;

            VisualSettings.ObserveProperty(M => M.Expanded)
                .Subscribe(M => IsVisible = M);
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

            // Modern version doesn't have preview window in MainWindow
            // Just dispose the frame
            _lastFrame?.Dispose();
            _lastFrame = Frame;
        }

        public void Dispose()
        {
            _lastFrame?.Dispose();
            _d3D9PreviewAssister?.Dispose();
            _texture?.Dispose();
        }
    }
}
