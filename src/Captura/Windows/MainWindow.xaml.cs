using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Markup;
using Captura.Models;

namespace Captura
{
    public partial class MainWindow
    {
        public static MainWindow Instance { get; private set; }

        readonly MainWindowHelper _helper;
        Expander _currentExpander;

        public MainWindow()
        {
            Instance = this;
            
            InitializeComponent();

            _helper = ServiceProvider.Get<MainWindowHelper>();

            _helper.MainViewModel.Init(!App.CmdOptions.NoPersist, !App.CmdOptions.Reset);

            _helper.HotkeySetup.Setup();

            _helper.TimerModel.Init();

            Loaded += (Sender, Args) =>
            {
                RepositionWindowIfOutside();

                ServiceProvider.Get<WebcamPage>().SetupPreview();

                _helper.HotkeySetup.ShowUnregistered();
                
                // Load the appropriate UI mode on startup
                SwitchUIMode();
            };

            if (App.CmdOptions.Tray || _helper.Settings.Tray.MinToTrayOnStartup)
                Hide();

            Closing += (Sender, Args) =>
            {
                if (!TryExit())
                    Args.Cancel = true;
            };

            // Register to bring this instance to foreground when other instances are launched.
            SingleInstanceManager.StartListening(WakeApp);
        }

        // Public accessors for preview in Modern UI mode
        public PictureBox DisplayImage { get; private set; }
        public System.Windows.Interop.D3DImage D3DImage { get; private set; }
        public WindowsFormsHost WinFormsHost { get; private set; }

