using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace wam.Helpers
{
    /// <summary>
    /// Configuration model for application settings (snow effect, messages, etc.)
    /// Designed to be easily switchable from local file to remote URL.
    /// </summary>
    public class AppConfig
    {
        [JsonPropertyName("snow_effect")]
        public bool SnowEffect { get; set; } = false;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("snowflake_count")]
        public int SnowflakeCount { get; set; } = 50;

        [JsonPropertyName("min_speed")]
        public double MinSpeed { get; set; } = 1.0;

        [JsonPropertyName("max_speed")]
        public double MaxSpeed { get; set; } = 4.0;

        [JsonPropertyName("min_size")]
        public double MinSize { get; set; } = 3.0;

        [JsonPropertyName("max_size")]
        public double MaxSize { get; set; } = 8.0;
    }

    /// <summary>
    /// Service class to load configuration from local file or remote URL.
    /// Currently uses local file; can be extended for remote config easily.
    /// </summary>
    public static class ConfigService
    {
        private static readonly string LocalConfigPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "local_config.json");

        private static AppConfig _cachedConfig = null;

        /// <summary>
        /// Loads configuration from local JSON file.
        /// Returns default config if file doesn't exist or parsing fails.
        /// </summary>
        public static AppConfig LoadConfig()
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            try
            {
                if (!File.Exists(LocalConfigPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Config file not found at: {LocalConfigPath}");
                    _cachedConfig = CreateDefaultConfig();
                    return _cachedConfig;
                }

                string json = File.ReadAllText(LocalConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _cachedConfig = config ?? new AppConfig();
                System.Diagnostics.Debug.WriteLine($"Config loaded: SnowEffect={_cachedConfig.SnowEffect}, Message={_cachedConfig.Message}");
                return _cachedConfig;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Config load error: {ex.Message}");
                _cachedConfig = new AppConfig();
                return _cachedConfig;
            }
        }

        /// <summary>
        /// Async version for future remote config support.
        /// Currently wraps the sync method.
        /// </summary>
        public static async Task<AppConfig> LoadConfigAsync()
        {
            // FUTURE: Replace with HttpClient call to remote URL
            // Example:
            // var response = await _httpClient.GetStringAsync("https://raw.githubusercontent.com/.../config.json");
            // return JsonSerializer.Deserialize<AppConfig>(response);

            return await Task.Run(() => LoadConfig());
        }

        /// <summary>
        /// Clears the cached config to force reload on next call.
        /// </summary>
        public static void ReloadConfig()
        {
            _cachedConfig = null;
        }

        /// <summary>
        /// Creates and saves a default config file if none exists.
        /// </summary>
        private static AppConfig CreateDefaultConfig()
        {
            var defaultConfig = new AppConfig
            {
                SnowEffect = true,
                Message = "Happy Holidays!",
                SnowflakeCount = 50,
                MinSpeed = 1.0,
                MaxSpeed = 4.0,
                MinSize = 3.0,
                MaxSize = 8.0
            };

            try
            {
                string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(LocalConfigPath, json);
                System.Diagnostics.Debug.WriteLine($"Default config created at: {LocalConfigPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create default config: {ex.Message}");
            }

            return defaultConfig;
        }
    }
}
