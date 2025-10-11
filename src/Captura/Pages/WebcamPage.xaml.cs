using System;
using System.ComponentModel;
using System.Drawing;
using System.Reactive.Linq;
using WSize = System.Windows.Size;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Captura.ViewModels;
using Captura.Webcam;
using Captura.Windows.Gdi;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Xceed.Wpf.Toolkit.Core.Utilities;

namespace Captura
{
    public partial class WebcamPage : INotifyPropertyChanged
    {
        readonly WebcamModel _webcamModel;
        readonly ScreenShotModel _screenShotModel;
        readonly IPlatformServices _platformServices;
        readonly WebcamOverlayReactor _reactor;

        public WebcamPage(WebcamModel WebcamModel,
            ScreenShotModel ScreenShotModel,
            IPlatformServices PlatformServices,
            WebcamOverlaySettings WebcamSettings)
        {
            _webcamModel = WebcamModel;
            _screenShotModel = ScreenShotModel;
            _platformServices = PlatformServices;

            _reactor = new WebcamOverlayReactor(WebcamSettings);

            Loaded += OnLoaded;

            InitializeComponent();
        }

        bool _isLoadingScreenshot;
        public bool IsLoadingScreenshot
        {
            get => _isLoadingScreenshot;
            set
            {
                if (_isLoadingScreenshot != value)
                {
                    _isLoadingScreenshot = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadingScreenshot)));
                }
            }
        }

        bool _isLoadingCamera;
        public bool IsLoadingCamera
        {
            get => _isLoadingCamera;
            set
            {
                if (_isLoadingCamera != value)
                {
                    _isLoadingCamera = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoadingCamera)));
                }
            }
        }

        bool _isCameraReady;
        public bool IsCameraReady
        {
            get => _isCameraReady;
            set
            {
                if (_isCameraReady != value)
                {
                    _isCameraReady = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCameraReady)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        bool _loaded;
        static WSize _cachedFrameSize = new WSize(1920, 1080);

        void OnLoaded(object Sender, RoutedEventArgs E)
        {
            if (_loaded)
                return;

            _loaded = true;

            var control = PreviewTarget;

            control.BindOne(MarginProperty,
                _reactor.Location.Select(M => new Thickness(M.X, M.Y, 0, 0)).ToReadOnlyReactivePropertySlim());

            control.BindOne(WidthProperty,
                _reactor.Size.Select(M => M.Width).ToReadOnlyReactivePropertySlim());
            control.BindOne(HeightProperty,
                _reactor.Size.Select(M => M.Height).ToReadOnlyReactivePropertySlim());

            control.BindOne(OpacityProperty, _reactor.Opacity);

            _reactor.FrameSize.OnNext(_cachedFrameSize);
        }

        async Task UpdateBackgroundAsync()
        {
            IsLoadingScreenshot = true;

            try
            {
                await Task.Yield();

                var source = await WpfExtensions.GetBackground();

                await Dispatcher.InvokeAsync(() =>
                {
                    Img.Source = source;

                    if (Img.ActualWidth > 0 && Img.ActualHeight > 0)
                    {
                        _cachedFrameSize = new WSize(Img.ActualWidth, Img.ActualHeight);
                        _reactor.FrameSize.OnNext(_cachedFrameSize);
                    }
                }, System.Windows.Threading.DispatcherPriority.Loaded);
            }
            finally
            {
                IsLoadingScreenshot = false;
            }
        }

        IReadOnlyReactiveProperty<IWebcamCapture> _webcamCapture;
        bool _acquired;

        public void SetupPreview()
        {
            _webcamModel.PreviewClicked += SettingsWindow.ShowWebcamPage;

            IsVisibleChanged += async (S, E) =>
            {
                if (IsVisible)
                {
                    await Task.Delay(300);
                    _ = UpdateBackgroundAsync();
                    await InitializeCameraAsync();
                }
                else
                {
                    // Hide preview when page is not visible but keep camera running to avoid flicker
                    if (_webcamCapture?.Value != null)
                    {
                        try { _webcamCapture.Value.SetPreviewVisibility(false); } catch { }
                    }
                }
            };

            void OnRegionChange()
            {
                if (!IsVisible)
                    return;

                if (Img.ActualWidth > 0 && Img.ActualHeight > 0)
                {
                    _cachedFrameSize = new WSize(Img.ActualWidth, Img.ActualHeight);
                    _reactor.FrameSize.OnNext(_cachedFrameSize);
                }
            }

            PreviewGrid.LayoutUpdated += (S, E) => OnRegionChange();

            _webcamModel
                .ObserveProperty(M => M.SelectedCam)
                .Subscribe(M => OnCameraChanged());

            _reactor.Location
                .CombineLatest(_reactor.Size, (M, N) =>
                {
                    UpdateWebcamPreview();
                    return 0;
                })
                .Subscribe();

            UpdateWebcamPreview();

            Unloaded += (s, e) =>
            {
                // Cleanup when page is unloaded (not just hidden)
                if (_acquired && _webcamCapture != null)
                {
                    _webcamModel.ReleaseCapture();
                    _webcamCapture = null;
                    _acquired = false;
                }
            };
        }

        async void OnCameraChanged()
        {
            if (!IsVisible)
                return;

            if (_webcamCapture != null)
            {
                _webcamModel.ReleaseCapture();
                _webcamCapture = null;
                _acquired = false;
            }

            await InitializeCameraAsync();
        }

        async Task InitializeCameraAsync()
        {
            // Don't release and recreate if camera is already initialized
            if (_webcamCapture != null)
            {
                // Camera already initialized, just update the preview
                // UpdateWebcamPreview will automatically show the preview
                UpdateWebcamPreview();
                return;
            }

            IsCameraReady = false;

            if (_webcamModel.SelectedCam is NoWebcamItem)
            {
                return;
            }

            while (IsLoadingScreenshot)
            {
                await Task.Delay(50);
            }

            await Task.Delay(300);

            await Dispatcher.InvokeAsync(() =>
            {
                IsCameraReady = true;
            });

            IsLoadingCamera = true;

            try
            {
                await Task.Yield();

                _webcamCapture = _webcamModel.InitCapture();
                _acquired = true;

                if (_webcamCapture.Value is { } capture)
                {
                    _reactor.WebcamSize.OnNext(new WSize(capture.Width, capture.Height));

                    await Dispatcher.InvokeAsync(() =>
                    {
                        UpdateWebcamPreview();
                    }, System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            finally
            {
                IsLoadingCamera = false;
            }
        }

        async void Refresh_OnClick(object Sender, RoutedEventArgs E)
        {
            await UpdateBackgroundAsync();
        }

        async void CaptureImage_OnClick(object Sender, RoutedEventArgs E)
        {
            try
            {
                var img = _webcamCapture.Value?.Capture(GraphicsBitmapLoader.Instance);

                await _screenShotModel.SaveScreenShot(img);
            }
            catch { }
        }

        void ShowCameraProperties_OnClick(object Sender, RoutedEventArgs E)
        {
            try
            {
                if (_webcamCapture?.Value == null)
                {
                    MessageBox.Show("No camera is currently active. Please select a camera first.", 
                        "Camera Not Active", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var properties = _webcamCapture.Value.GetCameraProperties();
                var window = new CameraPropertiesWindow(properties)
                {
                    Owner = Window.GetWindow(this)
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to get camera properties: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Rectangle GetPreviewWindowRect()
        {
            var parentWindow = VisualTreeHelperEx.FindAncestorByType<Window>(this);

            var relativePt = PreviewGrid.TranslatePoint(new System.Windows.Point(0, 0), parentWindow);

            var position = _reactor.Location.Value;
            var size = _reactor.Size.Value;

            var rect = new RectangleF((float)(relativePt.X + position.X),
                (float)(relativePt.Y + position.Y),
                (float)(size.Width),
                (float)(size.Height));

            return rect.ApplyDpi();
        }

        void UpdateWebcamPreview()
        {
            if (_webcamCapture?.Value == null)
                return;

            try
            {
                // Get the window handle
                var parentWindow = VisualTreeHelperEx.FindAncestorByType<Window>(this);
                if (parentWindow == null)
                {
                    // Window not ready yet, retry after a short delay
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_webcamCapture != null && IsVisible)
                            UpdateWebcamPreview();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }

                var windowSource = PresentationSource.FromVisual(parentWindow) as HwndSource;
                if (windowSource == null)
                {
                    // Retry
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_webcamCapture != null && IsVisible)
                            UpdateWebcamPreview();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                    return;
                }

                var win = _platformServices.GetWindow(windowSource.Handle);
                var rect = GetPreviewWindowRect();

                _webcamCapture.Value.UpdatePreview(win, rect);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateWebcamPreview failed: {ex.Message}");
            }
        }
    }
}
