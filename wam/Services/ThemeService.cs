using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace wam.Services
{
    public class ThemeService
    {
        private const string SETTINGS_FILE = "theme_settings.json";
        private static ThemeService _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        public event EventHandler<ThemeChangedEventArgs> ThemeChanged;

        private ThemeMode _currentTheme = ThemeMode.Light;

        public ThemeMode CurrentTheme
        {
            get => _currentTheme;
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(value));
                }
            }
        }

        private ThemeService()
        {
            LoadThemeSettings();
        }

        public async Task SetThemeAsync(ThemeMode theme)
        {
            CurrentTheme = theme;
            ApplyTheme(theme);
            await SaveThemeSettingsAsync();
        }

        private void ApplyTheme(ThemeMode theme)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // Mevcut tema kaynaklarını temizle
                var existingThemes = new List<ResourceDictionary>();
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    if (dict.Source?.OriginalString?.Contains("Theme.xaml") == true)
                    {
                        existingThemes.Add(dict);
                    }
                }
                
                foreach (var dict in existingThemes)
                {
                    app.Resources.MergedDictionaries.Remove(dict);
                }

                // Yeni temayı yükle
                var themeUri = theme == ThemeMode.Dark
                    ? new Uri("pack://application:,,,/Themes/DarkTheme.xaml")
                    : new Uri("pack://application:,,,/Themes/LightTheme.xaml");

                var themeDict = new ResourceDictionary { Source = themeUri };
                app.Resources.MergedDictionaries.Add(themeDict);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme application failed: {ex.Message}");
            }
        }

        private void LoadThemeSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                    CurrentTheme = settings?.Theme ?? ThemeMode.Light;
                }
                else
                {
                    CurrentTheme = ThemeMode.Light;
                }
                
                ApplyTheme(CurrentTheme);
            }
            catch
            {
                CurrentTheme = ThemeMode.Light;
                ApplyTheme(CurrentTheme);
            }
        }

        private async Task SaveThemeSettingsAsync()
        {
            try
            {
                var settings = new ThemeSettings { Theme = CurrentTheme };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                var path = GetSettingsPath();
                
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save theme settings: {ex.Message}");
            }
        }

        private string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM");
            return Path.Combine(dir, SETTINGS_FILE);
        }

        private class ThemeSettings
        {
            public ThemeMode Theme { get; set; } = ThemeMode.Light;
        }
    }

    public enum ThemeMode
    {
        Light,
        Dark
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public ThemeMode Theme { get; }

        public ThemeChangedEventArgs(ThemeMode theme)
        {
            Theme = theme;
        }
    }
}