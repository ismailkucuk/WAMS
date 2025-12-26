using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace wam.Services
{
    public class LocalizationService
    {
        private const string SETTINGS_FILE = "language_settings.json";
        private static LocalizationService _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        public event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        private string _currentLanguage = "tr-TR";

        public string CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(value));
                }
            }
        }

        public static readonly string[] SupportedLanguages = { "tr-TR", "en-US" };

        private LocalizationService()
        {
            LoadLanguageSettings();
        }

        public async Task SetLanguageAsync(string languageCode)
        {
            if (!IsLanguageSupported(languageCode))
            {
                languageCode = "tr-TR";
            }

            CurrentLanguage = languageCode;
            ApplyLanguage(languageCode);
            await SaveLanguageSettingsAsync();
        }

        public bool IsLanguageSupported(string languageCode)
        {
            return Array.Exists(SupportedLanguages, l => l.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyLanguage(string languageCode)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null) return;

                // Mevcut dil kaynaklarını temizle
                var existingLanguages = new List<ResourceDictionary>();
                foreach (var dict in app.Resources.MergedDictionaries)
                {
                    if (dict.Source?.OriginalString?.Contains("Strings.") == true)
                    {
                        existingLanguages.Add(dict);
                    }
                }

                foreach (var dict in existingLanguages)
                {
                    app.Resources.MergedDictionaries.Remove(dict);
                }

                // Yeni dili yükle
                var languageUri = new Uri($"pack://application:,,,/Resources/Strings.{languageCode}.xaml");

                var languageDict = new ResourceDictionary { Source = languageUri };
                app.Resources.MergedDictionaries.Add(languageDict);

                // UI thread için CultureInfo güncelle
                var culture = new CultureInfo(languageCode);
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Language application failed: {ex.Message}");
            }
        }

        /// <summary>
        /// C# kodundan string'lere erişim için
        /// </summary>
        public string GetString(string key, string fallback = null)
        {
            try
            {
                var app = Application.Current;
                if (app?.Resources == null)
                    return fallback ?? key;

                if (app.Resources.Contains(key))
                {
                    return app.Resources[key]?.ToString() ?? fallback ?? key;
                }

                return fallback ?? key;
            }
            catch
            {
                return fallback ?? key;
            }
        }

        private void LoadLanguageSettings()
        {
            try
            {
                var path = GetSettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<LanguageSettings>(json);
                    CurrentLanguage = settings?.Language ?? "tr-TR";
                }
                else
                {
                    CurrentLanguage = "tr-TR";
                }

                ApplyLanguage(CurrentLanguage);
            }
            catch
            {
                CurrentLanguage = "tr-TR";
                ApplyLanguage(CurrentLanguage);
            }
        }

        private async Task SaveLanguageSettingsAsync()
        {
            try
            {
                var settings = new LanguageSettings { Language = CurrentLanguage };
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
                System.Diagnostics.Debug.WriteLine($"Failed to save language settings: {ex.Message}");
            }
        }

        private string GetSettingsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WAM");
            return Path.Combine(dir, SETTINGS_FILE);
        }

        private class LanguageSettings
        {
            public string Language { get; set; } = "tr-TR";
        }
    }

    public class LanguageChangedEventArgs : EventArgs
    {
        public string Language { get; }

        public LanguageChangedEventArgs(string language)
        {
            Language = language;
        }
    }
}
