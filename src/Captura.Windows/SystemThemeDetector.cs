using Microsoft.Win32;
using System;

namespace Captura.Windows
{
    public static class SystemThemeDetector
    {
        private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string RegistryValueName = "AppsUseLightTheme";

        /// <summary>
        /// Determines if the system is using a dark theme.
        /// Returns true if dark theme is enabled, false if light theme is enabled.
        /// On non-Windows platforms or if unable to detect, returns false (light theme).
        /// </summary>
        public static bool IsSystemDarkTheme()
        {
            try
            {
                // Only works on Windows
                if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                {
                    return false;
                }

                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath))
                {
                    if (key != null)
                    {
                        var value = key.GetValue(RegistryValueName);
                        if (value != null && int.TryParse(value.ToString(), out int result))
                        {
                            // 0 = Dark theme, 1 = Light theme
                            return result == 0;
                        }
                    }
                }
            }
            catch
            {
                // If we can't detect, default to light theme
            }

            return false;
        }

        /// <summary>
        /// Event handler for system theme changes
        /// </summary>
        public static event EventHandler SystemThemeChanged;

        private static void OnSystemThemeChanged()
        {
            SystemThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Starts monitoring system theme changes
        /// </summary>
        public static void StartMonitoring()
        {
            // Note: For proper implementation, we would need to use SystemEvents.UserPreferenceChanged
            // This is a simplified version
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        /// <summary>
        /// Stops monitoring system theme changes
        /// </summary>
        public static void StopMonitoring()
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                OnSystemThemeChanged();
            }
        }
    }
}
