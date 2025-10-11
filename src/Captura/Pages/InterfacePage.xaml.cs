using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Captura.ViewModels;
using Captura.Windows;
using FirstFloor.ModernUI.Presentation;

namespace Captura
{
    public partial class InterfacePage
    {
        public InterfacePage()
        {
            InitializeComponent();
            DataContextChanged += InterfacePage_DataContextChanged;
        }

        void InterfacePage_DataContextChanged(object Sender, DependencyPropertyChangedEventArgs E)
        {
            // Initialize the UI state when DataContext is set
            if (DataContext is ViewModelBase vm)
            {
                // The SelectedValuePath and SelectedValue binding should handle the combobox automatically
                // But we'll ensure it's set correctly just in case
                if (ThemeModeComboBox != null && !string.IsNullOrEmpty(vm.Settings.UI.ThemeMode))
                {
                    ThemeModeComboBox.SelectedValue = vm.Settings.UI.ThemeMode;
                }
            }
        }

        void SelectedAccentColorChanged(object Sender, RoutedPropertyChangedEventArgs<Color?> E)
        {
            if (E.NewValue != null && DataContext is ViewModelBase vm)
            {
                AppearanceManager.Current.AccentColor = E.NewValue.Value;

                vm.Settings.UI.AccentColor = E.NewValue.Value.ToString();
            }
        }

        void FollowSystemThemeClick(object Sender, RoutedEventArgs E)
        {
            if (DataContext is ViewModelBase vm)
            {
                if (vm.Settings.UI.FollowSystemTheme)
                {
                    // Apply system theme
                    ApplySystemTheme();
                }
                else
                {
                    // Apply manual theme selection
                    ApplyManualTheme(vm.Settings.UI.ThemeMode);
                }
            }
        }

        void ThemeModeChanged(object Sender, SelectionChangedEventArgs E)
        {
            if (DataContext is ViewModelBase vm && Sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var themeMode = selectedItem.Tag.ToString();
                    vm.Settings.UI.ThemeMode = themeMode;
                    
                    // Only apply if not following system theme
                    if (!vm.Settings.UI.FollowSystemTheme)
                    {
                        ApplyManualTheme(themeMode);
                    }
                }
            }
        }

        void ApplySystemTheme()
        {
            bool isDark = SystemThemeDetector.IsSystemDarkTheme();
            AppearanceManager.Current.ThemeSource = isDark
                ? AppearanceManager.DarkThemeSource
                : AppearanceManager.LightThemeSource;
        }

        void ApplyManualTheme(string themeMode)
        {
            AppearanceManager.Current.ThemeSource = themeMode == "Dark"
                ? AppearanceManager.DarkThemeSource
                : AppearanceManager.LightThemeSource;
        }
    }
}
