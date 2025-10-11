using Captura.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Captura.Video;
using Captura.ViewModels;
using RecordingViewModel = Captura.ViewModels.RecordingViewModel;
using ScreenShotViewModel = Captura.ViewModels.ScreenShotViewModel;
using Color = System.Windows.Media.Color;

namespace Captura
{
    public partial class RegionSelector : IRegionProvider, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        readonly IVideoSourcePicker _videoSourcePicker;
        readonly RegionItem _regionItem;

        bool _widthBoxChanging, _heightBoxChanging, _resizing;

        private string _recordButtonIcon;
        public string RecordButtonIcon
        {
            get => _recordButtonIcon;
            set
            {
                if (_recordButtonIcon != value)
                {
                    _recordButtonIcon = value;
                    OnPropertyChanged();
                }
            }
        }

        private void AnimatePauseButton(double toAngle)
        {
            if (PauseBtn != null)
            {
                var rotateTransform = PauseBtn.RenderTransform as System.Windows.Media.RotateTransform;
                if (rotateTransform != null)
                {
                    var animation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        To = toAngle,
                        Duration = TimeSpan.FromMilliseconds(150)
                    };
                    rotateTransform.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, animation);
                }
            }
        }

        public RegionSelector(IVideoSourcePicker VideoSourcePicker)
        {
            _videoSourcePicker = VideoSourcePicker;

            InitializeComponent();

            var platformServices = ServiceProvider.Get<IPlatformServices>();
            _regionItem = new RegionItem(this, platformServices);

            Closing += (S, E) => E.Cancel = true;

            InitDimensionBoxes();

            Loaded += (S, E) => {
                MainControls.DataContext = ServiceProvider.Get<MainViewModel>();
                
                var recordingViewModel = ServiceProvider.Get<RecordingViewModel>();
                
                recordingViewModel.RecorderState.Subscribe(state =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var icons = ServiceProvider.Get<IIconSet>();
                        RecordButtonIcon = state == RecorderState.NotRecording ? icons.Record : icons.Stop;
                        AnimatePauseButton(state == RecorderState.Paused ? 90 : 0);
                        
                        if (PauseBtn != null)
                        {
                            PauseBtn.IsEnabled = state != RecorderState.NotRecording;
                        }
                    });
                });
            };

            var regionSelectorViewModel = ServiceProvider.Get<RegionSelectorViewModel>();
            
            ModesBox.ItemsSource = new[]
            {
                new KeyValuePair<InkCanvasEditingMode, string>(InkCanvasEditingMode.None, "Pointer"),
                new KeyValuePair<InkCanvasEditingMode, string>(InkCanvasEditingMode.Ink, "Pencil"),
                new KeyValuePair<InkCanvasEditingMode, string>(InkCanvasEditingMode.EraseByPoint, "Eraser"),
                new KeyValuePair<InkCanvasEditingMode, string>(InkCanvasEditingMode.EraseByStroke, "Stroke Eraser")
            };

            ModesBox.SelectedIndex = 0;
            
            // Initialize ColorPicker with saved brush color from settings
            var savedBrushColor = regionSelectorViewModel.BrushColor.Value;
            ColorPicker.SelectedColor = savedBrushColor;
            InkCanvas.DefaultDrawingAttributes.Color = savedBrushColor;
            
            SizeBox.Value = 10;

            InkCanvas.DefaultDrawingAttributes.FitToCurve = true;
            
            // Subscribe to ExitDrawingModeCommand
            regionSelectorViewModel
                .ExitDrawingModeCommand
                .Subscribe(() => 
                {
                    regionSelectorViewModel.SelectedTool.Value = InkCanvasEditingMode.None;
                    ModesBox.SelectedIndex = 0; // Set ListBox selection to Pointer
                });
            
            // Subscribe to ClearAllDrawingsCommand
            regionSelectorViewModel
                .ClearAllDrawingsCommand
                .Subscribe(() => InkCanvas.Strokes.Clear());
            
            // Add right-click handler to exit drawing mode
            InkCanvas.PreviewMouseRightButtonDown += (s, e) =>
            {
                regionSelectorViewModel.SelectedTool.Value = InkCanvasEditingMode.None;
                ModesBox.SelectedIndex = 0;
                e.Handled = true;
            };
            
            // Add keyboard handler at Window level to exit drawing mode (ESC)
            // This catches keys before any child control can process them
            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    regionSelectorViewModel.SelectedTool.Value = InkCanvasEditingMode.None;
                    ModesBox.SelectedIndex = 0;
                    e.Handled = true;
                }
            };
        }

        void SizeBox_OnValueChanged(object Sender, RoutedPropertyChangedEventArgs<object> E)
        {
            if (InkCanvas != null && E.NewValue is int i)
                InkCanvas.DefaultDrawingAttributes.Height = InkCanvas.DefaultDrawingAttributes.Width = i;
        }

        void ModesBox_OnSelectionChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (ModesBox.SelectedValue is InkCanvasEditingMode mode)
            {
                InkCanvas.EditingMode = mode;

                if (mode == InkCanvasEditingMode.Ink)
                {
                    InkCanvas.UseCustomCursor = true;
                    InkCanvas.Cursor = Cursors.Pen;
                }
                else InkCanvas.UseCustomCursor = false;

                InkCanvas.Background = new SolidColorBrush(mode == InkCanvasEditingMode.None
                    ? Colors.Transparent
                    : Color.FromArgb(1, 0, 0, 0));
                
                // Update ViewModel's SelectedTool
                var regionSelectorViewModel = ServiceProvider.Get<RegionSelectorViewModel>();
                regionSelectorViewModel.SelectedTool.Value = mode;
            }
        }

        void ColorPicker_OnSelectedColorChanged(object Sender, RoutedPropertyChangedEventArgs<Color?> E)
        {
            if (E.NewValue != null && InkCanvas != null)
            {
                var newColor = E.NewValue.Value;
                InkCanvas.DefaultDrawingAttributes.Color = newColor;
                
                // Update ViewModel's BrushColor to trigger save to settings
                var regionSelectorViewModel = ServiceProvider.Get<RegionSelectorViewModel>();
                regionSelectorViewModel.BrushColor.Value = newColor;
            }
        }

        const int LeftOffset = 3,
            TopOffset = 3;

        Rectangle? _region;
        
        void InitDimensionBoxes()
        {
            WidthBox.Minimum = (int)((Region.MinWidth - LeftOffset * 2) * Dpi.X);
            HeightBox.Minimum = (int)((Region.MinHeight - TopOffset * 2) * Dpi.Y);

            void SizeChange()
            {
                if (_widthBoxChanging || _heightBoxChanging)
                    return;

                _resizing = true;

                var selectedRegion = SelectedRegion;

                WidthBox.Value = selectedRegion.Width;
                HeightBox.Value = selectedRegion.Height;

                _resizing = false;
            }

            SizeChanged += (S, E) => SizeChange();

            SizeChange();

            WidthBox.ValueChanged += (S, E) =>
            {
                if (!_resizing && E.NewValue is int width)
                {
                    _widthBoxChanging = true;

                    var selectedRegion = SelectedRegion;

                    selectedRegion.Width = width;

                    SelectedRegion = selectedRegion;

                    _widthBoxChanging = false;
                }
            };

            HeightBox.ValueChanged += (S, E) =>
            {
                if (!_resizing && E.NewValue is int height)
                {
                    _heightBoxChanging = true;

                    var selectedRegion = SelectedRegion;

                    selectedRegion.Height = height;

                    SelectedRegion = selectedRegion;

                    _heightBoxChanging = false;
                }
            };
        }

        void ScreenShotButton_Click(object Sender, RoutedEventArgs E)
        {
            var screenShotViewModel = ServiceProvider.Get<ScreenShotViewModel>();
            if (screenShotViewModel.ScreenShotCommand.CanExecute(null))
            {
                screenShotViewModel.ScreenShotCommand.Execute(null);
            }
        }

        void RecordButton_Click(object Sender, RoutedEventArgs E)
        {
            var recordingViewModel = ServiceProvider.Get<RecordingViewModel>();
            if (recordingViewModel.RecordCommand.CanExecute(null))
            {
                recordingViewModel.RecordCommand.Execute(null);
            }
        }

        void PauseButton_Click(object Sender, RoutedEventArgs E)
        {
            var recordingViewModel = ServiceProvider.Get<RecordingViewModel>();
            if (recordingViewModel.PauseCommand.CanExecute(null))
            {
                recordingViewModel.PauseCommand.Execute(null);
            }
        }
        
        protected override void OnLocationChanged(EventArgs E)
        {
            base.OnLocationChanged(E);

            UpdateRegion();
        }

        // Prevent Maximizing
        protected override void OnStateChanged(EventArgs E)
        {
            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;

            base.OnStateChanged(E);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo SizeInfo)
        {
            UpdateRegion();

            InkCanvas.Strokes.Clear();

            base.OnRenderSizeChanged(SizeInfo);
        }

        #region IRegionProvider
        public event Action SelectorHidden;

        public bool SelectorVisible
        {
            get => Visibility == Visibility.Visible;
            set
            {
                if (value)
                    Show();
                else Hide();
            }
        }
        
        void UpdateRegion()
        {
            // Force layout update so ActualWidth/ActualHeight reflect current Width/Height
            Region.UpdateLayout();
            
            _region = Dispatcher.Invoke(() =>
                new Rectangle((int)((Left + LeftOffset) * Dpi.X),
                    (int)((Top + TopOffset) * Dpi.Y),
                    (int)((Region.ActualWidth - 2 * LeftOffset) * Dpi.X),
                    (int)((Region.ActualHeight - 2 * TopOffset) * Dpi.Y)));

            var regionName = _region.ToString().Replace("{", "")
                .Replace("}", "")
                .Replace(",", ", ");
            
            System.Diagnostics.Debug.WriteLine($"[RegionSelector] UpdateRegion: {regionName}");
            System.Diagnostics.Debug.WriteLine($"[RegionSelector]   Window Position: Left={Left}, Top={Top}");
            System.Diagnostics.Debug.WriteLine($"[RegionSelector]   Region Size: W={Region.ActualWidth}, H={Region.ActualHeight}");
            
            _regionItem.Name = regionName;
        }

        // Ignoring Borders and Header
        public Rectangle SelectedRegion
        {
            get
            {
                if (_region == null)
                    UpdateRegion();

                return _region.Value;
            }
            set
            {
                if (value == Rectangle.Empty)
                    return;
                
                System.Diagnostics.Debug.WriteLine($"[RegionSelector] SelectedRegion setter called with: {value}");
                
                Dispatcher.Invoke(() =>
                {
                    Region.Width = value.Width / Dpi.X + 2 * LeftOffset;
                    Region.Height = value.Height / Dpi.Y + 2 * TopOffset;

                    Left = value.Left / Dpi.X - LeftOffset;
                    Top = value.Top / Dpi.Y - TopOffset;
                    
                    System.Diagnostics.Debug.WriteLine($"[RegionSelector] Set Window Position: Left={Left}, Top={Top}");
                    System.Diagnostics.Debug.WriteLine($"[RegionSelector] Set Region Size: W={Region.Width}, H={Region.Height}");
                    
                    // Update the region name immediately after setting the size
                    UpdateRegion();
                });
            }
        }

        public void Lock()
        {
            Dispatcher.Invoke(() =>
            {
                ResizeMode = ResizeMode.NoResize;
                Snapper.IsEnabled = false;

                WidthBox.IsEnabled = HeightBox.IsEnabled = false;
            });
        }
        
        public void Release()
        {
            Dispatcher.Invoke(() =>
            {
                ResizeMode = ResizeMode.CanResize;
                Snapper.IsEnabled = true;

                WidthBox.IsEnabled = HeightBox.IsEnabled = true;

                Show();
            });
        }

        public IVideoItem VideoSource => _regionItem;

        public IntPtr Handle => new WindowInteropHelper(this).Handle;
        #endregion

        void Snapper_OnClick(object Sender, RoutedEventArgs E)
        {
            // PickWindow now needs a Predicate, not IntPtr[]
            var win = _videoSourcePicker.PickWindow(w => w.Handle != Handle);

            if (win == null)
                return;

            SelectedRegion = win.Rectangle;

            // Prevent going outside
            if (Left < 0)
            {
                // Decrease Width
                try { Width += Left; }
                catch { }
                finally { Left = 0; }
            }

            if (Top < 0)
            {
                // Decrease Height
                try { Height += Top; }
                catch { }
                finally { Top = 0; }
            }
        }

        void UIElement_OnPreviewMouseLeftButtonDown(object Sender, MouseButtonEventArgs E)
        {
            DragMove();
        }

        System.Threading.Timer _updateTimer;
        bool _updatePending;

        void Thumb_OnDragDelta(object Sender, DragDeltaEventArgs E)
        {
            if (Sender is FrameworkElement element)
            {
                void DoTop()
                {
                    var oldTop = Top;
                    var oldBottom = Top + Region.Height;
                    var top = Top + E.VerticalChange;

                    if (top > 0)
                        Top = top;
                    else
                    {
                        Top = 0;
                        Region.Width = oldBottom;
                        return;
                    }

                    var height = Region.Height - E.VerticalChange;

                    if (height > Region.MinHeight)
                        Region.Height = height;
                    else Top = oldTop;
                }

                void DoLeft()
                {
                    var oldLeft = Left;
                    var oldRight = Left + Region.Width;
                    var left = Left + E.HorizontalChange;

                    if (left > 0)
                        Left = left;
                    else
                    {
                        Left = 0;
                        Region.Width = oldRight;
                        return;
                    }

                    var width = Region.Width - E.HorizontalChange;

                    if (width > Region.MinWidth)
                        Region.Width = width;
                    else Left = oldLeft;
                }

                void DoBottom()
                {
                    var height = Region.Height + E.VerticalChange;

                    if (height > 0)
                        Region.Height = height;
                }

                void DoRight()
                {
                    var width = Region.Width + E.HorizontalChange;

                    if (width > 0)
                        Region.Width = width;
                }

                void DoMove()
                {
                    Left += E.HorizontalChange;
                    Top += E.VerticalChange;
                }

                switch (element.Tag)
                {
                    case "Top":
                        DoMove();
                        break;

                    case "Bottom":
                        DoBottom();
                        break;

                    case "Left":
                        DoLeft();
                        break;

                    case "Right":
                        DoRight();
                        break;

                    case "TopLeft":
                        DoTop();
                        DoLeft();
                        break;

                    case "TopRight":
                        DoTop();
                        DoRight();
                        break;

                    case "BottomLeft":
                        DoBottom();
                        DoLeft();
                        break;

                    case "BottomRight":
                        DoBottom();
                        DoRight();
                        break;
                }

                // Throttle updates to avoid hundreds of calls per second
                // Update at most every 50ms (20 updates per second)
                if (!_updatePending)
                {
                    _updatePending = true;
                    _updateTimer?.Dispose();
                    _updateTimer = new System.Threading.Timer(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            UpdateRegion();
                            _updatePending = false;
                        });
                    }, null, 50, System.Threading.Timeout.Infinite);
                }
            }
        }

        void Thumb_OnDragCompleted(object Sender, DragCompletedEventArgs E)
        {
            // Final update when drag completes to ensure accuracy
            _updateTimer?.Dispose();
            _updatePending = false;
            Dispatcher.BeginInvoke(new Action(() => UpdateRegion()), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}