        void WakeApp()
        {
            Dispatcher.Invoke(() =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }

                Activate();
            });
        }

        void RepositionWindowIfOutside()
        {
            // Window dimensions taking care of DPI
            var rect = new RectangleF((float) Left,
                (float) Top,
                (float) ActualWidth,
                (float) ActualHeight).ApplyDpi();
            
            if (!Screen.AllScreens.Any(M => M.Bounds.Contains(rect)))
            {
                Left = 50;
                Top = 50;
            }
        }

        void Grid_PreviewMouseLeftButtonDown(object Sender, MouseButtonEventArgs Args)
        {
            DragMove();

            Args.Handled = true;
        }

        void MinButton_Click(object Sender, RoutedEventArgs Args) => SystemCommands.MinimizeWindow(this);

        void CloseButton_Click(object Sender, RoutedEventArgs Args)
        {
            if (_helper.Settings.Tray.MinToTrayOnClose)
            {
                Hide();
            }
            else Close();
        }

        void SystemTray_TrayMouseDoubleClick(object Sender, RoutedEventArgs Args)
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else this.ShowAndFocus();
        }

        bool TryExit()
        {
            if (!_helper.RecordingViewModel.CanExit())
                return false;

            ServiceProvider.Dispose();

            return true;
        }

        void MenuExit_Click(object Sender, RoutedEventArgs Args) => Close();

        void HideButton_Click(object Sender, RoutedEventArgs Args) => Hide();

        void ShowMainWindow(object Sender, RoutedEventArgs E) => this.ShowAndFocus();

        void UIToggleButton_Click(object Sender, RoutedEventArgs Args)
        {
            // Toggle the UI mode setting
            _helper.Settings.UI.UseClassicUI = !_helper.Settings.UI.UseClassicUI;
            
            // Save the settings
            _helper.Settings.Save();
            
            // Switch UI immediately
            SwitchUIMode();
            
            // Show a message to inform the user
            var mode = _helper.Settings.UI.UseClassicUI ? "Classic" : "Modern";
            System.Windows.MessageBox.Show(
                $"UI switched to {mode} mode!",
                "UI Mode Changed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        void SwitchUIMode()
        {
            // Clear current content
            if (_currentExpander != null)
            {
                MainContent.Content = null;
                _currentExpander = null;
            }

            if (_helper.Settings.UI.UseClassicUI)
            {
                LoadClassicLayout();
            }
            else
            {
                LoadModernLayout();
            }
        }

        void LoadModernLayout()
        {
            // Hide CollapsedBar (used in modern when collapsed)
            CollapsedBarContainer.Visibility = Visibility.Collapsed;
            
            // Close separate preview window if open
            PreviewWindow.Instance.Hide();

            // Create Modern UI Expander
            _currentExpander = CreateModernExpander();
            MainContent.Content = _currentExpander;
        }

        void LoadClassicLayout()
        {
            // Show CollapsedBar container (classic uses it differently)
            CollapsedBarContainer.Visibility = Visibility.Collapsed; // Classic handles this in binding
            
            // Create Classic UI Expander
            _currentExpander = CreateClassicExpander();
            MainContent.Content = _currentExpander;
        }

        Expander CreateModernExpander()
        {
            var expander = new Expander
            {
                Padding = new Thickness(5, 0, 0, 0),
                IsExpanded = _helper.Settings.UI.Expanded
            };

            // Bind IsExpanded
            var expandedBinding = new System.Windows.Data.Binding("Settings.UI.Expanded")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            };
            expander.SetBinding(Expander.IsExpandedProperty, expandedBinding);

            // Create header
            expander.Header = CreateModernHeader();

            // Create content
            expander.Content = CreateModernContent();

            return expander;
        }

        object CreateModernHeader()
        {
            // Modern header with fancy styling
            var grid = new Grid();
            
            // Border for background
            var border = new Border
            {
                CornerRadius = new CornerRadius(15, 15, 25, 25),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 2, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Width = 70,
                Height = 33
            };
            border.SetResourceReference(Border.BackgroundProperty, "ButtonBackgroundHover");
            grid.Children.Add(border);

            var dockPanel = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            // CollapsedBar
            var collapsedBar = new CollapsedBar { Margin = new Thickness(-30, 0, 0, 0) };
            DockPanel.SetDock(collapsedBar, Dock.Bottom);
            dockPanel.Children.Add(collapsedBar);

            // ScreenShotButton
            var screenShotBtn = new ScreenShotButton();
            dockPanel.Children.Add(screenShotBtn);

            // Record button
            var recordBtn = CreateModernButton("RecordStop", "RecordingViewModel.RecordCommand", "#ee2c2c", "RecordingViewModel.RecorderState.Value", "StateToRecordButtonGeometryConverter");
            dockPanel.Children.Add(recordBtn);

            // PauseButton
            var pauseBtn = new PauseButton();
            dockPanel.Children.Add(pauseBtn);

            // Close button (right side)
            var closeBtn = CreateModernButton("Close", null, "#77ef5350", null, null, "Icons.Close");
            closeBtn.Click += CloseButton_Click;
            DockPanel.SetDock(closeBtn, Dock.Right);
            dockPanel.Children.Add(closeBtn);

            // Hide to tray button (right side)
            var hideBtn = CreateModernButton("MinTray", null, null, null, null, "Icons.DoubleDown");
            hideBtn.Click += HideButton_Click;
            hideBtn.Opacity = 0.7;
            DockPanel.SetDock(hideBtn, Dock.Right);
            dockPanel.Children.Add(hideBtn);

            // Minimize button (right side)
            var minBtn = CreateModernButton("Minimize", null, null, null, null, "Icons.Minimize");
            minBtn.Click += MinButton_Click;
            minBtn.Opacity = 0.7;
            DockPanel.SetDock(minBtn, Dock.Right);
            dockPanel.Children.Add(minBtn);
            
            // UI Toggle button (right side) - between time and minimize
            var toggleBtn = CreateModernButton("Toggle UI Mode (New/Classic)", null, null, null, null, "Icons.Contrast");
            toggleBtn.Click += UIToggleButton_Click;
            toggleBtn.Opacity = 0.7;
            DockPanel.SetDock(toggleBtn, Dock.Right);
            dockPanel.Children.Add(toggleBtn);

            // Timer/Countdown grid (center)
            var timerGrid = CreateTimerGrid();
            dockPanel.Children.Add(timerGrid);

            grid.Children.Add(dockPanel);
            return grid;
        }

        object CreateClassicHeader()
        {
            // Classic header with simple buttons
            var dockPanel = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            // Screenshot button
            var screenShotBtn = CreateModernButton("ScreenShot", "ScreenShotViewModel.ScreenShotCommand", null, null, null, "Icons.Camera");
            screenShotBtn.Opacity = 0.9;
            dockPanel.Children.Add(screenShotBtn);

            // Record button
            var recordBtn = CreateModernButton("RecordStop", "RecordingViewModel.RecordCommand", "#b71c1c", "RecordingViewModel.RecorderState.Value", "StateToRecordButtonGeometryConverter");
            dockPanel.Children.Add(recordBtn);

            // Pause button with rotation
            var pauseBtn = CreatePauseButtonWithRotation();
            dockPanel.Children.Add(pauseBtn);

            // Close button (right side)
            var closeBtn = CreateModernButton("Close", null, "#77ef5350", null, null, "Icons.Close");
            closeBtn.Click += CloseButton_Click;
            DockPanel.SetDock(closeBtn, Dock.Right);
            dockPanel.Children.Add(closeBtn);

            // Hide to tray button (right side)
            var hideBtn = CreateModernButton("MinTray", null, null, null, null, "Icons.DoubleDown");
            hideBtn.Click += HideButton_Click;
            hideBtn.Opacity = 0.7;
            DockPanel.SetDock(hideBtn, Dock.Right);
            dockPanel.Children.Add(hideBtn);

            // Minimize button (right side)
            var minBtn = CreateModernButton("Minimize", null, null, null, null, "Icons.Minimize");
            minBtn.Click += MinButton_Click;
            minBtn.Opacity = 0.7;
            DockPanel.SetDock(minBtn, Dock.Right);
            dockPanel.Children.Add(minBtn);
            
            // UI Toggle button (right side)
            var toggleBtn = CreateModernButton("Toggle UI Mode (New/Classic)", null, null, null, null, "Icons.Contrast");
            toggleBtn.Click += UIToggleButton_Click;
            toggleBtn.Opacity = 0.7;
            DockPanel.SetDock(toggleBtn, Dock.Right);
            dockPanel.Children.Add(toggleBtn);

            // Timer grid (center)
            var timerGrid = CreateClassicTimerGrid();
            dockPanel.Children.Add(timerGrid);

            return dockPanel;
        }

        DockPanel CreateModernContent()
        {
            var dockPanel = new DockPanel { Margin = new Thickness(-5, 0, -5, 0), Height = 300 };

            // Frame for HomePage
            var frame = new Frame
            {
                Source = new Uri("../Pages/HomePage.xaml", UriKind.Relative),
                NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
            };
            DockPanel.SetDock(frame, Dock.Top);
            dockPanel.Children.Add(frame);

            // GridSplitter
            var splitter1 = new GridSplitter { Height = 1, Margin = new Thickness(0, 2, 0, 2), IsEnabled = false };
            DockPanel.SetDock(splitter1, Dock.Top);
            dockPanel.Children.Add(splitter1);

            // FPS Label
            var fpsLabel = new Label { Margin = new Thickness(5, 0, 5, 0) };
            var fpsBinding = new System.Windows.Data.Binding("ViewConditions.FpsVisibility.Value")
            {
                Source = ServiceProvider.Get<ViewConditions>()
            };
            fpsLabel.SetBinding(UIElement.VisibilityProperty, fpsBinding);
            
            var fpsTextBlock = new TextBlock();
            fpsTextBlock.Inlines.Add("FPS: ");
            var fpsRun = new System.Windows.Documents.Run();
            var fpsRunBinding = new System.Windows.Data.Binding("FpsManager.Fps")
            {
                Source = ServiceProvider.Get<FpsManager>(),
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            fpsRun.SetBinding(System.Windows.Documents.Run.TextProperty, fpsRunBinding);
            fpsTextBlock.Inlines.Add(fpsRun);
            fpsLabel.Content = fpsTextBlock;
            
            DockPanel.SetDock(fpsLabel, Dock.Bottom);
            dockPanel.Children.Add(fpsLabel);

            // GridSplitter
            var splitter2 = new GridSplitter { Height = 1, Margin = new Thickness(0, 2, 0, 2), IsEnabled = false };
            DockPanel.SetDock(splitter2, Dock.Bottom);
            dockPanel.Children.Add(splitter2);

            // Preview area (WinFormsHost + D3DImage)
            var previewGrid = CreatePreviewGrid();
            dockPanel.Children.Add(previewGrid);

            return dockPanel;
        }

        DockPanel CreateClassicContent()
        {
            var dockPanel = new DockPanel { Margin = new Thickness(-5, 0, 0, 0), MaxHeight = 650 };

            // Copyright label
            var copyrightLabel = new Label
            {
                Content = "© Mathew Sachin",
                Opacity = 0.9,
                Margin = new Thickness(5)
            };
            DockPanel.SetDock(copyrightLabel, Dock.Bottom);
            dockPanel.Children.Add(copyrightLabel);

            // OutputFolderControl
            var outputFolder = new OutputFolderControl();
            DockPanel.SetDock(outputFolder, Dock.Bottom);
            dockPanel.Children.Add(outputFolder);
            
            // Show Preview button for classic mode
            var previewButton = new System.Windows.Controls.Button
            {
                Content = "Show Preview Window",
                Margin = new Thickness(5),
                Padding = new Thickness(10, 5, 10, 5)
            };
            previewButton.Click += (s, e) => PreviewWindow.Instance.ShowAndFocus();
            DockPanel.SetDock(previewButton, Dock.Bottom);
            dockPanel.Children.Add(previewButton);

            // Frame for MainPageClassic (with tabs)
            var frame = new Frame
            {
                Source = new Uri("../Pages/MainPageClassic.xaml", UriKind.Relative),
                NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden
            };
            dockPanel.Children.Add(frame);

            return dockPanel;
        }

        Grid CreatePreviewGrid()
        {
            var grid = new Grid();

            // Preview label
            var previewLabel = new Label
            {
                Content = "Preview",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Opacity = 0.7
            };
            grid.Children.Add(previewLabel);

            // WindowsFormsHost
            WinFormsHost = new WindowsFormsHost { Visibility = Visibility.Collapsed };
            DisplayImage = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom };
            WinFormsHost.Child = DisplayImage;
            grid.Children.Add(WinFormsHost);

            // D3DImage
            D3DImage = new System.Windows.Interop.D3DImage();
            var image = new Image { Source = D3DImage };
            grid.Children.Add(image);

            return grid;
        }

        Grid CreateTimerGrid()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, -2, 0, -2),
                Background = System.Windows.Media.Brushes.Transparent
            };
            grid.PreviewMouseLeftButtonDown += Grid_PreviewMouseLeftButtonDown;

            // Border for background
            var border = new Border
            {
                CornerRadius = new CornerRadius(15),
                BorderThickness = new Thickness(0),
                Margin = new Thickness(30, 5, 30, 5)
            };
            border.SetResourceReference(Border.BackgroundProperty, "ButtonBackgroundHover");
            grid.Children.Add(border);

            // Duration label stack
            var durationStack = new StackPanel
            {
                Margin = new Thickness(10, -1, 10, -1),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Name = "DurationLabel"
            };

            // TimeSpan label
            var timeLabel = new Label();
            var timeBinding = new System.Windows.Data.Binding("TimerModel.TimeSpan")
            {
                Source = ServiceProvider.Get<TimerModel>()
            };
            timeLabel.SetBinding(Label.ContentProperty, timeBinding);
            durationStack.Children.Add(timeLabel);

            // Duration label
            var durationLabel = new Label
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                FontSize = 8
            };
            var durationBinding = new System.Windows.Data.Binding("Settings.Duration")
            {
                Converter = new SecondsToTimeSpanConverter()
            };
            durationLabel.SetBinding(Label.ContentProperty, durationBinding);
            durationStack.Children.Add(durationLabel);

            var visibilityBinding = new System.Windows.Data.Binding("TimerModel.Countdown")
            {
                Source = ServiceProvider.Get<TimerModel>(),
                Converter = new IsLessThanConverter(),
                ConverterParameter = 1
            };
            durationStack.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            grid.Children.Add(durationStack);

            // Countdown label
            var countdownLabel = new Label
            {
                Margin = new Thickness(0, -1, 0, -1),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            countdownLabel.SetResourceReference(FrameworkElement.StyleProperty, "CountdownLabel");
            var countdownBinding = new System.Windows.Data.Binding("TimerModel.Countdown")
            {
                Source = ServiceProvider.Get<TimerModel>()
            };
            countdownLabel.SetBinding(Label.ContentProperty, countdownBinding);

            var countdownVisBinding = new System.Windows.Data.Binding("Visibility")
            {
                ElementName = "DurationLabel",
                Converter = new NegatingConverter()
            };
            countdownLabel.SetBinding(UIElement.VisibilityProperty, countdownVisBinding);

            grid.Children.Add(countdownLabel);

            return grid;
        }

        Grid CreateClassicTimerGrid()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, -2, 0, -2),
                Background = System.Windows.Media.Brushes.Transparent
            };
            grid.PreviewMouseLeftButtonDown += Grid_PreviewMouseLeftButtonDown;

            // Duration label stack
            var durationStack = new StackPanel
            {
                Margin = new Thickness(10, -1, 10, -1),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Name = "DurationLabel"
            };

            // TimeSpan label
            var timeLabel = new Label();
            var timeBinding = new System.Windows.Data.Binding("TimerModel.TimeSpan")
            {
                Source = ServiceProvider.Get<TimerModel>()
            };
            timeLabel.SetBinding(Label.ContentProperty, timeBinding);
            durationStack.Children.Add(timeLabel);

            // Duration label
            var durationLabel = new Label
            {
                ContentStringFormat = "{0}",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                FontSize = 8
            };
            var durationBinding = new System.Windows.Data.Binding("Settings.Duration")
            {
                Converter = new SecondsToTimeSpanConverter()
            };
            durationLabel.SetBinding(Label.ContentProperty, durationBinding);
            var visibilityDurationBinding = new System.Windows.Data.Binding("Settings.Duration")
            {
                Converter = new NotNullConverter()
            };
            durationLabel.SetBinding(UIElement.VisibilityProperty, visibilityDurationBinding);
            durationStack.Children.Add(durationLabel);

            var visibilityBinding = new System.Windows.Data.Binding("TimerModel.Countdown")
            {
                Source = ServiceProvider.Get<TimerModel>(),
                Converter = new IsLessThanConverter(),
                ConverterParameter = 1
            };
            durationStack.SetBinding(UIElement.VisibilityProperty, visibilityBinding);

            grid.Children.Add(durationStack);

            // Countdown label
            var countdownLabel = new Label
            {
                Margin = new Thickness(0, -1, 0, -1),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            countdownLabel.SetResourceReference(FrameworkElement.StyleProperty, "CountdownLabel");
            var countdownBinding = new System.Windows.Data.Binding("TimerModel.Countdown")
            {
                Source = ServiceProvider.Get<TimerModel>()
            };
            countdownLabel.SetBinding(Label.ContentProperty, countdownBinding);

            var countdownVisBinding = new System.Windows.Data.Binding("Visibility")
            {
                ElementName = "DurationLabel",
                Converter = new NegatingConverter()
            };
            countdownLabel.SetBinding(UIElement.VisibilityProperty, countdownVisBinding);

            grid.Children.Add(countdownLabel);

            return grid;
        }

        ModernButton CreateModernButton(string tooltip, string command, string foreground, string iconBinding, string iconConverter, string iconPath = null)
        {
            var btn = new ModernButton();

            // Tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                var tooltipBinding = new System.Windows.Data.Binding(tooltip)
                {
                    Source = ServiceProvider.Get<LanguageManager>(),
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                btn.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);
            }

            // Command
            if (!string.IsNullOrEmpty(command))
            {
                var parts = command.Split('.');
                object source = ServiceProvider.Get<MainViewModel>();
                foreach (var part in parts.Take(parts.Length - 1))
                {
                    var prop = source.GetType().GetProperty(part);
                    source = prop?.GetValue(source);
                }
                var cmdProp = source?.GetType().GetProperty(parts.Last());
                var cmd = cmdProp?.GetValue(source) as ICommand;
                btn.Command = cmd;
            }

            // Foreground
            if (!string.IsNullOrEmpty(foreground))
            {
                btn.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(foreground);
            }

            // IconData
            if (!string.IsNullOrEmpty(iconBinding))
            {
                var binding = new System.Windows.Data.Binding(iconBinding)
                {
                    Source = ServiceProvider.Get<MainViewModel>()
                };
                if (!string.IsNullOrEmpty(iconConverter))
                {
                    binding.Converter = (IValueConverter)FindResource($"{iconConverter}");
                }
                btn.SetBinding(ModernButton.IconDataProperty, binding);
            }
            else if (!string.IsNullOrEmpty(iconPath))
            {
                var binding = new System.Windows.Data.Binding(iconPath)
                {
                    Source = ServiceProvider.Get<IIconSet>()
                };
                btn.SetBinding(ModernButton.IconDataProperty, binding);
            }

            return btn;
        }

        ModernButton CreatePauseButtonWithRotation()
        {
            var btn = new ModernButton
            {
                Opacity = 0.9,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new System.Windows.Media.RotateTransform()
            };

            // Tooltip
            var tooltipBinding = new System.Windows.Data.Binding("PauseResume")
            {
                Source = ServiceProvider.Get<LanguageManager>(),
                Mode = System.Windows.Data.BindingMode.OneWay
            };
            btn.SetBinding(FrameworkElement.ToolTipProperty, tooltipBinding);

            // Command
            var cmd = ServiceProvider.Get<MainViewModel>().RecordingViewModel.PauseCommand;
            btn.Command = cmd;

            // IconData
            var iconBinding = new System.Windows.Data.Binding("Icons.Pause")
            {
                Source = ServiceProvider.Get<IIconSet>()
            };
            btn.SetBinding(ModernButton.IconDataProperty, iconBinding);

            // Style with rotation animation
            // (Simplified - full animation would require more complex setup)

            return btn;
        }

        Expander CreateClassicExpander()
        {
            var expander = new Expander
            {
                Padding = new Thickness(5, 0, 0, 0),
                IsExpanded = _helper.Settings.UI.Expanded
            };

            // Bind IsExpanded
            var expandedBinding = new System.Windows.Data.Binding("Settings.UI.Expanded")
            {
                Mode = System.Windows.Data.BindingMode.TwoWay
            };
            expander.SetBinding(Expander.IsExpandedProperty, expandedBinding);

            // Create header
            expander.Header = CreateClassicHeader();

            // Create content
            expander.Content = CreateClassicContent();

            return expander;
        }
    }
}